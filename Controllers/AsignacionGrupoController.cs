using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;

namespace InventarioFisico.Controllers
{
    public class AsignacionGrupoRequest
    {
        public int OperacionId { get; set; }
        public int GrupoId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AsignacionGrupoController : ControllerBase
    {
        private readonly GrupoConteoService _grupoService;
        private readonly ILogger<AsignacionGrupoController> _logger;

        public AsignacionGrupoController(
            GrupoConteoService grupoService,
            ILogger<AsignacionGrupoController> logger)
        {
            _grupoService = grupoService;
            _logger = logger;
        }

        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarOperacionAGrupo([FromBody] AsignacionGrupoRequest req)
        {
            try
            {
                if (req == null || req.OperacionId <= 0 || req.GrupoId <= 0)
                    return BadRequest(new { mensaje = "operacionId y grupoId inválidos." });

                await _grupoService.AsociarOperacionGrupoAsync(req.OperacionId, req.GrupoId);

                return Ok(new { mensaje = "Grupo asignado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar operación a grupo");
                return StatusCode(500, new { mensaje = "Error al asignar el grupo.", detalle = ex.Message });
            }
        }

        [HttpPost("desasociar")]
        public async Task<IActionResult> DesasociarOperacionGrupo([FromBody] AsignacionGrupoRequest req)
        {
            try
            {
                if (req == null || req.OperacionId <= 0 || req.GrupoId <= 0)
                    return BadRequest(new { mensaje = "operacionId y grupoId inválidos." });

                await _grupoService.DesasociarOperacionGrupoAsync(req.OperacionId, req.GrupoId);

                return Ok(new { mensaje = "Grupo desasociado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desasociar operación y grupo");
                return StatusCode(500, new { mensaje = "Error al desasociar el grupo.", detalle = ex.Message });
            }
        }
    }
}
