using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Inventario;

namespace SteeLog.Presentation
{
    public sealed class InventarioPresenter
    {
        private readonly IUseCaseExecutor executor;
        public InventarioPresenter(IUseCaseExecutor executor) { this.executor = executor; }
        public Task<ResumenInventario> ResumenAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, ResumenInventario>(x => x.ResumenAsync(token));
        public Task<List<InventarioMaterial>> MaterialesAsync(string busqueda, string categoria, string estado, CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, List<InventarioMaterial>>(x => x.MaterialesAsync(busqueda, categoria, estado, token));
        public Task<List<InventarioRecorte>> RecortesAsync(string busqueda, string categoria, CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, List<InventarioRecorte>>(x => x.RecortesAsync(busqueda, categoria, token));
        public Task<List<InventarioHerramienta>> HerramientasAsync(string busqueda, string estado, string obra, CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, List<InventarioHerramienta>>(x => x.HerramientasAsync(busqueda, estado, obra, token));
        public Task<List<InventarioInsumo>> InsumosAsync(string busqueda, string estado, string obra, CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, List<InventarioInsumo>>(x => x.InsumosAsync(busqueda, estado, obra, token));
        public Task<List<string>> CategoriasAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, List<string>>(x => x.CategoriasAsync(token));
        public Task<List<string>> ObrasAsync(bool herramientas, CancellationToken token) => executor.ExecuteAsync<ConsultarInventario, List<string>>(x => herramientas ? x.ObrasConHerramientasAsync(token) : x.ObrasActivasAsync(token));
    }
}
