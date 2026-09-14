using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// Cómo arma el <see cref="BarDbContext"/> la herramienta <c>dotnet ef</c> cuando
    /// genera o aplica migraciones.
    ///
    /// Hace falta porque los comandos de migración no levantan la aplicación: no hay
    /// ventana ni arranque de WPF, así que nadie le pasa las opciones al contexto. Sin
    /// esta clase, <c>dotnet ef</c> intenta adivinar cómo construirlo y falla.
    ///
    /// Lee la misma cadena de conexión que usa la aplicación, así que no hay forma de
    /// que las migraciones apunten a una base y el programa a otra.
    /// </summary>
    public class BarDbContextFactory : IDesignTimeDbContextFactory<BarDbContext>
    {
        public BarDbContext CreateDbContext(string[] args)
        {
            string cadena = CadenaDeConexion.Leer();

            var opciones = new DbContextOptionsBuilder<BarDbContext>()
                .UseMySql(cadena, ServerVersion.AutoDetect(cadena))
                .Options;

            return new BarDbContext(opciones);
        }
    }
}
