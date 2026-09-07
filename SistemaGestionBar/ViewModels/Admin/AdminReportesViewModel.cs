using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// RF-14. Genera informes en CSV y deja el registro en la tabla Reporte:
    /// qué se generó, quién lo pidió y dónde quedó el archivo.
    /// </summary>
    public class AdminReportesViewModel : SeccionAdminViewModel
    {
        private readonly ServicioReportes _servicio;
        private readonly SesionActual _sesion;

        public AdminReportesViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, SesionActual sesion)
            : base(repositorio, dialogo, "Reportes", "ChartBoxOutline",
                   "Informes estadísticos y su bitácora de generación")
        {
            _servicio = new ServicioReportes(repositorio);
            _sesion = sesion;

            Tipos = new ObservableCollection<TipoReporte>(Enum.GetValues<TipoReporte>());
            Historial = new ObservableCollection<Reporte>();
            TipoSeleccionado = TipoReporte.VentasPorPeriodo;

            GenerarCommand = new RelayCommand(Generar);
            AbrirCarpetaCommand = new RelayCommand(AbrirCarpeta);
            AbrirArchivoCommand = new RelayCommand<Reporte>(AbrirArchivo);

            Recargar();
        }

        public ObservableCollection<TipoReporte> Tipos { get; }
        public ObservableCollection<Reporte> Historial { get; }

        private TipoReporte _tipoSeleccionado;
        public TipoReporte TipoSeleccionado
        {
            get => _tipoSeleccionado;
            set
            {
                if (SetProperty(ref _tipoSeleccionado, value))
                    OnPropertyChanged(nameof(DescripcionTipo));
            }
        }

        public string DescripcionTipo => TipoSeleccionado switch
        {
            TipoReporte.VentasPorPeriodo => "Todas las ventas registradas, con cajero, mesero, ubicación y total.",
            TipoReporte.ProductosMasVendidos => "Ranking de productos por unidades vendidas y facturación.",
            TipoReporte.StockCritico => "Insumos y productos que llegaron a su stock mínimo.",
            TipoReporte.CierreDeCaja => "Totales cobrados hoy, agrupados por método de pago.",
            _ => string.Empty
        };

        public string CarpetaDestino => ServicioReportes.CarpetaDestino;

        public bool HayHistorial => Historial.Count > 0;

        public ICommand GenerarCommand { get; }
        public ICommand AbrirCarpetaCommand { get; }
        public ICommand AbrirArchivoCommand { get; }

        public sealed override void Recargar()
        {
            Historial.Clear();
            foreach (var r in Repositorio.ObtenerReportes())
                Historial.Add(r);

            OnPropertyChanged(nameof(HayHistorial));
        }

        private void Generar()
        {
            if (_sesion.Usuario is null)
            {
                Fallar("No hay una sesión activa.");
                return;
            }

            if (Aplicar(_servicio.Generar(TipoSeleccionado, _sesion.Usuario)))
                Recargar();
        }

        private void AbrirCarpeta()
        {
            try
            {
                Directory.CreateDirectory(CarpetaDestino);
                Process.Start(new ProcessStartInfo(CarpetaDestino) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Fallar($"No se pudo abrir la carpeta: {ex.Message}");
            }
        }

        private void AbrirArchivo(Reporte reporte)
        {
            if (string.IsNullOrWhiteSpace(reporte.RutaArchivo) || !File.Exists(reporte.RutaArchivo))
            {
                Fallar("El archivo ya no está en esa ruta.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(reporte.RutaArchivo) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Fallar($"No se pudo abrir el archivo: {ex.Message}");
            }
        }
    }
}
