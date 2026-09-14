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
    /// <summary>
    /// ABM de productos (RF-12) con el editor de receta incorporado.
    ///
    /// El editor trabaja sobre campos sueltos y no sobre la entidad seleccionada:
    /// si escribiéramos directo sobre el Producto de la lista, cancelar no podría
    /// deshacer nada porque los cambios ya estarían aplicados.
    ///
    /// El panel derecho tiene dos caras: con el formulario cerrado muestra la ficha del
    /// producto elegido y ofrece Editar y Eliminar; con el formulario abierto muestra los
    /// campos y ofrece Guardar y Cancelar. Las acciones están al pie de los datos que
    /// afectan, no arriba de la lista: así no hay que acordarse de qué tarjeta quedó
    /// seleccionada para saber qué se va a borrar.
    /// </summary>
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

        // ---------------------------------------------------------------
        // Selección
        // ---------------------------------------------------------------
        private Producto? _seleccionado;

        /// <summary>
        /// Lo que elige el usuario en el catálogo.
        ///
        /// <b>Con el formulario abierto la selección queda congelada.</b> Si se pudiera
        /// mover, quedaría remarcada una tarjeta que no es la que está mostrando el
        /// editor: el usuario vería "Vino Malbec" resaltado mientras edita otra cosa.
        /// Se rechaza el cambio y se vuelve a avisar la propiedad, que es lo que hace
        /// que la lista devuelva la selección a donde estaba.
        /// </summary>
        public Producto? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (EnEdicion)
                {
                    if (ReferenceEquals(_seleccionado, value))
                        return;

                    // Solo se avisa cuando el intento vino de un clic. Si el filtro dejó
                    // la lista sin selección (value nulo) no hay nada que explicar.
                    if (value is not null)
                        Fallar("Terminá la edición —guardá o cancelá— antes de elegir otro producto.");

                    OnPropertyChanged(nameof(Seleccionado));
                    return;
                }

                EstablecerSeleccion(value);
            }
        }

        /// <summary>
        /// Cambia la selección sin pasar por el candado. Lo usan Recargar y Enfocar, que
        /// mueven la selección por su cuenta y no son el usuario haciendo clic.
        /// </summary>
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

        /// <summary>Procedimiento del producto elegido, para la ficha de solo lectura.</summary>
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

        /// <summary>La ficha de solo lectura y el formulario son excluyentes.</summary>
        public bool NoEnEdicion => !EnEdicion;

        protected override bool ValidacionActiva => EnEdicion;

        private bool _esAlta;
        public string TituloEditor => !EnEdicion
            ? "Detalle del producto"
            : _esAlta ? "Nuevo producto" : $"Editando: {_nombre}";

        // ---------------------------------------------------------------
        // Campos del editor
        // ---------------------------------------------------------------
        /// <summary>
        /// Mientras se carga la ficha en el editor los setters no deben reaccionar como
        /// si el usuario hubiera cambiado algo: cambiar de categoría borra la receta, y
        /// eso no puede pasar por el solo hecho de abrir un producto.
        /// </summary>
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

                // Pasar un cóctel a Botellas deja una receta que ya no se ve: se limpia
                // acá, mientras el usuario está mirando, y no en silencio al guardar.
                if (!_cargando && !CategoriaAdmiteReceta)
                {
                    LimpiarComposicion();
                    Instrucciones = string.Empty;
                }

                RefrescarVisibilidadDeReceta();
            }
        }

        private string _rutaImagen = string.Empty;
        public string RutaImagen { get => _rutaImagen; set => SetCampo(ref _rutaImagen, value); }

        private string _instrucciones = string.Empty;
        public string Instrucciones { get => _instrucciones; set => SetCampo(ref _instrucciones, value); }

        private Ingrediente? _ingredienteAAgregar;
        public Ingrediente? IngredienteAAgregar { get => _ingredienteAAgregar; set => SetProperty(ref _ingredienteAAgregar, value); }

        /// <summary>Si no hay composición, el producto es de venta directa y usa su propio stock.</summary>
        public bool EsDeVentaDirecta => Composicion.Count == 0;

        // ---------------------------------------------------------------
        // Qué categorías llevan receta
        // ---------------------------------------------------------------
        /// <summary>
        /// Una botella o una gaseosa se venden cerradas: no tienen procedimiento ni
        /// insumos, así que mostrarles el bloque de receta es ofrecer un formulario que
        /// nunca se completa. Solo los cócteles se preparan.
        ///
        /// La condición mira el NOMBRE de la categoría porque el DER no tiene una columna
        /// que diga "esta categoría se prepara"; si el bar agrega otra familia preparada,
        /// el lugar para arreglarlo es una columna nueva en Categoria, no un if más acá.
        /// </summary>
        public bool CategoriaAdmiteReceta => EsCategoriaPreparada(CategoriaSeleccionada);

        /// <summary>
        /// Lo que realmente decide si el bloque se ve. Incluye el caso del producto que
        /// ya tiene receta cargada aunque su categoría hoy diga otra cosa: esconderla
        /// sería hacer desaparecer datos existentes sin avisar.
        /// </summary>
        public bool MostrarReceta => CategoriaAdmiteReceta || Composicion.Count > 0;

        private static bool EsCategoriaPreparada(Categoria? categoria)
        {
            if (categoria is null)
                return false;

            return SinAcentos(categoria.NombreCategoria).Contains("coctel", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>"Cócteles" y "Cocteles" tienen que ser la misma palabra para esta regla.</summary>
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

            foreach (var linea in Composicion.Where(l => l.Cantidad <= 0))
                Agregar(nameof(Composicion),
                        $"La cantidad de \"{linea.Ingrediente.Nombre}\" debe ser mayor a cero.");
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

            EstablecerSeleccion(Productos.FirstOrDefault(p => p.IdProducto == idPrevio) ?? Productos.FirstOrDefault());
        }

        /// <summary>
        /// Llega acá cuando el resumen manda a reponer un producto de venta directa:
        /// se lo selecciona y se abre el formulario listo para corregir el stock.
        /// </summary>
        public override void Enfocar(object? foco)
        {
            if (foco is not AlertaStock alerta || alerta.EsInsumo)
                return;

            var producto = Productos.FirstOrDefault(p => p.IdProducto == alerta.Id);
            if (producto is null)
                return;

            EstablecerSeleccion(producto);
            Editar();
            Informar($"Actualizá el stock de \"{producto.Nombre}\" y guardá.");
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
            // En un alta la categoría arranca vacía y no en la primera de la lista:
            // elegirla es lo que decide si el producto lleva receta, así que tiene que
            // ser una decisión del usuario y no un default que pasa desapercibido.
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

            // Un formulario recién cargado arranca sin nada en rojo: las reglas se
            // calculan igual, pero no se muestran hasta que el usuario toque un campo
            // o apriete Guardar.
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
            // El botón está habilitado aunque falten datos: deshabilitado no explica QUÉ
            // falta. Se aprieta, se marcan los campos y se dice cuántos hay que corregir.
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

            // Se copian los campos del editor recién ahora, cuando el usuario confirmó.
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

        /// <summary>
        /// La cantidad de cada renglón se edita en su propio ViewModel, así que el
        /// formulario no se entera por su cuenta. Sin esto, una cantidad en cero no se
        /// marcaría hasta apretar Guardar.
        /// </summary>
        private LineaRecetaViewModel Vigilar(LineaRecetaViewModel linea)
        {
            linea.PropertyChanged += AlCambiarUnaLinea;
            return linea;
        }

        /// <summary>Vacía la receta soltando primero las suscripciones de cada renglón.</summary>
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
