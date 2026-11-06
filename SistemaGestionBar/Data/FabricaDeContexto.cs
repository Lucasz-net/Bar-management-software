using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    public class FabricaDeContexto
    {
        private readonly DbContextOptions<BarDbContext> _opciones = CrearOpciones();
        private readonly SesionActual _sesion;

        public FabricaDeContexto(SesionActual sesion)
        {
            _sesion = sesion;
        }

        public BarDbContext Crear() => new(_opciones, _sesion);

        public static DbContextOptions<BarDbContext> CrearOpciones()
        {
            string cadena = CadenaDeConexion.Leer();

            return new DbContextOptionsBuilder<BarDbContext>()
                .UseMySql(cadena, ServerVersion.AutoDetect(cadena))
                .Options;
        }
    }
}
