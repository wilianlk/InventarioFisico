using System;
using System.Linq;
using System.Threading.Tasks;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class CerrarConteoService
    {
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly ConsolidacionRepository _consolidacionRepo;
        private readonly InventarioRepository _inventarioRepo;

        public CerrarConteoService(
            OperacionConteoRepository conteoRepo,
            ConsolidacionRepository consolidacionRepo,
            InventarioRepository inventarioRepo)
        {
            _conteoRepo = conteoRepo;
            _consolidacionRepo = consolidacionRepo;
            _inventarioRepo = inventarioRepo;
        }

        public async Task CerrarConteoAsync(int conteoId)
        {
            var conteo = await _conteoRepo.ObtenerPorIdAsync(conteoId);
            if (conteo == null)
                throw new InvalidOperationException("Conteo no existe");

            if (conteo.Estado != "CERRADO")
                await _conteoRepo.CerrarConteoAsync(conteoId);

            await ActualizarOperacionConsolidadaSiAplicaAsync(conteo.OperacionId);
        }

        public async Task ActualizarOperacionConsolidadaSiAplicaAsync(int operacionId)
        {
            var operacion = await _inventarioRepo.ObtenerOperacionPorIdAsync(operacionId);
            if (operacion == null)
                return;

            var estadoOperacion = (operacion.Estado ?? string.Empty).Trim();

            if (string.Equals(estadoOperacion, "FINALIZADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estadoOperacion, "ELIMINADA", StringComparison.OrdinalIgnoreCase))
                return;

            var conteosOperacion = await _conteoRepo.ObtenerPorOperacionAsync(operacionId);
            if (conteosOperacion.Count == 0)
                return;

            var todosCerrados = conteosOperacion.All(c =>
            {
                var estadoConteo = (c.Estado ?? string.Empty).Trim();
                return string.Equals(estadoConteo, "CERRADO", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(estadoConteo, "FINALIZADO", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(estadoConteo, "FINALIZADA", StringComparison.OrdinalIgnoreCase);
            });
            if (!todosCerrados)
                return;

            foreach (var conteoOperacion in conteosOperacion)
            {
                var tieneDetalle = await _consolidacionRepo.ExisteDetalleConteoAsync(
                    conteoOperacion.OperacionId,
                    conteoOperacion.GrupoId,
                    conteoOperacion.NumeroConteo
                );

                if (tieneDetalle)
                    continue;

                var cabeceraId = await _consolidacionRepo.InsertarCabeceraDesdeConteoAsync(
                    conteoOperacion.OperacionId,
                    conteoOperacion.GrupoId,
                    conteoOperacion.NumeroConteo
                );

                await _consolidacionRepo.InsertarDetalleDesdeConteoAsync(
                    cabeceraId,
                    conteoOperacion.OperacionId,
                    conteoOperacion.GrupoId,
                    conteoOperacion.NumeroConteo
                );
            }

            // Cuando todos los conteos estén cerrados, la operación pasa a CONSOLIDADA
            // salvo que ya esté en un estado terminal.
            if (!string.Equals(estadoOperacion, "CONSOLIDADA", StringComparison.OrdinalIgnoreCase))
                await _inventarioRepo.ActualizarEstadoAsync(operacionId, "CONSOLIDADA");
        }
    }
}
