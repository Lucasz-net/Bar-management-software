using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    public static class DatosPruebaRecetas
    {
        public static Dictionary<string, (List<string> Ingredientes, string Preparacion)> ObtenerRecetasMock()
        {
            return new Dictionary<string, (List<string> Ingredientes, string Preparacion)>
            {
                ["Mojito"] = (
                    new List<string> { "50 ml de ron blanco", "8 hojas de menta fresca", "25 ml de jugo de lima", "2 cucharaditas de azúcar", "Soda", "Hielo" },
                    "Machacar la menta con el azúcar y el jugo de lima en el vaso. Agregar el ron y el hielo, completar con soda y remover suavemente."
                ),
                ["Margarita"] = (
                    new List<string> { "50 ml de tequila", "25 ml de triple sec", "25 ml de jugo de limón", "Sal para el borde", "Hielo" },
                    "Escarchar el borde de la copa con sal. Agitar el tequila, el triple sec y el jugo de limón con hielo en una coctelera y servir colado."
                ),
                ["Whisky Sour"] = (
                    new List<string> { "50 ml de whisky", "25 ml de jugo de limón", "15 ml de almíbar", "1 clara de huevo (opcional)", "Hielo" },
                    "Agitar todos los ingredientes con hielo en una coctelera hasta enfriar bien y servir colado en un vaso con hielo."
                )
            };
        }
    }
}
