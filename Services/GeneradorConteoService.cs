using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class GeneradorConteoService
    {
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly OperacionConteoItemsRepository _itemsRepo;
        private readonly GrupoUbicacionService _ubicacionService;
        private readonly GrupoConteoService _grupoService;

        public GeneradorConteoService(
            OperacionConteoRepository conteoRepo,
            OperacionConteoItemsRepository itemsRepo,
            GrupoUbicacionService ubicacionService,
            GrupoConteoService grupoService)
        {
            _conteoRepo = conteoRepo;
            _itemsRepo = itemsRepo;
            _ubicacionService = ubicacionService;
            _grupoService = grupoService;
        }

        public async Task GenerarConteosInicialesAsync(int operacionId, int numeroConteo)
        {
            var grupos = await _grupoService.ObtenerPorOperacionAsync(operacionId);

            foreach (var grupo in grupos)
            {
                var existente = await _conteoRepo.ObtenerPorOperacionGrupoNumeroAsync(
                    operacionId,
                    grupo.Id,
                    numeroConteo
                );

                if (existente != null)
                    continue;

                var conteo = new OperacionConteo
                {
                    OperacionId = operacionId,
                    GrupoId = grupo.Id,
                    NumeroConteo = numeroConteo,
                    Estado = "EN_CONTEO"
                };

                var conteoId = await _conteoRepo.CrearAsync(conteo);

                var items = await _ubicacionService.ObtenerAsync(grupo.Id);

                var itemsAInsertar = new List<OperacionConteoItem>();
                var clavesInsertadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var it in items)
                {
                    var clave =
                        $"{it.Item?.Trim().ToUpper()}|" +
                        $"{it.Ubicacion?.Trim().ToUpper()}|" +
                        $"{it.Lote?.Trim().ToUpper()}";

                    if (!clavesInsertadas.Add(clave))
                        continue;

                    itemsAInsertar.Add(new OperacionConteoItem
                    {
                        OperacionId = operacionId,
                        GrupoId = grupo.Id,
                        NumeroConteo = numeroConteo,
                        CodigoItem = it.Item,
                        Prod = it.Prod,
                        Descripcion = it.Descripcion,
                        Udm = it.Udm,
                        Etiqueta = it.Etiqueta,
                        Lote = it.Lote,
                        Costo = it.Costo,
                        CantidadSistema = it.CantidadSistema,
                        CantidadContada = 0,
                        Ubicacion = it.Ubicacion,
                        Bodega = it.Bodega,
                        Cmpy = it.Cmpy,
                        ConteoId = conteoId
                    });
                }

                if (itemsAInsertar.Count > 0)
                    await _itemsRepo.InsertarAsync(itemsAInsertar);
            }
        }
    }
}
