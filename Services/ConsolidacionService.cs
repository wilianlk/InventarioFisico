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

        public Task<List<dynamic>> ObtenerConteosFinalizadosAsync()
        {
            return _repo.ObtenerConteosFinalizadosAsync();
        }

        public async Task<string> CerrarConsolidacionAsync(int operacionId)
        {
            var yaFinalizada = await _repo.ConsolidacionFinalizadaAsync(operacionId);
            if (yaFinalizada)
                return "La operación ya se encuentra en estado FINALIZADA";

            var cerrados = await _repo.ConteosCerradosAsync(operacionId);
            if (!cerrados)
                throw new InvalidOperationException("Existen conteos abiertos.");

            await _repo.CalcularCantidadFinalAsync(operacionId);
            await _repo.MarcarConsolidacionFinalizadaAsync(operacionId);

            return "Operación finalizada correctamente";
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

        public Task<bool> ConsolidacionFinalizadaAsync(int operacionId)
        {
            return _repo.ConsolidacionFinalizadaAsync(operacionId);
        }
    }
}
