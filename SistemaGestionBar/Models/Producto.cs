using System.Collections.Generic;
using System.Linq;

namespace SistemaGestionBar.Models
{
    public class Producto : EntidadAuditable
    {
        public int IdProducto { get; set; }
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }

        /// <summary>Umbral de reposición, solo para productos de venta directa.</summary>
        public int StockMinimo { get; set; }

        /// <summary>Ruta de la foto; no se guarda el binario en la base.</summary>
        public string? RutaImagen { get; set; }

        public Categoria Categoria { get; set; } = null!;
        public ICollection<ProductoIngrediente> Ingredientes { get; set; } = new List<ProductoIngrediente>();

        public Receta? Receta { get; set; }

        public bool TieneReceta => Ingredientes.Any();

        public bool StockBajo => !TieneReceta && StockMinimo > 0 && Stock <= StockMinimo;
    }
}
