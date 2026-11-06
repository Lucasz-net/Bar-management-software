namespace SistemaGestionBar.Models
{
    public class VentaDetalle
    {
        public int IdVentaDetalle { get; set; }
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        /// <summary>Precio al momento de la venta, para no alterar tickets históricos.</summary>
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }

        public Venta Venta { get; set; } = null!;
        public Producto Producto { get; set; } = null!;

        public void RecalcularSubtotal() => Subtotal = Cantidad * PrecioUnitario;
    }
}
