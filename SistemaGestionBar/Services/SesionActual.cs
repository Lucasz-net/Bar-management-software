using SistemaGestionBar.Models;

namespace SistemaGestionBar.Services
{
    /// <summary>
    /// Quién está usando el sistema en este momento.
    /// El repositorio lo consulta para completar los campos de auditoría (RF-11)
    /// sin que cada pantalla tenga que ir pasando el id del usuario a mano.
    /// Cuando entre EF Core, esto es lo que lee el interceptor de SaveChanges.
    /// </summary>
    public class SesionActual
    {
        public Usuario? Usuario { get; set; }

        public int? IdUsuario => Usuario?.IdUsuario;

        public bool EsAdministrador => Usuario?.EsAdministrador ?? false;
    }
}
