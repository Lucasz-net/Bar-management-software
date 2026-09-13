using System.Collections.Generic;
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
            OpcionesCategoria = new ObservableCollection<OpcionFiltro<Categoria?>>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion && FormularioValido);
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

        // ---------------------------------------------------------------
        // Filtros: texto libre, categoría y estado de stock
        // ---------------------------------------------------------------
        public IReadOnlyList<OpcionFiltro<EstadoStock>> OpcionesStock { get; } = new[]
        {
            new OpcionFiltro<EstadoStock>("Todo el catálogo", EstadoStock.Todos),
            new OpcionFiltro<EstadoStock>("Solo a reponer", EstadoStock.AReponer),
            new OpcionFiltro<EstadoStock>("Con stock suficiente", EstadoStock.Suficiente)
        };

        private OpcionFiltro<EstadoStock>? _estadoStock;
        public OpcionFiltro<EstadoStock>? EstadoStockFiltro
        {
            get => _estadoStock;
            set
            {
                if (SetProperty(ref _estadoStock, value))
                    RefrescarVista();
            }
        }

        /// <summary>
        /// Categorías para el combo de filtro, con "Todas" adelante. Es una lista de
        /// opciones y no de categorías para que el combo nunca quede sin selección: un
        /// combo vacío obliga a adivinar qué significa.
        /// </summary>
        public ObservableCollection<OpcionFiltro<Categoria?>> OpcionesCategoria { get; }

        private OpcionFiltro<Categoria?>? _categoriaFiltro;
        public OpcionFiltro<Categoria?>? CategoriaFiltro
        {
            get => _categoriaFiltro;
            set
            {
                if (SetProperty(ref _categoriaFiltro, value))
                    RefrescarVista();
            }
        }

        public override bool HayFiltroAplicado =>
            HayBusqueda
            || (CategoriaFiltro is not null && CategoriaFiltro.Valor is not null)
            || (EstadoStockFiltro is not null && EstadoStockFiltro.Valor != EstadoStock.Todos);

        protected override bool Coincide(object item)
        {
            if (item is not Producto producto)
                return false;

            if (CategoriaFiltro?.Valor is Categoria categoria && producto.IdCategoria != categoria.IdCategoria)
                return false;

            // El estado de stock solo distingue a los productos de venta directa:
            // los que se preparan no tienen stock propio, lo tienen sus insumos.
            var estado = EstadoStockFiltro?.Valor ?? EstadoStock.Todos;
            bool pasaEstado = estado switch
            {
                EstadoStock.AReponer => producto.StockBajo,
                EstadoStock.Suficiente => !producto.StockBajo,
                _ => true
            };

            if (!pasaEstado)
                return false;

            if (!HayBusqueda)
                return true;

            return Contiene(producto.Nombre, Termino)
                || Contiene(producto.Descripcion, Termino)
                || Contiene(producto.Categoria?.NombreCategoria, Termino);
        }

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            CategoriaFiltro = OpcionesCategoria.FirstOrDefault();
            EstadoStockFiltro = OpcionesStock[0];
        }

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
                {
                    OnPropertyChanged(nameof(TituloEditor));
                    Revalidar();
                }
            }
        }

        protected override bool ValidacionActiva => EnEdicion;

        private bool _esAlta;
        public string TituloEditor => !EnEdicion
            ? "Detalle del producto"
            : _esAlta ? "Nuevo producto" : $"Editando: {_nombre}";

        // ---------------------------------------------------------------
        // Campos del editor
        // ---------------------------------------------------------------
        private string _nombre = string.Empty;
        public string Nombre { get => _nombre; set => SetCampo(ref _nombre, value); }

        private string _descripcion = string.Empty;
        public string Descripcion { get => _descripcion; set => SetCampo(ref _descripcion, value); }

        private decimal _precio;
        public decimal Precio { get => _precio; set => SetCampo(ref _precio, value); }

        private int _stock;
        public int Stock { get => _stock; set => SetCampo(ref _stock, value); }

        private int _stockMinimo;
        public int StockMinimo { get => _stockMinimo; set => SetCampo(ref _stockMinimo, value); }

        private Categoria? _categoria;
        public Categoria? CategoriaSeleccionada { get => _categoria; set => SetCampo(ref _categoria, value); }

        private string _rutaImagen = string.Empty;
        public string RutaImagen { get => _rutaImagen; set => SetCampo(ref _rutaImagen, value); }

        private string _instrucciones = string.Empty;
        public string Instrucciones { get => _instrucciones; set => SetCampo(ref _instrucciones, value); }

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

        // ---------------------------------------------------------------
        // Validación (las mismas reglas que aplica el repositorio)
        // ---------------------------------------------------------------
        protected override void DeclararReglas()
        {
            Requerido(Nombre, nameof(Nombre), "El nombre del producto");
            LargoMaximo(Nombre, nameof(Nombre), "El nombre", 80);
            NoRepetido(Nombre, nameof(Nombre),
                       Productos.Where(p => _esAlta || p.IdProducto != Seleccionado?.IdProducto)
                                .Select(p => p.Nombre),
                       "Ya existe un producto con ese nombre.");

            LargoMaximo(Descripcion, nameof(Descripcion), "La descripción", 200);

            MayorACero(Precio, nameof(Precio), "El precio");
            NoNegativo(Stock, nameof(Stock), "El stock");
            NoNegativo(StockMinimo, nameof(StockMinimo), "El stock mínimo");

            RequeridoElegir(CategoriaSeleccionada, nameof(CategoriaSeleccionada), "la categoría");

            // Un producto que se prepara necesita decir cómo (RF-04).
            if (Composicion.Count > 0 && string.IsNullOrWhiteSpace(Instrucciones))
                Agregar(nameof(Instrucciones),
                        "El producto tiene receta: escriba el procedimiento de preparación.");
        }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdProducto;

            Productos.Clear();
            foreach (var p in Repositorio.ObtenerProductos())
                Productos.Add(p);

            Categorias.Clear();
            foreach (var c in Repositorio.ObtenerCategorias())
                Categorias.Add(c);

            // El combo de filtro se rearma con las categorías vigentes, conservando
            // la elección del usuario si esa categoría sigue existiendo.
            int? idCategoriaFiltro = CategoriaFiltro?.Valor?.IdCategoria;
            OpcionesCategoria.Clear();
            OpcionesCategoria.Add(new OpcionFiltro<Categoria?>("Todas las categorías", null));
            foreach (var c in Categorias)
                OpcionesCategoria.Add(new OpcionFiltro<Categoria?>(c.NombreCategoria, c));

            _categoriaFiltro = OpcionesCategoria.FirstOrDefault(o => o.Valor?.IdCategoria == idCategoriaFiltro)
                               ?? OpcionesCategoria[0];
            OnPropertyChanged(nameof(CategoriaFiltro));

            Ingredientes.Clear();
            foreach (var i in Repositorio.ObtenerIngredientes())
                Ingredientes.Add(i);

            IngredienteAAgregar = Ingredientes.FirstOrDefault();

            // El filtro se engancha al final, cuando ya están cargadas las opciones
            // de las que depende el predicado.
            ConfigurarFiltro(Productos);
            _estadoStock ??= OpcionesStock[0];
            RefrescarVista();

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
            Revalidar();
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
            Revalidar();
        }

        private void QuitarIngrediente(LineaRecetaViewModel linea)
        {
            Composicion.Remove(linea);
            OnPropertyChanged(nameof(EsDeVentaDirecta));
            Revalidar();
        }
    }
}
