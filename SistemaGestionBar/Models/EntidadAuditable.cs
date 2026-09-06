using System;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Campos de auditoría exigidos por RF-11. Todas las entidades del sistema heredan
    /// de aquí para registrar cuándo se creó/modificó el registro y quién lo hizo.
    /// En EF Core estas columnas se completan interceptando SaveChanges().
    /// </summary>
    public abstract class EntidadAuditable
    {
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        /// <summary>Id del usuario que realizó la última modificación.</summary>
        public int? UsuarioModificacion { get; set; }
    }
}
