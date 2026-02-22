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
                throw new InvalidOperationException("Debe asignar al menos un grupo para crear la operaci贸n.");

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
                    "Antes de crear esta operaci贸n, completa personas y ubicaciones en: " +
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
                throw new InvalidOperationException("La operaci贸n no existe.");

            if (operacion.Estado == "ELIMINADA")
                throw new InvalidOperationException("No se puede cerrar una operaci髇 eliminada.");

            if (operacion.Estado == "CERRADA" || operacion.Estado == "CONSOLIDADA" || operacion.Estado == "FINALIZADA")
                return;

            await _repo.ActualizarEstadoAsync(id, "CERRADA");
        }

        public async Task<(int NumeroAnterior, int NumeroNuevo)> ActualizarNumeroConteoAsync(int id, int numeroConteo)
        {
            var operacion = await _repo.ObtenerOperacionPorIdAsync(id);
            if (operacion == null)
                throw new InvalidOperationException("La operaci贸n no existe.");

            if (operacion.Estado == "CERRADA" || operacion.Estado == "CONSOLIDADA" || operacion.Estado == "FINALIZADA" || operacion.Estado == "ELIMINADA")
                throw new InvalidOperationException("No se puede editar una operacion cerrada, consolidada, finalizada o eliminada.");

            if (numeroConteo < 1 || numeroConteo > 3)
                throw new InvalidOperationException("El n煤mero de conteo debe estar entre 1 y 3.");


            await _repo.ActualizarNumeroConteoAsync(id, numeroConteo);
            return (operacion.NumeroConteo, numeroConteo);
        }

        public async Task EliminarOperacionAsync(int id)
        {
            var operacion = await _repo.ObtenerOperacionPorIdAsync(id);
            if (operacion == null)
                throw new InvalidOperationException("La operaci贸n no existe.");

            await _repo.EliminarOperacionAsync(id);
        }

        public async Task<string> ObtenerEstadoOperacionAsync(int operacionId)
        {
            var operacion = await _repo.ObtenerOperacionPorIdAsync(operacionId);
            return operacion?.Estado;
        }
    }
}




