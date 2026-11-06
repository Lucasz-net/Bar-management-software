using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public abstract class SeccionAdminViewModel : ViewModelValidable
    {
        protected SeccionAdminViewModel(
            IRepositorioBar repositorio,
            IServicioDialogo dialogo,
            DestinoAdmin destino,
            string titulo,
            string icono,
            string subtitulo)
        {
            Repositorio = repositorio;
            Dialogo = dialogo;
            Destino = destino;
            Titulo = titulo;
            Icono = icono;
            Subtitulo = subtitulo;

            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros, () => HayFiltroAplicado);
        }

        protected IRepositorioBar Repositorio { get; }
        protected IServicioDialogo Dialogo { get; }

        public DestinoAdmin Destino { get; }

        public string Titulo { get; }

        public string Icono { get; }

        public string Subtitulo { get; }

        private bool _estaSeleccionada;

        public bool EstaSeleccionada
        {
            get => _estaSeleccionada;
            set => SetProperty(ref _estaSeleccionada, value);
        }

        private ICollectionView? _vista;

        public ICollectionView? Vista => _vista;

        private string _busqueda = string.Empty;

        public string Busqueda
        {
            get => _busqueda;
            set
            {
                if (SetProperty(ref _busqueda, value))
                    RefrescarVista();
            }
        }

        protected string Termino => Busqueda.Trim();

        public bool HayBusqueda => !string.IsNullOrWhiteSpace(Busqueda);

        public virtual bool HayFiltroAplicado => HayBusqueda;

        private int _cantidadVisible;

        public int CantidadVisible
        {
            get => _cantidadVisible;
            private set => SetProperty(ref _cantidadVisible, value);
        }

        private int _cantidadTotal;
        public int CantidadTotal
        {
            get => _cantidadTotal;
            private set => SetProperty(ref _cantidadTotal, value);
        }

        public ICommand LimpiarFiltrosCommand { get; }

        protected void ConfigurarFiltro(IEnumerable coleccion)
        {
            if (_vista is not null)
                return;

            _vista = CollectionViewSource.GetDefaultView(coleccion);
            _vista.Filter = item => Coincide(item);
            OnPropertyChanged(nameof(Vista));
        }

        protected void RefrescarVista()
        {
            _vista?.Refresh();
            ActualizarContadores();
            RefrescarIndicadores();

            OnPropertyChanged(nameof(HayBusqueda));
            OnPropertyChanged(nameof(HayFiltroAplicado));
            CommandManager.InvalidateRequerySuggested();
        }

        protected virtual void RefrescarIndicadores() { }

        private void ActualizarContadores()
        {
            if (_vista is null)
                return;

            CantidadVisible = _vista.Cast<object>().Count();
            CantidadTotal = _vista.SourceCollection?.Cast<object>().Count() ?? 0;
        }

        protected virtual bool Coincide(object item) => true;

        protected virtual void LimpiarFiltros()
        {
            Busqueda = string.Empty;
        }

        protected static bool Contiene(string? texto, string termino) =>
            !string.IsNullOrEmpty(texto) &&
            texto.Contains(termino, StringComparison.OrdinalIgnoreCase);

        protected static bool ContieneDocumento(string? documento, string termino)
        {
            string numeros = Models.Documento.Normalizar(termino);

            return (numeros.Length > 0 && Contiene(Models.Documento.Normalizar(documento), numeros))
                || Contiene(Models.Documento.Formatear(documento), termino);
        }

        protected bool Aplicar(ResultadoOperacion resultado)
        {
            if (resultado.Exito)
                Informar(resultado.Mensaje);
            else
                Fallar(resultado.Mensaje);

            return resultado.Exito;
        }

        public abstract void Recargar();

        public event EventHandler<PedidoDeNavegacion>? NavegacionSolicitada;

        protected void IrA(DestinoAdmin destino, object? foco = null) =>
            NavegacionSolicitada?.Invoke(this, new PedidoDeNavegacion(destino, foco));

        private IReadOnlyCollection<DestinoAdmin> _destinos = Array.Empty<DestinoAdmin>();

        public void ConfigurarDestinos(IReadOnlyCollection<DestinoAdmin> destinos)
        {
            _destinos = destinos;
            AlCambiarLosDestinos();
        }

        protected bool PuedeIrA(DestinoAdmin destino) => _destinos.Contains(destino);

        protected void IrAReponer(AlertaStock alerta)
        {
            if (alerta is null)
                return;

            IrA(alerta.EsInsumo ? DestinoAdmin.Inventario : DestinoAdmin.Productos, alerta);
        }

        protected virtual void AlCambiarLosDestinos() { }

        public virtual void Enfocar(object? foco) { }

        protected bool ConfirmarEliminacion(string queEs, string nombre, string? detalle = null)
        {
            string cuerpo = $"¿Estás seguro de que querés eliminar {queEs} \"{nombre}\"?";

            if (!string.IsNullOrWhiteSpace(detalle))
                cuerpo += $"\n\n{detalle}";

            return Dialogo.Confirmar($"Eliminar {queEs}", cuerpo + "\n\nEsta acción no se puede deshacer.");
        }
    }

    public enum DestinoAdmin
    {
        Resumen,

        Gestion,

        Productos,
        Inventario,

        Personas,
        Usuarios,

        Equipo,

        Ventas,
        Reportes,
        Parametros,

        Backups
    }

    public record PedidoDeNavegacion(DestinoAdmin Destino, object? Foco);
}