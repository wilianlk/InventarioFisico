using System;
using System.Collections.Generic;
using System.Linq;
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
        private static decimal? D(DB2DataReader r, int i) =>
            r.IsDBNull(i) ? null : Convert.ToDecimal(r.GetValue(i));
        private static string BuildDi81Key(string codigoProducto, string? lote, string ubicacion) =>
            $"{codigoProducto}|{lote ?? ""}|{ubicacion}";
        private static decimal? ResolverCantidadFinalDesdeConteos(ConsolidadoRowTemp row, decimal? fallback = null)
        {
            foreach (var entry in row.ConteosPorNumero.OrderByDescending(x => x.Key))
            {
                var cantidad = entry.Value?.Cantidad;
                if (cantidad.HasValue)
                    return cantidad.Value;
            }

            return fallback ?? row.CantidadFinal;
        }

        private static void SetConteoCantidad(Consolidado row, int numeroConteo, decimal? cantidad)
        {
            if (numeroConteo == 1) row.CantidadConteo1 ??= cantidad;
            if (numeroConteo == 2) row.CantidadConteo2 ??= cantidad;
            if (numeroConteo == 3) row.CantidadConteo3 ??= cantidad;

            if (!cantidad.HasValue)
                return;

            if (!row.NumeroConteoFinal.HasValue || numeroConteo >= row.NumeroConteoFinal.Value)
            {
                row.NumeroConteoFinal = numeroConteo;
                row.CantidadFinal = cantidad;
            }
        }

        private static List<Consolidado> ToConsolidadoList(Dictionary<string, Consolidado> byKey)
        {
            var result = new List<Consolidado>(byKey.Count);
            foreach (var row in byKey.Values)
            {
                row.CantidadFinal ??= row.CantidadConteo3 ?? row.CantidadConteo2 ?? row.CantidadConteo1;
                result.Add(row);
            }

            return result;
        }

        public async Task<List<ConteoFinalizadoDto>> ObtenerConteosFinalizadosAsync()
        {
            var dict = new Dictionary<string, ConsolidadoRowTemp>();
            static string BuildKey(int operacionId, string codigoItem, string? lote, string ubicacion, string grupo) =>
                string.Join("|", operacionId.ToString(), codigoItem, lote ?? "", ubicacion, grupo);

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
                    g.gc_nombre,
                    o.estado,
                    COALESCE(d.cantidad_final,d.cantidad_conteo3,d.cantidad_conteo2,d.cantidad_conteo1) AS cantidad_conteo
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d ON d.cabecera_id = c.id
                INNER JOIN grupo_conteo g ON g.gc_id = d.grupo_id
                INNER JOIN operaciones_inventario o ON o.id = c.operacion_id
                WHERE UPPER(TRIM(o.estado)) IN ('CONSOLIDADA', 'FINALIZADA')
                ORDER BY d.codigo_item, d.lote, d.ubicacion
            ";

            using var cmd = new DB2Command(sql, conn);
            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                var operacionId = r.GetInt32(1);
                var numeroConteo = r.GetInt32(2);
                var codigoItem = T(r.GetString(3));
                var lote = r.IsDBNull(8) ? null : T(r.GetString(8));
                var ubicacion = T(r.GetString(9));
                var grupo = T(r.GetString(19));
                var estadoOperacion = r.IsDBNull(20) ? string.Empty : T(r.GetString(20));
                var cantidadConteo = D(r, 21);

                var key = BuildKey(operacionId, codigoItem, lote, ubicacion, grupo);

                if (!dict.TryGetValue(key, out var row))
                {
                    row = new ConsolidadoRowTemp
                    {
                        OperacionId = operacionId,
                        EstadoOperacion = estadoOperacion,
                        NumeroConteo = numeroConteo,
                        CodigoItem = codigoItem,
                        Prod = r.IsDBNull(4) ? null : T(r.GetString(4)),
                        Descripcion = r.IsDBNull(5) ? null : T(r.GetString(5)),
                        Udm = r.IsDBNull(6) ? null : T(r.GetString(6)),
                        Etiqueta = r.IsDBNull(7) ? null : T(r.GetString(7)),
                        Lote = lote,
                        Ubicacion = ubicacion,
                        Bodega = r.IsDBNull(10) ? null : T(r.GetString(10)),
                        Cmpy = r.IsDBNull(11) ? null : T(r.GetString(11)),
                        Costo = D(r, 12),
                        CantidadSistema = D(r, 13),
                        CantidadFinal = null,
                        NoEncontrado = !r.IsDBNull(18) && r.GetInt16(18) == 1,
                        NombreGrupo = grupo
                    };

                    dict.Add(key, row);
                }
                else
                {
                    row.NumeroConteo = Math.Max(row.NumeroConteo, numeroConteo);
                    row.NoEncontrado = row.NoEncontrado || (!r.IsDBNull(18) && r.GetInt16(18) == 1);
                    if (string.IsNullOrWhiteSpace(row.EstadoOperacion) && !string.IsNullOrWhiteSpace(estadoOperacion))
                        row.EstadoOperacion = estadoOperacion;
                }

                row.Bloques.Add(r.GetInt32(0));

                var n = numeroConteo;
                if (n == 1) row.Conteo1 ??= cantidadConteo ?? D(r, 14);
                if (n == 2) row.Conteo2 ??= cantidadConteo ?? D(r, 15);
                if (n == 3) row.Conteo3 ??= cantidadConteo ?? D(r, 16);

                if (!row.ConteosPorNumero.TryGetValue(n, out var conteo))
                {
                    row.ConteosPorNumero[n] = new ConteoValorDto
                    {
                        NumeroConteo = n,
                        Cantidad = cantidadConteo,
                        Grupo = grupo
                    };
                }
                else
                {
                    if (!conteo.Cantidad.HasValue && cantidadConteo.HasValue)
                        conteo.Cantidad = cantidadConteo;

                    if (string.IsNullOrWhiteSpace(conteo.Grupo) && !string.IsNullOrWhiteSpace(grupo))
                        conteo.Grupo = grupo;
                }

                row.CantidadFinal = ResolverCantidadFinalDesdeConteos(row, D(r, 17));
            }

            var result = new List<ConteoFinalizadoDto>();
            foreach (var v in dict.Values)
            {
                var conteos = v.ConteosPorNumero.Values
                    .OrderBy(x => x.NumeroConteo)
                    .ToList();

                if (conteos.Count == 0)
                {
                    if (v.Conteo1.HasValue)
                    {
                        conteos.Add(new ConteoValorDto
                        {
                            NumeroConteo = 1,
                            Cantidad = v.Conteo1,
                            Grupo = v.NombreGrupo
                        });
                    }

                    if (v.Conteo2.HasValue)
                    {
                        conteos.Add(new ConteoValorDto
                        {
                            NumeroConteo = 2,
                            Cantidad = v.Conteo2,
                            Grupo = v.NombreGrupo
                        });
                    }

                    if (v.Conteo3.HasValue)
                    {
                        conteos.Add(new ConteoValorDto
                        {
                            NumeroConteo = 3,
                            Cantidad = v.Conteo3,
                            Grupo = v.NombreGrupo
                        });
                    }
                }

                var cantidadConteo1 = conteos
                    .Where(x => x.NumeroConteo == 1)
                    .Select(x => x.Cantidad)
                    .FirstOrDefault(x => x.HasValue)
                    ?? v.Conteo1;

                var cantidadConteo2 = conteos
                    .Where(x => x.NumeroConteo == 2)
                    .Select(x => x.Cantidad)
                    .FirstOrDefault(x => x.HasValue)
                    ?? v.Conteo2;

                var cantidadConteo3 = conteos
                    .Where(x => x.NumeroConteo >= 3)
                    .OrderByDescending(x => x.NumeroConteo)
                    .Select(x => x.Cantidad)
                    .FirstOrDefault(x => x.HasValue)
                    ?? v.Conteo3;

                var cantidadFinal = cantidadConteo3
                    ?? cantidadConteo2
                    ?? cantidadConteo1
                    ?? v.CantidadFinal;

                result.Add(new ConteoFinalizadoDto
                {
                    OperacionId = v.OperacionId,
                    EstadoOperacion = v.EstadoOperacion,
                    Id = new ConteoFinalizadoIdDto
                    {
                        Bloques = new List<int>(v.Bloques)
                    },
                    NumeroConteo = v.NumeroConteo,
                    CodigoItem = v.CodigoItem,
                    Prod = v.Prod,
                    Descripcion = v.Descripcion,
                    Udm = v.Udm,
                    Etiqueta = v.Etiqueta,
                    Lote = v.Lote,
                    Ubicacion = v.Ubicacion,
                    Bodega = v.Bodega,
                    Cmpy = v.Cmpy,
                    Costo = v.Costo,
                    CantidadSistema = v.CantidadSistema,
                    CantidadConteo1 = cantidadConteo1,
                    CantidadConteo2 = cantidadConteo2,
                    CantidadConteo3 = cantidadConteo3,
                    CantidadFinal = cantidadFinal,
                    NoEncontrado = v.NoEncontrado,
                    Grupo = v.NombreGrupo,
                    Conteos = conteos
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
                _ => "cantidad_final"
            };

            var sql = $@"
                INSERT INTO consolidacion_detalle
                (cabecera_id,operacion_id,grupo_id,codigo_item,prod,descripcion,udm,etiqueta,lote,ubicacion,
                 bodega,cmpy,costo,cantidad_sistema,{col},no_encontrado)
                SELECT
                    {cabeceraId},operacion_id,grupo_id,codigo_item,prod,descripcion,udm,etiqueta,lote,ubicacion,
                    bodega,cmpy,costo,cantidad_sistema,cantidad_contada,no_encontrado
                FROM operacion_conteo_items
                WHERE operacion_id=? AND grupo_id=? AND numero_conteo=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
            cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExisteDetalleConteoAsync(int operacionId, int grupoId, int numeroConteo)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT COUNT(*)
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d ON d.cabecera_id = c.id
                WHERE c.operacion_id=? AND c.grupo_id=? AND c.numero_conteo=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
            cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task CalcularCantidadFinalAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE consolidacion_detalle
                SET cantidad_final = COALESCE(cantidad_conteo3,cantidad_conteo2,cantidad_conteo1,cantidad_final)
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
            var byKey = new Dictionary<string, Consolidado>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT d.codigo_item,d.lote,d.ubicacion,c.numero_conteo,d.cantidad_final
                FROM consolidacion_cabecera c
                INNER JOIN consolidacion_detalle d ON d.cabecera_id=c.id
                WHERE c.operacion_id=? AND c.estado='FINALIZADA'
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var codigo = T(r.GetString(0));
                var lote = r.IsDBNull(1) ? null : T(r.GetString(1));
                var ubicacion = T(r.GetString(2));
                var numeroConteo = r.GetInt32(3);
                var cantidad = D(r, 4);
                var key = BuildDi81Key(codigo, lote, ubicacion);

                if (!byKey.TryGetValue(key, out var row))
                {
                    row = new Consolidado
                    {
                        OperacionId = operacionId,
                        CodigoProducto = codigo,
                        Lote = lote,
                        Ubicacion = ubicacion
                    };
                    byKey[key] = row;
                }

                SetConteoCantidad(row, numeroConteo, cantidad);
            }

            return ToConsolidadoList(byKey);
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoFallbackParaDI81Async(int operacionId)
        {
            var byKey = new Dictionary<string, Consolidado>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    i.codigo_item,
                    i.lote,
                    i.ubicacion,
                    oc.numero_conteo,
                    i.cantidad_contada
                FROM operacion_conteo_items i, operacion_conteo oc
                WHERE oc.estado IN ('CERRADO','FINALIZADO','FINALIZADA')
                  AND oc.operacion_id=?
                  AND oc.operacion_id=i.operacion_id
                  AND oc.grupo_id=i.grupo_id
                  AND oc.numero_conteo=i.numero_conteo
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var codigo = T(r.GetString(0));
                var lote = r.IsDBNull(1) ? null : T(r.GetString(1));
                var ubicacion = T(r.GetString(2));
                var numeroConteo = r.GetInt32(3);
                var cantidad = D(r, 4);

                var key = BuildDi81Key(codigo, lote, ubicacion);
                if (!byKey.TryGetValue(key, out var row))
                {
                    row = new Consolidado
                    {
                        OperacionId = operacionId,
                        CodigoProducto = codigo,
                        Lote = lote,
                        Ubicacion = ubicacion
                    };
                    byKey[key] = row;
                }

                SetConteoCantidad(row, numeroConteo, cantidad);
            }

            return ToConsolidadoList(byKey);
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoDesdeConteosFinalizadosAsync(int operacionId)
        {
            var byKey = new Dictionary<string, Consolidado>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            void Upsert(string codigo, string lote, string ubicacion, int numeroConteo, decimal? cantidad)
            {
                if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(ubicacion))
                    return;

                var key = BuildDi81Key(codigo, lote, ubicacion);
                if (!byKey.TryGetValue(key, out var row))
                {
                    row = new Consolidado
                    {
                        OperacionId = operacionId,
                        CodigoProducto = codigo,
                        Lote = lote,
                        Ubicacion = ubicacion
                    };
                    byKey[key] = row;
                }

                SetConteoCantidad(row, numeroConteo, cantidad);
            }

            var sqlDetalle = @"
                SELECT
                    d.codigo_item,
                    d.lote,
                    d.ubicacion,
                    c.numero_conteo,
                    COALESCE(d.cantidad_final,d.cantidad_conteo3,d.cantidad_conteo2,d.cantidad_conteo1)
                FROM consolidacion_cabecera c, consolidacion_detalle d
                WHERE c.id=d.cabecera_id
                  AND c.operacion_id=?
                  AND c.estado IN ('PENDIENTE','FINALIZADA')
            ";

            using (var cmdDetalle = new DB2Command(sqlDetalle, conn))
            {
                cmdDetalle.Parameters.Add(new DB2Parameter { Value = operacionId });
                using var rDetalle = (DB2DataReader)await cmdDetalle.ExecuteReaderAsync();
                while (await rDetalle.ReadAsync())
                {
                    Upsert(
                        T(rDetalle.GetString(0)),
                        rDetalle.IsDBNull(1) ? null : T(rDetalle.GetString(1)),
                        T(rDetalle.GetString(2)),
                        rDetalle.GetInt32(3),
                        D(rDetalle, 4)
                    );
                }
            }

            if (byKey.Count == 0)
            {
                var sqlConteos = @"
                    SELECT
                        i.codigo_item,
                        i.lote,
                        i.ubicacion,
                        oc.numero_conteo,
                        i.cantidad_contada
                    FROM operacion_conteo_items i, operacion_conteo oc
                    WHERE oc.operacion_id=?
                      AND oc.estado IN ('CERRADO','FINALIZADO','FINALIZADA')
                      AND oc.operacion_id=i.operacion_id
                      AND oc.grupo_id=i.grupo_id
                      AND oc.numero_conteo=i.numero_conteo
                ";

                using var cmdConteos = new DB2Command(sqlConteos, conn);
                cmdConteos.Parameters.Add(new DB2Parameter { Value = operacionId });
                using var rConteos = (DB2DataReader)await cmdConteos.ExecuteReaderAsync();
                while (await rConteos.ReadAsync())
                {
                    Upsert(
                        T(rConteos.GetString(0)),
                        rConteos.IsDBNull(1) ? null : T(rConteos.GetString(1)),
                        T(rConteos.GetString(2)),
                        rConteos.GetInt32(3),
                        D(rConteos, 4)
                    );
                }
            }

            return ToConsolidadoList(byKey);
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoDirectoItemsAsync(int operacionId)
        {
            var byKey = new Dictionary<string, Consolidado>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    i.codigo_item,
                    i.lote,
                    i.ubicacion,
                    i.numero_conteo,
                    i.cantidad_contada
                FROM operacion_conteo_items i
                WHERE i.operacion_id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using var r = (DB2DataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var codigo = T(r.GetString(0));
                var lote = r.IsDBNull(1) ? null : T(r.GetString(1));
                var ubicacion = T(r.GetString(2));
                var numeroConteo = r.GetInt32(3);
                var cantidad = D(r, 4) ?? 0m;

                if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(ubicacion))
                    continue;

                var key = BuildDi81Key(codigo, lote, ubicacion);
                if (!byKey.TryGetValue(key, out var row))
                {
                    row = new Consolidado
                    {
                        OperacionId = operacionId,
                        CodigoProducto = codigo,
                        Lote = lote,
                        Ubicacion = ubicacion
                    };
                    byKey[key] = row;
                }

                SetConteoCantidad(row, numeroConteo, cantidad);
            }

            return ToConsolidadoList(byKey);
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

            var sql = @"
                SELECT COUNT(*)
                FROM operacion_conteo
                WHERE operacion_id=?
                  AND UPPER(TRIM(estado)) NOT IN ('CERRADO','FINALIZADO','FINALIZADA')
            ";
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
