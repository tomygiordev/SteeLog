using System;
using System.Collections.Generic;
namespace SteeLog.Core.Application.Reportes
{
    public class InformeMovimientoDetallePdf
    {
        public int id_movimiento { get; set; }
        public DateTime fecha { get; set; }
        public string nroremito { get; set; }
        public string tipo_movimiento { get; set; }
        public string material { get; set; }
        public decimal cantidad { get; set; }
        public string origen { get; set; }
        public string destino { get; set; }
        public string observaciones { get; set; }
        public decimal costoflete { get; set; }
        public bool esrecorte { get; set; }
        public string medida_recorte { get; set; }
        public bool esinsumo { get; set; }
        public bool es_compra_eventual { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal precio_total { get; set; }
    }

    public class InformeObraResumenPdf
    {
        public int CantRemitos { get; set; }
        public int CantMovimientos { get; set; }
        public int TotalMateriales { get; set; }
        public decimal TotalFletes { get; set; }
        public decimal TotalPendiente { get; set; }
    }

    public class InformeHerramientaDetallePdf
    {
        public int id_herramienta { get; set; }
        public string nombre { get; set; }
        public string estado { get; set; }
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fecha_retorno { get; set; }
        public bool en_obra { get; set; }
    }

    public class InformeInsumoDetallePdf
    {
        public string nombre { get; set; }
        public bool consumido { get; set; }
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fecha_retorno { get; set; }
        public bool en_obra { get; set; }
    }

    public class InformeCanaletaPlegadoDetallePdf
    {
        public int id { get; set; }
        public int id_obra { get; set; }
        public string descripcion { get; set; }
        public decimal precio { get; set; }
        public bool pagado { get; set; }
        public DateTime fecha_creacion { get; set; }
    }

    public sealed class InformeObraSnapshot
    {
        public string NombreObra { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public bool MostrarValorizacion { get; set; }
        public InformeObraResumenPdf Resumen { get; set; }
        public List<InformeMovimientoDetallePdf> Movimientos { get; set; }
        public List<InformeHerramientaDetallePdf> Herramientas { get; set; }
        public List<InformeInsumoDetallePdf> Insumos { get; set; }
        public List<InformeCanaletaPlegadoDetallePdf> Canaletas { get; set; }
    }
    public interface IInformeObraPdf { void Generar(string ruta, InformeObraSnapshot snapshot); }
}
