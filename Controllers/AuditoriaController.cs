using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using InventarioFisico.Services;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditoriaController : ControllerBase
    {
        private readonly AuditoriaService _auditoriaService;

        public AuditoriaController(AuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        // GET: api/auditoria
        [HttpGet]
        public async Task<IActionResult> ObtenerAuditoria(
            [FromQuery] string? usuario = null,
            [FromQuery] string? bodega = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var registros = await _auditoriaService.ObtenerAuditoriaAsync(usuario, bodega, fechaInicio, fechaFin);
                return Ok(registros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al consultar la auditoría.", detalle = ex.Message });
            }
        }

        // GET: api/auditoria/exportar
        [HttpGet("exportar")]
        public async Task<IActionResult> ExportarAuditoriaCSV(
            [FromQuery] string? usuario = null,
            [FromQuery] string? bodega = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var contenido = await _auditoriaService.ExportarAuditoriaCSVAsync(usuario, bodega, fechaInicio, fechaFin);
                var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
                var nombreArchivo = $"Auditoria_{DateTime.Now:yyyyMMdd_HHmm}.csv";

                return File(bytes, "text/csv", nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al exportar la auditoría.", detalle = ex.Message });
            }
        }
    }
}
