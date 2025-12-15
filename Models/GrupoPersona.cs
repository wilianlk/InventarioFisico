namespace InventarioFisico.Models
{
    public class GrupoPersona
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; }
    }
}
