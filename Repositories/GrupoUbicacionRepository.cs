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

        public async Task<List<GrupoUbicacion>> ObtenerFiltrosAsync(int? grupoId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    gu.gu_grupo_id,
                    gu.gu_bodega,
                    gu.gu_ubicacion,
                    gu.gu_rack,
                    gu.gu_lado,
                    gu.gu_altura,
                    gu.gu_posicion,
                    gc.gc_nombre
                FROM grupo_ubicacion gu
                JOIN grupo_conteo gc
                  ON gc.gc_id = gu.gu_grupo_id
            ";

            if (grupoId.HasValue)
                sql += " WHERE gu.gu_grupo_id = ?";

            using var cmd = new DB2Command(sql, conn);

            if (grupoId.HasValue)
                cmd.Parameters.Add(new DB2Parameter("grupo", grupoId.Value));

            var lista = new List<GrupoUbicacion>();

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new GrupoUbicacion
                {
                    GrupoId = r.GetInt32(0),
                    Bodega = r.GetString(1).Trim(),
                    Ubicaciones = r.IsDBNull(2) ? null : r.GetString(2).Trim(),
                    Rack = r.IsDBNull(3) ? "" : r.GetString(3).Trim(),
                    Lado = r.IsDBNull(4) ? "" : r.GetString(4).Trim(),
                    Altura = r.IsDBNull(5) ? "" : r.GetString(5).Trim(),
                    Ubicacion = r.IsDBNull(6) ? "" : r.GetString(6).Trim(),
                    GrupoNombre = r.GetString(7).Trim()
                });
            }

            return lista;
        }

        public async Task<List<ItemPhystag>> ObtenerItemsPorFiltroAsync(
            string bodega,
            string rack,
            string lado,
            string altura,
            string ubicacion)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p.tg_cmpy,
                    p.tg_ware,
                    p.tg_tag,
                    p.tg_part,
                    p.tg_prod,
                    p.tg_bin,
            ";

            if (bodega == "11")
            {
                sql += @"
                    SUBSTR(p.tg_bin,1,1),
                    '',
                    SUBSTR(p.tg_bin,2,1),
                    SUBSTR(p.tg_bin,3,2),
                ";
            }
            else
            {
                sql += @"
                    SUBSTR(p.tg_bin,1,2),
                    SUBSTR(p.tg_bin,3,1),
                    SUBSTR(p.tg_bin,4,1),
                    SUBSTR(p.tg_bin,5,2),
                ";
            }

            sql += @"
                    p.tg_lot,
                    p.tg_desc,
                    p.tg_stku,
                    p.tg_cost,
                    p.tg_count
                FROM phystag p
                WHERE p.tg_cmpy = 'RE'
                  AND p.tg_ware = ?
            ";

            if (rack != "")
                sql += bodega == "11"
                    ? " AND SUBSTR(p.tg_bin,1,1) = ?"
                    : " AND SUBSTR(p.tg_bin,1,2) = ?";

            if (bodega == "13M" && lado != "")
                sql += " AND SUBSTR(p.tg_bin,3,1) = ?";

            if (altura != "")
                sql += bodega == "11"
                    ? " AND SUBSTR(p.tg_bin,2,1) = ?"
                    : " AND SUBSTR(p.tg_bin,4,1) = ?";

            if (ubicacion != "")
                sql += bodega == "11"
                    ? " AND SUBSTR(p.tg_bin,3,2) = ?"
                    : " AND SUBSTR(p.tg_bin,5,2) = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("bodega", bodega));

            if (rack != "") cmd.Parameters.Add(new DB2Parameter("rack", rack));
            if (bodega == "13M" && lado != "") cmd.Parameters.Add(new DB2Parameter("lado", lado));
            if (altura != "") cmd.Parameters.Add(new DB2Parameter("altura", altura));
            if (ubicacion != "") cmd.Parameters.Add(new DB2Parameter("ubicacion", ubicacion));

            var lista = new List<ItemPhystag>();

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ItemPhystag
                {
                    Cmpy = r.GetString(0).Trim(),
                    Bodega = r.GetString(1).Trim(),
                    Etiqueta = r.IsDBNull(2) ? "" : r.GetString(2).Trim(),
                    Item = r.GetString(3).Trim(),
                    Prod = r.IsDBNull(4) ? "" : r.GetString(4).Trim(),
                    Ubicacion = r.GetString(5).Trim(),
                    RackPasillo = r.GetString(6).Trim(),
                    Lado = r.GetString(7).Trim(),
                    Altura = r.GetString(8).Trim(),
                    Posicion = r.GetString(9).Trim(),
                    Lote = r.IsDBNull(10) ? "" : r.GetString(10).Trim(),
                    Descripcion = r.GetString(11).Trim(),
                    Udm = r.IsDBNull(12) ? "" : r.GetString(12).Trim(),
                    Costo = r.GetDecimal(13),
                    CantidadSistema = r.GetDecimal(14)
                });
            }

            return lista;
        }

        public async Task<List<ItemPhystag>> ObtenerItemsPorUbicacionExactaAsync(
            string bodega,
            string ubicaciones)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p.tg_cmpy,
                    p.tg_ware,
                    p.tg_tag,
                    p.tg_part,
                    p.tg_prod,
                    p.tg_bin,
            ";

            if (bodega == "11")
            {
                sql += @"
                    SUBSTR(p.tg_bin,1,1),
                    '',
                    SUBSTR(p.tg_bin,2,1),
                    SUBSTR(p.tg_bin,3,2),
                ";
            }
            else
            {
                sql += @"
                    SUBSTR(p.tg_bin,1,2),
                    SUBSTR(p.tg_bin,3,1),
                    SUBSTR(p.tg_bin,4,1),
                    SUBSTR(p.tg_bin,5,2),
                ";
            }

            sql += @"
                    p.tg_lot,
                    p.tg_desc,
                    p.tg_stku,
                    p.tg_cost,
                    p.tg_count
                FROM phystag p
                WHERE p.tg_cmpy = 'RE'
                  AND p.tg_ware = ?
                  AND p.tg_bin = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("bodega", bodega));
            cmd.Parameters.Add(new DB2Parameter("ubicaciones", ubicaciones));

            var lista = new List<ItemPhystag>();

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ItemPhystag
                {
                    Cmpy = r.GetString(0).Trim(),
                    Bodega = r.GetString(1).Trim(),
                    Etiqueta = r.IsDBNull(2) ? "" : r.GetString(2).Trim(),
                    Item = r.GetString(3).Trim(),
                    Prod = r.IsDBNull(4) ? "" : r.GetString(4).Trim(),
                    Ubicacion = r.GetString(5).Trim(),
                    RackPasillo = r.GetString(6).Trim(),
                    Lado = r.GetString(7).Trim(),
                    Altura = r.GetString(8).Trim(),
                    Posicion = r.GetString(9).Trim(),
                    Lote = r.IsDBNull(10) ? "" : r.GetString(10).Trim(),
                    Descripcion = r.GetString(11).Trim(),
                    Udm = r.IsDBNull(12) ? "" : r.GetString(12).Trim(),
                    Costo = r.GetDecimal(13),
                    CantidadSistema = r.GetDecimal(14)
                });
            }

            return lista;
        }

        public async Task<bool> ExisteEnInventarioAsync(string bodega, string ubicaciones)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT FIRST 1 1
                FROM phystag
                WHERE tg_cmpy = 'RE'
                  AND tg_ware = ?
                  AND tg_bin = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("bodega", bodega));
            cmd.Parameters.Add(new DB2Parameter("ubicaciones", ubicaciones));

            return await cmd.ExecuteScalarAsync() != null;
        }

        public async Task<List<(string Id, string Descripcion)>> ObtenerBodegasAsync()
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                SELECT war_id, war_desc
                FROM warehous
                WHERE war_cmpy = 'RE'
                  AND war_id IN ('11','13M')
                ORDER BY war_id
            ";

            using var cmd = new DB2Command(sql, conn);

            var lista = new List<(string, string)>();
            using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
                lista.Add((r.GetString(0).Trim(), r.GetString(1).Trim()));

            return lista;
        }

        public async Task AgregarAsync(GrupoUbicacion u)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO grupo_ubicacion
                (gu_grupo_id, gu_bodega, gu_ubicacion, gu_rack, gu_lado, gu_altura, gu_posicion)
                VALUES (?,?,?,?,?,?,?)
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", u.GrupoId));
            cmd.Parameters.Add(new DB2Parameter("bodega", u.Bodega));
            cmd.Parameters.Add(new DB2Parameter("ubicaciones", (object?)u.Ubicaciones ?? System.DBNull.Value));
            cmd.Parameters.Add(new DB2Parameter("rack", u.Rack));
            cmd.Parameters.Add(new DB2Parameter("lado", u.Lado));
            cmd.Parameters.Add(new DB2Parameter("altura", u.Altura));
            cmd.Parameters.Add(new DB2Parameter("ubicacion", u.Ubicacion));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ItemPhystag>> ObtenerItemsPorCodigoItemAsync(string bodega, string codigoItem, string? lote = null)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p.tg_cmpy,
                    p.tg_ware,
                    p.tg_tag,
                    p.tg_part,
                    p.tg_prod,
                    p.tg_bin,
            ";

            if (bodega == "11")
            {
                sql += @"
                    SUBSTR(p.tg_bin,1,1),
                    '',
                    SUBSTR(p.tg_bin,2,1),
                    SUBSTR(p.tg_bin,3,2),
                ";
            }
            else
            {
                sql += @"
                    SUBSTR(p.tg_bin,1,2),
                    SUBSTR(p.tg_bin,3,1),
                    SUBSTR(p.tg_bin,4,1),
                    SUBSTR(p.tg_bin,5,2),
                ";
            }

            sql += @"
                    p.tg_lot,
                    p.tg_desc,
                    p.tg_stku,
                    p.tg_cost,
                    p.tg_count
                FROM phystag p
                WHERE p.tg_cmpy = 'RE'
                  AND p.tg_ware = ?
                  AND UPPER(p.tg_part) = UPPER(?)
            ";

            if (!string.IsNullOrWhiteSpace(lote))
                sql += " AND UPPER(p.tg_lot) = UPPER(?)";

            sql += " ORDER BY p.tg_bin, p.tg_lot";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("bodega", bodega));
            cmd.Parameters.Add(new DB2Parameter("item", codigoItem.Trim().ToUpper()));

            if (!string.IsNullOrWhiteSpace(lote))
                cmd.Parameters.Add(new DB2Parameter("lote", lote.Trim().ToUpper()));

            var lista = new List<ItemPhystag>();

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ItemPhystag
                {
                    Cmpy = r.GetString(0).Trim(),
                    Bodega = r.GetString(1).Trim(),
                    Etiqueta = r.IsDBNull(2) ? "" : r.GetString(2).Trim(),
                    Item = r.GetString(3).Trim(),
                    Prod = r.IsDBNull(4) ? "" : r.GetString(4).Trim(),
                    Ubicacion = r.GetString(5).Trim(),
                    RackPasillo = r.GetString(6).Trim(),
                    Lado = r.GetString(7).Trim(),
                    Altura = r.GetString(8).Trim(),
                    Posicion = r.GetString(9).Trim(),
                    Lote = r.IsDBNull(10) ? "" : r.GetString(10).Trim(),
                    Descripcion = r.GetString(11).Trim(),
                    Udm = r.IsDBNull(12) ? "" : r.GetString(12).Trim(),
                    Costo = r.GetDecimal(13),
                    CantidadSistema = r.GetDecimal(14)
                });
            }

            return lista;
        }

        public async Task EliminarAsync(
            int grupoId,
            string bodega,
            string rack,
            string lado,
            string altura,
            string ubicacion)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"
                DELETE FROM grupo_ubicacion
                WHERE gu_grupo_id = ?
                  AND gu_bodega = ?
                  AND gu_rack = ?
                  AND gu_lado = ?
                  AND gu_altura = ?
                  AND gu_posicion = ?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", grupoId));
            cmd.Parameters.Add(new DB2Parameter("bodega", bodega));
            cmd.Parameters.Add(new DB2Parameter("rack", rack));
            cmd.Parameters.Add(new DB2Parameter("lado", lado));
            cmd.Parameters.Add(new DB2Parameter("altura", altura));
            cmd.Parameters.Add(new DB2Parameter("ubicacion", ubicacion));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
