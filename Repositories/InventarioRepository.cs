using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;

namespace InventarioFisico.Repositories
{
    public class InventarioRepository
    {
        private readonly IConnectionStringProvider _provider;

        public InventarioRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public async Task<List<InventarioOperacion>> ObtenerOperacionesAsync()
        {
            var lista = new List<InventarioOperacion>();
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT id,bodega,fecha,observaciones,estado,usuario_creacion,fecha_creacion,numero_conteo
                        FROM operaciones_inventario";

            using var cmd = new DB2Command(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new InventarioOperacion
                {
                    Id = reader.GetInt32(0),
                    Bodega = reader.GetString(1),
                    Fecha = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                    Observaciones = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Estado = reader.GetString(4),
                    UsuarioCreacion = reader.GetString(5),
                    FechaCreacion = reader.GetDateTime(6),
                    NumeroConteo = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                });
            }

            return lista;
        }

        public async Task<InventarioOperacion> ObtenerOperacionPorIdAsync(int id)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"SELECT id,bodega,fecha,observaciones,estado,usuario_creacion,fecha_creacion,numero_conteo
                        FROM operaciones_inventario
                        WHERE id = ?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("id", id));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new InventarioOperacion
                {
                    Id = reader.GetInt32(0),
                    Bodega = reader.GetString(1),
                    Fecha = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                    Observaciones = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Estado = reader.GetString(4),
                    UsuarioCreacion = reader.GetString(5),
                    FechaCreacion = reader.GetDateTime(6),
                    NumeroConteo = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                };
            }

            return null;
        }

        public async Task<int> CrearOperacionAsync(InventarioOperacion operacion)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO operaciones_inventario
                (bodega,fecha,observaciones,estado,usuario_creacion,fecha_creacion,numero_conteo)
                VALUES (?,?,?,?,?,?,?);
                SELECT dbinfo('sqlca.sqlerrd1') FROM systables WHERE tabid = 1;
            ";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("bodega", operacion.Bodega));
            cmd.Parameters.Add(new DB2Parameter("fecha", operacion.Fecha));
            cmd.Parameters.Add(new DB2Parameter("observaciones", operacion.Observaciones));
            cmd.Parameters.Add(new DB2Parameter("estado", operacion.Estado));
            cmd.Parameters.Add(new DB2Parameter("usuario_creacion", operacion.UsuarioCreacion));
            cmd.Parameters.Add(new DB2Parameter("fecha_creacion", operacion.FechaCreacion));
            cmd.Parameters.Add(new DB2Parameter("numero_conteo", operacion.NumeroConteo));

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task ActualizarEstadoAsync(int id, string nuevoEstado)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "UPDATE operaciones_inventario SET estado = ? WHERE id = ?";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("estado", nuevoEstado));
            cmd.Parameters.Add(new DB2Parameter("id", id));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarOperacionAsync(int id)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();

            try
            {
                var sqlDetalle = @"
                    DELETE FROM consolidacion_detalle
                    WHERE cabecera_id IN (
                        SELECT id FROM consolidacion_cabecera WHERE operacion_id = ?
                    )";

                var sqlCabecera = "DELETE FROM consolidacion_cabecera WHERE operacion_id = ?";
                var sqlItems = "DELETE FROM operacion_conteo_items WHERE operacion_id = ?";
                var sqlConteo = "DELETE FROM operacion_conteo WHERE operacion_id = ?";
                var sqlGrupos = "DELETE FROM operacion_grupos WHERE operacion_id = ?";
                var sqlBloques = "DELETE FROM bloque_conteo WHERE bc_operacion_id = ?";
                var sqlOperacion = "DELETE FROM operaciones_inventario WHERE id = ?";

                foreach (var sql in new[]
                {
                    sqlDetalle,
                    sqlCabecera,
                    sqlItems,
                    sqlConteo,
                    sqlGrupos,
                    sqlBloques,
                    sqlOperacion
                })
                {
                    using var cmd = new DB2Command(sql, conn, tx);
                    cmd.Parameters.Add(new DB2Parameter("id", id));
                    await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> ExisteOperacionAsync(string bodega, DateTime fecha)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "SELECT COUNT(*) FROM operaciones_inventario WHERE bodega = ? AND fecha = ?";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("bodega", bodega));
            cmd.Parameters.Add(new DB2Parameter("fecha", fecha));

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
    }
}
