namespace SistemaGestionBar.ViewModels.Admin
{
    public record OpcionFiltro<T>(string Texto, T Valor);

    public enum VinculoPersona
    {
        Todas,
        Clientes,
        Empleados,
        SinVinculo
    }

    public enum EstadoStock
    {
        Todos,
        AReponer,
        Suficiente
    }

    public enum PeriodoVenta
    {
        Todo,
        Hoy,
        UltimaSemana,
        UltimoMes,
        Personalizado
    }
}
