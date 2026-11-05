using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MySqlConnector;
using SistemaGestionBar.Data;

namespace SistemaGestionBar.Services
{
    public class ServicioBackupSql
    {
        /// <summary>Variable de entorno opcional con la carpeta «bin» de MySQL, si no está instalado en la ruta habitual.</summary>
        public const string VariableCarpetaMySql = "MYSQL_BIN_DIR";

        public string ObtenerDirectorioBackups()
        {
            string ruta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SistemaGestionBar", "Backups");

            Directory.CreateDirectory(ruta);
            return ruta;
        }

        public Task<ResultadoOperacion> CrearBackupManualAsync(string motivo, string creadoPor) =>
            CrearBackupAsync(motivo, creadoPor);

        private async Task<ResultadoOperacion> CrearBackupAsync(string motivo, string creadoPor)
        {
            string? mysqldump = LocalizarEjecutable("mysqldump.exe");
            if (mysqldump is null)
                return ResultadoOperacion.Error(MensajeSinMySql("mysqldump.exe"));

            string? archivoOpciones = null;
            string? rutaDestino = null;

            try
            {
                string nombreArchivo = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                rutaDestino = Path.Combine(ObtenerDirectorioBackups(), nombreArchivo);

                // // La contraseña va en un archivo temporal y no en la línea de comandos, donde cualquiera la vería.
                archivoOpciones = CrearArchivoDeOpciones();

                string argumentos = $"--defaults-extra-file=\"{archivoOpciones}\" " +
                                    "--routines --events --no-tablespaces --set-gtid-purged=OFF " +
                                    $"{Conexion().Database} --result-file=\"{rutaDestino}\"";

                var (codigo, error) = await EjecutarAsync(mysqldump, argumentos, null);

                if (codigo != 0)
                {
                    BorrarSiExiste(rutaDestino);
                    return ResultadoOperacion.Error($"Error de MySQL: {error}");
                }

                GuardarMetadatos(rutaDestino, new MetadatosDeBackup(DateTime.Now, creadoPor, motivo));

                return ResultadoOperacion.Ok($"Backup creado exitosamente en Documentos.\nArchivo: {nombreArchivo}");
            }
            catch (Exception ex)
            {
                if (rutaDestino is not null)
                    BorrarSiExiste(rutaDestino);

                return ResultadoOperacion.Error($"Error inesperado: {ex.Message}");
            }
            finally
            {
                if (archivoOpciones is not null)
                    BorrarSiExiste(archivoOpciones);
            }
        }

