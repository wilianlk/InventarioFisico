namespace InventarioFisico.Models
{
    public class GrupoUbicacion
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public string? GrupoNombre { get; set; }

        public string Bodega { get; set; } = "";
        public string? Ubicaciones { get; set; }
        public string Rack { get; set; } = "";
        public string Lado { get; set; } = "";
        public string Altura { get; set; } = "";
        public string Ubicacion { get; set; } = "";
    }
}
