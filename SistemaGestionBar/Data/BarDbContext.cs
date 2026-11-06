using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>Mapa entre las clases de Models y las tablas de MySQL. Tablas y columnas en minúscula con guión bajo; los enums se guardan como texto.</summary>
    public class BarDbContext : DbContext
    {
        private readonly SesionActual? _sesion;

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

        private const string Dinero = "decimal(12,2)";
        private const string Cantidad = "decimal(12,3)";
        private const string Fecha = "datetime(6)";

        /// <summary>Se apaga solo en la siembra inicial, que trae sus propias fechas.</summary>
        public bool AuditarCambios { get; set; } = true;

        public override int SaveChanges()
        {
            EstamparAuditoria();
            return base.SaveChanges();
        }

        // // Un único punto por el que pasan todos los cambios: ningún método del repositorio puede olvidar auditar.
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

        private static void ConfigurarPersonas(ModelBuilder modelo)
        {
            modelo.Entity<Persona>(persona =>
            {
                persona.ToTable("persona");
                persona.HasKey(p => p.IdPersona);
                persona.Property(p => p.IdPersona).HasColumnName("id_persona");
                persona.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(Limites.NombrePersona).IsRequired();
                persona.Property(p => p.Apellido).HasColumnName("apellido").HasMaxLength(Limites.ApellidoPersona).IsRequired();
                persona.Property(p => p.Telefono).HasColumnName("telefono").HasMaxLength(Limites.Telefono);
                persona.Property(p => p.Email).HasColumnName("email").HasMaxLength(Limites.Email);

                persona.Property(p => p.DniCuit).HasColumnName("dni_cuit").HasMaxLength(20).IsRequired();
                // // El DNI es la clave de negocio: evita cargar dos veces a la misma persona.
                persona.HasIndex(p => p.DniCuit).IsUnique();

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
                rol.Property(r => r.NombreRol).HasColumnName("nombre_rol").HasMaxLength(Limites.NombreRol).IsRequired();
                rol.HasIndex(r => r.NombreRol).IsUnique();
            });

            modelo.Entity<Usuario>(usuario =>
            {
                usuario.ToTable("usuario");
                usuario.HasKey(u => u.IdUsuario);
                usuario.Property(u => u.IdUsuario).HasColumnName("id_usuario");
                usuario.Property(u => u.IdPersona).HasColumnName("id_persona");
                usuario.Property(u => u.IdRol).HasColumnName("id_rol");

                usuario.Property(u => u.Email).HasColumnName("email").HasMaxLength(Limites.Email).IsRequired();
                usuario.HasIndex(u => u.Email).IsUnique();

                usuario.Property(u => u.Clave).HasColumnName("clave").HasMaxLength(Limites.ClaveHasheada).IsRequired();

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

                // // Ser cliente depende de la persona: si se borra, esta fila se va con ella.
                cliente.HasOne(c => c.Persona)
                       .WithOne(p => p.Cliente)
                       .HasForeignKey<Cliente>(c => c.IdPersona)
                       .OnDelete(DeleteBehavior.Cascade);

                cliente.Ignore(c => c.NombreMostrado);
            });
        }

        private static void ConfigurarCatalogo(ModelBuilder modelo)
        {
            modelo.Entity<Categoria>(categoria =>
            {
                categoria.ToTable("categoria");
                categoria.HasKey(c => c.IdCategoria);
                categoria.Property(c => c.IdCategoria).HasColumnName("id_categoria");
                categoria.Property(c => c.NombreCategoria).HasColumnName("nombre_categoria").HasMaxLength(Limites.NombreParametrico).IsRequired();
                categoria.HasIndex(c => c.NombreCategoria).IsUnique();
            });

            modelo.Entity<Producto>(producto =>
            {
                producto.ToTable("producto");
                producto.HasKey(p => p.IdProducto);
                producto.Property(p => p.IdProducto).HasColumnName("id_producto");
                producto.Property(p => p.IdCategoria).HasColumnName("id_categoria");
                producto.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(Limites.NombreProducto).IsRequired();
                producto.Property(p => p.Descripcion).HasColumnName("descripcion").HasMaxLength(Limites.DescripcionProducto);
                producto.Property(p => p.Precio).HasColumnName("precio").HasColumnType(Dinero);
                producto.Property(p => p.Stock).HasColumnName("stock");

                producto.Property(p => p.StockMinimo).HasColumnName("stock_minimo");

                producto.Property(p => p.RutaImagen).HasColumnName("ruta_imagen").HasMaxLength(Limites.RutaImagen);

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
                ingrediente.Property(i => i.Nombre).HasColumnName("nombre").HasMaxLength(Limites.NombreInsumo).IsRequired();
                ingrediente.Property(i => i.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(Limites.UnidadMedida).IsRequired();
                ingrediente.Property(i => i.Stock).HasColumnName("stock").HasColumnType(Cantidad);
                ingrediente.Property(i => i.StockMinimo).HasColumnName("stock_minimo").HasColumnType(Cantidad);

                ingrediente.HasIndex(i => i.Nombre).IsUnique();
                ingrediente.Ignore(i => i.StockBajo);
            });

            modelo.Entity<ProductoIngrediente>(composicion =>
            {
                composicion.ToTable("producto_ingrediente");

                // // Un insumo aparece una sola vez por producto.
                composicion.HasKey(pi => new { pi.IdProducto, pi.IdIngrediente });

                composicion.Property(pi => pi.IdProducto).HasColumnName("id_producto");
                composicion.Property(pi => pi.IdIngrediente).HasColumnName("id_ingrediente");
                composicion.Property(pi => pi.CantidadNecesaria).HasColumnName("cantidad_necesaria").HasColumnType(Cantidad);

                composicion.HasOne(pi => pi.Producto)
                           .WithMany(p => p.Ingredientes)
                           .HasForeignKey(pi => pi.IdProducto)
                           .OnDelete(DeleteBehavior.Cascade);

                // // Un insumo en uso no se puede borrar.
                composicion.HasOne(pi => pi.Ingrediente)
                           .WithMany()
                           .HasForeignKey(pi => pi.IdIngrediente)
                           .OnDelete(DeleteBehavior.Restrict);

                composicion.Ignore(pi => pi.Descripcion);
            });

            modelo.Entity<Receta>(receta =>
            {
                receta.ToTable("receta");

                // // Clave primaria y foránea a la vez (1 a 1 con Producto).
                receta.HasKey(r => r.IdProducto);
                receta.Property(r => r.IdProducto).HasColumnName("id_producto").ValueGeneratedNever();
                receta.Property(r => r.Instrucciones).HasColumnName("preparacion").IsRequired();

                receta.HasOne(r => r.Producto)
                      .WithOne(p => p.Receta)
                      .HasForeignKey<Receta>(r => r.IdProducto)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigurarParametricas(ModelBuilder modelo)
        {
            modelo.Entity<MetodoPago>(metodo =>
            {
                metodo.ToTable("metodo_pago");
                metodo.HasKey(m => m.IdMetodoPago);
                metodo.Property(m => m.IdMetodoPago).HasColumnName("id_metodo_pago");
                metodo.Property(m => m.NombreMetodo).HasColumnName("nombre_metodo").HasMaxLength(Limites.NombreParametrico).IsRequired();
                metodo.HasIndex(m => m.NombreMetodo).IsUnique();
            });

            modelo.Entity<Ubicacion>(ubicacion =>
            {
                ubicacion.ToTable("ubicacion");
                ubicacion.HasKey(u => u.IdUbicacion);
                ubicacion.Property(u => u.IdUbicacion).HasColumnName("id_ubicacion");
                ubicacion.Property(u => u.NombreUbicacion).HasColumnName("nombre_ubicacion").HasMaxLength(Limites.NombreParametrico).IsRequired();
                ubicacion.Property(u => u.Capacidad).HasColumnName("capacidad");

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

                // // El mesero es opcional: en barra y para llevar no hay.
                venta.HasOne(v => v.Cajero).WithMany()
                     .HasForeignKey(v => v.IdCajero).OnDelete(DeleteBehavior.Restrict);

                venta.HasOne(v => v.Mesero).WithMany()
                     .HasForeignKey(v => v.IdMesero).OnDelete(DeleteBehavior.Restrict);

                venta.HasOne(v => v.Ubicacion).WithMany()
                     .HasForeignKey(v => v.IdUbicacion).OnDelete(DeleteBehavior.Restrict);

                // // El total se suma del detalle, no se persiste.
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

                // // Foto del precio al vender: los tickets viejos no cambian si sube el producto.
                detalle.Property(d => d.PrecioUnitario).HasColumnName("precio_unitario").HasColumnType(Dinero);
                detalle.Property(d => d.Subtotal).HasColumnName("subtotal").HasColumnType(Dinero);

                detalle.HasOne(d => d.Venta).WithMany(v => v.Detalles)
                       .HasForeignKey(d => d.IdVenta).OnDelete(DeleteBehavior.Cascade);

                detalle.HasOne(d => d.Producto).WithMany()
                       .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
            });
        }

        // // Las tres columnas de auditoría son iguales en todas las entidades auditables.
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
