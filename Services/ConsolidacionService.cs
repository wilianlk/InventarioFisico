using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class ConsolidacionService
    {
        private readonly ConsolidacionRepository _repo;
        private readonly InventarioRepository _inventarioRepo;
        public ConsolidacionService(ConsolidacionRepository repo, InventarioRepository inventarioRepo)
        {
            _repo = repo;
            _inventarioRepo = inventarioRepo;
        }

        public async Task<ConteosFinalizadosResponseDto> ObtenerConteosFinalizadosAsync()
        {
            var items = await _repo.ObtenerConteosFinalizadosAsync();
            var idsOperacion = items
                .Select(i => i.OperacionId)
                .Distinct()
                .ToList();

            var detalleOperaciones = await Task.WhenAll(
                idsOperacion.Select(async id => new
                {
                    Id = id,
                    Operacion = await _inventarioRepo.ObtenerOperacionPorIdAsync(id)
                })
            );

            var detallePorId = detalleOperaciones.ToDictionary(x => x.Id, x => x.Operacion);
            var operaciones = items
                .GroupBy(i => i.OperacionId)
                .OrderBy(g => g.Key)
                .Where(g =>
                {
                    detallePorId.TryGetValue(g.Key, out var operacion);
                    var estadoOperacion = (operacion?.Estado ?? string.Empty).Trim().ToUpperInvariant();
                    return estadoOperacion == "CONSOLIDADA"
                        || estadoOperacion == "FINALIZADA";
                })
                .Select(g =>
                {
                    var estado = g
                        .Select(x => (x.EstadoOperacion ?? string.Empty).Trim())
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                    if (string.IsNullOrWhiteSpace(estado))
                        estado = "CONSOLIDADA";

                    detallePorId.TryGetValue(g.Key, out var operacion);
                    var totalRegistros = g.Count();
                    var totalNoEncontrados = g.Count(x => x.NoEncontrado);
                    var totalReferencias = g
                        .Select(x => $"{x.CodigoItem}|{x.Lote}|{x.Ubicacion}")
                        .Distinct()
                        .Count();
                    var conteosOperacion = g
                        .SelectMany(x => x.Conteos ?? new List<ConteoValorDto>())
                        .Select(x => x.NumeroConteo)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();
                    var numeroConteo = operacion?.NumeroConteo ?? g.Max(x => x.NumeroConteo);
                    var fechaOperacion = operacion != null && operacion.Fecha != DateTime.MinValue
                        ? operacion.Fecha
                        : (DateTime?)null;
                    var fechaCreacion = operacion != null && operacion.FechaCreacion != DateTime.MinValue
                        ? operacion.FechaCreacion
                        : (DateTime?)null;

                    return new ConteosFinalizadosOperacionDto
                    {
                        Cabecera = new ConteosFinalizadosCabeceraDto
                        {
                            OperacionId = g.Key,
                            Estado = estado,
                            Finalizada = string.Equals(estado, "FINALIZADA", StringComparison.OrdinalIgnoreCase),
                            Bodega = operacion?.Bodega?.Trim() ?? string.Empty,
                            FechaOperacion = fechaOperacion,
                            UsuarioCreacion = operacion?.UsuarioCreacion?.Trim() ?? string.Empty,
                            FechaCreacion = fechaCreacion,
                            NumeroConteo = numeroConteo,
                            Observaciones = operacion?.Observaciones?.Trim() ?? string.Empty,
                            TotalRegistros = totalRegistros,
                            TotalNoEncontrados = totalNoEncontrados,
                            TotalReferencias = totalReferencias,
                            Conteos = conteosOperacion,
                            TotalConteos = conteosOperacion.Count
                        },
                        Items = g.ToList()
                    };
                })
                .ToList();

            return new ConteosFinalizadosResponseDto
            {
                Operaciones = operaciones
            };
        }

        public async Task<string> CerrarConsolidacionAsync(int operacionId)
        {
            var operacion = await _inventarioRepo.ObtenerOperacionPorIdAsync(operacionId);
            if (operacion == null)
                throw new InvalidOperationException("La operación no existe.");

            var estadoOperacion = (operacion.Estado ?? string.Empty).Trim().ToUpperInvariant();

            if (estadoOperacion == "ELIMINADA")
                throw new InvalidOperationException("No se puede finalizar una operación eliminada.");

            if (estadoOperacion != "CONSOLIDADA" && estadoOperacion != "FINALIZADA")
                throw new InvalidOperationException("La operación debe estar CONSOLIDADA para finalizar consolidación.");

            var yaFinalizada = await _repo.ConsolidacionFinalizadaAsync(operacionId);
            if (yaFinalizada)
            {
                await _inventarioRepo.ActualizarEstadoAsync(operacionId, "FINALIZADA");
                return "La operación ya se encuentra en estado FINALIZADA";
            }

            var cerrados = await _repo.ConteosCerradosAsync(operacionId);
            if (!cerrados)
                throw new InvalidOperationException("Existen conteos en curso.");

            await _repo.CalcularCantidadFinalAsync(operacionId);
            await _repo.MarcarConsolidacionFinalizadaAsync(operacionId);
            await _inventarioRepo.ActualizarEstadoAsync(operacionId, "FINALIZADA");

            return "Operación finalizada correctamente";
        }

        public async Task<string> GenerarArchivoDI81Async(int operacionId)
        {
            var finalizada = await _repo.ConsolidacionFinalizadaAsync(operacionId);
            if (!finalizada)
                throw new InvalidOperationException("La operación no está FINALIZADA.");

            var data = await _repo.ObtenerConsolidadoParaDI81Async(operacionId);
            if (data.Count == 0)
                data = await _repo.ObtenerConsolidadoFallbackParaDI81Async(operacionId);
            if (data.Count == 0)
                data = await _repo.ObtenerConsolidadoDesdeConteosFinalizadosAsync(operacionId);
            if (data.Count == 0)
                data = await _repo.ObtenerConsolidadoDirectoItemsAsync(operacionId);

            if (data.Count == 0)
            {
                var preview = await _repo.ObtenerConteosFinalizadosAsync();
                foreach (var row in preview)
                {
                    if (row.OperacionId != operacionId)
                        continue;

                    var cantidadDesdeConteos = row.Conteos?
                        .OrderByDescending(c => c.NumeroConteo)
                        .Select(c => c.Cantidad)
                        .FirstOrDefault(c => c.HasValue);

                    var cantidadFinal =
                        row.CantidadFinal ??
                        cantidadDesdeConteos ??
                        row.CantidadConteo3 ??
                        row.CantidadConteo2 ??
                        row.CantidadConteo1 ??
                        0m;

                    data.Add(new Consolidado
                    {
                        OperacionId = operacionId,
                        CodigoProducto = row.CodigoItem,
                        Ubicacion = row.Ubicacion,
                        Lote = row.Lote,
                        CantidadFinal = cantidadFinal
                    });
                }
            }

            if (data.Count == 0)
                throw new InvalidOperationException("No hay datos consolidados.");

            var sb = new StringBuilder();

            foreach (var i in data)
            {
                if (i.CantidadFinal == null)
                    throw new InvalidOperationException("Existen registros sin cantidad final.");

                sb.AppendLine(string.Join(",",
                    i.CodigoProducto,
                    i.Ubicacion,
                    i.Lote,
                    i.CantidadFinal.Value.ToString(CultureInfo.InvariantCulture)
                ));
            }

            return sb.ToString();
        }

    }
}
