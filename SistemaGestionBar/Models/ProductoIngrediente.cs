namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla intermedia Producto_Ingrediente: la receta. Indica cuánto consume de cada
    /// insumo UNA unidad del producto. Clave primaria compuesta (IdProducto, IdIngrediente).
    /// </summary>
    public class ProductoIngrediente
    {
        public int IdProducto { get; set; }
        public int IdIngrediente { get; set; }
        public decimal CantidadNecesaria { get; set; }

        // Navegación
        public Producto Producto { get; set; } = null!;
        public Ingrediente Ingrediente { get; set; } = null!;

        /// <summary>Renglón que se muestra en el modal de receta. Ej: "50 ml de Ron Blanco".</summary>
        public string Descripcion => $"{CantidadNecesaria:0.##} {Ingrediente?.UnidadMedida} de {Ingrediente?.Nombre}";
    }
}
