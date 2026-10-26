using System.Collections.Generic;

namespace WP_Inventario.Modelos
{
    public class ReporteStockSeccion
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public int orden { get; set; }

        public override string ToString()
        {
            return orden + ". " + nombre;
        }
    }

    public class ReporteStockMaterialConfigurado
    {
        public int idMaterial { get; set; }
        public int orden { get; set; }
    }

    public class ReporteStockMapeo
    {
        public string nombreSeccion { get; set; }
        public int ordenSeccion { get; set; }
        public int ordenMaterial { get; set; }
    }
}
