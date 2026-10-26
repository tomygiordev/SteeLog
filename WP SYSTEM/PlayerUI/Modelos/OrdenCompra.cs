using System;
using System.Collections.Generic;

namespace WP_Inventario.Modelos
{
    public class OrdenCompra
    {
        public int id_orden_compra { get; set; }
        public DateTime fecha { get; set; }
        public string nro_orden { get; set; }
        public string nro_remito { get; set; }
        public int id_proveedor { get; set; }
        public string proveedor_nombre { get; set; }
        public int id_destino { get; set; }
        public string destino_nombre { get; set; }
        public decimal costoflete { get; set; }
        public string observaciones { get; set; }
        public string estado { get; set; }
        public string usuario_creacion { get; set; }
        public string usuario_revision { get; set; }
        public DateTime? fecha_revision { get; set; }
        public string observacion_revision { get; set; }
        public int cantidad_items { get; set; }
        public decimal total_cantidad_pedida { get; set; }
        public decimal total_cantidad_recibida { get; set; }
        public List<OrdenCompraDetalle> Detalles { get; set; }

        public OrdenCompra()
        {
            Detalles = new List<OrdenCompraDetalle>();
        }
    }
}
