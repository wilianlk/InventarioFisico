using System.Collections.Generic;
using System.Threading.Tasks;
using IBM.Data.Db2;
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

        public Task<List<GrupoUbicacion>> ObtenerPorGrupoAsync(int grupoId)
        {
            return _repo.ObtenerPorGrupoAsync(grupoId);
        }

        public async Task AgregarAsync(int grupoId, string ubicacion)
        {
            ubicacion = ubicacion.Trim().ToUpper();

            var ubicacionesGrupo = await _repo.ObtenerUbicacionesPorGrupoAsync(grupoId);
            if (ubicacionesGrupo.Exists(u => u.Trim().ToUpper() == ubicacion))
                throw new System.InvalidOperationException(
                    $"La ubicación {ubicacion} ya está asignada a este grupo."
                );

            var u = new GrupoUbicacion
            {
                GrupoId = grupoId,
                Ubicacion = ubicacion
            };

            await _repo.AgregarAsync(u);
        }

        public async Task<List<string>> ObtenerRangoUbicacionesAsync(string desde, string hasta)
        {
            return await _repo.ObtenerRangoUbicacionesAsync(desde, hasta);
        }

        public async Task<List<ItemPhystag>> ObtenerItemsPorUbicacionAsync(string ubicacion)
        {
            using var conn = new DB2Connection(_repo.ObtenerProveedor().Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    p.tg_cmpy,
                    p.tg_ware,
                    p.tg_tag,
                    p.tg_part,
                    p.tg_prod,
                    p.tg_bin,
                    p.tg_lot,
                    p.tg_desc,
                    p.tg_stku,
                    p.tg_cost,
                    p.tg_count
                FROM phystag p
                JOIN itemmain i 
                      ON i.pr_cmpy = p.tg_cmpy
                     AND i.pr_id   = p.tg_part
                WHERE p.tg_cmpy = 'RE'
                  AND p.tg_bin = ?
                ORDER BY p.tg_part
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("ubicacion", ubicacion));

            var lista = new List<ItemPhystag>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new ItemPhystag
                {
                    Cmpy = reader.GetString(0),
                    Bodega = reader.GetString(1),
                    Etiqueta = reader.GetString(2),
                    Item = reader.GetString(3),
                    Prod = reader.GetString(4),
                    Ubicacion = reader.GetString(5),
                    Lote = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Descripcion = reader.GetString(7),
                    Udm = reader.GetString(8),
                    Costo = reader.GetDecimal(9),
                    CantidadSistema = reader.GetDecimal(10)
                });
            }

            return lista;
        }

        public Task EliminarAsync(int grupoId, string ubicacion)
        {
            return _repo.EliminarAsync(grupoId, ubicacion.Trim().ToUpper());
        }
    }
}
