using InventarioFisico.Models;

namespace InventarioFisico.Services
{
    public class UbicacionParserService
    {
        public UbicacionParsed Parse(string ubicacion)
        {
            var pasillo = ubicacion.Substring(0, 2);
            var lado = ubicacion.Substring(2, 1);
            var altura = int.Parse(ubicacion.Substring(3, 1));
            var posicion = int.Parse(ubicacion.Substring(4));

            return new UbicacionParsed
            {
                Original = ubicacion,
                Pasillo = pasillo,
                Lado = lado,
                Altura = altura,
                Posicion = posicion
            };
        }

        public bool UbicacionDentroDeRango(UbicacionParsed u, BloqueConteo b)
        {
            if (u.Pasillo != b.Pasillo) return false;
            if (u.Lado != b.Lado) return false;
            if (u.Altura < b.AlturaInicio || u.Altura > b.AlturaFin) return false;
            if (u.Posicion < b.PosicionInicio || u.Posicion > b.PosicionFin) return false;
            return true;
        }
    }
}
