using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.Services
{
    /// <summary>
    /// RF-14. Genera el archivo del informe y deja constancia en la tabla Reporte.
    ///
    /// El archivo se escribe de verdad en disco porque el requerimiento pide guardar
    /// "la ruta física del archivo generado": una ruta que no apunta a nada no sirve
    /// para auditar. Se usa CSV con punto y coma, que es lo que abre Excel en es-AR.
    /// </summary>
    public class ServicioReportes
    {
        private readonly IRepositorioBar _repositorio;

        public ServicioReportes(IRepositorioBar repositorio)
        {
            _repositorio = repositorio;
        }

        /// <summary>Carpeta donde quedan los informes. Se crea sola la primera vez.</summary>
        public static string CarpetaDestino => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SistemaGestionBar", "Reportes");

        public ResultadoOperacion Generar(TipoReporte tipo, Usuario generador)
        {
            try
            {
                Directory.CreateDirectory(CarpetaDestino);

                var filas = tipo switch
                {
                    TipoReporte.VentasPorPeriodo => VentasPorPeriodo(),
                    TipoReporte.ProductosMasVendidos => ProductosMasVendidos(),
                    TipoReporte.StockCritico => StockCritico(),
                    TipoReporte.CierreDeCaja => CierreDeCaja(),
                    _ => new List<string[]>()
                };

                if (filas.Count <= 1)
                    return ResultadoOperacion.Error("No hay datos para ese informe todavía.");

                string archivo = $"{tipo}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string ruta = Path.Combine(CarpetaDestino, archivo);

                var contenido = new StringBuilder();
                foreach (var fila in filas)
                    contenido.AppendLine(string.Join(";", fila.Select(Escapar)));

                // UTF-8 con BOM: sin el BOM, Excel abre los acentos rotos.
                File.WriteAllText(ruta, contenido.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                var reporte = new Reporte
                {
                    TipoReporte = tipo,
                    IdUsuarioGenerador = generador.IdUsuario,
                    RutaArchivo = ruta
                };

                var registro = _repositorio.RegistrarReporte(reporte);
                if (!registro.Exito)
                    return registro;

                return ResultadoOperacion.Ok($"Informe generado en {ruta}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ResultadoOperacion.Error($"No se pudo escribir el archivo: {ex.Message}");
            }
        }

        private static string Escapar(string valor)
        {
            valor ??= string.Empty;
            return valor.Contains(';') || valor.Contains('"')
                ? $"\"{valor.Replace("\"", "\"\"")}\""
                : valor;
        }

        private static string Numero(decimal valor) => valor.ToString("0.##", CultureInfo.CurrentCulture);

        private List<string[]> VentasPorPeriodo()
        {
            var filas = new List<string[]>
            {
                new[] { "Venta", "Fecha", "Modalidad", "Ubicacion", "Cajero", "Mesero", "Cliente", "Metodo de pago", "Total" }
            };

            foreach (var v in _repositorio.ObtenerVentas())
            {
                filas.Add(new[]
                {
                    v.IdVenta.ToString(),
                    v.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    v.ModalidadConsumo.ToString(),
                    v.Ubicacion?.NombreUbicacion ?? "",
                    v.Cajero?.NombreCompleto ?? "",
                    v.Mesero?.NombreCompleto ?? "",
                    v.Cliente?.NombreMostrado ?? "",
                    v.MetodoPago?.NombreMetodo ?? "",
                    Numero(v.Total)
                });
            }

            return filas;
        }

        private List<string[]> ProductosMasVendidos()
        {
            var filas = new List<string[]>
            {
                new[] { "Producto", "Unidades vendidas", "Facturado" }
            };

            var ranking = _repositorio.ObtenerVentas()
                .SelectMany(v => v.Detalles)
                .GroupBy(d => d.IdProducto)
                .Select(g => new
                {
                    Nombre = g.First().Producto?.Nombre ?? $"#{g.Key}",
                    Unidades = g.Sum(d => d.Cantidad),
                    Facturado = g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(x => x.Unidades);

            foreach (var item in ranking)
                filas.Add(new[] { item.Nombre, item.Unidades.ToString(), Numero(item.Facturado) });

            return filas;
        }

        private List<string[]> StockCritico()
        {
            var filas = new List<string[]>
            {
                new[] { "Item", "Stock actual", "Stock minimo", "Unidad" }
            };

            foreach (var a in _repositorio.ObtenerAlertasStock())
                filas.Add(new[] { a.Nombre, Numero(a.Stock), Numero(a.StockMinimo), a.UnidadMedida });

            return filas;
        }

        private List<string[]> CierreDeCaja()
        {
            var filas = new List<string[]>
            {
                new[] { "Metodo de pago", "Operaciones", "Total cobrado" }
            };

            var delDia = _repositorio.ObtenerVentas()
                .Where(v => v.EstadoVenta == EstadoVenta.Confirmada && v.FechaHora.Date == DateTime.Today)
                .GroupBy(v => v.MetodoPago?.NombreMetodo ?? "Sin método")
                .OrderBy(g => g.Key);

            decimal total = 0;
            foreach (var grupo in delDia)
            {
                decimal subtotal = grupo.Sum(v => v.Total);
                total += subtotal;
                filas.Add(new[] { grupo.Key, grupo.Count().ToString(), Numero(subtotal) });
            }

            if (filas.Count > 1)
                filas.Add(new[] { "TOTAL", "", Numero(total) });

            return filas;
        }
    }
}
