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

        public IReadOnlyList<OpcionFiltro<PeriodoDeReporte>> OpcionesPeriodo { get; } = new[]
        {
            new OpcionFiltro<PeriodoDeReporte>("Hoy", PeriodoDeReporte.Hoy),
            new OpcionFiltro<PeriodoDeReporte>("Última semana", PeriodoDeReporte.UltimaSemana),
            new OpcionFiltro<PeriodoDeReporte>("Último mes", PeriodoDeReporte.UltimoMes),
            new OpcionFiltro<PeriodoDeReporte>("Todo", PeriodoDeReporte.Todo),
            new OpcionFiltro<PeriodoDeReporte>("Elegir fecha", PeriodoDeReporte.Personalizado)
        };

        private OpcionFiltro<PeriodoDeReporte>? _periodo;
        public OpcionFiltro<PeriodoDeReporte>? Periodo
        {
            get => _periodo;
            set
            {
                if (value?.Valor == PeriodoDeReporte.Personalizado && !ElegirFechas())
                {
                    // Se canceló el calendario: la lista vuelve a mostrar la opción anterior.
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(Periodo)));
                    return;
                }

                if (SetProperty(ref _periodo, value))
                {
                    OnPropertyChanged(nameof(EsPersonalizado));
                    Recargar();
                }
            }
        }
        private DateTime? _desde;
        private DateTime? _hasta;

        public bool EsPersonalizado => Periodo?.Valor == PeriodoDeReporte.Personalizado;

        public string RangoTexto => _desde is DateTime d && _hasta is DateTime h
            ? (d == h ? $"{d:dd/MM/yyyy}" : $"{d:dd/MM/yyyy} - {h:dd/MM/yyyy}")
            : string.Empty;

        public ICommand CambiarFechasCommand => new RelayCommand(() =>
        {
            if (ElegirFechas())
                Recargar();
        });

        private bool ElegirFechas()
        {
            var rango = Dialogo.ElegirRangoDeFechas(_desde, _hasta);
            if (rango is null)
                return false;

            (_desde, _hasta) = rango.Value;
            OnPropertyChanged(nameof(RangoTexto));
            return true;
        }

        private bool _soloConVentas;

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

        private decimal _totalDelPeriodo;

        public decimal TotalDelPeriodo
        {
            get => _totalDelPeriodo;
            private set => SetProperty(ref _totalDelPeriodo, value);
        }

        public int CantidadQueVendio => Filas.Count(f => f.VentasCobradas > 0);

        protected override void RefrescarIndicadores() =>
            OnPropertyChanged(nameof(CantidadQueVendio));

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

        public bool PuedeVerCuentas => PuedeIrA(DestinoAdmin.Usuarios);

        protected override void AlCambiarLosDestinos() =>
            OnPropertyChanged(nameof(PuedeVerCuentas));

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdUsuario;

            _periodo ??= OpcionesPeriodo[1];
            var ventas = ArmadorDeReportes.VentasConfirmadas(
                Repositorio, new PeriodoElegido(_periodo.Valor, _desde, _hasta));

            TotalDelPeriodo = ventas.Sum(v => v.Total);

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

        public int VentasCobradas { get; }

        public decimal TotalCobrado { get; }

        public int MesasAtendidas { get; }

        public decimal ImporteAtendido { get; }

        private decimal TotalDelPeriodo { get; }

        public decimal TicketPromedio => VentasCobradas == 0 ? 0 : TotalCobrado / VentasCobradas;

        public double Participacion =>
            TotalDelPeriodo <= 0 ? 0 : (double)(TotalCobrado / TotalDelPeriodo);

        public string PorcentajeParticipacion => $"{Participacion:P0}";

        public bool Vendio => VentasCobradas > 0;

        public string Iniciales =>
            string.Concat(Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Take(2)
                                .Select(parte => char.ToUpperInvariant(parte[0])));
    }
}
