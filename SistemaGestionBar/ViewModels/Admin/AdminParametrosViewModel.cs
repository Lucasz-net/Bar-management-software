using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    /// <summary>
    /// ABM de las tablas paramétricas que pide RF-12: categorías, métodos de pago
    /// y ubicaciones. Son listas cortas y de estructura simple, así que van juntas
    /// en una sola pantalla en vez de tres secciones casi vacías.
    /// </summary>
    public class AdminParametrosViewModel : SeccionAdminViewModel
    {
        public AdminParametrosViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, "Parámetros", "TuneVariant",
                   "Categorías, métodos de pago y ubicaciones del local")
        {
            Categorias = new ObservableCollection<Categoria>();
            MetodosPago = new ObservableCollection<MetodoPago>();
            Ubicaciones = new ObservableCollection<Ubicacion>();

            AgregarCategoriaCommand = new RelayCommand(AgregarCategoria);
            GuardarCategoriaCommand = new RelayCommand<Categoria>(c => { if (Aplicar(Repositorio.GuardarCategoria(c))) Recargar(); });
            EliminarCategoriaCommand = new RelayCommand<Categoria>(EliminarCategoria);

            AgregarMetodoCommand = new RelayCommand(AgregarMetodo);
            GuardarMetodoCommand = new RelayCommand<MetodoPago>(m => { if (Aplicar(Repositorio.GuardarMetodoPago(m))) Recargar(); });
            EliminarMetodoCommand = new RelayCommand<MetodoPago>(EliminarMetodo);

            AgregarUbicacionCommand = new RelayCommand(AgregarUbicacion);
            GuardarUbicacionCommand = new RelayCommand<Ubicacion>(u => { if (Aplicar(Repositorio.GuardarUbicacion(u))) Recargar(); });
            EliminarUbicacionCommand = new RelayCommand<Ubicacion>(EliminarUbicacion);

            Recargar();
        }

        public ObservableCollection<Categoria> Categorias { get; }
        public ObservableCollection<MetodoPago> MetodosPago { get; }
        public ObservableCollection<Ubicacion> Ubicaciones { get; }
        /// <summary>
        /// Estáticas a propósito: un DataGridComboBoxColumn no forma parte del árbol
        /// visual, así que no hereda el DataContext y no puede bindear a una propiedad
        /// de instancia. La fuente estática es la forma estándar de alimentarlo.
        /// </summary>
        public static IReadOnlyList<TipoUbicacion> TodosLosTipos { get; } = Enum.GetValues<TipoUbicacion>();
        public static IReadOnlyList<EstadoUbicacion> TodosLosEstados { get; } = Enum.GetValues<EstadoUbicacion>();

        public ICommand AgregarCategoriaCommand { get; }
        public ICommand GuardarCategoriaCommand { get; }
        public ICommand EliminarCategoriaCommand { get; }

        public ICommand AgregarMetodoCommand { get; }
        public ICommand GuardarMetodoCommand { get; }
        public ICommand EliminarMetodoCommand { get; }

        public ICommand AgregarUbicacionCommand { get; }
        public ICommand GuardarUbicacionCommand { get; }
        public ICommand EliminarUbicacionCommand { get; }

        public sealed override void Recargar()
        {
            Categorias.Clear();
            foreach (var c in Repositorio.ObtenerCategorias())
                Categorias.Add(c);

            MetodosPago.Clear();
            foreach (var m in Repositorio.ObtenerMetodosPago())
                MetodosPago.Add(m);

            Ubicaciones.Clear();
            foreach (var u in Repositorio.ObtenerTodasLasUbicaciones())
                Ubicaciones.Add(u);
        }

        // Las altas se agregan a la lista en memoria con Id 0; el repositorio
        // les asigna número recién cuando se guardan.
        private void AgregarCategoria()
        {
            LimpiarMensaje();
            Categorias.Add(new Categoria { NombreCategoria = "Nueva categoría" });
        }

        private void AgregarMetodo()
        {
            LimpiarMensaje();
            MetodosPago.Add(new MetodoPago { NombreMetodo = "Nuevo método" });
        }

        private void AgregarUbicacion()
        {
            LimpiarMensaje();
            Ubicaciones.Add(new Ubicacion
            {
                NombreUbicacion = $"Mesa {Ubicaciones.Count(u => u.Tipo == TipoUbicacion.Mesa) + 1}",
                Capacidad = 4,
                Tipo = TipoUbicacion.Mesa,
                Estado = EstadoUbicacion.Libre
            });
        }

        private void EliminarCategoria(Categoria categoria)
        {
            if (categoria.IdCategoria == 0) { Categorias.Remove(categoria); return; }

            if (!Dialogo.Confirmar("Eliminar categoría", $"¿Eliminar \"{categoria.NombreCategoria}\"?"))
                return;

            if (Aplicar(Repositorio.EliminarCategoria(categoria.IdCategoria)))
                Recargar();
        }

        private void EliminarMetodo(MetodoPago metodo)
        {
            if (metodo.IdMetodoPago == 0) { MetodosPago.Remove(metodo); return; }

            if (!Dialogo.Confirmar("Eliminar método de pago", $"¿Eliminar \"{metodo.NombreMetodo}\"?"))
                return;

            if (Aplicar(Repositorio.EliminarMetodoPago(metodo.IdMetodoPago)))
                Recargar();
        }

        private void EliminarUbicacion(Ubicacion ubicacion)
        {
            if (ubicacion.IdUbicacion == 0) { Ubicaciones.Remove(ubicacion); return; }

            if (!Dialogo.Confirmar("Eliminar ubicación", $"¿Eliminar \"{ubicacion.NombreUbicacion}\"?"))
                return;

            if (Aplicar(Repositorio.EliminarUbicacion(ubicacion.IdUbicacion)))
                Recargar();
        }
    }
}
