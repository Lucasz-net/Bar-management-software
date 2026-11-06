using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestionBar.Reportes
{
    public class DocumentoDeReporte : IDocument
    {
        private const string Acento = "#1565C0";
        private const string Tinta = "#212121";
        private const string Gris = "#616161";
        private const string Linea = "#E0E0E0";
        private const string Fondo = "#F5F7FA";

        private readonly ContenidoDeReporte _contenido;

        public DocumentoDeReporte(ContenidoDeReporte contenido)
        {
            _contenido = contenido;
        }

        public void Compose(IDocumentContainer contenedor)
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(28);
                pagina.DefaultTextStyle(t => t.FontSize(9).FontColor(Tinta));

                pagina.Header().Element(Encabezado);
                pagina.Content().PaddingTop(14).Element(Cuerpo);
                pagina.Footer().Element(Pie);
            });
        }

        private void Encabezado(IContainer contenedor)
        {
            contenedor.BorderBottom(2).BorderColor(Acento).PaddingBottom(8).Row(fila =>
            {
                fila.RelativeItem().Column(columna =>
                {
                    columna.Item().Text("SISTEMA DE GESTIÓN DE BAR")
                           .FontSize(8).FontColor(Gris).LetterSpacing(0.12f);

                    columna.Item().PaddingTop(2).Text(_contenido.Titulo)
                           .FontSize(17).Bold().FontColor(Acento);

                    columna.Item().Text(_contenido.Subtitulo).FontSize(9).FontColor(Gris);
                });

                fila.ConstantItem(170).AlignRight().Column(columna =>
                {
                    columna.Item().AlignRight().Text("Generado por").FontSize(7).FontColor(Gris);
                    columna.Item().AlignRight().Text(_contenido.GeneradoPor).FontSize(9).Bold();
                    columna.Item().PaddingTop(3).AlignRight()
                           .Text(_contenido.GeneradoEl.ToString("dd/MM/yyyy HH:mm"))
                           .FontSize(8).FontColor(Gris);
                });
            });
        }

        private void Cuerpo(IContainer contenedor)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(16);

                foreach (var bloque in _contenido.Bloques)
                {
                    columna.Item().Column(caja =>
                    {
                        caja.Item().PaddingBottom(5).Text(bloque.Titulo)
                            .FontSize(11).Bold().FontColor(Tinta);

                        switch (bloque)
                        {
                            case BloqueIndicadores indicadores:
                                caja.Item().Element(c => Indicadores(c, indicadores));
                                break;

                            case BloqueTabla tabla:
                                caja.Item().Element(c => Tabla(c, tabla));
                                break;

                            case BloqueTexto texto:
                                caja.Item().Text(texto.Texto).FontSize(9).FontColor(Gris).Italic();
                                break;

                            case BloqueGrafico grafico:
                                caja.Item().Element(c => Grafico(c, grafico));
                                break;
                        }
                    });
                }
            });
        }

        private static void Indicadores(IContainer contenedor, BloqueIndicadores bloque)
        {
            contenedor.Row(fila =>
            {
                fila.Spacing(8);

                foreach (var valor in bloque.Valores)
                {
                    fila.RelativeItem()
                        .Background(Fondo)
                        .Border(1).BorderColor(Linea)
                        .Padding(8)
                        .Column(columna =>
                        {
                            columna.Item().Text(valor.Etiqueta).FontSize(7.5f).FontColor(Gris);
                            columna.Item().PaddingTop(2).Text(valor.Valor)
                                   .FontSize(14).Bold().FontColor(Acento);
                        });
                }
            });
        }

        private static void Tabla(IContainer contenedor, BloqueTabla bloque)
        {
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(definicion =>
                {
                    foreach (var columna in bloque.Columnas)
                        definicion.RelativeColumn(columna.Peso);
                });

                tabla.Header(encabezado =>
                {
                    foreach (var columna in bloque.Columnas)
                    {
                        var celda = encabezado.Cell()
                                              .Background(Acento)
                                              .PaddingVertical(5).PaddingHorizontal(5);

                        (columna.ALaDerecha ? celda.AlignRight() : celda)
                            .Text(columna.Encabezado).FontSize(8).Bold().FontColor(Colors.White);
                    }
                });

                int numeroDeFila = 0;

                foreach (var fila in bloque.Filas)
                {
                    string fondo = fila.EsTotal ? Fondo : numeroDeFila % 2 == 0 ? Colors.White : "#FAFAFA";
                    numeroDeFila++;

                    for (int i = 0; i < bloque.Columnas.Count; i++)
                    {
                        var columna = bloque.Columnas[i];
                        string texto = i < fila.Celdas.Count ? fila.Celdas[i] : string.Empty;

                        var celda = tabla.Cell()
                                         .Background(fondo)
                                         .BorderBottom(fila.EsTotal ? 0 : 1).BorderColor(Linea)
                                         .BorderTop(fila.EsTotal ? 1.5f : 0).BorderColor(Linea)
                                         .PaddingVertical(4).PaddingHorizontal(5);

                        var renglon = (columna.ALaDerecha ? celda.AlignRight() : celda).Text(texto).FontSize(8.5f);

                        if (fila.EsTotal)
                            renglon.Bold();
                    }
                }
            });
        }

        private static void Grafico(IContainer contenedor, BloqueGrafico bloque)
        {
            if (bloque.Tipo == TipoDeGrafico.Circular)
                Circular(contenedor, bloque);
            else
                Barras(contenedor, bloque);
        }

        private static void Barras(IContainer contenedor, BloqueGrafico bloque)
        {
            decimal maximo = bloque.Series.Count == 0 ? 0 : bloque.Series.Max(s => s.Valor);

            contenedor.Column(columna =>
            {
                columna.Spacing(7);

                for (int i = 0; i < bloque.Series.Count; i++)
                {
                    var serie = bloque.Series[i];
                    float proporcion = maximo <= 0 ? 0f : (float)(serie.Valor / maximo);
                    string color = PaletaDeGrafico.Color(i);

                    columna.Item().Row(fila =>
                    {
                        fila.ConstantItem(120).AlignMiddle().Text(serie.Etiqueta).FontSize(8.5f).FontColor(Tinta);

                        fila.RelativeItem().PaddingHorizontal(8).AlignMiddle().Row(pista =>
                        {
                            pista.RelativeItem(Math.Max(proporcion, 0.001f))
                                 .Height(13).Background(color);

                            pista.RelativeItem(Math.Max(1f - proporcion, 0.001f))
                                 .Height(13).Background(Fondo);
                        });

                        fila.ConstantItem(75).AlignMiddle().AlignRight()
                            .Text(serie.ValorTexto).FontSize(8.5f).Bold().FontColor(Acento);
                    });
                }
            });
        }

        private static void Circular(IContainer contenedor, BloqueGrafico bloque)
        {
            decimal total = bloque.Series.Sum(s => s.Valor);

            contenedor.Row(fila =>
            {
                fila.ConstantItem(110).Height(110).Svg(ArmarSvgDeTorta(bloque.Series, total));

                fila.RelativeItem().PaddingLeft(16).Column(leyenda =>
                {
                    leyenda.Spacing(5);

                    for (int i = 0; i < bloque.Series.Count; i++)
                    {
                        var serie = bloque.Series[i];
                        string porcentaje = total <= 0 ? string.Empty : $" ({(double)(serie.Valor / total):P0})";

                        leyenda.Item().Row(linea =>
                        {
                            linea.ConstantItem(9).Height(9).Background(PaletaDeGrafico.Color(i));
                            linea.RelativeItem().PaddingLeft(6)
                                 .Text($"{serie.Etiqueta}{porcentaje} · {serie.ValorTexto}").FontSize(8.5f).FontColor(Tinta);
                        });
                    }
                });
            });
        }

        private static string ArmarSvgDeTorta(IReadOnlyList<SerieDeGrafico> series, decimal total)
        {
            const double cx = 50, cy = 50, radio = 48;

            var sb = new StringBuilder();
            sb.Append("<svg viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\">");

            if (total <= 0 || series.Count == 0)
            {
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{radio}\" fill=\"#E0E0E0\" />");
            }
            else
            {
                double angulo = -90;

                for (int i = 0; i < series.Count; i++)
                {
                    double proporcion = (double)(series[i].Valor / total);

                    if (series.Count == 1 || proporcion >= 0.9995)
                    {
                        sb.Append(CultureInfo.InvariantCulture,
                            $"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{radio}\" fill=\"{PaletaDeGrafico.Color(i)}\" />");
                        break;
                    }

                    double siguiente = angulo + proporcion * 360.0;
                    var (x1, y1) = PuntoEnCirculo(cx, cy, radio, angulo);
                    var (x2, y2) = PuntoEnCirculo(cx, cy, radio, siguiente);
                    int arcoGrande = (siguiente - angulo) > 180 ? 1 : 0;

                    sb.Append(CultureInfo.InvariantCulture,
                        $"<path d=\"M{cx},{cy} L{x1:F2},{y1:F2} A{radio},{radio} 0 {arcoGrande} 1 {x2:F2},{y2:F2} Z\" fill=\"{PaletaDeGrafico.Color(i)}\" />");

                    angulo = siguiente;
                }
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static (double X, double Y) PuntoEnCirculo(double cx, double cy, double radio, double grados)
        {
            double radianes = grados * Math.PI / 180.0;
            return (cx + radio * Math.Cos(radianes), cy + radio * Math.Sin(radianes));
        }

        private void Pie(IContainer contenedor)
        {
            contenedor.BorderTop(1).BorderColor(Linea).PaddingTop(5).Row(fila =>
            {
                fila.RelativeItem().Text(_contenido.Titulo).FontSize(7.5f).FontColor(Gris);

                fila.ConstantItem(110).AlignRight().Text(texto =>
                {
                    texto.DefaultTextStyle(t => t.FontSize(7.5f).FontColor(Gris));
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }
    }
}
