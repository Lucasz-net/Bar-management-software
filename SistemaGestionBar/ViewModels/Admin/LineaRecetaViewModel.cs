using SistemaGestionBar.Models;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>Un renglón editable de la composición de la receta (Producto_Ingrediente).</summary>
    public class LineaRecetaViewModel : ViewModelBase
    {
        public LineaRecetaViewModel(Ingrediente ingrediente, decimal cantidad)
        {
            _ingrediente = ingrediente;
            _cantidad = cantidad;
        }

        private Ingrediente _ingrediente;
        public Ingrediente Ingrediente
        {
            get => _ingrediente;
            set
            {
                if (SetProperty(ref _ingrediente, value))
                    OnPropertyChanged(nameof(UnidadMedida));
            }
        }

        private decimal _cantidad;
        public decimal Cantidad
        {
            get => _cantidad;
            set => SetProperty(ref _cantidad, value);
        }

        public string UnidadMedida => Ingrediente?.UnidadMedida ?? string.Empty;

        public ProductoIngrediente AFila(int idProducto) => new()
        {
            IdProducto = idProducto,
            IdIngrediente = Ingrediente.IdIngrediente,
            CantidadNecesaria = Cantidad,
            Ingrediente = Ingrediente
        };
    }
}
