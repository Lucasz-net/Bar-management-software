namespace SistemaGestionBar.Models
{
    /// <summary>RF-03: define si la venta ocupa una Ubicacion o la deja en null.</summary>
    public enum ModalidadConsumo
    {
        Local,
        ParaLlevar
    }

    public enum EstadoVenta
    {
        Pendiente,
        Confirmada,
        Anulada
    }

    public enum EstadoUbicacion
    {
        Libre,
        Ocupada,
        Reservada
    }

    /// <summary>
    /// EXTENSIÓN AL DER: distingue mesa de barra. Hace falta porque en la barra
    /// atiende el barman y la venta NO lleva mesero, mientras que en mesa sí.
    /// Deducirlo del texto de nombre_ubicacion seria fragil, asi que es un campo propio.
    /// Propuesta: columna nueva tipo_ubicacion en la tabla Ubicacion.
    /// </summary>
    public enum TipoUbicacion
    {
        Mesa,
        Barra
    }

    public enum TipoReporte
    {
        VentasPorPeriodo,
        ProductosMasVendidos,
        StockCritico,
        CierreDeCaja
    }
}
