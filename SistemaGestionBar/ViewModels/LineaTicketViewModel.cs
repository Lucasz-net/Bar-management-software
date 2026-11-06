using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    public class LineaTicketViewModel : ViewModelBase
    {
        public LineaTicketViewModel(ProductoCatalogoViewModel producto)
        {
            Producto = producto;
            _cantidad = 1;
        }

        public ProductoCatalogoViewModel Producto { get; }

        public int IdProducto => Producto.IdProducto;
        public string Nombre => Producto.Nombre;
        public decimal PrecioUnitario => Producto.Precio;

        private int _cantidad;
        public int Cantidad
        {
            get => _cantidad;
            set
            {
                if (SetProperty(ref _cantidad, value))
                    OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal Subtotal => Cantidad * PrecioUnitario;

        public VentaDetalle ConvertirADetalle()
        {
            var detalle = new VentaDetalle
            {
                IdProducto = IdProducto,
                Cantidad = Cantidad,
                PrecioUnitario = PrecioUnitario,
                Producto = Producto.Producto
            };
            detalle.RecalcularSubtotal();
            return detalle;
        }
    }
}
