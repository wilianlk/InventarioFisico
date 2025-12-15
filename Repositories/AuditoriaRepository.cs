using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;
using IBM.Data.Db2;

namespace InventarioFisico.Repositories
{
    public class AuditoriaRepository
    {
        private readonly IConnectionStringProvider _provider;

        public AuditoriaRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public async Task<List<Auditoria>> ObtenerAuditoriaAsync(string usuario, string bodega, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var lista = new List<Auditoria>();
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "SELECT id, usuario, bodega, accion, descripcion, fecha, hora FROM auditoria_inventario WHERE 1=1";

            if (!string.IsNullOrEmpty(usuario))
                sql += $" AND usuario = '{usuario}'";
            if (!string.IsNullOrEmpty(bodega))
                sql += $" AND bodega = '{bodega}'";
            if (fechaInicio.HasValue)
                sql += $" AND fecha >= '{fechaInicio:yyyy-MM-dd}'";
            if (fechaFin.HasValue)
                sql += $" AND fecha <= '{fechaFin:yyyy-MM-dd}'";

            using var cmd = new DB2Command(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new Auditoria
                {
                    Id = reader.GetInt32(0),
                    Usuario = reader.GetString(1),
                    Bodega = reader.GetString(2),
                    Accion = reader.GetString(3),
                    Descripcion = reader.GetString(4),
                    Fecha = reader.GetDateTime(5),
                    Hora = reader.GetString(6)
                });
            }
            return lista;
        }

        public async Task RegistrarAuditoriaAsync(Auditoria registro)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            var sql = "INSERT INTO auditoria_inventario (usuario, bodega, accion, descripcion, fecha, hora) VALUES (?, ?, ?, ?, ?, ?)";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("usuario", registro.Usuario));
            cmd.Parameters.Add(new DB2Parameter("bodega", registro.Bodega));
            cmd.Parameters.Add(new DB2Parameter("accion", registro.Accion));
            cmd.Parameters.Add(new DB2Parameter("descripcion", registro.Descripcion));
            cmd.Parameters.Add(new DB2Parameter("fecha", registro.Fecha));
            cmd.Parameters.Add(new DB2Parameter("hora", registro.Hora));

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
