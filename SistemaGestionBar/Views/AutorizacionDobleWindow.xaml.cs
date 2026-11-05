using System.Windows;
using SistemaGestionBar.ViewModels;

namespace SistemaGestionBar.Views
{
    public partial class AutorizacionDobleWindow : Window
    {
        public AutorizacionDobleWindow()
        {
            InitializeComponent();
        }

        private void AlAutorizar(object sender, RoutedEventArgs e) =>
            (DataContext as AutorizacionDobleViewModel)?.Confirmar(claveAdmin, claveGerente);
    }
}
