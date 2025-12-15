using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrupoConteoController : ControllerBase
    {
        private readonly GrupoConteoService _service;
        private readonly ILogger<GrupoConteoController> _logger;

        public GrupoConteoController(GrupoConteoService service, ILogger<GrupoConteoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> Crear([FromQuery] string nombre, [FromQuery] int usuarioId)
        {
            try
            {
                var id = await _service.CrearGrupoAsync(nombre, usuarioId);
                return Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al crear grupo");
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear grupo");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("todos")]
        public async Task<IActionResult> Todos()
        {
            try
            {
                return Ok(await _service.ObtenerTodosAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los grupos");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("por-operacion/{operacionId:int}")]
        public async Task<IActionResult> PorOperacion(int operacionId)
        {
            try
            {
                return Ok(await _service.ObtenerPorOperacionAsync(operacionId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos por operación");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("disponibles")]
        public async Task<IActionResult> Disponibles()
        {
            try
            {
                return Ok(await _service.ObtenerDisponiblesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos disponibles");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{grupoId:int}/activar")]
        public async Task<IActionResult> Activar(int grupoId)
        {
            try
            {
                await _service.ActivarGrupoAsync(grupoId);
                return Ok(new { mensaje = "Grupo activado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar grupo");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{grupoId:int}/inactivar")]
        public async Task<IActionResult> Inactivar(int grupoId)
        {
            try
            {
                await _service.InactivarGrupoAsync(grupoId);
                return Ok(new { mensaje = "Grupo inactivado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inactivar grupo");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
