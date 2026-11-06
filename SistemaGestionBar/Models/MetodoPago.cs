namespace SistemaGestionBar.Models
{
    public class MetodoPago : EntidadAuditable
    {
        public int IdMetodoPago { get; set; }
        public string NombreMetodo { get; set; } = string.Empty;
    }
}
