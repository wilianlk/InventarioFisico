using System;

namespace InventarioFisico.Models
{
    public class Di81Archivo
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string CodigoProducto { get; set; }
        public string Lote { get; set; }
        public string Ubicacion { get; set; }
        public decimal CantidadFinal { get; set; }
        public string UsuarioGeneracion { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public string RutaArchivo { get; set; }

        public Di81Archivo()
        {
            FechaGeneracion = DateTime.Now;
        }
    }
}
