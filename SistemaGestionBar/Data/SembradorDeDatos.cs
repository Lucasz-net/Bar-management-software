using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.Data
{
    public static class SembradorDeDatos
    {
        public static void Preparar(FabricaDeContexto fabrica)
        {
            using var db = fabrica.Crear();

            db.Database.Migrate();

            NormalizarDocumentos(db);

            if (db.Roles.Any())
                return;

            Sembrar(db);
        }

        private static void NormalizarDocumentos(BarDbContext db)
        {
            var personas = db.Personas.ToList();

            var pendientes = personas
                .Where(p => p.DniCuit != Documento.Normalizar(p.DniCuit))
                .ToList();

            if (pendientes.Count == 0)
                return;

            foreach (var persona in pendientes)
            {
                string normalizado = Documento.Normalizar(persona.DniCuit);

                bool loTieneOtro = personas.Any(otra => otra.IdPersona != persona.IdPersona &&
                                                        otra.DniCuit == normalizado);

                if (normalizado.Length == 0 || loTieneOtro)
                    continue;

                persona.DniCuit = normalizado;
            }

            db.AuditarCambios = false;
            db.SaveChanges();
            db.AuditarCambios = true;
        }

        private static void Sembrar(BarDbContext db)
        {
            var datos = DatosPrueba.Crear();

            db.AuditarCambios = false;

            db.Roles.AddRange(datos.Roles);
            db.Personas.AddRange(datos.Personas);
            db.Usuarios.AddRange(datos.Usuarios);
            db.Clientes.AddRange(datos.Clientes);

            db.Categorias.AddRange(datos.Categorias);
            db.Ingredientes.AddRange(datos.Ingredientes);
            db.Productos.AddRange(datos.Productos);
            db.ProductoIngredientes.AddRange(datos.ProductoIngredientes);
            db.Recetas.AddRange(datos.Recetas);

            db.MetodosPago.AddRange(datos.MetodosPago);
            db.Ubicaciones.AddRange(datos.Ubicaciones);

            db.Ventas.AddRange(datos.Ventas);

            db.SaveChanges();
            db.AuditarCambios = true;
        }
    }
}
