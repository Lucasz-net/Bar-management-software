using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Reportes;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Qué vendió cada uno. Es el "control de empleados" del gerente.
    ///
    /// <b>Solo lectura, y a propósito.</b> Acá no se crea, no se edita y no se borra
    /// nada: esta pantalla contesta "cómo viene trabajando el equipo", no "quién entra al
    /// sistema". Esa segunda pregunta es de <see cref="AdminUsuariosViewModel">Usuarios</see>,
    /// que solo tiene el administrador, y ese es el límite que sostiene todo el esquema de
    /// permisos: si el gerente pudiera asignar roles, se daría uno de administrador.
    ///
    /// Los datos personales se corrigen en Personas y la cuenta en Usuarios; desde la
    /// ficha de la derecha se salta a las dos.
    ///
    /// <b>La cuenta es la misma que la del reporte "Ventas por empleado"</b>, con la misma
    /// regla: solo ventas confirmadas, porque una anulada no factura.
    /// </summary>
    public class EquipoViewModel : SeccionAdminViewModel
    {
        public EquipoViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Equipo, "Equipo", "AccountGroupOutline",
                   "Cuánto vendió cada uno y qué mesas atendió")
        {
            Filas = new ObservableCollection<DesempenoDeEmpleado>();

            IrAPersonasCommand = new RelayCommand(
                () => IrA(DestinoAdmin.Personas, Seleccionado?.Persona),
                () => Seleccionado?.Persona is not null && PuedeIrA(DestinoAdmin.Personas));

            IrAUsuariosCommand = new RelayCommand(
                () => IrA(DestinoAdmin.Usuarios, Seleccionado?.Persona),
                () => Seleccionado?.Persona is not null && PuedeIrA(DestinoAdmin.Usuarios));

            IrAReportesCommand = new RelayCommand(() => IrA(DestinoAdmin.Reportes),
                                                  () => PuedeIrA(DestinoAdmin.Reportes));

            Recargar();
        }

        public ObservableCollection<DesempenoDeEmpleado> Filas { get; }

        // ---------------------------------------------------------------
        // Filtros
        // ---------------------------------------------------------------
        /// <summary>
        /// Las mismas ventanas que los reportes y que el historial de ventas: si el gerente
        /// mira "la última semana" en tres pantallas, tiene que significar lo mismo en las tres.
        /// </summary>
        public IReadOnlyList<OpcionFiltro<PeriodoDeReporte>> OpcionesPeriodo { get; } = new[]
        {
            new OpcionFiltro<PeriodoDeReporte>("Hoy", PeriodoDeReporte.Hoy),
            new OpcionFiltro<PeriodoDeReporte>("Última semana", PeriodoDeReporte.UltimaSemana),
            new OpcionFiltro<PeriodoDeReporte>("Último mes", PeriodoDeReporte.UltimoMes),
            new OpcionFiltro<PeriodoDeReporte>("Todo", PeriodoDeReporte.Todo)
        };

        private OpcionFiltro<PeriodoDeReporte>? _periodo;
        public OpcionFiltro<PeriodoDeReporte>? Periodo
        {
            get => _periodo;
            set
            {
                // El período no filtra filas: cambia el número de cada fila, así que hay
                // que rehacer la cuenta y no solo refrescar la vista.
                if (SetProperty(ref _periodo, value))
                    Recargar();
            }
        }

        private bool _soloConVentas;

        /// <summary>
        /// Esconde a quien no vendió nada en el período. Sirve para leer el ranking; por
        /// defecto está apagado, porque "no vendió nada" también es información.
        /// </summary>
        public bool SoloConVentas
        {
            get => _soloConVentas;
            set
            {
                if (SetProperty(ref _soloConVentas, value))
                    RefrescarVista();
            }
        }

        public override bool HayFiltroAplicado => HayBusqueda || SoloConVentas;

        protected override bool Coincide(object item)
        {
            if (item is not DesempenoDeEmpleado fila)
                return false;

            if (SoloConVentas && fila.VentasCobradas == 0)
                return false;

            if (!HayBusqueda)
                return true;

            return Contiene(fila.Nombre, Termino)
                || Contiene(fila.Rol, Termino)
                || Contiene(fila.Correo, Termino)
                || ContieneDocumento(fila.Persona?.DniCuit, Termino);
        }

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            SoloConVentas = false;
        }

        // ---------------------------------------------------------------
        // Indicadores del encabezado
        // ---------------------------------------------------------------
        private decimal _totalDelPeriodo;

        /// <summary>Lo facturado en el período, que es el 100% contra el que se reparte todo.</summary>
        public decimal TotalDelPeriodo
        {
            get => _totalDelPeriodo;
            private set => SetProperty(ref _totalDelPeriodo, value);
        }

        public int CantidadQueVendio => Filas.Count(f => f.VentasCobradas > 0);

        protected override void RefrescarIndicadores() =>
            OnPropertyChanged(nameof(CantidadQueVendio));

        // ---------------------------------------------------------------
        // Selección
        // ---------------------------------------------------------------
        private DesempenoDeEmpleado? _seleccionado;
        public DesempenoDeEmpleado? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (SetProperty(ref _seleccionado, value))
                    OnPropertyChanged(nameof(HaySeleccion));
            }
        }

        public bool HaySeleccion => Seleccionado is not null;

        public ICommand IrAPersonasCommand { get; }
        public ICommand IrAUsuariosCommand { get; }
        public ICommand IrAReportesCommand { get; }

        /// <summary>El gerente no tiene Usuarios: el atajo a la cuenta no se le ofrece.</summary>
        public bool PuedeVerCuentas => PuedeIrA(DestinoAdmin.Usuarios);

        protected override void AlCambiarLosDestinos() =>
            OnPropertyChanged(nameof(PuedeVerCuentas));

        // ---------------------------------------------------------------
        // Cálculo
        // ---------------------------------------------------------------
        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdUsuario;

            _periodo ??= OpcionesPeriodo[1];
            var desde = ArmadorDeReportes.FechaDesde(_periodo.Valor);

            var ventas = Repositorio.ObtenerVentas()
                                    .Where(v => v.EstadoVenta == EstadoVenta.Confirmada)
                                    .Where(v => desde is null || v.FechaHora >= desde)
                                    .ToList();

            TotalDelPeriodo = ventas.Sum(v => v.Total);

            // Agrupado por id y no por la navegación de cada venta: con AsNoTracking cada
            // venta trae su propia copia del usuario, y el rol no viene incluido.
            var cobros = ventas.GroupBy(v => v.IdCajero)
                               .ToDictionary(g => g.Key,
                                             g => (Cantidad: g.Count(), Importe: g.Sum(v => v.Total)));

            var atenciones = ventas.Where(v => v.IdMesero is not null)
                                   .GroupBy(v => v.IdMesero!.Value)
                                   .ToDictionary(g => g.Key,
                                                 g => (Cantidad: g.Count(), Importe: g.Sum(v => v.Total)));

            Filas.Clear();

            foreach (var usuario in Repositorio.ObtenerUsuarios())
            {
                cobros.TryGetValue(usuario.IdUsuario, out var cobrado);
                atenciones.TryGetValue(usuario.IdUsuario, out var atendido);

                Filas.Add(new DesempenoDeEmpleado(
                    usuario,
                    cobrado.Cantidad,
                    cobrado.Importe,
                    atendido.Cantidad,
                    atendido.Importe,
                    TotalDelPeriodo));
            }

            // Ordenado por lo cobrado: la pantalla se lee como un ranking.
            var ordenadas = Filas.OrderByDescending(f => f.TotalCobrado)
                                 .ThenBy(f => f.Nombre)
                                 .ToList();

            Filas.Clear();
            foreach (var fila in ordenadas)
                Filas.Add(fila);

            ConfigurarFiltro(Filas);
            RefrescarVista();

            Seleccionado = Filas.FirstOrDefault(f => f.IdUsuario == idPrevio) ?? Filas.FirstOrDefault();
        }

        /// <summary>Llega acá el salto desde otra sección señalando a una persona.</summary>
        public override void Enfocar(object? foco)
        {
            int? idPersona = foco switch
            {
                Persona persona => persona.IdPersona,
                Usuario usuario => usuario.IdPersona,
                _ => null
            };

            if (idPersona is null)
                return;

            var fila = Filas.FirstOrDefault(f => f.Persona?.IdPersona == idPersona);
            if (fila is not null)
                Seleccionado = fila;
        }
    }

    /// <summary>
    /// Una fila de la pantalla: lo que hizo una cuenta en el período elegido.
    ///
    /// Separa las dos formas de participar en una venta, porque son dos trabajos
    /// distintos: el <b>cajero</b> es quien cobró y el <b>mesero</b> quien tomó el pedido.
    /// En la barra y en las ventas para llevar no hay mesero (RF-02), así que un barman
    /// puede tener mucho cobrado y ninguna mesa.
    /// </summary>
    public class DesempenoDeEmpleado
    {
        public DesempenoDeEmpleado(
            Usuario usuario,
            int ventasCobradas,
            decimal totalCobrado,
            int mesasAtendidas,
            decimal importeAtendido,
            decimal totalDelPeriodo)
        {
            Usuario = usuario;
            VentasCobradas = ventasCobradas;
            TotalCobrado = totalCobrado;
            MesasAtendidas = mesasAtendidas;
            ImporteAtendido = importeAtendido;
            TotalDelPeriodo = totalDelPeriodo;
        }

        public Usuario Usuario { get; }

        public Persona? Persona => Usuario.Persona;

        public int IdUsuario => Usuario.IdUsuario;
        public string Nombre => Usuario.NombreCompleto;
        public string Rol => Usuario.NombreRol;
        public string Correo => Usuario.Email;

        /// <summary>Ventas que esta cuenta cobró.</summary>
        public int VentasCobradas { get; }

        public decimal TotalCobrado { get; }

        /// <summary>Ventas en las que figura como quien tomó el pedido.</summary>
        public int MesasAtendidas { get; }

        public decimal ImporteAtendido { get; }

        private decimal TotalDelPeriodo { get; }

        public decimal TicketPromedio => VentasCobradas == 0 ? 0 : TotalCobrado / VentasCobradas;

        /// <summary>Qué porción de lo facturado pasó por esta cuenta. Alimenta la barra.</summary>
        public double Participacion =>
            TotalDelPeriodo <= 0 ? 0 : (double)(TotalCobrado / TotalDelPeriodo);

        public string PorcentajeParticipacion => $"{Participacion:P0}";

        public bool Vendio => VentasCobradas > 0;

        /// <summary>
        /// Las iniciales del avatar. Se arman acá y no en la vista para que la grilla no
        /// tenga que partir un nombre con un converter.
        /// </summary>
        public string Iniciales =>
            string.Concat(Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(parte => char.ToUpperInvariant(parte[0])));
    }
}
