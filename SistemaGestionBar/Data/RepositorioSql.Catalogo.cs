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
        public IReadOnlyList<Ingrediente> ObtenerIngredientes()
        {
            using var db = _fabrica.Crear();
            return db.Ingredientes.AsNoTracking().OrderBy(i => i.Nombre).ToList();
        }

        public ResultadoOperacion GuardarProducto(Producto producto)
        {
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                return ResultadoOperacion.Error("El nombre del producto es obligatorio.");

            if (producto.Precio <= 0)
                return ResultadoOperacion.Error("El precio debe ser mayor a cero.");

            if (producto.Precio > Limites.Dinero)
                return ResultadoOperacion.Error($"El precio no puede superar {Limites.Dinero:N2}.");

            if (producto.Stock < 0 || producto.StockMinimo < 0)
                return ResultadoOperacion.Error("El stock no puede ser negativo.");

            var largo = ControlarLargos(
                (producto.Nombre, "El nombre del producto", Limites.NombreProducto),
                (producto.Descripcion, "La descripción", Limites.DescripcionProducto),
                (producto.RutaImagen, "La ruta de la imagen", Limites.RutaImagen));

            if (largo is not null)
                return largo;

            string nombre = producto.Nombre.Trim();

            using var db = _fabrica.Crear();

            if (!db.Categorias.Any(c => c.IdCategoria == producto.IdCategoria))
                return ResultadoOperacion.Error("Seleccione una categoría válida.");

            if (db.Productos.Any(p => p.IdProducto != producto.IdProducto && p.Nombre == nombre))
                return ResultadoOperacion.Error($"Ya existe un producto llamado \"{nombre}\".");

            bool esAlta = producto.IdProducto == 0;
            var (fila, error) = ObtenerOCrear(db.Productos, producto.IdProducto, "El producto ya no existe.");
            if (fila is null)
                return error!;

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

            int ventas = db.VentaDetalles.Count(d => d.IdProducto == idProducto);
            if (ventas > 0)
                return ResultadoOperacion.Error(
                    $"No se puede eliminar: \"{producto.Nombre}\" aparece en {ventas} venta(s) registrada(s).");

            db.Productos.Remove(producto);
            db.SaveChanges();

            return ResultadoOperacion.Ok($"Producto \"{producto.Nombre}\" eliminado.");
        }

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

        public ResultadoOperacion GuardarIngrediente(Ingrediente ingrediente)
        {
            if (string.IsNullOrWhiteSpace(ingrediente.Nombre))
                return ResultadoOperacion.Error("El nombre del insumo es obligatorio.");

            if (string.IsNullOrWhiteSpace(ingrediente.UnidadMedida))
                return ResultadoOperacion.Error("Indique la unidad de medida (ml, g, unidades...).");

            if (ingrediente.Stock < 0 || ingrediente.StockMinimo < 0)
                return ResultadoOperacion.Error("El stock no puede ser negativo.");

            if (ingrediente.Stock > Limites.Cantidad || ingrediente.StockMinimo > Limites.Cantidad)
                return ResultadoOperacion.Error($"El stock no puede superar {Limites.Cantidad:N3}.");

            var largo = ControlarLargos(
                (ingrediente.Nombre, "El nombre del insumo", Limites.NombreInsumo),
                (ingrediente.UnidadMedida, "La unidad de medida", Limites.UnidadMedida));

            if (largo is not null)
                return largo;

            string nombre = ingrediente.Nombre.Trim();

            using var db = _fabrica.Crear();

            if (db.Ingredientes.Any(i => i.IdIngrediente != ingrediente.IdIngrediente && i.Nombre == nombre))
                return ResultadoOperacion.Error($"Ya existe un insumo llamado \"{nombre}\".");

            bool esAlta = ingrediente.IdIngrediente == 0;
            var (fila, error) = ObtenerOCrear(db.Ingredientes, ingrediente.IdIngrediente, "El insumo ya no existe.");
            if (fila is null)
                return error!;

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

        public ResultadoOperacion AjustarStock(bool esInsumo, int id, decimal cantidad)
        {
            if (cantidad == 0)
                return ResultadoOperacion.Error("Indicá cuánto entra o cuánto sale.");

            using var db = _fabrica.Crear();

            // // Leer, sumar y guardar tiene que ser atómico: si no, un ajuste y una venta simultáneos se pisarían.
            using var transaccion = db.Database.BeginTransaction();

            if (esInsumo)
                BloquearInsumo(db, id);
            else
                BloquearProducto(db, id);

            if (esInsumo)
            {
                var insumo = db.Ingredientes.FirstOrDefault(i => i.IdIngrediente == id);
                if (insumo is null)
                    return ResultadoOperacion.Error("El insumo ya no existe. Recargá la pantalla.");

                if (insumo.Stock + cantidad < 0)
                    return ResultadoOperacion.Error(
                        $"No se puede descontar {Math.Abs(cantidad):0.##}: " +
                        $"hay {insumo.Stock:0.##} {insumo.UnidadMedida} de \"{insumo.Nombre}\".");

                if (insumo.Stock + cantidad > Limites.Cantidad)
                    return ResultadoOperacion.Error(
                        $"No entra tanto: el stock de \"{insumo.Nombre}\" no puede superar " +
                        $"{Limites.Cantidad:N3} {insumo.UnidadMedida}.");

                insumo.Stock += cantidad;

                db.SaveChanges();
                transaccion.Commit();

                return ResultadoOperacion.Ok(
                    $"\"{insumo.Nombre}\": {(cantidad > 0 ? "+" : "")}{cantidad:0.##} {insumo.UnidadMedida}. " +
                    $"Quedan {insumo.Stock:0.##}.");
            }

            var producto = db.Productos.FirstOrDefault(p => p.IdProducto == id);
            if (producto is null)
                return ResultadoOperacion.Error("El producto ya no existe. Recargá la pantalla.");

            if (cantidad != decimal.Truncate(cantidad))
                return ResultadoOperacion.Error(
                    $"\"{producto.Nombre}\" se cuenta por unidades: la cantidad tiene que ser un número entero.");

            if (producto.Stock + cantidad < 0)
                return ResultadoOperacion.Error(
                    $"No se puede descontar {Math.Abs(cantidad):0.##}: " +
                    $"hay {producto.Stock} de \"{producto.Nombre}\".");

            if (producto.Stock + cantidad > Limites.StockDeProducto)
                return ResultadoOperacion.Error(
                    $"No entra tanto: el stock de \"{producto.Nombre}\" no puede superar " +
                    $"{Limites.StockDeProducto:N0} unidades.");

            producto.Stock += (int)cantidad;
            db.SaveChanges();
            transaccion.Commit();

            return ResultadoOperacion.Ok(
                $"\"{producto.Nombre}\": {(cantidad > 0 ? "+" : "")}{cantidad:0.##}. " +
                $"Quedan {producto.Stock}.");
        }

        public ResultadoOperacion GuardarCategoria(Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.NombreCategoria))
                return ResultadoOperacion.Error("El nombre de la categoría es obligatorio.");

            var largo = ControlarLargos(
                (categoria.NombreCategoria, "El nombre de la categoría", Limites.NombreParametrico));

            if (largo is not null)
                return largo;

            string nombre = categoria.NombreCategoria.Trim();

            using var db = _fabrica.Crear();

            if (db.Categorias.Any(c => c.IdCategoria != categoria.IdCategoria && c.NombreCategoria == nombre))
                return ResultadoOperacion.Error("Ya existe una categoría con ese nombre.");

            bool esAlta = categoria.IdCategoria == 0;
            var (fila, error) = ObtenerOCrear(db.Categorias, categoria.IdCategoria, "La categoría ya no existe.");
            if (fila is null)
                return error!;

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
    }
}
