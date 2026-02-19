namespace InventarioFisico.Models
{
    public class OperacionConteoItem
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public int GrupoId { get; set; }
        public int NumeroConteo { get; set; }
        public string CodigoItem { get; set; }
        public string Prod { get; set; }
        public string Descripcion { get; set; }
        public string Udm { get; set; }
        public string Etiqueta { get; set; }
        public string Lote { get; set; }
        public decimal Costo { get; set; }
        public decimal CantidadSistema { get; set; }
        public int CantidadContada { get; set; }
        public string Ubicacion { get; set; }
        public string Bodega { get; set; }
        public string Cmpy { get; set; }
        public bool NoEncontrado { get; set; }
    }
}
