using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace InventarioFisico.Repositories
{
    public class OperacionConteoItemsRepository
    {
        private readonly IConnectionStringProvider _provider;

        public OperacionConteoItemsRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public async Task InsertarAsync(List<OperacionConteoItem> items)
        {
            if (items == null || items.Count == 0) return;

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();

            try
            {
                var sql = @"
                    INSERT INTO operacion_conteo_items
                    (operacion_id,grupo_id,numero_conteo,codigo_item,prod,descripcion,udm,
                     etiqueta,lote,costo,cantidad_sistema,cantidad_contada,
                     ubicacion,bodega,cmpy,no_encontrado,oc_id)
                    VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
                ";

                using var cmd = new DB2Command(sql, conn, tx);

                for (int i = 0; i < 17; i++) cmd.Parameters.Add(new DB2Parameter());

                foreach (var it in items)
                {
                    cmd.Parameters[0].Value = it.OperacionId;
                    cmd.Parameters[1].Value = it.GrupoId;
                    cmd.Parameters[2].Value = it.NumeroConteo;
                    cmd.Parameters[3].Value = it.CodigoItem;
                    cmd.Parameters[4].Value = (object?)it.Prod ?? DBNull.Value;
                    cmd.Parameters[5].Value = (object?)it.Descripcion ?? DBNull.Value;
                    cmd.Parameters[6].Value = (object?)it.Udm ?? DBNull.Value;
                    cmd.Parameters[7].Value = (object?)it.Etiqueta ?? DBNull.Value;
                    cmd.Parameters[8].Value = (object?)it.Lote ?? DBNull.Value;
                    cmd.Parameters[9].Value = (object?)it.Costo ?? DBNull.Value;
                    cmd.Parameters[10].Value = (object?)it.CantidadSistema ?? DBNull.Value;
                    cmd.Parameters[11].Value = it.CantidadContada;
                    cmd.Parameters[12].Value = it.Ubicacion;
                    cmd.Parameters[13].Value = (object?)it.Bodega ?? DBNull.Value;
                    cmd.Parameters[14].Value = (object?)it.Cmpy ?? DBNull.Value;
                    cmd.Parameters[15].Value = it.NoEncontrado ? 1 : 0;
                    cmd.Parameters[16].Value = (object?)it.ConteoId ?? DBNull.Value;

                    await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        public async Task<OperacionConteoItem> ObtenerPorIdAsync(int idItem)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id,operacion_id,grupo_id,numero_conteo,codigo_item,prod,
                       descripcion,udm,etiqueta,lote,costo,
                       cantidad_sistema,cantidad_contada,ubicacion,bodega,cmpy,
                       no_encontrado,oc_id
                FROM operacion_conteo_items
                WHERE id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = idItem });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync()) return Map(r);

            return null;
        }

        public async Task<List<OperacionConteoItem>> ObtenerAsync(int operacionId, int grupoId, int numeroConteo)
        {
            var lista = new List<OperacionConteoItem>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id,operacion_id,grupo_id,numero_conteo,codigo_item,prod,
                       descripcion,udm,etiqueta,lote,costo,
                       cantidad_sistema,cantidad_contada,ubicacion,bodega,cmpy,
                       no_encontrado,oc_id
                FROM operacion_conteo_items
                WHERE operacion_id=?
                  AND grupo_id=?
                  AND numero_conteo=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });
            cmd.Parameters.Add(new DB2Parameter { Value = grupoId });
            cmd.Parameters.Add(new DB2Parameter { Value = numeroConteo });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(Map(r));
            }

            return lista;
        }

        public async Task<List<OperacionConteoItem>> ObtenerPorConteoAsync(int conteoId)
        {
            var lista = new List<OperacionConteoItem>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id,operacion_id,grupo_id,numero_conteo,codigo_item,prod,
                       descripcion,udm,etiqueta,lote,costo,
                       cantidad_sistema,cantidad_contada,ubicacion,bodega,cmpy,
                       no_encontrado,oc_id
                FROM operacion_conteo_items
                WHERE oc_id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = conteoId });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(Map(r));
            }

            return lista;
        }

        public async Task<List<OperacionConteoItem>> ObtenerNoEncontradosPorConteoAsync(int conteoId)
        {
            var lista = new List<OperacionConteoItem>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id,operacion_id,grupo_id,numero_conteo,codigo_item,prod,
                       descripcion,udm,etiqueta,lote,costo,
                       cantidad_sistema,cantidad_contada,ubicacion,bodega,cmpy,
                       no_encontrado,oc_id
                FROM operacion_conteo_items
                WHERE oc_id=?
                  AND no_encontrado=1
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = conteoId });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(Map(r));
            }

            return lista;
        }

        public async Task<List<OperacionConteoItem>> ObtenerPorOperacionAsync(int operacionId)
        {
            var lista = new List<OperacionConteoItem>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT id,operacion_id,grupo_id,numero_conteo,codigo_item,prod,
                       descripcion,udm,etiqueta,lote,costo,
                       cantidad_sistema,cantidad_contada,ubicacion,bodega,cmpy,
                       no_encontrado,oc_id
                FROM operacion_conteo_items
                WHERE operacion_id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            using DbDataReader r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(Map(r));
            }

            return lista;
        }

        public async Task<Dictionary<int, (int TotalItems, int ItemsContados)>> ObtenerAvancePorOperacionAsync()
        {
            var resultado = new Dictionary<int, (int TotalItems, int ItemsContados)>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT operacion_id,
                       COUNT(*) AS total_items,
                       SUM(CASE WHEN cantidad_contada > 0 THEN 1 ELSE 0 END) AS items_contados
                FROM operacion_conteo_items
                GROUP BY operacion_id
            ";

            using var cmd = new DB2Command(sql, conn);
            using DbDataReader r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                var operacionId = r.GetInt32(0);
                var totalItems = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
                var itemsContados = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));

                resultado[operacionId] = (totalItems, itemsContados);
            }

            return resultado;
        }

        public async Task<Dictionary<int, (int TotalItems, int ItemsContados)>> ObtenerAvancePorConteoAsync()
        {
            var resultado = new Dictionary<int, (int TotalItems, int ItemsContados)>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                SELECT oc.id AS conteo_id,
                       COUNT(oci.id) AS total_items,
                       SUM(CASE WHEN oci.cantidad_contada > 0 THEN 1 ELSE 0 END) AS items_contados
                FROM operacion_conteo oc
                LEFT JOIN operacion_conteo_items oci
                  ON oci.oc_id = oc.id
                GROUP BY oc.id
            ";

            using var cmd = new DB2Command(sql, conn);
            using DbDataReader r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                var conteoId = r.GetInt32(0);
                var totalItems = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
                var itemsContados = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));

                resultado[conteoId] = (totalItems, itemsContados);
            }

            return resultado;
        }

        public async Task EliminarPorOperacionAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                DELETE FROM operacion_conteo_items
                WHERE operacion_id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = operacionId });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ActualizarCantidadContadaAsync(int idItem, int cantidadContada)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE operacion_conteo_items
                SET cantidad_contada=?
                WHERE id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = cantidadContada });
            cmd.Parameters.Add(new DB2Parameter { Value = idItem });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ActualizarNoEncontradoAsync(int idItem, bool noEncontrado)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE operacion_conteo_items
                SET no_encontrado=?
                WHERE id=?
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = noEncontrado ? 1 : 0 });
            cmd.Parameters.Add(new DB2Parameter { Value = idItem });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> ActualizarNoEncontradoPorCodigoAsync(int conteoId, string codigoItem, bool noEncontrado)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE operacion_conteo_items
                SET no_encontrado=?
                WHERE oc_id=?
                  AND TRIM(codigo_item)=TRIM(?)
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter { Value = noEncontrado ? 1 : 0 });
            cmd.Parameters.Add(new DB2Parameter { Value = conteoId });
            cmd.Parameters.Add(new DB2Parameter { Value = codigoItem });

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task ActualizarConteoIdFaltantesAsync()
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                UPDATE operacion_conteo_items oci
                SET oc_id = (
                    SELECT oc.id
                    FROM operacion_conteo oc
                    WHERE oc.operacion_id = oci.operacion_id
                      AND oc.grupo_id = oci.grupo_id
                      AND oc.numero_conteo = oci.numero_conteo
                    FIRST 1
                )
                WHERE oci.oc_id IS NULL
            ";

            using var cmd = new DB2Command(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static OperacionConteoItem Map(DbDataReader r)
        {
            return new OperacionConteoItem
            {
                Id = r.GetInt32(0),
                OperacionId = r.GetInt32(1),
                GrupoId = r.GetInt32(2),
                NumeroConteo = r.GetInt32(3),
                CodigoItem = r.IsDBNull(4) ? "" : r.GetString(4),
                Prod = r.IsDBNull(5) ? null : r.GetString(5),
                Descripcion = r.IsDBNull(6) ? null : r.GetString(6),
                Udm = r.IsDBNull(7) ? null : r.GetString(7),
                Etiqueta = r.IsDBNull(8) ? null : r.GetString(8),
                Lote = r.IsDBNull(9) ? null : r.GetString(9),
                Costo = r.IsDBNull(10) ? 0m : r.GetDecimal(10),
                CantidadSistema = r.IsDBNull(11) ? 0m : r.GetDecimal(11),
                CantidadContada = r.IsDBNull(12) ? 0 : r.GetInt32(12),
                Ubicacion = r.IsDBNull(13) ? "" : r.GetString(13),
                Bodega = r.IsDBNull(14) ? null : r.GetString(14),
                Cmpy = r.IsDBNull(15) ? null : r.GetString(15),
                NoEncontrado = !r.IsDBNull(16) && r.GetInt16(16) == 1,
                ConteoId = r.IsDBNull(17) ? null : r.GetInt32(17)
            };
        }
    }
}
