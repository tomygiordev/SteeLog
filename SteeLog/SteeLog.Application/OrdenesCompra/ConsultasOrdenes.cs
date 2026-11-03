using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.OrdenesCompra
{
    public sealed class OrdenLectura
    {
        public int Id { get; set; } public DateTime Fecha { get; set; } public string Numero { get; set; }
        public string Remito { get; set; } public int ProveedorId { get; set; } public string Proveedor { get; set; }
        public int DestinoId { get; set; } public string Destino { get; set; } public decimal Flete { get; set; }
        public string Observaciones { get; set; } public string Estado { get; set; } public string UsuarioCreacion { get; set; }
        public string UsuarioRevision { get; set; } public DateTime? FechaRevision { get; set; } public string ObservacionRevision { get; set; }
        public int CantidadItems { get; set; } public decimal TotalPedido { get; set; } public decimal TotalRecibido { get; set; }
        public List<LineaLectura> Lineas { get; set; } = new List<LineaLectura>();
    }
    public sealed class LineaLectura
    {
        public int Id { get; set; } public int OrdenId { get; set; } public int MaterialId { get; set; } public string Material { get; set; }
        public decimal Pedida { get; set; } public decimal Recibida { get; set; } public decimal Faltante { get; set; }
        public string Observacion { get; set; } public decimal StockAlCrear { get; set; } public decimal? StockDespues { get; set; }
    }
    public sealed class ProveedorLectura { public int Id { get; set; } public string Nombre { get; set; } }
    public sealed class MaterialLectura { public int Id { get; set; } public string Nombre { get; set; } public decimal Stock { get; set; } public decimal Minimo { get; set; } public string Categoria { get; set; } }
    public sealed class PendienteLectura
    {
        public string Numero { get; set; } public string Proveedor { get; set; } public string Material { get; set; }
        public decimal Pedida { get; set; } public decimal Recibida { get; set; } public decimal Faltante { get; set; } public string Estado { get; set; }
    }
    public interface IOrdenesConsulta
    {
        Task<IReadOnlyList<OrdenLectura>> ListarAsync(string estado, bool soloAbiertas, bool historial, bool excluirCompletas, CancellationToken token = default);
        Task<OrdenLectura> ObtenerAsync(int id, CancellationToken token = default);
        Task<IReadOnlyList<ProveedorLectura>> ProveedoresAsync(CancellationToken token = default);
        Task<IReadOnlyList<MaterialLectura>> MaterialesAsync(CancellationToken token = default);
        Task<string> DestinoAsync(CancellationToken token = default);
        Task<IReadOnlyList<PendienteLectura>> PendientesAsync(CancellationToken token = default);
    }
    public sealed class ConsultarOrdenes
    {
        private readonly Session session; private readonly IOrdenesConsulta consulta;
        public ConsultarOrdenes(Session session, IOrdenesConsulta consulta) { this.session = session; this.consulta = consulta; }
        public Task<IReadOnlyList<OrdenLectura>> ListarAsync(string estado, bool recepcion, bool historial, CancellationToken token = default)
        {
            session.Require(Role.Galpon);
            return consulta.ListarAsync(estado, recepcion || session.Role != Role.Admin, historial, !historial, token);
        }
        public Task<OrdenLectura> ObtenerAsync(int id, CancellationToken token = default) { session.Require(Role.Galpon); return consulta.ObtenerAsync(id, token); }
        public Task<IReadOnlyList<ProveedorLectura>> ProveedoresAsync(CancellationToken token = default) { session.Require(Role.Admin); return consulta.ProveedoresAsync(token); }
        public Task<IReadOnlyList<MaterialLectura>> MaterialesAsync(CancellationToken token = default) { session.Require(Role.Admin); return consulta.MaterialesAsync(token); }
        public Task<string> DestinoAsync(CancellationToken token = default) { session.Require(Role.Admin); return consulta.DestinoAsync(token); }
        public Task<IReadOnlyList<PendienteLectura>> PendientesAsync(CancellationToken token = default) { session.Require(Role.Galpon); return consulta.PendientesAsync(token); }
    }
}
