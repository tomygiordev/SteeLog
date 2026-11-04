using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Activos;
namespace SteeLog.Presentation
{
    public sealed class CargasHerramientas
    {
        private int revision; private int cerrada;
        public int Iniciar() => Interlocked.Increment(ref revision);
        public bool EsActual(int solicitud) => Volatile.Read(ref cerrada)==0 && Volatile.Read(ref revision)==solicitud;
        public void Cerrar() { Interlocked.Exchange(ref cerrada,1); Interlocked.Increment(ref revision); }
    }
    public interface IHerramientasVista { void SetOcupada(bool ocupada); void MostrarError(Exception error); }
    public sealed class HerramientasPresenter
    {
        private readonly IUseCaseExecutor executor; private readonly IHerramientasVista vista; private int ocupado;
        public HerramientasPresenter(IUseCaseExecutor executor,IHerramientasVista vista) { this.executor=executor ?? throw new ArgumentNullException(nameof(executor)); this.vista=vista ?? throw new ArgumentNullException(nameof(vista)); }
        private async Task<int?> Ejecutar(Func<Task<int>> operacion, Func<bool> vigente)
        {
            if(Interlocked.CompareExchange(ref ocupado,1,0)!=0) return null;
            try { vista.SetOcupada(true); return await operacion(); }
            catch(OperationCanceledException) { return null; }
            catch(Exception e) { if(vigente == null || vigente()) vista.MostrarError(e); return null; }
            finally { Interlocked.Exchange(ref ocupado,0); vista.SetOcupada(false); }
        }
        public Task<IReadOnlyList<HerramientaLectura>> HerramientasAsync(CancellationToken token=default) => executor.ExecuteAsync<ConsultarHerramientas,IReadOnlyList<HerramientaLectura>>(x=>x.HerramientasAsync(token));
        public Task<IReadOnlyList<ObraHerramientaLectura>> ObrasAsync(CancellationToken token=default) => executor.ExecuteAsync<ConsultarHerramientas,IReadOnlyList<ObraHerramientaLectura>>(x=>x.ObrasAsync(token));
        public Task<IReadOnlyList<HistorialHerramientaLectura>> HistorialAsync(CancellationToken token=default) => executor.ExecuteAsync<ConsultarHerramientas,IReadOnlyList<HistorialHerramientaLectura>>(x=>x.HistorialAsync(token));
        public Task<int?> GuardarAsync(int? id,string nombre,string estado,int? obra,string observaciones,CancellationToken token=default,Func<bool> vigente=null) => Ejecutar(()=>executor.ExecuteAsync<GestionarHerramientas,int>(x=>id.HasValue?x.EditarAsync(id.Value,nombre,estado,obra,observaciones,token):x.CrearAsync(nombre,estado,obra,observaciones,token)),vigente);
        public Task<int?> EliminarAsync(int id,CancellationToken token=default,Func<bool> vigente=null) => Ejecutar(()=>executor.ExecuteAsync<GestionarHerramientas,int>(x=>x.EliminarAsync(id,token)),vigente);
    }
}
