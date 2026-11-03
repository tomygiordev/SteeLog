using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Inventario
{
    public sealed class FiltroMaterialLectura
    {
        public string Nombre { get; set; } public decimal? Desde { get; set; } public decimal? Hasta { get; set; }
        public IReadOnlyList<int> Categorias { get; set; } public IReadOnlyList<int> Proveedores { get; set; }
        public bool SoloStockBajo { get; set; } public bool Priorizados { get; set; }
    }
    public sealed class MaterialInventarioLectura
    {
        public int Id { get; set; } public string Nombre { get; set; } public int CategoriaId { get; set; } public string Categoria { get; set; }
        public decimal Precio { get; set; } public decimal Stock { get; set; } public decimal Minimo { get; set; }
        public decimal CantidadUsada { get; set; } public int CantidadMovimientos { get; set; } public int? UbicacionId { get; set; }
    }
    public sealed class CategoriaMaterialLectura { public int Id { get; set; } public string Nombre { get; set; } public bool Recortable { get; set; } }
    public sealed class ProveedorMaterialLectura { public int Id { get; set; } public string Nombre { get; set; } }
    public interface IMaterialesConsulta
    {
        Task<IReadOnlyList<MaterialInventarioLectura>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token = default);
        Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token = default);
        Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default);
        Task<IReadOnlyList<int>> ProveedoresAsync(int materialId, CancellationToken token = default);
    }
    public sealed class ConsultarMateriales
    {
        private readonly Session actor; private readonly IMaterialesConsulta consulta;
        public ConsultarMateriales(Session actor, IMaterialesConsulta consulta) { this.actor=actor; this.consulta=consulta; }
        public Task<IReadOnlyList<MaterialInventarioLectura>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.ListarAsync(filtro ?? new FiltroMaterialLectura(),token); }
        public Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.CategoriasAsync(token); }
        public Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.CatalogoProveedoresAsync(token); }
        public Task<IReadOnlyList<int>> ProveedoresAsync(int id, CancellationToken token = default) { actor.Require(Role.Admin); return consulta.ProveedoresAsync(id,token); }
    }
}
