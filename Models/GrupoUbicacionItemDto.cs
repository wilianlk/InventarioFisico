using System.ComponentModel.DataAnnotations;

namespace InventarioFisico.Models
{
    public class GrupoUbicacionItemDto
    {
        [Required] public string Ubicacion { get; set; } = "";
        public string Rack { get; set; } = "";
        public string Lado { get; set; } = "";
        public string Altura { get; set; } = "";
        public string Posicion { get; set; } = "";
    }
}
