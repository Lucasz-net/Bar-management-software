using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    public partial class RepositorioSql : IRepositorioBar
    {
        private readonly FabricaDeContexto _fabrica;
        private readonly SesionActual _sesion;

        public RepositorioSql(FabricaDeContexto fabrica, SesionActual sesion)
        {
            _fabrica = fabrica;
            _sesion = sesion;
        }

        public Usuario? Autenticar(string email, string clave)
        {
            string correo = (email ?? string.Empty).Trim();
            if (correo.Length == 0)
                return null;

            using var db = _fabrica.Crear();

            var usuario = db.Usuarios
                            .AsNoTracking()
                            .Include(u => u.Persona)
                            .Include(u => u.Rol)
                            .FirstOrDefault(u => u.Email == correo);

            if (usuario is null)
                return null;

            return SeguridadHelper.VerificarClave(clave, usuario.Clave) ? usuario : null;
        }

        public ResultadoOperacion AutorizarSupervisor(string email, string clave)
        {
            var usuario = Autenticar(email, clave);

            if (usuario is null)
                return ResultadoOperacion.Error("Credenciales incorrectas.");

            if (usuario.Rol.NombreRol != RolesSistema.Administrador &&
                usuario.Rol.NombreRol != RolesSistema.Gerente)
            {
                return ResultadoOperacion.Error($"La cuenta de {usuario.Persona.NombreCompleto} no tiene permisos de supervisor.");
            }

            return ResultadoOperacion.Ok($"Autorizado por {usuario.Persona.NombreCompleto}.");
        }

        public ResultadoOperacion AutorizarRol(string email, string clave, string rol)
        {
            var usuario = Autenticar(email, clave);

            if (usuario is null)
                return ResultadoOperacion.Error("Credenciales incorrectas.");

            if (usuario.Rol.NombreRol != rol)
                return ResultadoOperacion.Error($"La cuenta de {usuario.Persona.NombreCompleto} no es de {rol.ToLowerInvariant()}.");

            return ResultadoOperacion.Ok($"Autorizado por {usuario.Persona.NombreCompleto}.");
        }

        public ResultadoOperacion ConfirmarContrasenaPropia(string clave)
        {
            if (_sesion.Usuario is null)
                return ResultadoOperacion.Error("No hay una sesión activa.");

            var usuario = Autenticar(_sesion.Usuario.Email, clave);

            return usuario is null
                ? ResultadoOperacion.Error("Contraseña incorrecta.")
                : ResultadoOperacion.Ok();
        }

        // ---------------------------------------------------------------
        // Consultas del punto de venta
        // ---------------------------------------------------------------
        public IReadOnlyList<Categoria> ObtenerCategorias()
        {
            using var db = _fabrica.Crear();
            return db.Categorias.AsNoTracking()
                     .OrderBy(c => c.NombreCategoria)
                     .ToList();
        }

        public IReadOnlyList<Producto> ObtenerProductos()
        {
            using var db = _fabrica.Crear();
            return db.Productos.AsNoTracking()
                     .Include(p => p.Categoria)
                     .Include(p => p.Receta)
                     .Include(p => p.Ingredientes)
                        .ThenInclude(pi => pi.Ingrediente)
                     .OrderBy(p => p.IdCategoria)
                     .ThenBy(p => p.Nombre)
                     .ToList();
        }

        public IReadOnlyList<MetodoPago> ObtenerMetodosPago()
        {
            using var db = _fabrica.Crear();
            return db.MetodosPago.AsNoTracking().OrderBy(m => m.IdMetodoPago).ToList();
        }

        public IReadOnlyList<Ubicacion> ObtenerUbicaciones()
        {
            using var db = _fabrica.Crear();
            return db.Ubicaciones.AsNoTracking()
                     .OrderBy(u => u.Tipo)
                     .ThenBy(u => u.NombreUbicacion)
                     .ToList();
        }

        public IReadOnlyList<Cliente> ObtenerClientes()
        {
            using var db = _fabrica.Crear();
            return db.Clientes.AsNoTracking()
                     .Include(c => c.Persona)
                     .OrderBy(c => c.IdCliente)
                     .ToList();
        }

        public IReadOnlyList<Usuario> ObtenerPersonalDeAtencion()
        {
            using var db = _fabrica.Crear();
            return db.Usuarios.AsNoTracking()
                     .Include(u => u.Persona)
                     .Include(u => u.Rol)
                     .Where(u => u.Rol.NombreRol == RolesSistema.Vendedor ||
                                 u.Rol.NombreRol == RolesSistema.Mesero)
                     .OrderBy(u => u.Persona.Apellido)
                     .ThenBy(u => u.Persona.Nombre)
                     .ToList();
        }

        public IReadOnlyList<Venta> ObtenerVentas()
        {
            using var db = _fabrica.Crear();
            return db.Ventas.AsNoTracking()
                     .Include(v => v.MetodoPago)
                     .Include(v => v.Cliente).ThenInclude(c => c.Persona)
                     .Include(v => v.Cajero).ThenInclude(u => u.Persona)
                     .Include(v => v.Mesero).ThenInclude(u => u!.Persona)
                     .Include(v => v.Ubicacion)
                     .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                     .OrderBy(v => v.IdVenta)
                     .ToList();
        }

        public int ContarVentasDelDia()
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            using var db = _fabrica.Crear();

            return db.Ventas.Count(v => v.EstadoVenta == EstadoVenta.Confirmada &&
                                        v.FechaHora >= hoy && v.FechaHora < manana);
        }

        // // Sin receta es el stock propio; con receta, cuántas unidades alcanzan con el insumo más escaso.
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

        public IReadOnlyList<AlertaStock> ObtenerAlertasStock()
        {
            using var db = _fabrica.Crear();

            var alertas = db.Ingredientes.AsNoTracking()
                            .Where(i => i.Stock <= i.StockMinimo)
                            .Select(i => new AlertaStock(i.Nombre, i.Stock, i.StockMinimo,
                                                         i.UnidadMedida, true, i.IdIngrediente))
                            .ToList();

            alertas.AddRange(db.Productos.AsNoTracking()
                               .Where(p => !p.Ingredientes.Any() &&
                                           p.StockMinimo > 0 &&
                                           p.Stock <= p.StockMinimo)
                               .Select(p => new AlertaStock(p.Nombre, p.Stock, p.StockMinimo,
                                                            "u.", false, p.IdProducto))
                               .ToList());

            return alertas.OrderBy(a => a.Stock).ToList();
        }

        public ResultadoOperacion RegistrarVenta(Venta venta)
        {
            using var db = _fabrica.Crear();

            var validacion = Validar(db, venta);
            if (!validacion.Exito)
                return validacion;

            var idsProducto = venta.Detalles.Select(d => d.IdProducto).Distinct().ToList();

            // // El descuento de stock y la venta se guardan juntos o no se guarda nada.
            using var transaccion = db.Database.BeginTransaction();

            try
            {
                // // Se bloquean los productos e insumos ANTES de leer el stock: si otra caja vende lo mismo
                // // a la vez, espera su turno y ve el stock ya descontado en vez de dejarlo en negativo.
                BloquearStock(db, idsProducto);

                var productos = db.Productos
                                  .Include(p => p.Ingredientes).ThenInclude(pi => pi.Ingrediente)
                                  .Where(p => idsProducto.Contains(p.IdProducto))
                                  .ToList();

                if (productos.Count != idsProducto.Count)
                    return ResultadoOperacion.Error("Hay un producto del ticket que ya no existe.");

                string? faltante = BuscarFaltanteDeStock(venta, productos);
                if (faltante is not null)
                    return ResultadoOperacion.Error($"Sin stock suficiente para {faltante}.");

                var nueva = new Venta
                {
                    FechaHora = DateTime.Now,
                    EstadoVenta = EstadoVenta.Confirmada,
                    IdMetodoPago = venta.IdMetodoPago,
                    IdCliente = venta.IdCliente,
                    IdCajero = venta.IdCajero,
                    IdMesero = venta.IdMesero,
                    IdUbicacion = venta.IdUbicacion,
                    ModalidadConsumo = venta.ModalidadConsumo
                };

                foreach (var detalle in venta.Detalles)
                {
                    nueva.Detalles.Add(new VentaDetalle
                    {
                        IdProducto = detalle.IdProducto,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario,
                        Subtotal = detalle.Cantidad * detalle.PrecioUnitario
                    });
                }

                db.Ventas.Add(nueva);
                DescontarStock(nueva, productos);

                db.SaveChanges();

                transaccion.Commit();

                venta.IdVenta = nueva.IdVenta;
                venta.FechaHora = nueva.FechaHora;
                venta.EstadoVenta = nueva.EstadoVenta;

                return ResultadoOperacion.Ok(
                    $"Venta #{nueva.IdVenta} confirmada por {nueva.Total:C0}. Factura {NumeroDeFactura(nueva.IdVenta)}.");
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                return ResultadoOperacion.Error("No se pudo registrar la venta: " + MensajeDe(ex));
            }
        }

        public IReadOnlyList<Factura> ObtenerFacturas()
        {
            using var db = _fabrica.Crear();

            return VentasParaFacturar(db)
                     .OrderByDescending(v => v.IdVenta)
                     .ToList()
                     .Select(ConstruirFactura)
                     .ToList();
        }

        public Factura? ObtenerFacturaDeVenta(int idVenta)
        {
            using var db = _fabrica.Crear();
            var venta = VentasParaFacturar(db).FirstOrDefault(v => v.IdVenta == idVenta);
            return venta is null ? null : ConstruirFactura(venta);
        }

        private static IQueryable<Venta> VentasParaFacturar(BarDbContext db) =>
            db.Ventas.AsNoTracking()
              .Include(v => v.Cliente).ThenInclude(c => c.Persona)
              .Include(v => v.Cajero).ThenInclude(u => u.Persona)
              .Include(v => v.Mesero).ThenInclude(u => u!.Persona)
              .Include(v => v.MetodoPago)
              .Include(v => v.Ubicacion)
              .Include(v => v.Detalles).ThenInclude(d => d.Producto)
              .Where(v => v.EstadoVenta == EstadoVenta.Confirmada);

        private static Factura ConstruirFactura(Venta venta)
        {
            var factura = new Factura
            {
                IdVenta = venta.IdVenta,
                Numero = NumeroDeFactura(venta.IdVenta),
                FechaEmision = venta.FechaHora,
                Importe = venta.Total,
                ClienteNombre = venta.Cliente?.NombreMostrado,
                CajeroNombre = venta.Cajero?.NombreCompleto,
                MetodoPagoNombre = venta.MetodoPago?.NombreMetodo,
                Modalidad = venta.ModalidadConsumo,
                UbicacionNombre = venta.Ubicacion?.NombreUbicacion,
                MeseroNombre = venta.Mesero?.NombreCompleto,
                Venta = venta
            };

            foreach (var detalle in venta.Detalles)
            {
                factura.Lineas.Add(new FacturaLinea
                {
                    NombreProducto = detalle.Producto?.Nombre ?? string.Empty,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario
                });
            }

            return factura;
        }

        internal static string NumeroDeFactura(int idVenta) => $"F-{idVenta:0000}";

        // // Normaliza el pedido según la modalidad: la barra y "para llevar" no llevan mesero, y "para llevar" tampoco ubicación.
        private static ResultadoOperacion Validar(BarDbContext db, Venta venta)
        {
            if (!venta.Detalles.Any())
                return ResultadoOperacion.Error("Agregue al menos un producto al ticket.");

            if (venta.Detalles.Any(d => d.Cantidad <= 0))
                return ResultadoOperacion.Error("Todas las cantidades del ticket deben ser mayores a cero.");

            if (venta.Detalles.Any(d => d.PrecioUnitario < 0))
                return ResultadoOperacion.Error("Un producto del ticket tiene un precio inválido.");

            if (venta.IdCajero <= 0)
                return ResultadoOperacion.Error("No se identificó al cajero de la operación.");

            if (venta.IdMetodoPago <= 0)
                return ResultadoOperacion.Error("Seleccione el método de pago.");

            if (venta.IdCliente <= 0)
                return ResultadoOperacion.Error("Seleccione el cliente de la venta.");

            if (venta.ModalidadConsumo == ModalidadConsumo.Local)
            {
                if (venta.IdUbicacion is null)
                    return ResultadoOperacion.Error("Seleccione la mesa o la barra del pedido.");

                var ubicacion = db.Ubicaciones.AsNoTracking()
                                  .FirstOrDefault(u => u.IdUbicacion == venta.IdUbicacion);

                if (ubicacion is null)
                    return ResultadoOperacion.Error("La ubicación seleccionada no existe.");

                if (ubicacion.RequiereMesero)
                {
                    if (venta.IdMesero is null)
                        return ResultadoOperacion.Error("Seleccione el mesero que tomó el pedido.");
                }
                else
                {
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

        private static string? BuscarFaltanteDeStock(Venta venta, List<Producto> productos)
        {
            var consumoInsumos = new Dictionary<int, decimal>();
            var consumoProductos = new Dictionary<int, int>();

            foreach (var detalle in venta.Detalles)
            {
                var producto = productos.First(p => p.IdProducto == detalle.IdProducto);

                if (producto.TieneReceta)
                {
                    foreach (var linea in producto.Ingredientes)
                    {
                        consumoInsumos.TryGetValue(linea.IdIngrediente, out var acumulado);
                        consumoInsumos[linea.IdIngrediente] = acumulado + linea.CantidadNecesaria * detalle.Cantidad;
                    }
                }
                else
                {
                    consumoProductos.TryGetValue(producto.IdProducto, out var acumulado);
                    consumoProductos[producto.IdProducto] = acumulado + detalle.Cantidad;
                }
            }

            foreach (var (idIngrediente, cantidad) in consumoInsumos)
            {
                var ingrediente = productos.SelectMany(p => p.Ingredientes)
                                           .First(pi => pi.IdIngrediente == idIngrediente)
                                           .Ingrediente;

                if (ingrediente.Stock < cantidad)
                    return ingrediente.Nombre;
            }

            foreach (var (idProducto, cantidad) in consumoProductos)
            {
                var producto = productos.First(p => p.IdProducto == idProducto);
                if (producto.Stock < cantidad)
                    return producto.Nombre;
            }

            return null;
        }

        private static void DescontarStock(Venta venta, List<Producto> productos)
        {
            foreach (var detalle in venta.Detalles)
            {
                var producto = productos.First(p => p.IdProducto == detalle.IdProducto);

                if (producto.TieneReceta)
                {
                    foreach (var linea in producto.Ingredientes)
                        linea.Ingrediente.Stock -= linea.CantidadNecesaria * detalle.Cantidad;
                }
                else
                {
                    producto.Stock -= detalle.Cantidad;
                }
            }
        }

        private static string MensajeDe(Exception ex) =>
            (ex.InnerException ?? ex).Message;
    }
}