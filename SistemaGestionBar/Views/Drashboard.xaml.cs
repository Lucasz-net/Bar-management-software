using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SistemaGestionBar.Views
{
    public partial class Drashboard : UserControl
    {
        private List<OrderItem> _orderItems = new List<OrderItem>();
        private decimal _subtotal = 0;

        public Drashboard()
        {
            InitializeComponent();
            Loaded += Drashboard_Loaded;
        }

        private void Drashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Fecha actual
            txtFecha.Text = DateTime.Now.ToString("dddd, dd MMMM", new CultureInfo("es-ES"));
            txtTotalOrdenes.Text = "Total : 0 Órdenes";
        }

        // ========== PLACEHOLDER DEL SEARCH ==========
        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = Brushes.Black;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            }
        }

        // ========== AGREGAR PRODUCTO AL PEDIDO ==========
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split('|');
                string nombre = parts[0];
                decimal precio = decimal.Parse(parts[1], CultureInfo.InvariantCulture);

                // Buscar si ya existe
                var existente = _orderItems.Find(x => x.Nombre == nombre);
                if (existente != null)
                {
                    existente.Cantidad++;
                }
                else
                {
                    _orderItems.Add(new OrderItem { Nombre = nombre, Precio = precio, Cantidad = 1 });
                }

                ActualizarPedido();
            }
        }

        private void ActualizarPedido()
        {
            pnlOrderItems.Children.Clear();
            _subtotal = 0;

            if (_orderItems.Count == 0)
            {
                txtEmptyOrder.Visibility = Visibility.Visible;
                pnlOrderItems.Children.Add(txtEmptyOrder);
            }
            else
            {
                txtEmptyOrder.Visibility = Visibility.Collapsed;

                foreach (var item in _orderItems)
                {
                    _subtotal += item.Precio * item.Cantidad;

                    // Crear visual del item
                    Border itemBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(248, 248, 248)),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    Grid grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    StackPanel left = new StackPanel();
                    left.Children.Add(new TextBlock
                    {
                        Text = item.Nombre,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                    });
                    left.Children.Add(new TextBlock
                    {
                        Text = $"${item.Precio:F2} × {item.Cantidad}",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                        Margin = new Thickness(0, 2, 0, 0)
                    });

                    TextBlock totalItem = new TextBlock
                    {
                        Text = $"${item.Precio * item.Cantidad:F2}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Grid.SetColumn(left, 0);
                    Grid.SetColumn(totalItem, 1);
                    grid.Children.Add(left);
                    grid.Children.Add(totalItem);
                    itemBorder.Child = grid;

                    pnlOrderItems.Children.Add(itemBorder);
                }
            }

            // Actualizar totales
            decimal impuesto = _subtotal * 0.16m;
            decimal total = _subtotal + impuesto;

            txtSubtotal.Text = $"${_subtotal:F2}";
            txtImpuesto.Text = $"${impuesto:F2}";
            txtTotal.Text = $"${total:F2}";
        }

        private void BtnPagar_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Count == 0)
            {
                MessageBox.Show("No hay productos en el pedido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"Cobrar total: {txtTotal.Text}\n\n¡Pedido procesado!", "Pago", MessageBoxButton.OK, MessageBoxImage.Information);

            // Limpiar pedido
            _orderItems.Clear();
            ActualizarPedido();
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abrir reportes...", "Reportes");
        }
    }

    // Clase auxiliar para los items del pedido
    public class OrderItem
    {
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}