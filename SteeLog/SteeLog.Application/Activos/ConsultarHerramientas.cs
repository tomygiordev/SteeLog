using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Activos
{
    public sealed class HerramientaLectura { public int Id { get; set; } public string Nombre { get; set; } public string Estado { get; set; } public int? ObraId { get; set; } public string Obra { get; set; } public string Observaciones { get; set; } }
    public sealed class ObraHerramientaLectura { public int Id { get; set; } public string Nombre { get; set; } }
    public sealed class HistorialHerramientaLectura { public int Id { get; set; } public int HerramientaId { get; set; } public string Nombre { get; set; } public string Estado { get; set; } public string Origen { get; set; } public string Destino { get; set; } public DateTime Fecha { get; set; } }
    public interface IHerramientasConsulta
    {
        Task<IReadOnlyList<HerramientaLectura>> HerramientasAsync(CancellationToken token=default);
        Task<IReadOnlyList<ObraHerramientaLectura>> ObrasAsync(CancellationToken token=default);
        Task<IReadOnlyList<HistorialHerramientaLectura>> HistorialAsync(CancellationToken token=default);
    }
    public sealed class ConsultarHerramientas
    {
        private readonly Session actor; private readonly IHerramientasConsulta consulta;
        public ConsultarHerramientas(Session actor,IHerramientasConsulta consulta) { this.actor=actor ?? throw new ArgumentNullException(nameof(actor)); this.consulta=consulta ?? throw new ArgumentNullException(nameof(consulta)); }
        public Task<IReadOnlyList<HerramientaLectura>> HerramientasAsync(CancellationToken token=default) { actor.Require(Role.Galpon); return consulta.HerramientasAsync(token); }
        public Task<IReadOnlyList<ObraHerramientaLectura>> ObrasAsync(CancellationToken token=default) { actor.Require(Role.Galpon); return consulta.ObrasAsync(token); }
        public Task<IReadOnlyList<HistorialHerramientaLectura>> HistorialAsync(CancellationToken token=default) { actor.Require(Role.Galpon); return consulta.HistorialAsync(token); }
    }
}
