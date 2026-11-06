using System;
using System.Windows.Input;

namespace SistemaGestionBar.ViewModels
{
    public class RangoFechasViewModel : ViewModelBase, ICierrable
    {
        private DateTime? _desde;
        private DateTime? _hasta;

        public event EventHandler? CierreSolicitado;

        public RangoFechasViewModel(DateTime? desde, DateTime? hasta)
        {
            _desde = desde;
            _hasta = hasta ?? desde;

            AplicarCommand = new RelayCommand(Aplicar, () => Desde is not null);
            CancelarCommand = new RelayCommand(() => CierreSolicitado?.Invoke(this, EventArgs.Empty));
        }

        public DateTime? Desde
        {
            get => _desde;
            private set => SetProperty(ref _desde, value);
        }

        public DateTime? Hasta
        {
            get => _hasta;
            private set => SetProperty(ref _hasta, value);
        }

        public bool SeAplico { get; private set; }

        /// <summary>Llamado por la ventana cuando cambia la selección del calendario.</summary>
        public void Seleccionar(DateTime desde, DateTime hasta)
        {
            Desde = desde.Date;
            Hasta = hasta.Date;
            OnPropertyChanged(nameof(Resumen));
        }

        public string Resumen
        {
            get
            {
                if (Desde is null)
                    return "Tocá la primera fecha en el calendario";

                if (Hasta is null || Hasta == Desde)
                    return $"{Desde:dd/MM/yyyy} · tocá otra fecha para completar el rango";

                return $"{Desde:dd/MM/yyyy}  →  {Hasta:dd/MM/yyyy}";
            }
        }

        public ICommand AplicarCommand { get; }
        public ICommand CancelarCommand { get; }

        private void Aplicar()
        {
            Hasta ??= Desde;
            SeAplico = true;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
