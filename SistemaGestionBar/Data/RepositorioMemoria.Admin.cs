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

        public IReadOnlyList<Persona> ObtenerPersonas() =>
            _datos.Personas.OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ToList();

        /// <summary>
        /// El DNI/CUIT es el dato identificatorio de la persona: dos filas con el mismo
        /// documento son la misma persona. Por eso la búsqueda ignora puntos y guiones,
        /// que son formato y no dato: "40.123.456" y "40123456" son el mismo documento.
        /// </summary>
        public Persona? BuscarPersonaPorDocumento(string? dniCuit)
        {
            string clave = NormalizarDocumento(dniCuit);
            if (clave.Length == 0)
                return null;

            return _datos.Personas.FirstOrDefault(p => NormalizarDocumento(p.DniCuit) == clave);
        }

        private static string NormalizarDocumento(string? documento) =>
            new string((documento ?? string.Empty).Where(char.IsDigit).ToArray());

        public IReadOnlyList<Usuario> ObtenerUsuarios() =>
            _datos.Usuarios.OrderBy(u => u.Persona.Apellido).ThenBy(u => u.Persona.Nombre).ToList();

        public IReadOnlyList<Rol> ObtenerRoles() =>
            _datos.Roles.OrderBy(r => r.IdRol).ToList();

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
        // Persona: el padrón (RF-10). Una persona puede no ser nada todavía,
        // ser cliente del bar, ser empleada, o las dos cosas a la vez.
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarPersona(Persona persona, bool esCliente)
        {
            if (string.IsNullOrWhiteSpace(persona.Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(persona.Apellido))
                return ResultadoOperacion.Error("El apellido es obligatorio.");

            // El DNI/CUIT es el dato identificatorio: sin él no se puede saber si una
            // persona que vuelve es la misma que ya está cargada.
            if (string.IsNullOrWhiteSpace(persona.DniCuit))
                return ResultadoOperacion.Error("El DNI/CUIT es obligatorio: identifica a la persona.");

            // RF-13: comparando sin puntos ni guiones, que son formato y no dato.
            var conMismoDocumento = BuscarPersonaPorDocumento(persona.DniCuit);
            if (conMismoDocumento is not null && conMismoDocumento.IdPersona != persona.IdPersona)
                return ResultadoOperacion.Error(
                    $"El DNI/CUIT {persona.DniCuit.Trim()} ya es de {conMismoDocumento.NombreCompleto}.");

            // El correo personal es opcional, pero si está tampoco se repite: sirve
            // para encontrar a la persona y repetido vuelve ambigua la búsqueda.
            if (!string.IsNullOrWhiteSpace(persona.Email))
            {
                bool correoDuplicado = _datos.Personas.Any(p =>
                    p.IdPersona != persona.IdPersona &&
                    string.Equals(p.Email?.Trim(), persona.Email.Trim(), StringComparison.OrdinalIgnoreCase));
                if (correoDuplicado)
                    return ResultadoOperacion.Error($"Ya hay una persona con el correo {persona.Email.Trim()}.");
            }

            persona.Nombre = persona.Nombre.Trim();
            persona.Apellido = persona.Apellido.Trim();
            persona.DniCuit = persona.DniCuit.Trim();
            persona.Telefono = string.IsNullOrWhiteSpace(persona.Telefono) ? null : persona.Telefono.Trim();
            persona.Email = string.IsNullOrWhiteSpace(persona.Email) ? null : persona.Email.Trim();

            bool esAlta = persona.IdPersona == 0;
            if (esAlta)
            {
                persona.IdPersona = ProximoId(_datos.Personas, p => p.IdPersona);
                Auditar(persona, esAlta: true);
                _datos.Personas.Add(persona);
            }
            else
            {
                Auditar(persona, esAlta: false);
            }

            var resultadoCliente = SincronizarCliente(persona, esCliente);
            if (!resultadoCliente.Exito)
                return resultadoCliente;

            return ResultadoOperacion.Ok(
                esAlta ? $"Persona \"{persona.NombreCompleto}\" creada."
                       : $"Persona \"{persona.NombreCompleto}\" actualizada.");
        }

        /// <summary>
        /// Crea o quita la fila de Cliente según el tilde del formulario. Dar de baja al
        /// cliente no borra a la persona: el padrón es uno solo y la persona sigue existiendo.
        /// </summary>
        private ResultadoOperacion SincronizarCliente(Persona persona, bool esCliente)
        {
            var cliente = _datos.Clientes.FirstOrDefault(c => c.IdPersona == persona.IdPersona);

            if (esCliente && cliente is null)
            {
                cliente = new Cliente
                {
                    IdCliente = ProximoId(_datos.Clientes, c => c.IdCliente),
                    IdPersona = persona.IdPersona,
                    FechaRegistro = DateTime.Now,
                    Persona = persona
                };
                Auditar(cliente, esAlta: true);
                _datos.Clientes.Add(cliente);
                persona.Cliente = cliente;
            }
            else if (!esCliente && cliente is not null)
            {
                // RF-13: un cliente con ventas no se borra, o el histórico queda colgado.
                int ventas = _datos.Ventas.Count(v => v.IdCliente == cliente.IdCliente);
                if (ventas > 0)
                    return ResultadoOperacion.Error(
                        $"No se puede quitar el cliente: {persona.NombreCompleto} figura en {ventas} venta(s).");

                _datos.Clientes.Remove(cliente);
                persona.Cliente = null;
            }

            return ResultadoOperacion.Ok();
        }

        public ResultadoOperacion EliminarPersona(int idPersona)
        {
            var persona = _datos.Personas.FirstOrDefault(p => p.IdPersona == idPersona);
            if (persona is null)
                return ResultadoOperacion.Error("La persona no existe.");

            // RF-13: primero hay que dar de baja lo que cuelga de ella.
            if (_datos.Usuarios.Any(u => u.IdPersona == idPersona))
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {persona.NombreCompleto} tiene una cuenta de usuario. Elimínela primero en Usuarios.");

            var cliente = _datos.Clientes.FirstOrDefault(c => c.IdPersona == idPersona);
            if (cliente is not null)
            {
                int ventas = _datos.Ventas.Count(v => v.IdCliente == cliente.IdCliente);
                if (ventas > 0)
                    return ResultadoOperacion.Error(
                        $"No se puede eliminar: {persona.NombreCompleto} figura como cliente en {ventas} venta(s).");

                _datos.Clientes.Remove(cliente);
            }

            _datos.Personas.Remove(persona);
            return ResultadoOperacion.Ok($"Persona \"{persona.NombreCompleto}\" eliminada.");
        }

        // ---------------------------------------------------------------
        // Usuario: la CUENTA de acceso de una persona que ya está en el padrón.
        // Este método no escribe datos personales: eso es responsabilidad de Persona.
        // ---------------------------------------------------------------
        public ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva)
        {
            var persona = _datos.Personas.FirstOrDefault(p => p.IdPersona == usuario.IdPersona);
            if (persona is null)
                return ResultadoOperacion.Error("Seleccione la persona a la que pertenece la cuenta.");

            var rol = _datos.Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            if (rol is null)
                return ResultadoOperacion.Error("Seleccione un rol válido.");

            if (string.IsNullOrWhiteSpace(usuario.Email))
                return ResultadoOperacion.Error("El correo de trabajo es obligatorio: es la credencial de acceso.");

            // RF-13: el correo de trabajo es el nombre de usuario, no puede repetirse.
            bool correoDuplicado = _datos.Usuarios.Any(u =>
                u.IdUsuario != usuario.IdUsuario &&
                string.Equals(u.Email.Trim(), usuario.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (correoDuplicado)
                return ResultadoOperacion.Error($"Ya hay una cuenta con el correo {usuario.Email.Trim()}.");

            // Una persona tiene una sola cuenta: dos cuentas para la misma persona
            // harían ambiguo quién cobró una venta.
            bool personaYaTieneCuenta = _datos.Usuarios.Any(u =>
                u.IdUsuario != usuario.IdUsuario && u.IdPersona == usuario.IdPersona);
            if (personaYaTieneCuenta)
                return ResultadoOperacion.Error($"{persona.NombreCompleto} ya tiene una cuenta de usuario.");

            bool esAlta = usuario.IdUsuario == 0;

            if (esAlta && string.IsNullOrWhiteSpace(claveNueva))
                return ResultadoOperacion.Error("Defina una contraseña para la cuenta nueva.");

            if (!string.IsNullOrWhiteSpace(claveNueva) && claveNueva.Trim().Length < 8)
                return ResultadoOperacion.Error("La contraseña debe tener al menos 8 caracteres.");

            // Bajar de rol al último administrador dejaría el sistema sin quien administre.
            if (!esAlta && EsElUltimoAdministrador(usuario) && rol.NombreRol != RolesSistema.Administrador)
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            usuario.Email = usuario.Email.Trim();
            usuario.Persona = persona;
            usuario.Rol = rol;

            if (!string.IsNullOrWhiteSpace(claveNueva))
                usuario.Clave = SeguridadHelper.GenerarHash(claveNueva.Trim());

            if (esAlta)
            {
                usuario.IdUsuario = ProximoId(_datos.Usuarios, u => u.IdUsuario);
                Auditar(usuario, esAlta: true);
                _datos.Usuarios.Add(usuario);

                persona.Usuario = usuario;
                rol.Usuarios.Add(usuario);
                return ResultadoOperacion.Ok($"Cuenta de \"{persona.NombreCompleto}\" creada.");
            }

            Auditar(usuario, esAlta: false);
            return ResultadoOperacion.Ok($"Cuenta de \"{persona.NombreCompleto}\" actualizada.");
        }

        private bool EsElUltimoAdministrador(Usuario usuario) =>
            usuario.NombreRol == RolesSistema.Administrador &&
            _datos.Usuarios.Count(u => u.NombreRol == RolesSistema.Administrador) == 1;

        public ResultadoOperacion EliminarUsuario(int idUsuario)
        {
            var usuario = _datos.Usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);
            if (usuario is null)
                return ResultadoOperacion.Error("La cuenta no existe.");

            if (usuario.IdUsuario == _sesion.IdUsuario)
                return ResultadoOperacion.Error("No puede eliminar la cuenta con la que está trabajando.");

            // RF-13: las ventas guardan quién cobró y quién tomó el pedido.
            int ventas = _datos.Ventas.Count(v => v.IdCajero == idUsuario || v.IdMesero == idUsuario);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {usuario.NombreCompleto} figura en {ventas} venta(s).");

            if (EsElUltimoAdministrador(usuario))
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            string nombre = usuario.NombreCompleto;
            usuario.Rol?.Usuarios.Remove(usuario);
            _datos.Usuarios.Remove(usuario);

            // La Persona queda en el padrón: puede ser también cliente, y RF-10 la centraliza.
            var persona = _datos.Personas.FirstOrDefault(p => p.IdPersona == usuario.IdPersona);
            if (persona is not null)
                persona.Usuario = null;

            return ResultadoOperacion.Ok($"Cuenta de \"{nombre}\" eliminada.");
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
    }
}
