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

    /// <summary>
    /// Nombres de rol conocidos, para no repartir literales por todo el código.
    ///
    /// Vendedor y Mesero describen el puesto, no el permiso: las dos cuentas hacen
    /// exactamente lo mismo en el sistema (ver <see cref="Usuario.EsPersonalDeAtencion"/>).
    /// El rol Mesero se conserva porque una venta en mesa tiene que registrar quién
    /// atendió (RF-02) y ahí el puesto sí es un dato del negocio.
    /// </summary>
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string Gerente = "Gerente";
        public const string Vendedor = "Vendedor";
        public const string Mesero = "Mesero";
    }
}
