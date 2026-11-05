using System.Windows;
using MaterialDesignThemes.Wpf;

namespace SistemaGestionBar.Behaviors
{
    /// <summary>Ícono opcional que se dibuja a la izquierda dentro de un ComboBox.</summary>
    public static class ComboAssist
    {
        public static readonly DependencyProperty IconoProperty =
            DependencyProperty.RegisterAttached("Icono", typeof(PackIconKind?), typeof(ComboAssist),
                new PropertyMetadata(null));

        public static PackIconKind? GetIcono(DependencyObject objeto) =>
            (PackIconKind?)objeto.GetValue(IconoProperty);

        public static void SetIcono(DependencyObject objeto, PackIconKind? valor) =>
            objeto.SetValue(IconoProperty, valor);
    }
}
