using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventarioFisico.Models
{
    public class GrupoUbicacionAgregarDto
    {
        [Required] public int GrupoId { get; set; }
        [Required] public string Bodega { get; set; } = "";
        [Required] public List<GrupoUbicacionItemDto> Ubicaciones { get; set; } = new();
    }
}
