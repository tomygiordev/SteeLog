using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Inventario
{
    public sealed class RecorteLectura
    {
        public int Id { get; set; } public int MaterialId { get; set; }
        public string Material { get; set; } public string Categoria { get; set; }
        public decimal Ancho { get; set; } public decimal Altura { get; set; } public decimal Medida { get; set; }
        public decimal Cantidad { get; set; } public string Estado { get; set; } public DateTime FechaCreacion { get; set; }
    }
    public sealed class OpcionRecorte { public int Id { get; set; } public string Nombre { get; set; } }
    public interface IRecortesConsulta
    {
        Task<IReadOnlyList<OpcionRecorte>> CategoriasAsync(CancellationToken token = default);
        Task<IReadOnlyList<OpcionRecorte>> MaterialesAsync(int categoriaId, CancellationToken token = default);
        Task<IReadOnlyList<RecorteLectura>> ListarAsync(int? materialId, decimal? ancho, decimal? altura, decimal? medida, CancellationToken token = default);
    }
    public sealed class ConsultarRecortes
    {
        private readonly Session actor; private readonly IRecortesConsulta consulta;
        public ConsultarRecortes(Session actor, IRecortesConsulta consulta) { this.actor = actor; this.consulta = consulta; }
        public Task<IReadOnlyList<OpcionRecorte>> CategoriasAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.CategoriasAsync(token); }
        public Task<IReadOnlyList<OpcionRecorte>> MaterialesAsync(int categoriaId, CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.MaterialesAsync(categoriaId, token); }
        public Task<IReadOnlyList<RecorteLectura>> ListarAsync(int? materialId = null, decimal? ancho = null, decimal? altura = null, decimal? medida = null, CancellationToken token = default)
        { actor.Require(Role.Galpon); return consulta.ListarAsync(materialId, ancho, altura, medida, token); }
    }
}
