using System;

namespace WP_Inventario.Modelos
{
    public class ConteoInventario
    {
        public int id_conteo { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string usuario_inicio { get; set; }
        public string usuario_fin { get; set; }
        public string estado { get; set; }
        public string observaciones { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }

        public int cantidad_materiales { get; set; }
        public int cantidad_diferencias { get; set; }

        public string EstadoDescripcion
        {
            get
            {
                if (string.IsNullOrEmpty(estado)) return "-";
                return estado.ToUpper();
            }
        }
    }
}
