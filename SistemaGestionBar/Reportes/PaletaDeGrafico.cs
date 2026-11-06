namespace SistemaGestionBar.Reportes
{
    public static class PaletaDeGrafico
    {
        private static readonly string[] Colores =
        {
            "#1565C0", "#2E7D32", "#E65100", "#C62828",
            "#6A1B9A", "#00838F", "#AD6800", "#5D4037"
        };

        public static string Color(int indice) => Colores[indice % Colores.Length];
    }
}
