using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Activos;
using SteeLog.Core.Application.Common;
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
    public sealed class OpcionFiltroInsumo
    {
        public int Id { get; set; }
        public string Etiqueta { get; set; }
    }
    public interface IInsumosVista { void SetOcupada(bool ocupada); void MostrarError(Exception error); }
    public interface IInsumosResultadoIncierto { void MarcarResultadoIncierto(); }
    public sealed class InsumosPresenter
    {
        private readonly IUseCaseExecutor executor; private readonly IInsumosVista vista; private readonly EjecucionExclusiva ejecucion;
        public InsumosPresenter(IUseCaseExecutor executor,IInsumosVista vista) { this.executor=executor; this.vista=vista; ejecucion=new EjecucionExclusiva(valor => vista.SetOcupada(valor), error => vista.MostrarError(error)); }
        private Task<int?> Ejecutar(Func<Task<int>> operacion)
            => ejecucion.Ejecutar<int?>(async () => await operacion(),
                e => { if (vista is IInsumosResultadoIncierto incierto) incierto.MarcarResultadoIncierto(); else vista.MostrarError(e); });
        public static IReadOnlyList<OpcionFiltroInsumo> OpcionesFiltro(IReadOnlyList<InsumoLectura> datos)
        {
            var repetidos = new HashSet<string>(datos.GroupBy(x => x.Nombre ?? "", StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.OrdinalIgnoreCase);
            return datos.Select(x => new OpcionFiltroInsumo {
                Id = x.Id, Etiqueta = repetidos.Contains(x.Nombre ?? "") ? $"{x.Nombre} (#{x.Id})" : x.Nombre
            }).ToArray();
        }
        public static IReadOnlyList<HistorialInsumoLectura> FiltrarHistorial(IReadOnlyList<HistorialInsumoLectura> datos, int? insumoId)
            => insumoId.HasValue ? datos.Where(x => x.InsumoId == insumoId.Value).ToArray() : datos;
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
