namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Persona. Centraliza los datos personales de empleados y clientes (RF-10),
    /// evitando duplicar nombre/DNI/teléfono/email en cada tabla que los necesite.
    /// </summary>
    public class Persona : EntidadAuditable
    {
        public int IdPersona { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? DniCuit { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }

        // Navegación
        public Usuario? Usuario { get; set; }
        public Cliente? Cliente { get; set; }
    }
}
