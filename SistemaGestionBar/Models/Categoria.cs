using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    public class Categoria : EntidadAuditable
    {
        public int IdCategoria { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
