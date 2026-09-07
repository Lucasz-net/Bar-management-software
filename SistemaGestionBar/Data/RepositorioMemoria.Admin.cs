using System;
using System.Collections.Generic;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// Parte del repositorio que atiende al módulo de administración (RF-12 a RF-14).
    /// Está separada del punto de venta para que cada archivo cuente una sola historia.
    ///
    /// Dos reglas se aplican en TODOS los métodos de esta clase:
    ///  - RF-13: se valida antes de tocar los datos (duplicados y claves foráneas activas).
    ///  - RF-11: se estampan los campos de auditoría con el usuario de la sesión.
    /// </summary>
    public partial class RepositorioMemoria
    {
        // ---------------------------------------------------------------
        // Consultas
        // ---------------------------------------------------------------
        public IReadOnlyList<Ingrediente> ObtenerIngredientes() =>
            _datos.Ingredientes.OrderBy(i => i.Nombre).ToList();

        public IReadOnlyList<Usuario> ObtenerUsuarios() =>
            _datos.Usuarios.OrderBy(u => u.NombreCompleto).ToList();

        public IReadOnlyList<Rol> ObtenerRoles() =>
            _datos.Roles.OrderBy(r => r.IdRol).ToList();

        public IReadOnlyList<Reporte> ObtenerReportes() =>
            _datos.Reportes.OrderByDescending(r => r.FechaGeneracion).ToList();

        public IReadOnlyList<Ubicacion> ObtenerTodasLasUbicaciones() =>
            _datos.Ubicaciones.OrderBy(u => u.Tipo).ThenBy(u => u.NombreUbicacion).ToList();

        // ---------------------------------------------------------------
        // Auditoría (RF-11)
        // ---------------------------------------------------------------
        /// <summary>
        /// Completa fecha y usuario de la modificación. Es lo mismo que hará el
        /// interceptor de SaveChanges cuando entre EF Core.
        /// </summary>
        private void Auditar(EntidadAuditable entidad, bool esAlta)
        {
            var ahora = DateTime.Now;

            if (esAlta)
                entidad.FechaCreacion = ahora;
            else
                entidad.FechaModificacion = ahora;

            entidad.UsuarioModificacion = _sesion.IdUsuario;
        }

        private static int ProximoId<T>(IEnumerable<T> items, Func<T, int> selector) =>
            items.Any() ? items.Max(selector) + 1 : 1;

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

            var categoria = _datos.Categorias.FirstOrDefault(c => c.IdCategoria == producto.IdCategoria);
            if (categoria is null)
                return ResultadoOperacion.Error("Seleccione una categoría válida.");

            // RF-13: no repetir el nombre dentro del catálogo.
            bool duplicado = _datos.Productos.Any(p =>
                p.IdProducto != producto.IdProducto &&
                string.Equals(p.Nombre.Trim(), producto.Nombre.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicado)
                return ResultadoOperacion.Error($"Ya existe un producto llamado \"{producto.Nombre.Trim()}\".");

            producto.Nombre = producto.Nombre.Trim();
            producto.Categoria = categoria;

            bool esAlta = producto.IdProducto == 0;
            if (esAlta)
            {
                producto.IdProducto = ProximoId(_datos.Productos, p => p.IdProducto);
                Auditar(producto, esAlta: true);
                _datos.Productos.Add(producto);
                categoria.Productos.Add(producto);
                return ResultadoOperacion.Ok($"Producto \"{producto.Nombre}\" creado.");
            }

            Auditar(producto, esAlta: false);
            return ResultadoOperacion.Ok($"Producto \"{producto.Nombre}\" actualizado.");
        }

        public ResultadoOperacion EliminarProducto(int idProducto)
        {
            var producto = _datos.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
            if (producto is null)
                return ResultadoOperacion.Error("El producto no existe.");

            // RF-13: no se borra algo que ya fue vendido, o el histórico queda colgado.
            int ventas = _datos.Ventas.SelectMany(v => v.Detalles).Count(d => d.IdProducto == idProducto);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: \"{producto.Nombre}\" aparece en {ventas} venta(s) registrada(s).");

            _datos.ProductoIngredientes.RemoveAll(pi => pi.IdProducto == idProducto);
            _datos.Recetas.RemoveAll(r => r.IdProducto == idProducto);
            producto.Categoria?.Productos.Remove(producto);
            _datos.Productos.Remove(producto);

            return ResultadoOperacion.Ok($"Producto \"{producto.Nombre}\" eliminado.");
        }

        // ---------------------------------------------------------------
        // Receta: procedimiento (tabla Receta) + composición (Producto_Ingrediente)
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarReceta(int idProducto, string? instrucciones, IEnumerable<ProductoIngrediente> composicion)
        {
            var producto = _datos.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
            if (producto is null)
                return ResultadoOperacion.Error("El producto no existe.");

            var lineas = composicion.ToList();

            if (lineas.Any(l => l.CantidadNecesaria <= 0))
                return ResultadoOperacion.Error("Todas las cantidades de la receta deben ser mayores a cero.");

            if (lineas.GroupBy(l => l.IdIngrediente).Any(g => g.Count() > 1))
                return ResultadoOperacion.Error("Hay un ingrediente repetido en la receta.");

            // Reemplazo completo de la composición
            _datos.ProductoIngredientes.RemoveAll(pi => pi.IdProducto == idProducto);
            producto.Ingredientes.Clear();

            foreach (var linea in lineas)
            {
                var ingrediente = _datos.Ingredientes.FirstOrDefault(i => i.IdIngrediente == linea.IdIngrediente);
                if (ingrediente is null)
                    return ResultadoOperacion.Error("Hay un ingrediente inexistente en la receta.");

                var fila = new ProductoIngrediente
                {
                    IdProducto = idProducto,
                    IdIngrediente = ingrediente.IdIngrediente,
                    CantidadNecesaria = linea.CantidadNecesaria,
                    Producto = producto,
                    Ingrediente = ingrediente
                };
                _datos.ProductoIngredientes.Add(fila);
                producto.Ingredientes.Add(fila);
            }

            // El procedimiento solo tiene sentido si el producto se prepara
            _datos.Recetas.RemoveAll(r => r.IdProducto == idProducto);
            producto.Receta = null;

            if (lineas.Any() && !string.IsNullOrWhiteSpace(instrucciones))
            {
                var receta = new Receta
                {
                    IdProducto = idProducto,
                    Instrucciones = instrucciones.Trim(),
                    Producto = producto
                };
                Auditar(receta, esAlta: true);
                _datos.Recetas.Add(receta);
                producto.Receta = receta;
            }

            Auditar(producto, esAlta: false);
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

            bool duplicado = _datos.Ingredientes.Any(i =>
                i.IdIngrediente != ingrediente.IdIngrediente &&
                string.Equals(i.Nombre.Trim(), ingrediente.Nombre.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicado)
                return ResultadoOperacion.Error($"Ya existe un insumo llamado \"{ingrediente.Nombre.Trim()}\".");

            ingrediente.Nombre = ingrediente.Nombre.Trim();
            ingrediente.UnidadMedida = ingrediente.UnidadMedida.Trim();

            if (ingrediente.IdIngrediente == 0)
            {
                ingrediente.IdIngrediente = ProximoId(_datos.Ingredientes, i => i.IdIngrediente);
                Auditar(ingrediente, esAlta: true);
                _datos.Ingredientes.Add(ingrediente);
                return ResultadoOperacion.Ok($"Insumo \"{ingrediente.Nombre}\" creado.");
            }

            Auditar(ingrediente, esAlta: false);
            return ResultadoOperacion.Ok($"Insumo \"{ingrediente.Nombre}\" actualizado.");
        }

        public ResultadoOperacion EliminarIngrediente(int idIngrediente)
        {
            var ingrediente = _datos.Ingredientes.FirstOrDefault(i => i.IdIngrediente == idIngrediente);
            if (ingrediente is null)
                return ResultadoOperacion.Error("El insumo no existe.");

            // RF-13: bloqueo por clave foránea activa (el caso que nombra el requerimiento).
            var recetas = _datos.ProductoIngredientes
                .Where(pi => pi.IdIngrediente == idIngrediente)
                .Select(pi => pi.Producto.Nombre)
                .Distinct()
                .ToList();

            if (recetas.Any())
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: \"{ingrediente.Nombre}\" se usa en {string.Join(", ", recetas)}.");

            _datos.Ingredientes.Remove(ingrediente);
            return ResultadoOperacion.Ok($"Insumo \"{ingrediente.Nombre}\" eliminado.");
        }

        // ---------------------------------------------------------------
        // Usuario + Persona (RF-10: los datos personales van a Persona)
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva)
        {
            var persona = usuario.Persona;
            if (persona is null || string.IsNullOrWhiteSpace(persona.Nombre))
                return ResultadoOperacion.Error("El nombre de la persona es obligatorio.");

            if (string.IsNullOrWhiteSpace(persona.Email))
                return ResultadoOperacion.Error("El correo es obligatorio: es la credencial de acceso.");

            var rol = _datos.Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            if (rol is null)
                return ResultadoOperacion.Error("Seleccione un rol válido.");

            // RF-13: el correo es la credencial de login, no puede repetirse.
            bool correoDuplicado = _datos.Personas.Any(p =>
                p.IdPersona != persona.IdPersona &&
                string.Equals(p.Email, persona.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (correoDuplicado)
                return ResultadoOperacion.Error($"Ya hay una persona registrada con el correo {persona.Email.Trim()}.");

            // RF-13: el DNI/CUIT tampoco.
            if (!string.IsNullOrWhiteSpace(persona.DniCuit))
            {
                bool dniDuplicado = _datos.Personas.Any(p =>
                    p.IdPersona != persona.IdPersona &&
                    string.Equals(p.DniCuit, persona.DniCuit.Trim(), StringComparison.OrdinalIgnoreCase));
                if (dniDuplicado)
                    return ResultadoOperacion.Error($"Ya hay una persona con el DNI/CUIT {persona.DniCuit.Trim()}.");
            }

            bool esAlta = usuario.IdUsuario == 0;

            if (esAlta && string.IsNullOrWhiteSpace(claveNueva))
                return ResultadoOperacion.Error("Defina una contraseña para el usuario nuevo.");

            if (!string.IsNullOrWhiteSpace(claveNueva) && claveNueva.Trim().Length < 8)
                return ResultadoOperacion.Error("La contraseña debe tener al menos 8 caracteres.");

            persona.Nombre = persona.Nombre.Trim();
            persona.Email = persona.Email.Trim();
            persona.DniCuit = persona.DniCuit?.Trim();
            persona.Telefono = persona.Telefono?.Trim();

            usuario.Rol = rol;
            usuario.IdPersona = persona.IdPersona;

            if (!string.IsNullOrWhiteSpace(claveNueva))
                usuario.Clave = SeguridadHelper.GenerarHash(claveNueva.Trim());

            if (esAlta)
            {
                persona.IdPersona = ProximoId(_datos.Personas, p => p.IdPersona);
                Auditar(persona, esAlta: true);
                _datos.Personas.Add(persona);

                usuario.IdPersona = persona.IdPersona;
                usuario.IdUsuario = ProximoId(_datos.Usuarios, u => u.IdUsuario);
                Auditar(usuario, esAlta: true);
                _datos.Usuarios.Add(usuario);

                persona.Usuario = usuario;
                rol.Usuarios.Add(usuario);
                return ResultadoOperacion.Ok($"Usuario \"{persona.Nombre}\" creado.");
            }

            Auditar(persona, esAlta: false);
            Auditar(usuario, esAlta: false);
            return ResultadoOperacion.Ok($"Usuario \"{persona.Nombre}\" actualizado.");
        }

        public ResultadoOperacion EliminarUsuario(int idUsuario)
        {
            var usuario = _datos.Usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);
            if (usuario is null)
                return ResultadoOperacion.Error("El usuario no existe.");

            if (usuario.IdUsuario == _sesion.IdUsuario)
                return ResultadoOperacion.Error("No puede eliminar el usuario con el que está trabajando.");

            // RF-13: las ventas guardan quién cobró y quién tomó el pedido.
            int ventas = _datos.Ventas.Count(v => v.IdCajero == idUsuario || v.IdMesero == idUsuario);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {usuario.NombreCompleto} figura en {ventas} venta(s).");

            if (_datos.Reportes.Any(r => r.IdUsuarioGenerador == idUsuario))
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {usuario.NombreCompleto} generó reportes registrados.");

            if (usuario.NombreRol == RolesSistema.Administrador &&
                _datos.Usuarios.Count(u => u.NombreRol == RolesSistema.Administrador) == 1)
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            string nombre = usuario.NombreCompleto;
            usuario.Rol?.Usuarios.Remove(usuario);
            _datos.Usuarios.Remove(usuario);

            // La Persona queda: puede ser también cliente, y RF-10 la centraliza.
            var persona = _datos.Personas.FirstOrDefault(p => p.IdPersona == usuario.IdPersona);
            if (persona is not null)
                persona.Usuario = null;

            return ResultadoOperacion.Ok($"Usuario \"{nombre}\" eliminado.");
        }

        // ---------------------------------------------------------------
        // Tablas paramétricas
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarCategoria(Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.NombreCategoria))
                return ResultadoOperacion.Error("El nombre de la categoría es obligatorio.");

            bool duplicado = _datos.Categorias.Any(c =>
                c.IdCategoria != categoria.IdCategoria &&
                string.Equals(c.NombreCategoria.Trim(), categoria.NombreCategoria.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicado)
                return ResultadoOperacion.Error("Ya existe una categoría con ese nombre.");

            categoria.NombreCategoria = categoria.NombreCategoria.Trim();

            if (categoria.IdCategoria == 0)
            {
                categoria.IdCategoria = ProximoId(_datos.Categorias, c => c.IdCategoria);
                Auditar(categoria, esAlta: true);
                _datos.Categorias.Add(categoria);
                return ResultadoOperacion.Ok($"Categoría \"{categoria.NombreCategoria}\" creada.");
            }

            Auditar(categoria, esAlta: false);
            return ResultadoOperacion.Ok($"Categoría \"{categoria.NombreCategoria}\" actualizada.");
        }

        public ResultadoOperacion EliminarCategoria(int idCategoria)
        {
            var categoria = _datos.Categorias.FirstOrDefault(c => c.IdCategoria == idCategoria);
            if (categoria is null)
                return ResultadoOperacion.Error("La categoría no existe.");

            int productos = _datos.Productos.Count(p => p.IdCategoria == idCategoria);
            if (productos > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {productos} producto(s) usan la categoría \"{categoria.NombreCategoria}\".");

            _datos.Categorias.Remove(categoria);
            return ResultadoOperacion.Ok($"Categoría \"{categoria.NombreCategoria}\" eliminada.");
        }

        public ResultadoOperacion GuardarMetodoPago(MetodoPago metodoPago)
        {
            if (string.IsNullOrWhiteSpace(metodoPago.NombreMetodo))
                return ResultadoOperacion.Error("El nombre del método de pago es obligatorio.");

            bool duplicado = _datos.MetodosPago.Any(m =>
                m.IdMetodoPago != metodoPago.IdMetodoPago &&
                string.Equals(m.NombreMetodo.Trim(), metodoPago.NombreMetodo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicado)
                return ResultadoOperacion.Error("Ya existe un método de pago con ese nombre.");

            metodoPago.NombreMetodo = metodoPago.NombreMetodo.Trim();

            if (metodoPago.IdMetodoPago == 0)
            {
                metodoPago.IdMetodoPago = ProximoId(_datos.MetodosPago, m => m.IdMetodoPago);
                Auditar(metodoPago, esAlta: true);
                _datos.MetodosPago.Add(metodoPago);
                return ResultadoOperacion.Ok($"Método de pago \"{metodoPago.NombreMetodo}\" creado.");
            }

            Auditar(metodoPago, esAlta: false);
            return ResultadoOperacion.Ok($"Método de pago \"{metodoPago.NombreMetodo}\" actualizado.");
        }

        public ResultadoOperacion EliminarMetodoPago(int idMetodoPago)
        {
            var metodo = _datos.MetodosPago.FirstOrDefault(m => m.IdMetodoPago == idMetodoPago);
            if (metodo is null)
                return ResultadoOperacion.Error("El método de pago no existe.");

            int ventas = _datos.Ventas.Count(v => v.IdMetodoPago == idMetodoPago);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {ventas} venta(s) se cobraron con \"{metodo.NombreMetodo}\".");

            if (_datos.MetodosPago.Count == 1)
                return ResultadoOperacion.Error("Debe quedar al menos un método de pago.");

            _datos.MetodosPago.Remove(metodo);
            return ResultadoOperacion.Ok($"Método de pago \"{metodo.NombreMetodo}\" eliminado.");
        }

        public ResultadoOperacion GuardarUbicacion(Ubicacion ubicacion)
        {
            if (string.IsNullOrWhiteSpace(ubicacion.NombreUbicacion))
                return ResultadoOperacion.Error("El nombre de la ubicación es obligatorio.");

            if (ubicacion.Capacidad <= 0)
                return ResultadoOperacion.Error("La capacidad debe ser mayor a cero.");

            bool duplicado = _datos.Ubicaciones.Any(u =>
                u.IdUbicacion != ubicacion.IdUbicacion &&
                string.Equals(u.NombreUbicacion.Trim(), ubicacion.NombreUbicacion.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicado)
                return ResultadoOperacion.Error("Ya existe una ubicación con ese nombre.");

            ubicacion.NombreUbicacion = ubicacion.NombreUbicacion.Trim();

            if (ubicacion.IdUbicacion == 0)
            {
                ubicacion.IdUbicacion = ProximoId(_datos.Ubicaciones, u => u.IdUbicacion);
                Auditar(ubicacion, esAlta: true);
                _datos.Ubicaciones.Add(ubicacion);
                return ResultadoOperacion.Ok($"Ubicación \"{ubicacion.NombreUbicacion}\" creada.");
            }

            Auditar(ubicacion, esAlta: false);
            return ResultadoOperacion.Ok($"Ubicación \"{ubicacion.NombreUbicacion}\" actualizada.");
        }

        public ResultadoOperacion EliminarUbicacion(int idUbicacion)
        {
            var ubicacion = _datos.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
            if (ubicacion is null)
                return ResultadoOperacion.Error("La ubicación no existe.");

            int ventas = _datos.Ventas.Count(v => v.IdUbicacion == idUbicacion);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {ventas} venta(s) se hicieron en \"{ubicacion.NombreUbicacion}\".");

            _datos.Ubicaciones.Remove(ubicacion);
            return ResultadoOperacion.Ok($"Ubicación \"{ubicacion.NombreUbicacion}\" eliminada.");
        }

        // ---------------------------------------------------------------
        // Reporte (RF-14)
        // ---------------------------------------------------------------
        public ResultadoOperacion RegistrarReporte(Reporte reporte)
        {
            if (reporte.IdUsuarioGenerador <= 0)
                return ResultadoOperacion.Error("No se identificó al usuario que generó el reporte.");

            reporte.IdReporte = ProximoId(_datos.Reportes, r => r.IdReporte);
            reporte.FechaGeneracion = DateTime.Now;
            reporte.UsuarioGenerador = _datos.Usuarios.First(u => u.IdUsuario == reporte.IdUsuarioGenerador);
            Auditar(reporte, esAlta: true);

            _datos.Reportes.Add(reporte);
            return ResultadoOperacion.Ok($"Reporte #{reporte.IdReporte} registrado.");
        }
    }
}
