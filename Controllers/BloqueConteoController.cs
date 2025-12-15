using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;
using InventarioFisico.Models;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BloqueConteoController : ControllerBase
    {
        private readonly BloqueConteoService _service;
        private readonly ILogger<BloqueConteoController> _logger;

        public BloqueConteoController(BloqueConteoService service, ILogger<BloqueConteoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> Crear([FromBody] BloqueConteo bloque)
        {
            try
            {
                var id = await _service.CrearBloqueAsync(bloque);
                return Ok(new { mensaje = "Bloque creado correctamente.", id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear bloque");
                return StatusCode(500, new { mensaje = "Error al crear el bloque.", detalle = ex.Message });
            }
        }

        [HttpGet("{operacionId}")]
        public async Task<IActionResult> Obtener(int operacionId)
        {
            try
            {
                var bloques = await _service.ObtenerPorOperacionAsync(operacionId);
                return Ok(bloques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bloques");
                return StatusCode(500, new { mensaje = "Error al obtener los bloques.", detalle = ex.Message });
            }
        }

        [HttpPut("asignar")]
        public async Task<IActionResult> Asignar(int bloqueId, int? grupoId)
        {
            try
            {
                await _service.ActualizarGrupoAsync(bloqueId, grupoId);
                return Ok(new { mensaje = "Bloque asignado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar grupo al bloque");
                return StatusCode(500, new { mensaje = "Error al asignar el grupo.", detalle = ex.Message });
            }
        }
    }
}
