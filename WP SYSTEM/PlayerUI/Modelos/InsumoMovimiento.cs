using System;

namespace WP_Inventario.Modelos
{
    public class InsumoMovimiento
    {
        public int id { get; set; }
        public string nombre_insumo { get; set; }
        public string obra_origen { get; set; }
        public string obra_destino { get; set; }
        public DateTime fecha { get; set; }
    }
}
