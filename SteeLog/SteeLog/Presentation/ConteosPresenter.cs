using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Inventario;
namespace SteeLog.Presentation
{
    public sealed class ConteosPresenter
    {
        private readonly IUseCaseExecutor executor;
        public ConteosPresenter(IUseCaseExecutor executor) { this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); }
        public Task<IReadOnlyList<MaterialInventarioLectura>> MaterialesAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarMateriales,IReadOnlyList<MaterialInventarioLectura>>(x => x.ListarAsync(null,t));
        public Task<ConteoLectura> AbiertoAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarConteos,ConteoLectura>(x => x.AbiertoAsync(t));
        public Task<ConteoLectura> ObtenerAsync(int id,CancellationToken t) => executor.ExecuteAsync<ConsultarConteos,ConteoLectura>(x => x.ObtenerAsync(id,t));
        public Task<IReadOnlyList<ConteoLectura>> HistorialAsync(DateTime? d,DateTime? h,string b,CancellationToken t) => executor.ExecuteAsync<ConsultarConteos,IReadOnlyList<ConteoLectura>>(x => x.HistorialAsync(d,h,b,t));
        public Task<IReadOnlyList<DetalleConteoLectura>> DetallesAsync(int id,CancellationToken t) => executor.ExecuteAsync<ConsultarConteos,IReadOnlyList<DetalleConteoLectura>>(x => x.DetallesAsync(id,t));
        public Task<IReadOnlyDictionary<int,SeccionConteo>> SeccionesAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarConteos,IReadOnlyDictionary<int,SeccionConteo>>(x => x.SeccionesAsync(t));
        public Task<int> CrearAsync(CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,int>(x => x.CrearAsync(t));
        public Task<bool> CargarAsync(int id,int material,decimal cantidad,CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,bool>(x => x.CargarAsync(id,material,cantidad,t));
        public Task<bool> CorregirAsync(int id,int material,decimal cantidad,CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,bool>(x => x.CorregirAsync(id,material,cantidad,t));
        public Task<bool> QuitarAsync(int id,int detalle,CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,bool>(x => x.QuitarAsync(id,detalle,t));
        public Task<bool> FinalizarAsync(int id,string observaciones,CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,bool>(x => x.FinalizarAsync(id,observaciones,t));
        public Task<bool> CancelarAsync(int id,CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,bool>(x => x.CancelarAsync(id,t));
        public Task<bool> EliminarAsync(int id,CancellationToken t) => executor.ExecuteAsync<OperacionesConteo,bool>(x => x.EliminarAsync(id,t));
    }
}
