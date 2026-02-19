using Microsoft.AspNetCore.Mvc;
using InventarioFisico.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperacionConteoItemsController : ControllerBase
    {
        private readonly OperacionConteoItemsService _itemsService;
        private readonly ILogger<OperacionConteoItemsController> _logger;

        public OperacionConteoItemsController(
            OperacionConteoItemsService itemsService,
            ILogger<OperacionConteoItemsController> logger)
        {
            _itemsService = itemsService;
            _logger = logger;
        }

        [HttpGet("conteo/{conteoId:int}")]
        public async Task<IActionResult> ObtenerPorConteo(int conteoId)
        {
            try
            {
                _logger.LogInformation("Obtener items por conteo. ConteoId={ConteoId}", conteoId);
                var items = await _itemsService.ObtenerPorConteoAsync(conteoId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener items por conteo. ConteoId={ConteoId}", conteoId);
                return StatusCode(500, new { mensaje = "Error al obtener items por conteo." });
            }
        }

        [HttpGet("conteo/{conteoId:int}/no-encontrados")]
        public async Task<IActionResult> ObtenerNoEncontradosPorConteo(int conteoId)
        {
            try
            {
                _logger.LogInformation("Obtener items no encontrados por conteo. ConteoId={ConteoId}", conteoId);
                var items = await _itemsService.ObtenerNoEncontradosPorConteoAsync(conteoId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener items no encontrados. ConteoId={ConteoId}", conteoId);
                return StatusCode(500, new { mensaje = "Error al obtener items no encontrados." });
            }
        }

        [HttpPut("{itemId}/cantidad-contada")]
        public async Task<IActionResult> ActualizarCantidadContada(int itemId, [FromBody] int cantidadContada)
        {
            try
            {
                _logger.LogInformation("Actualizar cantidad contada. ItemId={ItemId}, Cantidad={Cantidad}", itemId, cantidadContada);
                await _itemsService.ActualizarCantidadContadaAsync(itemId, cantidadContada);
                return Ok(new { mensaje = "Cantidad contada actualizada correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Item no encontrado. ItemId={ItemId}", itemId);
                return NotFound(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflicto al actualizar cantidad contada. ItemId={ItemId}", itemId);
                return Conflict(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cantidad contada. ItemId={ItemId}", itemId);
                return StatusCode(500, new { mensaje = "Error al actualizar cantidad contada.", detalle = ex.Message });
            }
        }

        [HttpPut("{itemId}/no-encontrado")]
        public async Task<IActionResult> ActualizarNoEncontrado(int itemId, [FromBody] bool noEncontrado)
        {
            try
            {
                _logger.LogInformation("Actualizar NoEncontrado. ItemId={ItemId}, NoEncontrado={NoEncontrado}", itemId, noEncontrado);
                await _itemsService.ActualizarNoEncontradoAsync(itemId, noEncontrado);
                return Ok(new { mensaje = "Estado NoEncontrado actualizado correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Item no encontrado al actualizar NoEncontrado. ItemId={ItemId}", itemId);
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar NoEncontrado. ItemId={ItemId}", itemId);
                return StatusCode(500, new { mensaje = "Error al actualizar NoEncontrado.", detalle = ex.Message });
            }
        }
    }
}
