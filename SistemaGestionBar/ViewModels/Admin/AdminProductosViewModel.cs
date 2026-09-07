using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// ABM de productos (RF-12) con el editor de receta incorporado.
    ///
    /// El editor trabaja sobre campos sueltos y no sobre la entidad seleccionada:
    /// si escribiéramos directo sobre el Producto de la lista, cancelar no podría
    /// deshacer nada porque los cambios ya estarían aplicados.
    /// </summary>
    public class AdminProductosViewModel : SeccionAdminViewModel
    {
        public AdminProductosViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, "Productos", "GlassCocktail",
                   "Catálogo del punto de venta, precios, stock y recetas")
        {
            Productos = new ObservableCollection<Producto>();
            Categorias = new ObservableCollection<Categoria>();
            Ingredientes = new ObservableCollection<Ingrediente>();
            Composicion = new ObservableCollection<LineaRecetaViewModel>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null && !EnEdicion);
            AgregarIngredienteCommand = new RelayCommand(AgregarIngrediente, () => EnEdicion && Ingredientes.Any());
            QuitarIngredienteCommand = new RelayCommand<LineaRecetaViewModel>(QuitarIngrediente);

            Recargar();
        }

        public ObservableCollection<Producto> Productos { get; }
        public ObservableCollection<Categoria> Categorias { get; }
        public ObservableCollection<Ingrediente> Ingredientes { get; }
        public ObservableCollection<LineaRecetaViewModel> Composicion { get; }

        private Producto? _seleccionado;
        public Producto? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (!SetProperty(ref _seleccionado, value))
                    return;

                if (!EnEdicion)
                    CargarEnEditor(value);
            }
        }

        private bool _enEdicion;
        public bool EnEdicion
        {
            get => _enEdicion;
            private set
            {
                if (SetProperty(ref _enEdicion, value))
                    OnPropertyChanged(nameof(TituloEditor));
            }
        }

        private bool _esAlta;
        public string TituloEditor => !EnEdicion
            ? "Detalle del producto"
            : _esAlta ? "Nuevo producto" : $"Editando: {_nombre}";

        // ---------------------------------------------------------------
        // Campos del editor
        // ---------------------------------------------------------------
        private string _nombre = string.Empty;
        public string Nombre { get => _nombre; set => SetProperty(ref _nombre, value); }

        private string _descripcion = string.Empty;
        public string Descripcion { get => _descripcion; set => SetProperty(ref _descripcion, value); }

        private decimal _precio;
        public decimal Precio { get => _precio; set => SetProperty(ref _precio, value); }

        private int _stock;
        public int Stock { get => _stock; set => SetProperty(ref _stock, value); }

        private int _stockMinimo;
        public int StockMinimo { get => _stockMinimo; set => SetProperty(ref _stockMinimo, value); }

        private Categoria? _categoria;
        public Categoria? CategoriaSeleccionada { get => _categoria; set => SetProperty(ref _categoria, value); }

        private string _rutaImagen = string.Empty;
        public string RutaImagen { get => _rutaImagen; set => SetProperty(ref _rutaImagen, value); }

        private string _instrucciones = string.Empty;
        public string Instrucciones { get => _instrucciones; set => SetProperty(ref _instrucciones, value); }

        private Ingrediente? _ingredienteAAgregar;
        public Ingrediente? IngredienteAAgregar { get => _ingredienteAAgregar; set => SetProperty(ref _ingredienteAAgregar, value); }

        /// <summary>Si no hay composición, el producto es de venta directa y usa su propio stock.</summary>
        public bool EsDeVentaDirecta => Composicion.Count == 0;

        // ---------------------------------------------------------------
        // Comandos
        // ---------------------------------------------------------------
        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand AgregarIngredienteCommand { get; }
        public ICommand QuitarIngredienteCommand { get; }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdProducto;

            Productos.Clear();
            foreach (var p in Repositorio.ObtenerProductos())
                Productos.Add(p);

            Categorias.Clear();
            foreach (var c in Repositorio.ObtenerCategorias())
                Categorias.Add(c);

            Ingredientes.Clear();
            foreach (var i in Repositorio.ObtenerIngredientes())
                Ingredientes.Add(i);

            IngredienteAAgregar = Ingredientes.FirstOrDefault();
            Seleccionado = Productos.FirstOrDefault(p => p.IdProducto == idPrevio) ?? Productos.FirstOrDefault();
        }

        private void CargarEnEditor(Producto? producto)
        {
            Nombre = producto?.Nombre ?? string.Empty;
            Descripcion = producto?.Descripcion ?? string.Empty;
            Precio = producto?.Precio ?? 0;
            Stock = producto?.Stock ?? 0;
            StockMinimo = producto?.StockMinimo ?? 0;
            RutaImagen = producto?.RutaImagen ?? string.Empty;
            Instrucciones = producto?.Receta?.Instrucciones ?? string.Empty;
            CategoriaSeleccionada = producto is null
                ? Categorias.FirstOrDefault()
                : Categorias.FirstOrDefault(c => c.IdCategoria == producto.IdCategoria);

            Composicion.Clear();
            if (producto is not null)
            {
                foreach (var linea in producto.Ingredientes.OrderByDescending(i => i.CantidadNecesaria))
                    Composicion.Add(new LineaRecetaViewModel(linea.Ingrediente, linea.CantidadNecesaria));
            }

            OnPropertyChanged(nameof(EsDeVentaDirecta));
        }

        private void Nuevo()
        {
            LimpiarMensaje();
            _esAlta = true;
            EnEdicion = true;
            Seleccionado = null;
            CargarEnEditor(null);
        }

        private void Editar()
        {
            if (Seleccionado is null)
                return;

            LimpiarMensaje();
            _esAlta = false;
            EnEdicion = true;
            CargarEnEditor(Seleccionado);
        }

        private void Cancelar()
        {
            EnEdicion = false;
            _esAlta = false;
            LimpiarMensaje();
            CargarEnEditor(Seleccionado);
        }

        private void Guardar()
        {
            var producto = _esAlta ? new Producto() : Seleccionado;
            if (producto is null)
            {
                Fallar("No hay un producto seleccionado.");
                return;
            }

            // Se copian los campos del editor recién ahora, cuando el usuario confirmó.
            producto.Nombre = Nombre;
            producto.Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();
            producto.Precio = Precio;
            producto.Stock = Stock;
            producto.StockMinimo = StockMinimo;
            producto.RutaImagen = string.IsNullOrWhiteSpace(RutaImagen) ? null : RutaImagen.Trim();
            producto.IdCategoria = CategoriaSeleccionada?.IdCategoria ?? 0;

            var resultado = Repositorio.GuardarProducto(producto);
            if (!Aplicar(resultado))
                return;

            var receta = Repositorio.GuardarReceta(
                producto.IdProducto,
                Instrucciones,
                Composicion.Select(l => l.AFila(producto.IdProducto)));

            if (!Aplicar(receta))
                return;

            EnEdicion = false;
            _esAlta = false;
            Informar(resultado.Mensaje);

            Recargar();
            Seleccionado = Productos.FirstOrDefault(p => p.IdProducto == producto.IdProducto);
        }

        private void Eliminar()
        {
            if (Seleccionado is null)
                return;

            if (!Dialogo.Confirmar("Eliminar producto",
                    $"¿Eliminar \"{Seleccionado.Nombre}\" del catálogo?"))
                return;

            if (Aplicar(Repositorio.EliminarProducto(Seleccionado.IdProducto)))
                Recargar();
        }

        private void AgregarIngrediente()
        {
            if (IngredienteAAgregar is null)
                return;

            if (Composicion.Any(l => l.Ingrediente.IdIngrediente == IngredienteAAgregar.IdIngrediente))
            {
                Fallar($"\"{IngredienteAAgregar.Nombre}\" ya está en la receta.");
                return;
            }

            Composicion.Add(new LineaRecetaViewModel(IngredienteAAgregar, 1));
            OnPropertyChanged(nameof(EsDeVentaDirecta));
            LimpiarMensaje();
        }

        private void QuitarIngrediente(LineaRecetaViewModel linea)
        {
            Composicion.Remove(linea);
            OnPropertyChanged(nameof(EsDeVentaDirecta));
        }
    }
}
