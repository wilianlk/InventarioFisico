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

        public ConsolidacionController(ConsolidacionService service, ILogger<ConsolidacionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("conteos-finalizados")]
        public async Task<IActionResult> ObtenerConteosFinalizados()
        {
            var data = await _service.ObtenerConteosFinalizadosAsync();
            return Ok(new { items = data });
        }

        [HttpPost("cerrar/{operacionId:int}")]
        public async Task<IActionResult> CerrarConsolidacion(int operacionId)
        {
            await _service.CerrarConsolidacionAsync(operacionId);
            return Ok(new { mensaje = "Consolidación cerrada correctamente." });
        }

        [HttpPost("generar-di81/{operacionId:int}")]
        public async Task<IActionResult> GenerarArchivoDI81(int operacionId)
        {
            var contenido = await _service.GenerarArchivoDI81Async(operacionId);
            var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
            return File(bytes, "text/plain", $"DI81_{operacionId}.txt");
        }

        [HttpGet("consolidacion-finalizada/{operacionId:int}")]
        public async Task<IActionResult> ConsolidacionFinalizada(int operacionId)
        {
            var finalizada = await _service.ConsolidacionFinalizadaAsync(operacionId);
            return Ok(new { finalizada });
        }
    }
}
