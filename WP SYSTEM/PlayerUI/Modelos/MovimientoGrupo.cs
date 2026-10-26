using System;
using System.Collections.Generic;

namespace WP_Inventario.Modelos
{
    public class MovimientoGrupo
    {
        public int id_movimiento { get; set; }
        public DateTime fecha { get; set; }
        public string nroremito { get; set; }
        public string tipo_movimiento { get; set; }
        public string origen { get; set; }
        public string destino { get; set; }
        public string observaciones { get; set; }
        public decimal precio_total { get; set; }
        
        public List<HistorialMovimiento> Detalles { get; set; } = new List<HistorialMovimiento>();
    }
}
