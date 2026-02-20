using Microsoft.AspNetCore.Mvc;
using InventarioFisico.Services;
using InventarioFisico.Repositories;
using InventarioFisico.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly InventarioService _inventarioService;
        private readonly GrupoConteoService _grupoService;
        private readonly GeneradorConteoService _generadorConteoService;
        private readonly ValidacionCierreService _validacionCierreService;
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly OperacionConteoItemsRepository _itemsRepo;
        private readonly CerrarConteoService _cerrarConteoService;

        public InventarioController(
            InventarioService inventarioService,
            GrupoConteoService grupoService,
            GeneradorConteoService generadorConteoService,
            ValidacionCierreService validacionCierreService,
            OperacionConteoRepository conteoRepo,
            OperacionConteoItemsRepository itemsRepo,
            CerrarConteoService cerrarConteoService)
        {
            _inventarioService = inventarioService;
            _grupoService = grupoService;
            _generadorConteoService = generadorConteoService;
            _validacionCierreService = validacionCierreService;
            _conteoRepo = conteoRepo;
            _itemsRepo = itemsRepo;
            _cerrarConteoService = cerrarConteoService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerOperaciones()
        {
            var operaciones = await _inventarioService.ObtenerOperacionesAsync();
            return Ok(operaciones);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerOperacionPorId(int id)
        {
            var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(id);
            if (operacion == null)
                return NotFound(new { mensaje = "Operación no encontrada." });

            return Ok(operacion);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearOperacion([FromBody] InventarioOperacion operacion)
        {
            try
            {
                var operacionId = await _inventarioService.CrearOperacionAsync(operacion);

                if (operacion.GruposIds != null)
                {
                    foreach (var grupoId in operacion.GruposIds)
                        await _grupoService.AsociarOperacionGrupoAsync(operacionId, grupoId);
                }

                await _generadorConteoService.GenerarConteosInicialesAsync(
                    operacionId,
                    operacion.NumeroConteo
                );

                return Ok(new { mensaje = "Operación creada correctamente.", id = operacionId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("cerrar/{id:int}")]
        public async Task<IActionResult> CerrarOperacion(int id)
        {
            await _validacionCierreService.ValidarAsync(id);
            await _inventarioService.CerrarOperacionAsync(id);
            return Ok(new { mensaje = "Operación cerrada exitosamente." });
        }

        [HttpDelete("eliminar/{id:int}")]
        public async Task<IActionResult> EliminarOperacion(int id)
        {
            try
            {
                await _inventarioService.EliminarOperacionAsync(id);
                return Ok(new { mensaje = "Operación eliminada correctamente." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        [HttpGet("conteo/actual")]
        public async Task<IActionResult> ObtenerConteoActual()
        {
            var conteos = await _conteoRepo.ObtenerAbiertosAsync();
            if (conteos.Count == 0)
                return NotFound(new { mensaje = "No hay conteos abiertos." });

            var respuesta = new List<object>();

            foreach (var conteo in conteos)
            {
                var grupo = await _grupoService.ObtenerPorIdAsync(conteo.GrupoId);
                if (grupo == null)
                    continue;

                var items = await _itemsRepo.ObtenerAsync(
                    conteo.OperacionId,
                    conteo.GrupoId,
                    conteo.NumeroConteo
                );

                respuesta.Add(new
                {
                    operacionId = conteo.OperacionId,
                    grupoId = grupo.Id,
                    grupo = grupo.Nombre,
                    conteoId = conteo.Id,
                    numeroConteo = conteo.NumeroConteo,
                    estadoConteo = conteo.Estado,
                    items
                });
            }

            return Ok(respuesta);
        }

        [HttpGet("conteo/por-grupo/{operacionId:int}/{grupoId:int}")]
        public async Task<IActionResult> ObtenerConteoPorGrupo(int operacionId, int grupoId)
        {
            var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
            if (grupo == null)
                return NotFound(new { mensaje = "Grupo no encontrado." });

            var conteo = await _conteoRepo.ObtenerAbiertoPorOperacionYGrupoAsync(operacionId, grupoId);
            if (conteo == null)
                return NotFound(new { mensaje = "No hay conteo abierto para este grupo." });

            var items = await _itemsRepo.ObtenerAsync(
                conteo.OperacionId,
                conteo.GrupoId,
                conteo.NumeroConteo
            );

            return Ok(new
            {
                operacionId = conteo.OperacionId,
                grupoId = conteo.GrupoId,
                grupo = grupo.Nombre,
                conteoId = conteo.Id,
                numeroConteo = conteo.NumeroConteo,
                estadoConteo = conteo.Estado,
                items
            });
        }

        [HttpPut("conteo/{conteoId:int}/finalizar")]
        public async Task<IActionResult> FinalizarConteo(int conteoId)
        {
            var conteo = await _conteoRepo.ObtenerPorIdAsync(conteoId);
            if (conteo == null)
                return NotFound(new { mensaje = "Conteo no encontrado." });

            if (conteo.Estado == "CERRADO")
                return BadRequest(new { mensaje = "El conteo ya está cerrado." });

            await _cerrarConteoService.CerrarConteoAsync(conteoId);
            return Ok(new { mensaje = "Conteo cerrado correctamente." });
        }

        [HttpGet("avance/{operacionId:int}")]
        public async Task<IActionResult> ObtenerAvance(int operacionId)
        {
            var items = await _itemsRepo.ObtenerPorOperacionAsync(operacionId);

            if (items == null || !items.Any())
                return NotFound(new { mensaje = "No hay ítems para esta operación." });

            var total = items.Count;
            var contados = items.Count(x => x.CantidadContada > 0);

            return Ok(new
            {
                operacionId,
                totalItems = total,
                itemsContados = contados,
                porcentaje = total == 0 ? 0 : (contados * 100) / total
            });
        }
    }
}
