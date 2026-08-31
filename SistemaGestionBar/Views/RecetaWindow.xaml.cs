using System.Collections.Generic;
using System.Windows;

namespace SistemaGestionBar.Views
{
    public partial class RecetaWindow : Window
    {
        public RecetaWindow(string nombreCoctel, List<string> ingredientes, string preparacion)
        {
            InitializeComponent();
            txtNombreCoctel.Text = nombreCoctel;
            lstIngredientes.ItemsSource = ingredientes;
            txtPreparacion.Text = preparacion;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
