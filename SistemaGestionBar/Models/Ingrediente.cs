namespace SistemaGestionBar.Models
{
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
