using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminUsuariosViewModel : SeccionAdminViewModel
    {
        public AdminUsuariosViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Usuarios, "Usuarios", "AccountKeyOutline",
                   "Cuentas de acceso al sistema: correo de inicio de sesión, rol y contraseña")
        {
            Usuarios = new ObservableCollection<Usuario>();
            Roles = new ObservableCollection<Rol>();
            OpcionesRol = new ObservableCollection<OpcionFiltro<Rol?>>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null && !EnEdicion);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null && !EnEdicion);
            IrAPersonasCommand = new RelayCommand(IrAPersonas, () => PuedeIrA(DestinoAdmin.Personas));

            Recargar();
        }

        public ObservableCollection<Usuario> Usuarios { get; }
        public ObservableCollection<Rol> Roles { get; }

        public ObservableCollection<OpcionFiltro<Rol?>> OpcionesRol { get; }

        private OpcionFiltro<Rol?>? _rolFiltro;
        public OpcionFiltro<Rol?>? RolFiltro
        {
            get => _rolFiltro;
            set
            {
                if (SetProperty(ref _rolFiltro, value))
                    RefrescarVista();
            }
        }

        public override bool HayFiltroAplicado =>
            HayBusqueda || (RolFiltro is not null && RolFiltro.Valor is not null);

        protected override bool Coincide(object item)
        {
            if (item is not Usuario usuario)
                return false;

            if (RolFiltro?.Valor is Rol rol && usuario.IdRol != rol.IdRol)
                return false;

            if (!HayBusqueda)
                return true;

            return Contiene(usuario.Email, Termino)
                || Contiene(usuario.NombreRol, Termino)
                || Contiene(usuario.Persona?.Nombre, Termino)
                || Contiene(usuario.Persona?.Apellido, Termino)
                || ContieneDocumento(usuario.Persona?.DniCuit, Termino);
        }

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            RolFiltro = OpcionesRol.FirstOrDefault();
        }

        public int CantidadAdministradores =>
            Usuarios.Count(u => u.NombreRol == RolesSistema.Administrador);

        public int CantidadEnSalon => Usuarios.Count(u => u.EsPersonalDeAtencion);

        protected override void RefrescarIndicadores()
        {
            OnPropertyChanged(nameof(CantidadAdministradores));
            OnPropertyChanged(nameof(CantidadEnSalon));
        }

        private Usuario? _seleccionado;

        public Usuario? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (EnEdicion)
                {
                    if (ReferenceEquals(_seleccionado, value))
                        return;

                    if (value is not null)
                        Fallar("Terminá la edición —guardá o cancelá— antes de elegir otra cuenta.");

                    OnPropertyChanged(nameof(Seleccionado));
                    return;
                }

                EstablecerSeleccion(value);
            }
        }

        private void EstablecerSeleccion(Usuario? usuario)
        {
            if (!SetProperty(ref _seleccionado, usuario, nameof(Seleccionado)))
                return;

            OnPropertyChanged(nameof(HaySeleccion));

            if (!EnEdicion)
                CargarEnEditor(usuario);
        }

        public bool HaySeleccion => Seleccionado is not null;

        private bool _enEdicion;
        public bool EnEdicion
        {
            get => _enEdicion;
            private set
            {
                if (SetProperty(ref _enEdicion, value))
                {
                    OnPropertyChanged(nameof(TituloEditor));
                    OnPropertyChanged(nameof(NoEnEdicion));
                    Revalidar();
                }
            }
        }

        public bool NoEnEdicion => !EnEdicion;

        protected override bool ValidacionActiva => EnEdicion;

        private bool _esAlta;

        public bool EsAlta => _esAlta;

        public string TituloEditor => !EnEdicion
            ? "Ficha de la cuenta"
            : _esAlta ? "Nueva cuenta de acceso" : $"Editando la cuenta de {_nombrePersona}";

        private string _dni = string.Empty;

        public string Dni
        {
            get => _dni;
            set
            {
                if (SetCampo(ref _dni, value))
                    BuscarEnElPadron();
            }
        }

        private Persona? _persona;

        private string _nombrePersona = string.Empty;

        public string NombrePersona
        {
            get => _nombrePersona;
            private set => SetProperty(ref _nombrePersona, value);
        }

        private string _contactoPersona = string.Empty;
        public string ContactoPersona
        {
            get => _contactoPersona;
            private set => SetProperty(ref _contactoPersona, value);
        }

        public bool HayPersona => _persona is not null;

        private bool _dniSinResultado;

        public bool DniSinResultado
        {
            get => _dniSinResultado;
            private set => SetProperty(ref _dniSinResultado, value);
        }

        private string _emailTrabajo = string.Empty;

        public string EmailTrabajo { get => _emailTrabajo; set => SetCampo(ref _emailTrabajo, value); }

        private Rol? _rolSeleccionado;
        public Rol? RolSeleccionado { get => _rolSeleccionado; set => SetCampo(ref _rolSeleccionado, value); }

        private string _claveNueva = string.Empty;
        public string ClaveNueva { get => _claveNueva; set => SetCampo(ref _claveNueva, value); }

        private Usuario? CuentaActual => _esAlta ? null : Seleccionado;

        public string AyudaClave => CuentaActual is null
            ? "Mínimo 8 caracteres."
            : "Dejar vacío para conservar la contraseña actual.";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }

        public ICommand IrAPersonasCommand { get; }

        protected override void DeclararReglas()
        {
            Requerido(Dni, nameof(Dni), "El DNI del titular");
            FormatoDni(Dni, nameof(Dni));

            if (!HayPersona && Dni.Count(char.IsDigit) >= DigitosDeDni)
                Agregar(nameof(Dni), "Ese DNI no está en el padrón.");

            Requerido(EmailTrabajo, nameof(EmailTrabajo), "El correo de trabajo");
            FormatoCorreo(EmailTrabajo, nameof(EmailTrabajo));
            LargoMaximo(EmailTrabajo, nameof(EmailTrabajo), "El correo de trabajo", Limites.Email);
            NoRepetido(EmailTrabajo, nameof(EmailTrabajo), OtrasCuentas().Select(u => u.Email),
                       "Ya hay otra cuenta con ese correo.");

            RequeridoElegir(RolSeleccionado, nameof(RolSeleccionado), "el rol");

            if (CuentaActual is null)
                Requerido(ClaveNueva, nameof(ClaveNueva), "La contraseña");

            LargoMinimo(ClaveNueva, nameof(ClaveNueva), "La contraseña", 8);
        }

        private IEnumerable<Usuario> OtrasCuentas() =>
            Usuarios.Where(u => u.IdUsuario != Seleccionado?.IdUsuario);

        private void BuscarEnElPadron()
        {
            if (!_esAlta || !EnEdicion)
                return;

            var encontrada = Repositorio.BuscarPersonaPorDocumento(Dni);

            if (encontrada is null)
            {
                EstablecerPersona(null);

                LimpiarMensaje();

                DniSinResultado = Dni.Count(char.IsDigit) >= DigitosDeDni;
                return;
            }

            DniSinResultado = false;

            var yaConCuenta = Usuarios.FirstOrDefault(u => u.IdPersona == encontrada.IdPersona);
            if (yaConCuenta is not null)
            {
                Cancelar();
                EstablecerSeleccion(Usuarios.FirstOrDefault(u => u.IdUsuario == yaConCuenta.IdUsuario));
                Informar($"{encontrada.NombreCompleto} ya tiene cuenta ({yaConCuenta.Email}). " +
                         "Se abrió su ficha: usá Editar para cambiarle el rol o la contraseña.");
                return;
            }

            EstablecerPersona(encontrada);
            Informar($"{encontrada.NombreCompleto} está en el padrón: la cuenta se va a crear a su nombre.");
        }

        private void EstablecerPersona(Persona? persona)
        {
            _persona = persona;

            NombrePersona = persona?.NombreCompleto ?? string.Empty;
            ContactoPersona = persona is null
                ? string.Empty
                : string.Join("  ·  ", new[] { persona.Email, persona.Telefono }
                                       .Where(d => !string.IsNullOrWhiteSpace(d))!);

            OnPropertyChanged(nameof(HayPersona));
            OnPropertyChanged(nameof(TituloEditor));
            Revalidar();
        }

        private void IrAPersonas() => IrA(DestinoAdmin.Personas, _persona);

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdUsuario;

            Usuarios.Clear();
            foreach (var usuario in Repositorio.ObtenerUsuarios())
                Usuarios.Add(usuario);

            Roles.Clear();
            foreach (var rol in Repositorio.ObtenerRoles())
                Roles.Add(rol);

            if (OpcionesRol.Count == 0)
            {
                OpcionesRol.Add(new OpcionFiltro<Rol?>("Todos los roles", null));
                foreach (var rol in Roles)
                    OpcionesRol.Add(new OpcionFiltro<Rol?>(rol.NombreRol, rol));
                _rolFiltro = OpcionesRol[0];
                OnPropertyChanged(nameof(RolFiltro));
            }

            ConfigurarFiltro(Usuarios);
            RefrescarVista();

            EstablecerSeleccion(Usuarios.FirstOrDefault(u => u.IdUsuario == idPrevio) ?? Usuarios.FirstOrDefault());
        }

        public override void Enfocar(object? foco)
        {
            if (foco is not Persona persona)
                return;

            var cuenta = Usuarios.FirstOrDefault(u => u.IdPersona == persona.IdPersona);
            if (cuenta is not null)
            {
                EstablecerSeleccion(cuenta);
                return;
            }

            Nuevo();
            Dni = Documento.Normalizar(persona.DniCuit);
        }

        private void CargarEnEditor(Usuario? usuario)
        {
            _dni = Documento.Normalizar(usuario?.Persona?.DniCuit);
            _emailTrabajo = usuario?.Email ?? string.Empty;
            _rolSeleccionado = usuario is null ? null : Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            _claveNueva = string.Empty;
            DniSinResultado = false;

            EstablecerPersona(usuario?.Persona);

            foreach (var propiedad in new[]
                     {
                         nameof(Dni), nameof(EmailTrabajo), nameof(RolSeleccionado),
                         nameof(ClaveNueva), nameof(TituloEditor), nameof(AyudaClave)
                     })
            {
                OnPropertyChanged(propiedad);
            }

            ReiniciarValidacion();
        }

        private void Nuevo()
        {
            LimpiarMensaje();
            _esAlta = true;
            OnPropertyChanged(nameof(EsAlta));
            _seleccionado = null;
            OnPropertyChanged(nameof(Seleccionado));
            OnPropertyChanged(nameof(HaySeleccion));
            CargarEnEditor(null);
            EnEdicion = true;
            ReiniciarValidacion();
        }

        private void Editar()
        {
            if (Seleccionado is null)
                return;

            LimpiarMensaje();
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            CargarEnEditor(Seleccionado);
            EnEdicion = true;
            ReiniciarValidacion();
        }

        private void Cancelar()
        {
            EnEdicion = false;
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            LimpiarMensaje();
            CargarEnEditor(Seleccionado);
        }

        private void Guardar()
        {
            if (!IntentarConfirmar())
            {
                Fallar(MensajeDeFormularioInvalido);
                return;
            }

            if (_persona is null)
            {
                Fallar("Escribí el DNI de una persona que ya esté en el padrón.");
                return;
            }

            var cuenta = _esAlta ? new Usuario() : Seleccionado;
            if (cuenta is null)
            {
                Fallar("No hay una cuenta seleccionada.");
                return;
            }

            cuenta.IdPersona = _persona.IdPersona;
            cuenta.IdRol = RolSeleccionado?.IdRol ?? 0;
            cuenta.Email = EmailTrabajo.Trim();

            if (!Aplicar(Repositorio.GuardarUsuario(cuenta, ClaveNueva)))
                return;

            EnEdicion = false;
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            Recargar();
            EstablecerSeleccion(Usuarios.FirstOrDefault(u => u.IdUsuario == cuenta.IdUsuario));
        }

        private void Eliminar()
        {
            if (Seleccionado is null)
                return;

            if (!ConfirmarEliminacion("la cuenta de", Seleccionado.NombreCompleto,
                                      "La persona sigue en el padrón: solo pierde el acceso al sistema."))
                return;

            if (Aplicar(Repositorio.EliminarUsuario(Seleccionado.IdUsuario)))
                Recargar();
        }
    }
}
