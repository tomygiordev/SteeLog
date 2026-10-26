using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WP_Inventario.Modelos
{
    public class Movimiento
    {
        public int id_movimiento { get; set; }
        public DateTime fecha { get; set; }
        public string nroremito { get; set; }
        public int id_origen { get; set; }
        public string tipo_origen { get; set; }
        public int id_destino { get; set; }
        public string tipo_destino { get; set; }
        public int id_tipomovimiento { get; set; }
        public int id_obra { get; set; }
        public decimal costoflete { get; set; }
        public int? id_flete { get; set; }
        public string observaciones { get; set; }
        public decimal precio_total { get; set; }
    }
}
