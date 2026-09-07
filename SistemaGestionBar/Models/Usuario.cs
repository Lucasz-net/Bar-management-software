namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Usuario. Credenciales de acceso al sistema.
    /// Los datos personales viven en Persona (RF-10), por eso el login se valida
    /// con Persona.Email + Usuario.Clave. La clave se guarda siempre hasheada (RF-09).
    /// </summary>
    public class Usuario : EntidadAuditable
    {
        public int IdUsuario { get; set; }
        public int IdPersona { get; set; }
        public int IdRol { get; set; }

        /// <summary>Hash PBKDF2 de la contraseña. Nunca texto plano.</summary>
        public string Clave { get; set; } = string.Empty;

        // Navegación
        public Persona Persona { get; set; } = null!;
        public Rol Rol { get; set; } = null!;

        public string NombreCompleto => Persona?.Nombre ?? string.Empty;
        public string NombreRol => Rol?.NombreRol ?? string.Empty;
        public bool EsAdministrador => NombreRol == RolesSistema.Administrador;
        public bool EsGerente => NombreRol == RolesSistema.Gerente;

        /// <summary>
        /// Quiénes entran al tablero en lugar del punto de venta (RF-09).
        /// El administrador ve todo; el gerente, solo lo que hace a la gestión del negocio.
        /// </summary>
        public bool AccedeAlTablero => EsAdministrador || EsGerente;
    }
}
