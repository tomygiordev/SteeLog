using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Conteos
{
    public sealed class ConteoLectura
    {
        public int Id { get; set; } public DateTime Inicio { get; set; } public DateTime? Fin { get; set; }
        public string UsuarioInicio { get; set; } public string UsuarioFin { get; set; } public string Estado { get; set; } public string Observaciones { get; set; }
        public int Materiales { get; set; } public int Diferencias { get; set; }
    }
    public sealed class DetalleConteoLectura
    {
        public int Id { get; set; } public int ConteoId { get; set; } public int MaterialId { get; set; }
        public string Nombre { get; set; } public string Categoria { get; set; } public decimal Cantidad { get; set; }
        public decimal? Stock { get; set; } public decimal? Diferencia { get; set; } public DateTime FechaCarga { get; set; }
        public string EstadoDiferencia => Diferencia > 0 ? "Sobrante" : Diferencia < 0 ? "Faltante" : "Sin diferencia";
    }
    public interface IConteosConsulta
    {
        Task<ConteoLectura> ObtenerAsync(int id, CancellationToken token = default);
        Task<ConteoLectura> AbiertoAsync(string email, CancellationToken token = default);
        Task<IReadOnlyList<ConteoLectura>> HistorialAsync(DateTime? desde, DateTime? hasta, string busqueda, CancellationToken token = default);
        Task<IReadOnlyList<DetalleConteoLectura>> DetallesAsync(int id, CancellationToken token = default);
        Task<IReadOnlyDictionary<int, SeccionConteo>> SeccionesAsync(CancellationToken token = default);
    }
    public sealed class ConsultarConteos
    {
        private readonly Session session; private readonly IConteosConsulta consulta;
        public ConsultarConteos(Session session, IConteosConsulta consulta) { this.session = session; this.consulta = consulta; }
        public Task<ConteoLectura> ObtenerAsync(int id, CancellationToken t = default) { session.Require(Role.Galpon); return consulta.ObtenerAsync(id, t); }
        public Task<ConteoLectura> AbiertoAsync(CancellationToken t = default) { session.Require(Role.Galpon); return consulta.AbiertoAsync(session.Current.Email.Trim(), t); }
        public Task<IReadOnlyList<ConteoLectura>> HistorialAsync(DateTime? desde, DateTime? hasta, string busqueda, CancellationToken t = default) { session.Require(Role.Galpon); return consulta.HistorialAsync(desde, hasta, busqueda, t); }
        public Task<IReadOnlyList<DetalleConteoLectura>> DetallesAsync(int id, CancellationToken t = default) { session.Require(Role.Galpon); return consulta.DetallesAsync(id, t); }
        public Task<IReadOnlyDictionary<int, SeccionConteo>> SeccionesAsync(CancellationToken t = default) { session.Require(Role.Galpon); return consulta.SeccionesAsync(t); }
    }
}
