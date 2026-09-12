using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaGestionBar.Views
{
    public partial class FacturaWindow : Window
    {
        public FacturaWindow()
        {
            InitializeComponent();
        }

        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                // Imprimir el contenido de la ventana (el borde principal)
                if (this.Content is Visual visual)
                {
                    pd.PrintVisual(visual, "Factura");
                }
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
