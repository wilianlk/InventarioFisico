using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace InventarioFisico.Repositories
{
    public class GrupoConteoRepository
    {
        private readonly IConnectionStringProvider _provider;

        public GrupoConteoRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public async Task<int> CrearAsync(GrupoConteo grupo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string insert = @"
                INSERT INTO grupo_conteo
                (gc_nombre, gc_estado, gc_fecha_creacion, gc_usuario_creacion)
                VALUES (?, ?, ?, ?)";

            using (var cmd = new DB2Command(insert, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = grupo.Nombre });
                cmd.Parameters.Add(new DB2Parameter { Value = grupo.Estado });
                cmd.Parameters.Add(new DB2Parameter { Value = grupo.FechaCreacion });
                cmd.Parameters.Add(new DB2Parameter { Value = grupo.UsuarioCreacion });
                await cmd.ExecuteNonQueryAsync();
            }

            const string getId = "SELECT DBINFO('sqlca.sqlerrd1') FROM systables WHERE tabid = 1";
            using var cmdGet = new DB2Command(getId, conn);
            return int.Parse((await cmdGet.ExecuteScalarAsync()).ToString());
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = "SELECT COUNT(1) FROM grupo_conteo WHERE UPPER(gc_nombre) = UPPER(?)";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = nombre });

            var result = await cmd.ExecuteScalarAsync();
            return int.Parse(result.ToString()) > 0;
        }

        public async Task<List<GrupoConteo>> ObtenerTodosAsync()
        {
            var lista = new List<GrupoConteo>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT gc_id, gc_nombre, gc_estado, gc_fecha_creacion, gc_usuario_creacion
                FROM grupo_conteo";

            using var cmd = new DB2Command(sql, conn);
            using DbDataReader r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                lista.Add(Map(r));
            }

            return lista;
        }

        public async Task<List<GrupoConteo>> ObtenerPorOperacionAsync(int operacionId)
        {
            var lista = new List<GrupoConteo>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT g.gc_id, g.gc_nombre, g.gc_estado, g.gc_fecha_creacion, g.gc_usuario_creacion
                FROM grupo_conteo g
                INNER JOIN operacion_grupos og ON og.grupo_id = g.gc_id
                WHERE og.operacion_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(Map(r));
            }

            return lista;
        }

        public async Task<List<GrupoConteo>> ObtenerDisponiblesAsync()
        {
            var lista = new List<GrupoConteo>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    gc.gc_id,
                    gc.gc_nombre,
                    gc.gc_estado,
                    gc.gc_fecha_creacion,
                    gc.gc_usuario_creacion,
                    0 AS tiene_conteo_abierto,
                    CAST(NULL AS INTEGER) AS operacion_id_conteo_abierto
                FROM grupo_conteo gc
                WHERE gc.gc_estado = 'ACTIVO'";

            using var cmd = new DB2Command(sql, conn);
            using DbDataReader r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                lista.Add(new GrupoConteo
                {
                    Id = r.GetInt32(0),
                    Nombre = r.GetString(1).Trim(),
                    Estado = r.GetString(2).Trim(),
                    FechaCreacion = r.GetString(3),
                    UsuarioCreacion = r.GetInt32(4),
                    TieneConteoAbierto = r.GetInt32(5) == 1,
                    OperacionIdConteoAbierto = r.IsDBNull(6) ? null : r.GetInt32(6)
                });
            }

            return lista;
        }

        public async Task ActualizarEstadoAsync(int grupoId, string estado)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = "UPDATE grupo_conteo SET gc_estado = ? WHERE gc_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = estado });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AsociarOperacionGrupoAsync(int operacionId, int grupoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = "INSERT INTO operacion_grupos (operacion_id, grupo_id) VALUES (?, ?)";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DesasociarOperacionGrupoAsync(int operacionId, int grupoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = "DELETE FROM operacion_grupos WHERE operacion_id = ? AND grupo_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<GrupoConteo> ObtenerPorIdAsync(int id)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT gc_id, gc_nombre, gc_estado, gc_fecha_creacion, gc_usuario_creacion
                FROM grupo_conteo
                WHERE gc_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = id });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return Map(r);
            }

            return null;
        }

        private static GrupoConteo Map(DbDataReader r)
        {
            return new GrupoConteo
            {
                Id = r.GetInt32(0),
                Nombre = r.GetString(1).Trim(),
                Estado = r.GetString(2).Trim(),
                FechaCreacion = r.GetString(3),
                UsuarioCreacion = r.GetInt32(4)
            };
        }
    }
}
