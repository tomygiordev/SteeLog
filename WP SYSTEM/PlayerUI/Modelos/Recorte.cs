using System;

namespace WP_Inventario.Modelos
{
    public class Recorte
    {
        public int id { get; set; }
        public int id_material { get; set; }
        public string nombre_material { get; set; }
        public string nombre_categoria { get; set; }
        public decimal ancho { get; set; }
        public decimal altura { get; set; }
        public decimal medida { get; set; }
        public decimal cantidad { get; set; }
        public string estado { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}
