using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// Deja la base lista para usar: aplica las migraciones pendientes y, si está vacía,
    /// la llena con los datos de <see cref="DatosPrueba"/>.
    ///
    /// <b>Para qué sirve en la práctica.</b> Un integrante clona el repositorio, crea su
    /// <c>conexion.local.txt</c> y abre la aplicación: encuentra el bar funcionando, con
    /// su catálogo, su padrón y una semana de ventas. No tiene que correr comandos ni
    /// pedirle a nadie un volcado de la base.
    ///
    /// <b>Por qué la semilla no va dentro de una migración.</b> Una migración guarda
    /// valores fijos, y acá hay dos que no lo son: las contraseñas se hashean con un salt
    /// aleatorio, y las ventas de ejemplo son "de los últimos siete días" contados desde
    /// hoy. Metidas en una migración quedarían congeladas el día que se generó, y el
    /// resumen del bar abriría siempre sin ventas del día.
    ///
    /// <b>Siembra una sola vez.</b> Si ya hay roles cargados, no toca nada: los cambios
    /// que cada uno haga desde la aplicación son suyos y no se pisan al reabrir.
    /// </summary>
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

        /// <summary>
        /// Deja los DNI guardados con los números pelados.
        ///
        /// Hace falta porque hasta el 2026-09-15 el padrón se cargaba con puntos
        /// ("38.987.654") y ahora se guarda sin ellos ("38987654"). Sin esta pasada la
        /// columna quedaría con las dos formas conviviendo: se ven iguales en pantalla
        /// —de eso se encarga <see cref="Documento.Formatear"/>— pero el dato guardado
        /// sería inconsistente, y la unicidad del DNI se apoya en él.
        ///
        /// Es idempotente y no borra nada: solo saca los separadores, así que una segunda
        /// corrida no encuentra nada que hacer. Si dos filas quedaran con el mismo número
        /// se saltea la segunda antes que romper el índice único: son datos del usuario y
        /// quién se queda con ese DNI no es una decisión que le toque tomar al arranque.
        /// </summary>
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

                // El normalizado de esta misma fila ya está en el conjunto; si aparece dos
                // veces es porque otra persona lo tiene tal cual y habría choque.
                bool loTieneOtro = personas.Any(otra => otra.IdPersona != persona.IdPersona &&
                                                        otra.DniCuit == normalizado);

                if (normalizado.Length == 0 || loTieneOtro)
                    continue;

                persona.DniCuit = normalizado;
            }

            // Sin auditoría: esto es un arreglo de formato, no una edición del usuario, y
            // no tiene por qué figurar como que alguien tocó esas fichas hoy (RF-11).
            db.AuditarCambios = false;
            db.SaveChanges();
            db.AuditarCambios = true;
        }

        private static void Sembrar(BarDbContext db)
        {
            var datos = DatosPrueba.Crear();

            // Las fechas vienen con los datos: una venta de hace seis días tiene que
            // decir que se creó hace seis días, no ahora.
            db.AuditarCambios = false;

            // Se agregan los agregados completos y EF Core ordena los INSERT según las
            // claves foráneas. Los objetos ya están enlazados entre sí por
            // EnlazarNavegacion(), así que cada fila se registra una sola vez aunque
            // aparezca en varias listas.
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
