using System;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    public class AutorizacionViewModel : ViewModelBase, ICierrable
    {
        private readonly IRepositorioBar _repositorio;

        private string _emailSupervisor = string.Empty;
        private string _mensajeError = string.Empty;

        public event EventHandler? CierreSolicitado;

        public AutorizacionViewModel(IRepositorioBar repositorio)
        {
            _repositorio = repositorio;

            ConfirmarCommand = new RelayCommand(EjecutarConfirmar, _ => !string.IsNullOrWhiteSpace(EmailSupervisor));
            CancelarCommand = new RelayCommand(Cancelar);
        }

        public string EmailSupervisor
        {
            get => _emailSupervisor;
            set => SetProperty(ref _emailSupervisor, value);
        }

        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public bool EstaAutorizado { get; private set; }

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

            var resultado = _repositorio.AutorizarSupervisor(EmailSupervisor, cajaClave.Password);

            EstaAutorizado = resultado.Exito;
            MensajeError = resultado.Exito ? string.Empty : resultado.Mensaje;

            if (resultado.Exito)
                CierreSolicitado?.Invoke(this, EventArgs.Empty);
            else
                cajaClave.Clear();
        }

        private void Cancelar()
        {
            EstaAutorizado = false;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
