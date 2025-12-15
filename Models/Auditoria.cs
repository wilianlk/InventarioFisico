using System;

namespace InventarioFisico.Models
{
    public class Auditoria
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public string Bodega { get; set; }
        public string Accion { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
        public string Hora { get; set; }
    }
}
