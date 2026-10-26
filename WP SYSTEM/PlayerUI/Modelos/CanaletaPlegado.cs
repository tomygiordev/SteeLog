using System;

namespace WP_Inventario.Modelos
{
    public class CanaletaPlegado
    {
        public int id { get; set; }
        public int id_obra { get; set; }
        public string nombre_obra { get; set; }
        public string descripcion { get; set; }
        public decimal precio { get; set; }
        public bool pagado { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}
