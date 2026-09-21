using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using SistemaGestionBar.ViewModels;
using SistemaGestionBar.Views;

namespace SistemaGestionBar.Services
{
    /// <summary>Implementación WPF de IServicioDialogo. Es la única clase que abre ventanas.</summary>
    public class ServicioDialogo : IServicioDialogo
    {
        public void MostrarReceta(RecetaViewModel receta)
        {
            var ventana = new RecetaWindow
            {
                DataContext = receta,
                Owner = VentanaActiva()
            };
            ventana.ShowDialog();
        }

        public void MostrarFactura(ViewModels.FacturaViewModel factura)
        {
            var ventana = new Views.FacturaWindow
            {
                DataContext = factura,
                Owner = VentanaActiva()
            };
            ventana.ShowDialog();
        }

        /// <summary>
        /// El ViewModel no conoce WPF, así que no puede cerrar su propia ventana: avisa con
        /// <c>CierreSolicitado</c> cuando el ajuste se guardó y el cierre lo hace acá, que es
        /// la única clase que sabe qué es una Window. Cancelar cierra por IsCancel y deja
        /// <c>SeAplico</c> en false.
        /// </summary>
        public bool AjustarStock(AjusteDeStockViewModel ajuste)
        {
            var ventana = new AjusteStockWindow
            {
                DataContext = ajuste,
                Owner = VentanaActiva()
            };

            void Cerrar(object? origen, EventArgs e) => ventana.Close();

            ajuste.CierreSolicitado += Cerrar;
            ventana.ShowDialog();
            ajuste.CierreSolicitado -= Cerrar;

            return ajuste.SeAplico;
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

        /// <summary>
        /// UseShellExecute es obligatorio: sin eso Windows intenta ejecutar el PDF como si
        /// fuera un programa en vez de abrirlo con el lector asociado.
        /// </summary>
        public void AbrirArchivo(string ruta) =>
            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });

        private static Window VentanaActiva() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? Application.Current.MainWindow;
    }
}
