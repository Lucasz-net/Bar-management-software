namespace SistemaGestionBar.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string? Rol { get; set; }
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Clave { get; set; }
    }
}
