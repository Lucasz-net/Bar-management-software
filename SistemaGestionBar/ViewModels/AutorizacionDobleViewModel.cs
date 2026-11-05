using System;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>Pide la cuenta y la contraseña de un administrador y de un gerente a la vez.</summary>
    public class AutorizacionDobleViewModel : ViewModelBase, ICierrable
    {
        private readonly IRepositorioBar _repositorio;

        private string _emailAdministrador;
        private string _emailGerente;
        private string _mensajeError = string.Empty;

        public event EventHandler? CierreSolicitado;

        public AutorizacionDobleViewModel(IRepositorioBar repositorio, string motivoDeLaAccion, Usuario? sesion = null)
        {
            _repositorio = repositorio;
            MotivoDeLaAccion = motivoDeLaAccion;

            _emailAdministrador = sesion?.EsAdministrador == true ? sesion.Email : string.Empty;
            _emailGerente = sesion?.EsGerente == true ? sesion.Email : string.Empty;

            CancelarCommand = new RelayCommand(() => CierreSolicitado?.Invoke(this, EventArgs.Empty));
        }

        public string MotivoDeLaAccion { get; }

        public string EmailAdministrador
        {
            get => _emailAdministrador;
            set => SetProperty(ref _emailAdministrador, value);
        }

        public string EmailGerente
        {
            get => _emailGerente;
            set => SetProperty(ref _emailGerente, value);
        }

        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public bool EstaAutorizado { get; private set; }

        public ICommand CancelarCommand { get; }

        public void Confirmar(PasswordBox claveAdmin, PasswordBox claveGerente)
        {
            if (string.IsNullOrWhiteSpace(EmailAdministrador) || string.IsNullOrWhiteSpace(EmailGerente) ||
                string.IsNullOrEmpty(claveAdmin.Password) || string.IsNullOrEmpty(claveGerente.Password))
            {
                MensajeError = "Completá la cuenta y la contraseña de ambos.";
                return;
            }

            var admin = _repositorio.AutorizarRol(EmailAdministrador, claveAdmin.Password, RolesSistema.Administrador);
            if (!admin.Exito)
            {
                MensajeError = "Administrador: " + admin.Mensaje;
                claveAdmin.Clear();
                return;
            }

            var gerente = _repositorio.AutorizarRol(EmailGerente, claveGerente.Password, RolesSistema.Gerente);
            if (!gerente.Exito)
            {
                MensajeError = "Gerente: " + gerente.Mensaje;
                claveGerente.Clear();
                return;
            }

            EstaAutorizado = true;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
