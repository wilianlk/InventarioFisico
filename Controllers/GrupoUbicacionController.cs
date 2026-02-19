using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InventarioFisico.Services;
using InventarioFisico.Models;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrupoUbicacionController : ControllerBase
    {
        private readonly GrupoUbicacionService _service;
        private readonly GrupoConteoService _grupoService;

        public GrupoUbicacionController(GrupoUbicacionService service, GrupoConteoService grupoService)
        {
            _service = service;
            _grupoService = grupoService;
        }

        public class GrupoUbicacionAgregarDto
        {
            [Required] public int GrupoId { get; set; }
            [Required] public string Bodega { get; set; } = "";
            [Required] public List<GrupoUbicacionItemDto> Ubicaciones { get; set; } = new();
        }

        [HttpGet]
        public async Task<IActionResult> Obtener([FromQuery] int? grupoId)
        {
            var resultado = await _service.ObtenerAsync(grupoId);

            return Ok(new
            {
                total = resultado.Count,
                data = resultado
            });
        }

        [HttpGet("previsualizar")]
        public async Task<IActionResult> Previsualizar(
            [FromQuery][Required] string bodega,
            [FromQuery] string? rack,
            [FromQuery] string? lado,
            [FromQuery] string? altura,
            [FromQuery] string? ubicacion)
        {
            var resultado = await _service.PrevisualizarAsync(
                bodega,
                rack,
                lado,
                altura,
                ubicacion
            );

            return Ok(new
            {
                total = resultado.Count,
                data = resultado
            });
        }

        [HttpGet("bodegas")]
        public async Task<IActionResult> ObtenerBodegas()
        {
            var data = await _service.ObtenerBodegasAsync();

            return Ok(new
            {
                total = data.Count,
                data
            });
        }

        [HttpPost("agregar")]
        public async Task<IActionResult> Agregar([FromBody] GrupoUbicacionAgregarDto req)
        {
            var grupo = await _grupoService.ObtenerPorIdAsync(req.GrupoId);
            if (grupo == null)
                return NotFound(new { mensaje = "Grupo no encontrado." });

            if (grupo.Estado != "ACTIVO")
                return Conflict(new { mensaje = "No se pueden modificar ubicaciones: el grupo est\u00e1 INACTIVO." });

            var lista = new List<GrupoUbicacion>();

            foreach (var u in req.Ubicaciones)
            {
                lista.Add(new GrupoUbicacion
                {
                    GrupoId = req.GrupoId,
                    Bodega = req.Bodega,
                    Ubicaciones = u.Ubicacion,
                    Rack = u.Rack,
                    Lado = u.Lado,
                    Altura = u.Altura,
                    Ubicacion = u.Posicion
                });
            }

            await _service.AgregarAsync(req.GrupoId, req.Bodega, lista);

            return Ok(new { mensaje = "Ubicaciones agregadas correctamente." });
        }

        [HttpDelete("eliminar")]
        public async Task<IActionResult> Eliminar(
            [FromQuery] int grupoId,
            [FromQuery] string bodega,
            [FromQuery] string? rack,
            [FromQuery] string? lado,
            [FromQuery] string? altura,
            [FromQuery] string? ubicacion)
        {
            var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
            if (grupo == null)
                return NotFound(new { mensaje = "Grupo no encontrado." });

            if (grupo.Estado != "ACTIVO")
                return Conflict(new { mensaje = "No se pueden modificar ubicaciones: el grupo est\u00e1 INACTIVO." });

            await _service.EliminarAsync(
                grupoId,
                bodega,
                rack,
                lado,
                altura,
                ubicacion
            );

            return Ok(new { mensaje = "Filtro eliminado correctamente." });
        }
    }
}
