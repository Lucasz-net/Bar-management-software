using System;
using SistemaGestionBar.ViewModels;

namespace SistemaGestionBar.Services
{
    public interface IServicioDialogo
    {
        void MostrarReceta(RecetaViewModel receta);

        void MostrarFactura(FacturaViewModel factura);

        bool AjustarStock(AjusteDeStockViewModel ajuste);

        bool PedirAutorizacionSupervisor(AutorizacionViewModel autorizacion);

        bool PedirConfirmacionDeContrasena(ConfirmarContrasenaViewModel confirmacion);

        bool PedirAutorizacionDoble(AutorizacionDobleViewModel autorizacion);

        /// <summary>Devuelve el motivo escrito, o null si se canceló.</summary>
        string? PedirMotivoDeBackup();

        /// <summary>Muestra un calendario para elegir dos fechas. Null si se canceló.</summary>
        (DateTime Desde, DateTime Hasta)? ElegirRangoDeFechas(DateTime? desde, DateTime? hasta);

        /// <summary>Muestra las imágenes disponibles. Devuelve (true, ruta o null para quitar) o (false, null) si se canceló.</summary>
        (bool SeAplico, string? Ruta) ElegirImagenDeProducto(string? rutaActual);

        void Informar(string titulo, string mensaje);

        bool Confirmar(string titulo, string mensaje);

        string? ElegirDondeGuardarPdf(string nombreSugerido);

        string? ElegirArchivoSql(string directorioInicial);

        void AbrirArchivo(string ruta);
    }
}