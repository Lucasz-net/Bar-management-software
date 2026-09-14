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
    /// <summary>
    /// Sección de reportes del tablero. Disponible para administrador y gerente: es
    /// información de gestión del negocio, que es justamente lo que el gerente sí ve.
    ///
    /// <b>No se guarda ningún reporte.</b> Se arman en el momento a partir de las tablas
    /// que ya existen (Venta, Venta_Detalle, Producto, Ingrediente, Usuario) y el PDF va
    /// al disco del usuario, no a la base. Guardarlos obligaría a rehacerlos cada vez que
    /// cambia una venta; por eso la cátedra pidió eliminar la tabla Reporte y por eso
    /// esta pantalla no la necesita.
    ///
    /// La vista previa y el PDF salen del mismo <see cref="ContenidoDeReporte"/>, así que
    /// lo que se ve en pantalla es literalmente lo que se exporta.
    /// </summary>
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

                // Elegir un reporte ya lo genera: obligar a apretar un botón más para ver
                // lo que se acaba de pedir no aporta nada, y el costo es una consulta local.
                Generar();
            }
        }

        public bool HaySeleccion => Seleccionado is not null;

        /// <summary>El catálogo y el inventario son una foto de hoy: el período no los cambia.</summary>
        public bool PeriodoEsAplicable => Seleccionado?.UsaPeriodo == true;

        public IReadOnlyList<OpcionFiltro<PeriodoDeReporte>> OpcionesPeriodo { get; } = new[]
        {
            new OpcionFiltro<PeriodoDeReporte>("Hoy", PeriodoDeReporte.Hoy),
            new OpcionFiltro<PeriodoDeReporte>("Última semana", PeriodoDeReporte.UltimaSemana),
            new OpcionFiltro<PeriodoDeReporte>("Último mes", PeriodoDeReporte.UltimoMes),
            new OpcionFiltro<PeriodoDeReporte>("Todo el historial", PeriodoDeReporte.Todo)
        };

        private OpcionFiltro<PeriodoDeReporte>? _periodo;
        public OpcionFiltro<PeriodoDeReporte>? Periodo
        {
            get => _periodo;
            set
            {
                if (SetProperty(ref _periodo, value) && Seleccionado is not null)
                    Generar();
            }
        }

        // ---------------------------------------------------------------
        // El reporte armado
        // ---------------------------------------------------------------
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
                ? Periodo?.Valor ?? PeriodoDeReporte.UltimaSemana
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
                // El caso real: el archivo está abierto en el lector de PDF y Windows lo
                // tiene bloqueado. Decirlo con estas palabras evita la captura de pantalla
                // de un IOException que no le explica nada a nadie.
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

        /// <summary>
        /// Al entrar a la sección se rearma el reporte que esté elegido: si alguien cargó
        /// una venta o repuso stock desde otra pantalla, los números tienen que ser los de ahora.
        /// </summary>
        public override void Recargar() => Generar();
    }

    /// <summary>
    /// Un reporte ofrecido en la lista. <paramref name="UsaPeriodo"/> es lo que apaga el
    /// selector de fechas para los reportes que son una foto del estado actual.
    /// </summary>
    public record OpcionDeReporte(
        TipoDeReporte Tipo,
        string Titulo,
        string Icono,
        string Descripcion,
        bool UsaPeriodo);
}
