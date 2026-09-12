using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;
using System.ComponentModel;
using System.Windows.Data;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// ABM de usuarios (RF-12) sobre el par Persona + Usuario.
    /// Los datos personales van a Persona y las credenciales a Usuario (RF-10):
    /// una sola pantalla escribe las dos tablas.
    /// </summary>
    public class AdminUsuariosViewModel : SeccionAdminViewModel
    {
        public AdminUsuariosViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, "Usuarios", "AccountGroupOutline",
                   "Empleados del bar, sus datos personales y su perfil de acceso")
        {
            Usuarios = new ObservableCollection<Usuario>();
            Roles = new ObservableCollection<Rol>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null && !EnEdicion);

            Recargar();
        }

        public ObservableCollection<Usuario> Usuarios { get; }
        public ObservableCollection<Rol> Roles { get; }
        private ICollectionView? _usuariosView;
        public ICollectionView? UsuariosView => _usuariosView;

        private string _filtro = string.Empty;
        public string Filtro
        {
            get => _filtro;
            set
            {
                if (SetProperty(ref _filtro, value))
                {
                    _usuariosView?.Refresh();
                }
            }
        }

        private Usuario? _seleccionado;
        public Usuario? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (SetProperty(ref _seleccionado, value) && !EnEdicion)
                    CargarEnEditor(value);
            }
        }

        private bool _enEdicion;
        public bool EnEdicion
        {
            get => _enEdicion;
            private set
            {
                if (SetProperty(ref _enEdicion, value))
                {
                    OnPropertyChanged(nameof(TituloEditor));
                    OnPropertyChanged(nameof(AyudaClave));
                }
            }
        }

        private bool _esAlta;
        public string TituloEditor => !EnEdicion
            ? "Detalle del usuario"
            : _esAlta ? "Nuevo usuario" : $"Editando: {_nombre}";

        public string AyudaClave => _esAlta
            ? "Mínimo 8 caracteres."
            : "Dejar vacío para conservar la contraseña actual.";

        private string _nombre = string.Empty;
        public string Nombre { get => _nombre; set => SetProperty(ref _nombre, value); }

        private string _dniCuit = string.Empty;
        public string DniCuit { get => _dniCuit; set => SetProperty(ref _dniCuit, value); }

        private string _telefono = string.Empty;
        public string Telefono { get => _telefono; set => SetProperty(ref _telefono, value); }

        private string _email = string.Empty;
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private Rol? _rolSeleccionado;
        public Rol? RolSeleccionado { get => _rolSeleccionado; set => SetProperty(ref _rolSeleccionado, value); }

        private string _claveNueva = string.Empty;
        public string ClaveNueva { get => _claveNueva; set => SetProperty(ref _claveNueva, value); }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdUsuario;

            Usuarios.Clear();
            foreach (var u in Repositorio.ObtenerUsuarios())
                Usuarios.Add(u);

            if (_usuariosView is null)
            {
                _usuariosView = CollectionViewSource.GetDefaultView(Usuarios);
                _usuariosView.Filter = o => FiltrarUsuario(o as Usuario);
            }
            else
            {
                _usuariosView.Refresh();
            }

            Roles.Clear();
            foreach (var r in Repositorio.ObtenerRoles())
                Roles.Add(r);

            Seleccionado = Usuarios.FirstOrDefault(u => u.IdUsuario == idPrevio) ?? Usuarios.FirstOrDefault();
        }

        private bool FiltrarUsuario(Usuario? u)
        {
            if (u is null)
                return false;

            if (string.IsNullOrWhiteSpace(Filtro))
                return true;

            var q = Filtro.Trim();

            if (!string.IsNullOrWhiteSpace(u.Persona?.Nombre) && u.Persona.Nombre.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(u.Persona?.Email) && u.Persona.Email.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(u.Persona?.DniCuit) && u.Persona.DniCuit.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(u.Rol?.NombreRol) && u.Rol.NombreRol.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void CargarEnEditor(Usuario? usuario)
        {
            Nombre = usuario?.Persona?.Nombre ?? string.Empty;
            DniCuit = usuario?.Persona?.DniCuit ?? string.Empty;
            Telefono = usuario?.Persona?.Telefono ?? string.Empty;
            Email = usuario?.Persona?.Email ?? string.Empty;
            ClaveNueva = string.Empty;
            RolSeleccionado = usuario is null
                ? Roles.FirstOrDefault()
                : Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
        }

        private void Nuevo()
        {
            LimpiarMensaje();
            _esAlta = true;
            EnEdicion = true;
            Seleccionado = null;
            CargarEnEditor(null);
        }

        private void Editar()
        {
            if (Seleccionado is null)
                return;

            LimpiarMensaje();
            _esAlta = false;
            EnEdicion = true;
            CargarEnEditor(Seleccionado);
        }

        private void Cancelar()
        {
            EnEdicion = false;
            _esAlta = false;
            LimpiarMensaje();
            CargarEnEditor(Seleccionado);
        }

        private void Guardar()
        {
            Usuario usuario;

            if (_esAlta)
            {
                usuario = new Usuario { Persona = new Persona() };
            }
            else
            {
                if (Seleccionado is null)
                {
                    Fallar("No hay un usuario seleccionado.");
                    return;
                }
                usuario = Seleccionado;
            }

            usuario.Persona.Nombre = Nombre;
            usuario.Persona.DniCuit = string.IsNullOrWhiteSpace(DniCuit) ? null : DniCuit;
            usuario.Persona.Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono;
            usuario.Persona.Email = Email;
            usuario.IdRol = RolSeleccionado?.IdRol ?? 0;

            if (!Aplicar(Repositorio.GuardarUsuario(usuario, ClaveNueva)))
                return;

            EnEdicion = false;
            _esAlta = false;
            ClaveNueva = string.Empty;
            Recargar();
            Seleccionado = Usuarios.FirstOrDefault(u => u.IdUsuario == usuario.IdUsuario);
        }

        private void Eliminar()
        {
            if (Seleccionado is null)
                return;

            if (!Dialogo.Confirmar("Eliminar usuario",
                    $"¿Dar de baja el acceso de {Seleccionado.NombreCompleto}?"))
                return;

            if (Aplicar(Repositorio.EliminarUsuario(Seleccionado.IdUsuario)))
                Recargar();
        }
    }
}
