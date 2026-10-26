namespace WP_Inventario.Modelos
{
    public class OrdenCompraDetalle
    {
        public int id_detalle { get; set; }
        public int id_orden_compra { get; set; }
        public int id_material { get; set; }
        public string material_nombre { get; set; }
        public decimal cantidad_pedida { get; set; }
        public decimal cantidad_recibida { get; set; }
        public decimal cantidad_faltante { get; set; }
        public decimal cantidad_recibida_anterior { get; set; }
        public string observacion_item { get; set; }
        public decimal stock_al_crear { get; set; }
        public decimal? stock_despues_recepcion { get; set; }

        public string estado_item
        {
            get { return cantidad_faltante == 0 ? "Completo" : "Incompleto"; }
        }
    }
}
