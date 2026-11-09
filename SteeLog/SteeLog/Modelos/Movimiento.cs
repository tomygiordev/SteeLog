using System;

namespace SteeLog.Modelos
{
    public class Movimiento
    {
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
