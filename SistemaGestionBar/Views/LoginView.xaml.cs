using System.Windows.Controls;

namespace SistemaGestionBar.Views
{
    /// <summary>
    /// Vista de login. La autenticación la hace el ViewModel y la navegación por rol la
    /// hace MainViewModel; lo único que vive acá es dónde arranca el cursor.
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            // El foco inicial se pone al cargar y no con FocusManager.FocusedElement en el
            // XAML: esa propiedad se evalúa mientras se construye el árbol, cuando el
            // nombre CampoCorreo todavía no está registrado, y el binding queda roto.
            //
            // Es puro asunto de la vista —en qué campo aparece el cursor— así que no tiene
            // por qué pasar por el ViewModel.
            Loaded += (_, _) => CampoCorreo.Focus();
        }
    }
}
