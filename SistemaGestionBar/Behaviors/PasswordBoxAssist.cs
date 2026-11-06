using System.Windows;
using System.Windows.Controls;

namespace SistemaGestionBar.Behaviors
{
    public static class PasswordBoxAssist
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxAssist),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AlCambiarPassword));

        public static readonly DependencyProperty EnlazarProperty = DependencyProperty.RegisterAttached(
            "Enlazar",
            typeof(bool),
            typeof(PasswordBoxAssist),
            new PropertyMetadata(false, AlCambiarEnlazar));

        private static readonly DependencyProperty ActualizandoProperty = DependencyProperty.RegisterAttached(
            "Actualizando",
            typeof(bool),
            typeof(PasswordBoxAssist),
            new PropertyMetadata(false));

        public static string GetPassword(DependencyObject obj) => (string)obj.GetValue(PasswordProperty);
        public static void SetPassword(DependencyObject obj, string value) => obj.SetValue(PasswordProperty, value);

        public static bool GetEnlazar(DependencyObject obj) => (bool)obj.GetValue(EnlazarProperty);
        public static void SetEnlazar(DependencyObject obj, bool value) => obj.SetValue(EnlazarProperty, value);

        private static bool GetActualizando(DependencyObject obj) => (bool)obj.GetValue(ActualizandoProperty);
        private static void SetActualizando(DependencyObject obj, bool value) => obj.SetValue(ActualizandoProperty, value);

        private static void AlCambiarPassword(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox caja)
                return;

            caja.PasswordChanged -= AlEscribirEnLaCaja;

            if (!GetActualizando(caja))
                caja.Password = e.NewValue as string ?? string.Empty;

            caja.PasswordChanged += AlEscribirEnLaCaja;
        }

        private static void AlCambiarEnlazar(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox caja)
                return;

            if ((bool)e.OldValue)
                caja.PasswordChanged -= AlEscribirEnLaCaja;

            if ((bool)e.NewValue)
                caja.PasswordChanged += AlEscribirEnLaCaja;
        }

        private static void AlEscribirEnLaCaja(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox caja)
                return;

            SetActualizando(caja, true);
            SetPassword(caja, caja.Password);
            SetActualizando(caja, false);
        }
    }
}
