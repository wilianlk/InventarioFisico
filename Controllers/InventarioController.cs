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
        private readonly CerrarConteoService _cerrarConteoService;
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
            CerrarConteoService cerrarConteoService,
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
            _cerrarConteoService = cerrarConteoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerOperaciones()
        {
            var operaciones = await _inventarioService.ObtenerOperacionesAsync();
            return Ok(operaciones);
        }

        [HttpGet("{id}")]
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
                        mensaje = "Antes de crear esta operación, completa personas y ubicaciones en los siguientes grupos:",
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

        [HttpPut("cerrar/{id}")]
        public async Task<IActionResult> CerrarOperacion(int id)
        {
            await _validacionCierreService.ValidarAsync(id);
            await _inventarioService.CerrarOperacionAsync(id);
            return Ok(new { mensaje = "Operación cerrada exitosamente." });
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
    }
}
