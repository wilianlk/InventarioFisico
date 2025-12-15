using System;
using System.Collections.Generic;

namespace InventarioFisico.Models
{
    public class InventarioOperacion
    {
        public int Id { get; set; }
        public string Bodega { get; set; }
        public DateTime Fecha { get; set; }
        public string Observaciones { get; set; }
        public string Estado { get; set; }
        public string UsuarioCreacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int NumeroConteo { get; set; }
        public List<int> GruposIds { get; set; }

        public InventarioOperacion()
        {
            Estado = "EN_PREPARACION";
            FechaCreacion = DateTime.Now;
        }
    }
}
