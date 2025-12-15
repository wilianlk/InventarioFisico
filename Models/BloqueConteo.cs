namespace InventarioFisico.Models
{
    public class BloqueConteo
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public int? GrupoId { get; set; }
        public string UbicacionInicio { get; set; }
        public string UbicacionFin { get; set; }
        public string Pasillo { get; set; }
        public string Lado { get; set; }
        public int AlturaInicio { get; set; }
        public int AlturaFin { get; set; }
        public int PosicionInicio { get; set; }
        public int PosicionFin { get; set; }
    }
}
