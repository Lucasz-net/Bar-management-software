namespace SistemaGestionBar.Models
{
    public class Usuario : EntidadAuditable
    {
        public int IdUsuario { get; set; }
        public int IdPersona { get; set; }
        public int IdRol { get; set; }

        public string Email { get; set; } = string.Empty;

        /// <summary>Hash PBKDF2, nunca texto plano.</summary>
        public string Clave { get; set; } = string.Empty;

        public Persona Persona { get; set; } = null!;
        public Rol Rol { get; set; } = null!;

        public string NombreCompleto => Persona?.NombreCompleto ?? string.Empty;
        public string NombreRol => Rol?.NombreRol ?? string.Empty;
        public bool EsAdministrador => NombreRol == RolesSistema.Administrador;
        public bool EsGerente => NombreRol == RolesSistema.Gerente;

        /// <summary>Vendedor y mesero tienen los mismos permisos; el rol solo describe el puesto.</summary>
        public bool EsPersonalDeAtencion =>
            NombreRol == RolesSistema.Vendedor || NombreRol == RolesSistema.Mesero;

        public bool AccedeAlTablero => EsAdministrador || EsGerente;
    }
}
