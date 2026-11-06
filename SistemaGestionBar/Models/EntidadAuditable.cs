using System;

namespace SistemaGestionBar.Models
{
    /// <summary>Campos de auditoría que BarDbContext completa al guardar.</summary>
    public abstract class EntidadAuditable
    {
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        public int? UsuarioModificacion { get; set; }
    }
}
