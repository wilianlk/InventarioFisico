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
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly OperacionConteoItemsRepository _itemsRepo;
        private readonly CerrarConteoService _cerrarConteoService;

        public InventarioController(
            InventarioService inventarioService,
            GrupoConteoService grupoService,
            GeneradorConteoService generadorConteoService,
            OperacionConteoRepository conteoRepo,
            OperacionConteoItemsRepository itemsRepo,
            CerrarConteoService cerrarConteoService)
        {
            _inventarioService = inventarioService;
            _grupoService = grupoService;
            _generadorConteoService = generadorConteoService;
            _conteoRepo = conteoRepo;
            _itemsRepo = itemsRepo;
            _cerrarConteoService = cerrarConteoService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerOperaciones()
        {
            var operaciones = (await _inventarioService.ObtenerOperacionesAsync())
                .OrderByDescending(o => o.Id)
                .ToList();

            var avances = await _itemsRepo.ObtenerAvancePorOperacionAsync();
            var avancesPorConteo = await _itemsRepo.ObtenerAvancePorConteoAsync();
            var conteosTasks = operaciones
                .ToDictionary(
                    operacion => operacion.Id,
                    operacion => _conteoRepo.ObtenerPorOperacionAsync(operacion.Id)
                );

            await Task.WhenAll(conteosTasks.Values);

            var respuesta = operaciones
                .Select(operacion =>
                {
                    var totalItems = 0;
                    var itemsContados = 0;

                    if (avances.TryGetValue(operacion.Id, out var avance))
                    {
                        totalItems = avance.TotalItems;
                        itemsContados = avance.ItemsContados;
                    }

                    var porcentaje = totalItems == 0
                        ? 0
                        : (itemsContados * 100) / totalItems;

                    var conteos = conteosTasks[operacion.Id].Result
                        .OrderBy(c => c.NumeroConteo)
                        .ThenBy(c => c.GrupoId)
                        .GroupBy(c => c.NumeroConteo)
                        .Select(grupoConteo =>
                        {
                            var grupos = grupoConteo
                                .OrderBy(c => c.GrupoId)
                                .Select(c =>
                                {
                                    var totalItemsGrupo = 0;
                                    var itemsContadosGrupo = 0;

                                    if (avancesPorConteo.TryGetValue(c.Id, out var avanceConteo))
                                    {
                                        totalItemsGrupo = avanceConteo.TotalItems;
                                        itemsContadosGrupo = avanceConteo.ItemsContados;
                                    }

                                    var porcentajeGrupo = totalItemsGrupo == 0
                                        ? 0
                                        : (itemsContadosGrupo * 100) / totalItemsGrupo;

                                    return new
                                    {
                                        id = c.Id,
                                        grupoId = c.GrupoId,
                                        grupo = c.NombreGrupo,
                                        numeroConteo = c.NumeroConteo,
                                        estado = c.Estado,
                                        fechaCreacion = c.FechaCreacion,
                                        porcentajes = new
                                        {
                                            totalItems = totalItemsGrupo,
                                            itemsContados = itemsContadosGrupo,
                                            porcentaje = porcentajeGrupo
                                        }
                                    };
                                })
                                .ToList();

                            return new
                            {
                                numeroConteo = grupoConteo.Key,
                                grupos
                            };
                        })
                        .ToList();

                    return new
                    {
                        operacion = new
                        {
                            id = operacion.Id,
                            bodega = operacion.Bodega,
                            fecha = operacion.Fecha,
                            observaciones = operacion.Observaciones,
                            estado = operacion.Estado,
                            usuarioCreacion = operacion.UsuarioCreacion,
                            fechaCreacion = operacion.FechaCreacion,
                            numeroConteo = operacion.NumeroConteo,
                            gruposIds = operacion.GruposIds,
                            porcentajes = new
                            {
                                totalItems,
                                itemsContados,
                                porcentaje
                            },
                            conteos
                        }
                    };
                })
                .ToList();

            return Ok(respuesta);
        }

        [HttpGet("porcentajes")]
        public async Task<IActionResult> ObtenerPorcentajesOperaciones()
        {
            var operaciones = (await _inventarioService.ObtenerOperacionesAsync())
                .OrderByDescending(o => o.Id)
                .ToList();

            var avances = await _itemsRepo.ObtenerAvancePorOperacionAsync();
            var avancesPorConteo = await _itemsRepo.ObtenerAvancePorConteoAsync();
            var conteosTasks = operaciones
                .ToDictionary(
                    operacion => operacion.Id,
                    operacion => _conteoRepo.ObtenerPorOperacionAsync(operacion.Id)
                );

            await Task.WhenAll(conteosTasks.Values);

            var respuesta = operaciones
                .Select(operacion =>
                {
                    var totalItems = 0;
                    var itemsContados = 0;

                    if (avances.TryGetValue(operacion.Id, out var avance))
                    {
                        totalItems = avance.TotalItems;
                        itemsContados = avance.ItemsContados;
                    }

                    var porcentaje = totalItems == 0
                        ? 0
                        : (itemsContados * 100) / totalItems;

                    var conteos = conteosTasks[operacion.Id].Result
                        .OrderBy(c => c.NumeroConteo)
                        .ThenBy(c => c.GrupoId)
                        .GroupBy(c => c.NumeroConteo)
                        .Select(grupoConteo =>
                        {
                            var grupos = grupoConteo
                                .OrderBy(c => c.GrupoId)
                                .Select(c =>
                                {
                                    var totalItemsGrupo = 0;
                                    var itemsContadosGrupo = 0;

                                    if (avancesPorConteo.TryGetValue(c.Id, out var avanceConteo))
                                    {
                                        totalItemsGrupo = avanceConteo.TotalItems;
                                        itemsContadosGrupo = avanceConteo.ItemsContados;
                                    }

                                    var porcentajeGrupo = totalItemsGrupo == 0
                                        ? 0
                                        : (itemsContadosGrupo * 100) / totalItemsGrupo;

                                    return new
                                    {
                                        conteoId = c.Id,
                                        grupoId = c.GrupoId,
                                        grupo = c.NombreGrupo,
                                        numeroConteo = c.NumeroConteo,
                                        porcentajes = new
                                        {
                                            totalItems = totalItemsGrupo,
                                            itemsContados = itemsContadosGrupo,
                                            porcentaje = porcentajeGrupo
                                        }
                                    };
                                })
                                .ToList();

                            return new
                            {
                                numeroConteo = grupoConteo.Key,
                                grupos
                            };
                        })
                        .ToList();

                    return new
                    {
                        operacion = new
                        {
                            id = operacion.Id,
                            porcentajes = new
                            {
                                totalItems,
                                itemsContados,
                                porcentaje
                            },
                            conteos
                        }
                    };
                })
                .ToList();

            return Ok(respuesta);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerOperacionPorId(int id)
        {
            var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(id);
            if (operacion == null)
                return NotFound(new { mensaje = "Operacion no encontrada." });

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

                return Ok(new { mensaje = "Operacion creada correctamente.", id = operacionId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("conteo/agregar/{operacionId:int}")]
        [HttpPut("conteo/editar/{operacionId:int}")]
        public async Task<IActionResult> AgregarConteoOperacion(int operacionId, [FromBody] InventarioEditarConteosRequest request)
        {
            if (request == null)
                return BadRequest(new { mensaje = "Debe enviar el numero de conteo." });

            var operacion = await _inventarioService.ObtenerOperacionPorIdAsync(operacionId);
            if (operacion == null)
                return NotFound(new { mensaje = "Operacion no encontrada." });

            try
            {
                if (request.NumeroConteo < 1 || request.NumeroConteo > 3)
                    return BadRequest(new { mensaje = "El número de conteo debe estar entre 1 y 3." });


                var gruposSolicitados = request.GruposIds?
                    .Distinct()
                    .ToList() ?? new List<int>();

                if (gruposSolicitados.Count > 0)
                {
                    await _inventarioService.ValidarCreacionAsync(new InventarioOperacion
                    {
                        GruposIds = gruposSolicitados
                    });

                    var gruposActuales = await _grupoService.ObtenerPorOperacionAsync(operacionId);
                    var gruposActualesSet = gruposActuales
                        .Select(g => g.Id)
                        .ToHashSet();

                    foreach (var grupoId in gruposSolicitados)
                    {
                        if (gruposActualesSet.Contains(grupoId))
                            continue;

                        await _grupoService.AsociarOperacionGrupoAsync(operacionId, grupoId);
                        gruposActualesSet.Add(grupoId);
                    }
                }

                var (numeroAnterior, numeroNuevo) = await _inventarioService.ActualizarNumeroConteoAsync(operacionId, request.NumeroConteo);

                await _generadorConteoService.GenerarConteosInicialesAsync(operacionId, request.NumeroConteo);

                return Ok(new
                {
                    mensaje = "Conteo agregado y conteos generados correctamente.",
                    operacionId,
                    numeroConteoAnterior = numeroAnterior,
                    numeroConteoActual = numeroNuevo
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPut("cerrar/{id:int}")]
        public async Task<IActionResult> CerrarOperacion(int id)
        {
            try
            {
                var conteos = await _conteoRepo.ObtenerPorOperacionAsync(id);
                var conteosCerrados = 0;

                foreach (var conteo in conteos)
                {
                    var estabaAbierto = conteo.Estado != "CERRADO";
                    await _cerrarConteoService.CerrarConteoAsync(conteo.Id);
                    if (estabaAbierto)
                        conteosCerrados++;
                }

                await _inventarioService.CerrarOperacionAsync(id);
                await _cerrarConteoService.ActualizarOperacionConsolidadaSiAplicaAsync(id);
                return Ok(new
                {
                    mensaje = "Operacion cerrada exitosamente.",
                    conteosCerrados
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("eliminar/{id:int}")]
        public async Task<IActionResult> EliminarOperacion(int id)
        {
            try
            {
                await _inventarioService.EliminarOperacionAsync(id);
                return Ok(new { mensaje = "Operacion eliminada correctamente." });
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
                return NotFound(new { mensaje = "No hay conteos en conteo." });

            var respuesta = new List<object>();

            foreach (var conteo in conteos)
            {
                var grupo = await _grupoService.ObtenerPorIdAsync(conteo.GrupoId);
                if (grupo == null)
                    continue;

                var items = await _itemsRepo.ObtenerPorConteoAsync(conteo.Id);

                if (items.Count == 0)
                {
                    // Compatibilidad con registros legacy sin oc_id.
                    items = await _itemsRepo.ObtenerAsync(
                        conteo.OperacionId,
                        conteo.GrupoId,
                        conteo.NumeroConteo
                    );
                }

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

        [HttpGet("conteo/actual/kpis")]
        public async Task<IActionResult> ObtenerKpisConteoActual()
        {
            var conteos = await _conteoRepo.ObtenerAbiertosAsync();
            var operaciones = await _inventarioService.ObtenerOperacionesAsync();
            var operacionesConsolidadas = operaciones.Count(o =>
                string.Equals((o.Estado ?? string.Empty).Trim(), "CONSOLIDADA", StringComparison.OrdinalIgnoreCase));

            if (conteos.Count == 0)
            {
                return Ok(new
                {
                    conteosActivos = 0,
                    itemsContados = 0,
                    noEncontrados = 0,
                    operacionesConsolidadas
                });
            }

            var itemsContados = 0;
            var noEncontrados = 0;

            foreach (var conteo in conteos)
            {
                var items = await _itemsRepo.ObtenerPorConteoAsync(conteo.Id);

                if (items.Count == 0)
                {
                    // Compatibilidad con registros legacy sin oc_id.
                    items = await _itemsRepo.ObtenerAsync(
                        conteo.OperacionId,
                        conteo.GrupoId,
                        conteo.NumeroConteo
                    );
                }

                // CantidadContada es int (no nullable) en backend; se toma > 0 como "contado".
                itemsContados += items.Count(i => i.CantidadContada > 0);
                noEncontrados += items.Count(i => i.NoEncontrado);
            }

            return Ok(new
            {
                conteosActivos = conteos.Count(c => c.Estado == "EN_CONTEO"),
                itemsContados,
                noEncontrados,
                operacionesConsolidadas
            });
        }

        [HttpGet("conteo/por-grupo/{operacionId:int}/{grupoId:int}")]
        public async Task<IActionResult> ObtenerConteoPorGrupo(int operacionId, int grupoId)
        {
            var grupo = await _grupoService.ObtenerPorIdAsync(grupoId);
            if (grupo == null)
                return NotFound(new { mensaje = "Grupo no encontrado." });

            var conteo = await _conteoRepo.ObtenerAbiertoPorOperacionYGrupoAsync(operacionId, grupoId);
            if (conteo == null)
                return NotFound(new { mensaje = "No hay conteo en curso para este grupo." });

            var items = await _itemsRepo.ObtenerPorConteoAsync(conteo.Id);

            if (items.Count == 0)
            {
                // Compatibilidad con registros legacy sin oc_id.
                items = await _itemsRepo.ObtenerAsync(
                    conteo.OperacionId,
                    conteo.GrupoId,
                    conteo.NumeroConteo
                );
            }

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

        [HttpGet("conteo/{conteoId:int}")]
        public async Task<IActionResult> ObtenerConteoPorId(int conteoId)
        {
            var conteo = await _conteoRepo.ObtenerPorIdAsync(conteoId);
            if (conteo != null)
            {
                var grupo = await _grupoService.ObtenerPorIdAsync(conteo.GrupoId);
                var grupoNombre = grupo?.Nombre ?? string.Empty;

                var items = await _itemsRepo.ObtenerPorConteoAsync(conteo.Id);

                if (items.Count == 0)
                {
                    // Compatibilidad con registros legacy sin oc_id.
                    items = await _itemsRepo.ObtenerAsync(
                        conteo.OperacionId,
                        conteo.GrupoId,
                        conteo.NumeroConteo
                    );
                }

                return Ok(new
                {
                    operacionId = conteo.OperacionId,
                    grupoId = conteo.GrupoId,
                    grupo = grupoNombre,
                    conteoId = conteo.Id,
                    numeroConteo = conteo.NumeroConteo,
                    estadoConteo = conteo.Estado,
                    items
                });
            }

            var itemsPorConteo = await _itemsRepo.ObtenerPorConteoAsync(conteoId);
            if (itemsPorConteo.Count == 0)
                return NotFound(new { mensaje = "Conteo no encontrado." });

            var primerItem = itemsPorConteo[0];
            var grupoFallback = await _grupoService.ObtenerPorIdAsync(primerItem.GrupoId);
            var grupoNombreFallback = grupoFallback?.Nombre ?? string.Empty;

            return Ok(new
            {
                operacionId = primerItem.OperacionId,
                grupoId = primerItem.GrupoId,
                grupo = grupoNombreFallback,
                conteoId,
                numeroConteo = primerItem.NumeroConteo,
                estadoConteo = "NO_REGISTRADO_EN_OPERACION_CONTEO",
                items = itemsPorConteo
            });
        }

        [HttpPut("conteo/{conteoId:int}/finalizar")]
        public async Task<IActionResult> FinalizarConteo(int conteoId)
        {
            return await FinalizarConteoInterno(conteoId, null, null);
        }

        [HttpPut("cerrar/conteo/{conteoId:int}")]
        public async Task<IActionResult> CerrarConteoIndividual(int conteoId)
        {
            return await FinalizarConteoInterno(conteoId, null, null);
        }

        [HttpPut("operacion/{operacionId:int}/conteo/{numeroConteo:int}/{conteoId:int}/finalizar")]
        public async Task<IActionResult> FinalizarConteoValidado(int operacionId, int numeroConteo, int conteoId)
        {
            return await FinalizarConteoInterno(conteoId, operacionId, numeroConteo);
        }

        private async Task<IActionResult> FinalizarConteoInterno(int conteoId, int? operacionId, int? numeroConteo)
        {
            var conteo = await _conteoRepo.ObtenerPorIdAsync(conteoId);
            if (conteo == null)
                return NotFound(new { mensaje = "Conteo no encontrado." });

            if (operacionId.HasValue && conteo.OperacionId != operacionId.Value)
            {
                return BadRequest(new
                {
                    mensaje = "El conteo no pertenece a la operacion indicada."
                });
            }

            if (numeroConteo.HasValue && conteo.NumeroConteo != numeroConteo.Value)
            {
                return BadRequest(new
                {
                    mensaje = "El conteo no pertenece al numero de conteo indicado."
                });
            }

            var yaCerrado = conteo.Estado == "CERRADO";

            await _cerrarConteoService.CerrarConteoAsync(conteoId);
            if (yaCerrado)
                return Ok(new { mensaje = "Conteo ya estaba cerrado. Se valido consolidacion." });

            return Ok(new { mensaje = "Conteo cerrado correctamente." });
        }

        [HttpDelete("conteo/eliminar/{conteoId:int}")]
        public async Task<IActionResult> EliminarConteo(int conteoId)
        {
            var conteo = await _conteoRepo.ObtenerPorIdAsync(conteoId);
            if (conteo == null)
                return NotFound(new { mensaje = "Conteo no encontrado." });

            await _conteoRepo.EliminarConteoAsync(
                conteoId,
                conteo.OperacionId,
                conteo.GrupoId,
                conteo.NumeroConteo
            );

            return Ok(new
            {
                mensaje = "Conteo eliminado correctamente.",
                conteoId
            });
        }

    }
}
