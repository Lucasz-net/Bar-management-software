namespace SistemaGestionBar.Models
{
    public class Receta : EntidadAuditable
    {
        public int IdProducto { get; set; }

        /// <summary>Un paso por línea.</summary>
        public string Instrucciones { get; set; } = string.Empty;

        public Producto Producto { get; set; } = null!;
    }
}
