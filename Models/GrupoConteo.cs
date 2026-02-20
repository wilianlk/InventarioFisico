using System;

namespace InventarioFisico.Models
{
    public class GrupoConteo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string FechaCreacion { get; set; }
        public int UsuarioCreacion { get; set; }
        public bool TieneConteoAbierto { get; set; }
        public int? OperacionIdConteoAbierto { get; set; }
    }
}
