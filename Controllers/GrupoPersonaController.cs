using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrupoPersonaController : ControllerBase
    {
        private readonly GrupoPersonaService _service;
        private readonly GrupoConteoService _grupoService;
        private readonly ILogger<GrupoPersonaController> _logger;

        public GrupoPersonaController(
            GrupoPersonaService service,
            GrupoConteoService grupoService,
            ILogger<GrupoPersonaController> logger)
        {
            _service = service;
            _grupoService = grupoService;
            _logger = logger;
        }

        [HttpPost("agregar")]
        public async Task<IActionResult> Agregar(int grupoId, int usuarioId, string usuarioNombre)
        {
            try
            {
                var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
                if (grupo == null)
                    return NotFound(new { mensaje = "Grupo no encontrado." });

                if (grupo.Estado != "ACTIVO")
                    return Conflict(new { mensaje = "No se pueden modificar personas: el grupo est\u00e1 INACTIVO." });

                await _service.AgregarPersonaAsync(grupoId, usuarioId, usuarioNombre);
                return Ok(new { mensaje = "Usuario agregado correctamente al grupo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar usuario al grupo");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("eliminar")]
        public async Task<IActionResult> Eliminar(int grupoId, int usuarioId)
        {
            try
            {
                var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
                if (grupo == null)
                    return NotFound(new { mensaje = "Grupo no encontrado." });

                if (grupo.Estado != "ACTIVO")
                    return Conflict(new { mensaje = "No se pueden modificar personas: el grupo est\u00e1 INACTIVO." });

                await _service.EliminarPersonaAsync(grupoId, usuarioId);
                return Ok(new { mensaje = "Usuario eliminado correctamente del grupo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario del grupo");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("personas/{grupoId}")]
        public async Task<IActionResult> ObtenerPorGrupo(int grupoId)
        {
            try
            {
                var personas = await _service.ObtenerPersonasAsync(grupoId);
                return Ok(personas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener personas del grupo");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
