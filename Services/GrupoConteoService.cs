using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class GrupoConteoService
    {
        private readonly GrupoConteoRepository _repo;

        public GrupoConteoService(GrupoConteoRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> CrearGrupoAsync(string nombre, int usuarioId)
        {
            var nombreTrim = nombre?.Trim();
            if (string.IsNullOrEmpty(nombreTrim))
                throw new InvalidOperationException("El nombre del grupo es obligatorio.");

            if (await _repo.ExisteNombreAsync(nombreTrim))
                throw new InvalidOperationException("Ya existe un grupo con ese nombre.");

            var grupo = new GrupoConteo
            {
                Nombre = nombreTrim,
                Estado = "ACTIVO",
                FechaCreacion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UsuarioCreacion = usuarioId
            };

            return await _repo.CrearAsync(grupo);
        }

        public Task<List<GrupoConteo>> ObtenerTodosAsync()
        {
            return _repo.ObtenerTodosAsync();
        }

        public Task<List<GrupoConteo>> ObtenerPorOperacionAsync(int operacionId)
        {
            return _repo.ObtenerPorOperacionAsync(operacionId);
        }

        public Task<List<GrupoConteo>> ObtenerDisponiblesAsync()
        {
            return _repo.ObtenerDisponiblesAsync();
        }

        public Task InactivarGrupoAsync(int grupoId)
        {
            return _repo.ActualizarEstadoAsync(grupoId, "INACTIVO");
        }

        public Task ActivarGrupoAsync(int grupoId)
        {
            return _repo.ActualizarEstadoAsync(grupoId, "ACTIVO");
        }

        public Task AsociarOperacionGrupoAsync(int operacionId, int grupoId)
        {
            return _repo.AsociarOperacionGrupoAsync(operacionId, grupoId);
        }

        public Task DesasociarOperacionGrupoAsync(int operacionId, int grupoId)
        {
            return _repo.DesasociarOperacionGrupoAsync(operacionId, grupoId);
        }

        public Task<GrupoConteo> ObtenerPorIdAsync(int id)
        {
            return _repo.ObtenerPorIdAsync(id);
        }
    }
}
