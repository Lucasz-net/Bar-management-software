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

        public void Informar(string titulo, string mensaje) =>
            MessageBox.Show(VentanaActiva(), mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);

        public bool Confirmar(string titulo, string mensaje) =>
            MessageBox.Show(VentanaActiva(), mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;

        private static Window VentanaActiva() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? Application.Current.MainWindow;
    }
}
