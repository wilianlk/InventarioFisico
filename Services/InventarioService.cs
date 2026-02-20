using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class InventarioService
    {
        private readonly InventarioRepository _repo;
        private readonly GrupoPersonaService _personaService;
        private readonly GrupoUbicacionService _ubicacionService;
        private readonly GrupoConteoService _grupoService;

        public InventarioService(
            InventarioRepository repo,
            GrupoPersonaService personaService,
            GrupoUbicacionService ubicacionService,
            GrupoConteoService grupoService)
        {
            _repo = repo;
            _personaService = personaService;
            _ubicacionService = ubicacionService;
            _grupoService = grupoService;
        }

        public async Task<List<InventarioOperacion>> ObtenerOperacionesAsync()
        {
            return await _repo.ObtenerOperacionesAsync();
        }

        public async Task<InventarioOperacion> ObtenerOperacionPorIdAsync(int id)
        {
            return await _repo.ObtenerOperacionPorIdAsync(id);
        }

        public async Task ValidarCreacionAsync(InventarioOperacion operacion)
        {
            if (operacion.GruposIds == null || operacion.GruposIds.Count == 0)
                throw new InvalidOperationException("Debe asignar al menos un grupo para crear la operación.");

            var gruposIncompletos = new List<string>();

            foreach (var grupoId in operacion.GruposIds.Distinct())
            {
                var personas = await _personaService.ObtenerPersonasAsync(grupoId);
                var ubicaciones = await _ubicacionService.ObtenerAsync(grupoId);

                if (!personas.Any() || !ubicaciones.Any())
                {
                    var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
                    if (grupo != null)
                        gruposIncompletos.Add(grupo.Nombre);
                }
            }

            if (gruposIncompletos.Any())
            {
                throw new InvalidOperationException(
                    "Antes de crear esta operación, completa personas y ubicaciones en: " +
                    string.Join(", ", gruposIncompletos)
                );
            }
        }

        public async Task<int> CrearOperacionAsync(InventarioOperacion operacion)
        {
            await ValidarCreacionAsync(operacion);

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
                return;

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

        public async Task<string> ObtenerEstadoOperacionAsync(int operacionId)
        {
            var operacion = await _repo.ObtenerOperacionPorIdAsync(operacionId);
            return operacion?.Estado;
        }
    }
}
