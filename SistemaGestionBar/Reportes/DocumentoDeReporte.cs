using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestionBar.Reportes
{
    /// <summary>
    /// Dibuja un <see cref="ContenidoDeReporte"/> como PDF.
    ///
    /// Esta clase no sabe nada del bar: no consulta la base ni decide qué mostrar, solo
    /// pinta los bloques que le pasan. Por eso los cinco reportes comparten un mismo
    /// encabezado, una misma tabla y un mismo pie sin repetir código, y agregar un sexto
    /// reporte no toca este archivo.
    ///
    /// Se usa QuestPDF en su licencia Community (gratuita para este caso), que se declara
    /// una sola vez al arrancar la app en App.xaml.cs.
    /// </summary>
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
                    // Rayado cebra: con tablas de treinta filas es lo que evita que el ojo
                    // salte de renglón al leer un importe en la última columna.
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
