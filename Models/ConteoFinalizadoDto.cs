using System.Collections.Generic;
using System;

namespace InventarioFisico.Models
{
    public class ConteosFinalizadosCabeceraDto
    {
        public int OperacionId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool Finalizada { get; set; }
        public string Bodega { get; set; } = string.Empty;
        public DateTime? FechaOperacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime? FechaCreacion { get; set; }
        public int NumeroConteo { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public int TotalRegistros { get; set; }
        public int TotalNoEncontrados { get; set; }
        public int TotalReferencias { get; set; }
        public List<int> Conteos { get; set; } = new();
        public int TotalConteos { get; set; }
    }

    public class ConteosFinalizadosOperacionDto
    {
        public ConteosFinalizadosCabeceraDto Cabecera { get; set; } = new();
        public List<ConteoFinalizadoDto> Items { get; set; } = new();
    }

    public class ConteosFinalizadosResponseDto
    {
        public List<ConteosFinalizadosOperacionDto> Operaciones { get; set; } = new();
    }

    public class ConteoFinalizadoIdDto
    {
        public List<int> Bloques { get; set; } = new();
    }

    public class ConteoFinalizadoDto
    {
        public int OperacionId { get; set; }
        public string EstadoOperacion { get; set; } = string.Empty;
        public ConteoFinalizadoIdDto Id { get; set; } = new();
        public int NumeroConteo { get; set; }
        public string CodigoItem { get; set; } = string.Empty;
        public string Prod { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Udm { get; set; } = string.Empty;
        public string Etiqueta { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Bodega { get; set; } = string.Empty;
        public string Cmpy { get; set; } = string.Empty;
        public decimal? Costo { get; set; }
        public decimal? CantidadSistema { get; set; }
        public decimal? CantidadConteo1 { get; set; }
        public decimal? CantidadConteo2 { get; set; }
        public decimal? CantidadConteo3 { get; set; }
        public decimal? CantidadFinal { get; set; }
        public bool NoEncontrado { get; set; }
        public string Grupo { get; set; } = string.Empty;
        public List<ConteoValorDto> Conteos { get; set; } = new();
    }

    public class ConteoValorDto
    {
        public int NumeroConteo { get; set; }
        public decimal? Cantidad { get; set; }
        public string Grupo { get; set; } = string.Empty;
    }
}
