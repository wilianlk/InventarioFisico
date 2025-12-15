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

        public async Task<bool> OperacionEstaConsolidadaAsync(int operacionId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT COUNT(*)
                        FROM consolidado_inventario
                        WHERE operacion_id = ?
                          AND (fecha_aprobacion IS NOT NULL OR estado = 'RECONTADO')";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("operacion_id", operacionId));

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task<List<Consolidado>> ObtenerConsolidadoAsync(int operacionId)
        {
            var lista = new List<Consolidado>();
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT codigo_producto, descripcion, lote, ubicacion,
                               cantidad_conteo1, cantidad_conteo2, cantidad_conteo3, cantidad_final, estado,
                               usuario_aprobacion, fecha_aprobacion
                        FROM consolidado_inventario
                        WHERE operacion_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("operacion_id", operacionId));
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new Consolidado
                {
                    CodigoProducto = reader.GetString(0),
                    Descripcion = reader.GetString(1),
                    Lote = reader.GetString(2),
                    Ubicacion = reader.GetString(3),
                    CantidadConteo1 = reader.GetDecimal(4),
                    CantidadConteo2 = reader.GetDecimal(5),
                    CantidadConteo3 = reader.GetDecimal(6),
                    CantidadFinal = reader.GetDecimal(7),
                    Estado = reader.GetString(8),
                    UsuarioAprobacion = reader.GetString(9),
                    FechaAprobacion = reader.GetDateTime(10)
                });
            }
            return lista;
        }

        public async Task AprobarReconteoAsync(Consolidado consolidado)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"UPDATE consolidado_inventario
                        SET cantidad_conteo3 = ?, cantidad_final = ?, estado = 'RECONTADO',
                            usuario_aprobacion = ?, fecha_aprobacion = ?
                        WHERE operacion_id = ? AND codigo_producto = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("cantidad_conteo3", consolidado.CantidadConteo3));
            cmd.Parameters.Add(new DB2Parameter("cantidad_final", consolidado.CantidadFinal));
            cmd.Parameters.Add(new DB2Parameter("usuario_aprobacion", consolidado.UsuarioAprobacion));
            cmd.Parameters.Add(new DB2Parameter("fecha_aprobacion", consolidado.FechaAprobacion));
            cmd.Parameters.Add(new DB2Parameter("operacion_id", consolidado.OperacionId));
            cmd.Parameters.Add(new DB2Parameter("codigo_producto", consolidado.CodigoProducto));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Di81Archivo>> GenerarArchivoDI81Async(int operacionId)
        {
            var lista = new List<Di81Archivo>();
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT codigo_producto, lote, ubicacion, cantidad_final
                        FROM consolidado_inventario
                        WHERE operacion_id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("operacion_id", operacionId));
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new Di81Archivo
                {
                    OperacionId = operacionId,
                    CodigoProducto = reader.GetString(0),
                    Lote = reader.GetString(1),
                    Ubicacion = reader.GetString(2),
                    CantidadFinal = reader.GetDecimal(3),
                    FechaGeneracion = DateTime.Now
                });
            }
            return lista;
        }
    }
}
