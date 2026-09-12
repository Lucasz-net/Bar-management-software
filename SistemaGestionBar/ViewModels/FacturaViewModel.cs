using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    public class FacturaViewModel : ViewModelBase
    {
        public FacturaViewModel(Factura factura)
        {
            Factura = factura;
        }

        public Factura Factura { get; }
    }
}
