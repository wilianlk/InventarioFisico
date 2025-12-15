using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class ConsolidacionService
    {
        private readonly ConsolidacionRepository _repo;

        public ConsolidacionService(ConsolidacionRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoAsync(int operacionId)
        {
            return await _repo.ObtenerConsolidadoAsync(operacionId);
        }

        public async Task AprobarReconteoAsync(Consolidado consolidado)
        {
            consolidado.FechaAprobacion = DateTime.Now;
            await _repo.AprobarReconteoAsync(consolidado);
        }

        public async Task<string> GenerarArchivoDI81Async(int operacionId)
        {
            var consolidado = await _repo.ObtenerConsolidadoAsync(operacionId);
            var sb = new StringBuilder();
            sb.AppendLine("CODIGO;LOTE;UBICACION;CANTIDAD_FINAL");
            foreach (var item in consolidado)
                sb.AppendLine($"{item.CodigoProducto};{item.Lote};{item.Ubicacion};{item.CantidadFinal}");
            return sb.ToString();
        }
    }
}
