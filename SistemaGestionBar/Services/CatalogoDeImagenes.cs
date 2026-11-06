using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace SistemaGestionBar.Services
{
    public record ImagenDeProducto(string Nombre, string Ruta);

    /// <summary>Las ilustraciones incluidas en el programa (Assets/Productos), leídas del ensamblado.</summary>
    public static class CatalogoDeImagenes
    {
        private const string Carpeta = "assets/productos/";

        public static string RutaDe(string archivo) =>
            $"pack://application:,,,/SistemaGestionBar;component/Assets/Productos/{archivo}";

        public static IReadOnlyList<ImagenDeProducto> Listar()
        {
            var ensamblado = Assembly.GetExecutingAssembly();
            var recurso = ensamblado.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(".g.resources"));
            if (recurso is null)
                return Array.Empty<ImagenDeProducto>();

            using var flujo = ensamblado.GetManifestResourceStream(recurso)!;
            using var lector = new ResourceReader(flujo);

            var imagenes = new List<ImagenDeProducto>();
            foreach (System.Collections.DictionaryEntry entrada in lector)
            {
                string clave = (string)entrada.Key;
                if (!clave.StartsWith(Carpeta, StringComparison.OrdinalIgnoreCase))
                    continue;

                string archivo = clave[Carpeta.Length..];
                imagenes.Add(new ImagenDeProducto(Nombrar(archivo), RutaDe(archivo)));
            }

            return imagenes.OrderBy(i => i.Nombre).ToList();
        }

        private static string Nombrar(string archivo)
        {
            string sinExtension = Path.GetFileNameWithoutExtension(archivo).Replace('-', ' ');
            return CultureInfo.GetCultureInfo("es-AR").TextInfo.ToTitleCase(sinExtension);
        }
    }
}
