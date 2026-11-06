using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    public partial class RepositorioSql
    {
        private static ResultadoOperacion? ControlarLargos(
            params (string? Valor, string Etiqueta, int Maximo)[] campos)
        {
            foreach (var campo in campos)
            {
                if ((campo.Valor?.Trim().Length ?? 0) > campo.Maximo)
                    return ResultadoOperacion.Error(
                        $"{campo.Etiqueta} no puede superar los {campo.Maximo} caracteres.");
            }

            return null;
        }

        /// <summary>Toma un bloqueo de escritura sobre los productos indicados y los insumos de sus recetas. Exige una transacción abierta; se libera al confirmarla o deshacerla.</summary>
        private static void BloquearStock(BarDbContext db, IEnumerable<int> idsProducto)
        {
            string ids = ListaDeIds(idsProducto);
            if (ids.Length == 0)
                return;

            // // Los ids son enteros: no hay nada que inyectar. Siempre en el mismo orden para no provocar bloqueos cruzados.
            EjecutarBloqueo(db,
                $"SELECT id_producto FROM producto WHERE id_producto IN ({ids}) ORDER BY id_producto FOR UPDATE");

            EjecutarBloqueo(db,
                "SELECT id_ingrediente FROM ingrediente WHERE id_ingrediente IN " +
                $"(SELECT id_ingrediente FROM producto_ingrediente WHERE id_producto IN ({ids})) " +
                "ORDER BY id_ingrediente FOR UPDATE");
        }

        private static void BloquearInsumo(BarDbContext db, int idIngrediente) =>
            EjecutarBloqueo(db,
                $"SELECT id_ingrediente FROM ingrediente WHERE id_ingrediente = {idIngrediente} FOR UPDATE");

        private static void BloquearProducto(BarDbContext db, int idProducto) =>
            EjecutarBloqueo(db,
                $"SELECT id_producto FROM producto WHERE id_producto = {idProducto} FOR UPDATE");

        private static string ListaDeIds(IEnumerable<int> ids) =>
            string.Join(",", ids.Distinct().OrderBy(i => i));

        private static void EjecutarBloqueo(BarDbContext db, string sql)
        {
            var transaccion = db.Database.CurrentTransaction
                ?? throw new InvalidOperationException("El bloqueo de stock necesita una transacción abierta.");

            var conexion = db.Database.GetDbConnection();
            using var comando = conexion.CreateCommand();
            comando.Transaction = transaccion.GetDbTransaction();
            comando.CommandText = sql;
            comando.ExecuteNonQuery();
        }

        /// <summary>Devuelve una fila nueva ya agregada al contexto si el id es 0, o la existente; si no está, el error.</summary>
        private static (T? Fila, ResultadoOperacion? Error) ObtenerOCrear<T>(
            DbSet<T> tabla, int id, string mensajeSiNoExiste) where T : class, new()
        {
            if (id == 0)
            {
                var nueva = new T();
                tabla.Add(nueva);
                return (nueva, null);
            }

            var existente = tabla.Find(id);
            return existente is null
                ? (null, ResultadoOperacion.Error(mensajeSiNoExiste))
                : (existente, null);
        }
    }
}
