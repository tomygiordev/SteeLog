using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Movimientos
{
    public sealed class LineaEdicion
    {
        public LineaMovimiento Linea { get; set; }
        public string Nombre { get; set; }
        public int? InsumoId { get; set; }
    }
    public sealed class MovimientoEdicion
    {
        public string TipoNombre { get; set; }
        public string TipoImpacto { get; set; }
        public CabeceraMovimiento Cabecera { get; set; }
        public List<LineaEdicion> Lineas { get; set; } = new List<LineaEdicion>();
    }
    public interface IMovimientoEdicionConsulta { Task<MovimientoEdicion> ObtenerAsync(int id, CancellationToken token=default); }
    public sealed class ConsultarMovimientoEdicion
    {
        private readonly Session actor; private readonly IMovimientoEdicionConsulta consulta;
        public ConsultarMovimientoEdicion(Session actor,IMovimientoEdicionConsulta consulta) { this.actor=actor; this.consulta=consulta; }
        public Task<MovimientoEdicion> ObtenerAsync(int id,CancellationToken token=default) { actor.Require(Role.Admin); return consulta.ObtenerAsync(id,token); }
    }
}
