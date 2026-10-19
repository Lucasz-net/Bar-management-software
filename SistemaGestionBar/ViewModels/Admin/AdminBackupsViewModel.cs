using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Data;
using SistemaGestionBar.Services;
using MySqlConnector;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminBackupsViewModel : SeccionAdminViewModel
    {
        private readonly ServicioBackupSql _servicioBackup;

        public AdminBackupsViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Backups, "Respaldos", "Database", "Gestión de copias de seguridad de la base de datos")
        {
            _servicioBackup = new ServicioBackupSql();
            ArchivosRespaldo = new ObservableCollection<ArchivoBackupViewModel>();

            CrearBackupCommand = new RelayCommand(EjecutarCrearBackup);
            RestaurarBackupCommand = new RelayCommand<ArchivoBackupViewModel>(EjecutarRestaurarBackup);
            EliminarBackupCommand = new RelayCommand<ArchivoBackupViewModel>(EjecutarEliminarBackup);
        }

        public ObservableCollection<ArchivoBackupViewModel> ArchivosRespaldo { get; }

        public ICommand CrearBackupCommand { get; }
        public ICommand RestaurarBackupCommand { get; }
        public ICommand EliminarBackupCommand { get; }

        public override void Recargar()
        {
            LimpiarMensaje();
            CargarArchivosLocales();
            ConfigurarFiltro(ArchivosRespaldo);
            RefrescarVista();
        }

        protected override bool Coincide(object item)
        {
            if (item is not ArchivoBackupViewModel archivo) return false;
            return string.IsNullOrWhiteSpace(Termino) || Contiene(archivo.Nombre, Termino);
        }

        private void CargarArchivosLocales()
        {
            ArchivosRespaldo.Clear();
            string directorio = _servicioBackup.ObtenerDirectorioBackups();

            var archivos = new DirectoryInfo(directorio).GetFiles("*.sql")
                                                        .OrderByDescending(f => f.CreationTime)
                                                        .ToList();

            foreach (var file in archivos)
            {
                ArchivosRespaldo.Add(new ArchivoBackupViewModel
                {
                    Nombre = file.Name,
                    RutaCompleta = file.FullName,
                    Fecha = file.CreationTime.ToString("dd/MM/yyyy HH:mm"),
                    Tamanio = (file.Length / 1024.0).ToString("0.##") + " KB"
                });
            }
        }

        private async void EjecutarCrearBackup()
        {
            var db = CadenaDeConexion.Parsear();
            Informar("Creando copia de seguridad. Por favor espere...");

            var resultado = await _servicioBackup.CrearBackupManualAsync(db.Server, db.Port, db.Database, db.User, db.Password);

            Aplicar(resultado);
            if (resultado.Exito) CargarArchivosLocales();
        }

        private async void EjecutarRestaurarBackup(ArchivoBackupViewModel archivo)
        {
            bool seguro = Dialogo.Confirmar("Peligro de pérdida de datos",
                $"¿Estás seguro de restaurar el backup '{archivo.Nombre}'?\n\nEsto reemplazará la base de datos y se perderá cualquier información no respaldada. El sistema se cerrará tras finalizar.");

            if (!seguro) return;

            try
            {
                var db = CadenaDeConexion.Parsear();
                Informar("Restaurando la base de datos...");

                // Limpiamos los pools de conexiones para que MySQL no bloquee la base de datos.
                MySqlConnection.ClearAllPools();

                var resultado = await _servicioBackup.RestaurarBackupAsync(archivo.RutaCompleta, db.Server, db.Port, db.Database, db.User, db.Password);

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
                if (File.Exists(archivo.RutaCompleta)) File.Delete(archivo.RutaCompleta);
                CargarArchivosLocales();
                Informar("Archivo eliminado correctamente.");
            }
            catch (System.Exception ex)
            {
                Fallar("No se pudo eliminar el archivo: " + ex.Message);
            }
        }
    }

    public class ArchivoBackupViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string RutaCompleta { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Tamanio { get; set; } = string.Empty;
    }
}