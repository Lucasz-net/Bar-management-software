using System.Collections.Generic;

namespace SistemaGestionBar.Models
{
    public static class DatosPruebaUsuarios
    {
        public static List<Usuario> ObtenerUsuariosMock()
        {
            return new List<Usuario>
            {
                new Usuario { IdUsuario = 1, Rol = "Administrador", Nombre = "Admin", Correo = "admin@bar.com", Clave = "12345678" },
                new Usuario { IdUsuario = 2, Rol = "Vendedor", Nombre = "Martina Gómez", Correo = "vendedor@bar.com", Clave = "12345678" }
            };
        }
    }
}
