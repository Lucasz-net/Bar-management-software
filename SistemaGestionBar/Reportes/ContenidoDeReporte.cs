using System;
using System.Collections.Generic;

namespace SistemaGestionBar.Reportes
{
    /// <summary>
    /// Lo que DICE un reporte, sin nada de cómo se dibuja.
    ///
    /// <b>Por qué separar las dos cosas.</b> El mismo contenido se muestra en pantalla
    /// —para que el usuario vea lo que va a exportar antes de exportarlo— y se imprime en
    /// el PDF. Si cada reporte armara su propio PDF a mano, la vista previa y el archivo
    /// se irían separando con el primer cambio, y habría que escribir cada reporte dos
    /// veces. Acá se arma una sola estructura y la dibujan los dos.
    ///
    /// Analogía web: esto es el JSON que devuelve la API; el PDF y la pantalla son dos
    /// componentes que lo renderizan distinto.
    /// </summary>
    public record ContenidoDeReporte(
        string Titulo,
        string Subtitulo,
        string GeneradoPor,
        DateTime GeneradoEl,
        IReadOnlyList<BloqueDeReporte> Bloques)
    {
        /// <summary>Nombre de archivo sugerido al guardar: sin espacios ni acentos.</summary>
        public string NombreDeArchivoSugerido =>
            $"{Simplificar(Titulo)}-{GeneradoEl:yyyy-MM-dd}.pdf";

        private static string Simplificar(string texto)
        {
            var limpio = new System.Text.StringBuilder(texto.Length);

            foreach (char c in texto.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    == System.Globalization.UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(c))
                    limpio.Append(char.ToLowerInvariant(c));
                else if (c is ' ' or '-' && limpio.Length > 0 && limpio[^1] != '-')
                    limpio.Append('-');
            }

            return limpio.ToString().Trim('-');
        }
    }

    /// <summary>Una parte del reporte. Cada tipo sabe la vista previa y el PDF dibujarlo.</summary>
    public abstract record BloqueDeReporte(string Titulo);

    /// <summary>Los números de arriba: lo que se lee de un vistazo antes de la tabla.</summary>
    public record BloqueIndicadores(string Titulo, IReadOnlyList<ValorIndicador> Valores)
        : BloqueDeReporte(Titulo);

    public record ValorIndicador(string Etiqueta, string Valor);

    /// <summary>
    /// Una tabla. Las celdas son texto ya formateado: el formato de moneda y de fecha se
    /// decide al armar el reporte, no al dibujarlo, para que la pantalla y el PDF muestren
    /// exactamente lo mismo.
    /// </summary>
    public record BloqueTabla(
        string Titulo,
        IReadOnlyList<ColumnaDeReporte> Columnas,
        IReadOnlyList<FilaDeReporte> Filas) : BloqueDeReporte(Titulo);

    /// <param name="Peso">Ancho relativo de la columna respecto de las demás.</param>
    /// <param name="ALaDerecha">Los números se alinean a la derecha; el texto, a la izquierda.</param>
    public record ColumnaDeReporte(string Encabezado, float Peso, bool ALaDerecha = false);

    /// <summary>
    /// Una fila. Es una clase y no un <c>string[]</c> pelado porque el XAML necesita
    /// bindear a algo con nombre, y porque así una fila de totales puede marcarse para
    /// que salga en negrita sin inventar una convención.
    /// </summary>
    public record FilaDeReporte(IReadOnlyList<string> Celdas, bool EsTotal = false);

    /// <summary>Un párrafo suelto: se usa cuando un bloque no tiene filas que mostrar.</summary>
    public record BloqueTexto(string Titulo, string Texto) : BloqueDeReporte(Titulo);
}
