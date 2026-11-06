using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SistemaGestionBar.Behaviors
{
    public static class SelectorAssist
    {
        public static readonly DependencyProperty DesplazarASeleccionProperty =
            DependencyProperty.RegisterAttached(
                "DesplazarASeleccion",
                typeof(bool),
                typeof(SelectorAssist),
                new PropertyMetadata(false, AlCambiarElDesplazamiento));

        public static bool GetDesplazarASeleccion(DependencyObject objeto) =>
            (bool)objeto.GetValue(DesplazarASeleccionProperty);

        public static void SetDesplazarASeleccion(DependencyObject objeto, bool valor) =>
            objeto.SetValue(DesplazarASeleccionProperty, valor);

        private static void AlCambiarElDesplazamiento(DependencyObject objeto, DependencyPropertyChangedEventArgs e)
        {
            if (objeto is not Selector selector)
                return;

            selector.SelectionChanged -= AlCambiarLaSeleccion;

            if (e.NewValue is true)
                selector.SelectionChanged += AlCambiarLaSeleccion;
        }

        private static void AlCambiarLaSeleccion(object remitente, SelectionChangedEventArgs e)
        {
            if (remitente is not Selector selector || selector.SelectedItem is null)
                return;

            selector.Dispatcher.BeginInvoke(new Action(() =>
            {
                object? item = selector.SelectedItem;
                if (item is null)
                    return;

                switch (selector)
                {
                    case DataGrid grilla:
                        grilla.ScrollIntoView(item, grilla.Columns.FirstOrDefault());
                        break;
                    case ListBox lista:
                        lista.ScrollIntoView(item);
                        break;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        public static readonly DependencyProperty SeleccionBloqueadaProperty =
            DependencyProperty.RegisterAttached(
                "SeleccionBloqueada",
                typeof(bool),
                typeof(SelectorAssist),
                new PropertyMetadata(false, AlCambiarElBloqueo));

        public static bool GetSeleccionBloqueada(DependencyObject objeto) =>
            (bool)objeto.GetValue(SeleccionBloqueadaProperty);

        public static void SetSeleccionBloqueada(DependencyObject objeto, bool valor) =>
            objeto.SetValue(SeleccionBloqueadaProperty, valor);

        private static void AlCambiarElBloqueo(DependencyObject objeto, DependencyPropertyChangedEventArgs e)
        {
            if (objeto is not Selector selector)
                return;

            selector.PreviewMouseLeftButtonDown -= AlIntentarSeleccionarConElMouse;
            selector.PreviewKeyDown -= AlIntentarSeleccionarConElTeclado;

            if (e.NewValue is not true)
                return;

            selector.PreviewMouseLeftButtonDown += AlIntentarSeleccionarConElMouse;
            selector.PreviewKeyDown += AlIntentarSeleccionarConElTeclado;
        }

        private static void AlIntentarSeleccionarConElMouse(object remitente, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject origen && EsUnaFila(origen))
                e.Handled = true;
        }

        private static void AlIntentarSeleccionarConElTeclado(object remitente, KeyEventArgs e)
        {
            e.Handled = e.Key is Key.Up or Key.Down or Key.Left or Key.Right
                              or Key.Home or Key.End or Key.PageUp or Key.PageDown
                              or Key.Space;
        }

        private static bool EsUnaFila(DependencyObject origen)
        {
            for (var actual = origen; actual is not null; actual = Padre(actual))
            {
                if (actual is ScrollBar)
                    return false;

                if (actual is ListBoxItem or DataGridRow or DataGridCell)
                    return true;
            }

            return false;
        }

        private static DependencyObject? Padre(DependencyObject objeto) =>
            objeto is Visual or Visual3D
                ? VisualTreeHelper.GetParent(objeto)
                : LogicalTreeHelper.GetParent(objeto);
    }
}
