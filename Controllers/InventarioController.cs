using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InventarioFisico.Services;
using InventarioFisico.Repositories;
using InventarioFisico.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace InventarioFisico.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private readonly InventarioService _inventarioService;
        private readonly GrupoConteoService _grupoService;
        private readonly GrupoPersonaService _personaService;
        private readonly BloqueConteoService _bloqueService;
        private readonly GeneradorConteoService _generadorConteoService;
        private readonly ValidacionCierreService _validacionCierreService;
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly OperacionConteoItemsRepository _itemsRepo;
        private readonly GrupoUbicacionRepository _grupoUbicacionRepo;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(
            InventarioService inventarioService,
            GrupoConteoService grupoService,
            GrupoPersonaService personaService,
            BloqueConteoService bloqueService,
            GeneradorConteoService generadorConteoService,
            ValidacionCierreService validacionCierreService,
            OperacionConteoRepository conteoRepo,
            OperacionConteoItemsRepository itemsRepo,
            GrupoUbicacionRepository grupoUbicacionRepo,
            ILogger<InventarioController> logger)
        {
            _inventarioService = inventarioService;
            _grupoService = grupoService;
            _personaService = personaService;
            _bloqueService = bloqueService;
            _generadorConteoService = generadorConteoService;
            _validacionCierreService = validacionCierreService;
            _conteoRepo = conteoRepo;
            _itemsRepo = itemsRepo;
            _grupoUbicacionRepo = grupoUbicacionRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerOperaciones()
        {
            try
            {
                var operaciones = await _inventarioService.ObtenerOperacionesAsync();
                return Ok(operaciones);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al obtener operaciones");
                return StatusCode(500, new { mensaje = "Error al obtener operaciones.", detalle = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerOperacionPorId(int id)
        {
            try
            {
                var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(id);
                if (operacion == null)
                    return NotFound(new { mensaje = "Operación no encontrada." });

                return Ok(operacion);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al consultar operación");
                return StatusCode(500, new { mensaje = "Error al consultar la operación.", detalle = ex.Message });
            }
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearOperacion([FromBody] InventarioOperacion operacion)
        {
            try
            {
                if (operacion.GruposIds != null && operacion.GruposIds.Any())
                {
                    var gruposIncompletos = new List<string>();

                    foreach (var grupoId in operacion.GruposIds.Distinct())
                    {
                        var personas = await _personaService.ObtenerPersonasAsync(grupoId);
                        var ubicaciones = await _grupoUbicacionRepo.ObtenerUbicacionesPorGrupoAsync(grupoId);

                        if (!personas.Any() || !ubicaciones.Any())
                        {
                            var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
                            if (grupo != null)
                                gruposIncompletos.Add(grupo.Nombre);
                        }
                    }

                    if (gruposIncompletos.Any())
                    {
                        return BadRequest(new
                        {
                            mensaje = "Antes de cerrar esta operación, completa personas y ubicaciones en los siguientes grupos:",
                            detalle = gruposIncompletos
                        });
                    }
                }

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
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al crear operación");
                return StatusCode(500, new { mensaje = "Error al crear operación.", detalle = ex.Message });
            }
        }

        [HttpPut("cerrar/{id}")]
        public async Task<IActionResult> CerrarOperacion(int id)
        {
            try
            {
                var grupos = await _grupoService.ObtenerPorOperacionAsync(id);
                var bloques = await _bloqueService.ObtenerPorOperacionAsync(id);

                foreach (var g in grupos)
                {
                    if (g.Estado == "ACTIVO")
                    {
                        var personas = await _personaService.ObtenerPersonasAsync(g.Id);
                        if (!personas.Any())
                            return BadRequest(new { mensaje = "No se puede cerrar: hay grupos sin personas." });
                    }
                }

                foreach (var g in grupos)
                {
                    if (g.Estado == "ACTIVO" && !bloques.Any(b => b.GrupoId == g.Id))
                        return BadRequest(new { mensaje = "No se puede cerrar: hay grupos sin bloques asignados." });
                }

                if (bloques.Any(b => b.GrupoId == null))
                    return BadRequest(new { mensaje = "No se puede cerrar: hay bloques sin asignar." });

                await _validacionCierreService.ValidarAsync(id);
                await _inventarioService.CerrarOperacionAsync(id);

                return Ok(new { mensaje = "Operación cerrada exitosamente." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar operación");
                return StatusCode(500, new { mensaje = "Error al cerrar operación.", detalle = ex.Message });
            }
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarOperacion(int id)
        {
            try
            {
                await _inventarioService.EliminarOperacionAsync(id);
                return Ok(new { mensaje = "Operación eliminada correctamente." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar operación");
                return StatusCode(500, new { mensaje = "Error al eliminar operación.", detalle = ex.Message });
            }
        }

        [HttpGet("detalle/{operacionId}")]
        public async Task<IActionResult> ObtenerDetalleOperacion(int operacionId)
        {
            try
            {
                var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(operacionId);
                if (operacion == null)
                    return NotFound(new { mensaje = "Operación no encontrada." });

                var grupos = await _grupoService.ObtenerPorOperacionAsync(operacionId);

                var respuesta = new
                {
                    operacion_id = operacionId,
                    bodega = operacion.Bodega,
                    numero_conteo = operacion.NumeroConteo,
                    grupos = new List<object>()
                };

                foreach (var grupo in grupos)
                {
                    var grupoObj = new
                    {
                        grupo_id = grupo.Id,
                        nombre = grupo.Nombre,
                        ubicaciones = new List<object>()
                    };

                    var ubicaciones = await _grupoUbicacionRepo.ObtenerUbicacionesPorGrupoAsync(grupo.Id);
                    var itemsConteo = await _itemsRepo.ObtenerAsync(
                        operacionId,
                        grupo.Id,
                        operacion.NumeroConteo
                    );

                    if (ubicaciones.Count == 0)
                    {
                        grupoObj.ubicaciones.Add(new { ubicacion = "", items = itemsConteo });
                    }
                    else
                    {
                        foreach (var ubicacion in ubicaciones)
                        {
                            grupoObj.ubicaciones.Add(new
                            {
                                ubicacion,
                                items = itemsConteo.Where(i => i.Ubicacion.Trim() == ubicacion.Trim()).ToList()
                            });
                        }
                    }

                    respuesta.grupos.Add(grupoObj);
                }

                return Ok(respuesta);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalle de la operación");
                return StatusCode(500, new { mensaje = "Error al obtener detalle de la operación.", detalle = ex.Message });
            }
        }

        [HttpGet("conteo/actual")]
        public async Task<IActionResult> ObtenerConteoActual()
        {
            try
            {
                var conteos = await _conteoRepo.ObtenerPendientesAsync();
                if (conteos == null || conteos.Count == 0)
                    return NotFound(new { mensaje = "No hay conteos pendientes." });

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
                        numeroConteo = conteo.NumeroConteo,
                        items
                    });
                }

                return Ok(respuesta);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conteo actual");
                return StatusCode(500, new { mensaje = "Error al obtener conteo actual.", detalle = ex.Message });
            }
        }

        [HttpGet("conteo/por-grupo/{operacionId:int}/{grupoId:int}")]
        public async Task<IActionResult> ObtenerConteoPorGrupo(int operacionId, int grupoId)
        {
            try
            {
                var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(operacionId);
                if (operacion == null)
                    return NotFound(new { mensaje = "Operación no encontrada." });

                var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
                if (grupo == null)
                    return NotFound(new { mensaje = "Grupo no encontrado." });

                var pendientes = await _conteoRepo.ObtenerPendientesAsync();
                var conteo = pendientes
                    .Where(c => c.OperacionId == operacionId && c.GrupoId == grupoId)
                    .OrderByDescending(c => c.FechaCreacion)
                    .FirstOrDefault();

                if (conteo == null)
                    return NotFound(new { mensaje = "No hay conteo activo para este grupo." });

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
                    items
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conteo por grupo");
                return StatusCode(500, new { mensaje = "Error al obtener conteo por grupo.", detalle = ex.Message });
            }
        }
    }
}
