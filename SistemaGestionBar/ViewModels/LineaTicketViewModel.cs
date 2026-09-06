using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// Un renglón del ticket en pantalla. Al confirmar la venta se convierte
    /// en una fila de Venta_Detalle (RF-01).
    /// </summary>
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

        /// <summary>Subtotal del renglón. La sumatoria de estos es el total de la venta.</summary>
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
