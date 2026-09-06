namespace SistemaGestionBar.Models
{
    /// <summary>Tabla Ubicacion. Mesas y barra del local (RF-03).</summary>
    public class Ubicacion : EntidadAuditable
    {
        public int IdUbicacion { get; set; }
        public string NombreUbicacion { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public EstadoUbicacion Estado { get; set; } = EstadoUbicacion.Libre;

        /// <summary>EXTENSIÓN AL DER: ver comentario en <see cref="TipoUbicacion"/>.</summary>
        public TipoUbicacion Tipo { get; set; } = TipoUbicacion.Mesa;

        /// <summary>
        /// En la barra atiende el barman y la venta no lleva mesero;
        /// en mesa, en cambio, hay que registrar quien tomo el pedido (RF-02).
        /// </summary>
        public bool RequiereMesero => Tipo == TipoUbicacion.Mesa;

        /// <summary>Texto del combo de ubicaciones. Ej: "Mesa 1 · 4 pers.".</summary>
        public string Descripcion => $"{NombreUbicacion} · {Capacidad} pers.";
    }
}
