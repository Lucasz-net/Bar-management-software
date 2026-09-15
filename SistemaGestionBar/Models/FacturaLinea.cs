namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Renglón de la factura. NO es una entidad de EF Core: se arma en memoria junto
    /// con <see cref="Factura"/>, a partir de un <see cref="VentaDetalle"/>.
    ///
    /// El nombre del producto va copiado (no por clave foránea) igual que antes: ver
    /// <see cref="Factura"/> para la salvedad sobre nombres que cambian después.
    /// </summary>
    public class FacturaLinea
    {
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
