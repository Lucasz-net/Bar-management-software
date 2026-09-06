namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Ingrediente. Insumo que se descuenta al vender un producto con receta (RF-07)
    /// y que dispara alertas visuales al llegar al mínimo (RF-08).
    /// </summary>
    public class Ingrediente : EntidadAuditable
    {
        public int IdIngrediente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal StockMinimo { get; set; }

        public bool StockBajo => Stock <= StockMinimo;
    }
}
