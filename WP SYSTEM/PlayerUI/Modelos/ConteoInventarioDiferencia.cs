namespace WP_Inventario.Modelos
{
    public class ConteoInventarioDiferencia : ConteoInventarioDetalle
    {
        public decimal? stock_sistema { get; set; }
        public decimal? diferencia { get; set; }

        public string estado_diferencia
        {
            get
            {
                if (!diferencia.HasValue) return "Sin diferencia";
                decimal d = diferencia.Value;
                if (d > 0) return "Sobrante";
                if (d < 0) return "Faltante";
                return "Sin diferencia";
            }
        }
    }
}
