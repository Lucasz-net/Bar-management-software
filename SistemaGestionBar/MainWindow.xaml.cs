using System.Windows;

namespace SistemaGestionBar
{
    /// <summary>
    /// Cascaron de la aplicacion. Sin logica: el DataContext se declara en el XAML
    /// y toda la navegacion vive en MainViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
