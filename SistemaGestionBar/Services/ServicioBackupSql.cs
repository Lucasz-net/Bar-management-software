using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SistemaGestionBar.Services
{
    /// <summary>
    /// Servicio encargado de orquestar las copias de seguridad usando los binarios 
    /// nativos de MySQL. Es la forma profesional de respaldar una base de datos 
    /// relacional, manteniendo intactas claves foráneas y fechas de auditoría.
    /// </summary>
    public class ServicioBackupSql
    {
        // Ruta de los binarios en tu entorno local
        private const string DirectorioMySql = @"C:\Program Files\MySQL\MySQL Server 26.7\bin\";
        private readonly string _rutaMySqlDump = Path.Combine(DirectorioMySql, "mysqldump.exe");
        private readonly string _rutaMySql = Path.Combine(DirectorioMySql, "mysql.exe");

        /// <summary>
        /// Obtiene o crea la ruta a la carpeta Documentos/SistemaGestionBar/Backups
        /// </summary>
        public string ObtenerDirectorioBackups()
        {
            string misDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string rutaBackups = Path.Combine(misDocumentos, "SistemaGestionBar", "Backups");

            if (!Directory.Exists(rutaBackups))
            {
                Directory.CreateDirectory(rutaBackups);
            }

            return rutaBackups;
        }

        /// <summary>
        /// Genera un archivo .sql con toda la base de datos de forma asíncrona.
        /// </summary>
        public async Task<ResultadoOperacion> CrearBackupManualAsync(string server, string port, string database, string user, string password)
        {
            if (!File.Exists(_rutaMySqlDump))
                return ResultadoOperacion.Error($"No se encontró mysqldump.exe en {_rutaMySqlDump}. Verificá la instalación de MySQL.");

            try
            {
                string directorio = ObtenerDirectorioBackups();
                string nombreArchivo = $"Backup_{DateTime.Now:yyyyMMdd_HHmm}.sql";
                string rutaDestino = Path.Combine(directorio, nombreArchivo);

                // CLAVE: Agregamos --set-gtid-purged=OFF para que el backup no requiera privilegios SUPER al restaurarse
                string argumentos = $"-h {server} -P {port} -u {user} -p\"{password}\" " +
                                    $"--routines --events --no-tablespaces --set-gtid-purged=OFF {database} " +
                                    $"--result-file=\"{rutaDestino}\"";

                var procesoInfo = new ProcessStartInfo
                {
                    FileName = _rutaMySqlDump,
                    Arguments = argumentos,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true // Oculta la ventana negra de consola
                };

                using var proceso = Process.Start(procesoInfo);
                if (proceso is null)
                    return ResultadoOperacion.Error("No se pudo iniciar el proceso de backup.");

                await proceso.WaitForExitAsync();

                if (proceso.ExitCode != 0)
                {
                    string error = await proceso.StandardError.ReadToEndAsync();
                    if (File.Exists(rutaDestino)) File.Delete(rutaDestino); // Limpiar archivo corrupto
                    return ResultadoOperacion.Error($"Error de MySQL: {error}");
                }

                return ResultadoOperacion.Ok($"Backup creado exitosamente en Documentos.\nArchivo: {nombreArchivo}");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Error($"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Restaura un archivo .sql reemplazando la base de datos actual.
        /// ADVERTENCIA: Deben cerrarse las conexiones de EF Core antes de ejecutar esto.
        /// </summary>
        public async Task<ResultadoOperacion> RestaurarBackupAsync(string rutaArchivoSql, string server, string port, string database, string user, string password)
        {
            if (!File.Exists(_rutaMySql))
                return ResultadoOperacion.Error($"No se encontró mysql.exe en {_rutaMySql}.");

            if (!File.Exists(rutaArchivoSql))
                return ResultadoOperacion.Error("No se encontró el archivo de backup seleccionado.");

            try
            {
                // Solo le pasamos los argumentos de conexión, SIN el "-e"
                string argumentos = $"-h {server} -P {port} -u {user} -p\"{password}\" {database}";

                var procesoInfo = new ProcessStartInfo
                {
                    FileName = _rutaMySql,
                    Arguments = argumentos,
                    RedirectStandardInput = true, // CLAVE: Abrimos la entrada de datos
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proceso = Process.Start(procesoInfo);
                if (proceso is null)
                    return ResultadoOperacion.Error("No se pudo iniciar el proceso de restauración.");

                // Forma 100% blindada: Leemos el archivo SQL y se lo inyectamos directamente
                // a mysql.exe en memoria. Así no hay errores de sintaxis ni problemas con las rutas.
                using (var fileStream = File.OpenRead(rutaArchivoSql))
                {
                    await fileStream.CopyToAsync(proceso.StandardInput.BaseStream);
                }

                proceso.StandardInput.Close(); // Fundamental para avisarle a MySQL que el archivo terminó

                await proceso.WaitForExitAsync();

                if (proceso.ExitCode != 0)
                {
                    string error = await proceso.StandardError.ReadToEndAsync();
                    return ResultadoOperacion.Error($"Error al restaurar: {error}");
                }

                return ResultadoOperacion.Ok("La base de datos fue restaurada con éxito. El sistema debe reiniciarse.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Error($"Error inesperado: {ex.Message}");
            }
        }
    }
}