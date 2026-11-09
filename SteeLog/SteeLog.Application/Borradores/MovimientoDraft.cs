using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SteeLog.Core.Application.Borradores
{
    [DataContract]
    public class MovimientoMaterialDraft
    {
        [DataMember] public int? IdMaterial { get; set; }
        [DataMember] public string NombreMaterial { get; set; }
        [DataMember] public string NombreMaterialEventual { get; set; }
        [DataMember] public decimal Cantidad { get; set; }
        [DataMember] public bool EsRecorte { get; set; }
        [DataMember] public int? IdRecorte { get; set; }
        [DataMember] public decimal? AnchoRecorte { get; set; }
        [DataMember] public decimal? AlturaRecorte { get; set; }
        [DataMember] public decimal? MedidaRecorte { get; set; }
        [DataMember] public bool EsInsumo { get; set; }
        [DataMember] public int? IdInsumo { get; set; }
        [DataMember] public bool EsCompraEventual { get; set; }
        [DataMember] public decimal PrecioUnitario { get; set; }
    }

    [DataContract]
    public class MovimientoDraft
    {
        [DataMember] public DateTime FechaGuardado { get; set; }
        [DataMember(EmitDefaultValue = false)] public string Version { get; set; }
        /// <summary>
        /// Remito del intento de guardado con resultado incierto (el movimiento pudo haberse persistido).
        /// null = sin incierto. Los borradores anteriores sin este campo se leen como no inciertos.
        /// </summary>
        [DataMember(EmitDefaultValue = false)] public string ResultadoInciertoRemito { get; set; }
        [DataMember] public int IdTipoMovimiento { get; set; }
        [DataMember] public string NombreTipoMovimiento { get; set; }
        [DataMember] public string NroRemito { get; set; }
        [DataMember] public int IdOrigen { get; set; }
        [DataMember] public string TipoOrigen { get; set; }
        [DataMember] public int IdDestino { get; set; }
        [DataMember] public string TipoDestino { get; set; }
        [DataMember] public int IdObra { get; set; }
        [DataMember] public decimal CostoFlete { get; set; }
        [DataMember] public int? IdFlete { get; set; }
        [DataMember] public string Observaciones { get; set; }
        [DataMember] public DateTime Fecha { get; set; }
        [DataMember] public decimal PrecioTotal { get; set; }
        [DataMember] public List<MovimientoMaterialDraft> Materiales { get; set; }

        public MovimientoDraft()
        {
            Materiales = new List<MovimientoMaterialDraft>();
        }
    }

}
