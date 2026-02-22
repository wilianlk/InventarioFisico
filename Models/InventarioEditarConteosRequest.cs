using System.Collections.Generic;

namespace InventarioFisico.Models
{
    public class InventarioEditarConteosRequest
    {
        public int NumeroConteo { get; set; }
        public List<int> GruposIds { get; set; } = new();
    }
}
