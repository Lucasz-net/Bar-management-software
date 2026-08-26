using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    public class DatosPrueba
    {
        public static List<Producto> ObtenerProductosMock()
        {
            return new List<Producto>
            {
                new Producto { IdProducto = 1, Nombre = "Fernet con Coca", Descripcion = "Trago clásico (30% Fernet, 70% Cola).", Precio = 4500, IdCategoria = 1 },
                new Producto { IdProducto = 2, Nombre = "Patagonia Amber Lager", Descripcion = "Cerveza en botella de 730ml.", Precio = 3200, IdCategoria = 2 },
                new Producto { IdProducto = 3, Nombre = "Mojito Tradicional", Descripcion = "Ron blanco, menta fresca, lima y soda.", Precio = 3800, IdCategoria = 1 },
                new Producto { IdProducto = 4, Nombre = "Gin Tonic Pepino", Descripcion = "Gin artesanal, agua tónica y pepino.", Precio = 4200, IdCategoria = 1 },
                new Producto { IdProducto = 5, Nombre = "Vodka Smirnoff (Botella)", Descripcion = "Botella cerrada de 700ml.", Precio = 12000, IdCategoria = 2 }
            };
        }
    }
}