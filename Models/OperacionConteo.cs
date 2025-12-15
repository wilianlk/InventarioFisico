namespace InventarioFisico.Models
{
    public class OperacionConteo
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public int GrupoId { get; set; }
        public int NumeroConteo { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string NombreGrupo { get; set; }
    }
}
