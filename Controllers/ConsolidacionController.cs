using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using InventarioFisico.Services;
using InventarioFisico.Models;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsolidacionController : ControllerBase
    {
        private readonly ConsolidacionService _consolidacionService;

        public ConsolidacionController(ConsolidacionService consolidacionService)
        {
            _consolidacionService = consolidacionService;
        }

        // GET: api/consolidacion/{operacionId}
        [HttpGet("{operacionId}")]
        public async Task<IActionResult> ObtenerConsolidado(int operacionId)
        {
            try
            {
                var resultado = await _consolidacionService.ObtenerConsolidadoAsync(operacionId);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener la consolidación.", detalle = ex.Message });
            }
        }

        // POST: api/consolidacion/aprobar-reconteo
        [HttpPost("aprobar-reconteo")]
        public async Task<IActionResult> AprobarReconteo([FromBody] Consolidado consolidado)
        {
            try
            {
                await _consolidacionService.AprobarReconteoAsync(consolidado);
                return Ok(new { mensaje = "Reconteo aprobado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al aprobar el reconteo.", detalle = ex.Message });
            }
        }

        // POST: api/consolidacion/generar-di81/{operacionId}
        [HttpPost("generar-di81/{operacionId}")]
        public async Task<IActionResult> GenerarArchivoDI81(int operacionId)
        {
            try
            {
                var archivo = await _consolidacionService.GenerarArchivoDI81Async(operacionId);
                var bytes = System.Text.Encoding.UTF8.GetBytes(archivo);
                var nombreArchivo = $"DI81_{operacionId}_{DateTime.Now:yyyyMMdd_HHmm}.txt";

                return File(bytes, "text/plain", nombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar el archivo DI81.", detalle = ex.Message });
            }
        }
    }
}
