using System;

namespace WP_Inventario.Modelos
{
    public class Insumo
    {
        public Insumo() { }
        public int id { get; set; }
        public string nombre { get; set; }
        public int? id_obra { get; set; }
        public string nombre_obra { get; set; }
        public bool consumido { get; set; }
        public string observaciones { get; set; }
    }
}
