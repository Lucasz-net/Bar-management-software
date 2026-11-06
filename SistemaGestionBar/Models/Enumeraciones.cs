namespace SistemaGestionBar.Models
{
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

    /// <summary>En la barra atiende el barman y la venta no lleva mesero.</summary>
    public enum TipoUbicacion
    {
        Mesa,
        Barra
    }
}
