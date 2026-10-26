using System;

namespace WP_Inventario.Modelos
{
    public class ConteoInventarioDetalle
    {
        public int id_detalle { get; set; }
        public int id_conteo { get; set; }
        public int id_material { get; set; }
        public string material_nombre_snapshot { get; set; }
        public string material_categoria_snapshot { get; set; }
        public decimal cantidad_contada { get; set; }
        public DateTime fecha_carga { get; set; }
    }
}
