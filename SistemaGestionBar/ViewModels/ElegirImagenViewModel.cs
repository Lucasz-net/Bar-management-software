using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SistemaGestionBar.Services;

namespace SistemaGestionBar.ViewModels
{
    public class ElegirImagenViewModel : ViewModelBase, ICierrable
    {
        public event EventHandler? CierreSolicitado;

        public ElegirImagenViewModel(string? rutaActual)
        {
            Opciones = CatalogoDeImagenes.Listar()
                .Select(i => new OpcionDeImagen(i.Nombre, i.Ruta, i.Ruta == rutaActual))
                .ToList();
            Elegida = rutaActual;

            ElegirCommand = new RelayCommand<OpcionDeImagen>(imagen =>
            {
                Elegida = imagen.Ruta;
                SeAplico = true;
                CierreSolicitado?.Invoke(this, EventArgs.Empty);
            });

            QuitarCommand = new RelayCommand(() =>
            {
                Elegida = null;
                SeAplico = true;
                CierreSolicitado?.Invoke(this, EventArgs.Empty);
            });

            CancelarCommand = new RelayCommand(() => CierreSolicitado?.Invoke(this, EventArgs.Empty));
        }

        public IReadOnlyList<OpcionDeImagen> Opciones { get; }

        /// <summary>Ruta actualmente asignada (para resaltarla) y, al cerrar, la elegida.</summary>
        public string? Elegida { get; private set; }

        public bool SeAplico { get; private set; }

        public ICommand ElegirCommand { get; }
        public ICommand QuitarCommand { get; }
        public ICommand CancelarCommand { get; }
    }

    public record OpcionDeImagen(string Nombre, string Ruta, bool EsActual);
}
