using System;
using System.Collections.Generic;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Reportes
{
    public class ArmadorDeReportes
    {
        private readonly IRepositorioBar _repositorio;

        public ArmadorDeReportes(IRepositorioBar repositorio)
        {
            _repositorio = repositorio;
        }

        public ContenidoDeReporte Armar(TipoDeReporte tipo, PeriodoElegido periodo, string generadoPor) => tipo switch
        {
            TipoDeReporte.Ventas => Ventas(periodo, generadoPor),
            TipoDeReporte.ProductosVendidos => ProductosVendidos(periodo, generadoPor),
            TipoDeReporte.Catalogo => Catalogo(generadoPor),
            TipoDeReporte.Inventario => Inventario(generadoPor),
            TipoDeReporte.Empleados => Empleados(periodo, generadoPor),
            TipoDeReporte.VentasPorCategoria => VentasPorCategoria(periodo, generadoPor),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };

        private ContenidoDeReporte Ventas(PeriodoElegido periodo, string generadoPor)
        {
            var ventas = VentasDelPeriodo(periodo);
            var bloques = new List<BloqueDeReporte>();

            decimal total = ventas.Sum(v => v.Total);
            int unidades = ventas.SelectMany(v => v.Detalles).Sum(d => d.Cantidad);

            bloques.Add(new BloqueIndicadores("Resumen", new[]
            {
                new ValorIndicador("Ventas confirmadas", ventas.Count.ToString()),
                new ValorIndicador("Total facturado", Moneda(total)),
                new ValorIndicador("Ticket promedio", Moneda(ventas.Count == 0 ? 0 : total / ventas.Count)),
                new ValorIndicador("Unidades vendidas", unidades.ToString())
            }));

            if (ventas.Count == 0)
            {
                bloques.Add(new BloqueTexto("Detalle", "No hubo ventas confirmadas en el período elegido."));
                return Nuevo("Reporte de ventas", periodo, generadoPor, bloques);
            }

            bloques.Add(GraficoDeImporte("Por método de pago", TipoDeGrafico.Barras,
                ventas.GroupBy(v => v.MetodoPago?.NombreMetodo ?? "—")));

            bloques.Add(Agrupado("Por método de pago", "Método",
                ventas.GroupBy(v => v.MetodoPago?.NombreMetodo ?? "—"), total));

            bloques.Add(GraficoDeImporte("Por modalidad de consumo", TipoDeGrafico.Circular,
                ventas.GroupBy(v => v.ModalidadConsumo == ModalidadConsumo.Local ? "En el local" : "Para llevar")));

            bloques.Add(Agrupado("Por modalidad de consumo", "Modalidad",
                ventas.GroupBy(v => v.ModalidadConsumo == ModalidadConsumo.Local ? "En el local" : "Para llevar"),
                total));

            bloques.Add(new BloqueTabla("Detalle de ventas",
                new[]
                {
                    new ColumnaDeReporte("N°", 0.7f),
                    new ColumnaDeReporte("Fecha", 1.6f),
                    new ColumnaDeReporte("Dónde", 1.5f),
                    new ColumnaDeReporte("Quién atendió", 2f),
                    new ColumnaDeReporte("Cliente", 2f),
                    new ColumnaDeReporte("Total", 1.2f, ALaDerecha: true)
                },
                ventas.OrderBy(v => v.IdVenta)
                      .Select(v => new FilaDeReporte(new[]
                      {
                          "#" + v.IdVenta,
                          v.FechaHora.ToString("dd/MM/yy HH:mm"),
                          v.Ubicacion?.NombreUbicacion ?? "Para llevar",
                          v.Cajero?.NombreCompleto ?? "—",
                          v.Cliente?.NombreMostrado ?? "—",
                          Moneda(v.Total)
                      }))
                      .Append(new FilaDeReporte(new[] { "", "", "", "", "TOTAL", Moneda(total) }, EsTotal: true))
                      .ToList()));

            return Nuevo("Reporte de ventas", periodo, generadoPor, bloques);
        }

        private static BloqueTabla Agrupado(string titulo, string encabezado,
                                            IEnumerable<IGrouping<string, Venta>> grupos, decimal total)
        {
            var filas = grupos
                .Select(g => new { Clave = g.Key, Cantidad = g.Count(), Importe = g.Sum(v => v.Total) })
                .OrderByDescending(g => g.Importe)
                .Select(g => new FilaDeReporte(new[]
                {
                    g.Clave,
                    g.Cantidad.ToString(),
                    Moneda(g.Importe),
                    Porcentaje(g.Importe, total)
                }))
                .ToList();

            return new BloqueTabla(titulo,
                new[]
                {
                    new ColumnaDeReporte(encabezado, 3f),
                    new ColumnaDeReporte("Ventas", 1f, ALaDerecha: true),
                    new ColumnaDeReporte("Importe", 1.5f, ALaDerecha: true),
                    new ColumnaDeReporte("%", 1f, ALaDerecha: true)
                },
                filas);
        }

        private static BloqueGrafico GraficoDeImporte(string titulo, TipoDeGrafico tipo,
                                                       IEnumerable<IGrouping<string, Venta>> grupos) =>
            new(titulo, tipo, grupos
                .Select(g => new { Clave = g.Key, Importe = g.Sum(v => v.Total) })
                .OrderByDescending(g => g.Importe)
                .Select(g => new SerieDeGrafico(g.Clave, g.Importe, Moneda(g.Importe)))
                .ToList());

        private ContenidoDeReporte ProductosVendidos(PeriodoElegido periodo, string generadoPor)
        {
            var ventas = VentasDelPeriodo(periodo);
            var catalogo = _repositorio.ObtenerProductos().ToDictionary(p => p.IdProducto);
            var bloques = new List<BloqueDeReporte>();

            var renglones = ventas.SelectMany(v => v.Detalles).ToList();
            decimal importeTotal = renglones.Sum(d => d.Subtotal);

            if (renglones.Count == 0)
            {
                bloques.Add(new BloqueIndicadores("Resumen", new[]
                {
                    new ValorIndicador("Unidades vendidas", "0"),
                    new ValorIndicador("Productos distintos", "0"),
                    new ValorIndicador("Importe total", Moneda(0))
                }));
                bloques.Add(new BloqueTexto("Ranking", "No se vendió ningún producto en el período elegido."));
                return Nuevo("Productos más vendidos", periodo, generadoPor, bloques);
            }

            var ranking = renglones
                .GroupBy(d => d.IdProducto)
                .Select(g => new
                {
                    IdProducto = g.Key,
                    Nombre = g.First().Producto?.Nombre ?? "—",
                    Unidades = g.Sum(d => d.Cantidad),
                    Importe = g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(g => g.Unidades)
                .ThenByDescending(g => g.Importe)
                .ToList();

            var lider = ranking[0];

            bloques.Add(new BloqueIndicadores("Resumen", new[]
            {
                new ValorIndicador("Unidades vendidas", renglones.Sum(d => d.Cantidad).ToString()),
                new ValorIndicador("Productos distintos", renglones.Select(d => d.IdProducto).Distinct().Count().ToString()),
                new ValorIndicador("Importe total", Moneda(importeTotal)),
                new ValorIndicador("Más vendido", $"{lider.Nombre} ({lider.Unidades} u.)")
            }));

            bloques.Add(new BloqueGrafico("Ranking por unidades vendidas", TipoDeGrafico.Barras,
                ranking.Take(8)
                       .Select(r => new SerieDeGrafico(r.Nombre, r.Unidades, r.Unidades + " u."))
                       .ToList()));

            bloques.Add(new BloqueTabla("Ranking",
                new[]
                {
                    new ColumnaDeReporte("#", 0.5f),
                    new ColumnaDeReporte("Producto", 3f),
                    new ColumnaDeReporte("Categoría", 1.8f),
                    new ColumnaDeReporte("Unidades", 1.1f, ALaDerecha: true),
                    new ColumnaDeReporte("Importe", 1.5f, ALaDerecha: true),
                    new ColumnaDeReporte("%", 1f, ALaDerecha: true)
                },
                ranking.Select((r, i) => new FilaDeReporte(new[]
                {
                    (i + 1).ToString(),
                    r.Nombre,
                    catalogo.TryGetValue(r.IdProducto, out var p) ? p.Categoria?.NombreCategoria ?? "—" : "—",
                    r.Unidades.ToString(),
                    Moneda(r.Importe),
                    Porcentaje(r.Importe, importeTotal)
                }))
                .Append(new FilaDeReporte(new[]
                {
                    "", "TOTAL", "",
                    ranking.Sum(r => r.Unidades).ToString(),
                    Moneda(importeTotal),
                    "100 %"
                }, EsTotal: true))
                .ToList()));

            return Nuevo("Productos más vendidos", periodo, generadoPor, bloques);
        }

        private ContenidoDeReporte Catalogo(string generadoPor)
        {
            var productos = _repositorio.ObtenerProductos();

            var bloques = new List<BloqueDeReporte>
            {
                new BloqueIndicadores("Resumen", new[]
                {
                    new ValorIndicador("Productos en catálogo", productos.Count.ToString()),
                    new ValorIndicador("Se preparan con receta", productos.Count(p => p.TieneReceta).ToString()),
                    new ValorIndicador("De venta directa", productos.Count(p => !p.TieneReceta).ToString()),
                    new ValorIndicador("Categorías", productos.Select(p => p.IdCategoria).Distinct().Count().ToString())
                }),

                new BloqueTabla("Catálogo completo",
                    new[]
                    {
                        new ColumnaDeReporte("Producto", 3f),
                        new ColumnaDeReporte("Categoría", 1.8f),
                        new ColumnaDeReporte("Precio", 1.3f, ALaDerecha: true),
                        new ColumnaDeReporte("Stock", 1f, ALaDerecha: true),
                        new ColumnaDeReporte("Mínimo", 1f, ALaDerecha: true),
                        new ColumnaDeReporte("Se descuenta de", 2f)
                    },
                    productos.OrderBy(p => p.Categoria?.NombreCategoria)
                             .ThenBy(p => p.Nombre)
                             .Select(p => new FilaDeReporte(new[]
                             {
                                 p.Nombre,
                                 p.Categoria?.NombreCategoria ?? "—",
                                 Moneda(p.Precio),
                                 p.TieneReceta ? "—" : p.Stock.ToString(),
                                 p.TieneReceta ? "—" : p.StockMinimo.ToString(),
                                 p.TieneReceta ? "Insumos de la receta" : "Su propio stock"
                             }))
                             .ToList())
            };

            return Nuevo("Catálogo de productos", PeriodoDeReporte.Todo, generadoPor, bloques);
        }

        private ContenidoDeReporte Inventario(string generadoPor)
        {
            var insumos = _repositorio.ObtenerIngredientes();
            var directos = _repositorio.ObtenerProductos().Where(p => !p.TieneReceta).ToList();

            var bloques = new List<BloqueDeReporte>
            {
                new BloqueIndicadores("Resumen", new[]
                {
                    new ValorIndicador("Insumos en inventario", insumos.Count.ToString()),
                    new ValorIndicador("Insumos a reponer", insumos.Count(i => i.StockBajo).ToString()),
                    new ValorIndicador("Productos de venta directa", directos.Count.ToString()),
                    new ValorIndicador("Productos a reponer", directos.Count(p => p.StockBajo).ToString())
                }),

                new BloqueTabla("Insumos",
                    new[]
                    {
                        new ColumnaDeReporte("Insumo", 3f),
                        new ColumnaDeReporte("Unidad", 1.2f),
                        new ColumnaDeReporte("Stock", 1.3f, ALaDerecha: true),
                        new ColumnaDeReporte("Mínimo", 1.3f, ALaDerecha: true),
                        new ColumnaDeReporte("Falta", 1.3f, ALaDerecha: true),
                        new ColumnaDeReporte("Estado", 1.5f)
                    },
                    insumos.OrderByDescending(i => i.StockBajo)
                           .ThenBy(i => i.Nombre)
                           .Select(i => new FilaDeReporte(new[]
                           {
                               i.Nombre,
                               i.UnidadMedida,
                               Cantidad(i.Stock),
                               Cantidad(i.StockMinimo),
                               i.StockBajo ? Cantidad(i.StockMinimo - i.Stock) : "—",
                               i.StockBajo ? "REPONER" : "En nivel"
                           }))
                           .ToList())
            };

            var aReponer = directos.Where(p => p.StockBajo).ToList();

            if (aReponer.Count == 0)
            {
                bloques.Add(new BloqueTexto("Productos de venta directa",
                    "Ningún producto de venta directa llegó a su punto de reposición."));
            }
            else
            {
                bloques.Add(new BloqueTabla("Productos de venta directa a reponer",
                    new[]
                    {
                        new ColumnaDeReporte("Producto", 3f),
                        new ColumnaDeReporte("Categoría", 2f),
                        new ColumnaDeReporte("Stock", 1.2f, ALaDerecha: true),
                        new ColumnaDeReporte("Mínimo", 1.2f, ALaDerecha: true),
                        new ColumnaDeReporte("Falta", 1.2f, ALaDerecha: true)
                    },
                    aReponer.OrderBy(p => p.Stock)
                            .Select(p => new FilaDeReporte(new[]
                            {
                                p.Nombre,
                                p.Categoria?.NombreCategoria ?? "—",
                                p.Stock.ToString(),
                                p.StockMinimo.ToString(),
                                (p.StockMinimo - p.Stock).ToString()
                            }))
                            .ToList()));
            }

            return Nuevo("Inventario y stock", PeriodoDeReporte.Todo, generadoPor, bloques);
        }

        private ContenidoDeReporte Empleados(PeriodoElegido periodo, string generadoPor)
        {
            var ventas = VentasDelPeriodo(periodo);
            var bloques = new List<BloqueDeReporte>();

            var empleados = _repositorio.ObtenerUsuarios().ToDictionary(u => u.IdUsuario);

            decimal total = ventas.Sum(v => v.Total);

            bloques.Add(new BloqueIndicadores("Resumen", new[]
            {
                new ValorIndicador("Ventas del período", ventas.Count.ToString()),
                new ValorIndicador("Total facturado", Moneda(total)),
                new ValorIndicador("Cajeros con ventas",
                    ventas.Select(v => v.IdCajero).Distinct().Count().ToString())
            }));

            if (ventas.Count == 0)
            {
                bloques.Add(new BloqueTexto("Detalle", "No hubo ventas confirmadas en el período elegido."));
                return Nuevo("Ventas por empleado", periodo, generadoPor, bloques);
            }

            var porCajero = ventas.GroupBy(v => v.IdCajero)
                      .Select(g => new
                      {
                          Nombre = Nombrar(empleados, g.Key),
                          Rol = empleados.TryGetValue(g.Key, out var u) ? u.NombreRol : "—",
                          Cantidad = g.Count(),
                          Importe = g.Sum(v => v.Total)
                      })
                      .OrderByDescending(g => g.Importe)
                      .ToList();

            bloques.Add(new BloqueGrafico("Cobrado por cajero", TipoDeGrafico.Barras,
                porCajero.Select(g => new SerieDeGrafico(g.Nombre, g.Importe, Moneda(g.Importe))).ToList()));

            bloques.Add(new BloqueTabla("Cobrado por cajero",
                new[]
                {
                    new ColumnaDeReporte("Cajero", 3f),
                    new ColumnaDeReporte("Rol", 1.8f),
                    new ColumnaDeReporte("Ventas", 1.1f, ALaDerecha: true),
                    new ColumnaDeReporte("Total cobrado", 1.6f, ALaDerecha: true),
                    new ColumnaDeReporte("%", 1f, ALaDerecha: true)
                },
                porCajero.Select(g => new FilaDeReporte(new[]
                      {
                          g.Nombre, g.Rol, g.Cantidad.ToString(), Moneda(g.Importe), Porcentaje(g.Importe, total)
                      }))
                      .ToList()));

            var conMesero = ventas.Where(v => v.IdMesero is not null).ToList();

            if (conMesero.Count == 0)
            {
                bloques.Add(new BloqueTexto("Atendido por mesero",
                    "Ninguna venta del período se registró con mesero: solo hubo barra y para llevar."));
            }
            else
            {
                bloques.Add(new BloqueTabla("Atendido por mesero",
                    new[]
                    {
                        new ColumnaDeReporte("Mesero", 3f),
                        new ColumnaDeReporte("Mesas atendidas", 1.6f, ALaDerecha: true),
                        new ColumnaDeReporte("Importe", 1.6f, ALaDerecha: true)
                    },
                    conMesero.GroupBy(v => v.IdMesero!.Value)
                             .Select(g => new { Nombre = Nombrar(empleados, g.Key), Cantidad = g.Count(), Importe = g.Sum(v => v.Total) })
                             .OrderByDescending(g => g.Importe)
                             .Select(g => new FilaDeReporte(new[]
                             {
                                 g.Nombre, g.Cantidad.ToString(), Moneda(g.Importe)
                             }))
                             .ToList()));
            }

            return Nuevo("Ventas por empleado", periodo, generadoPor, bloques);
        }

        private ContenidoDeReporte VentasPorCategoria(PeriodoElegido periodo, string generadoPor)
        {
            var ventas = VentasDelPeriodo(periodo);
            var catalogo = _repositorio.ObtenerProductos().ToDictionary(p => p.IdProducto);
            var bloques = new List<BloqueDeReporte>();

            var renglones = ventas.SelectMany(v => v.Detalles).ToList();
            decimal importeTotal = renglones.Sum(d => d.Subtotal);

            if (renglones.Count == 0)
            {
                bloques.Add(new BloqueIndicadores("Resumen", new[]
                {
                    new ValorIndicador("Importe total", Moneda(0)),
                    new ValorIndicador("Categorías con ventas", "0")
                }));
                bloques.Add(new BloqueTexto("Detalle", "No se vendió ningún producto en el período elegido."));
                return Nuevo("Ventas por categoría", periodo, generadoPor, bloques);
            }

            var porCategoria = renglones
                .GroupBy(d => catalogo.TryGetValue(d.IdProducto, out var p) ? p.Categoria?.NombreCategoria ?? "Sin categoría" : "Sin categoría")
                .Select(g => new { Categoria = g.Key, Unidades = g.Sum(d => d.Cantidad), Importe = g.Sum(d => d.Subtotal) })
                .OrderByDescending(g => g.Importe)
                .ToList();

            var lider = porCategoria[0];

            bloques.Add(new BloqueIndicadores("Resumen", new[]
            {
                new ValorIndicador("Importe total", Moneda(importeTotal)),
                new ValorIndicador("Categorías con ventas", porCategoria.Count.ToString()),
                new ValorIndicador("Categoría líder", $"{lider.Categoria} ({Porcentaje(lider.Importe, importeTotal)})")
            }));

            bloques.Add(new BloqueGrafico("Importe por categoría", TipoDeGrafico.Circular,
                porCategoria.Select(c => new SerieDeGrafico(c.Categoria, c.Importe, Moneda(c.Importe))).ToList()));

            bloques.Add(new BloqueTabla("Detalle por categoría",
                new[]
                {
                    new ColumnaDeReporte("Categoría", 3f),
                    new ColumnaDeReporte("Unidades", 1.2f, ALaDerecha: true),
                    new ColumnaDeReporte("Importe", 1.5f, ALaDerecha: true),
                    new ColumnaDeReporte("%", 1f, ALaDerecha: true)
                },
                porCategoria.Select(c => new FilaDeReporte(new[]
                      {
                          c.Categoria, c.Unidades.ToString(), Moneda(c.Importe), Porcentaje(c.Importe, importeTotal)
                      }))
                      .Append(new FilaDeReporte(new[]
                      {
                          "TOTAL", porCategoria.Sum(c => c.Unidades).ToString(), Moneda(importeTotal), "100 %"
                      }, EsTotal: true))
                      .ToList()));

            return Nuevo("Ventas por categoría", periodo, generadoPor, bloques);
        }

        private List<Venta> VentasDelPeriodo(PeriodoElegido periodo) =>
            VentasConfirmadas(_repositorio, periodo);

        public static List<Venta> VentasConfirmadas(IRepositorioBar repositorio, PeriodoElegido periodo)
        {
            var desde = periodo.Desde;
            var hasta = periodo.HastaExclusivo;

            return repositorio.ObtenerVentas()
                              .Where(v => v.EstadoVenta == EstadoVenta.Confirmada)
                              .Where(v => desde is null || v.FechaHora >= desde)
                              .Where(v => hasta is null || v.FechaHora < hasta)
                              .ToList();
        }

        public static string DescribirPeriodo(PeriodoElegido periodo) => periodo.Tipo switch
        {
            PeriodoDeReporte.Hoy => $"Día {DateTime.Today:dd/MM/yyyy}",
            PeriodoDeReporte.UltimaSemana => $"Del {DateTime.Today.AddDays(-7):dd/MM/yyyy} al {DateTime.Today:dd/MM/yyyy}",
            PeriodoDeReporte.UltimoMes => $"Del {DateTime.Today.AddMonths(-1):dd/MM/yyyy} al {DateTime.Today:dd/MM/yyyy}",
            PeriodoDeReporte.Personalizado when periodo.Desde is DateTime d && periodo.HastaExclusivo is DateTime h =>
                d.Date == h.AddDays(-1).Date ? $"Día {d:dd/MM/yyyy}" : $"Del {d:dd/MM/yyyy} al {h.AddDays(-1):dd/MM/yyyy}",
            _ => "Todo el historial"
        };

        private static ContenidoDeReporte Nuevo(string titulo, PeriodoElegido periodo,
                                                string generadoPor, List<BloqueDeReporte> bloques) =>
            new(titulo, DescribirPeriodo(periodo), generadoPor, DateTime.Now, bloques);

        private static string Nombrar(Dictionary<int, Usuario> empleados, int id) =>
            empleados.TryGetValue(id, out var u) ? u.NombreCompleto : "Usuario #" + id;

        private static string Moneda(decimal valor) => valor.ToString("C0");

        private static string Cantidad(decimal valor) => valor.ToString("0.##");

        private static string Porcentaje(decimal parte, decimal total) =>
            total == 0 ? "—" : (parte / total).ToString("P1");
    }

    public enum TipoDeReporte
    {
        Ventas,
        ProductosVendidos,
        Catalogo,
        Inventario,
        Empleados,
        VentasPorCategoria
    }

    public enum PeriodoDeReporte
    {
        Hoy,
        UltimaSemana,
        UltimoMes,
        Todo,
        Personalizado
    }

    /// <summary>Período elegido; si es Personalizado lleva las dos fechas (ambas inclusivas).</summary>
    public readonly record struct PeriodoElegido(PeriodoDeReporte Tipo, DateTime? DesdeElegido = null, DateTime? HastaElegido = null)
    {
        public static implicit operator PeriodoElegido(PeriodoDeReporte tipo) => new(tipo);

        public DateTime? Desde => Tipo switch
        {
            PeriodoDeReporte.Hoy => DateTime.Today,
            PeriodoDeReporte.UltimaSemana => DateTime.Today.AddDays(-7),
            PeriodoDeReporte.UltimoMes => DateTime.Today.AddMonths(-1),
            PeriodoDeReporte.Personalizado => DesdeElegido?.Date,
            _ => null
        };

        /// <summary>Límite superior exclusivo: la medianoche posterior a la última fecha elegida.</summary>
        public DateTime? HastaExclusivo =>
            Tipo == PeriodoDeReporte.Personalizado ? HastaElegido?.Date.AddDays(1) : null;
    }
}
