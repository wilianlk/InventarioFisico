using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class BloqueConteoService
    {
        private readonly BloqueConteoRepository _repo;

        public BloqueConteoService(BloqueConteoRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> CrearBloqueAsync(BloqueConteo bloque)
        {
            Console.WriteLine($"Creando bloque para operacion {bloque.OperacionId}");
            return await _repo.CrearAsync(bloque);
        }

        public async Task<List<BloqueConteo>> ObtenerPorOperacionAsync(int operacionId)
        {
            return await _repo.ObtenerPorOperacionAsync(operacionId);
        }

        public async Task ActualizarGrupoAsync(int bloqueId, int? grupoId)
        {
            Console.WriteLine($"Asignando bloque {bloqueId} al grupo {grupoId}");
            await _repo.ActualizarGrupoAsync(bloqueId, grupoId);
        }

        public async Task<List<BloqueConteo>> ObtenerPorGrupoAsync(int grupoId)
        {
            return await _repo.ObtenerPorGrupoAsync(grupoId);
        }
    }
}
