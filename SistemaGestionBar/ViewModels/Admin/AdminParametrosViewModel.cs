using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Models;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels.Admin
{
    public class AdminParametrosViewModel : SeccionAdminViewModel
    {
        public AdminParametrosViewModel(IRepositorioBar repositorio, IServicioDialogo dialogo)
            : base(repositorio, dialogo, DestinoAdmin.Parametros, "Parámetros", "TuneVariant",
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
        public static IReadOnlyList<TipoUbicacion> TodosLosTipos { get; } = Enum.GetValues<TipoUbicacion>();

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
            foreach (var u in Repositorio.ObtenerUbicaciones())
                Ubicaciones.Add(u);
        }

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
                Tipo = TipoUbicacion.Mesa
            });
        }

        private void EliminarCategoria(Categoria categoria)
        {
            if (categoria.IdCategoria == 0) { Categorias.Remove(categoria); return; }

            if (!ConfirmarEliminacion("la categoría", categoria.NombreCategoria))
                return;

            if (Aplicar(Repositorio.EliminarCategoria(categoria.IdCategoria)))
                Recargar();
        }

        private void EliminarMetodo(MetodoPago metodo)
        {
            if (metodo.IdMetodoPago == 0) { MetodosPago.Remove(metodo); return; }

            if (!ConfirmarEliminacion("el método de pago", metodo.NombreMetodo))
                return;

            if (Aplicar(Repositorio.EliminarMetodoPago(metodo.IdMetodoPago)))
                Recargar();
        }

        private void EliminarUbicacion(Ubicacion ubicacion)
        {
            if (ubicacion.IdUbicacion == 0) { Ubicaciones.Remove(ubicacion); return; }

            if (!ConfirmarEliminacion("la ubicación", ubicacion.NombreUbicacion))
                return;

            if (Aplicar(Repositorio.EliminarUbicacion(ubicacion.IdUbicacion)))
                Recargar();
        }
    }
}
