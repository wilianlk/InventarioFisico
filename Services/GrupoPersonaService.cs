using System.Collections.Generic;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class GrupoPersonaService
    {
        private readonly GrupoPersonaRepository _repo;

        public GrupoPersonaService(GrupoPersonaRepository repo)
        {
            _repo = repo;
        }

        public async Task AgregarPersonaAsync(int grupoId, int usuarioId, string usuarioNombre)
        {
            var persona = new GrupoPersona
            {
                GrupoId = grupoId,
                UsuarioId = usuarioId,
                UsuarioNombre = usuarioNombre.Trim()
            };

            await _repo.AgregarAsync(persona);
        }

        public async Task EliminarPersonaAsync(int grupoId, int usuarioId)
        {
            await _repo.EliminarAsync(grupoId, usuarioId);
        }

        public async Task<List<GrupoPersona>> ObtenerPersonasAsync(int grupoId)
        {
            return await _repo.ObtenerPorGrupoAsync(grupoId);
        }
    }
}
