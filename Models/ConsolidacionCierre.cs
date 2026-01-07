using System.Collections.Generic;

namespace InventarioFisico.Models
{
    public class ConsolidacionCierre
    {
        public List<int> OperacionIds { get; set; } = new();

        public List<int> OperacionesFinalizadas { get; set; } = new();
    }
}
