namespace SteeLog.Modelos
{
    public class MovimientoMaterial
    {
        public decimal cantidad { get; set; }
        public int? id_material { get; set; }
        public string NombreMaterial { get; set; }
        public string NombreMaterialEventual { get; set; }
        public bool EsRecorte { get; set; }
        public int? IdRecorte { get; set; }
        public decimal? AnchoRecorte { get; set; }
        public decimal? AlturaRecorte { get; set; }
        public decimal? MedidaRecorte { get; set; }
        public bool EsInsumo { get; set; }
        public int? IdInsumo { get; set; }
        public bool EsCompraEventual { get; set; }
        public decimal precio_unitario { get; set; }
    }
}
