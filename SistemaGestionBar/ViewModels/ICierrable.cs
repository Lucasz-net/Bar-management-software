using System;

namespace SistemaGestionBar.ViewModels
{
    /// <summary>ViewModel de un diálogo modal que pide cerrar su ventana.</summary>
    public interface ICierrable
    {
        event EventHandler? CierreSolicitado;
    }
}
