using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Movimientos
{
    public sealed class HistorialMovimientoLectura
    {
        public int IdMovimiento { get; set; } public int? IdLinea { get; set; } public int? IdMaterial { get; set; }
        public DateTime Fecha { get; set; } public string Remito { get; set; } public string Tipo { get; set; } public string Material { get; set; }
        public decimal Cantidad { get; set; } public string Origen { get; set; } public string Destino { get; set; } public string Observaciones { get; set; }
        public bool EsRecorte { get; set; } public decimal? Medida { get; set; } public bool EsInsumo { get; set; } public bool Eventual { get; set; }
        public decimal PrecioUnitario { get; set; } public decimal PrecioTotal { get; set; }
    }
    public interface IHistorialMovimientoConsulta { Task<IReadOnlyList<HistorialMovimientoLectura>> ListarAsync(DateTime? desde,DateTime? hasta,string tipo,CancellationToken token=default); }
    public sealed class ConsultarHistorialMovimientos
    {
        private readonly Session actor; private readonly IHistorialMovimientoConsulta consulta;
        public ConsultarHistorialMovimientos(Session actor,IHistorialMovimientoConsulta consulta) { this.actor=actor; this.consulta=consulta; }
        public Task<IReadOnlyList<HistorialMovimientoLectura>> ListarAsync(DateTime? desde,DateTime? hasta,string tipo,CancellationToken token=default) { actor.Require(Role.Galpon); return consulta.ListarAsync(desde,hasta,tipo,token); }
    }
}
