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
        public ResultadoOperacion GuardarMetodoPago(MetodoPago metodoPago)
        {
            if (string.IsNullOrWhiteSpace(metodoPago.NombreMetodo))
                return ResultadoOperacion.Error("El nombre del método de pago es obligatorio.");

            var largo = ControlarLargos(
                (metodoPago.NombreMetodo, "El nombre del método de pago", Limites.NombreParametrico));

            if (largo is not null)
                return largo;

            string nombre = metodoPago.NombreMetodo.Trim();

            using var db = _fabrica.Crear();

            if (db.MetodosPago.Any(m => m.IdMetodoPago != metodoPago.IdMetodoPago && m.NombreMetodo == nombre))
                return ResultadoOperacion.Error("Ya existe un método de pago con ese nombre.");

            bool esAlta = metodoPago.IdMetodoPago == 0;
            var (fila, error) = ObtenerOCrear(db.MetodosPago, metodoPago.IdMetodoPago, "El método de pago ya no existe.");
            if (fila is null)
                return error!;

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

            var largo = ControlarLargos(
                (ubicacion.NombreUbicacion, "El nombre de la ubicación", Limites.NombreParametrico));

            if (largo is not null)
                return largo;

            string nombre = ubicacion.NombreUbicacion.Trim();

            using var db = _fabrica.Crear();

            if (db.Ubicaciones.Any(u => u.IdUbicacion != ubicacion.IdUbicacion && u.NombreUbicacion == nombre))
                return ResultadoOperacion.Error("Ya existe una ubicación con ese nombre.");

            bool esAlta = ubicacion.IdUbicacion == 0;
            var (fila, error) = ObtenerOCrear(db.Ubicaciones, ubicacion.IdUbicacion, "La ubicación ya no existe.");
            if (fila is null)
                return error!;

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
