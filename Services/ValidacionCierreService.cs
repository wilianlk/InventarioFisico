using System.Linq;
using System.Threading.Tasks;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class ValidacionCierreService
    {
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly OperacionConteoItemsRepository _itemsRepo;
        private readonly GrupoConteoService _grupoService;
        private readonly GrupoUbicacionRepository _ubicacionRepo;

        public ValidacionCierreService(
            OperacionConteoRepository conteoRepo,
            OperacionConteoItemsRepository itemsRepo,
            GrupoConteoService grupoService,
            GrupoUbicacionRepository ubicacionRepo)
        {
            _conteoRepo = conteoRepo;
            _itemsRepo = itemsRepo;
            _grupoService = grupoService;
            _ubicacionRepo = ubicacionRepo;
        }

        public async Task ValidarAsync(int operacionId)
        {
            var grupos = await _grupoService.ObtenerPorOperacionAsync(operacionId);
            var conteos = await _conteoRepo.ObtenerPorOperacionAsync(operacionId);

            if (conteos.Count == 0)
                throw new System.InvalidOperationException(
                    "No se puede cerrar la operación: no existen conteos generados."
                );

            foreach (var grupo in grupos)
            {
                var ubicaciones = await _ubicacionRepo.ObtenerUbicacionesPorGrupoAsync(grupo.Id);
                if (ubicaciones == null || ubicaciones.Count == 0)
                    throw new System.InvalidOperationException(
                        $"No se puede cerrar la operación: el grupo '{grupo.Nombre}' no tiene ubicaciones asignadas."
                    );

                var conteosGrupo = conteos.Where(c => c.GrupoId == grupo.Id).ToList();
                if (conteosGrupo.Count == 0)
                    throw new System.InvalidOperationException(
                        $"No se puede cerrar la operación: el grupo '{grupo.Nombre}' no tiene conteos generados."
                    );

                foreach (var c in conteosGrupo)
                {
                    var items = await _itemsRepo.ObtenerPorConteoAsync(c.Id);
                    if (items == null || items.Count == 0)
                        throw new System.InvalidOperationException(
                            $"No se puede cerrar la operación: el grupo '{grupo.Nombre}' tiene conteos sin ítems del sistema."
                        );
                }
            }
        }
    }
}
