using System;
using System.Collections.Generic;

namespace SistemaGestionBar.Reportes
{
    public record ContenidoDeReporte(
        string Titulo,
        string Subtitulo,
        string GeneradoPor,
        DateTime GeneradoEl,
        IReadOnlyList<BloqueDeReporte> Bloques)
    {
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

    public abstract record BloqueDeReporte(string Titulo);

    public record BloqueIndicadores(string Titulo, IReadOnlyList<ValorIndicador> Valores)
        : BloqueDeReporte(Titulo);

    public record ValorIndicador(string Etiqueta, string Valor);

    public record BloqueTabla(
        string Titulo,
        IReadOnlyList<ColumnaDeReporte> Columnas,
        IReadOnlyList<FilaDeReporte> Filas) : BloqueDeReporte(Titulo);

    public record ColumnaDeReporte(string Encabezado, float Peso, bool ALaDerecha = false);

    public record FilaDeReporte(IReadOnlyList<string> Celdas, bool EsTotal = false);

    public record BloqueTexto(string Titulo, string Texto) : BloqueDeReporte(Titulo);

    public record BloqueGrafico(
        string Titulo,
        TipoDeGrafico Tipo,
        IReadOnlyList<SerieDeGrafico> Series) : BloqueDeReporte(Titulo);

    public enum TipoDeGrafico
    {
        Barras,
        Circular
    }

    public record SerieDeGrafico(string Etiqueta, decimal Valor, string ValorTexto);
}
