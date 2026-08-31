using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SistemaGestionBar.Models;

namespace SistemaGestionBar.Views
{
    public partial class Dashboard : UserControl
    {
        private class ControlesProducto
        {
            public Border Tarjeta = null!;
            public Button BtnMinus = null!;
            public TextBlock TxtCantidad = null!;
        }

        private readonly Usuario _usuarioActual;
        private readonly List<OrderItem> _orderItems = new List<OrderItem>();
        private readonly Dictionary<string, ControlesProducto> _controlesProductos = new Dictionary<string, ControlesProducto>();
        private decimal _subtotal = 0;
        private string? _categoriaSeleccionada = null;

        private static readonly SolidColorBrush BordeEnOrden = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
        private static readonly SolidColorBrush BordeNormal = new SolidColorBrush(Color.FromRgb(0xE8, 0xE4, 0xD9));

        public Dashboard(Usuario usuarioActual)
        {
            InitializeComponent();
            _usuarioActual = usuarioActual;
            Loaded += Dashboard_Loaded;
        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Fecha actual
            txtFecha.Text = DateTime.Now.ToString("dddd, dd MMMM", new CultureInfo("es-ES"));
            txtTotalOrdenes.Text = "Total : 0 Órdenes";
            txtNombreVendedor.Text = _usuarioActual.Nombre;

            RegistrarControlesProductos();
        }

        private void RegistrarControlesProductos()
        {
            _controlesProductos["Corona Extra"] = new ControlesProducto { Tarjeta = cardCoronaExtra, BtnMinus = btnMinusCoronaExtra, TxtCantidad = txtQtyCoronaExtra };
            _controlesProductos["Heineken"] = new ControlesProducto { Tarjeta = cardHeineken, BtnMinus = btnMinusHeineken, TxtCantidad = txtQtyHeineken };
            _controlesProductos["Mojito"] = new ControlesProducto { Tarjeta = cardMojito, BtnMinus = btnMinusMojito, TxtCantidad = txtQtyMojito };
            _controlesProductos["Margarita"] = new ControlesProducto { Tarjeta = cardMargarita, BtnMinus = btnMinusMargarita, TxtCantidad = txtQtyMargarita };
            _controlesProductos["Whisky Sour"] = new ControlesProducto { Tarjeta = cardWhiskySour, BtnMinus = btnMinusWhiskySour, TxtCantidad = txtQtyWhiskySour };
            _controlesProductos["Limonada"] = new ControlesProducto { Tarjeta = cardLimonada, BtnMinus = btnMinusLimonada, TxtCantidad = txtQtyLimonada };
            _controlesProductos["Agua Mineral"] = new ControlesProducto { Tarjeta = cardAguaMineral, BtnMinus = btnMinusAguaMineral, TxtCantidad = txtQtyAguaMineral };
            _controlesProductos["Refresco"] = new ControlesProducto { Tarjeta = cardRefresco, BtnMinus = btnMinusRefresco, TxtCantidad = txtQtyRefresco };
        }

