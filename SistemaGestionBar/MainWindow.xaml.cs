using System.Windows;
using SistemaGestionBar.ViewModels;

namespace SistemaGestionBar
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}