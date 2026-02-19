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

        public OperacionConteoItemsService(OperacionConteoItemsRepository repo)
        {
            _repo = repo;
        }

        public Task<List<OperacionConteoItem>> ObtenerPorConteoAsync(int conteoId)
        {
            return _repo.ObtenerPorConteoAsync(conteoId);
        }

        public Task<List<OperacionConteoItem>> ObtenerNoEncontradosPorConteoAsync(int conteoId)
        {
            return _repo.ObtenerNoEncontradosPorConteoAsync(conteoId);
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

            await _repo.ActualizarCantidadContadaAsync(itemId, cantidadContada);
        }

        public async Task ActualizarNoEncontradoAsync(int itemId, bool noEncontrado)
        {
            var item = await _repo.ObtenerPorIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Ítem no encontrado.");

            await _repo.ActualizarNoEncontradoAsync(itemId, noEncontrado);
        }
    }
}
