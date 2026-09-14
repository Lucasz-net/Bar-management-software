using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Personas y cuentas de acceso en una sola pantalla (RF-10 y RF-12).
    ///
    /// El sistema tiene un solo padrón de personas. Sobre esa misma persona se activan
    /// dos cosas independientes: ser <b>cliente</b> del bar y tener <b>cuenta</b> para
    /// entrar al sistema. Por eso es una sección y no dos: son dos casilleros de la misma
    /// ficha, y separarlos obligaba a ir y volver entre pantallas para dar de alta a un
    /// empleado que además es cliente.
    ///
    /// El <b>DNI/CUIT es el dato identificatorio</b>: al escribirlo en un alta, si esa
    /// persona ya está en el padrón se traen sus datos en vez de duplicarla.
    /// </summary>
    public class AdminPersonalViewModel : SeccionAdminViewModel
    {
        public AdminPersonalViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Personas, "Personas", "AccountGroupOutline",
                   "Padrón de clientes y empleados, y las cuentas de acceso al sistema")
        {
            Personas = new ObservableCollection<Persona>();
            Roles = new ObservableCollection<Rol>();
            OpcionesRol = new ObservableCollection<OpcionFiltro<Rol?>>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionada is not null && !EnEdicion);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionada is not null && !EnEdicion);

            Recargar();
        }

        public ObservableCollection<Persona> Personas { get; }
        public ObservableCollection<Rol> Roles { get; }

        // ---------------------------------------------------------------
        // Filtros
        // ---------------------------------------------------------------
        public IReadOnlyList<OpcionFiltro<VinculoPersona>> OpcionesVinculo { get; } = new[]
        {
            new OpcionFiltro<VinculoPersona>("Todas", VinculoPersona.Todas),
            new OpcionFiltro<VinculoPersona>("Clientes", VinculoPersona.Clientes),
            new OpcionFiltro<VinculoPersona>("Empleados", VinculoPersona.Empleados),
            new OpcionFiltro<VinculoPersona>("Sin vínculo", VinculoPersona.SinVinculo)
        };

        private OpcionFiltro<VinculoPersona>? _vinculo;
        public OpcionFiltro<VinculoPersona>? Vinculo
        {
            get => _vinculo;
            set
            {
                if (SetProperty(ref _vinculo, value))
                    RefrescarVista();
            }
        }

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
            HayBusqueda
            || (Vinculo is not null && Vinculo.Valor != VinculoPersona.Todas)
            || (RolFiltro is not null && RolFiltro.Valor is not null);

        protected override bool Coincide(object item)
        {
            if (item is not Persona persona)
                return false;

            var vinculo = Vinculo?.Valor ?? VinculoPersona.Todas;
            bool pasaVinculo = vinculo switch
            {
                VinculoPersona.Clientes => persona.EsCliente,
                VinculoPersona.Empleados => persona.EsEmpleado,
                VinculoPersona.SinVinculo => !persona.EsCliente && !persona.EsEmpleado,
                _ => true
            };

            if (!pasaVinculo)
                return false;

            if (RolFiltro?.Valor is Rol rol && persona.Usuario?.IdRol != rol.IdRol)
                return false;

            if (!HayBusqueda)
                return true;

            return Contiene(persona.Nombre, Termino)
                || Contiene(persona.Apellido, Termino)
                || Contiene(persona.DniCuit, Termino)
                || Contiene(persona.Email, Termino)
                || Contiene(persona.Telefono, Termino)
                || Contiene(persona.Usuario?.Email, Termino)
                || Contiene(persona.Usuario?.NombreRol, Termino);
        }

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            Vinculo = OpcionesVinculo[0];
            RolFiltro = OpcionesRol.FirstOrDefault();
        }

        // ---------------------------------------------------------------
        // Indicadores del encabezado
        // ---------------------------------------------------------------
        public int CantidadClientes => Personas.Count(p => p.EsCliente);
        public int CantidadEmpleados => Personas.Count(p => p.EsEmpleado);

        protected override void RefrescarIndicadores()
        {
            OnPropertyChanged(nameof(CantidadClientes));
            OnPropertyChanged(nameof(CantidadEmpleados));
        }

        // ---------------------------------------------------------------
        // Selección
        // ---------------------------------------------------------------
        private Persona? _seleccionada;

        /// <summary>
        /// Con la ficha abierta para editar la selección queda congelada: si se pudiera
        /// mover, quedaría remarcada una fila que no es la que muestra el formulario.
        /// </summary>
        public Persona? Seleccionada
        {
            get => _seleccionada;
            set
            {
                if (EnEdicion)
                {
                    if (ReferenceEquals(_seleccionada, value))
                        return;

                    if (value is not null)
                        Fallar("Terminá la edición —guardá o cancelá— antes de elegir otra persona.");

                    // Vuelve a avisar la propiedad para que la grilla devuelva la
                    // selección a donde estaba.
                    OnPropertyChanged(nameof(Seleccionada));
                    return;
                }

                EstablecerSeleccion(value);
            }
        }

        /// <summary>Cambia la selección sin pasar por el candado: lo usa Recargar.</summary>
        private void EstablecerSeleccion(Persona? persona)
        {
            if (!SetProperty(ref _seleccionada, persona, nameof(Seleccionada)))
                return;

            OnPropertyChanged(nameof(HaySeleccion));

            if (!EnEdicion)
                CargarEnEditor(persona);
        }

        public bool HaySeleccion => Seleccionada is not null;

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
            ? "Ficha de la persona"
            : _esAlta ? "Nueva persona" : $"Editando a {_nombre} {_apellido}".TrimEnd();

        // ---------------------------------------------------------------
        // Datos personales
        // ---------------------------------------------------------------
        private string _dniCuit = string.Empty;

        /// <summary>
        /// Dato identificatorio. En un alta, escribirlo completo trae del padrón a la
        /// persona que ya lo tenga: es lo que evita cargar dos veces a la misma persona.
        /// </summary>
        public string DniCuit
        {
            get => _dniCuit;
            set
            {
                if (SetCampo(ref _dniCuit, value))
                    BuscarEnElPadron();
            }
        }

        private string _nombre = string.Empty;
        public string Nombre { get => _nombre; set { if (SetCampo(ref _nombre, value)) OnPropertyChanged(nameof(TituloEditor)); } }

        private string _apellido = string.Empty;
        public string Apellido { get => _apellido; set { if (SetCampo(ref _apellido, value)) OnPropertyChanged(nameof(TituloEditor)); } }

        private string _telefono = string.Empty;
        public string Telefono { get => _telefono; set => SetCampo(ref _telefono, value); }

        private string _email = string.Empty;
        public string Email { get => _email; set => SetCampo(ref _email, value); }

        private bool _esCliente;

        /// <summary>RF-05: tildarlo es lo que habilita a la persona en el punto de venta.</summary>
        public bool EsCliente { get => _esCliente; set => SetProperty(ref _esCliente, value); }

        // ---------------------------------------------------------------
        // Acceso al sistema
        // ---------------------------------------------------------------
        private bool _tieneCuenta;

        /// <summary>
        /// Destildarlo da de baja el acceso, pero la persona sigue en el padrón:
        /// un empleado que se va puede seguir siendo cliente del bar.
        /// </summary>
        public bool TieneCuenta
        {
            get => _tieneCuenta;
            set
            {
                if (SetProperty(ref _tieneCuenta, value))
                {
                    OnPropertyChanged(nameof(AyudaCuenta));
                    Revalidar();
                }
            }
        }

        private string _emailTrabajo = string.Empty;

        /// <summary>Correo del bar. Es el nombre de usuario con el que se inicia sesión (RF-09).</summary>
        public string EmailTrabajo { get => _emailTrabajo; set => SetCampo(ref _emailTrabajo, value); }

        private Rol? _rolSeleccionado;
        public Rol? RolSeleccionado { get => _rolSeleccionado; set => SetCampo(ref _rolSeleccionado, value); }

        private string _claveNueva = string.Empty;
        public string ClaveNueva { get => _claveNueva; set => SetCampo(ref _claveNueva, value); }

        /// <summary>La cuenta que se está editando, o null si la persona no tiene.</summary>
        private Usuario? CuentaActual => _esAlta ? null : Seleccionada?.Usuario;

        public string AyudaCuenta => CuentaActual is null
            ? "Mínimo 8 caracteres."
            : "Dejar vacío para conservar la contraseña actual.";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }

        // ---------------------------------------------------------------
        // Validación
        // ---------------------------------------------------------------
        protected override void DeclararReglas()
        {
            Requerido(DniCuit, nameof(DniCuit), "El DNI/CUIT");
            FormatoDocumento(DniCuit, nameof(DniCuit));

            Requerido(Nombre, nameof(Nombre), "El nombre");
            LargoMaximo(Nombre, nameof(Nombre), "El nombre", 60);

            Requerido(Apellido, nameof(Apellido), "El apellido");
            LargoMaximo(Apellido, nameof(Apellido), "El apellido", 60);

            FormatoTelefono(Telefono, nameof(Telefono));

            FormatoCorreo(Email, nameof(Email));
            NoRepetido(Email, nameof(Email), OtrasPersonas().Select(p => p.Email),
                       "Ya hay otra persona con ese correo.");

            if (!TieneCuenta)
                return;

            Requerido(EmailTrabajo, nameof(EmailTrabajo), "El correo de trabajo");
            FormatoCorreo(EmailTrabajo, nameof(EmailTrabajo));
            NoRepetido(EmailTrabajo, nameof(EmailTrabajo), OtrasCuentas().Select(u => u.Email),
                       "Ya hay otra cuenta con ese correo.");

            RequeridoElegir(RolSeleccionado, nameof(RolSeleccionado), "el rol");

            if (CuentaActual is null)
                Requerido(ClaveNueva, nameof(ClaveNueva), "La contraseña");

            LargoMinimo(ClaveNueva, nameof(ClaveNueva), "La contraseña", 8);
        }

        private IEnumerable<Persona> OtrasPersonas() =>
            Personas.Where(p => p.IdPersona != Seleccionada?.IdPersona);

        private IEnumerable<Usuario> OtrasCuentas() =>
            Personas.Where(p => p.Usuario is not null && p.IdPersona != Seleccionada?.IdPersona)
                    .Select(p => p.Usuario!);

        // ---------------------------------------------------------------
        // Búsqueda en el padrón por documento
        // ---------------------------------------------------------------
        /// <summary>
        /// Solo en un alta: si el documento tecleado ya está en el padrón, se traen los
        /// datos de esa persona y el alta se convierte en una edición. Así no quedan dos
        /// filas para el mismo DNI cuando un cliente conocido entra a trabajar al bar.
        /// </summary>
        private void BuscarEnElPadron()
        {
            if (!_esAlta || !EnEdicion)
                return;

            var encontrada = Repositorio.BuscarPersonaPorDocumento(DniCuit);
            if (encontrada is null)
                return;

            _esAlta = false;
            _seleccionada = Personas.FirstOrDefault(p => p.IdPersona == encontrada.IdPersona) ?? encontrada;
            OnPropertyChanged(nameof(Seleccionada));
            OnPropertyChanged(nameof(HaySeleccion));
            OnPropertyChanged(nameof(EsAlta));

            CargarEnEditor(_seleccionada);

            Informar($"{encontrada.NombreCompleto} ya estaba en el padrón: se cargaron sus datos. " +
                     "Los cambios que hagas ahora actualizan su ficha.");
        }

        // ---------------------------------------------------------------
        // Ciclo de vida
        // ---------------------------------------------------------------
        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionada?.IdPersona;

            Personas.Clear();
            foreach (var persona in Repositorio.ObtenerPersonas())
                Personas.Add(persona);

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

            ConfigurarFiltro(Personas);
            _vinculo ??= OpcionesVinculo[0];
            RefrescarVista();

            EstablecerSeleccion(Personas.FirstOrDefault(p => p.IdPersona == idPrevio) ?? Personas.FirstOrDefault());
        }

        private void CargarEnEditor(Persona? persona)
        {
            _dniCuit = persona?.DniCuit ?? string.Empty;
            _nombre = persona?.Nombre ?? string.Empty;
            _apellido = persona?.Apellido ?? string.Empty;
            _telefono = persona?.Telefono ?? string.Empty;
            _email = persona?.Email ?? string.Empty;
            _esCliente = persona?.EsCliente ?? false;

            var cuenta = persona?.Usuario;
            _tieneCuenta = cuenta is not null;
            _emailTrabajo = cuenta?.Email ?? string.Empty;
            _rolSeleccionado = cuenta is null ? null : Roles.FirstOrDefault(r => r.IdRol == cuenta.IdRol);
            _claveNueva = string.Empty;

            foreach (var propiedad in new[]
                     {
                         nameof(DniCuit), nameof(Nombre), nameof(Apellido), nameof(Telefono),
                         nameof(Email), nameof(EsCliente), nameof(TieneCuenta), nameof(EmailTrabajo),
                         nameof(RolSeleccionado), nameof(ClaveNueva), nameof(TituloEditor), nameof(AyudaCuenta)
                     })
            {
                OnPropertyChanged(propiedad);
            }

            // Ficha recién cargada: las reglas se calculan, pero nada se pinta de rojo
            // hasta que el usuario toque un campo o apriete Guardar.
            ReiniciarValidacion();
        }

        private void Nuevo()
        {
            LimpiarMensaje();
            _esAlta = true;
            OnPropertyChanged(nameof(EsAlta));
            _seleccionada = null;
            OnPropertyChanged(nameof(Seleccionada));
            OnPropertyChanged(nameof(HaySeleccion));
            CargarEnEditor(null);
            EnEdicion = true;
            ReiniciarValidacion();
        }

        private void Editar()
        {
            if (Seleccionada is null)
                return;

            LimpiarMensaje();
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            CargarEnEditor(Seleccionada);
            EnEdicion = true;
            ReiniciarValidacion();
        }

        private void Cancelar()
        {
            EnEdicion = false;
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            LimpiarMensaje();
            CargarEnEditor(Seleccionada);
        }

        private void Guardar()
        {
            // El botón está habilitado aunque falten datos: deshabilitado no explica QUÉ
            // falta. Se aprieta, se marcan los campos y se dice cuántos hay que corregir.
            if (!IntentarConfirmar())
            {
                Fallar(MensajeDeFormularioInvalido);
                return;
            }

            var persona = _esAlta ? new Persona() : Seleccionada;
            if (persona is null)
            {
                Fallar("No hay una persona seleccionada.");
                return;
            }

            persona.DniCuit = DniCuit.Trim();
            persona.Nombre = Nombre.Trim();
            persona.Apellido = Apellido.Trim();
            persona.Telefono = Telefono.Trim();
            persona.Email = Email.Trim();

            if (!Aplicar(Repositorio.GuardarPersona(persona, EsCliente)))
                return;

            var resultadoCuenta = SincronizarCuenta(persona);

            EnEdicion = false;
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            Recargar();
            EstablecerSeleccion(Personas.FirstOrDefault(p => p.IdPersona == persona.IdPersona));

            if (resultadoCuenta is not null)
                Aplicar(resultadoCuenta);
        }

        /// <summary>
        /// Alinea la cuenta con el tilde del formulario: la crea, la actualiza o la da de
        /// baja. Devuelve null si no hubo nada que hacer, para no pisar el mensaje del alta.
        /// </summary>
        private ResultadoOperacion? SincronizarCuenta(Persona persona)
        {
            var cuenta = persona.Usuario;

            if (TieneCuenta)
            {
                var aGuardar = cuenta ?? new Usuario();
                aGuardar.IdPersona = persona.IdPersona;
                aGuardar.IdRol = RolSeleccionado?.IdRol ?? 0;
                aGuardar.Email = EmailTrabajo.Trim();

                return Repositorio.GuardarUsuario(aGuardar, ClaveNueva);
            }

            return cuenta is null ? null : Repositorio.EliminarUsuario(cuenta.IdUsuario);
        }

        private void Eliminar()
        {
            if (Seleccionada is null)
                return;

            string? aviso = Seleccionada.EsEmpleado
                ? "Tiene cuenta de acceso al sistema: se da de baja junto con su ficha."
                : null;

            if (!ConfirmarEliminacion("la persona", Seleccionada.NombreCompleto, aviso))
                return;

            // La cuenta se va primero: el padrón no deja borrar a alguien que tiene acceso.
            if (Seleccionada.Usuario is not null &&
                !Aplicar(Repositorio.EliminarUsuario(Seleccionada.Usuario.IdUsuario)))
                return;

            if (Aplicar(Repositorio.EliminarPersona(Seleccionada.IdPersona)))
                Recargar();
        }
    }
}