        // ========== PLACEHOLDER DEL SEARCH ==========
        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Buscar")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = Brushes.Black;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Buscar";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            }
        }

        // ========== FILTRO POR CATEGORÍA ==========
        private void Categoria_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string categoria)
            {
                _categoriaSeleccionada = (_categoriaSeleccionada == categoria) ? null : categoria;
                ActualizarEstiloCategorias();
                FiltrarProductos();
            }
        }

        private void ActualizarEstiloCategorias()
        {
            AplicarEstiloCategoria(borderCatBebidas, txtTituloCatBebidas, txtCantidadCatBebidas, "Bebidas");
            AplicarEstiloCategoria(borderCatCocteles, txtTituloCatCocteles, txtCantidadCatCocteles, "Cócteles");
            AplicarEstiloCategoria(borderCatSinAlcohol, txtTituloCatSinAlcohol, txtCantidadCatSinAlcohol, "Bebidas sin alcohol");
        }

        private void AplicarEstiloCategoria(Border border, TextBlock titulo, TextBlock cantidad, string categoria)
        {
            bool seleccionada = _categoriaSeleccionada == categoria;

            if (seleccionada)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));
                border.BorderThickness = new Thickness(0);
                titulo.Foreground = Brushes.White;
                cantidad.Foreground = new SolidColorBrush(Color.FromRgb(0xBB, 0xDE, 0xFB));
            }
            else
            {
                border.Background = Brushes.White;
                border.BorderThickness = new Thickness(1.5);
                titulo.Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                cantidad.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            }
        }

        private void FiltrarProductos()
        {
            foreach (var child in pnlProductos.Children)
            {
                if (child is Border productoBorder && productoBorder.Tag is string tag)
                {
                    var partes = tag.Split('|');
                    string categoria = partes.Length > 2 ? partes[2] : string.Empty;

                    productoBorder.Visibility = (_categoriaSeleccionada == null || categoria == _categoriaSeleccionada)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }

        // ========== AGREGAR / QUITAR PRODUCTO DEL PEDIDO ==========
        private void ProductoCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tag)
            {
                AgregarProductoAlPedido(tag);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                AgregarProductoAlPedido(tag);
            }
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string nombre)
            {
                var existente = _orderItems.Find(x => x.Nombre == nombre);
                if (existente != null)
                {
                    existente.Cantidad--;
                    if (existente.Cantidad <= 0)
                    {
                        _orderItems.Remove(existente);
                    }
                    ActualizarPedido();
                }
            }
        }

        private void AgregarProductoAlPedido(string tag)
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

        private void QuitarProductoDelPedido(string nombre)
        {
            var existente = _orderItems.Find(x => x.Nombre == nombre);
            if (existente != null)
            {
                _orderItems.Remove(existente);
                ActualizarPedido();
            }
        }

        private void ActualizarTarjetasProductos()
        {
            foreach (var kvp in _controlesProductos)
            {
                var item = _orderItems.Find(x => x.Nombre == kvp.Key);
                int cantidad = item?.Cantidad ?? 0;
                var controles = kvp.Value;

                if (cantidad > 0)
                {
                    controles.Tarjeta.BorderBrush = BordeEnOrden;
                    controles.Tarjeta.BorderThickness = new Thickness(2.5);
                    controles.BtnMinus.Visibility = Visibility.Visible;
                    controles.TxtCantidad.Visibility = Visibility.Visible;
                    controles.TxtCantidad.Text = cantidad.ToString();
                }
                else
                {
                    controles.Tarjeta.BorderBrush = BordeNormal;
                    controles.Tarjeta.BorderThickness = new Thickness(1.5);
                    controles.BtnMinus.Visibility = Visibility.Collapsed;
                    controles.TxtCantidad.Visibility = Visibility.Collapsed;
                }
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
                        Text = $"{item.Cantidad} × ${item.Precio:F2}",
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x89, 0xEF)),
                        Margin = new Thickness(0, 2, 0, 0)
                    });

                    TextBlock totalItem = new TextBlock
                    {
                        Text = $"${item.Precio * item.Cantidad:F2}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 10, 0)
                    };

                    Button btnEliminar = new Button
                    {
                        Content = "✕",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = item.Nombre
                    };
                    btnEliminar.Click += (s, e) => QuitarProductoDelPedido(item.Nombre);

                    Grid.SetColumn(left, 0);
                    Grid.SetColumn(totalItem, 1);
                    Grid.SetColumn(btnEliminar, 2);
                    grid.Children.Add(left);
                    grid.Children.Add(totalItem);
                    grid.Children.Add(btnEliminar);
                    itemBorder.Child = grid;

                    pnlOrderItems.Children.Add(itemBorder);
                }
            }

            // Actualizar totales
            txtSubtotal.Text = $"${_subtotal:F2}";
            txtTotal.Text = $"${_subtotal:F2}";

            ActualizarTarjetasProductos();
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

        // ========== RECETA DE CÓCTELES ==========
        private void BtnReceta_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string nombreCoctel)
            {
                var recetas = DatosPruebaRecetas.ObtenerRecetasMock();
                if (recetas.TryGetValue(nombreCoctel, out var receta))
                {
                    var ventana = new RecetaWindow(nombreCoctel, receta.Ingredientes, receta.Preparacion)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    ventana.ShowDialog();
                }
            }
        }
    }

    // Clase auxiliar para los items del pedido
    public class OrderItem
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}
