using System;

namespace SistemaGestionBar.Models
{
    /// <summary>
    /// Tabla Reporte. Bitácora de informes generados (RF-14): qué se generó,
    /// quién lo pidió y dónde quedó el archivo, para auditoría posterior.
    /// </summary>
    public class Reporte : EntidadAuditable
    {
        public int IdReporte { get; set; }
        public TipoReporte TipoReporte { get; set; }
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public int IdUsuarioGenerador { get; set; }
        public string? RutaArchivo { get; set; }

        public Usuario UsuarioGenerador { get; set; } = null!;
    }
}
