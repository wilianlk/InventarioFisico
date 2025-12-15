using InventarioFisico.Models;
using InventarioFisico.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                var conteo = new OperacionConteo
                {
                    OperacionId = operacionId,
                    GrupoId = grupo.Id,
                    NumeroConteo = numeroConteo,
                    Estado = "PENDIENTE"
                };

                var conteoIdReal = await _conteoRepo.CrearAsync(conteo);

                var ubicaciones = await _ubicacionService.ObtenerPorGrupoAsync(grupo.Id);
                var itemsAInsertar = new List<OperacionConteoItem>();

                foreach (var u in ubicaciones)
                {
                    var items = await _ubicacionService.ObtenerItemsPorUbicacionAsync(u.Ubicacion);

                    foreach (var it in items)
                    {
                        itemsAInsertar.Add(new OperacionConteoItem
                        {
                            OperacionId = operacionId,
                            GrupoId = grupo.Id,
                            ConteoId = conteoIdReal,
                            CodigoItem = it.Item,
                            Prod = it.Prod,
                            Descripcion = it.Descripcion,
                            Udm = it.Udm,
                            Etiqueta = it.Etiqueta,
                            Lote = it.Lote,
                            Costo = it.Costo,
                            CantidadSistema = it.CantidadSistema,
                            CantidadContada = 0,
                            Ubicacion = u.Ubicacion,
                            Bodega = it.Bodega,
                            Cmpy = it.Cmpy
                        });
                    }
                }

                if (itemsAInsertar.Count > 0)
                    await _itemsRepo.InsertarAsync(itemsAInsertar);
            }
        }
    }
}
