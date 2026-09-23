using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Padrón de personas (RF-10 y RF-12). Acá se carga y se corrige <b>quién es</b>
    /// alguien: DNI, nombre, apellido, teléfono y correo personal.
    ///
    /// <b>Qué NO se hace acá.</b> Nada de credenciales. El correo de trabajo, el rol y la
    /// contraseña viven en la sección <see cref="AdminUsuariosViewModel">Usuarios</see>,
    /// que arma la cuenta sobre una persona que este padrón ya tiene. Estaban juntos y el
    /// formulario mentía: para dar de alta a un cliente había que pasar por delante de un
    /// bloque de credenciales que no le correspondía.
    ///
    /// Lo único que sigue siendo de las dos pantallas es el <b>DNI</b>: es el dato
    /// identificatorio, y al escribirlo en un alta trae a la persona que ya lo tenga en
    /// vez de duplicarla.
    /// </summary>
    public class AdminPersonalViewModel : SeccionAdminViewModel
    {
        public AdminPersonalViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Personas, "Personas", "CardAccountDetailsOutline",
                   "Padrón de datos personales de clientes y empleados")
        {
            Personas = new ObservableCollection<Persona>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionada is not null && !EnEdicion);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionada is not null && !EnEdicion);
            IrAUsuariosCommand = new RelayCommand(() => IrA(DestinoAdmin.Usuarios, Seleccionada),
                                                  () => PuedeIrA(DestinoAdmin.Usuarios));

            Recargar();
        }

        public ObservableCollection<Persona> Personas { get; }

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

        public override bool HayFiltroAplicado =>
            HayBusqueda || (Vinculo is not null && Vinculo.Valor != VinculoPersona.Todas);

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

            if (!HayBusqueda)
                return true;

            return Contiene(persona.Nombre, Termino)
                || Contiene(persona.Apellido, Termino)
                || ContieneDocumento(persona.DniCuit, Termino)
                || Contiene(persona.Email, Termino)
                || Contiene(persona.Telefono, Termino);
        }

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            Vinculo = OpcionesVinculo[0];
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
        private string _dni = string.Empty;

        /// <summary>
        /// Dato identificatorio, con los 8 números pelados. En un alta, escribirlo completo
        /// trae del padrón a la persona que ya lo tenga: es lo que evita cargar dos veces a
        /// la misma persona.
        ///
        /// Para mostrarlo en la grilla o en la ficha va <see cref="Persona.DniFormateado"/>,
        /// que es el que le pone los puntos.
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

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }

        /// <summary>
        /// Atajo a la otra mitad: desde la ficha se salta a Usuarios llevando la persona,
        /// que es lo que se quiere hacer justo después de dar de alta a un empleado.
        /// </summary>
        public ICommand IrAUsuariosCommand { get; }

        public bool PuedeGestionarCuentas => PuedeIrA(DestinoAdmin.Usuarios);

        protected override void AlCambiarLosDestinos() =>
            OnPropertyChanged(nameof(PuedeGestionarCuentas));

        // ---------------------------------------------------------------
        // Validación
        // ---------------------------------------------------------------
        protected override void DeclararReglas()
        {
            Requerido(Dni, nameof(Dni), "El DNI");
            FormatoDni(Dni, nameof(Dni));

            Requerido(Nombre, nameof(Nombre), "El nombre");
            LargoMaximo(Nombre, nameof(Nombre), "El nombre", Limites.NombrePersona);

            Requerido(Apellido, nameof(Apellido), "El apellido");
            LargoMaximo(Apellido, nameof(Apellido), "El apellido", Limites.ApellidoPersona);

            FormatoTelefono(Telefono, nameof(Telefono));
            LargoMaximo(Telefono, nameof(Telefono), "El teléfono", Limites.Telefono);

            FormatoCorreo(Email, nameof(Email));
            LargoMaximo(Email, nameof(Email), "El correo", Limites.Email);
            NoRepetido(Email, nameof(Email), OtrasPersonas().Select(p => p.Email),
                       "Ya hay otra persona con ese correo.");
        }

        private IEnumerable<Persona> OtrasPersonas() =>
            Personas.Where(p => p.IdPersona != Seleccionada?.IdPersona);

        // ---------------------------------------------------------------
        // Búsqueda en el padrón por documento
        // ---------------------------------------------------------------
        /// <summary>
        /// Solo en un alta: si el DNI tecleado ya está en el padrón, se traen los datos de
        /// esa persona y el alta se convierte en una edición. Así no quedan dos filas para
        /// el mismo DNI cuando un cliente conocido entra a trabajar al bar.
        /// </summary>
        private void BuscarEnElPadron()
        {
            if (!_esAlta || !EnEdicion)
                return;

            var encontrada = Repositorio.BuscarPersonaPorDocumento(Dni);
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

            ConfigurarFiltro(Personas);
            _vinculo ??= OpcionesVinculo[0];
            RefrescarVista();

            EstablecerSeleccion(Personas.FirstOrDefault(p => p.IdPersona == idPrevio) ?? Personas.FirstOrDefault());
        }

        /// <summary>Llega acá el salto desde Usuarios: señala en el padrón a esa persona.</summary>
        public override void Enfocar(object? foco)
        {
            int? id = foco switch
            {
                Persona persona => persona.IdPersona,
                Usuario usuario => usuario.IdPersona,
                int idPersona => idPersona,
                _ => null
            };

            if (id is null)
                return;

            var fila = Personas.FirstOrDefault(p => p.IdPersona == id);
            if (fila is not null)
                EstablecerSeleccion(fila);
        }

        private void CargarEnEditor(Persona? persona)
        {
            // Normalizado, no crudo: una ficha vieja guardada con puntos tiene que abrirse
            // en el formulario como se carga hoy, con los números pelados.
            _dni = Documento.Normalizar(persona?.DniCuit);
            _nombre = persona?.Nombre ?? string.Empty;
            _apellido = persona?.Apellido ?? string.Empty;
            _telefono = persona?.Telefono ?? string.Empty;
            _email = persona?.Email ?? string.Empty;
            _esCliente = persona?.EsCliente ?? false;

            foreach (var propiedad in new[]
                     {
                         nameof(Dni), nameof(Nombre), nameof(Apellido), nameof(Telefono),
                         nameof(Email), nameof(EsCliente), nameof(TituloEditor)
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

            persona.DniCuit = Documento.Normalizar(Dni);
            persona.Nombre = Nombre.Trim();
            persona.Apellido = Apellido.Trim();
            persona.Telefono = Telefono.Trim();
            persona.Email = Email.Trim();

            if (!Aplicar(Repositorio.GuardarPersona(persona, EsCliente)))
                return;

            EnEdicion = false;
            _esAlta = false;
            OnPropertyChanged(nameof(EsAlta));
            Recargar();
            EstablecerSeleccion(Personas.FirstOrDefault(p => p.IdPersona == persona.IdPersona));
        }

        private void Eliminar()
        {
            if (Seleccionada is null)
                return;

            // La cuenta NO se borra en cascada desde acá: dar de baja un acceso es una
            // decisión de la sección Usuarios y se toma mirando esa pantalla. Si la
            // persona tiene cuenta, el repositorio rechaza el borrado y lo dice.
            if (Seleccionada.EsEmpleado)
            {
                Fallar($"{Seleccionada.NombreCompleto} tiene una cuenta de acceso. " +
                       "Eliminá primero la cuenta en la sección Usuarios.");
                return;
            }

            if (!ConfirmarEliminacion("la persona", Seleccionada.NombreCompleto))
                return;

            if (Aplicar(Repositorio.EliminarPersona(Seleccionada.IdPersona)))
                Recargar();
        }
    }
}
