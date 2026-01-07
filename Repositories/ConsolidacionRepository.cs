using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;

namespace InventarioFisico.Repositories
{
    public class ConsolidacionRepository
    {
        private readonly IConnectionStringProvider _provider;

        public ConsolidacionRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        private static string T(string s) => (s ?? "").Trim();

        public async Task<List<dynamic>> ObtenerConteosFinalizadosAsync()
        {
            var dict = new Dictionary<string, ConsolidadoRowTemp>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    c.id,
                    c.numero_conteo,
                    d.codigo_item,
                    d.prod,
                    d.udm,
                    d.etiqueta,
                    d.lote,
                    d.ubicacion,
                    d.bodega,
                    d.cmpy,
                    d.costo,
                    d.cantidad_sistema,
                    d.cantidad_conteo1,
                    d.cantidad_conteo2,
                    d.cantidad_conteo3,
                    d.cantidad_final
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d
                    ON d.cabecera_id = c.id
                WHERE c.estado = 'PENDIENTE'
                ORDER BY d.codigo_item, d.lote, d.ubicacion
            ";

            using var cmd = new DB2Command(sql, conn);
            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                var key = string.Join("|",
                    T(r.GetString(2)),
                    r.IsDBNull(6) ? "" : T(r.GetString(6)),
                    T(r.GetString(7)),
                    r.IsDBNull(8) ? "" : T(r.GetString(8)),
                    r.IsDBNull(9) ? "" : T(r.GetString(9))
                );

                if (!dict.TryGetValue(key, out var row))
                {
                    row = new ConsolidadoRowTemp
                    {
                        CodigoItem = T(r.GetString(2)),
                        Prod = r.IsDBNull(3) ? null : T(r.GetString(3)),
                        Udm = r.IsDBNull(4) ? null : T(r.GetString(4)),
                        Etiqueta = r.IsDBNull(5) ? null : T(r.GetString(5)),
                        Lote = r.IsDBNull(6) ? null : T(r.GetString(6)),
                        Ubicacion = T(r.GetString(7)),
                        Bodega = r.IsDBNull(8) ? null : T(r.GetString(8)),
                        Cmpy = r.IsDBNull(9) ? null : T(r.GetString(9)),
                        Costo = r.IsDBNull(10) ? null : r.GetDecimal(10),
                        CantidadSistema = r.IsDBNull(11) ? null : r.GetDecimal(11),
                        CantidadFinal = r.IsDBNull(15) ? null : r.GetDecimal(15)
                    };

                    dict.Add(key, row);
                }

                row.Bloques.Add(r.GetInt32(0));

                var n = r.GetInt32(1);
                if (n == 1 && !r.IsDBNull(12)) row.Conteo1 = r.GetDecimal(12);
                if (n == 2 && !r.IsDBNull(13)) row.Conteo2 = r.GetDecimal(13);
                if (n == 3 && !r.IsDBNull(14)) row.Conteo3 = r.GetDecimal(14);
            }

            var result = new List<dynamic>();

            foreach (var v in dict.Values)
            {
                result.Add(new
                {
                    id = new { bloques = new List<int>(v.Bloques) },
                    codigoItem = v.CodigoItem,
                    prod = v.Prod,
                    udm = v.Udm,
                    etiqueta = v.Etiqueta,
                    lote = v.Lote,
                    ubicacion = v.Ubicacion,
                    bodega = v.Bodega,
                    cmpy = v.Cmpy,
                    costo = v.Costo,
                    cantidadSistema = v.CantidadSistema,
                    cantidadConteo1 = v.Conteo1,
                    cantidadConteo2 = v.Conteo2,
                    cantidadConteo3 = v.Conteo3,
                    cantidadFinal = v.CantidadFinal
                });
            }

            return result;
        }

        public async Task<int> InsertarCabeceraDesdeConteoAsync(int operacionId, int grupoId, int numeroConteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var selectSql = @"
                SELECT id
                FROM consolidacion_cabecera
                WHERE operacion_id=?
                  AND grupo_id=?
                  AND numero_conteo=?
            ";

            using (var cmd = new DB2Command(selectSql, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
                cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
                cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });
                var id = await cmd.ExecuteScalarAsync();
                if (id != null) return int.Parse(id.ToString());
            }

            var insertSql = @"
                INSERT INTO consolidacion_cabecera
                (operacion_id,grupo_id,numero_conteo,estado,fecha_creacion)
                VALUES(?, ?, ?, 'PENDIENTE', CURRENT YEAR TO SECOND)
            ";

