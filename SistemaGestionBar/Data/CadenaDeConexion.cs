using System;
using System.IO;

namespace SistemaGestionBar.Data
{
    public static class CadenaDeConexion
    {
        public const string ArchivoLocal = "conexion.local.txt";
        public const string ArchivoEjemplo = "conexion.ejemplo.txt";

        public static string Leer()
        {
            string ruta = Ruta(ArchivoLocal);

            if (!File.Exists(ruta))
                throw new InvalidOperationException(
                    $"No encontré «{ArchivoLocal}», que es donde va la cadena de conexión a MySQL.\n\n" +
                    $"Crealo en:\n  {ruta}\n\n" +
                    $"Copiá el contenido de «{ArchivoEjemplo}» y cambiá el usuario y la contraseña\n" +
                    $"por los de tu MySQL local. Ese archivo NO se sube al repositorio.");

            string cadena = File.ReadAllText(ruta).Trim();

            if (string.IsNullOrWhiteSpace(cadena))
                throw new InvalidOperationException(
                    $"«{ArchivoLocal}» está vacío. Tiene que tener la cadena de conexión en una sola línea.");

            return cadena;
        }

        /// <summary>Busca el archivo junto al ejecutable y, si no está, junto al .csproj (ejecución desde el IDE).</summary>
        private static string Ruta(string nombre)
        {
            string juntoAlEjecutable = Path.Combine(AppContext.BaseDirectory, nombre);

            if (File.Exists(juntoAlEjecutable))
                return juntoAlEjecutable;

            var carpeta = new DirectoryInfo(AppContext.BaseDirectory);

            while (carpeta is not null && carpeta.GetFiles("*.csproj").Length == 0)
                carpeta = carpeta.Parent;

            string juntoAlProyecto = carpeta is null ? juntoAlEjecutable : Path.Combine(carpeta.FullName, nombre);

            return File.Exists(juntoAlProyecto) ? juntoAlProyecto : juntoAlEjecutable;
        }
    }
}
