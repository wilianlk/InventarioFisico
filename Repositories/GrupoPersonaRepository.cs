using IBM.Data.Db2;
using InventarioFisico.Infrastructure;
using InventarioFisico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventarioFisico.Repositories
{
    public class GrupoPersonaRepository
    {
        private readonly IConnectionStringProvider _provider;

        public GrupoPersonaRepository(IConnectionStringProvider provider)
        {
            _provider = provider;
        }

        public async Task AgregarAsync(GrupoPersona persona)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"INSERT INTO grupo_persona 
                                 (gp_grupo_id, gp_usuario_id, gp_usuario_nombre)
                                 VALUES (?, ?, ?)";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", persona.GrupoId));
            cmd.Parameters.Add(new DB2Parameter("usuarioId", persona.UsuarioId));
            cmd.Parameters.Add(new DB2Parameter("nombre", persona.UsuarioNombre));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarAsync(int grupoId, int usuarioId)
        {
            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"DELETE FROM grupo_persona 
                                 WHERE gp_grupo_id=? AND gp_usuario_id=?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", grupoId));
            cmd.Parameters.Add(new DB2Parameter("usuario", usuarioId));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<GrupoPersona>> ObtenerPorGrupoAsync(int grupoId)
        {
            var lista = new List<GrupoPersona>();

            using var conn = new DB2Connection(_provider.Get());
            await conn.OpenAsync();

            const string sql = @"SELECT gp_id, gp_grupo_id, gp_usuario_id, gp_usuario_nombre
                                 FROM grupo_persona WHERE gp_grupo_id=?";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("grupo", grupoId));

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new GrupoPersona
                {
                    Id = r.GetInt32(0),
                    GrupoId = r.GetInt32(1),
                    UsuarioId = r.GetInt32(2),
                    UsuarioNombre = r.GetString(3).Trim()
                });
            }

            return lista;
        }
    }
}
