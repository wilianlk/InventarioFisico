using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class AuditoriaService
    {
        private readonly AuditoriaRepository _repo;

        public AuditoriaService(AuditoriaRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Auditoria>> ObtenerAuditoriaAsync(string usuario, string bodega, DateTime? fechaInicio, DateTime? fechaFin)
        {
            return await _repo.ObtenerAuditoriaAsync(usuario, bodega, fechaInicio, fechaFin);
        }

        public async Task RegistrarAuditoriaAsync(string usuario, string bodega, string accion, string descripcion)
        {
            var registro = new Auditoria
            {
                Usuario = usuario,
                Bodega = bodega,
                Accion = accion,
                Descripcion = descripcion,
                Fecha = DateTime.Now.Date,
                Hora = DateTime.Now.ToString("HH:mm:ss")
            };
            await _repo.RegistrarAuditoriaAsync(registro);
        }

        public async Task<string> ExportarAuditoriaCSVAsync(string usuario, string bodega, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var registros = await _repo.ObtenerAuditoriaAsync(usuario, bodega, fechaInicio, fechaFin);
            var sb = new StringBuilder();
            sb.AppendLine("ID;Usuario;Bodega;Accion;Descripcion;Fecha;Hora");
            foreach (var r in registros)
                sb.AppendLine($"{r.Id};{r.Usuario};{r.Bodega};{r.Accion};{r.Descripcion};{r.Fecha:yyyy-MM-dd};{r.Hora}");
            return sb.ToString();
        }
    }
}
