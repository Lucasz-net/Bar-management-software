using System.Windows.Controls;

namespace SistemaGestionBar.Views
{
    /// <summary>
    /// Vista de login. Sin lógica en code-behind: el ViewModel maneja la autenticación
    /// y MainViewModel realiza la navegación según el rol del usuario.
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }
    }
}
