using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Activos;

namespace SteeLog.Core.Application.Activos
{
    public interface IInsumosEscritura
    {
        Task<Insumo> BloquearAsync(int id, CancellationToken token = default);
        Task<int> CrearAsync(Insumo insumo, CancellationToken token = default);
        Task ActualizarAsync(Insumo insumo, CancellationToken token = default);
        Task RegistrarHistorialAsync(int id, int? origen, int? destino, bool consumidoOrigen, bool consumidoDestino, CancellationToken token = default);
        Task EliminarAsync(int id, CancellationToken token = default);
    }
    public sealed class GestionarInsumos
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IInsumosEscritura repo;
        public GestionarInsumos(Session actor, IUnitOfWork uow, IInsumosEscritura repo) { this.actor=actor; this.uow=uow; this.repo=repo; }
        private async Task<T> Transaccion<T>(Func<Task<T>> trabajo, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); await uow.BeginAsync(token).ConfigureAwait(false);
            bool inicioCommit = false;
            try { var r=await trabajo().ConfigureAwait(false); token.ThrowIfCancellationRequested(); inicioCommit=true; await uow.CommitAsync(token).ConfigureAwait(false); return r; }
            catch (Exception error)
            {
                try { await uow.RollbackAsync().ConfigureAwait(false); } catch { }
                if (inicioCommit && !(error is OperationCanceledException)) throw new ResultadoEscrituraInciertoException(error);
                throw;
            }
        }
        public Task<int> CrearAsync(string nombre, int? obraId, string observaciones, CancellationToken token=default)
        {
            actor.Require(Role.Galpon); var insumo=new Insumo(0,nombre,obraId,false,observaciones);
            return Transaccion(async () => { var id=await repo.CrearAsync(insumo,token).ConfigureAwait(false); await repo.RegistrarHistorialAsync(id,null,insumo.ObraId,false,insumo.Consumido,token).ConfigureAwait(false); return id; },token);
        }
        public Task<int> EditarAsync(int id,string nombre,int? obraId,bool consumido,string observaciones,CancellationToken token=default)
        {
            actor.Require(Role.Galpon); if(id<=0) throw new ArgumentOutOfRangeException(nameof(id));
            var nuevo=new Insumo(id,nombre,obraId,consumido,observaciones);
            return Transaccion(async () => { var anterior=await repo.BloquearAsync(id,token).ConfigureAwait(false); if(anterior==null) throw new InvalidOperationException("No se encontró el insumo."); await repo.ActualizarAsync(nuevo,token).ConfigureAwait(false); if(anterior.ObraId!=nuevo.ObraId || anterior.Consumido!=nuevo.Consumido) await repo.RegistrarHistorialAsync(id,anterior.ObraId,nuevo.ObraId,anterior.Consumido,nuevo.Consumido,token).ConfigureAwait(false); return id; },token);
        }
        public Task<int> EliminarAsync(int id,CancellationToken token=default)
        {
            actor.Require(Role.Galpon); if(id<=0) throw new ArgumentOutOfRangeException(nameof(id));
            return Transaccion(async () => { var anterior=await repo.BloquearAsync(id,token).ConfigureAwait(false); if(anterior==null) throw new InvalidOperationException("No se encontró el insumo."); await repo.EliminarAsync(id,token).ConfigureAwait(false); return id; },token);
        }
    }
}
