using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// Base de cada sección del tablero de administración.
    /// Concentra lo que todas repiten: el título del menú lateral, la barra de mensajes
    /// y el patrón de alta/edición/baja. Cada sección solo escribe su parte propia.
    /// </summary>
    public abstract class SeccionAdminViewModel : ViewModelBase
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
