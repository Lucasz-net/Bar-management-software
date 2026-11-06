using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using QuestPDF.Fluent;
using SistemaGestionBar.Models;
using SistemaGestionBar.Reportes;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminReportesViewModel : SeccionAdminViewModel
    {
        private readonly ArmadorDeReportes _armador;
        private readonly Usuario _usuarioActual;

        public AdminReportesViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo, Usuario usuarioActual)
            : base(repositorio, dialogo, DestinoAdmin.Reportes, "Reportes", "FilePdfBox",
                   "Generá reportes de ventas, productos y stock, y descargalos en PDF")
        {
            _armador = new ArmadorDeReportes(repositorio);
            _usuarioActual = usuarioActual;

            Disponibles = new ObservableCollection<OpcionDeReporte>
            {
                new(TipoDeReporte.Ventas, "Ventas del período", "CashRegister",
                    "Cuánto se facturó, con qué se pagó y el detalle venta por venta.", UsaPeriodo: true),

                new(TipoDeReporte.ProductosVendidos, "Productos más vendidos", "TrophyOutline",
                    "Ranking por unidades e importe: qué se mueve y qué no.", UsaPeriodo: true),

                new(TipoDeReporte.Empleados, "Ventas por empleado", "AccountGroupOutline",
                    "Cuánto cobró cada cajero y qué mesas atendió cada mesero.", UsaPeriodo: true),

                new(TipoDeReporte.VentasPorCategoria, "Ventas por categoría", "ChartPie",
                    "Cuánto facturó cada categoría del menú en el período.", UsaPeriodo: true),

                new(TipoDeReporte.Catalogo, "Catálogo de productos", "SilverwareForkKnife",
                    "Listado completo con precio, categoría y stock. No depende del período.", UsaPeriodo: false),

                new(TipoDeReporte.Inventario, "Inventario y stock", "PackageVariantClosed",
                    "Insumos y productos, con lo que falta para llegar al mínimo.", UsaPeriodo: false)
            };

            GenerarCommand = new RelayCommand(Generar, () => Seleccionado is not null);
            ExportarCommand = new RelayCommand(Exportar, () => Contenido is not null);

            Periodo = OpcionesPeriodo[2];
            Seleccionado = Disponibles[0];
        }

        public ObservableCollection<OpcionDeReporte> Disponibles { get; }

        private OpcionDeReporte? _seleccionado;
        public OpcionDeReporte? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (!SetProperty(ref _seleccionado, value))
                    return;

                OnPropertyChanged(nameof(HaySeleccion));
                OnPropertyChanged(nameof(PeriodoEsAplicable));

                Generar();
            }
        }

        public bool HaySeleccion => Seleccionado is not null;

        public bool PeriodoEsAplicable => Seleccionado?.UsaPeriodo == true;

        public IReadOnlyList<OpcionFiltro<PeriodoDeReporte>> OpcionesPeriodo { get; } = new[]
        {
            new OpcionFiltro<PeriodoDeReporte>("Hoy", PeriodoDeReporte.Hoy),
            new OpcionFiltro<PeriodoDeReporte>("Última semana", PeriodoDeReporte.UltimaSemana),
            new OpcionFiltro<PeriodoDeReporte>("Último mes", PeriodoDeReporte.UltimoMes),
            new OpcionFiltro<PeriodoDeReporte>("Todo el historial", PeriodoDeReporte.Todo),
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
                    if (Seleccionado is not null)
                        Generar();
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
                Generar();
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

        private ContenidoDeReporte? _contenido;
        public ContenidoDeReporte? Contenido
        {
            get => _contenido;
            private set
            {
                if (SetProperty(ref _contenido, value))
                    OnPropertyChanged(nameof(HayVistaPrevia));
            }
        }

        public bool HayVistaPrevia => Contenido is not null;

        public ICommand GenerarCommand { get; }
        public ICommand ExportarCommand { get; }

        private void Generar()
        {
            if (Seleccionado is null)
                return;

            LimpiarMensaje();

            var periodo = Seleccionado.UsaPeriodo
                ? new PeriodoElegido(Periodo?.Valor ?? PeriodoDeReporte.UltimaSemana, _desde, _hasta)
                : PeriodoDeReporte.Todo;

            try
            {
                Contenido = _armador.Armar(Seleccionado.Tipo, periodo, _usuarioActual.NombreCompleto);
            }
            catch (Exception ex)
            {
                Contenido = null;
                Fallar($"No se pudo armar el reporte: {ex.Message}");
            }
        }

        private void Exportar()
        {
            if (Contenido is null)
                return;

            string? ruta = Dialogo.ElegirDondeGuardarPdf(Contenido.NombreDeArchivoSugerido);
            if (ruta is null)
                return;

            try
            {
                new DocumentoDeReporte(Contenido).GeneratePdf(ruta);
            }
            catch (Exception ex)
            {
                Fallar($"No se pudo guardar el PDF: {ex.Message} " +
                       "Si el archivo ya está abierto, cerralo y volvé a intentar.");
                return;
            }

            Informar($"Reporte guardado en {ruta}");

            if (Dialogo.Confirmar("Reporte generado",
                    $"El reporte se guardó en:\n{ruta}\n\n¿Querés abrirlo ahora?"))
            {
                try
                {
                    Dialogo.AbrirArchivo(ruta);
                }
                catch (Exception ex)
                {
                    Fallar($"El PDF se guardó, pero no se pudo abrir: {ex.Message}");
                }
            }
        }

        public override void Recargar() => Generar();
    }

    public record OpcionDeReporte(
        TipoDeReporte Tipo,
        string Titulo,
        string Icono,
        string Descripcion,
        bool UsaPeriodo);
}
