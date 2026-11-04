using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Activos;
namespace SteeLog.Core.Application.Activos
{
    public interface IHerramientasEscritura
    {
        Task<Herramienta> BloquearAsync(int id, CancellationToken token = default);
        Task<int> CrearAsync(Herramienta herramienta, CancellationToken token = default);
        Task ActualizarAsync(Herramienta herramienta, CancellationToken token = default);
        Task RegistrarHistorialAsync(int id, int? origen, int? destino, CancellationToken token = default);
        Task EliminarAsync(int id, CancellationToken token = default);
    }
    public sealed class GestionarHerramientas
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IHerramientasEscritura repo;
        public GestionarHerramientas(Session actor, IUnitOfWork uow, IHerramientasEscritura repo) { this.actor=actor; this.uow=uow; this.repo=repo; }
        private async Task<int> Transaccion(Func<Task<int>> trabajo, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await uow.BeginAsync(token).ConfigureAwait(false);
            bool inicioCommit = false;
            try
            {
                var resultado = await trabajo().ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                inicioCommit = true;
                await uow.CommitAsync(token).ConfigureAwait(false);
                return resultado;
            }
            catch (Exception error)
            {
                try { await uow.RollbackAsync().ConfigureAwait(false); }
                catch { /* Conservar el fallo original, incluso si falla la limpieza. */ }
                if (inicioCommit) throw new ResultadoEscrituraInciertoException(error);
                throw;
            }
        }
        public Task<int> CrearAsync(string nombre,string estado,int? obra,string observaciones,CancellationToken token=default)
        {
            actor.Require(Role.Galpon); var h=new Herramienta(0,nombre,estado,obra,observaciones); h.Editar(nombre,estado,obra,observaciones);
            return Transaccion(async () => { var id=await repo.CrearAsync(h,token).ConfigureAwait(false); await repo.RegistrarHistorialAsync(id,null,h.ObraId,token).ConfigureAwait(false); return id; },token);
        }
        public Task<int> EditarAsync(int id,string nombre,string estado,int? obra,string observaciones,CancellationToken token=default)
        {
            actor.Require(Role.Galpon); if(id<=0) throw new ArgumentOutOfRangeException(nameof(id)); return Transaccion(async () => { var h=await repo.BloquearAsync(id,token).ConfigureAwait(false) ?? throw new InvalidOperationException("No se encontró la herramienta."); var origen=h.ObraId; h.Editar(nombre,estado,obra,observaciones); await repo.ActualizarAsync(h,token).ConfigureAwait(false); if(origen!=h.ObraId) await repo.RegistrarHistorialAsync(id,origen,h.ObraId,token).ConfigureAwait(false); return id; },token);
        }
        public Task<int> EnviarAGalponAsync(int id,CancellationToken token=default)
        {
            actor.Require(Role.Galpon); if(id<=0) throw new ArgumentOutOfRangeException(nameof(id));
            return Transaccion(async () => { var anterior=await repo.BloquearAsync(id,token).ConfigureAwait(false) ?? throw new InvalidOperationException("No se encontró la herramienta."); var origen=anterior.ObraId; anterior.EnviarAGalpon(); await repo.ActualizarAsync(anterior,token).ConfigureAwait(false); await repo.RegistrarHistorialAsync(id,origen,null,token).ConfigureAwait(false); return id; },token);
        }
        public Task<int> EliminarAsync(int id,CancellationToken token=default)
        {
            actor.Require(Role.Galpon); if(id<=0) throw new ArgumentOutOfRangeException(nameof(id));
            return Transaccion(async () => { if(await repo.BloquearAsync(id,token).ConfigureAwait(false)==null) throw new InvalidOperationException("No se encontró la herramienta."); await repo.EliminarAsync(id,token).ConfigureAwait(false); return id; },token);
        }
    }
}
