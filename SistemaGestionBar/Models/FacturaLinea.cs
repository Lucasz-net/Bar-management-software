namespace SistemaGestionBar.Models
{
    public class FacturaLinea
    {
        public int IdFacturaLinea { get; set; }
        public int IdFactura { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
