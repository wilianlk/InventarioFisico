using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IBM.Data.Db2;
using InventarioFisico.Models;
using InventarioFisico.Infrastructure;

namespace InventarioFisico.Repositories
{
    public class GrupoUbicacionRepository
    {
        private readonly IConnectionStringProvider _provider;

        public GrupoUbicacionRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public IConnectionStringProvider ObtenerProveedor()
        {
            return _provider;
        }

        public async Task<List<GrupoUbicacion>> ObtenerPorGrupoAsync(int grupoId)
        {
            var lista = new List<GrupoUbicacion>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT gu_id, gu_grupo_id, gu_ubicacion
                FROM grupo_ubicacion
                WHERE gu_grupo_id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", grupoId));

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new GrupoUbicacion
                {
                    Id = r.GetInt32(0),
                    GrupoId = r.GetInt32(1),
                    Ubicacion = r.GetString(2).Trim()
                });
            }

            return lista;
        }

        public async Task<(int GrupoId, string GrupoNombre)?> ObtenerGrupoAsignadoAsync(string ubicacion)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT FIRST 1 g.gc_id, g.gc_nombre
                FROM grupo_ubicacion u
                JOIN grupo_conteo g ON g.gc_id = u.gu_grupo_id
                WHERE UPPER(u.gu_ubicacion) = UPPER(?)
                  AND g.gc_estado = 'ACTIVO'
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("ubicacion", ubicacion));

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return (r.GetInt32(0), r.GetString(1).Trim());
            }

            return null;
        }

        public async Task<bool> ExisteUbicacionAsync(string ubicacion)
        {
            var asignado = await ObtenerGrupoAsignadoAsync(ubicacion);
            return asignado.HasValue;
        }

        public async Task<List<string>> ObtenerRangoUbicacionesAsync(string desde, string hasta)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT tg_bin
                FROM phystag
                WHERE tg_cmpy = 'RE'
                  AND tg_bin BETWEEN ? AND ?
                ORDER BY tg_bin
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("desde", desde));
            cmd.Parameters.Add(new DB2Parameter("hasta", hasta));

            var lista = new List<string>();

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(r.GetString(0).Trim());
            }

            return lista;
        }

        public async Task AgregarAsync(GrupoUbicacion ubicacion)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO grupo_ubicacion (gu_grupo_id, gu_ubicacion)
                VALUES (?, ?)
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", ubicacion.GrupoId));
            cmd.Parameters.Add(new DB2Parameter("ubicacion", ubicacion.Ubicacion));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarAsync(int grupoId, string ubicacion)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                DELETE FROM grupo_ubicacion
                WHERE gu_grupo_id = ? AND gu_ubicacion = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", grupoId));
            cmd.Parameters.Add(new DB2Parameter("ubicacion", ubicacion));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> ObtenerUbicacionesPorGrupoAsync(int grupoId)
        {
            var ubicaciones = new List<string>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT gu_ubicacion
                FROM grupo_ubicacion
                WHERE gu_grupo_id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", grupoId));

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                ubicaciones.Add(r.GetString(0).Trim());
            }

            return ubicaciones;
        }
    }
}
