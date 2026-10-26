using System;

namespace WP_Inventario.Modelos
{
    public class CanaletaPlegadoFoto
    {
        public int id { get; set; }
        public int canaleta_plegado_id { get; set; }
        public string bucket_id { get; set; }
        public string object_path { get; set; }
        public string nombre_archivo { get; set; }
        public string content_type { get; set; }
        public long tamanio_bytes { get; set; }
        public int orden { get; set; }
        public DateTime fecha_creacion { get; set; }
    }
}
