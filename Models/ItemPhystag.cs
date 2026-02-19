namespace InventarioFisico.Models
{
    public class ItemPhystag
    {
        public int? GrupoId { get; set; }
        public string? GrupoNombre { get; set; }

        public string Cmpy { get; set; } = "";
        public string Bodega { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string Item { get; set; } = "";
        public string Prod { get; set; } = "";
        public string Ubicacion { get; set; } = "";

        public string RackPasillo { get; set; } = "";
        public string Lado { get; set; } = "";
        public string Altura { get; set; } = "";
        public string Posicion { get; set; } = "";

        public string Lote { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Udm { get; set; } = "";
        public decimal Costo { get; set; }
        public decimal CantidadSistema { get; set; }
    }
}
