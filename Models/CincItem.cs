using System;

namespace InventarioFisico.Models
{
    public class CincItem
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string CodigoProducto { get; set; }
        public string Descripcion { get; set; }
        public string Lote { get; set; }
        public string Ubicacion { get; set; }
        public string Motivo { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; }

        public CincItem()
        {
            FechaRegistro = DateTime.Now;
            Estado = "Pendiente";
        }
    }
}
