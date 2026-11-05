using System;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    public class ConfirmarContrasenaViewModel : ViewModelBase, ICierrable
    {
        private readonly IRepositorioBar _repositorio;

        private string _mensajeError = string.Empty;

        public event EventHandler? CierreSolicitado;

        public ConfirmarContrasenaViewModel(IRepositorioBar repositorio, string? explicacion = null)
        {
            _repositorio = repositorio;
            Explicacion = explicacion ?? "Volvé a ingresar tu contraseña para continuar.";

            ConfirmarCommand = new RelayCommand(EjecutarConfirmar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        public string Explicacion { get; }

        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public bool EstaConfirmado { get; private set; }

        public ICommand ConfirmarCommand { get; }
        public ICommand CancelarCommand { get; }

        private void EjecutarConfirmar(object? parametro)
        {
            if (parametro is not PasswordBox cajaClave)
                return;

            if (string.IsNullOrWhiteSpace(cajaClave.Password))
            {
                MensajeError = "La contraseña es obligatoria.";
                return;
            }

            var resultado = _repositorio.ConfirmarContrasenaPropia(cajaClave.Password);

            EstaConfirmado = resultado.Exito;
            MensajeError = resultado.Exito ? string.Empty : resultado.Mensaje;

            if (resultado.Exito)
                CierreSolicitado?.Invoke(this, EventArgs.Empty);
            else
                cajaClave.Clear();
        }

        private void Cancelar()
        {
            EstaConfirmado = false;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
