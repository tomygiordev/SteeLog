namespace WP_Inventario.Modelos
{
    public class ReporteOrdenPendienteItem
    {
        public string nro_orden { get; set; }
        public string proveedor { get; set; }
        public string material { get; set; }
        public decimal cantidad_pedida { get; set; }
        public decimal cantidad_recibida { get; set; }
        public decimal cantidad_faltante { get; set; }
        public string estado { get; set; }
    }
}
