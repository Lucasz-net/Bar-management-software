using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// El ABM del tablero (RF-12), con sus validaciones (RF-13).
    ///
    /// <b>El patrón que se repite en todos los Guardar.</b> La pantalla manda un objeto
    /// suelto —lo editó en sus propios campos y no está atado a ningún contexto—. Acá se
    /// busca la fila real en la base, se le copian los datos y se guarda. No se intenta
    /// "pegar" el objeto de la pantalla al contexto: si lo hiciéramos, cualquier propiedad
    /// que la pantalla no haya cargado (una navegación vacía, por ejemplo) se escribiría
    /// como un borrado.
    ///
    /// Al final se le devuelve el id generado al objeto de la pantalla, porque los
    /// ViewModels lo usan para volver a seleccionar lo que acaban de crear.
    /// </summary>
    public partial class RepositorioSql
    {
        // ---------------------------------------------------------------
        // Consultas del tablero
        // ---------------------------------------------------------------
        public IReadOnlyList<Ingrediente> ObtenerIngredientes()
        {
            using var db = _fabrica.Crear();
            return db.Ingredientes.AsNoTracking().OrderBy(i => i.Nombre).ToList();
        }

        /// <summary>
        /// El padrón con su cuenta y su ficha de cliente: la pantalla de Personas muestra
        /// las tres cosas juntas y decide con ellas si alguien es empleado o cliente.
        /// </summary>
        public IReadOnlyList<Persona> ObtenerPersonas()
        {
            using var db = _fabrica.Crear();
            return db.Personas.AsNoTracking()
                     .Include(p => p.Usuario).ThenInclude(u => u!.Rol)
                     .Include(p => p.Cliente)
                     .OrderBy(p => p.Apellido)
                     .ThenBy(p => p.Nombre)
                     .ToList();
        }

        /// <summary>
        /// Busca por documento ignorando puntos y guiones, que son formato y no dato.
        ///
        /// La comparación se hace en memoria a propósito: sacar los puntos de una columna
        /// no se traduce a SQL. Sobre un padrón de un bar es intrascendente; si algún día
        /// fueran cientos de miles, la solución sería guardar una columna ya normalizada.
        /// </summary>
        public Persona? BuscarPersonaPorDocumento(string? dniCuit)
        {
            string clave = NormalizarDocumento(dniCuit);
            if (clave.Length == 0)
                return null;

            using var db = _fabrica.Crear();

            var candidatos = db.Personas.AsNoTracking()
                               .Select(p => new { p.IdPersona, p.DniCuit })
                               .ToList();

            var encontrada = candidatos.FirstOrDefault(p => NormalizarDocumento(p.DniCuit) == clave);
            if (encontrada is null)
                return null;

            return db.Personas.AsNoTracking()
                     .Include(p => p.Usuario).ThenInclude(u => u!.Rol)
                     .Include(p => p.Cliente)
                     .FirstOrDefault(p => p.IdPersona == encontrada.IdPersona);
        }

        private static string NormalizarDocumento(string? documento) =>
            new((documento ?? string.Empty).Where(char.IsDigit).ToArray());

        public IReadOnlyList<Usuario> ObtenerUsuarios()
        {
            using var db = _fabrica.Crear();
            return db.Usuarios.AsNoTracking()
                     .Include(u => u.Persona)
                     .Include(u => u.Rol)
                     .OrderBy(u => u.Persona.Apellido)
                     .ThenBy(u => u.Persona.Nombre)
                     .ToList();
        }

        public IReadOnlyList<Rol> ObtenerRoles()
        {
            using var db = _fabrica.Crear();
            return db.Roles.AsNoTracking().OrderBy(r => r.IdRol).ToList();
        }

        public IReadOnlyList<Ubicacion> ObtenerTodasLasUbicaciones()
        {
            using var db = _fabrica.Crear();
            return db.Ubicaciones.AsNoTracking()
                     .OrderBy(u => u.Tipo)
                     .ThenBy(u => u.NombreUbicacion)
                     .ToList();
        }

        // ---------------------------------------------------------------
        // Producto
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarProducto(Producto producto)
        {
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                return ResultadoOperacion.Error("El nombre del producto es obligatorio.");

            if (producto.Precio <= 0)
                return ResultadoOperacion.Error("El precio debe ser mayor a cero.");

            if (producto.Stock < 0 || producto.StockMinimo < 0)
                return ResultadoOperacion.Error("El stock no puede ser negativo.");

            string nombre = producto.Nombre.Trim();

            using var db = _fabrica.Crear();

            if (!db.Categorias.Any(c => c.IdCategoria == producto.IdCategoria))
                return ResultadoOperacion.Error("Seleccione una categoría válida.");

            // RF-13: no repetir el nombre dentro del catálogo. La collation del schema ya
            // compara sin distinguir mayúsculas ni acentos.
            if (db.Productos.Any(p => p.IdProducto != producto.IdProducto && p.Nombre == nombre))
                return ResultadoOperacion.Error($"Ya existe un producto llamado \"{nombre}\".");

            bool esAlta = producto.IdProducto == 0;
            Producto fila;

            if (esAlta)
            {
                fila = new Producto();
                db.Productos.Add(fila);
            }
            else
            {
                var existente = db.Productos.FirstOrDefault(p => p.IdProducto == producto.IdProducto);
                if (existente is null)
                    return ResultadoOperacion.Error("El producto ya no existe.");

                fila = existente;
            }

            fila.Nombre = nombre;
            fila.Descripcion = string.IsNullOrWhiteSpace(producto.Descripcion) ? null : producto.Descripcion.Trim();
            fila.Precio = producto.Precio;
            fila.Stock = producto.Stock;
            fila.StockMinimo = producto.StockMinimo;
            fila.RutaImagen = string.IsNullOrWhiteSpace(producto.RutaImagen) ? null : producto.RutaImagen.Trim();
            fila.IdCategoria = producto.IdCategoria;

            db.SaveChanges();

            producto.IdProducto = fila.IdProducto;
            producto.Nombre = nombre;

            return ResultadoOperacion.Ok(esAlta
                ? $"Producto \"{nombre}\" creado."
                : $"Producto \"{nombre}\" actualizado.");
        }

        public ResultadoOperacion EliminarProducto(int idProducto)
        {
            using var db = _fabrica.Crear();

            var producto = db.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
            if (producto is null)
                return ResultadoOperacion.Error("El producto no existe.");

            // RF-13: no se borra algo que ya fue vendido, o el histórico queda colgado.
            int ventas = db.VentaDetalles.Count(d => d.IdProducto == idProducto);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: \"{producto.Nombre}\" aparece en {ventas} venta(s) registrada(s).");

            // La receta y la composición se van con él: están configuradas en cascada.
            db.Productos.Remove(producto);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Producto \"{producto.Nombre}\" eliminado.");
        }

        // ---------------------------------------------------------------
        // Receta: procedimiento (tabla receta) + composición (producto_ingrediente)
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarReceta(int idProducto, string? instrucciones, IEnumerable<ProductoIngrediente> composicion)
        {
            var lineas = composicion.ToList();

            if (lineas.Any(l => l.CantidadNecesaria <= 0))
                return ResultadoOperacion.Error("Todas las cantidades de la receta deben ser mayores a cero.");

            if (lineas.GroupBy(l => l.IdIngrediente).Any(g => g.Count() > 1))
                return ResultadoOperacion.Error("Hay un ingrediente repetido en la receta.");

            using var db = _fabrica.Crear();

            var producto = db.Productos
                             .Include(p => p.Ingredientes)
                             .Include(p => p.Receta)
                             .FirstOrDefault(p => p.IdProducto == idProducto);

            if (producto is null)
                return ResultadoOperacion.Error("El producto no existe.");

            var idsInsumo = lineas.Select(l => l.IdIngrediente).ToList();
            int existentes = db.Ingredientes.Count(i => idsInsumo.Contains(i.IdIngrediente));
            if (existentes != idsInsumo.Count)
                return ResultadoOperacion.Error("Hay un ingrediente inexistente en la receta.");

            // Reemplazo completo: la composición que llega es la que queda.
            db.ProductoIngredientes.RemoveRange(producto.Ingredientes);

            foreach (var linea in lineas)
            {
                db.ProductoIngredientes.Add(new ProductoIngrediente
                {
                    IdProducto = idProducto,
                    IdIngrediente = linea.IdIngrediente,
                    CantidadNecesaria = linea.CantidadNecesaria
                });
            }

            // El procedimiento solo tiene sentido si el producto se prepara.
            if (producto.Receta is not null)
                db.Recetas.Remove(producto.Receta);

            if (lineas.Count > 0 && !string.IsNullOrWhiteSpace(instrucciones))
            {
                db.Recetas.Add(new Receta
                {
                    IdProducto = idProducto,
                    Instrucciones = instrucciones.Trim()
                });
            }

            db.SaveChanges();

            return ResultadoOperacion.Ok($"Receta de \"{producto.Nombre}\" guardada.");
        }

        // ---------------------------------------------------------------
        // Ingrediente
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarIngrediente(Ingrediente ingrediente)
        {
            if (string.IsNullOrWhiteSpace(ingrediente.Nombre))
                return ResultadoOperacion.Error("El nombre del insumo es obligatorio.");

            if (string.IsNullOrWhiteSpace(ingrediente.UnidadMedida))
                return ResultadoOperacion.Error("Indique la unidad de medida (ml, g, unidades...).");

            if (ingrediente.Stock < 0 || ingrediente.StockMinimo < 0)
                return ResultadoOperacion.Error("El stock no puede ser negativo.");

            string nombre = ingrediente.Nombre.Trim();

            using var db = _fabrica.Crear();

            if (db.Ingredientes.Any(i => i.IdIngrediente != ingrediente.IdIngrediente && i.Nombre == nombre))
                return ResultadoOperacion.Error($"Ya existe un insumo llamado \"{nombre}\".");

            bool esAlta = ingrediente.IdIngrediente == 0;
            Ingrediente fila;

            if (esAlta)
            {
                fila = new Ingrediente();
                db.Ingredientes.Add(fila);
            }
            else
            {
                var existente = db.Ingredientes.FirstOrDefault(i => i.IdIngrediente == ingrediente.IdIngrediente);
                if (existente is null)
                    return ResultadoOperacion.Error("El insumo ya no existe.");

                fila = existente;
            }

            fila.Nombre = nombre;
            fila.UnidadMedida = ingrediente.UnidadMedida.Trim();
            fila.Stock = ingrediente.Stock;
            fila.StockMinimo = ingrediente.StockMinimo;

            db.SaveChanges();

            ingrediente.IdIngrediente = fila.IdIngrediente;
            ingrediente.Nombre = nombre;

            return ResultadoOperacion.Ok(esAlta
                ? $"Insumo \"{nombre}\" creado."
                : $"Insumo \"{nombre}\" actualizado.");
        }

        public ResultadoOperacion EliminarIngrediente(int idIngrediente)
        {
            using var db = _fabrica.Crear();

            var ingrediente = db.Ingredientes.FirstOrDefault(i => i.IdIngrediente == idIngrediente);
            if (ingrediente is null)
                return ResultadoOperacion.Error("El insumo no existe.");

            // RF-13: el caso que nombra el requerimiento — no borrar un insumo en uso.
            var recetas = db.ProductoIngredientes
                            .Where(pi => pi.IdIngrediente == idIngrediente)
                            .Select(pi => pi.Producto.Nombre)
                            .Distinct()
                            .ToList();

            if (recetas.Count > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: \"{ingrediente.Nombre}\" se usa en {string.Join(", ", recetas)}.");

            db.Ingredientes.Remove(ingrediente);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Insumo \"{ingrediente.Nombre}\" eliminado.");
        }

        // ---------------------------------------------------------------
        // Persona: el padrón (RF-10)
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarPersona(Persona persona, bool esCliente)
        {
            if (string.IsNullOrWhiteSpace(persona.Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(persona.Apellido))
                return ResultadoOperacion.Error("El apellido es obligatorio.");

            if (string.IsNullOrWhiteSpace(persona.DniCuit))
                return ResultadoOperacion.Error("El DNI/CUIT es obligatorio: identifica a la persona.");

            var conMismoDocumento = BuscarPersonaPorDocumento(persona.DniCuit);
            if (conMismoDocumento is not null && conMismoDocumento.IdPersona != persona.IdPersona)
                return ResultadoOperacion.Error(
                    $"El DNI/CUIT {persona.DniCuit.Trim()} ya es de {conMismoDocumento.NombreCompleto}.");

            using var db = _fabrica.Crear();

            string? correo = string.IsNullOrWhiteSpace(persona.Email) ? null : persona.Email.Trim();

            if (correo is not null &&
                db.Personas.Any(p => p.IdPersona != persona.IdPersona && p.Email == correo))
                return ResultadoOperacion.Error($"Ya hay una persona con el correo {correo}.");

            bool esAlta = persona.IdPersona == 0;
            Persona fila;

            if (esAlta)
            {
                fila = new Persona();
                db.Personas.Add(fila);
            }
            else
            {
                var existente = db.Personas.FirstOrDefault(p => p.IdPersona == persona.IdPersona);
                if (existente is null)
                    return ResultadoOperacion.Error("La persona ya no existe.");

                fila = existente;
            }

            fila.Nombre = persona.Nombre.Trim();
            fila.Apellido = persona.Apellido.Trim();
            fila.DniCuit = persona.DniCuit.Trim();
            fila.Telefono = string.IsNullOrWhiteSpace(persona.Telefono) ? null : persona.Telefono.Trim();
            fila.Email = correo;

            db.SaveChanges();
            persona.IdPersona = fila.IdPersona;

            var resultadoCliente = SincronizarCliente(db, fila, esCliente);
            if (!resultadoCliente.Exito)
                return resultadoCliente;

            return ResultadoOperacion.Ok(esAlta
                ? $"Persona \"{fila.NombreCompleto}\" creada."
                : $"Persona \"{fila.NombreCompleto}\" actualizada.");
        }

        /// <summary>
        /// Alinea la fila de Cliente con el tilde "es cliente del bar": la crea, la deja
        /// como está, o la da de baja. Ser cliente es un casillero de la ficha, no otra
        /// persona.
        /// </summary>
        private static ResultadoOperacion SincronizarCliente(BarDbContext db, Persona persona, bool esCliente)
        {
            var cliente = db.Clientes.FirstOrDefault(c => c.IdPersona == persona.IdPersona);

            if (esCliente && cliente is null)
            {
                db.Clientes.Add(new Cliente
                {
                    IdPersona = persona.IdPersona,
                    FechaRegistro = DateTime.Now
                });

                db.SaveChanges();
            }
            else if (!esCliente && cliente is not null)
            {
                // RF-13: un cliente con ventas no se borra, o el histórico queda colgado.
                int ventas = db.Ventas.Count(v => v.IdCliente == cliente.IdCliente);
                if (ventas > 0)
                    return ResultadoOperacion.Error(
                        $"No se puede quitar el cliente: {persona.NombreCompleto} figura en {ventas} venta(s).");

                db.Clientes.Remove(cliente);
                db.SaveChanges();
            }

            return ResultadoOperacion.Ok();
        }

        public ResultadoOperacion EliminarPersona(int idPersona)
        {
            using var db = _fabrica.Crear();

            var persona = db.Personas.FirstOrDefault(p => p.IdPersona == idPersona);
            if (persona is null)
                return ResultadoOperacion.Error("La persona no existe.");

            string nombre = persona.NombreCompleto;

            // RF-13: primero hay que dar de baja lo que cuelga de ella.
            if (db.Usuarios.Any(u => u.IdPersona == idPersona))
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {nombre} tiene una cuenta de acceso. Désela de baja primero.");

            var cliente = db.Clientes.FirstOrDefault(c => c.IdPersona == idPersona);
            if (cliente is not null)
            {
                int ventas = db.Ventas.Count(v => v.IdCliente == cliente.IdCliente);
                if (ventas > 0)
                    return ResultadoOperacion.Error(
                        $"No se puede eliminar: {nombre} figura como cliente en {ventas} venta(s).");
            }

            db.Personas.Remove(persona);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Persona \"{nombre}\" eliminada.");
        }

        // ---------------------------------------------------------------
        // Usuario: la CUENTA de acceso de una persona que ya está en el padrón
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva)
        {
            if (string.IsNullOrWhiteSpace(usuario.Email))
                return ResultadoOperacion.Error("El correo de trabajo es obligatorio: es la credencial de acceso.");

            string correo = usuario.Email.Trim();

            using var db = _fabrica.Crear();

            var persona = db.Personas.FirstOrDefault(p => p.IdPersona == usuario.IdPersona);
            if (persona is null)
                return ResultadoOperacion.Error("Seleccione la persona a la que pertenece la cuenta.");

            var rol = db.Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            if (rol is null)
                return ResultadoOperacion.Error("Seleccione un rol válido.");

            // RF-13: el correo de trabajo es el nombre de usuario, no puede repetirse.
            if (db.Usuarios.Any(u => u.IdUsuario != usuario.IdUsuario && u.Email == correo))
                return ResultadoOperacion.Error($"Ya hay una cuenta con el correo {correo}.");

            // Una persona tiene una sola cuenta: dos cuentas harían ambiguo quién cobró.
            if (db.Usuarios.Any(u => u.IdUsuario != usuario.IdUsuario && u.IdPersona == usuario.IdPersona))
                return ResultadoOperacion.Error($"{persona.NombreCompleto} ya tiene una cuenta de acceso.");

            bool esAlta = usuario.IdUsuario == 0;

            if (esAlta && string.IsNullOrWhiteSpace(claveNueva))
                return ResultadoOperacion.Error("Defina una contraseña para la cuenta nueva.");

            if (!string.IsNullOrWhiteSpace(claveNueva) && claveNueva.Trim().Length < 8)
                return ResultadoOperacion.Error("La contraseña debe tener al menos 8 caracteres.");

            // Bajar de rol al último administrador dejaría el sistema sin quién administre.
            if (!esAlta && rol.NombreRol != RolesSistema.Administrador &&
                EsElUltimoAdministrador(db, usuario.IdUsuario))
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            Usuario fila;

            if (esAlta)
            {
                fila = new Usuario();
                db.Usuarios.Add(fila);
            }
            else
            {
                var existente = db.Usuarios.FirstOrDefault(u => u.IdUsuario == usuario.IdUsuario);
                if (existente is null)
                    return ResultadoOperacion.Error("La cuenta ya no existe.");

                fila = existente;
            }

            fila.IdPersona = usuario.IdPersona;
            fila.IdRol = usuario.IdRol;
            fila.Email = correo;

            // Dejarla vacía al editar conserva la contraseña actual.
            if (!string.IsNullOrWhiteSpace(claveNueva))
                fila.Clave = SeguridadHelper.GenerarHash(claveNueva.Trim());

            db.SaveChanges();
            usuario.IdUsuario = fila.IdUsuario;

            return ResultadoOperacion.Ok(esAlta
                ? $"Cuenta de \"{persona.NombreCompleto}\" creada."
                : $"Cuenta de \"{persona.NombreCompleto}\" actualizada.");
        }

        private static bool EsElUltimoAdministrador(BarDbContext db, int idUsuario)
        {
            bool esAdministrador = db.Usuarios.Any(u => u.IdUsuario == idUsuario &&
                                                        u.Rol.NombreRol == RolesSistema.Administrador);

            if (!esAdministrador)
                return false;

            return db.Usuarios.Count(u => u.Rol.NombreRol == RolesSistema.Administrador) == 1;
        }

        public ResultadoOperacion EliminarUsuario(int idUsuario)
        {
            using var db = _fabrica.Crear();

            var usuario = db.Usuarios
                            .Include(u => u.Persona)
                            .FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuario is null)
                return ResultadoOperacion.Error("La cuenta no existe.");

            if (usuario.IdUsuario == _sesion.IdUsuario)
                return ResultadoOperacion.Error("No puede eliminar la cuenta con la que está trabajando.");

            // RF-13: las ventas guardan quién cobró y quién tomó el pedido.
            int ventas = db.Ventas.Count(v => v.IdCajero == idUsuario || v.IdMesero == idUsuario);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {usuario.Persona.NombreCompleto} figura en {ventas} venta(s).");

            if (EsElUltimoAdministrador(db, idUsuario))
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            string nombre = usuario.Persona.NombreCompleto;

            // La persona queda en el padrón: puede seguir siendo cliente (RF-10).
            db.Usuarios.Remove(usuario);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Cuenta de \"{nombre}\" eliminada.");
        }

        // ---------------------------------------------------------------
        // Tablas paramétricas
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarCategoria(Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.NombreCategoria))
                return ResultadoOperacion.Error("El nombre de la categoría es obligatorio.");

            string nombre = categoria.NombreCategoria.Trim();

            using var db = _fabrica.Crear();

            if (db.Categorias.Any(c => c.IdCategoria != categoria.IdCategoria && c.NombreCategoria == nombre))
                return ResultadoOperacion.Error("Ya existe una categoría con ese nombre.");

            bool esAlta = categoria.IdCategoria == 0;
            Categoria fila;

            if (esAlta)
            {
                fila = new Categoria();
                db.Categorias.Add(fila);
            }
            else
            {
                var existente = db.Categorias.FirstOrDefault(c => c.IdCategoria == categoria.IdCategoria);
                if (existente is null)
                    return ResultadoOperacion.Error("La categoría ya no existe.");

                fila = existente;
            }

            fila.NombreCategoria = nombre;
            db.SaveChanges();

            categoria.IdCategoria = fila.IdCategoria;
            categoria.NombreCategoria = nombre;

            return ResultadoOperacion.Ok(esAlta
                ? $"Categoría \"{nombre}\" creada."
                : $"Categoría \"{nombre}\" actualizada.");
        }

        public ResultadoOperacion EliminarCategoria(int idCategoria)
        {
            using var db = _fabrica.Crear();

            var categoria = db.Categorias.FirstOrDefault(c => c.IdCategoria == idCategoria);
            if (categoria is null)
                return ResultadoOperacion.Error("La categoría no existe.");

            int productos = db.Productos.Count(p => p.IdCategoria == idCategoria);
            if (productos > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {productos} producto(s) usan la categoría \"{categoria.NombreCategoria}\".");

            db.Categorias.Remove(categoria);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Categoría \"{categoria.NombreCategoria}\" eliminada.");
        }

        public ResultadoOperacion GuardarMetodoPago(MetodoPago metodoPago)
        {
            if (string.IsNullOrWhiteSpace(metodoPago.NombreMetodo))
                return ResultadoOperacion.Error("El nombre del método de pago es obligatorio.");

            string nombre = metodoPago.NombreMetodo.Trim();

            using var db = _fabrica.Crear();

            if (db.MetodosPago.Any(m => m.IdMetodoPago != metodoPago.IdMetodoPago && m.NombreMetodo == nombre))
                return ResultadoOperacion.Error("Ya existe un método de pago con ese nombre.");

            bool esAlta = metodoPago.IdMetodoPago == 0;
            MetodoPago fila;

            if (esAlta)
            {
                fila = new MetodoPago();
                db.MetodosPago.Add(fila);
            }
            else
            {
                var existente = db.MetodosPago.FirstOrDefault(m => m.IdMetodoPago == metodoPago.IdMetodoPago);
                if (existente is null)
                    return ResultadoOperacion.Error("El método de pago ya no existe.");

                fila = existente;
            }

            fila.NombreMetodo = nombre;
            db.SaveChanges();

            metodoPago.IdMetodoPago = fila.IdMetodoPago;
            metodoPago.NombreMetodo = nombre;

            return ResultadoOperacion.Ok(esAlta
                ? $"Método de pago \"{nombre}\" creado."
                : $"Método de pago \"{nombre}\" actualizado.");
        }

        public ResultadoOperacion EliminarMetodoPago(int idMetodoPago)
        {
            using var db = _fabrica.Crear();

            var metodo = db.MetodosPago.FirstOrDefault(m => m.IdMetodoPago == idMetodoPago);
            if (metodo is null)
                return ResultadoOperacion.Error("El método de pago no existe.");

            int ventas = db.Ventas.Count(v => v.IdMetodoPago == idMetodoPago);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {ventas} venta(s) se cobraron con \"{metodo.NombreMetodo}\".");

            if (db.MetodosPago.Count() == 1)
                return ResultadoOperacion.Error("Debe quedar al menos un método de pago.");

            db.MetodosPago.Remove(metodo);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Método de pago \"{metodo.NombreMetodo}\" eliminado.");
        }

        public ResultadoOperacion GuardarUbicacion(Ubicacion ubicacion)
        {
            if (string.IsNullOrWhiteSpace(ubicacion.NombreUbicacion))
                return ResultadoOperacion.Error("El nombre de la ubicación es obligatorio.");

            if (ubicacion.Capacidad <= 0)
                return ResultadoOperacion.Error("La capacidad debe ser mayor a cero.");

            string nombre = ubicacion.NombreUbicacion.Trim();

            using var db = _fabrica.Crear();

            if (db.Ubicaciones.Any(u => u.IdUbicacion != ubicacion.IdUbicacion && u.NombreUbicacion == nombre))
                return ResultadoOperacion.Error("Ya existe una ubicación con ese nombre.");

            bool esAlta = ubicacion.IdUbicacion == 0;
            Ubicacion fila;

            if (esAlta)
            {
                fila = new Ubicacion();
                db.Ubicaciones.Add(fila);
            }
            else
            {
                var existente = db.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == ubicacion.IdUbicacion);
                if (existente is null)
                    return ResultadoOperacion.Error("La ubicación ya no existe.");

                fila = existente;
            }

            fila.NombreUbicacion = nombre;
            fila.Capacidad = ubicacion.Capacidad;
            fila.Tipo = ubicacion.Tipo;

            db.SaveChanges();

            ubicacion.IdUbicacion = fila.IdUbicacion;
            ubicacion.NombreUbicacion = nombre;

            return ResultadoOperacion.Ok(esAlta
                ? $"Ubicación \"{nombre}\" creada."
                : $"Ubicación \"{nombre}\" actualizada.");
        }

        public ResultadoOperacion EliminarUbicacion(int idUbicacion)
        {
            using var db = _fabrica.Crear();

            var ubicacion = db.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
            if (ubicacion is null)
                return ResultadoOperacion.Error("La ubicación no existe.");

            int ventas = db.Ventas.Count(v => v.IdUbicacion == idUbicacion);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {ventas} venta(s) se hicieron en \"{ubicacion.NombreUbicacion}\".");

            db.Ubicaciones.Remove(ubicacion);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Ubicación \"{ubicacion.NombreUbicacion}\" eliminada.");
        }
    }
}
