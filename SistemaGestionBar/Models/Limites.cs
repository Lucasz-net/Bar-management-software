namespace SistemaGestionBar.Models
{
    /// <summary>Largos y techos de las columnas, compartidos por el mapeo de EF Core, la validación y los MaxLength. Agrandar uno requiere una migración.</summary>
    public static class Limites
    {
        public const int NombrePersona = 60;
        public const int ApellidoPersona = 60;
        public const int Telefono = 30;
        public const int Email = 120;
        public const int NombreProducto = 80;
        public const int DescripcionProducto = 200;
        public const int RutaImagen = 260;
        public const int NombreInsumo = 60;
        public const int UnidadMedida = 15;
        public const int NombreParametrico = 40;
        public const int NombreRol = 40;
        public const int ClaveHasheada = 200;
        public const decimal Dinero = 9_999_999_999.99m;
        public const decimal Cantidad = 999_999_999.999m;

        /// <summary>Se valida contra este techo antes de sumar, porque un int desbordado queda negativo sin avisar.</summary>
        public const int StockDeProducto = int.MaxValue;
    }
}
