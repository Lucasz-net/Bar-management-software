using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Cuentas de acceso al sistema (RF-09 y RF-12).
    ///
    /// Una cuenta no es una persona: es el permiso de una persona <b>que ya está en el
    /// padrón</b> para entrar al sistema. Por eso acá no se escribe un solo dato personal.
    /// Lo que se define es el correo de trabajo con el que inicia sesión, el rol que tiene
    /// y su contraseña.
    ///
    /// <b>Cómo se elige a quién darle la cuenta.</b> Por DNI, igual que en Personas: se
    /// teclea el documento y, si está en el padrón, aparecen sus datos. Si no está, no se
    /// inventa una persona desde acá —eso sería volver a mezclar las dos pantallas—: se
    /// avisa y se ofrece el salto a Personas para darla de alta.
    /// </summary>
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

        // ---------------------------------------------------------------
        // Filtros
        // ---------------------------------------------------------------
        /// <summary>Roles para el combo de filtro, con "Todos" adelante.</summary>
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

        // ---------------------------------------------------------------
        // Indicadores del encabezado
        // ---------------------------------------------------------------
        public int CantidadAdministradores =>
            Usuarios.Count(u => u.NombreRol == RolesSistema.Administrador);

        public int CantidadEnSalon => Usuarios.Count(u => u.EsPersonalDeAtencion);

        protected override void RefrescarIndicadores()
        {
            OnPropertyChanged(nameof(CantidadAdministradores));
            OnPropertyChanged(nameof(CantidadEnSalon));
        }

        // ---------------------------------------------------------------
        // Selección
        // ---------------------------------------------------------------
        private Usuario? _seleccionado;

        /// <summary>
        /// Con el formulario abierto la selección queda congelada, igual que en el resto
        /// del tablero: moverla dejaría remarcada una fila que no es la que se está editando.
        /// </summary>
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

        /// <summary>
        /// En un alta el DNI se teclea; editando, no: cambiar de titular no es editar una
        /// cuenta, es darle de baja a uno y de alta a otro. Y las ventas ya registradas
        /// apuntan a esta cuenta.
        /// </summary>
        public bool EsAlta => _esAlta;

        public string TituloEditor => !EnEdicion
            ? "Ficha de la cuenta"
            : _esAlta ? "Nueva cuenta de acceso" : $"Editando la cuenta de {_nombrePersona}";

        // ---------------------------------------------------------------
        // La persona titular de la cuenta
        // ---------------------------------------------------------------
        private string _dni = string.Empty;

        /// <summary>
        /// Documento del titular. Al escribirlo completo se busca en el padrón: si está,
        /// se muestran sus datos y la cuenta queda enganchada a esa ficha; si no está,
        /// se avisa y se ofrece darla de alta en Personas.
        /// </summary>
        public string Dni
        {
            get => _dni;
            set
            {
                if (SetCampo(ref _dni, value))
                    BuscarEnElPadron();
            }
        }

        /// <summary>La persona que el DNI encontró. Sin ella no hay cuenta que guardar.</summary>
        private Persona? _persona;

        private string _nombrePersona = string.Empty;

        /// <summary>Nombre del titular, de solo lectura: se edita en Personas.</summary>
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

        /// <summary>Se buscó un DNI completo y el padrón no lo tiene: hay que darla de alta.</summary>
        public bool DniSinResultado
        {
            get => _dniSinResultado;
            private set => SetProperty(ref _dniSinResultado, value);
        }

        // ---------------------------------------------------------------
        // Datos de la cuenta
        // ---------------------------------------------------------------
        private string _emailTrabajo = string.Empty;

        /// <summary>Correo del bar: es el nombre de usuario con el que se inicia sesión (RF-09).</summary>
        public string EmailTrabajo { get => _emailTrabajo; set => SetCampo(ref _emailTrabajo, value); }

        private Rol? _rolSeleccionado;
        public Rol? RolSeleccionado { get => _rolSeleccionado; set => SetCampo(ref _rolSeleccionado, value); }

        private string _claveNueva = string.Empty;
        public string ClaveNueva { get => _claveNueva; set => SetCampo(ref _claveNueva, value); }

        /// <summary>La cuenta que se está editando, o null si es un alta.</summary>
        private Usuario? CuentaActual => _esAlta ? null : Seleccionado;

        public string AyudaClave => CuentaActual is null
            ? "Mínimo 8 caracteres."
            : "Dejar vacío para conservar la contraseña actual.";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }

        /// <summary>Salto al padrón para dar de alta al titular que todavía no existe.</summary>
        public ICommand IrAPersonasCommand { get; }

        // ---------------------------------------------------------------
        // Validación
        // ---------------------------------------------------------------
        protected override void DeclararReglas()
        {
            Requerido(Dni, nameof(Dni), "El DNI del titular");
            FormatoDni(Dni, nameof(Dni));

            // Recién con el documento completo: a medio teclear, "no está en el padrón"
            // es cierto y no aporta nada más que ruido rojo debajo del campo.
            if (!HayPersona && Dni.Count(char.IsDigit) >= DigitosDeDni)
                Agregar(nameof(Dni), "Ese DNI no está en el padrón.");

            Requerido(EmailTrabajo, nameof(EmailTrabajo), "El correo de trabajo");
            FormatoCorreo(EmailTrabajo, nameof(EmailTrabajo));
            NoRepetido(EmailTrabajo, nameof(EmailTrabajo), OtrasCuentas().Select(u => u.Email),
                       "Ya hay otra cuenta con ese correo.");

            RequeridoElegir(RolSeleccionado, nameof(RolSeleccionado), "el rol");

            if (CuentaActual is null)
                Requerido(ClaveNueva, nameof(ClaveNueva), "La contraseña");

            LargoMinimo(ClaveNueva, nameof(ClaveNueva), "La contraseña", 8);
        }

        private IEnumerable<Usuario> OtrasCuentas() =>
            Usuarios.Where(u => u.IdUsuario != Seleccionado?.IdUsuario);

        // ---------------------------------------------------------------
        // Búsqueda del titular en el padrón
        // ---------------------------------------------------------------
        /// <summary>
        /// Solo en un alta. Trae del padrón a la persona del DNI tecleado y, si ya tiene
        /// cuenta, lo dice en vez de dejar que el usuario complete el formulario para que
        /// el repositorio lo rechace al final.
        /// </summary>
        private void BuscarEnElPadron()
        {
            if (!_esAlta || !EnEdicion)
                return;

            var encontrada = Repositorio.BuscarPersonaPorDocumento(Dni);

            if (encontrada is null)
            {
                EstablecerPersona(null);

                // Sin esto queda arriba el "fulano está en el padrón" del DNI anterior,
                // en verde, contradiciendo al panel que dice que este no está.
                LimpiarMensaje();

                // El aviso recién tiene sentido con el documento completo: a medio
                // teclear, "no existe" es cierto y no sirve para nada.
                //
                // No se toca la barra de mensajes: el panel que aparece debajo del campo
                // ya lo dice y encima trae el botón para ir a cargarla. Decirlo también
                // arriba era el mismo texto tres veces en la misma pantalla.
                DniSinResultado = Dni.Count(char.IsDigit) >= DigitosDeDni;
                return;
            }

            DniSinResultado = false;

            var yaConCuenta = Usuarios.FirstOrDefault(u => u.IdPersona == encontrada.IdPersona);
            if (yaConCuenta is not null)
            {
                // No se abandona la edición: se corta el alta y se muestra la cuenta que
                // ya existe, que es lo que el usuario venía a buscar sin saberlo.
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

        // ---------------------------------------------------------------
        // Ciclo de vida
        // ---------------------------------------------------------------
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

        /// <summary>
        /// Llega acá el salto desde Personas. Si esa persona ya tiene cuenta se la
        /// selecciona; si no, se abre el alta con su DNI puesto, que es lo que se quiere
        /// hacer justo después de dar de alta a un empleado en el padrón.
        /// </summary>
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
