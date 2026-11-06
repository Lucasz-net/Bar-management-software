namespace SistemaGestionBar.Models
{
    public class Ubicacion : EntidadAuditable
    {
        public int IdUbicacion { get; set; }
        public string NombreUbicacion { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public TipoUbicacion Tipo { get; set; } = TipoUbicacion.Mesa;

        public bool RequiereMesero => Tipo == TipoUbicacion.Mesa;

        public string Descripcion => $"{NombreUbicacion} · {Capacidad} pers.";
    }
}
