using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    public class Rol : EntidadAuditable
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string Gerente = "Gerente";
        public const string Vendedor = "Vendedor";
        public const string Mesero = "Mesero";
    }
}
