using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminBackupsViewModel : SeccionAdminViewModel
    {
        private readonly ServicioBackupSql _servicioBackup;
        private readonly Usuario _usuarioActual;

        public AdminBackupsViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, Usuario usuarioActual)
            : base(repositorio, dialogo, DestinoAdmin.Backups, "Respaldos", "Database", "Gestión de copias de seguridad de la base de datos")
        {
            _servicioBackup = new ServicioBackupSql();
            _usuarioActual = usuarioActual;
            ArchivosRespaldo = new ObservableCollection<ArchivoBackupViewModel>();

            CrearBackupCommand = new RelayCommand(EjecutarCrearBackup);
            RestaurarBackupCommand = new RelayCommand<ArchivoBackupViewModel>(EjecutarRestaurarBackup);
            EliminarBackupCommand = new RelayCommand<ArchivoBackupViewModel>(EjecutarEliminarBackup);
        }

        public ObservableCollection<ArchivoBackupViewModel> ArchivosRespaldo { get; }

        public ICommand CrearBackupCommand { get; }
        public ICommand RestaurarBackupCommand { get; }
        public ICommand EliminarBackupCommand { get; }

        public IReadOnlyList<OpcionFiltro<bool>> OpcionesOrden { get; } = new[]
        {
            new OpcionFiltro<bool>("Más nuevo primero", true),
            new OpcionFiltro<bool>("Más antiguo primero", false)
        };

        private OpcionFiltro<bool> _orden = null!;
        public OpcionFiltro<bool> Orden
        {
            get => _orden;
            set
            {
                if (value is not null && SetProperty(ref _orden, value))
                    AplicarOrden();
            }
        }

        private void AplicarOrden()
        {
            if (Vista is null)
                return;

            Vista.SortDescriptions.Clear();
            Vista.SortDescriptions.Add(new System.ComponentModel.SortDescription(
                nameof(ArchivoBackupViewModel.FechaHora),
                Orden.Valor ? System.ComponentModel.ListSortDirection.Descending
                            : System.ComponentModel.ListSortDirection.Ascending));
        }

        public bool HayBackups => ArchivosRespaldo.Count > 0;

        public string CantidadTexto => ArchivosRespaldo.Count == 1 ? "1 copia" : $"{ArchivosRespaldo.Count} copias";

        public string UltimaCopiaTexto => HayBackups ? ArchivosRespaldo[0].FechaRelativa : "Todavía no hay ninguna";

        public string EspacioTotalTexto => FormatearTamanio(ArchivosRespaldo.Sum(a => a.TamanioBytes));

        public override void Recargar()
        {
            LimpiarMensaje();
            CargarArchivosLocales();
            ConfigurarFiltro(ArchivosRespaldo);
            _orden ??= OpcionesOrden[0];
            OnPropertyChanged(nameof(Orden));
            AplicarOrden();
            RefrescarVista();
        }

        protected override bool Coincide(object item)
        {
            if (item is not ArchivoBackupViewModel archivo) return false;
            return string.IsNullOrWhiteSpace(Termino)
                || Contiene(archivo.Nombre, Termino)
                || Contiene(archivo.Motivo, Termino)
                || Contiene(archivo.CreadoPor, Termino);
        }

        private void CargarArchivosLocales()
        {
            ArchivosRespaldo.Clear();
            string directorio = _servicioBackup.ObtenerDirectorioBackups();

            var archivos = new DirectoryInfo(directorio).GetFiles("*.sql")
                                                        .OrderByDescending(f => f.CreationTime)
                                                        .ToList();

            for (int i = 0; i < archivos.Count; i++)
            {
                var file = archivos[i];
                var metadatos = ServicioBackupSql.LeerMetadatos(file.FullName);
                var fecha = metadatos?.Fecha ?? file.CreationTime;

                ArchivosRespaldo.Add(new ArchivoBackupViewModel
                {
                    Nombre = file.Name,
                    RutaCompleta = file.FullName,
                    FechaHora = fecha,
                    Fecha = fecha.ToString("dd/MM/yyyy HH:mm"),
                    FechaRelativa = FormatearRelativo(fecha),
                    CreadoPor = string.IsNullOrWhiteSpace(metadatos?.CreadoPor) ? "No registrado" : metadatos.CreadoPor,
                    Motivo = string.IsNullOrWhiteSpace(metadatos?.Motivo) ? "Sin motivo registrado" : metadatos.Motivo,
                    TamanioBytes = file.Length,
                    Tamanio = FormatearTamanio(file.Length),
                    EsElMasReciente = i == 0
                });
            }

            OnPropertyChanged(nameof(HayBackups));
            OnPropertyChanged(nameof(CantidadTexto));
            OnPropertyChanged(nameof(UltimaCopiaTexto));
            OnPropertyChanged(nameof(EspacioTotalTexto));
        }

        private async void EjecutarCrearBackup()
        {
            var confirmacion = new ConfirmarContrasenaViewModel(Repositorio,
                "Ingresá tu contraseña para crear una copia de seguridad.");
            if (!Dialogo.PedirConfirmacionDeContrasena(confirmacion))
                return;

            string? motivo = Dialogo.PedirMotivoDeBackup();
            if (motivo is null)
                return;

            Informar("Creando copia de seguridad. Por favor espere...");

            var resultado = await _servicioBackup.CrearBackupManualAsync(motivo, _usuarioActual.NombreCompleto);

            Aplicar(resultado);
            if (resultado.Exito) CargarArchivosLocales();
        }

        private async void EjecutarRestaurarBackup(ArchivoBackupViewModel archivo)
        {
            var autorizacion = new AutorizacionDobleViewModel(Repositorio,
                "Restaurar reemplaza toda la base de datos. Hace falta la autorización de un administrador y de un gerente.",
                _usuarioActual);

            if (!Dialogo.PedirAutorizacionDoble(autorizacion)) return;

            bool seguro = Dialogo.Confirmar("Peligro de pérdida de datos",
                $"¿Estás seguro de restaurar el backup '{archivo.Nombre}'?\n\nEsto reemplazará la base de datos y se perderá cualquier información no respaldada. El sistema se cerrará tras finalizar.");

            if (!seguro) return;

            try
            {
                Informar("Restaurando la base de datos...");

                var resultado = await _servicioBackup.RestaurarBackupAsync(archivo.RutaCompleta);

                if (resultado.Exito)
                {
                    Dialogo.Informar("Restauración Completada", resultado.Mensaje);
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    Fallar(resultado.Mensaje);
                }
            }
            catch (System.Exception ex)
            {
                Fallar($"Error inesperado al restaurar: {ex.Message}");
            }
        }

        private void EjecutarEliminarBackup(ArchivoBackupViewModel archivo)
        {
            if (!Dialogo.Confirmar("Eliminar Backup", $"¿Eliminar el archivo '{archivo.Nombre}' de tu computadora?")) return;

            try
            {
                ServicioBackupSql.EliminarConMetadatos(archivo.RutaCompleta);
                CargarArchivosLocales();
                Informar("Archivo eliminado correctamente.");
            }
            catch (System.Exception ex)
            {
                Fallar("No se pudo eliminar el archivo: " + ex.Message);
            }
        }

        private static string FormatearRelativo(DateTime fecha)
        {
            int dias = (DateTime.Now.Date - fecha.Date).Days;
            string hora = fecha.ToString("HH:mm");

            return dias switch
            {
                0 => $"Hoy, {hora}",
                1 => $"Ayer, {hora}",
                < 7 => $"Hace {dias} días",
                _ => $"{fecha:dd/MM/yyyy}, {hora}"
            };
        }

        private static string FormatearTamanio(long bytes)
        {
            double kb = bytes / 1024.0;
            return kb >= 1024
                ? (kb / 1024.0).ToString("0.##") + " MB"
                : kb.ToString("0.##") + " KB";
        }
    }

    public class ArchivoBackupViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string RutaCompleta { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string FechaRelativa { get; set; } = string.Empty;
        public string CreadoPor { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Tamanio { get; set; } = string.Empty;
        public long TamanioBytes { get; set; }
        public bool EsElMasReciente { get; set; }
    }
}