        public async Task<ResultadoOperacion> RestaurarBackupAsync(string rutaArchivoSql)
        {
            string? mysql = LocalizarEjecutable("mysql.exe");
            if (mysql is null)
                return ResultadoOperacion.Error(MensajeSinMySql("mysql.exe"));

            if (!File.Exists(rutaArchivoSql))
                return ResultadoOperacion.Error("No se encontró el archivo de backup seleccionado.");

            // // Red de seguridad: si el backup elegido resulta malo, se puede volver al estado de antes.
            var previo = await CrearBackupAsync("Automático: antes de restaurar otro backup", "Sistema");
            if (!previo.Exito)
                return ResultadoOperacion.Error(
                    "No se restauró nada porque no se pudo respaldar el estado actual.\n" + previo.Mensaje);

            string? archivoOpciones = null;

            try
            {
                MySqlConnection.ClearAllPools();

                archivoOpciones = CrearArchivoDeOpciones();

                var (codigo, error) = await EjecutarAsync(
                    mysql,
                    $"--defaults-extra-file=\"{archivoOpciones}\" {Conexion().Database}",
                    rutaArchivoSql);

                if (codigo != 0)
                    return ResultadoOperacion.Error($"Error al restaurar: {error}");

                return ResultadoOperacion.Ok("La base de datos fue restaurada con éxito. El sistema debe reiniciarse.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Error($"Error inesperado: {ex.Message}");
            }
            finally
            {
                if (archivoOpciones is not null)
                    BorrarSiExiste(archivoOpciones);
            }
        }

        /// <summary>Quién, cuándo y por qué: se guarda en un .json junto al .sql.</summary>
        public static MetadatosDeBackup? LeerMetadatos(string rutaSql)
        {
            string ruta = RutaMetadatos(rutaSql);
            if (!File.Exists(ruta))
                return null;

            try
            {
                return JsonSerializer.Deserialize<MetadatosDeBackup>(File.ReadAllText(ruta));
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                return null;
            }
        }

        public static void EliminarConMetadatos(string rutaSql)
        {
            if (File.Exists(rutaSql)) File.Delete(rutaSql);

            string metadatos = RutaMetadatos(rutaSql);
            if (File.Exists(metadatos)) File.Delete(metadatos);
        }

        private static void GuardarMetadatos(string rutaSql, MetadatosDeBackup metadatos) =>
            File.WriteAllText(RutaMetadatos(rutaSql), JsonSerializer.Serialize(metadatos));

        private static string RutaMetadatos(string rutaSql) => Path.ChangeExtension(rutaSql, ".json");

        private static MySqlConnectionStringBuilder Conexion() => new(CadenaDeConexion.Leer());

        /// <summary>Archivo [client] temporal con host, puerto, usuario y contraseña, para pasárselo a mysql y mysqldump.</summary>
        private static string CrearArchivoDeOpciones()
        {
            var conexion = Conexion();
            string ruta = Path.Combine(Path.GetTempPath(), $"bar_{Guid.NewGuid():N}.cnf");

            var contenido = new StringBuilder();
            contenido.AppendLine("[client]");
            contenido.AppendLine($"host=\"{Escapar(conexion.Server)}\"");
            contenido.AppendLine($"port={conexion.Port}");
            contenido.AppendLine($"user=\"{Escapar(conexion.UserID)}\"");
            contenido.AppendLine($"password=\"{Escapar(conexion.Password)}\"");

            File.WriteAllText(ruta, contenido.ToString(), new UTF8Encoding(false));
            return ruta;
        }

        // // Dentro de comillas, el archivo de opciones de MySQL entiende \\ y \".
        private static string Escapar(string valor) => valor.Replace("\\", "\\\\").Replace("\"", "\\\"");

        /// <summary>Corre el proceso leyendo salida y errores en paralelo, para que no se bloquee si llenan el búfer.</summary>
        private static async Task<(int Codigo, string Error)> EjecutarAsync(
            string ejecutable, string argumentos, string? archivoDeEntrada)
        {
            using var proceso = Process.Start(new ProcessStartInfo
            {
                FileName = ejecutable,
                Arguments = argumentos,
                RedirectStandardInput = archivoDeEntrada is not null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("No se pudo iniciar el proceso de MySQL.");

            var salida = proceso.StandardOutput.ReadToEndAsync();
            var errores = proceso.StandardError.ReadToEndAsync();

            if (archivoDeEntrada is not null)
            {
                try
                {
                    await using var archivo = File.OpenRead(archivoDeEntrada);
                    await archivo.CopyToAsync(proceso.StandardInput.BaseStream);
                }
                catch (IOException)
                {
                    // mysql cortó la lectura (error en el SQL): el motivo está en su salida de errores.
                }
                finally
                {
                    // Sin cerrar la entrada, mysql sigue esperando más datos.
                    try { proceso.StandardInput.Close(); } catch (IOException) { }
                }
            }

            await proceso.WaitForExitAsync();
            await salida;

            return (proceso.ExitCode, (await errores).Trim());
        }

        private static void BorrarSiExiste(string ruta)
        {
            try
            {
                if (File.Exists(ruta))
                    File.Delete(ruta);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string MensajeSinMySql(string ejecutable) =>
            $"No se encontró {ejecutable}. Instalá MySQL, agregá su carpeta «bin» al PATH " +
            $"o definí la variable de entorno {VariableCarpetaMySql} con esa carpeta.";

        /// <summary>Busca el ejecutable en la variable MYSQL_BIN_DIR, en el PATH y en las instalaciones de Archivos de programa (la versión más nueva primero).</summary>
        private static string? LocalizarEjecutable(string nombre)
        {
            foreach (string carpeta in CarpetasCandidatas())
            {
                try
                {
                    string ruta = Path.Combine(carpeta.Trim().Trim('"'), nombre);
                    if (File.Exists(ruta))
                        return ruta;
                }
                catch (ArgumentException)
                {
                    // Entrada inválida en el PATH: se ignora.
                }
            }

            return null;
        }

        private static IEnumerable<string> CarpetasCandidatas()
        {
            string? configurada = Environment.GetEnvironmentVariable(VariableCarpetaMySql);
            if (!string.IsNullOrWhiteSpace(configurada))
                yield return configurada;

            foreach (string carpeta in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                         .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                yield return carpeta;

            var raices = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (string raiz in raices.Where(r => !string.IsNullOrEmpty(r)).Distinct())
            {
                string mysql = Path.Combine(raiz, "MySQL");
                if (!Directory.Exists(mysql))
                    continue;

                foreach (string servidor in Directory.GetDirectories(mysql, "MySQL Server *")
                                                     .OrderByDescending(VersionDe))
                    yield return Path.Combine(servidor, "bin");
            }
        }

        private static Version VersionDe(string carpeta)
        {
            string nombre = Path.GetFileName(carpeta).Replace("MySQL Server", string.Empty).Trim();
            return Version.TryParse(nombre, out var version) ? version : new Version(0, 0);
        }
    }

    public record MetadatosDeBackup(DateTime Fecha, string CreadoPor, string Motivo);
}
