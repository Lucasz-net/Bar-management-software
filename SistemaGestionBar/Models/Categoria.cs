using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    /// <summary>Tabla Categoria. Agrupa los productos del catálogo.</summary>
    public class Categoria : EntidadAuditable
    {
        public int IdCategoria { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
