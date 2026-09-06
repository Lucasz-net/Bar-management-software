using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    /// <summary>Tabla Rol. Define el perfil de acceso del usuario (RF-09).</summary>
    public class Rol : EntidadAuditable
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    /// <summary>Nombres de rol conocidos, para no repartir literales por todo el código.</summary>
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string Vendedor = "Vendedor";
        public const string Mesero = "Mesero";
    }
}
