using Microsoft.AspNetCore.Mvc;
using InventarioFisico.Models;
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

        [HttpPut("no-encontrado")]
        public async Task<IActionResult> ActualizarNoEncontrado([FromBody] ActualizarNoEncontradoRequest request)
        {
            try
            {
                _logger.LogInformation("Actualizar NoEncontrado. ConteoId={ConteoId}, CodigoItem={CodigoItem}, NoEncontrado={NoEncontrado}", 
                    request.ConteoId, request.CodigoItem, request.NoEncontrado);
                await _itemsService.ActualizarNoEncontradoPorCodigoAsync(request.ConteoId, request.CodigoItem, request.NoEncontrado);
                return Ok(new { mensaje = "Estado NoEncontrado actualizado correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Item no encontrado al actualizar NoEncontrado. CodigoItem={CodigoItem}", request.CodigoItem);
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar NoEncontrado. CodigoItem={CodigoItem}", request.CodigoItem);
                return StatusCode(500, new { mensaje = "Error al actualizar NoEncontrado.", detalle = ex.Message });
            }
        }

        [HttpPost("actualizar-conteo-id-faltantes")]
        public async Task<IActionResult> ActualizarConteoIdFaltantes()
        {
            try
            {
                _logger.LogInformation("Actualizando ConteoId faltantes en items");
                await _itemsService.ActualizarConteoIdFaltantesAsync();
                return Ok(new { mensaje = "ConteoId actualizados correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar ConteoId faltantes");
                return StatusCode(500, new { mensaje = "Error al actualizar ConteoId.", detalle = ex.Message });
            }
        }
    }

}
