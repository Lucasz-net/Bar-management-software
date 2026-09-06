using System.Windows;

namespace SistemaGestionBar.Views
{
    /// <summary>
    /// Modal de receta (RF-04). Lo abre ServicioDialogo con un RecetaViewModel
    /// como DataContext; el boton cierra por IsCancel, sin code-behind.
    /// </summary>
    public partial class RecetaWindow : Window
    {
        public RecetaWindow()
        {
            InitializeComponent();
        }
    }
}
