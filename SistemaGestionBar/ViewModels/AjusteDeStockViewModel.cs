using System;
using System.Globalization;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>
    /// La ventanita de ajuste de stock (RF-08). Modifica <b>el stock y nada más</b>.
    ///
    /// <b>Por qué no se usa el editor del ABM para esto.</b> El formulario de Productos pide
    /// nombre, categoría, precio, descripción, foto y receta, y valida la ficha entera: para
    /// anotar que llegaron 24 botellas es pedir de más y arriesgar de más —se puede tocar un
    /// precio sin querer, y una ficha con un dato viejo mal cargado impide guardar un
    /// movimiento de stock que no tiene nada que ver con eso—. Peor: el ABM pide el TOTAL
    /// NUEVO, así que había que calcular 6+24 de cabeza, que es donde se cometen los errores.
    ///
    /// Acá se carga <b>cuánto entra o cuánto sale</b> y la ventana muestra la cuenta hecha.
    /// Un número positivo es una entrada (llegó mercadería); uno negativo, una salida que no
    /// es una venta (rotura, merma, consumo interno).
    ///
    /// No abre ventanas ni las cierra: cuando el ajuste se guarda levanta
    /// <see cref="CierreSolicitado"/> y de cerrar se encarga <c>ServicioDialogo</c>, que es
    /// la única clase que conoce WPF.
    /// </summary>
    public class AjusteDeStockViewModel : ViewModelBase
    {
        private readonly IRepositorioBar _repositorio;

        private AjusteDeStockViewModel(IRepositorioBar repositorio, bool esInsumo, int id,
                                       string nombre, string unidad, decimal stock, decimal stockMinimo)
        {
            _repositorio = repositorio;

            EsInsumo = esInsumo;
            Id = id;
            Nombre = nombre;
            UnidadMedida = unidad;
            Stock = stock;
            StockMinimo = stockMinimo;

            SumarCommand = new RelayCommand<string>(paso => Mover(Leer(paso)));
            SugerirCommand = new RelayCommand(() => Cantidad = Sugerido, () => Sugerido > 0);
            GuardarCommand = new RelayCommand(Guardar, () => HayCantidad && EstaBien);

            // Si el ítem está bajo el mínimo la ventana se abre con la cantidad que le falta
            // ya propuesta: es lo que se viene a hacer el 90% de las veces, y el usuario la
            // confirma o la corrige en vez de tener que averiguarla.
            if (Sugerido > 0)
                Cantidad = Sugerido;
        }

        public static AjusteDeStockViewModel DeInsumo(IRepositorioBar repositorio, Ingrediente insumo) =>
            new(repositorio, true, insumo.IdIngrediente, insumo.Nombre, insumo.UnidadMedida,
                insumo.Stock, insumo.StockMinimo);

        public static AjusteDeStockViewModel DeProducto(IRepositorioBar repositorio, Producto producto) =>
            new(repositorio, false, producto.IdProducto, producto.Nombre, "unidades",
                producto.Stock, producto.StockMinimo);

        public bool EsInsumo { get; }
        public int Id { get; }
        public string Nombre { get; }
        public string UnidadMedida { get; }

        /// <summary>El stock guardado, tal como estaba al abrir la ventana.</summary>
        public decimal Stock { get; }

        public decimal StockMinimo { get; }

        public string Tipo => EsInsumo ? "Insumo" : "Producto";

        /// <summary>RF-08: mismo criterio que <c>Ingrediente.StockBajo</c> y <c>Producto.StockBajo</c>.</summary>
        public bool StockBajo => StockMinimo > 0 && Stock <= StockMinimo;

        public string TextoEstado => StockBajo
            ? "POR DEBAJO DEL MÍNIMO"
            : "STOCK EN NIVEL";

        // ---------------------------------------------------------------
        // Cuánto entra o cuánto sale
        // ---------------------------------------------------------------
        private string _cantidadTexto = string.Empty;

        /// <summary>
        /// Lo que hay tecleado en la casilla.
        ///
        /// <b>Por qué el campo es texto y no el decimal directo.</b> Un TextBox bindeado a un
        /// decimal con UpdateSourceTrigger=PropertyChanged se rompe mientras se escribe: la
        /// casilla vacía, un "-" solo o un "2," a medio tipear no convierten, y WPF deja el
        /// valor viejo en el ViewModel sin avisar. Guardando el texto crudo y convirtiéndolo
        /// acá, la ventana siempre sabe qué se escribió y puede decir que no es un número en
        /// vez de trabajar con un valor que ya no está en pantalla.
        /// </summary>
        public string CantidadTexto
        {
            get => _cantidadTexto;
            set
            {
                if (SetProperty(ref _cantidadTexto, value ?? string.Empty))
                    Releer();
            }
        }

        private decimal _cantidad;

        /// <summary>El movimiento con signo. Cero es "todavía no cargó nada".</summary>
        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                // Se pasa por el texto para que la casilla y el número nunca discrepen: los
                // botones +/- escriben acá y la casilla se actualiza sola.
                CantidadTexto = value == 0 ? string.Empty : value.ToString("0.##", CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Por qué lo tecleado no sirve, puesto por <see cref="Releer"/>. Se guarda el motivo
        /// en vez de recalcularlo: quien lo escribió es el único que sabe si falló la
        /// conversión o si el número era válido pero no para este ítem.
        /// </summary>
        private string? _problemaDelTexto;

        public bool HayCantidad => _cantidad != 0;

        /// <summary>Cómo queda el stock si se guarda. Es el punto de toda la ventana.</summary>
        public decimal StockResultante => Stock + _cantidad;

        public string? Problema => _problemaDelTexto
            ?? (StockResultante < 0 ? $"No hay tanto: quedan {Stock:0.##} {UnidadMedida}." : null);

        public bool EstaBien => Problema is null;

        public bool HayProblema => Problema is not null;

        /// <summary>
        /// Cuánto habría que sumar para salir del mínimo.
        ///
        /// Se suma <b>uno más</b> que la diferencia justa porque <c>StockBajo</c> es "menor o
        /// igual que el mínimo": quedarse exactamente en el mínimo seguiría mostrando la
        /// alerta, y el usuario volvería a esta misma ventana sin entender por qué.
        /// </summary>
        public decimal Sugerido => StockBajo ? StockMinimo - Stock + 1 : 0;

        // ---------------------------------------------------------------
        // Resultado
        // ---------------------------------------------------------------
        private string _mensaje = string.Empty;

        /// <summary>Lo que salió mal al guardar. Vacío mientras no haya pasado nada.</summary>
        public string Mensaje
        {
            get => _mensaje;
            private set
            {
                if (SetProperty(ref _mensaje, value))
                    OnPropertyChanged(nameof(HayMensaje));
            }
        }

        public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);

        /// <summary>True si el ajuste llegó a guardarse. Lo lee la pantalla que abrió la ventana.</summary>
        public bool SeAplico { get; private set; }

        /// <summary>Lo que el repositorio contestó al guardar, para que la sección lo muestre.</summary>
        public string MensajeDelGuardado { get; private set; } = string.Empty;

        /// <summary>Se guardó: hay que cerrar. Lo escucha <c>ServicioDialogo</c>.</summary>
        public event EventHandler? CierreSolicitado;

        public ICommand SumarCommand { get; }
        public ICommand SugerirCommand { get; }
        public ICommand GuardarCommand { get; }

        private void Mover(decimal paso)
        {
            if (paso == 0)
                return;

            // Nunca por debajo de cero: que el botón no pueda dejar la ventana en un estado
            // inválido es mejor que explicar después por qué no se puede guardar.
            Cantidad = Math.Max(_cantidad + paso, -Stock);
        }

        private static decimal Leer(string? paso) =>
            decimal.TryParse(paso, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor)
                ? valor
                : 0;

        private void Releer()
        {
            if (string.IsNullOrWhiteSpace(_cantidadTexto))
            {
                _cantidad = 0;
                _problemaDelTexto = null;
            }
            else if (decimal.TryParse(_cantidadTexto, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal valor))
            {
                // Un producto se cuenta por unidades: "2,5 botellas" no existe, y el
                // repositorio lo rechazaría igual. Mejor decirlo mientras se escribe.
                bool entero = valor == decimal.Truncate(valor);

                _cantidad = EsInsumo || entero ? valor : 0;
                _problemaDelTexto = EsInsumo || entero ? null : "Se cuenta por unidades enteras.";
            }
            else
            {
                _cantidad = 0;
                _problemaDelTexto = "Escribí un número.";
            }

            Mensaje = string.Empty;

            OnPropertyChanged(nameof(Cantidad));
            OnPropertyChanged(nameof(HayCantidad));
            OnPropertyChanged(nameof(StockResultante));
            OnPropertyChanged(nameof(Problema));
            OnPropertyChanged(nameof(HayProblema));
            OnPropertyChanged(nameof(EstaBien));

            CommandManager.InvalidateRequerySuggested();
        }

        private void Guardar()
        {
            // El repositorio valida de nuevo —es él quien hace cumplir la regla— pero acá se
            // corta antes para no mandar algo que ya se sabe que va a volver.
            if (!EstaBien)
            {
                Mensaje = Problema!;
                return;
            }

            var resultado = _repositorio.AjustarStock(EsInsumo, Id, _cantidad);

            if (!resultado.Exito)
            {
                Mensaje = resultado.Mensaje;
                return;
            }

            SeAplico = true;
            MensajeDelGuardado = resultado.Mensaje;
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
