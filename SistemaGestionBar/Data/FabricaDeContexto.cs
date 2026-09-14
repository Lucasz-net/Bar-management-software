using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// Fabrica un <see cref="BarDbContext"/> nuevo por cada operación.
    ///
    /// <b>Por qué uno por operación y no uno solo para toda la aplicación.</b> El
    /// DbContext lleva un registro de todo lo que leyó para saber qué cambió. Si viviera
    /// mientras la aplicación está abierta, ese registro crecería sin parar y —peor— las
    /// pantallas verían los datos como estaban la primera vez que se consultaron, no como
    /// están ahora. Un contexto corto se lee, se usa y se tira: cada consulta va a la base.
    ///
    /// La versión del servidor se detecta UNA vez, al arrancar: <c>AutoDetect</c> abre una
    /// conexión para preguntarla, y hacerlo en cada operación sería pagar ese viaje de más.
    /// </summary>
    public class FabricaDeContexto
    {
        private readonly DbContextOptions<BarDbContext> _opciones;
        private readonly SesionActual _sesion;

        public FabricaDeContexto(SesionActual sesion)
        {
            _sesion = sesion;

            string cadena = CadenaDeConexion.Leer();

            _opciones = new DbContextOptionsBuilder<BarDbContext>()
                .UseMySql(cadena, ServerVersion.AutoDetect(cadena))
                .Options;
        }

        public BarDbContext Crear() => new(_opciones, _sesion);
    }
}
