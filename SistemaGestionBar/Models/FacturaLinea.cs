namespace SistemaGestionBar.Models
{
    /// <summary>
    /// EXTENSIÓN AL DER. Renglón de la factura. Igual que Venta_Detalle no lleva
    /// auditoría propia: se audita el comprobante completo, no cada línea.
    ///
    /// El nombre del producto va copiado, no por clave foránea: ver <see cref="Factura"/>.
    /// </summary>
    public class FacturaLinea
    {
        public int IdFacturaLinea { get; set; }
        public int IdFactura { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
