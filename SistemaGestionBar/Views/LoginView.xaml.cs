using System.Windows;
using System.Windows.Controls;

namespace SistemaGestionBar.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(password))
            {
                txtMensaje.Text = "Complete todos los campos.";
                return;
            }

            // Usuario de prueba
            if (usuario == "admin" && password == "1234")
            {
                txtMensaje.Foreground =
                    System.Windows.Media.Brushes.Green;

                txtMensaje.Text = "¡Inicio de sesión correcto!";

                // Acá posteriormente podemos abrir el menú principal.
            }
            else
            {
                txtMensaje.Foreground =
                    System.Windows.Media.Brushes.Red;

                txtMensaje.Text = "Usuario o contraseña incorrectos.";
            }
        }
    }
}
