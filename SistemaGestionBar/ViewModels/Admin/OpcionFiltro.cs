namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Una opción de un combo de filtro: el texto que ve el usuario y el valor que
    /// usa el predicado. Evita tener que traducir enums en la vista con un converter.
    /// </summary>
    public record OpcionFiltro<T>(string Texto, T Valor);

    /// <summary>Con qué se relaciona una persona: cliente del bar, empleado, ambas o ninguna.</summary>
    public enum VinculoPersona
    {
        Todas,
        Clientes,
        Empleados,
        SinVinculo
    }

    /// <summary>Estado de stock, para filtrar inventario y catálogo.</summary>
    public enum EstadoStock
    {
        Todos,
        AReponer,
        Suficiente
    }

    /// <summary>Ventana de tiempo de las ventas.</summary>
    public enum PeriodoVenta
    {
        Todo,
        Hoy,
        UltimaSemana,
        UltimoMes
    }
}
