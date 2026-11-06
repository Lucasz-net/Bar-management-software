using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminProductosViewModel : SeccionAdminViewModel
    {
        public AdminProductosViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Productos, "Productos", "GlassCocktail",
                   "Catálogo del punto de venta, precios, stock y recetas")
        {
            Productos = new ObservableCollection<Producto>();
            Categorias = new ObservableCollection<Categoria>();
            Ingredientes = new ObservableCollection<Ingrediente>();
            Composicion = new ObservableCollection<LineaRecetaViewModel>();
            OpcionesCategoria = new ObservableCollection<OpcionFiltro<Categoria?>>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null && !EnEdicion);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null && !EnEdicion);
            AjustarStockCommand = new RelayCommand(AjustarStock,
                                                  () => Seleccionado is not null && !EnEdicion && !Seleccionado.TieneReceta);
            AgregarIngredienteCommand = new RelayCommand(AgregarIngrediente, () => EnEdicion && Ingredientes.Any());
            QuitarIngredienteCommand = new RelayCommand<LineaRecetaViewModel>(QuitarIngrediente);
            ElegirImagenCommand = new RelayCommand(ElegirImagen, () => EnEdicion);

            Recargar();
        }

        public ObservableCollection<Producto> Productos { get; }
        public ObservableCollection<Categoria> Categorias { get; }
        public ObservableCollection<Ingrediente> Ingredientes { get; }
        public ObservableCollection<LineaRecetaViewModel> Composicion { get; }

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
            || CategoriaFiltro?.Valor is not null
            || EstadoStockFiltro?.Valor is not (null or EstadoStock.Todos);

        protected override bool Coincide(object item)
        {
            if (item is not Producto producto)
                return false;

            if (CategoriaFiltro?.Valor is Categoria categoria && producto.IdCategoria != categoria.IdCategoria)
                return false;

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

        // // Durante la edición no se puede cambiar de producto, para no perder los cambios.
        public Producto? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (EnEdicion)
                {
                    if (ReferenceEquals(_seleccionado, value))
                        return;

                    if (value is not null)
                        Fallar("Terminá la edición —guardá o cancelá— antes de elegir otro producto.");

                    OnPropertyChanged(nameof(Seleccionado));
                    return;
                }

                EstablecerSeleccion(value);
            }
        }

        private void EstablecerSeleccion(Producto? producto)
        {
            if (!SetProperty(ref _seleccionado, producto, nameof(Seleccionado)))
                return;

            OnPropertyChanged(nameof(HaySeleccion));
            OnPropertyChanged(nameof(RecetaDelSeleccionado));

            if (!EnEdicion)
                CargarEnEditor(producto);
        }

        public bool HaySeleccion => Seleccionado is not null;

        public string RecetaDelSeleccionado =>
            string.IsNullOrWhiteSpace(Seleccionado?.Receta?.Instrucciones)
                ? "Sin procedimiento cargado."
                : Seleccionado!.Receta!.Instrucciones!;

        private bool _enEdicion;
        public bool EnEdicion
        {
            get => _enEdicion;
            private set
            {
                if (SetProperty(ref _enEdicion, value))
                {
                    OnPropertyChanged(nameof(NoEnEdicion));
                    OnPropertyChanged(nameof(TituloEditor));
                    Revalidar();
                }
            }
        }

        public bool NoEnEdicion => !EnEdicion;

        protected override bool ValidacionActiva => EnEdicion;

        private bool _esAlta;
        public string TituloEditor => !EnEdicion
            ? "Detalle del producto"
            : _esAlta ? "Nuevo producto" : $"Editando: {_nombre}";

        private bool _cargando;

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set { if (SetCampo(ref _nombre, value)) OnPropertyChanged(nameof(TituloEditor)); }
        }

        private string _descripcion = string.Empty;
        public string Descripcion { get => _descripcion; set => SetCampo(ref _descripcion, value); }

        private decimal _precio;
        public decimal Precio { get => _precio; set => SetCampo(ref _precio, value); }

        private int _stock;
        public int Stock { get => _stock; set => SetCampo(ref _stock, value); }

        private int _stockMinimo;
        public int StockMinimo { get => _stockMinimo; set => SetCampo(ref _stockMinimo, value); }

        private Categoria? _categoria;
        public Categoria? CategoriaSeleccionada
        {
            get => _categoria;
            set
            {
                if (!SetCampo(ref _categoria, value))
                    return;

                if (!_cargando && !CategoriaAdmiteReceta)
                {
                    LimpiarComposicion();
                    Instrucciones = string.Empty;
                }

                RefrescarVisibilidadDeReceta();
            }
        }

        private string _rutaImagen = string.Empty;
        public string RutaImagen
        {
            get => _rutaImagen;
            set
            {
                if (SetCampo(ref _rutaImagen, value))
                    OnPropertyChanged(nameof(TieneImagen));
            }
        }

        private string _instrucciones = string.Empty;
        public string Instrucciones { get => _instrucciones; set => SetCampo(ref _instrucciones, value); }

        private Ingrediente? _ingredienteAAgregar;
        public Ingrediente? IngredienteAAgregar { get => _ingredienteAAgregar; set => SetProperty(ref _ingredienteAAgregar, value); }

        public bool EsDeVentaDirecta => Composicion.Count == 0;

        public bool CategoriaAdmiteReceta => EsCategoriaPreparada(CategoriaSeleccionada);

        public bool MostrarReceta => CategoriaAdmiteReceta || Composicion.Count > 0;

        // // Solo los cócteles se preparan con receta; el resto es de venta directa.
        private static bool EsCategoriaPreparada(Categoria? categoria)
        {
            if (categoria is null)
                return false;

            return SinAcentos(categoria.NombreCategoria).Contains("coctel", StringComparison.OrdinalIgnoreCase);
        }

        private static string SinAcentos(string texto)
        {
            var descompuesto = texto.Normalize(NormalizationForm.FormD);
            var limpio = new StringBuilder(descompuesto.Length);

            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    limpio.Append(c);
            }

            return limpio.ToString().Normalize(NormalizationForm.FormC);
        }

        private void RefrescarVisibilidadDeReceta()
        {
            OnPropertyChanged(nameof(CategoriaAdmiteReceta));
            OnPropertyChanged(nameof(MostrarReceta));
            OnPropertyChanged(nameof(EsDeVentaDirecta));
        }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand AjustarStockCommand { get; }
        public ICommand AgregarIngredienteCommand { get; }
        public ICommand QuitarIngredienteCommand { get; }
        public ICommand ElegirImagenCommand { get; }

        public bool TieneImagen => !string.IsNullOrWhiteSpace(RutaImagen);

        protected override void DeclararReglas()
        {
            Requerido(Nombre, nameof(Nombre), "El nombre del producto");
            LargoMaximo(Nombre, nameof(Nombre), "El nombre", Limites.NombreProducto);
            NoRepetido(Nombre, nameof(Nombre),
                       Productos.Where(p => _esAlta || p.IdProducto != Seleccionado?.IdProducto)
                                .Select(p => p.Nombre),
                       "Ya existe un producto con ese nombre.");

            LargoMaximo(Descripcion, nameof(Descripcion), "La descripción", Limites.DescripcionProducto);
            LargoMaximo(RutaImagen, nameof(RutaImagen), "La ruta de la imagen", Limites.RutaImagen);

            MayorACero(Precio, nameof(Precio), "El precio");
            NoSupera(Precio, nameof(Precio), "El precio", Limites.Dinero);
            NoNegativo(Stock, nameof(Stock), "El stock");
            NoNegativo(StockMinimo, nameof(StockMinimo), "El stock mínimo");

            RequeridoElegir(CategoriaSeleccionada, nameof(CategoriaSeleccionada), "la categoría");

            if (Composicion.Count > 0 && string.IsNullOrWhiteSpace(Instrucciones))
                Agregar(nameof(Instrucciones),
                        "El producto tiene receta: escriba el procedimiento de preparación.");

            foreach (var linea in Composicion.Where(l => l.Cantidad <= 0))
                Agregar(nameof(Composicion),
                        $"La cantidad de \"{linea.Ingrediente.Nombre}\" debe ser mayor a cero.");

            foreach (var linea in Composicion.Where(l => l.Cantidad > Limites.Cantidad))
                Agregar(nameof(Composicion),
                        $"La cantidad de \"{linea.Ingrediente.Nombre}\" no puede superar " +
                        $"{Limites.Cantidad:N3}.");
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

            ConfigurarFiltro(Productos);
            _estadoStock ??= OpcionesStock[0];
            RefrescarVista();

            EstablecerSeleccion(Productos.FirstOrDefault(p => p.IdProducto == idPrevio) ?? Productos.FirstOrDefault());
        }

        public override void Enfocar(object? foco)
        {
            if (foco is not AlertaStock alerta || alerta.EsInsumo)
                return;

            var producto = Productos.FirstOrDefault(p => p.IdProducto == alerta.Id);
            if (producto is null)
                return;

            EstablecerSeleccion(producto);

            AjustarStock();
        }

        private void ElegirImagen()
        {
            var (seAplico, ruta) = Dialogo.ElegirImagenDeProducto(TieneImagen ? RutaImagen : null);
            if (seAplico)
                RutaImagen = ruta ?? string.Empty;
        }

        private void AjustarStock()
        {
            if (Seleccionado is null || Seleccionado.TieneReceta)
                return;

            LimpiarMensaje();

            var ajuste = AjusteDeStockViewModel.DeProducto(Repositorio, Seleccionado);

            if (!Dialogo.AjustarStock(ajuste))
                return;

            Recargar();
            Informar(ajuste.MensajeDelGuardado);
        }

        private void CargarEnEditor(Producto? producto)
        {
            _cargando = true;

            Nombre = producto?.Nombre ?? string.Empty;
            Descripcion = producto?.Descripcion ?? string.Empty;
            Precio = producto?.Precio ?? 0;
            Stock = producto?.Stock ?? 0;
            StockMinimo = producto?.StockMinimo ?? 0;
            RutaImagen = producto?.RutaImagen ?? string.Empty;
            Instrucciones = producto?.Receta?.Instrucciones ?? string.Empty;
            CategoriaSeleccionada = producto is null
                ? null
                : Categorias.FirstOrDefault(c => c.IdCategoria == producto.IdCategoria);

            LimpiarComposicion();
            if (producto is not null)
            {
                foreach (var linea in producto.Ingredientes.OrderByDescending(i => i.CantidadNecesaria))
                    Composicion.Add(Vigilar(new LineaRecetaViewModel(linea.Ingrediente, linea.CantidadNecesaria)));
            }

            _cargando = false;

            RefrescarVisibilidadDeReceta();

            ReiniciarValidacion();
        }

        private void Nuevo()
        {
            LimpiarMensaje();
            _esAlta = true;
            EstablecerSeleccion(null);
            CargarEnEditor(null);
            EnEdicion = true;
            ReiniciarValidacion();
        }

        private void Editar()
        {
            if (Seleccionado is null)
                return;

            LimpiarMensaje();
            _esAlta = false;
            CargarEnEditor(Seleccionado);
            EnEdicion = true;
            ReiniciarValidacion();
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
            if (!IntentarConfirmar())
            {
                Fallar(MensajeDeFormularioInvalido);
                return;
            }

            var producto = _esAlta ? new Producto() : Seleccionado;
            if (producto is null)
            {
                Fallar("No hay un producto seleccionado.");
                return;
            }

            producto.Nombre = Nombre.Trim();
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
            EstablecerSeleccion(Productos.FirstOrDefault(p => p.IdProducto == producto.IdProducto));
        }

        private void Eliminar()
        {
            if (Seleccionado is null)
                return;

            if (!ConfirmarEliminacion("el producto", Seleccionado.Nombre))
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

            Composicion.Add(Vigilar(new LineaRecetaViewModel(IngredienteAAgregar, 1)));
            RefrescarVisibilidadDeReceta();
            LimpiarMensaje();
            Revalidar();
        }

        private void QuitarIngrediente(LineaRecetaViewModel linea)
        {
            linea.PropertyChanged -= AlCambiarUnaLinea;
            Composicion.Remove(linea);
            RefrescarVisibilidadDeReceta();
            Revalidar();
        }

        private LineaRecetaViewModel Vigilar(LineaRecetaViewModel linea)
        {
            linea.PropertyChanged += AlCambiarUnaLinea;
            return linea;
        }

        private void LimpiarComposicion()
        {
            foreach (var linea in Composicion)
                linea.PropertyChanged -= AlCambiarUnaLinea;

            Composicion.Clear();
        }

        private void AlCambiarUnaLinea(object? origen, System.ComponentModel.PropertyChangedEventArgs e) =>
            Revalidar();
    }
}
