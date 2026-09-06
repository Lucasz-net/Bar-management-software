namespace SistemaGestionBar.Models
{
    /// <summary>Tabla Metodo_Pago. Tabla paramétrica que alimenta el combo de cobro (RF-06).</summary>
    public class MetodoPago : EntidadAuditable
    {
        public int IdMetodoPago { get; set; }
        public string NombreMetodo { get; set; } = string.Empty;
    }
}
