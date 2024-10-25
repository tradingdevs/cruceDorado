using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apiBinance.Bussines
{
    public class MediaMovil
    {
        private List<decimal> _precios; // Lista para almacenar los precios

        public MediaMovil()
        {
            _precios = new List<decimal>();
        }
        // Propiedad para la media móvil de 50 periodos
        public decimal MediaCorta
        {
            get
            {
                if (_precios.Count >= 50)
                    return _precios.TakeLast(50).Average();
                else
                    return 0; // No se puede calcular si no hay suficientes datos
            }
        }

        // Propiedad para la media móvil de 200 periodos
        public decimal MediaLarga
        {
            get
            {
                if (_precios.Count >= 200)
                    return _precios.TakeLast(200).Average();
                else
                    return 0; // No se puede calcular si no hay suficientes datos
            }
        }

        // Método para agregar un nuevo precio
        public void AgregarPrecio(decimal precio)
        {
            _precios.Add(precio);

            // Mantener solo los últimos 200 precios para optimizar memoria
            if (_precios.Count > 200)
            {
                _precios.RemoveAt(0);
            }
        }
    }
}
