using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class InventarioService
    {
        private readonly InventarioRepository _repo;

        public InventarioService(InventarioRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<InventarioOperacion>> ObtenerOperacionesAsync()
        {
            return await _repo.ObtenerOperacionesAsync();
        }

        public async Task<InventarioOperacion> ObtenerOperacionPorIdAsync(int id)
        {
            return await _repo.ObtenerOperacionPorIdAsync(id);
        }

        public async Task<int> CrearOperacionAsync(InventarioOperacion operacion)
        {
            if (operacion.GruposIds == null || operacion.GruposIds.Count == 0)
                throw new InvalidOperationException("Debe asignar al menos un grupo para crear la operación.");

            operacion.FechaCreacion = DateTime.Now;
            operacion.Estado = "EN_PREPARACION";

            return await _repo.CrearOperacionAsync(operacion);
        }

        public async Task CerrarOperacionAsync(int id)
        {
            var operacion = await _repo.ObtenerOperacionPorIdAsync(id);
            if (operacion == null)
                throw new InvalidOperationException("La operación no existe.");

            if (operacion.Estado == "CERRADA")
                throw new InvalidOperationException("La operación ya está cerrada.");

            await _repo.ActualizarEstadoAsync(id, "CERRADA");
        }

        public async Task EliminarOperacionAsync(int id)
        {
            var operacion = await _repo.ObtenerOperacionPorIdAsync(id);
            if (operacion == null)
                throw new InvalidOperationException("La operación no existe.");

            if (operacion.Estado != "EN_PREPARACION")
                throw new InvalidOperationException("Solo pueden eliminarse operaciones en estado EN_PREPARACION.");

            await _repo.EliminarOperacionAsync(id);
        }
    }
}
