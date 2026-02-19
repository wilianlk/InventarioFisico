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
                    c.operacion_id,
                    c.numero_conteo,
                    d.codigo_item,
                    d.prod,
                    d.descripcion,
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
                    d.cantidad_final,
                    d.no_encontrado,
                    g.gc_nombre
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d ON d.cabecera_id = c.id
                INNER JOIN grupo_conteo g ON g.gc_id = d.grupo_id
                WHERE c.estado IN ('PENDIENTE','FINALIZADA')
                ORDER BY d.codigo_item, d.lote, d.ubicacion
            ";

            using var cmd = new DB2Command(sql, conn);
            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                var key = string.Join("|",
                    T(r.GetString(3)),
                    r.IsDBNull(8) ? "" : T(r.GetString(8)),
                    T(r.GetString(9)),
                    T(r.GetString(19))
                );

                if (!dict.TryGetValue(key, out var row))
                {
                    row = new ConsolidadoRowTemp
                    {
                        OperacionId = r.GetInt32(1),
                        CodigoItem = T(r.GetString(3)),
                        Prod = r.IsDBNull(4) ? null : T(r.GetString(4)),
                        Descripcion = r.IsDBNull(5) ? null : T(r.GetString(5)),
                        Udm = r.IsDBNull(6) ? null : T(r.GetString(6)),
                        Etiqueta = r.IsDBNull(7) ? null : T(r.GetString(7)),
                        Lote = r.IsDBNull(8) ? null : T(r.GetString(8)),
                        Ubicacion = T(r.GetString(9)),
                        Bodega = r.IsDBNull(10) ? null : T(r.GetString(10)),
                        Cmpy = r.IsDBNull(11) ? null : T(r.GetString(11)),
                        Costo = r.IsDBNull(12) ? null : r.GetDecimal(12),
                        CantidadSistema = r.IsDBNull(13) ? null : r.GetDecimal(13),
                        CantidadFinal = r.IsDBNull(17) ? null : r.GetDecimal(17),
                        NoEncontrado = !r.IsDBNull(18) && r.GetInt16(18) == 1,
                        NombreGrupo = T(r.GetString(19))
                    };

                    dict.Add(key, row);
                }

                row.Bloques.Add(r.GetInt32(0));

                var n = r.GetInt32(2);
                if (n == 1 && !r.IsDBNull(14)) row.Conteo1 = r.GetDecimal(14);
                if (n == 2 && !r.IsDBNull(15)) row.Conteo2 = r.GetDecimal(15);
                if (n == 3 && !r.IsDBNull(16)) row.Conteo3 = r.GetDecimal(16);
            }

            var result = new List<dynamic>();
            foreach (var v in dict.Values)
            {
                result.Add(new
                {
                    operacionId = v.OperacionId,
                    id = new { bloques = new List<int>(v.Bloques) },
                    codigoItem = v.CodigoItem,
                    prod = v.Prod,
                    descripcion = v.Descripcion,
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
                    cantidadFinal = v.CantidadFinal,
                    noEncontrado = v.NoEncontrado,
                    grupo = v.NombreGrupo
                });
            }

            return result;
        }

        public async Task<int> InsertarCabeceraDesdeConteoAsync(int operacionId, int grupoId, int numeroConteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id FROM consolidacion_cabecera
                WHERE operacion_id=? AND grupo_id=? AND numero_conteo=?
            ";

            using (var cmd = new DB2Command(sql, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
                cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
                cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

                var id = await cmd.ExecuteScalarAsync();
                if (id != null) return int.Parse(id.ToString());
            }

            var insert = @"
                INSERT INTO consolidacion_cabecera
                (operacion_id,grupo_id,numero_conteo,estado,fecha_creacion)
                VALUES (?,?,?,'PENDIENTE',CURRENT YEAR TO SECOND)
            ";

            using (var cmd = new DB2Command(insert, conn))
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

        public async Task InsertarDetalleDesdeConteoAsync(int cabeceraId, int operacionId, int grupoId, int numeroConteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            string col = numeroConteo switch
            {
                1 => "cantidad_conteo1",
                2 => "cantidad_conteo2",
                3 => "cantidad_conteo3",
                _ => throw new InvalidOperationException("Número de conteo inválido")
            };

            var sql = $@"
                INSERT INTO consolidacion_detalle
                (cabecera_id,operacion_id,grupo_id,codigo_item,prod,descripcion,udm,etiqueta,lote,ubicacion,
                 bodega,cmpy,costo,cantidad_sistema,{col},no_encontrado)
                SELECT
                    ?,operacion_id,grupo_id,codigo_item,prod,descripcion,udm,etiqueta,lote,ubicacion,
                    bodega,cmpy,costo,cantidad_sistema,cantidad_contada,no_encontrado
                FROM operacion_conteo_items
                WHERE operacion_id=? AND grupo_id=? AND numero_conteo=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = cabeceraId });
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
            cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CalcularCantidadFinalAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE consolidacion_detalle
                SET cantidad_final = COALESCE(cantidad_conteo3,cantidad_conteo2,cantidad_conteo1)
                WHERE operacion_id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarcarConsolidacionFinalizadaAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "UPDATE consolidacion_cabecera SET estado='FINALIZADA' WHERE operacion_id=?";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoParaDI81Async(int operacionId)
        {
            var list = new List<Consolidado>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT d.codigo_item,d.lote,d.ubicacion,d.cantidad_final
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d ON d.cabecera_id=c.id
                WHERE c.operacion_id=? AND c.estado='FINALIZADA'
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new Consolidado
                {
                    CodigoProducto = T(r.GetString(0)),
                    Lote = r.IsDBNull(1) ? null : T(r.GetString(1)),
                    Ubicacion = T(r.GetString(2)),
                    CantidadFinal = r.IsDBNull(3) ? null : r.GetDecimal(3)
                });
            }

            return list;
        }

        public async Task<bool> ConsolidacionFinalizadaAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "SELECT COUNT(*) FROM consolidacion_cabecera WHERE operacion_id=? AND estado='FINALIZADA'";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<bool> ConteosCerradosAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "SELECT COUNT(*) FROM operacion_conteo WHERE operacion_id=? AND estado<>'CERRADO'";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            return (int)await cmd.ExecuteScalarAsync() == 0;
        }

        public async Task EliminarPorOperacionAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sqlDetalle = "DELETE FROM consolidacion_detalle WHERE operacion_id=?";
            using (var cmd = new DB2Command(sqlDetalle, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
                await cmd.ExecuteNonQueryAsync();
            }

            var sqlCabecera = "DELETE FROM consolidacion_cabecera WHERE operacion_id=?";
            using (var cmd = new DB2Command(sqlCabecera, conn))
            {
                cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