            using (var cmd = new DB2Command(insertSql, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
                cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
                cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });
                await cmd.ExecuteNonQueryAsync();
            }

            using var cmdGet = new DB2Command(
                "SELECT DBINFO('sqlca.sqlerrd1') FROM systables WHERE tabid=1", conn);

            return int.Parse((await cmdGet.ExecuteScalarAsync()).ToString());
        }

        public async Task InsertarDetalleDesdeConteoAsync(
            int cabeceraId,
            int operacionId,
            int grupoId,
            int numeroConteo
        )
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            string columnaConteo = numeroConteo switch
            {
                1 => "cantidad_conteo1",
                2 => "cantidad_conteo2",
                3 => "cantidad_conteo3",
                _ => throw new InvalidOperationException("Número de conteo inválido")
            };

            var selectSql = @"
                SELECT
                    codigo_item,
                    prod,
                    udm,
                    etiqueta,
                    lote,
                    ubicacion,
                    bodega,
                    cmpy,
                    costo,
                    cantidad_sistema,
                    cantidad_contada
                FROM operacion_conteo_items
                WHERE operacion_id = ?
                  AND grupo_id = ?
                  AND numero_conteo = ?
            ";

            using var selectCmd = new DB2Command(selectSql, conn);
            selectCmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            selectCmd.Parameters.Add(new DB2Parameter { Value = grupoId });
            selectCmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var insertSql = $@"
                    INSERT INTO consolidacion_detalle
                    (
                        cabecera_id,
                        codigo_item,
                        prod,
                        udm,
                        etiqueta,
                        lote,
                        ubicacion,
                        bodega,
                        cmpy,
                        costo,
                        cantidad_sistema,
                        {columnaConteo}
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ";

                using var insertCmd = new DB2Command(insertSql, conn);

                insertCmd.Parameters.Add(new DB2Parameter { Value = cabeceraId });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.GetString(0) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(1) ? null : reader.GetString(1) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(2) ? null : reader.GetString(2) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(3) ? null : reader.GetString(3) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(4) ? null : reader.GetString(4) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.GetString(5) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(6) ? null : reader.GetString(6) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(7) ? null : reader.GetString(7) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(8) ? null : reader.GetDecimal(8) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.IsDBNull(9) ? null : reader.GetDecimal(9) });
                insertCmd.Parameters.Add(new DB2Parameter { Value = reader.GetInt32(10) });

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> ConteosCerradosAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT COUNT(*)
                FROM operacion_conteo
                WHERE operacion_id=?
                  AND estado<>'CERRADO'
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            return (int)await cmd.ExecuteScalarAsync() == 0;
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoParaDI81Async(int operacionId)
        {
            var lista = new List<Consolidado>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    d.codigo_item,
                    d.lote,
                    d.ubicacion,
                    d.cantidad_final
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d
                    ON d.cabecera_id = c.id
                WHERE c.operacion_id = ?
                  AND c.estado = 'FINALIZADA'
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new Consolidado
                {
                    CodigoProducto = T(r.GetString(0)),
                    Lote = r.IsDBNull(1) ? null : T(r.GetString(1)),
                    Ubicacion = T(r.GetString(2)),
                    CantidadFinal = r.IsDBNull(3) ? null : r.GetDecimal(3)
                });
            }

            return lista;
        }

        public async Task BloquearOperacionAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE operaciones_inventario
                SET estado='FINALIZADA'
                WHERE id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ConsolidacionFinalizadaAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "SELECT estado FROM operaciones_inventario WHERE id=?";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            var estado = await cmd.ExecuteScalarAsync();
            return estado != null && T(estado.ToString()) == "FINALIZADA";
        }
    }

    class ConsolidadoRowTemp
    {
        public HashSet<int> Bloques { get; set; } = new();
        public string CodigoItem { get; set; }
        public string Prod { get; set; }
        public string Udm { get; set; }
        public string Etiqueta { get; set; }
        public string Lote { get; set; }
        public string Ubicacion { get; set; }
        public string Bodega { get; set; }
        public string Cmpy { get; set; }
        public decimal? Costo { get; set; }
        public decimal? CantidadSistema { get; set; }
        public decimal? Conteo1 { get; set; }
        public decimal? Conteo2 { get; set; }
        public decimal? Conteo3 { get; set; }
        public decimal? CantidadFinal { get; set; }
    }
}
