using System;
using System.IO;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// De dónde sale la cadena de conexión a MySQL.
    ///
    /// <b>Por qué un archivo aparte y no una constante en el código.</b> La cadena lleva la
    /// contraseña de la base, y este proyecto se versiona en GitHub y además se pasa de
    /// mano en mano entre los integrantes. Una contraseña escrita en un .cs termina en el
    /// historial del repositorio y ya no se saca más. Además, cada uno instala su propio
    /// MySQL local: la máquina de al lado no tiene el mismo usuario ni la misma clave.
    ///
    /// Por eso: <c>conexion.local.txt</c> lo escribe cada uno en su máquina y está en el
    /// .gitignore; <c>conexion.ejemplo.txt</c> sí se versiona, con un valor de mentira,
    /// para que el que clona sepa qué tiene que crear.
    /// </summary>
    public static class CadenaDeConexion
    {
        public const string ArchivoLocal = "conexion.local.txt";
        public const string ArchivoEjemplo = "conexion.ejemplo.txt";

        /// <summary>
        /// Lee la cadena del archivo local. Si no está, tira una excepción que explica
        /// exactamente qué hacer: es la primera pared contra la que choca alguien que
        /// clona el repositorio, así que el mensaje tiene que alcanzar para resolverlo.
        /// </summary>
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

        /// <summary>
        /// Primero busca el archivo junto al ejecutable, que es donde lo deja el build
        /// (el .csproj lo copia a la carpeta de salida). Si no está ahí —por ejemplo
        /// cuando alguien acaba de crearlo y todavía no recompiló— sube hasta la carpeta
        /// del proyecto, que es donde lo escribe cada uno a mano.
        /// </summary>
        private static string Ruta(string nombre)
        {
            string juntoAlEjecutable = Path.Combine(AppContext.BaseDirectory, nombre);

            if (File.Exists(juntoAlEjecutable))
                return juntoAlEjecutable;

            var carpeta = new DirectoryInfo(AppContext.BaseDirectory);

            while (carpeta is not null && carpeta.GetFiles("*.csproj").Length == 0)
                carpeta = carpeta.Parent;

            string juntoAlProyecto = carpeta is null ? juntoAlEjecutable : Path.Combine(carpeta.FullName, nombre);

            // Se devuelve la ruta del ejecutable si tampoco está en el proyecto: es la
            // que hay que nombrar en el mensaje de error, porque es donde debería estar.
            return File.Exists(juntoAlProyecto) ? juntoAlProyecto : juntoAlEjecutable;
        }
    }
}
