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

        public async Task<int> CrearAsync(OperacionConteo conteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO operacion_conteo (operacion_id, grupo_id, numero_conteo, estado)
                VALUES (?, ?, ?, ?);
                SELECT dbinfo('sqlca.sqlerrd1') FROM systables WHERE tabid = 1;
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("operacion_id", conteo.OperacionId));
            cmd.Parameters.Add(new DB2Parameter("grupo_id", conteo.GrupoId));
            cmd.Parameters.Add(new DB2Parameter("numero_conteo", conteo.NumeroConteo));
            cmd.Parameters.Add(new DB2Parameter("estado", conteo.Estado));

            var result = await cmd.ExecuteScalarAsync();
            return int.Parse(result.ToString());
        }

        public async Task<OperacionConteo> ObtenerPorIdAsync(int id)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id, operacion_id, grupo_id, numero_conteo, estado, fecha_creacion
                FROM operacion_conteo
                WHERE id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("id", id));

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = r.GetString(4),
                    FechaCreacion = r.GetDateTime(5)
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
                SELECT id, operacion_id, grupo_id, numero_conteo, estado, fecha_creacion
                FROM operacion_conteo
                WHERE operacion_id = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("operacion_id", operacionId));

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = r.GetString(4),
                    FechaCreacion = r.GetDateTime(5)
                });
            }

            return lista;
        }

        public async Task<List<OperacionConteo>> ObtenerPendientesAsync()
        {
            var lista = new List<OperacionConteo>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id, operacion_id, grupo_id, numero_conteo, estado, fecha_creacion
                FROM operacion_conteo
                WHERE estado = 'PENDIENTE'
                ORDER BY fecha_creacion
            ";

            using var cmd = new DB2Command(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = r.GetString(4),
                    FechaCreacion = r.GetDateTime(5)
                });
            }

            return lista;
        }

        public async Task<OperacionConteo> ObtenerPendientePorOperacionYGrupoAsync(int operacionId, int grupoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id, operacion_id, grupo_id, numero_conteo, estado, fecha_creacion
                FROM operacion_conteo
                WHERE operacion_id = ?
                  AND grupo_id = ?
                  AND estado = 'PENDIENTE'
                ORDER BY fecha_creacion
                FIRST 1
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("operacion_id", operacionId));
            cmd.Parameters.Add(new DB2Parameter("grupo_id", grupoId));

            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new OperacionConteo
                {
                    Id = r.GetInt32(0),
                    OperacionId = r.GetInt32(1),
                    GrupoId = r.GetInt32(2),
                    NumeroConteo = r.GetInt32(3),
                    Estado = r.GetString(4),
                    FechaCreacion = r.GetDateTime(5)
                };
            }

            return null;
        }
    }
}
