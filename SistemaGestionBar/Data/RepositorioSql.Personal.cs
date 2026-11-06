using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    public partial class RepositorioSql
    {
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

        public Persona? BuscarPersonaPorDocumento(string? dniCuit)
        {
            string clave = Documento.Normalizar(dniCuit);
            if (clave.Length == 0)
                return null;

            using var db = _fabrica.Crear();

            var candidatos = db.Personas.AsNoTracking()
                               .Select(p => new { p.IdPersona, p.DniCuit })
                               .ToList();

            var encontrada = candidatos.FirstOrDefault(p => Documento.Normalizar(p.DniCuit) == clave);
            if (encontrada is null)
                return null;

            return db.Personas.AsNoTracking()
                     .Include(p => p.Usuario).ThenInclude(u => u!.Rol)
                     .Include(p => p.Cliente)
                     .FirstOrDefault(p => p.IdPersona == encontrada.IdPersona);
        }

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

        public ResultadoOperacion GuardarPersona(Persona persona, bool esCliente)
        {
            if (string.IsNullOrWhiteSpace(persona.Nombre))
                return ResultadoOperacion.Error("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(persona.Apellido))
                return ResultadoOperacion.Error("El apellido es obligatorio.");

            var largo = ControlarLargos(
                (persona.Nombre, "El nombre", Limites.NombrePersona),
                (persona.Apellido, "El apellido", Limites.ApellidoPersona),
                (persona.Telefono, "El teléfono", Limites.Telefono),
                (persona.Email, "El correo", Limites.Email));

            if (largo is not null)
                return largo;

            string dni = Documento.Normalizar(persona.DniCuit);

            if (dni.Length == 0)
                return ResultadoOperacion.Error("El DNI es obligatorio: identifica a la persona.");

            if (dni.Length != Documento.Digitos)
                return ResultadoOperacion.Error($"El DNI tiene que tener {Documento.Digitos} números.");

            var conMismoDocumento = BuscarPersonaPorDocumento(dni);
            if (conMismoDocumento is not null && conMismoDocumento.IdPersona != persona.IdPersona)
                return ResultadoOperacion.Error(
                    $"El DNI {Documento.Formatear(dni)} ya es de {conMismoDocumento.NombreCompleto}.");

            using var db = _fabrica.Crear();

            string? correo = string.IsNullOrWhiteSpace(persona.Email) ? null : persona.Email.Trim();

            if (correo is not null &&
                db.Personas.Any(p => p.IdPersona != persona.IdPersona && p.Email == correo))
                return ResultadoOperacion.Error($"Ya hay una persona con el correo {correo}.");

            bool esAlta = persona.IdPersona == 0;
            var (fila, error) = ObtenerOCrear(db.Personas, persona.IdPersona, "La persona ya no existe.");
            if (fila is null)
                return error!;

            fila.Nombre = persona.Nombre.Trim();
            fila.Apellido = persona.Apellido.Trim();
            fila.DniCuit = dni;
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

        public ResultadoOperacion GuardarUsuario(Usuario usuario, string? claveNueva)
        {
            if (string.IsNullOrWhiteSpace(usuario.Email))
                return ResultadoOperacion.Error("El correo de trabajo es obligatorio: es la credencial de acceso.");

            var largo = ControlarLargos((usuario.Email, "El correo de trabajo", Limites.Email));
            if (largo is not null)
                return largo;

            string correo = usuario.Email.Trim();

            using var db = _fabrica.Crear();

            var persona = db.Personas.FirstOrDefault(p => p.IdPersona == usuario.IdPersona);
            if (persona is null)
                return ResultadoOperacion.Error("Seleccione la persona a la que pertenece la cuenta.");

            var rol = db.Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            if (rol is null)
                return ResultadoOperacion.Error("Seleccione un rol válido.");

            if (db.Usuarios.Any(u => u.IdUsuario != usuario.IdUsuario && u.Email == correo))
                return ResultadoOperacion.Error($"Ya hay una cuenta con el correo {correo}.");

            if (db.Usuarios.Any(u => u.IdUsuario != usuario.IdUsuario && u.IdPersona == usuario.IdPersona))
                return ResultadoOperacion.Error($"{persona.NombreCompleto} ya tiene una cuenta de acceso.");

            bool esAlta = usuario.IdUsuario == 0;

            if (esAlta && string.IsNullOrWhiteSpace(claveNueva))
                return ResultadoOperacion.Error("Defina una contraseña para la cuenta nueva.");

            if (!string.IsNullOrWhiteSpace(claveNueva) && claveNueva.Trim().Length < 8)
                return ResultadoOperacion.Error("La contraseña debe tener al menos 8 caracteres.");

            if (!esAlta && rol.NombreRol != RolesSistema.Administrador &&
                EsElUltimoAdministrador(db, usuario.IdUsuario))
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            var (fila, error) = ObtenerOCrear(db.Usuarios, usuario.IdUsuario, "La cuenta ya no existe.");
            if (fila is null)
                return error!;

            fila.IdPersona = usuario.IdPersona;
            fila.IdRol = usuario.IdRol;
            fila.Email = correo;

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

            int ventas = db.Ventas.Count(v => v.IdCajero == idUsuario || v.IdMesero == idUsuario);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: {usuario.Persona.NombreCompleto} figura en {ventas} venta(s).");

            if (EsElUltimoAdministrador(db, idUsuario))
                return ResultadoOperacion.Error("Debe quedar al menos un administrador en el sistema.");

            string nombre = usuario.Persona.NombreCompleto;

            db.Usuarios.Remove(usuario);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Cuenta de \"{nombre}\" eliminada.");
        }
    }
}
