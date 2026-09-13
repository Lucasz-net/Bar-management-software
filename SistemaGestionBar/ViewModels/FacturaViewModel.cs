using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Estado de la ventana del comprobante. Expone la factura y los dos comandos
    /// de la ventana, para que la vista no necesite manejadores en el code-behind.
    ///
    /// Imprimir es una operación de presentación pura —dibuja el visual de la
    /// ventana— así que el ViewModel recibe qué imprimir y no sabe nada de WPF
    /// más allá de eso.
    /// </summary>
    public class FacturaViewModel : ViewModelBase
    {
        public FacturaViewModel(Factura factura)
        {
            Factura = factura;

            ImprimirCommand = new RelayCommand<Visual>(Imprimir, v => v is not null);
            CerrarCommand = new RelayCommand<Window>(ventana => ventana?.Close());
        }

        public Factura Factura { get; }

        public string Titulo => $"Factura {Factura.Numero}";

        public ICommand ImprimirCommand { get; }
        public ICommand CerrarCommand { get; }

        private void Imprimir(Visual contenido)
        {
            var dialogo = new System.Windows.Controls.PrintDialog();
            if (dialogo.ShowDialog() != true)
                return;

            dialogo.PrintVisual(contenido, $"Factura {Factura.Numero}");
        }
    }
}
