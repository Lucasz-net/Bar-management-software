using System;
using System.Collections.Generic;
using System.Linq;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.Data
{
    /// <summary>
    /// Semilla de datos de prueba, con la MISMA forma que tendrán las tablas del DER.
    /// Cuando entre EF Core, este archivo se convierte casi textualmente en el
    /// método OnModelCreating(...).HasData(...) o en un Seeder de migración.
    ///
    /// Es una única fuente de verdad: todo el sistema lee de acá y las relaciones
    /// (navegación) quedan enlazadas al construirse, igual que haría el ORM.
    /// </summary>
    public class DatosPrueba
    {
        public List<Rol> Roles { get; } = new();
        public List<Persona> Personas { get; } = new();
        public List<Usuario> Usuarios { get; } = new();
        public List<Cliente> Clientes { get; } = new();
        public List<Categoria> Categorias { get; } = new();
        public List<Ingrediente> Ingredientes { get; } = new();
        public List<Producto> Productos { get; } = new();
        public List<ProductoIngrediente> ProductoIngredientes { get; } = new();
        public List<Receta> Recetas { get; } = new();
        public List<MetodoPago> MetodosPago { get; } = new();
        public List<Ubicacion> Ubicaciones { get; } = new();
        public List<Venta> Ventas { get; } = new();
        public List<Reporte> Reportes { get; } = new();
        public List<Factura> Facturas { get; } = new();

        /// <summary>Clave en texto plano de los tres usuarios de prueba.</summary>
        public const string ClaveDemo = "12345678";

        /// <summary>
        /// Ruta de la foto del producto. Los archivos viven en Assets/Productos/
        /// con Build Action = Resource (ver el .csproj).
        ///
        /// Se usa la pack URI completa con el nombre del ensamblado y no la forma corta
        /// "/Assets/...": la relativa se resuelve contra el ensamblado DE ENTRADA, asi que
        /// deja de funcionar apenas otro proyecto (un tester, un host) arranca la aplicacion.
        /// Cuando entre la base de datos, esta cadena es el valor de Producto.ruta_imagen.
        /// </summary>
        private static string RutaFoto(string archivo) =>
            $"pack://application:,,,/SistemaGestionBar;component/Assets/Productos/{archivo}.png";

        public static DatosPrueba Crear()
        {
            var datos = new DatosPrueba();
            datos.CargarSeguridad();
            datos.CargarParametricas();
            datos.CargarInventario();
            datos.CargarCatalogo();
            datos.CargarRecetas();
            datos.EnlazarNavegacion();
            return datos;
        }

        // ---------------------------------------------------------------
        // Rol / Persona / Usuario / Cliente
        // ---------------------------------------------------------------
        private void CargarSeguridad()
        {
            Roles.AddRange(new[]
            {
                new Rol { IdRol = 1, NombreRol = RolesSistema.Administrador },
                new Rol { IdRol = 2, NombreRol = RolesSistema.Vendedor },
                new Rol { IdRol = 3, NombreRol = RolesSistema.Mesero },
                new Rol { IdRol = 4, NombreRol = RolesSistema.Gerente }
            });

            Personas.AddRange(new[]
            {
                new Persona { IdPersona = 1, Nombre = "Nazareno Villalba",  DniCuit = "40.123.456", Telefono = "351-5550101", Email = "admin@bar.com" },
                new Persona { IdPersona = 2, Nombre = "Martina Gómez",  DniCuit = "38.987.654", Telefono = "351-5550102", Email = "vendedor@bar.com" },
                new Persona { IdPersona = 3, Nombre = "Jose Hernandez",   DniCuit = "41.222.333", Telefono = "351-5550103", Email = "mesero@bar.com" },
                new Persona { IdPersona = 4, Nombre = "Consumidor Final" },
                new Persona { IdPersona = 5, Nombre = "Sofía Ramírez",  DniCuit = "37.444.555", Telefono = "351-5550104", Email = "sofia@mail.com" },
                new Persona { IdPersona = 6, Nombre = "Bar El Ancla SRL", DniCuit = "30-71234567-9", Telefono = "351-5550105", Email = "compras@elancla.com" },
                new Persona { IdPersona = 7, Nombre = "Diego Ferrari", DniCuit = "35.666.777", Telefono = "351-5550106", Email = "gerente@bar.com" }
            });

            // Las claves se hashean igual que lo hará el alta real de usuarios (RF-09).
            Usuarios.AddRange(new[]
            {
                new Usuario { IdUsuario = 1, IdPersona = 1, IdRol = 1, Clave = SeguridadHelper.GenerarHash(ClaveDemo) },
                new Usuario { IdUsuario = 2, IdPersona = 2, IdRol = 2, Clave = SeguridadHelper.GenerarHash(ClaveDemo) },
                new Usuario { IdUsuario = 3, IdPersona = 3, IdRol = 3, Clave = SeguridadHelper.GenerarHash(ClaveDemo) },
                new Usuario { IdUsuario = 4, IdPersona = 7, IdRol = 4, Clave = SeguridadHelper.GenerarHash(ClaveDemo) }
            });

            Clientes.AddRange(new[]
            {
                new Cliente { IdCliente = 1, IdPersona = 4, FechaRegistro = new DateTime(2026, 1, 1) },
                new Cliente { IdCliente = 2, IdPersona = 5, FechaRegistro = new DateTime(2026, 3, 14) },
                new Cliente { IdCliente = 3, IdPersona = 6, FechaRegistro = new DateTime(2026, 5, 2) }
            });
        }

        // ---------------------------------------------------------------
        // Tablas paramétricas: Categoria / Metodo_Pago / Ubicacion
        // ---------------------------------------------------------------
        private void CargarParametricas()
        {
            // Tres categorías para el catálogo del vendedor. "Botellas" agrupa
            // cervezas, vinos y destilados: todo lo que se vende cerrado, sin preparar.
            Categorias.AddRange(new[]
            {
                new Categoria { IdCategoria = 1, NombreCategoria = "Cócteles" },
                new Categoria { IdCategoria = 2, NombreCategoria = "Sin Alcohol" },
                new Categoria { IdCategoria = 3, NombreCategoria = "Botellas" }
            });

            MetodosPago.AddRange(new[]
            {
                new MetodoPago { IdMetodoPago = 1, NombreMetodo = "Efectivo" },
                new MetodoPago { IdMetodoPago = 2, NombreMetodo = "Tarjeta de Débito" },
                new MetodoPago { IdMetodoPago = 3, NombreMetodo = "Tarjeta de Crédito" },
                new MetodoPago { IdMetodoPago = 4, NombreMetodo = "Transferencia / QR" }
            });

            // Una sola barra: la atiende el barman, por eso no lleva mesero (RF-02).
            Ubicaciones.AddRange(new[]
            {
                new Ubicacion { IdUbicacion = 1, NombreUbicacion = "Mesa 1", Capacidad = 4, Tipo = TipoUbicacion.Mesa,  Estado = EstadoUbicacion.Libre },
                new Ubicacion { IdUbicacion = 2, NombreUbicacion = "Mesa 2", Capacidad = 4, Tipo = TipoUbicacion.Mesa,  Estado = EstadoUbicacion.Libre },
                new Ubicacion { IdUbicacion = 3, NombreUbicacion = "Mesa 3", Capacidad = 6, Tipo = TipoUbicacion.Mesa,  Estado = EstadoUbicacion.Ocupada },
                new Ubicacion { IdUbicacion = 4, NombreUbicacion = "Mesa 4", Capacidad = 2, Tipo = TipoUbicacion.Mesa,  Estado = EstadoUbicacion.Libre },
                new Ubicacion { IdUbicacion = 5, NombreUbicacion = "Mesa 5", Capacidad = 6, Tipo = TipoUbicacion.Mesa,  Estado = EstadoUbicacion.Libre },
                new Ubicacion { IdUbicacion = 6, NombreUbicacion = "Barra",  Capacidad = 8, Tipo = TipoUbicacion.Barra, Estado = EstadoUbicacion.Libre }
            });
        }

        // ---------------------------------------------------------------
        // Ingrediente
        // ---------------------------------------------------------------
        private void CargarInventario()
        {
            Ingredientes.AddRange(new[]
            {
                new Ingrediente { IdIngrediente = 1,  Nombre = "Ron Blanco",     UnidadMedida = "ml",     Stock = 3000, StockMinimo = 500 },
                new Ingrediente { IdIngrediente = 2,  Nombre = "Menta fresca",   UnidadMedida = "hojas",  Stock = 180,  StockMinimo = 40 },
                new Ingrediente { IdIngrediente = 3,  Nombre = "Jugo de lima",   UnidadMedida = "ml",     Stock = 1500, StockMinimo = 300 },
                new Ingrediente { IdIngrediente = 4,  Nombre = "Azúcar",         UnidadMedida = "g",      Stock = 5000, StockMinimo = 500 },
                new Ingrediente { IdIngrediente = 5,  Nombre = "Soda",           UnidadMedida = "ml",     Stock = 4000, StockMinimo = 1000 },
                new Ingrediente { IdIngrediente = 6,  Nombre = "Tequila",        UnidadMedida = "ml",     Stock = 2000, StockMinimo = 500 },
                new Ingrediente { IdIngrediente = 7,  Nombre = "Triple Sec",     UnidadMedida = "ml",     Stock = 280,  StockMinimo = 300 },
                new Ingrediente { IdIngrediente = 8,  Nombre = "Jugo de limón",  UnidadMedida = "ml",     Stock = 1800, StockMinimo = 300 },
                new Ingrediente { IdIngrediente = 9,  Nombre = "Whisky",         UnidadMedida = "ml",     Stock = 2500, StockMinimo = 500 },
                new Ingrediente { IdIngrediente = 10, Nombre = "Almíbar",        UnidadMedida = "ml",     Stock = 900,  StockMinimo = 200 },
                new Ingrediente { IdIngrediente = 11, Nombre = "Fernet",         UnidadMedida = "ml",     Stock = 4000, StockMinimo = 800 },
                new Ingrediente { IdIngrediente = 12, Nombre = "Gaseosa cola",   UnidadMedida = "ml",     Stock = 8000, StockMinimo = 1500 },
                new Ingrediente { IdIngrediente = 13, Nombre = "Gin",            UnidadMedida = "ml",     Stock = 1600, StockMinimo = 400 },
                new Ingrediente { IdIngrediente = 14, Nombre = "Agua tónica",    UnidadMedida = "ml",     Stock = 2400, StockMinimo = 600 },
                new Ingrediente { IdIngrediente = 15, Nombre = "Pepino",         UnidadMedida = "rodajas", Stock = 45,  StockMinimo = 15 },
                new Ingrediente { IdIngrediente = 16, Nombre = "Hielo",          UnidadMedida = "cubos",  Stock = 600,  StockMinimo = 100 }
            });
        }

        // ---------------------------------------------------------------
        // Producto
        // ---------------------------------------------------------------
        private void CargarCatalogo()
        {
            Productos.AddRange(new[]
            {
                // Botellas (IdCategoria 3): venta directa, descuentan de Producto.Stock
                new Producto { IdProducto = 1, IdCategoria = 3, Nombre = "Corona Extra 710ml", Descripcion = "Cerveza lager mexicana, botella de 710 ml.", Precio = 3500, Stock = 48, StockMinimo = 12, RutaImagen = RutaFoto("corona-extra") },
                new Producto { IdProducto = 2, IdCategoria = 3, Nombre = "Heineken 473ml",     Descripcion = "Lager holandesa en lata de 473 ml.",         Precio = 3200, Stock = 36, StockMinimo = 12, RutaImagen = RutaFoto("heineken") },
                new Producto { IdProducto = 3, IdCategoria = 3, Nombre = "Patagonia Amber",    Descripcion = "Amber lager artesanal, botella de 730 ml.",  Precio = 4200, Stock = 6,  StockMinimo = 10, RutaImagen = RutaFoto("patagonia-amber") },

                // Cócteles (IdCategoria 1): se preparan, descuentan de Ingrediente
                new Producto { IdProducto = 4, IdCategoria = 1, Nombre = "Mojito", Descripcion = "Ron blanco, menta fresca, lima y soda.", Precio = 6500, RutaImagen = RutaFoto("mojito") },
                new Producto { IdProducto = 5, IdCategoria = 1, Nombre = "Margarita", Descripcion = "Tequila, triple sec y jugo de limón.", Precio = 7000, RutaImagen = RutaFoto("margarita") },
                new Producto { IdProducto = 6, IdCategoria = 1, Nombre = "Whisky Sour", Descripcion = "Whisky, limón y almíbar.", Precio = 8500, RutaImagen = RutaFoto("whisky-sour") },
                new Producto { IdProducto = 7, IdCategoria = 1, Nombre = "Fernet con Coca", Descripcion = "El clásico: 30% fernet, 70% cola.", Precio = 5500, RutaImagen = RutaFoto("fernet-con-coca") },
                new Producto { IdProducto = 8, IdCategoria = 1, Nombre = "Gin Tonic de Pepino", Descripcion = "Gin artesanal, tónica y pepino.", Precio = 7200, RutaImagen = RutaFoto("gin-tonic") },

                // Sin Alcohol (IdCategoria 2)
                new Producto { IdProducto = 9, IdCategoria = 2, Nombre = "Limonada de Menta", Descripcion = "Limón exprimido, menta y soda.", Precio = 3000, RutaImagen = RutaFoto("limonada") },
                new Producto { IdProducto = 10, IdCategoria = 2, Nombre = "Agua Mineral 500ml", Descripcion = "Agua mineral sin gas.", Precio = 1800, Stock = 60, StockMinimo = 15, RutaImagen = RutaFoto("agua-mineral") },
                new Producto { IdProducto = 11, IdCategoria = 2, Nombre = "Gaseosa Línea Cola", Descripcion = "Botella de 500 ml.", Precio = 2200, Stock = 72, StockMinimo = 20, RutaImagen = RutaFoto("gaseosa-cola") },

                // Botellas (IdCategoria 3): destilados y vinos
                new Producto { IdProducto = 12, IdCategoria = 3, Nombre = "Vodka Smirnoff 700ml", Descripcion = "Botella cerrada de 700 ml.", Precio = 12000, Stock = 12, StockMinimo = 4, RutaImagen = RutaFoto("vodka-smirnoff") },
                new Producto { IdProducto = 13, IdCategoria = 3, Nombre = "Vino Malbec 750ml", Descripcion = "Malbec de Mendoza, cosecha 2023.", Precio = 9500, Stock = 18, StockMinimo = 5, RutaImagen = RutaFoto("vino-malbec") }
            });
        }

        // ---------------------------------------------------------------
        // Producto_Ingrediente (que lleva y cuanto) + Receta (como se prepara).
        // Solo tienen filas los productos que se preparan: una botella de vino
        // no aparece en ninguna de las dos tablas.
        // ---------------------------------------------------------------
        private void CargarRecetas()
        {
            void Composicion(int idProducto, params (int IdIngrediente, decimal Cantidad)[] items)
            {
                foreach (var item in items)
                {
                    ProductoIngredientes.Add(new ProductoIngrediente
                    {
                        IdProducto = idProducto,
                        IdIngrediente = item.IdIngrediente,
                        CantidadNecesaria = item.Cantidad
                    });
                }
            }

            Composicion(4, (1, 50), (2, 8), (3, 25), (4, 10), (5, 100), (16, 6));   // Mojito
            Composicion(5, (6, 50), (7, 25), (8, 25), (16, 6));                     // Margarita
            Composicion(6, (9, 50), (8, 25), (10, 15), (16, 6));                    // Whisky Sour
            Composicion(7, (11, 70), (12, 230), (16, 8));                           // Fernet con Coca
            Composicion(8, (13, 60), (14, 200), (15, 3), (16, 6));                  // Gin Tonic de Pepino
            Composicion(9, (8, 60), (4, 15), (2, 6), (5, 200), (16, 6));            // Limonada de Menta

            // Tabla Receta: solo el procedimiento. Los ingredientes ya quedaron arriba.
            Recetas.Add(new Receta { IdProducto = 4, Instrucciones =   // Mojito
                "1. Colocar la menta con el azúcar y el jugo de lima en el vaso y macerar sin romper las hojas.\n2. Agregar el ron y llenar con hielo.\n3. Completar con soda y revolver de abajo hacia arriba.\n4. Decorar con un gajo de lima y un ramito de menta." });
            Recetas.Add(new Receta { IdProducto = 5, Instrucciones =   // Margarita
                "1. Escarchar el borde de la copa con sal.\n2. Volcar tequila, triple sec y jugo de limón en la coctelera con hielo.\n3. Agitar 12 segundos hasta que la coctelera se escarche.\n4. Servir colado en la copa, sin hielo." });
            Recetas.Add(new Receta { IdProducto = 6, Instrucciones =   // Whisky Sour
                "1. Volcar whisky, jugo de limón y almíbar en la coctelera.\n2. Agitar en seco (sin hielo) para integrar.\n3. Agregar hielo y agitar de nuevo hasta enfriar.\n4. Servir colado en vaso bajo con un hielo grande." });
            Recetas.Add(new Receta { IdProducto = 7, Instrucciones =   // Fernet con Coca
                "1. Llenar el vaso con hielo hasta el tope.\n2. Servir el fernet primero.\n3. Completar con la gaseosa cola inclinando el vaso para no perder gas.\n4. No revolver: se mezcla solo." });
            Recetas.Add(new Receta { IdProducto = 8, Instrucciones =   // Gin Tonic de Pepino
                "1. Enfriar la copa balón con hielo y descartar el agua.\n2. Servir el gin sobre hielo nuevo.\n3. Completar con la tónica bien fría.\n4. Agregar las rodajas de pepino y remover una sola vez." });
            Recetas.Add(new Receta { IdProducto = 9, Instrucciones =   // Limonada de Menta
                "1. Macerar la menta con el azúcar.\n2. Agregar el jugo de limón y mezclar hasta disolver.\n3. Llenar con hielo y completar con soda." });
        }

        // ---------------------------------------------------------------
        // Enlaza las propiedades de navegación, tal como haría el ORM al hacer Include().
        // ---------------------------------------------------------------
        private void EnlazarNavegacion()
        {
            foreach (var usuario in Usuarios)
            {
                usuario.Persona = Personas.First(p => p.IdPersona == usuario.IdPersona);
                usuario.Rol = Roles.First(r => r.IdRol == usuario.IdRol);
                usuario.Persona.Usuario = usuario;
                usuario.Rol.Usuarios.Add(usuario);
            }

            foreach (var cliente in Clientes)
            {
                cliente.Persona = Personas.First(p => p.IdPersona == cliente.IdPersona);
                cliente.Persona.Cliente = cliente;
            }

            foreach (var producto in Productos)
            {
                producto.Categoria = Categorias.First(c => c.IdCategoria == producto.IdCategoria);
                producto.Categoria.Productos.Add(producto);
            }

            foreach (var receta in Recetas)
            {
                receta.Producto = Productos.First(p => p.IdProducto == receta.IdProducto);
                receta.Producto.Receta = receta;
            }

            foreach (var receta in ProductoIngredientes)
            {
                receta.Producto = Productos.First(p => p.IdProducto == receta.IdProducto);
                receta.Ingrediente = Ingredientes.First(i => i.IdIngrediente == receta.IdIngrediente);
                receta.Producto.Ingredientes.Add(receta);
            }
        }
    }
}
