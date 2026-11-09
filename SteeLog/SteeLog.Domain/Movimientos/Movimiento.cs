using System;
using System.Collections.Generic;
using System.Linq;

namespace SteeLog.Domain.Movimientos
{
    public sealed class LineaMovimiento
    {
        public int? MaterialId { get; }
        public int? RecorteId { get; }
        public decimal? MedidaRecorte { get; }
        public decimal Cantidad { get; }
        public bool CompraEventual { get; }
        public string NombreEventual { get; }
        public decimal PrecioUnitario { get; }
        public LineaMovimiento(int? materialId, int? recorteId, decimal? medidaRecorte, decimal cantidad, bool compraEventual, string nombreEventual, decimal precioUnitario)
        {
            if (cantidad <= 0 || cantidad > 999999.9999m || decimal.Round(cantidad, 4) != cantidad)
                throw new ArgumentException("La cantidad debe ser positiva y tener hasta cuatro decimales y seis enteros.");
            if (precioUnitario < 0 || precioUnitario > 999999999999999999.99m) throw new ArgumentException("Precio unitario inválido.");
            if ((materialId.HasValue && materialId <= 0) || (recorteId.HasValue && recorteId <= 0)) throw new ArgumentException("Identidad de material o recorte inválida.");
            if (recorteId.HasValue && (!materialId.HasValue || compraEventual)) throw new ArgumentException("Un recorte requiere su material y no puede ser compra eventual.");
            if (compraEventual && (materialId.HasValue || string.IsNullOrWhiteSpace(nombreEventual))) throw new ArgumentException("La compra eventual requiere nombre y no admite material de inventario.");
            if (!compraEventual && !materialId.HasValue) throw new ArgumentException("La línea requiere un material.");
            MaterialId = materialId; RecorteId = recorteId; MedidaRecorte = medidaRecorte; Cantidad = cantidad; CompraEventual = compraEventual; NombreEventual = nombreEventual; PrecioUnitario = precioUnitario;
        }
    }
    public sealed class Movimiento
    {
        public DateTime Fecha { get; }
        public int OrigenId { get; }
        public string TipoOrigen { get; }
        public int DestinoId { get; }
        public string TipoDestino { get; }
        public int TipoId { get; }
        public string Remito { get; }
        public int? ObraId { get; }
        public decimal CostoFlete { get; }
        public int? FleteId { get; }
        public string Observaciones { get; }
        public decimal PrecioTotal { get; }
        public IReadOnlyList<LineaMovimiento> Lineas { get; }
        public IReadOnlyList<int> Insumos { get; }
        public string OrigenNombre { get; }
        public string DestinoNombre { get; }
        public Movimiento(DateTime fecha, int origenId, string tipoOrigen, int destinoId, string tipoDestino, int tipoId, string remito,
            int? obraId, decimal costoFlete, int? fleteId, string observaciones, decimal precioTotal,
            IEnumerable<LineaMovimiento> lineas, IEnumerable<int> insumos, string origenNombre, string destinoNombre)
        {
            if (origenId <= 0 || destinoId <= 0 || tipoId <= 0 || !TipoValido(tipoOrigen) || !TipoValido(tipoDestino)) throw new ArgumentException("Cabecera de movimiento inválida.");
            if (string.IsNullOrWhiteSpace(remito) || remito.Trim().Length > 50) throw new ArgumentException("El remito es obligatorio y admite hasta 50 caracteres.");
            if (costoFlete < 0 || costoFlete > 99999999.99m || precioTotal < 0 || precioTotal > 999999999999999999.99m) throw new ArgumentException("Importes de movimiento inválidos.");
            if (lineas == null || insumos == null) throw new ArgumentNullException("Los elementos del movimiento son obligatorios.");
            var lista = lineas.ToList(); var activos = insumos.ToList();
            if (lista.Any(x => x == null) || (lista.Count == 0 && activos.Count == 0)) throw new ArgumentException("El movimiento no tiene elementos válidos.");
            if (activos.Any(x => x <= 0) || activos.Distinct().Count() != activos.Count) throw new ArgumentException("Insumos inválidos o duplicados.");
            Fecha=fecha; OrigenId=origenId; TipoOrigen=tipoOrigen; DestinoId=destinoId; TipoDestino=tipoDestino; TipoId=tipoId; Remito=remito.Trim();
            ObraId=obraId; CostoFlete=costoFlete; FleteId=fleteId; Observaciones=observaciones; PrecioTotal=precioTotal;
            Lineas=lista.AsReadOnly(); Insumos=activos.AsReadOnly(); OrigenNombre=origenNombre; DestinoNombre=destinoNombre;
        }
        public const int TipoRegistroInternoId = 11;

        public static void ExigirEditable(int tipoId, string tipoNombre, string impactoExplicito = null)
        {
            if (impactoExplicito == "registro_interno" || tipoId == TipoRegistroInternoId || (tipoNombre ?? string.Empty).ToUpperInvariant().Contains("REGISTRO INTERNO"))
                throw new InvalidOperationException("El registro interno se gestiona desde materiales y no puede modificarse desde movimientos.");
        }
        private static bool TipoValido(string tipo) => tipo == "OBRA" || tipo == "PROVEEDOR" || tipo == "UBICACION";
    }
}
