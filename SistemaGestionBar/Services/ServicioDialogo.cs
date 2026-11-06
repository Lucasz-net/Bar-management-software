using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using SistemaGestionBar.ViewModels;
using SistemaGestionBar.Views;

namespace SistemaGestionBar.Services
{
    public class ServicioDialogo : IServicioDialogo
    {
        public void MostrarReceta(RecetaViewModel receta) => Mostrar(new RecetaWindow(), receta);

        public void MostrarFactura(FacturaViewModel factura) => Mostrar(new FacturaWindow(), factura);

        public bool AjustarStock(AjusteDeStockViewModel ajuste)
        {
            MostrarCerrable(new AjusteStockWindow(), ajuste);
            return ajuste.SeAplico;
        }

        public bool PedirAutorizacionSupervisor(AutorizacionViewModel autorizacion)
        {
            MostrarCerrable(new AutorizacionWindow(), autorizacion);
            return autorizacion.EstaAutorizado;
        }

        public bool PedirConfirmacionDeContrasena(ConfirmarContrasenaViewModel confirmacion)
        {
            MostrarCerrable(new ConfirmarContrasenaWindow(), confirmacion);
            return confirmacion.EstaConfirmado;
        }

        public bool PedirAutorizacionDoble(AutorizacionDobleViewModel autorizacion)
        {
            MostrarCerrable(new AutorizacionDobleWindow(), autorizacion);
            return autorizacion.EstaAutorizado;
        }

        public string? PedirMotivoDeBackup()
        {
            var motivo = new MotivoBackupViewModel();
            MostrarCerrable(new MotivoBackupWindow(), motivo);
            return motivo.EstaConfirmado ? motivo.MotivoLimpio : null;
        }

        public (DateTime Desde, DateTime Hasta)? ElegirRangoDeFechas(DateTime? desde, DateTime? hasta)
        {
            var rango = new RangoFechasViewModel(desde, hasta);
            MostrarCerrable(new RangoFechasWindow(), rango);

            return rango.SeAplico && rango.Desde is DateTime d && rango.Hasta is DateTime h
                ? (d, h)
                : null;
        }

        public (bool SeAplico, string? Ruta) ElegirImagenDeProducto(string? rutaActual)
        {
            var eleccion = new ElegirImagenViewModel(rutaActual);
            MostrarCerrable(new ElegirImagenWindow(), eleccion);
            return (eleccion.SeAplico, eleccion.Elegida);
        }

        public void Informar(string titulo, string mensaje) =>
            MessageBox.Show(VentanaActiva(), mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);

        public bool Confirmar(string titulo, string mensaje) =>
            MessageBox.Show(VentanaActiva(), mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;

        public string? ElegirDondeGuardarPdf(string nombreSugerido)
        {
            var cuadro = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Guardar reporte",
                FileName = nombreSugerido,
                DefaultExt = ".pdf",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            return cuadro.ShowDialog(VentanaActiva()) == true ? cuadro.FileName : null;
        }

        public string? ElegirArchivoSql(string directorioInicial)
        {
            var cuadro = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar Backup a Restaurar",
                DefaultExt = ".sql",
                Filter = "Archivos SQL (*.sql)|*.sql",
                InitialDirectory = directorioInicial
            };

            return cuadro.ShowDialog(VentanaActiva()) == true ? cuadro.FileName : null;
        }

        public void AbrirArchivo(string ruta) =>
            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });

        private static void Mostrar(Window ventana, object viewModel)
        {
            ventana.DataContext = viewModel;
            ventana.Owner = VentanaActiva();
            ventana.ShowDialog();
        }

        private static void MostrarCerrable<TVentana, TViewModel>(TVentana ventana, TViewModel viewModel)
            where TVentana : Window
            where TViewModel : ICierrable
        {
            void Cerrar(object? origen, EventArgs e) => ventana.Close();

            viewModel.CierreSolicitado += Cerrar;
            Mostrar(ventana, viewModel);
            viewModel.CierreSolicitado -= Cerrar;
        }

        private static Window VentanaActiva() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? Application.Current.MainWindow;
    }
}
