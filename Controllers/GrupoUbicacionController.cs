using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrupoUbicacionController : ControllerBase
    {
        private readonly GrupoUbicacionService _service;
        private readonly ILogger<GrupoUbicacionController> _logger;

        public GrupoUbicacionController(
            GrupoUbicacionService service,
            ILogger<GrupoUbicacionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("{grupoId:int}")]
        public async Task<IActionResult> Obtener(int grupoId)
        {
            try
            {
                var ubicaciones = await _service.ObtenerPorGrupoAsync(grupoId);

                var ubicacionesConItems = new List<object>();

                foreach (var ubicacion in ubicaciones)
                {
                    var items = await _service.ObtenerItemsPorUbicacionAsync(ubicacion.Ubicacion);
                    ubicacionesConItems.Add(new { ubicacion, items });
                }

                return Ok(ubicacionesConItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ubicaciones del grupo");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("rango")]
        public async Task<IActionResult> ObtenerRango([FromQuery] string desde, [FromQuery] string hasta)
        {
            try
            {
                var ubicaciones = await _service.ObtenerRangoUbicacionesAsync(desde, hasta);

                if (ubicaciones.Count == 0)
                {
                    return BadRequest(new { mensaje = "La ubicación no existe en inventario." });
                }

                return Ok(ubicaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ubicaciones en el rango");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("items")]
        public async Task<IActionResult> ObtenerItemsPorUbicacion([FromQuery] string ubicacion)
        {
            try
            {
                var items = await _service.ObtenerItemsPorUbicacionAsync(ubicacion);

                if (items == null || items.Count == 0)
                    return BadRequest(new { mensaje = "La ubicación no existe en inventario." });

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ítems de la ubicación");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("agregar")]
        public async Task<IActionResult> Agregar(int grupoId, string ubicacion)
        {
            try
            {
                await _service.AgregarAsync(grupoId, ubicacion);
                return Ok(new { mensaje = "Ubicación agregada correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar ubicación al grupo");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("eliminar")]
        public async Task<IActionResult> Eliminar(int grupoId, string ubicacion)
        {
            try
            {
                await _service.EliminarAsync(grupoId, ubicacion);
                return Ok(new { mensaje = "Ubicación eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar ubicación del grupo");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
