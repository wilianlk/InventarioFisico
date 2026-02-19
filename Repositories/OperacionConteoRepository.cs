using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventarioFisico.Repositories
{
    public class OperacionConteoRepository
    {
        private readonly IConnectionStringProvider _provider;

        public OperacionConteoRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        private static string TrimSafe(string s) => (s ?? "").Trim();

        private static string TrimOrEmpty(DB2DataReader r, int i)
        {
            if (r.IsDBNull(i)) return "";
            return (r.GetString(i) ?? "").Trim();
        }

        public async Task<int> CrearAsync(OperacionConteo conteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var insertSql = @"
                INSERT INTO operacion_conteo
                (operacion_id,grupo_id,numero_conteo,estado)
                VALUES (?,?,?,?)
            ";

            using (var cmd = new DB2Command(insertSql, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = conteo.OperacionId });
                cmd.Parameters.Add(new DB2Parameter { Value = conteo.GrupoId });
                cmd.Parameters.Add(new DB2Parameter { Value = conteo.NumeroConteo });
                cmd.Parameters.Add(new DB2Parameter { Value = TrimSafe(conteo.Estado) });
                await cmd.ExecuteNonQueryAsync();
            }

            using var cmdId = new DB2Command(
                "SELECT dbinfo('sqlca.sqlerrd1') FROM systables WHERE tabid = 1",
                conn
            );

            var id = await cmdId.ExecuteScalarAsync();
            return int.Parse(id.ToString());
        }

        public async Task<OperacionConteo> ObtenerPorOperacionGrupoNumeroAsync(int operacionId, int grupoId, int numeroConteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT oc.id,
                       oc.operacion_id,
                       oc.grupo_id,
                       oc.numero_conteo,
                       oc.estado,
                       oc.fecha_creacion,
                       gc.gc_nombre
                FROM operacion_conteo oc
                LEFT JOIN grupo_conteo gc
                    ON gc.gc_id = oc.grupo_id
                WHERE oc.operacion_id = ?
                  AND oc.grupo_id = ?
                  AND oc.numero_conteo = ?
                FIRST 1
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
            cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = TrimOrEmpty(r, 4),
                    FechaCreacion = r.GetDateTime(5),
                    NombreGrupo = TrimOrEmpty(r, 6)
                };
            }

            return null;
        }

        public async Task<OperacionConteo> ObtenerPorIdAsync(int id)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT oc.id,
                       oc.operacion_id,
                       oc.grupo_id,
                       oc.numero_conteo,
                       oc.estado,
                       oc.fecha_creacion,
                       gc.gc_nombre
                FROM operacion_conteo oc
                LEFT JOIN grupo_conteo gc
                    ON gc.gc_id = oc.grupo_id
                WHERE oc.id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = id });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = TrimOrEmpty(r, 4),
                    FechaCreacion = r.GetDateTime(5),
                    NombreGrupo = TrimOrEmpty(r, 6)
                };
            }

            return null;
        }

        public async Task<List<OperacionConteo>> ObtenerPorOperacionAsync(int operacionId)
        {
            var lista = new List<OperacionConteo>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT oc.id,
                       oc.operacion_id,
                       oc.grupo_id,
                       oc.numero_conteo,
                       oc.estado,
                       oc.fecha_creacion,
                       gc.gc_nombre
                FROM operacion_conteo oc
                LEFT JOIN grupo_conteo gc
                    ON gc.gc_id = oc.grupo_id
                WHERE oc.operacion_id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = TrimOrEmpty(r, 4),
                    FechaCreacion = r.GetDateTime(5),
                    NombreGrupo = TrimOrEmpty(r, 6)
                });
            }

            return lista;
        }

        public async Task<List<OperacionConteo>> ObtenerAbiertosAsync()
        {
            var lista = new List<OperacionConteo>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT oc.id,
                       oc.operacion_id,
                       oc.grupo_id,
                       oc.numero_conteo,
                       oc.estado,
                       oc.fecha_creacion,
                       gc.gc_nombre
                FROM operacion_conteo oc
                LEFT JOIN grupo_conteo gc
                    ON gc.gc_id = oc.grupo_id
                WHERE oc.estado = 'ABIERTO'
                ORDER BY oc.fecha_creacion
            ";

            using var cmd = new DB2Command(sql, conn);
            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = TrimOrEmpty(r, 4),
                    FechaCreacion = r.GetDateTime(5),
                    NombreGrupo = TrimOrEmpty(r, 6)
                });
            }

            return lista;
        }

        public async Task<OperacionConteo> ObtenerAbiertoPorOperacionYGrupoAsync(int operacionId, int grupoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT oc.id,
                       oc.operacion_id,
                       oc.grupo_id,
                       oc.numero_conteo,
                       oc.estado,
                       oc.fecha_creacion,
                       gc.gc_nombre
                FROM operacion_conteo oc
                LEFT JOIN grupo_conteo gc
                    ON gc.gc_id = oc.grupo_id
                WHERE oc.operacion_id = ?
                  AND oc.grupo_id = ?
                  AND oc.estado = 'ABIERTO'
                ORDER BY oc.fecha_creacion
                FIRST 1
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = TrimOrEmpty(r, 4),
                    FechaCreacion = r.GetDateTime(5),
                    NombreGrupo = TrimOrEmpty(r, 6)
                };
            }

            return null;
        }

        public async Task CerrarConteoAsync(int conteoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE operacion_conteo
                SET estado = 'CERRADO'
                WHERE id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = conteoId });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarPorOperacionAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                DELETE FROM operacion_conteo
                WHERE operacion_id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
