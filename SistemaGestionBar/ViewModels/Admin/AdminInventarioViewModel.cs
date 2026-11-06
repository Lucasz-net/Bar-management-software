using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminInventarioViewModel : SeccionAdminViewModel
    {
        public AdminInventarioViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Inventario, "Inventario", "PackageVariantClosed",
                   "Insumos que consumen las recetas, con su stock y punto de reposición")
        {
            Ingredientes = new ObservableCollection<Ingrediente>();

            NuevoCommand = new RelayCommand(Nuevo);
            EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null && !EnEdicion);
            GuardarCommand = new RelayCommand(Guardar, () => EnEdicion);
            CancelarCommand = new RelayCommand(Cancelar, () => EnEdicion);
            EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null && !EnEdicion);
            AjustarStockCommand = new RelayCommand(AjustarStock, () => Seleccionado is not null && !EnEdicion);

            Recargar();
        }

        public ObservableCollection<Ingrediente> Ingredientes { get; }

        public IReadOnlyList<OpcionFiltro<EstadoStock>> OpcionesStock { get; } = new[]
        {
            new OpcionFiltro<EstadoStock>("Todo el inventario", EstadoStock.Todos),
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

        public override bool HayFiltroAplicado =>
            HayBusqueda || (EstadoStockFiltro is not null && EstadoStockFiltro.Valor != EstadoStock.Todos);

        protected override bool Coincide(object item)
        {
            if (item is not Ingrediente ingrediente)
                return false;

            var estado = EstadoStockFiltro?.Valor ?? EstadoStock.Todos;
            bool pasaEstado = estado switch
            {
                EstadoStock.AReponer => ingrediente.StockBajo,
                EstadoStock.Suficiente => !ingrediente.StockBajo,
                _ => true
            };

            if (!pasaEstado)
                return false;

            if (!HayBusqueda)
                return true;

            return Contiene(ingrediente.Nombre, Termino)
                || Contiene(ingrediente.UnidadMedida, Termino);
        }

        protected override void LimpiarFiltros()
        {
            base.LimpiarFiltros();
            EstadoStockFiltro = OpcionesStock[0];
        }

        private Ingrediente? _seleccionado;

        public Ingrediente? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (EnEdicion)
                {
                    if (ReferenceEquals(_seleccionado, value))
                        return;

                    if (value is not null)
                        Fallar("Terminá la edición —guardá o cancelá— antes de elegir otro insumo.");

                    OnPropertyChanged(nameof(Seleccionado));
                    return;
                }

                EstablecerSeleccion(value);
            }
        }

        private void EstablecerSeleccion(Ingrediente? ingrediente)
        {
            if (!SetProperty(ref _seleccionado, ingrediente, nameof(Seleccionado)))
                return;

            OnPropertyChanged(nameof(HaySeleccion));

            if (!EnEdicion)
                CargarEnEditor(ingrediente);
        }

        public bool HaySeleccion => Seleccionado is not null;

        public decimal FaltanteParaElMinimo =>
            Seleccionado is null ? 0 : Math.Max(0, Seleccionado.StockMinimo - Seleccionado.Stock);

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
            ? "Detalle del insumo"
            : _esAlta ? "Nuevo insumo" : $"Editando: {_nombre}";

        private string _nombre = string.Empty;
        public string Nombre { get => _nombre; set => SetCampo(ref _nombre, value); }

        private string _unidadMedida = string.Empty;
        public string UnidadMedida { get => _unidadMedida; set => SetCampo(ref _unidadMedida, value); }

        private decimal _stock;
        public decimal Stock { get => _stock; set => SetCampo(ref _stock, value); }

        private decimal _stockMinimo;
        public decimal StockMinimo { get => _stockMinimo; set => SetCampo(ref _stockMinimo, value); }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand AjustarStockCommand { get; }

        protected override void DeclararReglas()
        {
            Requerido(Nombre, nameof(Nombre), "El nombre del insumo");
            LargoMaximo(Nombre, nameof(Nombre), "El nombre", Limites.NombreInsumo);
            NoRepetido(Nombre, nameof(Nombre),
                       Ingredientes.Where(i => _esAlta || i.IdIngrediente != Seleccionado?.IdIngrediente)
                                   .Select(i => i.Nombre),
                       "Ya existe un insumo con ese nombre.");

            Requerido(UnidadMedida, nameof(UnidadMedida), "La unidad de medida");
            LargoMaximo(UnidadMedida, nameof(UnidadMedida), "La unidad", Limites.UnidadMedida);

            NoNegativo(Stock, nameof(Stock), "El stock");
            NoSupera(Stock, nameof(Stock), "El stock", Limites.Cantidad, "N3");
            NoNegativo(StockMinimo, nameof(StockMinimo), "El stock mínimo");
            NoSupera(StockMinimo, nameof(StockMinimo), "El stock mínimo", Limites.Cantidad, "N3");
        }

        public sealed override void Recargar()
        {
            int? idPrevio = Seleccionado?.IdIngrediente;

            Ingredientes.Clear();
            foreach (var ingrediente in Repositorio.ObtenerIngredientes())
                Ingredientes.Add(ingrediente);

            ConfigurarFiltro(Ingredientes);
            _estadoStock ??= OpcionesStock[0];
            RefrescarVista();

            EstablecerSeleccion(Ingredientes.FirstOrDefault(i => i.IdIngrediente == idPrevio)
                                ?? Ingredientes.FirstOrDefault());
        }

        public override void Enfocar(object? foco)
        {
            if (foco is not AlertaStock alerta || !alerta.EsInsumo)
                return;

            var ingrediente = Ingredientes.FirstOrDefault(i => i.IdIngrediente == alerta.Id);
            if (ingrediente is null)
                return;

            EstablecerSeleccion(ingrediente);
            AjustarStock();
        }

        private void AjustarStock()
        {
            if (Seleccionado is null)
                return;

            LimpiarMensaje();

            var ajuste = AjusteDeStockViewModel.DeInsumo(Repositorio, Seleccionado);

            if (!Dialogo.AjustarStock(ajuste))
                return;

            Recargar();
            Informar(ajuste.MensajeDelGuardado);
        }

        private void CargarEnEditor(Ingrediente? ingrediente)
        {
            _nombre = ingrediente?.Nombre ?? string.Empty;
            _unidadMedida = ingrediente?.UnidadMedida ?? string.Empty;
            _stock = ingrediente?.Stock ?? 0;
            _stockMinimo = ingrediente?.StockMinimo ?? 0;

            OnPropertyChanged(nameof(Nombre));
            OnPropertyChanged(nameof(UnidadMedida));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(StockMinimo));
            OnPropertyChanged(nameof(TituloEditor));
            OnPropertyChanged(nameof(FaltanteParaElMinimo));

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

            var ingrediente = _esAlta ? new Ingrediente() : Seleccionado;
            if (ingrediente is null)
            {
                Fallar("No hay un insumo seleccionado.");
                return;
            }

            ingrediente.Nombre = Nombre.Trim();
            ingrediente.UnidadMedida = UnidadMedida.Trim();
            ingrediente.Stock = Stock;
            ingrediente.StockMinimo = StockMinimo;

            if (!Aplicar(Repositorio.GuardarIngrediente(ingrediente)))
                return;

            EnEdicion = false;
            _esAlta = false;
            Recargar();
            EstablecerSeleccion(Ingredientes.FirstOrDefault(i => i.IdIngrediente == ingrediente.IdIngrediente));
        }

        private void Eliminar()
        {
            if (Seleccionado is null)
                return;

            if (!ConfirmarEliminacion("el insumo", Seleccionado.Nombre))
                return;

            if (Aplicar(Repositorio.EliminarIngrediente(Seleccionado.IdIngrediente)))
                Recargar();
        }
    }
}
