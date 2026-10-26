using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WP_Inventario.Modelos
{
    public class MovimientoMaterial
    {
        public int id_movimientomaterial { get; set; }
        public decimal cantidad { get; set; }
        public int id_movimiento { get; set; }
        public int? id_material { get; set; }
        public string NombreMaterial { get; set; }
        public string NombreMaterialEventual { get; set; }
        public decimal StockActual { get; set; }
        public bool EsRecorte { get; set; }
        public int? IdRecorte { get; set; }
        public decimal? MedidaRecorte { get; set; }
        public bool EsInsumo { get; set; }
        public int? IdInsumo { get; set; }
        public bool EsCompraEventual { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal Total => cantidad * precio_unitario;
    }
}
