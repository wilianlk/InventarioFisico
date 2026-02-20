using System.Linq;
using System.Threading.Tasks;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class ValidacionCierreService
    {
        private readonly OperacionConteoRepository _conteoRepo;

        public ValidacionCierreService(
            OperacionConteoRepository conteoRepo)
        {
            _conteoRepo = conteoRepo;
        }

        public async Task ValidarAsync(int operacionId)
        {
            var conteos = await _conteoRepo.ObtenerPorOperacionAsync(operacionId);

            if (conteos == null || conteos.Count == 0)
                throw new System.InvalidOperationException(
                    "No se puede cerrar la operación: no existen conteos generados."
                );

            if (conteos.Any(c => c.Estado != "CERRADO"))
                throw new System.InvalidOperationException(
                    "No se puede cerrar la operación: existen conteos abiertos."
                );
        }
    }
}
