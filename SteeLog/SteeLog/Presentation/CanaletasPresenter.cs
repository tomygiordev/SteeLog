using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Activos;

namespace SteeLog.Presentation
{
    public sealed class CanaletasPresenter
    {
        private readonly IUseCaseExecutor executor;
        public CanaletasPresenter(IUseCaseExecutor executor) { this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); }
        public static async Task RefrescarTrasOperacionAsync(Func<Task> refrescar, Func<bool> vigente, Action<Exception> advertir)
        {
            try { await refrescar(); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (vigente()) advertir(ex); }
        }
        public Task<IReadOnlyList<ObraInsumoLectura>> ObrasAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarInsumos, IReadOnlyList<ObraInsumoLectura>>(x => x.ObrasAsync(t));
        public Task<IReadOnlyList<CanaletaLectura>> ListarAsync(int? obra, CancellationToken t) => executor.ExecuteAsync<ConsultarCanaletas, IReadOnlyList<CanaletaLectura>>(x => x.ListarAsync(obra,t));
        public Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t) => executor.ExecuteAsync<ConsultarCanaletas, IReadOnlyList<FotoCanaletaLectura>>(x => x.FotosAsync(id,t));
        public Task<byte[]> LeerFotoAsync(string path, CancellationToken t) => executor.ExecuteAsync<ConsultarCanaletas, byte[]>(x => x.LeerFotoAsync(path,t));
        public Task<bool> ConciliarAsync(CancellationToken t) => Task.Run(() => executor.ExecuteAsync<OperacionesCanaleta,bool>(async x => { await x.ConciliarPendientesAsync(t); return true; }), t);
        public Task<int> GuardarAsync(int? id,int obra,string descripcion,decimal precio,bool pagado,CancellationToken t) => executor.ExecuteAsync<OperacionesCanaleta,int>(x => x.GuardarAsync(id,obra,descripcion,precio,pagado,t));
        public Task<int> AdjuntarAsync(int id,string nombre,byte[] bytes,CancellationToken t) => executor.ExecuteAsync<OperacionesCanaleta,int>(x => x.AdjuntarAsync(id,nombre,bytes,t));
        public Task<bool> EliminarFotoAsync(int id,CancellationToken t) => executor.ExecuteAsync<OperacionesCanaleta,bool>(x => x.EliminarFotoAsync(id,t));
        public Task<bool> EliminarAsync(int id,CancellationToken t) => executor.ExecuteAsync<OperacionesCanaleta,bool>(x => x.EliminarAsync(id,t));
    }
}
