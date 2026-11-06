using System.Collections.Generic;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.Services
{
    public interface IRepositorioBar
    {
        Usuario? Autenticar(string email, string clave);

        ResultadoOperacion AutorizarSupervisor(string email, string clave);

        ResultadoOperacion ConfirmarContrasenaPropia(string clave);

        /// <summary>Valida cuenta y contraseña, y que la cuenta tenga exactamente el rol pedido.</summary>
        ResultadoOperacion AutorizarRol(string email, string clave, string rol);

        IReadOnlyList<Categoria> ObtenerCategorias();
        IReadOnlyList<Producto> ObtenerProductos();
        IReadOnlyList<MetodoPago> ObtenerMetodosPago();
        IReadOnlyList<Ubicacion> ObtenerUbicaciones();
        IReadOnlyList<Cliente> ObtenerClientes();

        IReadOnlyList<Usuario> ObtenerPersonalDeAtencion();

        int CalcularDisponibilidad(Producto producto);

        IReadOnlyList<AlertaStock> ObtenerAlertasStock();

        ResultadoOperacion RegistrarVenta(Venta venta);

        IReadOnlyList<Factura> ObtenerFacturas();

        Factura? ObtenerFacturaDeVenta(int idVenta);

        int ContarVentasDelDia();

        IReadOnlyList<Venta> ObtenerVentas();

        IReadOnlyList<Ingrediente> ObtenerIngredientes();
        IReadOnlyList<Persona> ObtenerPersonas();

        Persona? BuscarPersonaPorDocumento(string? dniCuit);

        IReadOnlyList<Usuario> ObtenerUsuarios();
        IReadOnlyList<Rol> ObtenerRoles();


        ResultadoOperacion GuardarProducto(Producto producto);
        ResultadoOperacion EliminarProducto(int idProducto);

        ResultadoOperacion GuardarReceta(int idProducto, string? instrucciones, IEnumerable<ProductoIngrediente> composicion);

        ResultadoOperacion GuardarIngrediente(Ingrediente ingrediente);
        ResultadoOperacion EliminarIngrediente(int idIngrediente);

        ResultadoOperacion AjustarStock(bool esInsumo, int id, decimal cantidad);

        ResultadoOperacion GuardarPersona(Persona persona, bool esCliente);
        ResultadoOperacion EliminarPersona(int idPersona);

        ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva);
        ResultadoOperacion EliminarUsuario(int idUsuario);

        ResultadoOperacion GuardarCategoria(Categoria categoria);
        ResultadoOperacion EliminarCategoria(int idCategoria);

        ResultadoOperacion GuardarMetodoPago(MetodoPago metodoPago);
        ResultadoOperacion EliminarMetodoPago(int idMetodoPago);

        ResultadoOperacion GuardarUbicacion(Ubicacion ubicacion);
        ResultadoOperacion EliminarUbicacion(int idUbicacion);
    }

    public record ResultadoOperacion(bool Exito, string Mensaje)
    {
        public static ResultadoOperacion Ok(string mensaje = "") => new(true, mensaje);
        public static ResultadoOperacion Error(string mensaje) => new(false, mensaje);
    }

    public record AlertaStock(
        string Nombre,
        decimal Stock,
        decimal StockMinimo,
        string UnidadMedida,
        bool EsInsumo,
        int Id)
    {
        public string Descripcion => $"{Nombre}: quedan {Stock:0.##} {UnidadMedida} (mínimo {StockMinimo:0.##})";

        public string DondeSeRepone => EsInsumo ? "Inventario" : "Productos";
    }
}