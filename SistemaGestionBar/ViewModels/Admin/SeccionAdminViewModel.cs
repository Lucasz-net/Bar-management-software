using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Base de cada sección del tablero de administración.
    /// Concentra lo que todas repiten: el título del menú lateral, la barra de mensajes,
    /// la validación del formulario y el buscador sobre la grilla.
    ///
    /// El filtro está acá y no en cada sección a propósito: la mecánica (una vista
    /// filtrada, un término de búsqueda, refrescar al tipear) es idéntica en las cinco
    /// pantallas. Lo único propio de cada una es QUÉ significa que una fila coincida,
    /// y eso es lo que redefine <see cref="Coincide"/>.
    /// </summary>
    public abstract class SeccionAdminViewModel : ViewModelValidable
    {
        protected SeccionAdminViewModel(
            IRepositorioBar repositorio,
            IServicioDialogo dialogo,
            string titulo,
            string icono,
            string subtitulo)
        {
            Repositorio = repositorio;
            Dialogo = dialogo;
            Titulo = titulo;
            Icono = icono;
            Subtitulo = subtitulo;

            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros, () => HayFiltroAplicado);
        }

        protected IRepositorioBar Repositorio { get; }
        protected IServicioDialogo Dialogo { get; }

        /// <summary>Texto del ítem en el menú lateral.</summary>
        public string Titulo { get; }

        /// <summary>Nombre del PackIcon de Material Design.</summary>
        public string Icono { get; }

        /// <summary>Bajada que se muestra en el encabezado de la sección.</summary>
        public string Subtitulo { get; }

        private bool _estaSeleccionada;
        public bool EstaSeleccionada
        {
            get => _estaSeleccionada;
            set => SetProperty(ref _estaSeleccionada, value);
        }

        // ---------------------------------------------------------------
        // Búsqueda y filtros (RF-12)
        // ---------------------------------------------------------------
        private ICollectionView? _vista;

        /// <summary>
        /// La grilla se bindea acá y no a la colección: así el filtro esconde filas
        /// sin sacarlas de la lista, y no hay que recargar del repositorio al tipear.
        /// </summary>
        public ICollectionView? Vista => _vista;

        private string _busqueda = string.Empty;

        /// <summary>Texto de la barra de búsqueda.</summary>
        public string Busqueda
        {
            get => _busqueda;
            set
            {
                if (SetProperty(ref _busqueda, value))
                    RefrescarVista();
            }
        }

        /// <summary>El término ya recortado, que es con lo que comparan las secciones.</summary>
        protected string Termino => Busqueda.Trim();

        public bool HayBusqueda => !string.IsNullOrWhiteSpace(Busqueda);

        /// <summary>
        /// Hay algo filtrando la grilla. Cada sección la amplía con sus propios
        /// filtros (categoría, rol, estado de stock, fechas...).
        /// </summary>
        public virtual bool HayFiltroAplicado => HayBusqueda;

        private int _cantidadVisible;

        /// <summary>Cuántas filas quedan a la vista, para el contador junto al buscador.</summary>
        public int CantidadVisible
        {
            get => _cantidadVisible;
            private set => SetProperty(ref _cantidadVisible, value);
        }

        private int _cantidadTotal;
        public int CantidadTotal
        {
            get => _cantidadTotal;
            private set => SetProperty(ref _cantidadTotal, value);
        }

        public ICommand LimpiarFiltrosCommand { get; }

        /// <summary>
        /// Engancha la vista filtrada a la colección de la sección. Se llama una sola vez,
        /// desde Recargar: GetDefaultView devuelve siempre la misma vista para la misma lista.
        /// </summary>
        protected void ConfigurarFiltro(IEnumerable coleccion)
        {
            if (_vista is not null)
                return;

            _vista = CollectionViewSource.GetDefaultView(coleccion);
            _vista.Filter = item => Coincide(item);
            OnPropertyChanged(nameof(Vista));
        }

        /// <summary>Vuelve a aplicar el filtro. Lo llaman los setters de cada filtro propio.</summary>
        protected void RefrescarVista()
        {
            _vista?.Refresh();
            ActualizarContadores();
            RefrescarIndicadores();

            OnPropertyChanged(nameof(HayBusqueda));
            OnPropertyChanged(nameof(HayFiltroAplicado));
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Gancho para las secciones que muestran totales: los números tienen que ser
        /// los de las filas visibles, no los de toda la tabla.
        /// </summary>
        protected virtual void RefrescarIndicadores() { }

        private void ActualizarContadores()
        {
            if (_vista is null)
                return;

            int visibles = 0;
            foreach (var _ in _vista)
                visibles++;

            int total = 0;
            if (_vista.SourceCollection is not null)
                foreach (var _ in _vista.SourceCollection)
                    total++;

            CantidadVisible = visibles;
            CantidadTotal = total;
        }

        /// <summary>
        /// ¿Esta fila pasa el filtro? Por defecto pasan todas: una sección sin buscador
        /// no tiene que escribir nada.
        /// </summary>
        protected virtual bool Coincide(object item) => true;

        /// <summary>Deja la grilla sin filtros. Cada sección agrega los suyos.</summary>
        protected virtual void LimpiarFiltros()
        {
            Busqueda = string.Empty;
        }

        /// <summary>Comparación de texto tolerante: ignora nulos y mayúsculas.</summary>
        protected static bool Contiene(string? texto, string termino) =>
            !string.IsNullOrEmpty(texto) &&
            texto.Contains(termino, StringComparison.OrdinalIgnoreCase);

        // ---------------------------------------------------------------
        // Barra de mensajes
        // ---------------------------------------------------------------
        private string _mensaje = string.Empty;
        public string Mensaje
        {
            get => _mensaje;
            private set
            {
                if (SetProperty(ref _mensaje, value))
                    OnPropertyChanged(nameof(HayMensaje));
            }
        }

        private bool _mensajeEsError;
        public bool MensajeEsError
        {
            get => _mensajeEsError;
            private set => SetProperty(ref _mensajeEsError, value);
        }

        public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        protected void Informar(string texto)
        {
            MensajeEsError = false;
            Mensaje = texto;
        }

        protected void Fallar(string texto)
        {
            MensajeEsError = true;
            Mensaje = texto;
        }

        protected void LimpiarMensaje() => Mensaje = string.Empty;

        /// <summary>Vuelca un ResultadoOperacion a la barra de mensajes y dice si salió bien.</summary>
        protected bool Aplicar(ResultadoOperacion resultado)
        {
            if (resultado.Exito)
                Informar(resultado.Mensaje);
            else
                Fallar(resultado.Mensaje);

            return resultado.Exito;
        }

        /// <summary>Se llama al entrar a la sección: cada una recarga sus listas.</summary>
        public abstract void Recargar();
    }
}
