using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SistemaGestionBar.ViewModels;

namespace SistemaGestionBar.Views
{
    public partial class RangoFechasWindow : Window
    {
        private DateTime? _primera;
        private bool _ajustando;

        public RangoFechasWindow()
        {
            InitializeComponent();
            Calendario.DisplayDateEnd = DateTime.Today;
            Loaded += (_, _) => MostrarSeleccionInicial();
        }

        private RangoFechasViewModel? Modelo => DataContext as RangoFechasViewModel;

        private void MostrarSeleccionInicial()
        {
            if (Modelo?.Desde is not DateTime desde)
                return;

            var hasta = Modelo.Hasta ?? desde;
            Pintar(desde, hasta);
            Calendario.DisplayDate = desde;
        }

        /// <summary>Primer toque = inicio; segundo toque = fin (se ordenan solas si van al revés).</summary>
        private void AlCambiarSeleccion(object sender, SelectionChangedEventArgs e)
        {
            if (_ajustando || Modelo is null || Calendario.SelectedDates.Count == 0)
                return;

            var tocada = Calendario.SelectedDates.First().Date;

            if (Calendario.SelectedDates.Count > 1)
            {
                // Arrastre: el calendario ya armó el rango.
                _primera = null;
                Modelo.Seleccionar(Calendario.SelectedDates.Min(), Calendario.SelectedDates.Max());
                return;
            }

            if (_primera is null)
            {
                _primera = tocada;
                Modelo.Seleccionar(tocada, tocada);
                return;
            }

            var a = _primera.Value;
            _primera = null;
            var (desde, hasta) = a <= tocada ? (a, tocada) : (tocada, a);
            Pintar(desde, hasta);
            Modelo.Seleccionar(desde, hasta);
        }

        private void Pintar(DateTime desde, DateTime hasta)
        {
            _ajustando = true;
            try
            {
                Calendario.SelectedDates.Clear();
                Calendario.SelectedDates.AddRange(desde, hasta);
            }
            finally
            {
                _ajustando = false;
            }
        }
    }
}
