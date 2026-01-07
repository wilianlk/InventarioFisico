using System;
using System.Threading.Tasks;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class CerrarConteoService
    {
        private readonly OperacionConteoRepository _conteoRepo;
        private readonly ConsolidacionRepository _consolidacionRepo;

        public CerrarConteoService(
            OperacionConteoRepository conteoRepo,
            ConsolidacionRepository consolidacionRepo)
        {
            _conteoRepo = conteoRepo;
            _consolidacionRepo = consolidacionRepo;
        }

        public async Task CerrarConteoAsync(int conteoId)
        {
            var conteo = await _conteoRepo.ObtenerPorIdAsync(conteoId);
            if (conteo == null)
                throw new InvalidOperationException("Conteo no existe");

            if (conteo.Estado == "CERRADO")
                return;

            await _conteoRepo.CerrarConteoAsync(conteoId);

            var cabeceraId = await _consolidacionRepo.InsertarCabeceraDesdeConteoAsync(
                conteo.OperacionId,
                conteo.GrupoId,
                conteo.NumeroConteo
            );

            await _consolidacionRepo.InsertarDetalleDesdeConteoAsync(
                cabeceraId,
                conteo.OperacionId,
                conteo.GrupoId,
                conteo.NumeroConteo
            );
        }
    }
}
