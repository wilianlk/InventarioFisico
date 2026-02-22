using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsolidacionController : ControllerBase
    {
        private readonly ConsolidacionService _service;
        private readonly ILogger<ConsolidacionController> _logger;

        public ConsolidacionController(ConsolidacionService service, ILogger<ConsolidacionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("conteos-finalizados")]
        public async Task<IActionResult> ObtenerConteosFinalizados()
        {
            try
            {
                _logger.LogInformation("Obtener conteos finalizados");
                var data = await _service.ObtenerConteosFinalizadosAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conteos finalizados");
                return StatusCode(500, new { mensaje = "Error al obtener conteos finalizados." });
            }
        }

        [HttpPost("cerrar/{operacionId:int}")]
        public async Task<IActionResult> CerrarConsolidacion(int operacionId)
        {
            try
            {
                _logger.LogInformation("Cerrar consolidación. OperacionId={OperacionId}", operacionId);
                await _service.CerrarConsolidacionAsync(operacionId);
                return Ok(new { mensaje = "Consolidación cerrada correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflicto al cerrar consolidación. OperacionId={OperacionId}", operacionId);
                return Conflict(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar consolidación. OperacionId={OperacionId}", operacionId);
                return StatusCode(500, new { mensaje = "Error al cerrar consolidación." });
            }
        }

        [HttpPost("generar-di81/{operacionId:int}")]
        public async Task<IActionResult> GenerarArchivoDI81(int operacionId)
        {
            try
            {
                _logger.LogInformation("Generar archivo DI81. OperacionId={OperacionId}", operacionId);
                var contenido = await _service.GenerarArchivoDI81Async(operacionId);
                var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
                return File(bytes, "text/plain", $"DI81_{operacionId}.txt");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflicto al generar DI81. OperacionId={OperacionId}", operacionId);
                return Conflict(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar DI81. OperacionId={OperacionId}", operacionId);
                return StatusCode(500, new { mensaje = "Error al generar archivo DI81." });
            }
        }

    }
}
