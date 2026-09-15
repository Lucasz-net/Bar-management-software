using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// El mapa entre las clases de <c>Models/</c> y las tablas de MySQL.
    ///
    /// <b>Qué hace este archivo y qué no.</b> Las clases POCO dicen qué datos hay; acá se
    /// dice cómo se guardan: nombres de tabla y columna, claves, índices únicos, tipos
    /// exactos de los decimales y qué pasa al borrar. Nada de lógica de negocio: eso vive
    /// en el repositorio, que es quien la hace cumplir venga el dato de donde venga.
    ///
    /// <b>Convención de nombres.</b> Tablas y columnas en minúscula con guión bajo
    /// (<c>venta_detalle</c>, <c>id_producto</c>), como en el DER. En minúscula a
    /// propósito: MySQL sobre Windows guarda los nombres en minúscula igual
    /// (<c>lower_case_table_names</c>), así que escribirlos con mayúsculas solo lograría
    /// que el código diga una cosa y DBeaver muestre otra. Y si algún día alguien abre el
    /// proyecto en Linux, donde los nombres SÍ distinguen mayúsculas, no se rompe nada.
    ///
    /// <b>Los enums se guardan como texto.</b> Cuesta unos bytes más que un entero, pero
    /// en DBeaver se lee <c>ParaLlevar</c> en vez de <c>1</c>, y una consulta escrita a
    /// mano no necesita ir al código a ver qué número era cada cosa.
    /// </summary>
    public class BarDbContext : DbContext
    {
        private readonly SesionActual? _sesion;

        /// <param name="sesion">
        /// Quién está usando el sistema, para estampar la auditoría de RF-11. Va nulo en
        /// los comandos de migración, que no tienen usuario logueado.
        /// </param>
        public BarDbContext(DbContextOptions<BarDbContext> opciones, SesionActual? sesion = null)
            : base(opciones)
        {
            _sesion = sesion;
        }

        public DbSet<Persona> Personas => Set<Persona>();
        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
        public DbSet<ProductoIngrediente> ProductoIngredientes => Set<ProductoIngrediente>();
        public DbSet<Receta> Recetas => Set<Receta>();
        public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
        public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();

        // Factura y FacturaLinea NO son entidades de EF Core: no tienen tabla propia.
        // El comprobante se arma en memoria a partir de Venta/VentaDetalle, ver
        // RepositorioSql.ConstruirFactura().

        // Tipos de columna, en un solo lugar para no repetirlos quince veces.
        private const string Dinero = "decimal(12,2)";
        private const string Cantidad = "decimal(12,3)";
        private const string Fecha = "datetime(6)";

        /// <summary>
        /// La siembra inicial trae sus propias fechas —las ventas de la última semana
        /// pasaron cuando dicen que pasaron— así que ahí la auditoría no se estampa.
        /// En todo el resto de la vida de la aplicación queda encendida.
        /// </summary>
        public bool AuditarCambios { get; set; } = true;

        /// <summary>
        /// RF-11 automático: en vez de que cada método del repositorio se acuerde de
        /// estampar quién y cuándo, se hace acá, en el único lugar por el que pasan
        /// todos los cambios. Un método nuevo que olvide auditar deja de ser posible.
        /// </summary>
        public override int SaveChanges()
        {
            EstamparAuditoria();
            return base.SaveChanges();
        }

        private void EstamparAuditoria()
        {
            if (!AuditarCambios)
                return;

            var ahora = DateTime.Now;
            int? usuario = _sesion?.IdUsuario;

            foreach (var entrada in ChangeTracker.Entries<EntidadAuditable>())
            {
                switch (entrada.State)
                {
                    case EntityState.Added:
                        entrada.Entity.FechaCreacion = ahora;
                        entrada.Entity.UsuarioModificacion = usuario;
                        break;

                    case EntityState.Modified:
                        entrada.Entity.FechaModificacion = ahora;
                        entrada.Entity.UsuarioModificacion = usuario;

                        // La fecha de alta no se toca nunca más: es cuándo nació la fila.
                        entrada.Property(nameof(EntidadAuditable.FechaCreacion)).IsModified = false;
                        break;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelo)
        {
            ConfigurarPersonas(modelo);
            ConfigurarCatalogo(modelo);
            ConfigurarParametricas(modelo);
            ConfigurarVentas(modelo);

            ConfigurarAuditoria(modelo);
        }

        // ---------------------------------------------------------------
        // Padrón, cuentas y clientes
        // ---------------------------------------------------------------
        private static void ConfigurarPersonas(ModelBuilder modelo)
        {
            modelo.Entity<Persona>(persona =>
            {
                persona.ToTable("persona");
                persona.HasKey(p => p.IdPersona);
                persona.Property(p => p.IdPersona).HasColumnName("id_persona");
                persona.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(60).IsRequired();
                persona.Property(p => p.Apellido).HasColumnName("apellido").HasMaxLength(60).IsRequired();
                persona.Property(p => p.Telefono).HasColumnName("telefono").HasMaxLength(30);
                persona.Property(p => p.Email).HasColumnName("email").HasMaxLength(120);

                // El DNI es la clave de negocio de la persona: obligatorio y único. Es lo
                // que impide que el mismo cliente entre dos veces al padrón cuando además
                // lo dan de alta como empleado.
                persona.Property(p => p.DniCuit).HasColumnName("dni_cuit").HasMaxLength(20).IsRequired();
                persona.HasIndex(p => p.DniCuit).IsUnique();

                // Calculadas: se arman al leer, no son columnas.
                persona.Ignore(p => p.NombreCompleto);
                persona.Ignore(p => p.NombreParaListado);
                persona.Ignore(p => p.EsEmpleado);
                persona.Ignore(p => p.EsCliente);
            });

            modelo.Entity<Rol>(rol =>
            {
                rol.ToTable("rol");
                rol.HasKey(r => r.IdRol);
                rol.Property(r => r.IdRol).HasColumnName("id_rol");
                rol.Property(r => r.NombreRol).HasColumnName("nombre_rol").HasMaxLength(40).IsRequired();
                rol.HasIndex(r => r.NombreRol).IsUnique();
            });

            modelo.Entity<Usuario>(usuario =>
            {
                usuario.ToTable("usuario");
                usuario.HasKey(u => u.IdUsuario);
                usuario.Property(u => u.IdUsuario).HasColumnName("id_usuario");
                usuario.Property(u => u.IdPersona).HasColumnName("id_persona");
                usuario.Property(u => u.IdRol).HasColumnName("id_rol");

                // El correo de trabajo es la credencial de login (RF-09): único, o la
                // autenticación quedaría ambigua.
                usuario.Property(u => u.Email).HasColumnName("email").HasMaxLength(120).IsRequired();
                usuario.HasIndex(u => u.Email).IsUnique();

                // Hash PBKDF2 + salt en base64, nunca la clave en claro.
                usuario.Property(u => u.Clave).HasColumnName("clave").HasMaxLength(200).IsRequired();

                // Una persona tiene a lo sumo UNA cuenta.
                usuario.HasOne(u => u.Persona)
                       .WithOne(p => p.Usuario)
                       .HasForeignKey<Usuario>(u => u.IdPersona)
                       .OnDelete(DeleteBehavior.Restrict);

                usuario.HasOne(u => u.Rol)
                       .WithMany(r => r.Usuarios)
                       .HasForeignKey(u => u.IdRol)
                       .OnDelete(DeleteBehavior.Restrict);

                usuario.Ignore(u => u.NombreCompleto);
                usuario.Ignore(u => u.NombreRol);
                usuario.Ignore(u => u.EsAdministrador);
                usuario.Ignore(u => u.EsGerente);
                usuario.Ignore(u => u.EsPersonalDeAtencion);
                usuario.Ignore(u => u.AccedeAlTablero);
            });

            modelo.Entity<Cliente>(cliente =>
            {
                cliente.ToTable("cliente");
                cliente.HasKey(c => c.IdCliente);
                cliente.Property(c => c.IdCliente).HasColumnName("id_cliente");
                cliente.Property(c => c.IdPersona).HasColumnName("id_persona");
                cliente.Property(c => c.FechaRegistro).HasColumnName("fecha_registro").HasColumnType(Fecha);

                // Ser cliente es un casillero de la ficha de la persona: si se borra la
                // persona, esa fila se va con ella.
                cliente.HasOne(c => c.Persona)
                       .WithOne(p => p.Cliente)
                       .HasForeignKey<Cliente>(c => c.IdPersona)
                       .OnDelete(DeleteBehavior.Cascade);

                cliente.Ignore(c => c.NombreMostrado);
            });
        }

        // ---------------------------------------------------------------
        // Catálogo, insumos y recetas
        // ---------------------------------------------------------------
        private static void ConfigurarCatalogo(ModelBuilder modelo)
        {
            modelo.Entity<Categoria>(categoria =>
            {
                categoria.ToTable("categoria");
                categoria.HasKey(c => c.IdCategoria);
                categoria.Property(c => c.IdCategoria).HasColumnName("id_categoria");
                categoria.Property(c => c.NombreCategoria).HasColumnName("nombre_categoria").HasMaxLength(40).IsRequired();
                categoria.HasIndex(c => c.NombreCategoria).IsUnique();
            });

            modelo.Entity<Producto>(producto =>
            {
                producto.ToTable("producto");
                producto.HasKey(p => p.IdProducto);
                producto.Property(p => p.IdProducto).HasColumnName("id_producto");
                producto.Property(p => p.IdCategoria).HasColumnName("id_categoria");
                producto.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
                producto.Property(p => p.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
                producto.Property(p => p.Precio).HasColumnName("precio").HasColumnType(Dinero);
                producto.Property(p => p.Stock).HasColumnName("stock");

                // EXTENSIÓN AL DER: umbral de reposición para la venta directa (RF-08).
                producto.Property(p => p.StockMinimo).HasColumnName("stock_minimo");

                // EXTENSIÓN AL DER: se guarda la RUTA de la foto, no el binario. Meter
                // imágenes como VARBINARY infla la base y hace lento cada SELECT.
                producto.Property(p => p.RutaImagen).HasColumnName("ruta_imagen").HasMaxLength(260);

                producto.HasIndex(p => p.Nombre).IsUnique();

                producto.HasOne(p => p.Categoria)
                        .WithMany(c => c.Productos)
                        .HasForeignKey(p => p.IdCategoria)
                        .OnDelete(DeleteBehavior.Restrict);

                producto.Ignore(p => p.TieneReceta);
                producto.Ignore(p => p.StockBajo);
            });

            modelo.Entity<Ingrediente>(ingrediente =>
            {
                ingrediente.ToTable("ingrediente");
                ingrediente.HasKey(i => i.IdIngrediente);
                ingrediente.Property(i => i.IdIngrediente).HasColumnName("id_ingrediente");
                ingrediente.Property(i => i.Nombre).HasColumnName("nombre").HasMaxLength(60).IsRequired();
                ingrediente.Property(i => i.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(15).IsRequired();
                ingrediente.Property(i => i.Stock).HasColumnName("stock").HasColumnType(Cantidad);
                ingrediente.Property(i => i.StockMinimo).HasColumnName("stock_minimo").HasColumnType(Cantidad);

                ingrediente.HasIndex(i => i.Nombre).IsUnique();
                ingrediente.Ignore(i => i.StockBajo);
            });

            modelo.Entity<ProductoIngrediente>(composicion =>
            {
                composicion.ToTable("producto_ingrediente");

                // Clave compuesta: un insumo aparece una sola vez por producto.
                composicion.HasKey(pi => new { pi.IdProducto, pi.IdIngrediente });

                composicion.Property(pi => pi.IdProducto).HasColumnName("id_producto");
                composicion.Property(pi => pi.IdIngrediente).HasColumnName("id_ingrediente");
                composicion.Property(pi => pi.CantidadNecesaria).HasColumnName("cantidad_necesaria").HasColumnType(Cantidad);

                // Borrar el producto se lleva su receta; borrar un insumo que está en uso
                // se bloquea, que es la regla de integridad de RF-13.
                composicion.HasOne(pi => pi.Producto)
                           .WithMany(p => p.Ingredientes)
                           .HasForeignKey(pi => pi.IdProducto)
                           .OnDelete(DeleteBehavior.Cascade);

                composicion.HasOne(pi => pi.Ingrediente)
                           .WithMany()
                           .HasForeignKey(pi => pi.IdIngrediente)
                           .OnDelete(DeleteBehavior.Restrict);

                composicion.Ignore(pi => pi.Descripcion);
            });

            modelo.Entity<Receta>(receta =>
            {
                receta.ToTable("receta");

                // EXTENSIÓN AL DER. Relación 1-1 opcional con Producto: la clave primaria
                // es además la foránea. Va en tabla aparte y no como columna de Producto
                // porque una botella no se prepara, y la columna quedaría en NULL para
                // más de la mitad del catálogo.
                receta.HasKey(r => r.IdProducto);
                receta.Property(r => r.IdProducto).HasColumnName("id_producto").ValueGeneratedNever();
                receta.Property(r => r.Instrucciones).HasColumnName("preparacion").IsRequired();

                receta.HasOne(r => r.Producto)
                      .WithOne(p => p.Receta)
                      .HasForeignKey<Receta>(r => r.IdProducto)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ---------------------------------------------------------------
        // Tablas paramétricas
        // ---------------------------------------------------------------
        private static void ConfigurarParametricas(ModelBuilder modelo)
        {
            modelo.Entity<MetodoPago>(metodo =>
            {
                metodo.ToTable("metodo_pago");
                metodo.HasKey(m => m.IdMetodoPago);
                metodo.Property(m => m.IdMetodoPago).HasColumnName("id_metodo_pago");
                metodo.Property(m => m.NombreMetodo).HasColumnName("nombre_metodo").HasMaxLength(40).IsRequired();
                metodo.HasIndex(m => m.NombreMetodo).IsUnique();
            });

            modelo.Entity<Ubicacion>(ubicacion =>
            {
                ubicacion.ToTable("ubicacion");
                ubicacion.HasKey(u => u.IdUbicacion);
                ubicacion.Property(u => u.IdUbicacion).HasColumnName("id_ubicacion");
                ubicacion.Property(u => u.NombreUbicacion).HasColumnName("nombre_ubicacion").HasMaxLength(40).IsRequired();
                ubicacion.Property(u => u.Capacidad).HasColumnName("capacidad");

                // EXTENSIÓN AL DER: Mesa o Barra. En la barra atiende el barman, así que
                // la venta va sin mesero, y deducirlo del texto del nombre sería frágil.
                ubicacion.Property(u => u.Tipo)
                         .HasColumnName("tipo_ubicacion")
                         .HasConversion<string>()
                         .HasMaxLength(20)
                         .IsRequired();

                ubicacion.HasIndex(u => u.NombreUbicacion).IsUnique();

                ubicacion.Ignore(u => u.RequiereMesero);
                ubicacion.Ignore(u => u.Descripcion);
            });
        }

        // ---------------------------------------------------------------
        // Ventas
        // ---------------------------------------------------------------
        private static void ConfigurarVentas(ModelBuilder modelo)
        {
            modelo.Entity<Venta>(venta =>
            {
                venta.ToTable("venta");
                venta.HasKey(v => v.IdVenta);
                venta.Property(v => v.IdVenta).HasColumnName("id_venta");
                venta.Property(v => v.FechaHora).HasColumnName("fecha_hora").HasColumnType(Fecha);
                venta.Property(v => v.IdMetodoPago).HasColumnName("id_metodo_pago");
                venta.Property(v => v.IdCliente).HasColumnName("id_cliente");
                venta.Property(v => v.IdCajero).HasColumnName("id_cajero");
                venta.Property(v => v.IdMesero).HasColumnName("id_mesero");
                venta.Property(v => v.IdUbicacion).HasColumnName("id_ubicacion");

                venta.Property(v => v.EstadoVenta)
                     .HasColumnName("estado_venta")
                     .HasConversion<string>()
                     .HasMaxLength(20)
                     .IsRequired();

                venta.Property(v => v.ModalidadConsumo)
                     .HasColumnName("modalidad_consumo")
                     .HasConversion<string>()
                     .HasMaxLength(20)
                     .IsRequired();

                venta.HasOne(v => v.MetodoPago).WithMany()
                     .HasForeignKey(v => v.IdMetodoPago).OnDelete(DeleteBehavior.Restrict);

                venta.HasOne(v => v.Cliente).WithMany()
                     .HasForeignKey(v => v.IdCliente).OnDelete(DeleteBehavior.Restrict);

                // Dos caminos distintos a Usuario, y por eso hay que declararlos: el
                // cajero que cobró y el mesero que atendió la mesa (RF-02). El mesero es
                // opcional porque en la barra y en "para llevar" no hay.
                venta.HasOne(v => v.Cajero).WithMany()
                     .HasForeignKey(v => v.IdCajero).OnDelete(DeleteBehavior.Restrict);

                venta.HasOne(v => v.Mesero).WithMany()
                     .HasForeignKey(v => v.IdMesero).OnDelete(DeleteBehavior.Restrict);

                venta.HasOne(v => v.Ubicacion).WithMany()
                     .HasForeignKey(v => v.IdUbicacion).OnDelete(DeleteBehavior.Restrict);

                // El total NO se persiste: se suma del detalle cada vez que se lee. Es la
                // regla del enunciado, y evita que la cabecera y el detalle se contradigan.
                venta.Ignore(v => v.Total);
            });

            modelo.Entity<VentaDetalle>(detalle =>
            {
                detalle.ToTable("venta_detalle");
                detalle.HasKey(d => d.IdVentaDetalle);
                detalle.Property(d => d.IdVentaDetalle).HasColumnName("id_venta_detalle");
                detalle.Property(d => d.IdVenta).HasColumnName("id_venta");
                detalle.Property(d => d.IdProducto).HasColumnName("id_producto");
                detalle.Property(d => d.Cantidad).HasColumnName("cantidad");

                // El precio unitario y el subtotal SÍ se guardan: son la foto del precio
                // al momento de vender. Si mañana sube el Mojito, los tickets viejos no
                // pueden cambiar.
                detalle.Property(d => d.PrecioUnitario).HasColumnName("precio_unitario").HasColumnType(Dinero);
                detalle.Property(d => d.Subtotal).HasColumnName("subtotal").HasColumnType(Dinero);

                detalle.HasOne(d => d.Venta).WithMany(v => v.Detalles)
                       .HasForeignKey(d => d.IdVenta).OnDelete(DeleteBehavior.Cascade);

                detalle.HasOne(d => d.Producto).WithMany()
                       .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
            });
        }

        // ---------------------------------------------------------------
        // Auditoría (RF-11)
        // ---------------------------------------------------------------
        /// <summary>
        /// Las tres columnas de auditoría son iguales en todas las entidades que heredan
        /// de <see cref="EntidadAuditable"/>, así que se configuran de una sola pasada en
        /// vez de repetirlas doce veces. Va al final, cuando el modelo ya tiene todas las
        /// entidades registradas.
        /// </summary>
        private static void ConfigurarAuditoria(ModelBuilder modelo)
        {
            var auditables = modelo.Model.GetEntityTypes()
                                   .Where(t => typeof(EntidadAuditable).IsAssignableFrom(t.ClrType))
                                   .ToList();

            foreach (var tipo in auditables)
            {
                var entidad = modelo.Entity(tipo.ClrType);

                entidad.Property(nameof(EntidadAuditable.FechaCreacion))
                       .HasColumnName("fecha_creacion")
                       .HasColumnType(Fecha);

                entidad.Property(nameof(EntidadAuditable.FechaModificacion))
                       .HasColumnName("fecha_modificacion")
                       .HasColumnType(Fecha);

                entidad.Property(nameof(EntidadAuditable.UsuarioModificacion))
                       .HasColumnName("usuario_modificacion");
            }
        }
    }
}
