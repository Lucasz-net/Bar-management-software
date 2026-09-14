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
        /// <summary>
        /// RF-09. El usuario entra con su CORREO DE TRABAJO (Usuario.Email), no con el
        /// correo personal de Persona. Devuelve null si no existe o la clave no coincide.
        /// </summary>
        Usuario? Autenticar(string email, string clave);

        IReadOnlyList<Categoria> ObtenerCategorias();
        IReadOnlyList<Producto> ObtenerProductos();
        IReadOnlyList<MetodoPago> ObtenerMetodosPago();
        IReadOnlyList<Ubicacion> ObtenerUbicaciones();
        IReadOnlyList<Cliente> ObtenerClientes();

        /// <summary>
        /// RF-02. Personal de salón habilitado para figurar como mesero de una venta.
        /// Incluye vendedores y meseros: las dos cuentas hacen el mismo trabajo.
        /// </summary>
        IReadOnlyList<Usuario> ObtenerPersonalDeAtencion();

        /// <summary>
        /// Cuántas unidades del producto se pueden vender ahora mismo.
        /// Con receta: el mínimo que permitan sus ingredientes. Sin receta: su propio stock (RF-07).
        /// </summary>
        int CalcularDisponibilidad(Producto producto);

        /// <summary>RF-08. Insumos y productos que llegaron o bajaron de su stock mínimo.</summary>
        IReadOnlyList<AlertaStock> ObtenerAlertasStock();

        /// <summary>
        /// RF-01 y RF-07. Confirma la venta, la numera, la persiste, descuenta el stock
        /// y emite su factura. Devuelve el detalle del error si la validación falla.
        /// </summary>
        ResultadoOperacion RegistrarVenta(Venta venta);

        // ---------------------------------------------------------------
        // Facturación
        // ---------------------------------------------------------------
        IReadOnlyList<Factura> ObtenerFacturas();

        /// <summary>Factura de una venta, o null si esa venta no llegó a emitir una.</summary>
        Factura? ObtenerFacturaDeVenta(int idVenta);

        /// <summary>Cantidad de ventas confirmadas del día, para el indicador del encabezado.</summary>
        int ContarVentasDelDia();

        /// <summary>Ventas registradas, de la más vieja a la más nueva.</summary>
        IReadOnlyList<Venta> ObtenerVentas();

        // ---------------------------------------------------------------
        // Consultas del módulo de administración
        // ---------------------------------------------------------------
        IReadOnlyList<Ingrediente> ObtenerIngredientes();
        IReadOnlyList<Persona> ObtenerPersonas();

        /// <summary>
        /// Busca una persona por su DNI/CUIT, que es su dato identificatorio: es lo que
        /// permite dar de alta un empleado sin volver a cargar los datos de alguien que
        /// ya está en el padrón. Devuelve null si no existe.
        /// </summary>
        Persona? BuscarPersonaPorDocumento(string? dniCuit);

        IReadOnlyList<Usuario> ObtenerUsuarios();
        IReadOnlyList<Rol> ObtenerRoles();

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

        /// <summary>
        /// Alta o edición del padrón de personas (RF-10). <paramref name="esCliente"/> crea
        /// o quita la fila de Cliente: es lo que habilita a la persona en el punto de venta.
        /// </summary>
        ResultadoOperacion GuardarPersona(Persona persona, bool esCliente);
        ResultadoOperacion EliminarPersona(int idPersona);

        /// <summary>
        /// Alta o edición de la CUENTA de un empleado sobre una persona que ya existe.
        /// No escribe datos personales: esos se editan en el padrón de personas.
        /// </summary>
        ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva);
        ResultadoOperacion EliminarUsuario(int idUsuario);

        ResultadoOperacion GuardarCategoria(Categoria categoria);
        ResultadoOperacion EliminarCategoria(int idCategoria);

        ResultadoOperacion GuardarMetodoPago(MetodoPago metodoPago);
        ResultadoOperacion EliminarMetodoPago(int idMetodoPago);

        ResultadoOperacion GuardarUbicacion(Ubicacion ubicacion);
        ResultadoOperacion EliminarUbicacion(int idUbicacion);
    }

    /// <summary>Resultado de una operación de negocio: éxito o mensaje de error para la UI.</summary>
    public record ResultadoOperacion(bool Exito, string Mensaje)
    {
        public static ResultadoOperacion Ok(string mensaje = "") => new(true, mensaje);
        public static ResultadoOperacion Error(string mensaje) => new(false, mensaje);
    }

    /// <summary>
    /// Fila de la alerta de inventario (RF-08).
    ///
    /// Lleva de dónde salió —qué tabla y qué fila— porque la alerta no es solo un aviso:
    /// desde el resumen se puede ir a reponer ese ítem, y para eso hay que poder
    /// encontrarlo. <paramref name="EsInsumo"/> distingue un Ingrediente (se repone en
    /// Inventario) de un producto de venta directa (se repone en Productos).
    /// </summary>
    public record AlertaStock(
        string Nombre,
        decimal Stock,
        decimal StockMinimo,
        string UnidadMedida,
        bool EsInsumo,
        int Id)
    {
        public string Descripcion => $"{Nombre}: quedan {Stock:0.##} {UnidadMedida} (mínimo {StockMinimo:0.##})";

        /// <summary>Dónde se repone: el rótulo del botón lo usa para no mentirle al usuario.</summary>
        public string DondeSeRepone => EsInsumo ? "Inventario" : "Productos";
    }
}
