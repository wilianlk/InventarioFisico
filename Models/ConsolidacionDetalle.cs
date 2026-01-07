namespace InventarioFisico.Models
{
    public class ConsolidacionDetalle
    {
        public int Id { get; set; }
        public string CodigoItem { get; set; }
        public string Prod { get; set; }
        public string Udm { get; set; }
        public string Etiqueta { get; set; }
        public string Lote { get; set; }
        public string Ubicacion { get; set; }
        public string Bodega { get; set; }
        public string Cmpy { get; set; }
        public decimal Costo { get; set; }
        public decimal CantidadSistema { get; set; }
        public decimal? CantidadConteo1 { get; set; }
        public decimal? CantidadConteo2 { get; set; }
        public decimal? CantidadConteo3 { get; set; }
        public decimal? CantidadFinal { get; set; }
    }
}
