using System;
using System.Collections.Generic;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// Implementación en memoria de IRepositorioBar sobre los datos de prueba.
    /// Contiene la lógica de negocio real (validaciones y descuento de stock) para que,
    /// al migrar a EF Core, solo cambie el origen de los datos y no las reglas.
    /// </summary>
    public partial class RepositorioMemoria : IRepositorioBar
    {
        private readonly DatosPrueba _datos;
        private readonly SesionActual _sesion;
        private int _proximoIdVenta = 1;
        private int _proximoIdDetalle = 1;

        public RepositorioMemoria() : this(DatosPrueba.Crear(), new SesionActual())
        {
        }

        public RepositorioMemoria(SesionActual sesion) : this(DatosPrueba.Crear(), sesion)
        {
        }

        public RepositorioMemoria(DatosPrueba datos, SesionActual sesion)
        {
            _datos = datos;
            _sesion = sesion;
        }

        // ---------------------------------------------------------------
        // Consultas
        // ---------------------------------------------------------------
        public Usuario? Autenticar(string email, string clave)
        {
            var usuario = _datos.Usuarios.FirstOrDefault(u =>
                string.Equals(u.Persona.Email, email, StringComparison.OrdinalIgnoreCase));

            if (usuario is null)
                return null;

            return SeguridadHelper.VerificarClave(clave, usuario.Clave) ? usuario : null;
        }

        public IReadOnlyList<Categoria> ObtenerCategorias() =>
            _datos.Categorias.OrderBy(c => c.NombreCategoria).ToList();

        public IReadOnlyList<Producto> ObtenerProductos() =>
            _datos.Productos.OrderBy(p => p.IdCategoria).ThenBy(p => p.Nombre).ToList();

        public IReadOnlyList<MetodoPago> ObtenerMetodosPago() =>
            _datos.MetodosPago.ToList();

        public IReadOnlyList<Ubicacion> ObtenerUbicaciones() =>
            _datos.Ubicaciones.Where(u => u.Estado != EstadoUbicacion.Reservada).ToList();

        public IReadOnlyList<Cliente> ObtenerClientes() =>
            _datos.Clientes.OrderBy(c => c.IdCliente).ToList();

        public IReadOnlyList<Usuario> ObtenerMeseros() =>
            _datos.Usuarios.Where(u => u.NombreRol == RolesSistema.Mesero).ToList();

        public IReadOnlyList<Venta> ObtenerVentas() =>
            _datos.Ventas.OrderBy(v => v.IdVenta).ToList();

        public int ContarVentasDelDia() =>
            _datos.Ventas.Count(v => v.EstadoVenta == EstadoVenta.Confirmada && v.FechaHora.Date == DateTime.Today);

        /// <summary>
        /// RF-07 en modo consulta: cuántas unidades se pueden despachar hoy.
        /// Con receta, manda el ingrediente más escaso.
        /// </summary>
        public int CalcularDisponibilidad(Producto producto)
        {
            if (!producto.TieneReceta)
                return producto.Stock;

            int disponible = int.MaxValue;
            foreach (var receta in producto.Ingredientes)
            {
                if (receta.CantidadNecesaria <= 0)
                    continue;

                int posibles = (int)Math.Floor(receta.Ingrediente.Stock / receta.CantidadNecesaria);
                disponible = Math.Min(disponible, posibles);
            }

            return disponible == int.MaxValue ? 0 : Math.Max(0, disponible);
        }

        /// <summary>RF-08: insumos y productos de venta directa por debajo del mínimo.</summary>
        public IReadOnlyList<AlertaStock> ObtenerAlertasStock()
        {
            var alertas = _datos.Ingredientes
                .Where(i => i.StockBajo)
                .Select(i => new AlertaStock(i.Nombre, i.Stock, i.StockMinimo, i.UnidadMedida))
                .ToList();

            // Productos de venta directa: cada uno con su propio stock_minimo.
            alertas.AddRange(_datos.Productos
                .Where(p => p.StockBajo)
                .Select(p => new AlertaStock(p.Nombre, p.Stock, p.StockMinimo, "u.")));

            return alertas.OrderBy(a => a.Stock).ToList();
        }

        // ---------------------------------------------------------------
        // Comandos
        // ---------------------------------------------------------------
        /// <summary>
        /// Valida las reglas de negocio, numera la venta, la guarda y descuenta el stock.
        /// Todo o nada: si algún renglón no tiene stock, no se descuenta nada.
        /// </summary>
        public ResultadoOperacion RegistrarVenta(Venta venta)
        {
            var validacion = Validar(venta);
            if (!validacion.Exito)
                return validacion;

            // Verificación previa de stock para no dejar el inventario a medio descontar.
            var faltante = BuscarFaltanteDeStock(venta);
            if (faltante is not null)
                return ResultadoOperacion.Error($"Sin stock suficiente para {faltante}.");

            venta.IdVenta = _proximoIdVenta++;
            venta.FechaHora = DateTime.Now;
            venta.EstadoVenta = EstadoVenta.Confirmada;
            venta.FechaCreacion = DateTime.Now;
            venta.UsuarioModificacion = venta.IdCajero;

            foreach (var detalle in venta.Detalles)
            {
                detalle.IdVentaDetalle = _proximoIdDetalle++;
                detalle.IdVenta = venta.IdVenta;
                detalle.RecalcularSubtotal();
                DescontarStock(detalle);
            }

            _datos.Ventas.Add(venta);
            return ResultadoOperacion.Ok($"Venta #{venta.IdVenta} confirmada por {venta.Total:C0}.");
        }

        private ResultadoOperacion Validar(Venta venta)
        {
            if (!venta.Detalles.Any())
                return ResultadoOperacion.Error("Agregue al menos un producto al ticket.");

            if (venta.IdCajero <= 0)
                return ResultadoOperacion.Error("No se identificó al cajero de la operación.");

            if (venta.IdMetodoPago <= 0)
                return ResultadoOperacion.Error("Seleccione el método de pago.");

            if (venta.IdCliente <= 0)
                return ResultadoOperacion.Error("Seleccione el cliente de la venta.");

            // RF-03: la ubicacion es obligatoria en el local y debe quedar nula para llevar.
            if (venta.ModalidadConsumo == ModalidadConsumo.Local)
            {
                if (venta.IdUbicacion is null)
                    return ResultadoOperacion.Error("Seleccione la mesa o la barra del pedido.");

                var ubicacion = _datos.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == venta.IdUbicacion);
                if (ubicacion is null)
                    return ResultadoOperacion.Error("La ubicación seleccionada no existe.");

                if (ubicacion.RequiereMesero)
                {
                    // RF-02: en mesa hay que registrar quien tomo el pedido.
                    if (venta.IdMesero is null)
                        return ResultadoOperacion.Error("Seleccione el mesero que tomó el pedido.");
                }
                else
                {
                    // En la barra atiende el barman: la venta no lleva mesero.
                    venta.IdMesero = null;
                    venta.Mesero = null;
                }
            }
            else
            {
                venta.IdUbicacion = null;
                venta.Ubicacion = null;
                venta.IdMesero = null;
                venta.Mesero = null;
            }

            return ResultadoOperacion.Ok();
        }

        /// <summary>Devuelve el nombre del primer producto/insumo sin stock, o null si alcanza para todo.</summary>
        private string? BuscarFaltanteDeStock(Venta venta)
        {
            var consumoIngredientes = new Dictionary<int, decimal>();
            var consumoProductos = new Dictionary<int, int>();

            foreach (var detalle in venta.Detalles)
            {
                var producto = _datos.Productos.First(p => p.IdProducto == detalle.IdProducto);

                if (producto.TieneReceta)
                {
                    foreach (var receta in producto.Ingredientes)
                    {
                        consumoIngredientes.TryGetValue(receta.IdIngrediente, out var acumulado);
                        consumoIngredientes[receta.IdIngrediente] = acumulado + receta.CantidadNecesaria * detalle.Cantidad;
                    }
                }
                else
                {
                    consumoProductos.TryGetValue(producto.IdProducto, out var acumulado);
                    consumoProductos[producto.IdProducto] = acumulado + detalle.Cantidad;
                }
            }

            foreach (var (idIngrediente, cantidad) in consumoIngredientes)
            {
                var ingrediente = _datos.Ingredientes.First(i => i.IdIngrediente == idIngrediente);
                if (ingrediente.Stock < cantidad)
                    return ingrediente.Nombre;
            }

            foreach (var (idProducto, cantidad) in consumoProductos)
            {
                var producto = _datos.Productos.First(p => p.IdProducto == idProducto);
                if (producto.Stock < cantidad)
                    return producto.Nombre;
            }

            return null;
        }

        /// <summary>
        /// RF-07: descuento inteligente. Con receta baja los insumos de Ingrediente;
        /// sin receta baja unidades del propio Producto.
        /// </summary>
        private void DescontarStock(VentaDetalle detalle)
        {
            var producto = _datos.Productos.First(p => p.IdProducto == detalle.IdProducto);
            var ahora = DateTime.Now;

            if (producto.TieneReceta)
            {
                foreach (var receta in producto.Ingredientes)
                {
                    receta.Ingrediente.Stock -= receta.CantidadNecesaria * detalle.Cantidad;
                    receta.Ingrediente.FechaModificacion = ahora;
                }
            }
            else
            {
                producto.Stock -= detalle.Cantidad;
                producto.FechaModificacion = ahora;
            }
        }
    }
}
