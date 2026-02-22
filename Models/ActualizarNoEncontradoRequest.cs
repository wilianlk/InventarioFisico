namespace InventarioFisico.Models
{
    public class ActualizarNoEncontradoRequest
    {
        public required int ConteoId { get; set; }
        public required string CodigoItem { get; set; }
        public required bool NoEncontrado { get; set; }
    }
}
