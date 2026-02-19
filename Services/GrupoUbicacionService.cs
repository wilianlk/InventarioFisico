using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class GrupoUbicacionService
    {
        private readonly GrupoUbicacionRepository _repo;

        public GrupoUbicacionService(GrupoUbicacionRepository repo)
        {
            _repo = repo;
        }

        private static string N(string? s)
            => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToUpper();

        private static string NormalizarBodega(string? bodega)
        {
            var b = N(bodega);
            if (b == "") throw new InvalidOperationException("Bodega obligatoria.");
            return b;
        }

        public async Task<List<ItemPhystag>> ObtenerAsync(int? grupoId)
        {
            var filtros = await _repo.ObtenerFiltrosAsync(grupoId);
            var resultado = new List<ItemPhystag>();

            foreach (var f in filtros.Where(x => !string.IsNullOrWhiteSpace(x.Ubicaciones)))
            {
                var items = await _repo.ObtenerItemsPorUbicacionExactaAsync(
                    f.Bodega,
                    f.Ubicaciones
                );

                foreach (var item in items)
                {
                    item.GrupoId = f.GrupoId;
                    item.GrupoNombre = f.GrupoNombre;
                }

                resultado.AddRange(items);
            }

            return resultado
                .GroupBy(x => new { x.Bodega, x.Ubicacion, x.Item, x.Lote, x.GrupoId })
                .Select(g => g.First())
                .ToList();
        }

        public async Task<List<ItemPhystag>> PrevisualizarAsync(
            string? bodega,
            string? rack,
            string? lado,
            string? altura,
            string? ubicacion)
        {
            var b = NormalizarBodega(bodega);
            var r = N(rack);
            var l = b == "11" ? "" : N(lado);
            var a = N(altura);
            var u = N(ubicacion);

            return await _repo.ObtenerItemsPorFiltroAsync(
                b,
                r,
                l,
                a,
                u
            );
        }

        public async Task AgregarAsync(
            int grupoId,
            string? bodega,
            List<GrupoUbicacion> ubicaciones)
        {
            if (grupoId <= 0)
                throw new InvalidOperationException("Grupo inválido.");

            var b = NormalizarBodega(bodega);

            if (ubicaciones == null || ubicaciones.Count == 0)
                throw new InvalidOperationException("No hay ubicaciones para agregar.");

            foreach (var u in ubicaciones)
            {
                if (!await _repo.ExisteEnInventarioAsync(b, u.Ubicaciones))
                    throw new InvalidOperationException($"La ubicación {u.Ubicaciones} no existe en inventario.");

                await _repo.AgregarAsync(new GrupoUbicacion
                {
                    GrupoId = grupoId,
                    Bodega = b,
                    Ubicaciones = N(u.Ubicaciones),
                    Rack = N(u.Rack),
                    Lado = N(u.Lado),
                    Altura = N(u.Altura),
                    Ubicacion = N(u.Ubicacion)
                });
            }
        }

        public async Task EliminarAsync(
            int grupoId,
            string? bodega,
            string? rack,
            string? lado,
            string? altura,
            string? ubicacion)
        {
            var b = NormalizarBodega(bodega);
            var l = b == "11" ? "" : N(lado);

            await _repo.EliminarAsync(
                grupoId,
                b,
                N(rack),
                l,
                N(altura),
                N(ubicacion)
            );
        }

        public async Task<List<ItemPhystag>> BuscarPorItemAsync(
            string? bodega,
            string? codigoItem,
            string? lote = null)
        {
            var b = NormalizarBodega(bodega);
            var item = N(codigoItem);
            
            if (string.IsNullOrWhiteSpace(item))
                throw new InvalidOperationException("Código de item es obligatorio.");

            var l = N(lote);
            
            return await _repo.ObtenerItemsPorCodigoItemAsync(b, item, string.IsNullOrWhiteSpace(l) ? null : l);
        }

        public async Task<List<object>> ObtenerBodegasAsync()
        {
            var data = await _repo.ObtenerBodegasAsync();

            return data
                .Select(x => new
                {
                    id = x.Id,
                    descripcion = x.Descripcion
                })
                .Cast<object>()
                .ToList();
        }
    }
}
