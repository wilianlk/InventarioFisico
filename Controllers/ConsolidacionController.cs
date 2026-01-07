using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;
using InventarioFisico.Models;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsolidacionController : ControllerBase
    {
        private readonly ConsolidacionService _service;
        private readonly ILogger<ConsolidacionController> _logger;

        public ConsolidacionController(
            ConsolidacionService service,
            ILogger<ConsolidacionController> logger
        )
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("conteos-finalizados")]
        public async Task<IActionResult> ObtenerConteosFinalizados()
        {
            try
            {
                _logger.LogInformation(
                    "Inicio ObtenerConteosFinalizados - Usuario: {User}",
                    User?.Identity?.Name ?? "anon"
                );

                var data = await _service.ObtenerConteosFinalizadosAsync();

                _logger.LogInformation(
                    "ObtenerConteosFinalizados OK - Registros: {Count}",
                    data?.Count() ?? 0
                );

                return Ok(new { items = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ObtenerConteosFinalizados");

                return StatusCode(
                    500,
                    new { mensaje = "Error interno al obtener la consolidación." }
                );
            }
        }

        [HttpPost("finalizar")]
        public async Task<IActionResult> FinalizarOperaciones([FromBody] ConsolidacionCierre request)
        {
            try
            {
                var result = await _service.FinalizarOperacionesAsync(request.OperacionIds);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno al finalizar la consolidación." });
            }
        }

        [HttpPost("generar-di81/{operacionId}")]
        public async Task<IActionResult> GenerarArchivoDI81(int operacionId)
        {
            try
            {
                var contenido = await _service.GenerarArchivoDI81Async(operacionId);
                var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
                return File(bytes, "text/plain", $"DI81_{operacionId}.txt");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno al generar el archivo." });
            }
        }

        [HttpGet("consolidacion-finalizada/{operacionId}")]
        public async Task<IActionResult> ConsolidacionFinalizada(int operacionId)
        {
            var finalizada = await _service.ConsolidacionFinalizadaAsync(operacionId);
            return Ok(new { finalizada });
        }
    }
}
