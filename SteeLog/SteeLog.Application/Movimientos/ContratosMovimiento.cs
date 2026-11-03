using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Domain.Inventario;

namespace SteeLog.Core.Application.Movimientos
{
    public sealed class CabeceraMovimiento
    {
        public DateTime Fecha { get; set; }
        public string Remito { get; set; }
        public int OrigenId { get; set; }
        public string TipoOrigen { get; set; }
        public int DestinoId { get; set; }
        public string TipoDestino { get; set; }
        public int TipoMovimientoId { get; set; }
        public int? ObraId { get; set; }
        public decimal CostoFlete { get; set; }
        public int? FleteId { get; set; }
        public string Observaciones { get; set; }
        public decimal PrecioTotal { get; set; }
    }
    public sealed class LineaMovimiento
    {
        public int? MaterialId { get; set; }
        public string MaterialEventual { get; set; }
        public decimal Cantidad { get; set; }
        public bool EsRecorte { get; set; }
        public int? RecorteId { get; set; }
        public decimal? AnchoRecorte { get; set; }
        public decimal? AlturaRecorte { get; set; }
        public decimal? MedidaRecorte { get; set; }
        public bool EsInsumo { get; set; }
        public int? InsumoId { get; set; }
        public bool EsCompraEventual { get; set; }
        public bool CompraEventual { get => EsCompraEventual; set => EsCompraEventual = value; }
        public decimal PrecioUnitario { get; set; }
        public decimal? ImpactoReal { get; set; }
    }
    public sealed class TrasladoInsumo
    {
        public int InsumoId { get; set; }
        public int? ObraOrigenId { get; set; }
        public int? ObraDestinoId { get; set; }
        public bool ConsumidoOrigen { get; set; }
        public bool ConsumidoDestino { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public bool SnapshotCompleto { get; set; }
        public int HistorialId { get; set; }
    }
    public sealed class MovimientoSolicitud
    {
        public CabeceraMovimiento Cabecera { get; set; }
        public List<LineaMovimiento> Lineas { get; set; } = new List<LineaMovimiento>();
        public List<int> Insumos { get; set; } = new List<int>();
        public string OrigenNombre { get; set; }
        public string DestinoNombre { get; set; }
    }
    public sealed class MovimientoPersistido
    {
        public int Id { get; set; }
        public CabeceraMovimiento Cabecera { get; set; }
        public string TipoNombre { get; set; }
        public List<LineaMovimiento> Lineas { get; set; } = new List<LineaMovimiento>();
        public List<TrasladoInsumo> Insumos { get; set; } = new List<TrasladoInsumo>();
    }
    public sealed class InsumoBloqueado
    {
        public int Id { get; set; }
        public int? ObraId { get; set; }
        public bool Consumido { get; set; }
        public int? UltimoHistorialId { get; set; }
    }
    // Todos los métodos usan la transacción del mismo IUnitOfWork. Se adquieren locks
    // por grupos: cabecera existente, materiales, recortes e insumos; IDs ascendentes.
    public interface IMovimientosEscritura
    {
        Task<MovimientoPersistido> BloquearAsync(int id, CancellationToken token = default);
        Task<string> NombreTipoAsync(int id, CancellationToken token = default);
        Task<IDictionary<int, int>> ObtenerMaterialesRecortesAsync(IReadOnlyList<int> ids, CancellationToken token = default);
        Task<IDictionary<int, decimal>> BloquearRecortesAsync(IReadOnlyList<int> ids, CancellationToken token = default);
        Task<IDictionary<int, InsumoBloqueado>> BloquearInsumosAsync(IReadOnlyList<int> ids, CancellationToken token = default);
        Task<decimal> GuardarRecorteAsync(int id, decimal saldo, CancellationToken token = default);
        Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken token = default);
        Task<int> CrearCabeceraAsync(CabeceraMovimiento cabecera, CancellationToken token = default);
        Task ActualizarCabeceraAsync(int id, CabeceraMovimiento cabecera, CancellationToken token = default);
        Task AgregarLineaAsync(int movimientoId, LineaMovimiento linea, decimal saldo, CancellationToken token = default);
        Task RegistrarTrasladoAsync(int movimientoId, TrasladoInsumo traslado, CancellationToken token = default);
        Task RevertirTrasladoAsync(TrasladoInsumo traslado, CancellationToken token = default);
        Task EliminarLineasYTrasladosAsync(int id, CancellationToken token = default);
        Task EliminarCabeceraAsync(int id, CancellationToken token = default);
    }
}
