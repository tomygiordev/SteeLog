using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Inventario;
namespace SteeLog.Presentation
{
    public sealed class MaterialesPresenter
    {
        private readonly IUseCaseExecutor executor;
        public MaterialesPresenter(IUseCaseExecutor executor) { this.executor=executor ?? throw new ArgumentNullException(nameof(executor)); }
        public Task<IReadOnlyList<MaterialInventarioLectura>> ListarAsync(FiltroMaterialLectura filtro,CancellationToken token) => executor.ExecuteAsync<ConsultarMateriales,IReadOnlyList<MaterialInventarioLectura>>(x=>x.ListarAsync(filtro,token));
        public Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarMateriales,IReadOnlyList<CategoriaMaterialLectura>>(x=>x.CategoriasAsync(token));
        public Task<IReadOnlyList<int>> ProveedoresAsync(int id,CancellationToken token) => executor.ExecuteAsync<ConsultarMateriales,IReadOnlyList<int>>(x=>x.ProveedoresAsync(id,token));
        public Task<int> CrearAsync(DatosMaterial datos,decimal stock,CancellationToken token) => executor.ExecuteAsync<CrearMaterial,int>(x=>x.EjecutarAsync(datos,stock,token));
        public Task<int> EditarAsync(int id,DatosMaterial datos,decimal? stock,decimal? esperado,CancellationToken token) => executor.ExecuteAsync<EditarMaterial,int>(async x=>{await x.EjecutarAsync(id,datos,stock,esperado,token); return id;});
        public Task<int> EliminarAsync(int id,CancellationToken token) => executor.ExecuteAsync<EliminarMaterial,int>(async x=>{await x.EjecutarAsync(id,token); return id;});
    }
}
