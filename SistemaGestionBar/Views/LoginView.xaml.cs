using System.Windows.Controls;

namespace SistemaGestionBar.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            Loaded += (_, _) => CampoCorreo.Focus();
        }
    }
}
