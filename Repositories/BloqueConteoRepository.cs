using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;

namespace InventarioFisico.Repositories
{
    public class BloqueConteoRepository
    {
        private readonly IConnectionStringProvider _provider;

        public BloqueConteoRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public async Task<int> CrearAsync(BloqueConteo bloque)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"INSERT INTO bloque_conteo 
                        (bc_operacion_id, bc_grupo_id, bc_ubicacion_inicio, bc_ubicacion_fin, bc_pasillo,
                         bc_lado, bc_altura_inicio, bc_altura_fin, bc_posicion_inicio, bc_posicion_fin)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("", bloque.OperacionId));
            cmd.Parameters.Add(new DB2Parameter("", bloque.GrupoId.HasValue ? bloque.GrupoId.Value : (object)DBNull.Value));
            cmd.Parameters.Add(new DB2Parameter("", bloque.UbicacionInicio));
            cmd.Parameters.Add(new DB2Parameter("", bloque.UbicacionFin));
            cmd.Parameters.Add(new DB2Parameter("", bloque.Pasillo));
            cmd.Parameters.Add(new DB2Parameter("", bloque.Lado));
            cmd.Parameters.Add(new DB2Parameter("", bloque.AlturaInicio));
            cmd.Parameters.Add(new DB2Parameter("", bloque.AlturaFin));
            cmd.Parameters.Add(new DB2Parameter("", bloque.PosicionInicio));
            cmd.Parameters.Add(new DB2Parameter("", bloque.PosicionFin));

            await cmd.ExecuteNonQueryAsync();

            var idCmd = new DB2Command("SELECT DBINFO('sqlca.sqlerrd1') FROM systables WHERE tabname = 'bloque_conteo' FETCH FIRST 1 ROWS ONLY", conn);
            var result = await idCmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<BloqueConteo>> ObtenerPorOperacionAsync(int operacionId)
        {
            var lista = new List<BloqueConteo>();
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT bc_id, bc_operacion_id, bc_grupo_id, bc_ubicacion_inicio, bc_ubicacion_fin,
                               bc_pasillo, bc_lado, bc_altura_inicio, bc_altura_fin, bc_posicion_inicio, bc_posicion_fin
                        FROM bloque_conteo
                        WHERE bc_operacion_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("", operacionId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new BloqueConteo
                {
                    Id = reader.GetInt32(0),
                    OperacionId = reader.GetInt32(1),
                    GrupoId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    UbicacionInicio = reader.GetString(3).Trim(),
                    UbicacionFin = reader.GetString(4).Trim(),
                    Pasillo = reader.GetString(5).Trim(),
                    Lado = reader.GetString(6).Trim(),
                    AlturaInicio = reader.GetInt32(7),
                    AlturaFin = reader.GetInt32(8),
                    PosicionInicio = reader.GetInt32(9),
                    PosicionFin = reader.GetInt32(10)
                });
            }

            return lista;
        }

        public async Task<List<BloqueConteo>> ObtenerPorGrupoAsync(int grupoId)
        {
            var lista = new List<BloqueConteo>();
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT bc_id, bc_operacion_id, bc_grupo_id, bc_ubicacion_inicio, bc_ubicacion_fin,
                               bc_pasillo, bc_lado, bc_altura_inicio, bc_altura_fin, bc_posicion_inicio, bc_posicion_fin
                        FROM bloque_conteo
                        WHERE bc_grupo_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("", grupoId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new BloqueConteo
                {
                    Id = reader.GetInt32(0),
                    OperacionId = reader.GetInt32(1),
                    GrupoId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    UbicacionInicio = reader.GetString(3).Trim(),
                    UbicacionFin = reader.GetString(4).Trim(),
                    Pasillo = reader.GetString(5).Trim(),
                    Lado = reader.GetString(6).Trim(),
                    AlturaInicio = reader.GetInt32(7),
                    AlturaFin = reader.GetInt32(8),
                    PosicionInicio = reader.GetInt32(9),
                    PosicionFin = reader.GetInt32(10)
                });
            }

            return lista;
        }

        public async Task ActualizarGrupoAsync(int bloqueId, int? grupoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"UPDATE bloque_conteo SET bc_grupo_id = ? WHERE bc_id = ?";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("", grupoId.HasValue ? grupoId.Value : (object)DBNull.Value));
            cmd.Parameters.Add(new DB2Parameter("", bloqueId));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
