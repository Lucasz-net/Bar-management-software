using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// ABM de insumos y control de stock (RF-12 y RF-08).
    /// Es la contracara del descuento automático del punto de venta: acá se repone.
    /// </summary>
    public class AdminInventarioViewModel : SeccionAdminViewModel
    {
        public AdminInventarioViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, "Inventario", "PackageVariantClosed",
                   "Insumos que consumen las recetas, con su stock y punto de reposición")
        {
            Ingredientes = new ObservableCollection<Ingrediente>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null && !EnEdicion);

            Recargar();
        }

        public ObservableCollection<Ingrediente> Ingredientes { get; }

        private Ingrediente? _seleccionado;
        public Ingrediente? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (SetProperty(ref _seleccionado, value) && !EnEdicion)
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
            ? "Detalle del insumo"
            : _esAlta ? "Nuevo insumo" : $"Editando: {_nombre}";

        private string _nombre = string.Empty;
        public string Nombre { get => _nombre; set => SetProperty(ref _nombre, value); }

        private string _unidadMedida = string.Empty;
        public string UnidadMedida { get => _unidadMedida; set => SetProperty(ref _unidadMedida, value); }

        private decimal _stock;
        public decimal Stock { get => _stock; set => SetProperty(ref _stock, value); }

        private decimal _stockMinimo;
        public decimal StockMinimo { get => _stockMinimo; set => SetProperty(ref _stockMinimo, value); }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdIngrediente;

            Ingredientes.Clear();
            foreach (var i in Repositorio.ObtenerIngredientes())
                Ingredientes.Add(i);

            Seleccionado = Ingredientes.FirstOrDefault(i => i.IdIngrediente == idPrevio) ?? Ingredientes.FirstOrDefault();
        }

        private void CargarEnEditor(Ingrediente? ingrediente)
        {
            Nombre = ingrediente?.Nombre ?? string.Empty;
            UnidadMedida = ingrediente?.UnidadMedida ?? string.Empty;
            Stock = ingrediente?.Stock ?? 0;
            StockMinimo = ingrediente?.StockMinimo ?? 0;
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
            var ingrediente = _esAlta ? new Ingrediente() : Seleccionado;
            if (ingrediente is null)
            {
                Fallar("No hay un insumo seleccionado.");
                return;
            }

            ingrediente.Nombre = Nombre;
            ingrediente.UnidadMedida = UnidadMedida;
            ingrediente.Stock = Stock;
            ingrediente.StockMinimo = StockMinimo;

            if (!Aplicar(Repositorio.GuardarIngrediente(ingrediente)))
                return;

            EnEdicion = false;
            _esAlta = false;
            Recargar();
            Seleccionado = Ingredientes.FirstOrDefault(i => i.IdIngrediente == ingrediente.IdIngrediente);
        }

        private void Eliminar()
        {
            if (Seleccionado is null)
                return;

            if (!Dialogo.Confirmar("Eliminar insumo", $"¿Eliminar \"{Seleccionado.Nombre}\" del inventario?"))
                return;

            if (Aplicar(Repositorio.EliminarIngrediente(Seleccionado.IdIngrediente)))
                Recargar();
        }
    }
}
