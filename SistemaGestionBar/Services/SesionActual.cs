using SistemaGestionBar.Models;

namespace SistemaGestionBar.Services
{
    public class SesionActual
    {
        public Usuario? Usuario { get; set; }

        public int? IdUsuario => Usuario?.IdUsuario;

        public bool EsAdministrador => Usuario?.EsAdministrador ?? false;
    }
}
