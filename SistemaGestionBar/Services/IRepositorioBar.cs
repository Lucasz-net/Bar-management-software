using System.Collections.Generic;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.Services
{
    /// <summary>
    /// Contrato de acceso a datos. Hoy lo implementa RepositorioMemoria (datos de prueba);
    /// mañana lo implementará un RepositorioSqlServer sobre el DbContext de EF Core
    /// sin tocar una sola línea de los ViewModels.
    ///
    /// Analogía web: es el "cliente de API". Los ViewModels son los componentes que lo
    /// consumen y no saben si detrás hay un mock o el backend real.
    /// </summary>
    public interface IRepositorioBar
    {
        /// <summary>RF-09. Devuelve null si el email no existe o la clave no coincide.</summary>
        Usuario? Autenticar(string email, string clave);

        IReadOnlyList<Categoria> ObtenerCategorias();
        IReadOnlyList<Producto> ObtenerProductos();
        IReadOnlyList<MetodoPago> ObtenerMetodosPago();
        IReadOnlyList<Ubicacion> ObtenerUbicaciones();
        IReadOnlyList<Cliente> ObtenerClientes();

        /// <summary>RF-02. Usuarios con rol Mesero, para asignar quién tomó el pedido.</summary>
        IReadOnlyList<Usuario> ObtenerMeseros();

        /// <summary>
        /// Cuántas unidades del producto se pueden vender ahora mismo.
        /// Con receta: el mínimo que permitan sus ingredientes. Sin receta: su propio stock (RF-07).
        /// </summary>
        int CalcularDisponibilidad(Producto producto);

        /// <summary>RF-08. Insumos y productos que llegaron o bajaron de su stock mínimo.</summary>
        IReadOnlyList<AlertaStock> ObtenerAlertasStock();

        /// <summary>
        /// RF-01 y RF-07. Confirma la venta, la numera, la persiste y descuenta el stock.
        /// Devuelve el resultado con el detalle del error si la validación falla.
        /// </summary>
        ResultadoOperacion RegistrarVenta(Venta venta);

        // ---------------------------------------------------------------
        // Facturación
        // ---------------------------------------------------------------
        IReadOnlyList<Factura> ObtenerFacturas();
        IReadOnlyList<Factura> ObtenerFacturasPorVenta(int idVenta);
        ResultadoOperacion RegistrarFactura(Factura factura);

        /// <summary>Cantidad de ventas confirmadas del día, para el indicador del encabezado.</summary>
        int ContarVentasDelDia();

        /// <summary>Ventas registradas, de la más vieja a la más nueva.</summary>
        IReadOnlyList<Venta> ObtenerVentas();

        // ---------------------------------------------------------------
        // Consultas del módulo de administración
        // ---------------------------------------------------------------
        IReadOnlyList<Ingrediente> ObtenerIngredientes();
        IReadOnlyList<Usuario> ObtenerUsuarios();
        IReadOnlyList<Rol> ObtenerRoles();
        IReadOnlyList<Reporte> ObtenerReportes();

        /// <summary>Todas las ubicaciones, incluidas las reservadas (el POS filtra, el ABM no).</summary>
        IReadOnlyList<Ubicacion> ObtenerTodasLasUbicaciones();

        // ---------------------------------------------------------------
        // ABM (RF-12). Cada método valida antes de tocar los datos (RF-13)
        // y estampa los campos de auditoría (RF-11).
        // ---------------------------------------------------------------
        ResultadoOperacion GuardarProducto(Producto producto);
        ResultadoOperacion EliminarProducto(int idProducto);

        /// <summary>
        /// Guarda la receta completa: el procedimiento (tabla Receta) y la composición
        /// (Producto_Ingrediente). Una lista vacía deja al producto sin receta.
        /// </summary>
        ResultadoOperacion GuardarReceta(int idProducto, string? instrucciones, IEnumerable<ProductoIngrediente> composicion);

        ResultadoOperacion GuardarIngrediente(Ingrediente ingrediente);
        ResultadoOperacion EliminarIngrediente(int idIngrediente);

        /// <summary>Alta o edición de un empleado: escribe Persona y Usuario juntos (RF-10).</summary>
        ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva);
        ResultadoOperacion EliminarUsuario(int idUsuario);

        ResultadoOperacion GuardarCategoria(Categoria categoria);
        ResultadoOperacion EliminarCategoria(int idCategoria);

        ResultadoOperacion GuardarMetodoPago(MetodoPago metodoPago);
        ResultadoOperacion EliminarMetodoPago(int idMetodoPago);

        ResultadoOperacion GuardarUbicacion(Ubicacion ubicacion);
        ResultadoOperacion EliminarUbicacion(int idUbicacion);

        /// <summary>RF-14: deja constancia del informe generado.</summary>
        ResultadoOperacion RegistrarReporte(Reporte reporte);
    }

    /// <summary>Resultado de una operación de negocio: éxito o mensaje de error para la UI.</summary>
    public record ResultadoOperacion(bool Exito, string Mensaje)
    {
        public static ResultadoOperacion Ok(string mensaje = "") => new(true, mensaje);
        public static ResultadoOperacion Error(string mensaje) => new(false, mensaje);
    }

    /// <summary>Fila de la alerta de inventario (RF-08).</summary>
    public record AlertaStock(string Nombre, decimal Stock, decimal StockMinimo, string UnidadMedida)
    {
        public string Descripcion => $"{Nombre}: quedan {Stock:0.##} {UnidadMedida} (mínimo {StockMinimo:0.##})";
    }
}
