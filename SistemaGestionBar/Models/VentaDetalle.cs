namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Venta_Detalle (renglón del ticket).
    /// Guarda PrecioUnitario como foto del precio al momento de la venta: si mañana
    /// cambia el precio del producto, los tickets históricos no se alteran.
    /// </summary>
    public class VentaDetalle
    {
        public int IdVentaDetalle { get; set; }
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        /// <summary>Persistido según el DER, pero siempre igual a Cantidad * PrecioUnitario.</summary>
        public decimal Subtotal { get; set; }

        // Navegación
        public Venta Venta { get; set; } = null!;
        public Producto Producto { get; set; } = null!;

        public void RecalcularSubtotal() => Subtotal = Cantidad * PrecioUnitario;
    }
}
