using System;

namespace WP_Inventario.Modelos
{
    public class KardexItem
    {
        public int id_movimiento { get; set; }
        public int? id_material { get; set; }
        public DateTime fecha { get; set; }
        public string tipo_movimiento { get; set; }
        public decimal cantidad { get; set; }
        public string origen { get; set; }
        public string destino { get; set; }
        public decimal saldo { get; set; }
        public string nombre_material { get; set; }
    }
}
