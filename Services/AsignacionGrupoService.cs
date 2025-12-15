using System.Linq;
using System.Threading.Tasks;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class AsignacionGrupoService
    {
        private readonly GrupoConteoRepository _grupoRepo;
        private readonly GrupoPersonaRepository _personaRepo;

        public AsignacionGrupoService(
            GrupoConteoRepository grupoRepo,
            GrupoPersonaRepository personaRepo)
        {
            _grupoRepo = grupoRepo;
            _personaRepo = personaRepo;
        }

        public async Task<int?> ObtenerGrupoPorUsuarioAsync(int operacionId, int usuarioId)
        {
            var grupos = await _grupoRepo.ObtenerPorOperacionAsync(operacionId);

            foreach (var g in grupos)
            {
                var personas = await _personaRepo.ObtenerPorGrupoAsync(g.Id);
                if (personas.Any(p => p.UsuarioId == usuarioId))
                    return g.Id;
            }

            return null;
        }
    }
}
