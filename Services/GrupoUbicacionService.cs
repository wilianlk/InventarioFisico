using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IBM.Data.Db2;
using InventarioFisico.Models;
using InventarioFisico.Repositories;

namespace InventarioFisico.Services
{
    public class GrupoUbicacionService
    {
        private readonly GrupoUbicacionRepository _repo;

        public GrupoUbicacionService(GrupoUbicacionRepository repo)
        {
            _repo = repo;
        }

        public Task<List<GrupoUbicacion>> ObtenerPorGrupoAsync(int grupoId)
        {
            return _repo.ObtenerPorGrupoAsync(grupoId);
        }

        public async Task AgregarAsync(int grupoId, string ubicacion)
        {
            ubicacion = ubicacion.Trim().ToUpper();

            var ubicacionesGrupo = await _repo.ObtenerUbicacionesPorGrupoAsync(grupoId);
            if (ubicacionesGrupo.Exists(u => u.Trim().ToUpper() == ubicacion))
                throw new InvalidOperationException(
                    $"La ubicación {ubicacion} ya está asignada a este grupo."
                );

            var u = new GrupoUbicacion
            {
                GrupoId = grupoId,
                Ubicacion = ubicacion
            };

            await _repo.AgregarAsync(u);
        }

        public async Task<List<string>> ObtenerRangoUbicacionesAsync(string desde, string hasta)
        {
            return await _repo.ObtenerRangoUbicacionesAsync(
                desde.Trim().ToUpper(),
                hasta.Trim().ToUpper()
            );
        }

        public UbicacionInterpretada InterpretarUbicacion(string bodega, string ubicacion)
        {
            if (string.IsNullOrWhiteSpace(bodega))
                throw new InvalidOperationException("La bodega es obligatoria para interpretar la ubicación.");

            if (string.IsNullOrWhiteSpace(ubicacion))
                throw new InvalidOperationException("La ubicación es obligatoria.");

            ubicacion = ubicacion.Trim().ToUpper();

            if (bodega == "13M")
            {
                return new UbicacionInterpretada
                {
                    Bodega = bodega,
                    RackPasillo = ubicacion.Substring(0, 2),
                    Lado = ubicacion.Substring(2, 1),
                    Altura = ubicacion.Substring(3, 1),
                    Posicion = ubicacion.Substring(4, 2)
                };
            }

            if (bodega == "11")
            {
                return new UbicacionInterpretada
                {
                    Bodega = bodega,
                    RackPasillo = ubicacion.Substring(0, 1),
                    Lado = null,
                    Altura = ubicacion.Substring(1, 1),
                    Posicion = ubicacion.Substring(2, 2)
                };
            }

            throw new InvalidOperationException(
                $"No existe nomenclatura definida para la bodega {bodega}."
            );
        }

        public Task EliminarAsync(int grupoId, string ubicacion)
        {
            return _repo.EliminarAsync(grupoId, ubicacion.Trim().ToUpper());
        }
    }

    public class UbicacionInterpretada
    {
        public string Bodega { get; set; }
        public string RackPasillo { get; set; }
        public string Lado { get; set; }
        public string Altura { get; set; }
        public string Posicion { get; set; }
    }
}
