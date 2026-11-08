using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Activos
{
    public sealed class InsumoLectura
    {
        public int Id { get; set; } public string Nombre { get; set; } public int? ObraId { get; set; }
        public string Obra { get; set; } public bool Consumido { get; set; } public string Observaciones { get; set; }
    }
    public sealed class ObraInsumoLectura { public int Id { get; set; } public string Nombre { get; set; } }
    public sealed class HistorialInsumoLectura
    {
        public int Id { get; set; } public int InsumoId { get; set; } public string Insumo { get; set; } public string Origen { get; set; }
        public string Destino { get; set; } public DateTime Fecha { get; set; }
    }
    public interface IInsumosConsulta
    {
        Task<IReadOnlyList<InsumoLectura>> InsumosAsync(CancellationToken token = default);
        Task<IReadOnlyList<ObraInsumoLectura>> ObrasAsync(CancellationToken token = default);
        Task<IReadOnlyList<HistorialInsumoLectura>> HistorialAsync(CancellationToken token = default);
    }
    public sealed class ConsultarInsumos
    {
        private readonly Session session; private readonly IInsumosConsulta consulta;
        public ConsultarInsumos(Session session, IInsumosConsulta consulta)
        { this.session = session ?? throw new ArgumentNullException(nameof(session)); this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta)); }
        public Task<IReadOnlyList<InsumoLectura>> InsumosAsync(CancellationToken token = default) { session.Require(Role.Galpon); return consulta.InsumosAsync(token); }
        public Task<IReadOnlyList<ObraInsumoLectura>> ObrasAsync(CancellationToken token = default) { session.Require(Role.Galpon); return consulta.ObrasAsync(token); }
        public Task<IReadOnlyList<HistorialInsumoLectura>> HistorialAsync(CancellationToken token = default) { session.Require(Role.Galpon); return consulta.HistorialAsync(token); }
    }
}
