using System;
using System.Globalization;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    public class AjusteDeStockViewModel : ViewModelBase, ICierrable
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

        public decimal Stock { get; }

        public decimal StockMinimo { get; }

        public string Tipo => EsInsumo ? "Insumo" : "Producto";

        public bool StockBajo => StockMinimo > 0 && Stock <= StockMinimo;

        public string TextoEstado => StockBajo
            ? "POR DEBAJO DEL MÍNIMO"
            : "STOCK EN NIVEL";

        private string _cantidadTexto = string.Empty;

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

        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                CantidadTexto = value == 0 ? string.Empty : value.ToString("0.##", CultureInfo.CurrentCulture);
            }
        }

        private string? _problemaDelTexto;

        public bool HayCantidad => _cantidad != 0;

        public decimal StockResultante => Stock + _cantidad;

        private decimal Techo => EsInsumo ? Limites.Cantidad : Limites.StockDeProducto;

        public string? Problema => _problemaDelTexto
            ?? (StockResultante < 0 ? $"No hay tanto: quedan {Stock:0.##} {UnidadMedida}." : null)
            ?? (StockResultante > Techo ? $"No entra tanto: el máximo es {Techo:N0}." : null);

        public bool EstaBien => Problema is null;

        public bool HayProblema => Problema is not null;

        public decimal Sugerido => StockBajo ? StockMinimo - Stock + 1 : 0;

        private string _mensaje = string.Empty;

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

        public bool SeAplico { get; private set; }

        public string MensajeDelGuardado { get; private set; } = string.Empty;

        public event EventHandler? CierreSolicitado;

        public ICommand SumarCommand { get; }
        public ICommand SugerirCommand { get; }
        public ICommand GuardarCommand { get; }

        private void Mover(decimal paso)
        {
            if (paso == 0)
                return;

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
