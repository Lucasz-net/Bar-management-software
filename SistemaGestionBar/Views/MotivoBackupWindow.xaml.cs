using System.Windows;

namespace SistemaGestionBar.Views
{
    public partial class MotivoBackupWindow : Window
    {
        public MotivoBackupWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => txtMotivo.Focus();
        }
    }
}
