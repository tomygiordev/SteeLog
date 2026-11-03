using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Activos;
namespace SteeLog.Presentation
{
    public sealed class CargasInsumos
    {
        private int revision;
        private int cerrada;
        public int Iniciar() => Interlocked.Increment(ref revision);
        public bool EsActual(int solicitud) => Volatile.Read(ref cerrada) == 0 && Volatile.Read(ref revision) == solicitud;
        public void Cerrar() { Interlocked.Exchange(ref cerrada, 1); Interlocked.Increment(ref revision); }
    }
    public interface IInsumosVista { void SetOcupada(bool ocupada); void MostrarError(Exception error); }
    public sealed class InsumosPresenter
    {
        private readonly IUseCaseExecutor executor; private readonly IInsumosVista vista; private int ocupado;
        public InsumosPresenter(IUseCaseExecutor executor,IInsumosVista vista) { this.executor=executor; this.vista=vista; }
        private async Task<int?> Ejecutar(Func<Task<int>> operacion)
        {
            if(Interlocked.CompareExchange(ref ocupado,1,0)!=0) return null;
            try { vista.SetOcupada(true); return await operacion(); }
            catch(OperationCanceledException) { return null; }
            catch(Exception e) { vista.MostrarError(e); return null; }
            finally { Interlocked.Exchange(ref ocupado,0); vista.SetOcupada(false); }
        }
        public Task<IReadOnlyList<InsumoLectura>> InsumosAsync(CancellationToken token = default)
            => executor.ExecuteAsync<ConsultarInsumos, IReadOnlyList<InsumoLectura>>(x => x.InsumosAsync(token));
        public Task<IReadOnlyList<ObraInsumoLectura>> ObrasAsync(CancellationToken token = default)
            => executor.ExecuteAsync<ConsultarInsumos, IReadOnlyList<ObraInsumoLectura>>(x => x.ObrasAsync(token));
        public Task<IReadOnlyList<HistorialInsumoLectura>> HistorialAsync(CancellationToken token = default)
            => executor.ExecuteAsync<ConsultarInsumos, IReadOnlyList<HistorialInsumoLectura>>(x => x.HistorialAsync(token));
        public Task<int?> GuardarAsync(int? id,string nombre,int? obra,bool consumido,string observaciones,CancellationToken token=default)
            => Ejecutar(() => executor.ExecuteAsync<GestionarInsumos,int>(x => id.HasValue ? x.EditarAsync(id.Value,nombre,obra,consumido,observaciones,token) : x.CrearAsync(nombre,obra,observaciones,token)));
        public Task<int?> EliminarAsync(int id,CancellationToken token=default)
            => Ejecutar(() => executor.ExecuteAsync<GestionarInsumos,int>(x => x.EliminarAsync(id,token)));
    }
}
