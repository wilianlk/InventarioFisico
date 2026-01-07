using System;
using System.Collections.Generic;
using System.Globalization;
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

        public async Task<List<dynamic>> ObtenerConteosFinalizadosAsync()
        {
            return await _repo.ObtenerConteosFinalizadosAsync();
        }

        public async Task<ConsolidacionCierre> FinalizarOperacionesAsync(List<int> operacionIds)
        {
            foreach (var operacionId in operacionIds)
            {
                var puede = await _repo.ConteosCerradosAsync(operacionId);
                if (!puede)
                    throw new InvalidOperationException($"La operación {operacionId} tiene conteos abiertos.");

                await _repo.BloquearOperacionAsync(operacionId);
            }

            return new ConsolidacionCierre
            {
                OperacionIds = operacionIds,
                OperacionesFinalizadas = new List<int>(operacionIds)
            };
        }

        public async Task<string> GenerarArchivoDI81Async(int operacionId)
        {
            var finalizada = await _repo.ConsolidacionFinalizadaAsync(operacionId);
            if (!finalizada)
                throw new InvalidOperationException("La operación no está FINALIZADA.");

            var data = await _repo.ObtenerConsolidadoParaDI81Async(operacionId);
            if (data.Count == 0)
                throw new InvalidOperationException("No hay datos consolidados.");

            var sb = new StringBuilder();

            foreach (var i in data)
            {
                if (i.CantidadFinal == null)
                    throw new InvalidOperationException("Existen registros sin cantidad final.");

                sb.AppendLine(string.Join(",",
                    i.CodigoProducto,
                    i.Ubicacion,
                    i.Lote,
                    i.CantidadFinal.Value.ToString(CultureInfo.InvariantCulture)
                ));
            }

            return sb.ToString();
        }

        public async Task<bool> ConsolidacionFinalizadaAsync(int operacionId)
        {
            return await _repo.ConsolidacionFinalizadaAsync(operacionId);
        }
    }
}
