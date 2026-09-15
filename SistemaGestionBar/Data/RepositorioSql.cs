using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// La implementación de <see cref="IRepositorioBar"/> contra MySQL. Reemplaza al
    /// repositorio en memoria sin que ningún ViewModel se entere: las pantallas siguen
    /// hablando con la interfaz.
    ///
    /// <b>Tres cosas que cambian respecto de la versión en memoria y conviene tener claras:</b>
    ///
    /// 1. <b>Las consultas devuelven copias, no las filas vivas.</b> Todas usan
    ///    <c>AsNoTracking</c> con los <c>Include</c> que la pantalla necesita. Una pantalla
    ///    puede modificar lo que recibe sin que eso llegue solo a la base: para guardar hay
    ///    que llamar al método correspondiente, igual que antes.
    ///
    /// 2. <b>Las comparaciones de texto las hace MySQL.</b> Donde antes había
    ///    <c>OrdinalIgnoreCase</c> ahora hay un <c>==</c> común, porque el schema usa la
    ///    collation <c>utf8mb4_0900_ai_ci</c>: el propio motor compara sin distinguir
    ///    mayúsculas ni acentos. <c>OrdinalIgnoreCase</c> además no se traduce a SQL —EF
    ///    Core lo rechaza o se trae la tabla entera al cliente—, así que no era una opción.
    ///
    /// 3. <b>La auditoría ya no se estampa a mano.</b> La hace
    ///    <see cref="BarDbContext.SaveChanges"/> para todo lo que pase por él.
    /// </summary>
    public partial class RepositorioSql : IRepositorioBar
    {
        private readonly FabricaDeContexto _fabrica;
        private readonly SesionActual _sesion;

        public RepositorioSql(FabricaDeContexto fabrica, SesionActual sesion)
        {
            _fabrica = fabrica;
            _sesion = sesion;
        }

        // ---------------------------------------------------------------
        // Autenticación (RF-09)
        // ---------------------------------------------------------------
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

        /// <summary>
        /// El catálogo completo con todo lo que las pantallas leen de un producto: su
        /// categoría, su receta y los insumos de esa receta. Sin los Include, el catálogo
        /// aparecería sin nombre de categoría y ningún producto "tendría receta".
        /// </summary>
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

        /// <summary>
        /// RF-02: quiénes pueden figurar como mesero. Vendedor y Mesero hacen el mismo
        /// trabajo, así que entran los dos. El filtro va por nombre de rol y no por la
        /// propiedad calculada <c>EsPersonalDeAtencion</c>, que no se traduce a SQL.
        /// </summary>
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

            // Un rango de fechas y no v.FechaHora.Date: comparar contra un rango deja que
            // MySQL use el índice, y .Date obligaría a calcular sobre cada fila.
            return db.Ventas.Count(v => v.EstadoVenta == EstadoVenta.Confirmada &&
                                        v.FechaHora >= hoy && v.FechaHora < manana);
        }

        /// <summary>
        /// Cuántas unidades se pueden vender ahora (RF-07). Trabaja sobre el producto que
        /// ya viene cargado con su receta, así que no toca la base: lo llama el punto de
        /// venta una vez por producto del catálogo.
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
            using var db = _fabrica.Crear();

            var alertas = db.Ingredientes.AsNoTracking()
                            .Where(i => i.Stock <= i.StockMinimo)
                            .Select(i => new AlertaStock(i.Nombre, i.Stock, i.StockMinimo,
                                                         i.UnidadMedida, true, i.IdIngrediente))
                            .ToList();

            // Solo los de venta directa: el que tiene receta no tiene stock propio, su
            // disponibilidad sale de los insumos.
            alertas.AddRange(db.Productos.AsNoTracking()
                               .Where(p => !p.Ingredientes.Any() &&
                                           p.StockMinimo > 0 &&
                                           p.Stock <= p.StockMinimo)
                               .Select(p => new AlertaStock(p.Nombre, p.Stock, p.StockMinimo,
                                                            "u.", false, p.IdProducto))
                               .ToList());

            return alertas.OrderBy(a => a.Stock).ToList();
        }

        // ---------------------------------------------------------------
        // Registrar venta (RF-01 y RF-07)
        // ---------------------------------------------------------------
        /// <summary>
        /// Confirma la venta, descuenta el stock y emite la factura, todo dentro de una
        /// <b>transacción</b>: o queda todo, o no queda nada. En memoria daba igual; contra
        /// una base, una falla a mitad de camino dejaría el inventario descontado sin venta.
        /// </summary>
        public ResultadoOperacion RegistrarVenta(Venta venta)
        {
            using var db = _fabrica.Crear();

            var validacion = Validar(db, venta);
            if (!validacion.Exito)
                return validacion;

            // Los productos del ticket, con su receta, para verificar y descontar stock.
            var idsProducto = venta.Detalles.Select(d => d.IdProducto).Distinct().ToList();

            var productos = db.Productos
                              .Include(p => p.Ingredientes).ThenInclude(pi => pi.Ingrediente)
                              .Where(p => idsProducto.Contains(p.IdProducto))
                              .ToList();

            if (productos.Count != idsProducto.Count)
                return ResultadoOperacion.Error("Hay un producto del ticket que ya no existe.");

            string? faltante = BuscarFaltanteDeStock(venta, productos);
            if (faltante is not null)
                return ResultadoOperacion.Error($"Sin stock suficiente para {faltante}.");

            using var transaccion = db.Database.BeginTransaction();

            try
            {
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

                // La venta se numera sola (auto_increment) y recién ahí se sabe su id,
                // que es lo que necesita el número de factura (F-0001 = IdVenta).
                db.SaveChanges();

                transaccion.Commit();

                // El id generado vuelve al objeto que trajo la pantalla, que es el que
                // sigue usando para mostrar el resultado.
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

        /// <summary>
        /// La factura ya no es una tabla propia: se arma al vuelo con los mismos datos
        /// que trae <see cref="ObtenerVentas"/> (Cliente, Cajero y Producto ya incluidos),
        /// así que no hace falta una consulta aparte.
        /// </summary>
        public IReadOnlyList<Factura> ObtenerFacturas()
        {
            using var db = _fabrica.Crear();

            // El ToList() trae las ventas antes de mapear: ConstruirFactura no es
            // traducible a SQL (arma objetos y concatena texto), así que tiene que
            // correr del lado del cliente, sobre la lista ya materializada.
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

        /// <summary>Solo las ventas confirmadas emiten comprobante (igual que antes).</summary>
        private static IQueryable<Venta> VentasParaFacturar(BarDbContext db) =>
            db.Ventas.AsNoTracking()
              .Include(v => v.Cliente).ThenInclude(c => c.Persona)
              .Include(v => v.Cajero).ThenInclude(u => u.Persona)
              .Include(v => v.Detalles).ThenInclude(d => d.Producto)
              .Where(v => v.EstadoVenta == EstadoVenta.Confirmada);

        /// <summary>
        /// Arma el comprobante en memoria a partir de una venta ya cargada con Cliente,
        /// Cajero y el Producto de cada renglón. Los nombres se copian igual que antes,
        /// pero reflejan el dato ACTUAL de Persona/Producto, no el del día de la venta:
        /// ver la nota en <see cref="Factura"/>.
        /// </summary>
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

        /// <summary>
        /// El número sale del id de la venta y no de un contador propio: así el
        /// comprobante y la venta que documenta comparten el mismo número, y no hace
        /// falta un segundo contador que se pueda desincronizar.
        /// </summary>
        internal static string NumeroDeFactura(int idVenta) => $"F-{idVenta:0000}";

        // ---------------------------------------------------------------
        // Reglas de la venta (RF-02 y RF-03)
        // ---------------------------------------------------------------
        private static ResultadoOperacion Validar(BarDbContext db, Venta venta)
        {
            if (!venta.Detalles.Any())
                return ResultadoOperacion.Error("Agregue al menos un producto al ticket.");

            if (venta.IdCajero <= 0)
                return ResultadoOperacion.Error("No se identificó al cajero de la operación.");

            if (venta.IdMetodoPago <= 0)
                return ResultadoOperacion.Error("Seleccione el método de pago.");

            if (venta.IdCliente <= 0)
                return ResultadoOperacion.Error("Seleccione el cliente de la venta.");

            // RF-03: la ubicación es obligatoria en el local y queda nula para llevar.
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
                    // RF-02: en mesa hay que registrar quién tomó el pedido.
                    if (venta.IdMesero is null)
                        return ResultadoOperacion.Error("Seleccione el mesero que tomó el pedido.");
                }
                else
                {
                    // En la barra atiende el barman: la venta va sin mesero.
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

        /// <summary>
        /// Verifica ANTES de tocar nada que alcance para todos los renglones. Es lo que
        /// hace que el descuento sea todo o nada y no quede el inventario a medias.
        /// </summary>
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

        /// <summary>
        /// RF-07: con receta se descuentan los insumos; sin receta, unidades del producto.
        /// Los objetos vienen del mismo contexto que va a guardar, así que alcanza con
        /// modificarlos: el SaveChanges de afuera los escribe.
        /// </summary>
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

        /// <summary>
        /// El mensaje útil de una excepción de base de datos está en la causa, no en el
        /// envoltorio de EF Core. Mostrar "An error occurred while saving" no ayuda a nadie.
        /// </summary>
        private static string MensajeDe(Exception ex) =>
            (ex.InnerException ?? ex).Message;
    }
}
