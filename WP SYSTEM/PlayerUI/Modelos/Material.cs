using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WP_Inventario.Modelos
{
    public class FiltroMaterialParams
    {
        public string Nombre { get; set; }
        public decimal? DesdeCantidad { get; set; }
        public decimal? HastaCantidad { get; set; }
        public List<int> IdsCategoria { get; set; } = new List<int>();
        public List<int> IdsProveedor { get; set; } = new List<int>();
        public bool SoloStockBajo { get; set; }
    }

    public class Material
    {
        public int id { set; get; }
        public string nombre { set; get; }
        public string unidad { set; get; }
        public int id_categoria { get; set; }
        public string nombre_categoria { get; set; }
        public decimal precioUnitario { get; set; }
        public decimal valorTotal => stockActual * precioUnitario;
        public decimal stockActual { get; set; }
        public decimal stockMinimo { get; set; }
        public decimal cantidadUsada { get; set; }
        public int cantidadMovimientos { get; set; }

        [Browsable(false)]
        public string reporteSeccion { get; set; }

        [Browsable(false)]
        public int reporteOrdenSeccion { get; set; }

        [Browsable(false)]
        public int reporteOrdenMaterial { get; set; }

        [Browsable(false)]
        public string reporteSeccionCabecera { get; set; }

        [Browsable(false)]
        public bool esFilaSeparadora { get; set; }

        [Browsable(false)]
        public string tituloSeparador { get; set; }
    }
}
