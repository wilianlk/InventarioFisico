using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class OperacionConteoItemsService
    {
        private readonly OperacionConteoItemsRepository _repo;
        private readonly InventarioService _inventarioService;
        private readonly ConsolidacionRepository _consolidacionRepo;

        public OperacionConteoItemsService(
            OperacionConteoItemsRepository repo,
            InventarioService inventarioService,
            ConsolidacionRepository consolidacionRepo)
        {
            _repo = repo;
            _inventarioService = inventarioService;
            _consolidacionRepo = consolidacionRepo;
        }

        public Task<List<OperacionConteoItem>> ObtenerPorConteoAsync(int conteoId)
        {
            return _repo.ObtenerPorConteoAsync(conteoId);
        }

        public Task<OperacionConteoItem> ObtenerPorIdAsync(int itemId)
        {
            return _repo.ObtenerPorIdAsync(itemId);
        }

        public async Task ActualizarCantidadContadaAsync(int itemId, int cantidadContada)
        {
            if (cantidadContada < 0)
                throw new InvalidOperationException("La cantidad contada no puede ser negativa.");

            var item = await _repo.ObtenerPorIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Ítem no encontrado.");

            var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(item.OperacionId);
            if (operacion == null)
                throw new InvalidOperationException("La operación no existe.");

            if (string.Equals(operacion.Estado, "CERRADA", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("No se puede modificar: la operación está cerrada.");

            if (await _consolidacionRepo.OperacionEstaConsolidadaAsync(item.OperacionId))
                throw new InvalidOperationException("No se puede modificar: la operación ya fue consolidada.");

            await _repo.ActualizarCantidadContadaAsync(itemId, cantidadContada);
        }
    }
}
