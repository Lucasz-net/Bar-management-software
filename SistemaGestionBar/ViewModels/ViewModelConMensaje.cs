namespace SistemaGestionBar.ViewModels
{
    /// <summary>Base de los ViewModels que muestran un mensaje de éxito o error al usuario.</summary>
    public abstract class ViewModelConMensaje : ViewModelBase
    {
        private string _mensaje = string.Empty;
        private bool _mensajeEsError;

        public string Mensaje
        {
            get => _mensaje;
            private set
            {
                if (SetProperty(ref _mensaje, value))
                    OnPropertyChanged(nameof(HayMensaje));
            }
        }

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
    }
}
