using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Movimientos
{
    public sealed class OpcionMovimiento { public int Id { get; set; } public string Nombre { get; set; } public string Tipo { get; set; } public bool Activo { get; set; } }
    public sealed class CatalogosMovimiento
    {
        public List<OpcionMovimiento> Tipos { get; set; } = new List<OpcionMovimiento>();
        public List<OpcionMovimiento> Destinos { get; set; } = new List<OpcionMovimiento>();
        public List<OpcionMovimiento> Fletes { get; set; } = new List<OpcionMovimiento>();
    }
    public interface ICatalogosMovimientoConsulta { Task<CatalogosMovimiento> ObtenerAsync(CancellationToken token=default); }
    public sealed class ConsultarCatalogosMovimiento
    {
        private readonly Session actor; private readonly ICatalogosMovimientoConsulta consulta;
        public ConsultarCatalogosMovimiento(Session actor, ICatalogosMovimientoConsulta consulta) { this.actor=actor; this.consulta=consulta; }
        public Task<CatalogosMovimiento> ObtenerAsync(CancellationToken token=default) { actor.Require(Role.Galpon); return consulta.ObtenerAsync(token); }
    }
}
