namespace SistemaGestionBar.Models
{
    public class ProductoIngrediente
    {
        public int IdProducto { get; set; }
        public int IdIngrediente { get; set; }
        public decimal CantidadNecesaria { get; set; }

        public Producto Producto { get; set; } = null!;
        public Ingrediente Ingrediente { get; set; } = null!;

        public string Descripcion => $"{CantidadNecesaria:0.##} {Ingrediente?.UnidadMedida} de {Ingrediente?.Nombre}";
    }
}
