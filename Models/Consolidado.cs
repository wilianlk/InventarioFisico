using System;

namespace InventarioFisico.Models
{
    public class Consolidado
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string CodigoProducto { get; set; }
        public string Descripcion { get; set; }
        public string Lote { get; set; }
        public string Ubicacion { get; set; }
        public decimal CantidadConteo1 { get; set; }
        public decimal CantidadConteo2 { get; set; }
        public decimal CantidadConteo3 { get; set; }
        public decimal CantidadFinal { get; set; }
        public string Estado { get; set; }
        public string UsuarioAprobacion { get; set; }
        public DateTime FechaAprobacion { get; set; }
    }
}
