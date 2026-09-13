namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Usuario. Cuenta de acceso al sistema de una persona que trabaja en el bar.
    /// Los datos personales viven en Persona (RF-10); acá va únicamente lo laboral:
    /// el correo de trabajo —que es el nombre de usuario— y la clave hasheada (RF-09).
    /// </summary>
    public class Usuario : EntidadAuditable
    {
        public int IdUsuario { get; set; }
        public int IdPersona { get; set; }
        public int IdRol { get; set; }

        /// <summary>
        /// EXTENSIÓN AL DER. Correo de trabajo y credencial de acceso. Es distinto del
        /// correo personal de Persona: el empleado entra al sistema con el correo del bar,
        /// y si además es cliente su correo particular no se ve afectado.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Hash PBKDF2 de la contraseña. Nunca texto plano.</summary>
        public string Clave { get; set; } = string.Empty;

        // Navegación
        public Persona Persona { get; set; } = null!;
        public Rol Rol { get; set; } = null!;

        public string NombreCompleto => Persona?.NombreCompleto ?? string.Empty;
        public string NombreRol => Rol?.NombreRol ?? string.Empty;
        public bool EsAdministrador => NombreRol == RolesSistema.Administrador;
        public bool EsGerente => NombreRol == RolesSistema.Gerente;

        /// <summary>
        /// Personal de salón: vendedor y mesero son la misma cuenta en la práctica.
        /// Ambos cobran en el punto de venta y ambos pueden figurar como el mesero que
        /// atendió una mesa (RF-02). El rol se conserva porque describe el puesto,
        /// no porque recorte lo que la cuenta puede hacer.
        /// </summary>
        public bool EsPersonalDeAtencion =>
            NombreRol == RolesSistema.Vendedor || NombreRol == RolesSistema.Mesero;

        /// <summary>
        /// Quiénes entran al tablero en lugar del punto de venta (RF-09).
        /// El administrador ve todo; el gerente, solo lo que hace a la gestión del negocio.
        /// </summary>
        public bool AccedeAlTablero => EsAdministrador || EsGerente;
    }
}
