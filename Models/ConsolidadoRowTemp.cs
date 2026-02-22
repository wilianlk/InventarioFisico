using System.Collections.Generic;

namespace InventarioFisico.Models
{
    public class ConsolidadoRowTemp
    {
        public int OperacionId { get; set; }
        public string EstadoOperacion { get; set; } = string.Empty;
        public int NumeroConteo { get; set; }

        public HashSet<int> Bloques { get; set; } = new();

        public string CodigoItem { get; set; }
        public string Prod { get; set; }
        public string Descripcion { get; set; }
        public string Udm { get; set; }
        public string Etiqueta { get; set; }
        public string Lote { get; set; }
        public string Ubicacion { get; set; }
        public string Bodega { get; set; }
        public string Cmpy { get; set; }

        public decimal? Costo { get; set; }
        public decimal? CantidadSistema { get; set; }

        public decimal? Conteo1 { get; set; }
        public decimal? Conteo2 { get; set; }
        public decimal? Conteo3 { get; set; }

        public Dictionary<int, ConteoValorDto> ConteosPorNumero { get; set; } = new();

        public string GrupoConteo1 { get; set; }
        public string GrupoConteo2 { get; set; }
        public string GrupoConteo3 { get; set; }

        public decimal? CantidadFinal { get; set; }
        public bool NoEncontrado { get; set; }

        // Se deja por compatibilidad (no se usa para C1/C2/C3)
        public string NombreGrupo { get; set; }
    }
}
