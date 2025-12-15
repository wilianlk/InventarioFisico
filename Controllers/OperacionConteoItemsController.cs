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
            var items = await _itemsService.ObtenerPorConteoAsync(conteoId);
            return Ok(items);
        }

        [HttpPut("{itemId}/cantidad-contada")]
        public async Task<IActionResult> ActualizarCantidadContada(int itemId, [FromBody] int cantidadContada)
        {
            try
            {
                await _itemsService.ActualizarCantidadContadaAsync(itemId, cantidadContada);
                return Ok(new { mensaje = "Cantidad contada actualizada correctamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cantidad contada. ItemId={ItemId}", itemId);
                return StatusCode(500, new { mensaje = "Error al actualizar cantidad contada.", detalle = ex.Message });
            }
        }
    }
}
