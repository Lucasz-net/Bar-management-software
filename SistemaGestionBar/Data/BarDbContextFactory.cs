using Microsoft.EntityFrameworkCore.Design;

namespace SistemaGestionBar.Data
{
    /// <summary>Lo usan las herramientas de migración de EF Core.</summary>
    public class BarDbContextFactory : IDesignTimeDbContextFactory<BarDbContext>
    {
        public BarDbContext CreateDbContext(string[] args) => new(FabricaDeContexto.CrearOpciones());
    }
}
