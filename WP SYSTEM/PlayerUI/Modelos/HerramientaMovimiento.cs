using System;

namespace WP_Inventario.Modelos
{
    public class HerramientaMovimiento
    {
        public int id { get; set; }
        public int id_herramienta { get; set; }
        public string nombre_herramienta { get; set; }
        public string estado { get; set; }
        public string obra_origen { get; set; }
        public string obra_destino { get; set; }
        public DateTime fecha { get; set; }
    }
}
