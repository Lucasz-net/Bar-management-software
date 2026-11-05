using System;
using System.Windows.Input;

namespace SistemaGestionBar.ViewModels
{
    public class MotivoBackupViewModel : ViewModelBase, ICierrable
    {
        public const int LargoMaximo = 120;

        private string _motivo = string.Empty;

        public event EventHandler? CierreSolicitado;

        public MotivoBackupViewModel()
        {
            ConfirmarCommand = new RelayCommand(Confirmar, () => !string.IsNullOrWhiteSpace(Motivo));
            CancelarCommand = new RelayCommand(() => CierreSolicitado?.Invoke(this, EventArgs.Empty));
        }

        public string Motivo
        {
            get => _motivo;
            set
            {
                if (SetProperty(ref _motivo, value))
                    OnPropertyChanged(nameof(Contador));
            }
        }

        public string Contador => $"{Motivo.Length}/{LargoMaximo}";

        public bool EstaConfirmado { get; private set; }

        public string MotivoLimpio => Motivo.Trim();

        public ICommand ConfirmarCommand { get; }
        public ICommand CancelarCommand { get; }

        private void Confirmar()
        {
            EstaConfirmado = true;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
