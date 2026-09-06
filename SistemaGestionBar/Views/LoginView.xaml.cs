using System.Windows.Controls;

namespace SistemaGestionBar.Views
{
    /// <summary>
    /// Vista de login. Sin logica: el enlace del PasswordBox lo resuelve
    /// la propiedad adjunta PasswordBoxAssist y el resto son bindings.
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }
    }
}
