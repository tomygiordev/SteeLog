using System;

namespace WP_Inventario.Modelos
{
    public class HistorialMovimiento
    {
        public int id_movimiento { get; set; }
        public int? id_movimientomaterial { get; set; }
        public int? id_material { get; set; }
        public DateTime fecha { get; set; }
        public string nroremito { get; set; }
        public string tipo_movimiento { get; set; }
        public string material { get; set; }
        public decimal cantidad { get; set; }
        public string origen { get; set; }
        public string destino { get; set; }
        public string observaciones { get; set; }
        public bool esrecorte { get; set; }
        public decimal? medida_recorte { get; set; }
        public bool esinsumo { get; set; }
        public bool es_compra_eventual { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal precio_total { get; set; }
        public decimal total => cantidad * precio_unitario;
    }
}
