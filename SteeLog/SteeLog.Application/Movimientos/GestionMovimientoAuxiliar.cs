using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Movimientos
{
    public interface IFletesEscritura
    {
        Task<int> CrearAsync(string nombre,CancellationToken token=default);
        Task DesactivarAsync(int id,CancellationToken token=default);
    }
    public sealed class GestionarFletes
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IFletesEscritura repo;
        public GestionarFletes(Session actor,IUnitOfWork uow,IFletesEscritura repo) { this.actor=actor;this.uow=uow;this.repo=repo; }
        private async Task<int> Transaccion(Func<Task<int>> trabajo,CancellationToken token)
        { token.ThrowIfCancellationRequested(); await uow.BeginAsync(token).ConfigureAwait(false); try { var r=await trabajo().ConfigureAwait(false); token.ThrowIfCancellationRequested(); await uow.CommitAsync(token).ConfigureAwait(false); return r; } catch { await uow.RollbackAsync().ConfigureAwait(false); throw; } }
        public Task<int> CrearAsync(string nombre,CancellationToken token=default)
        { actor.Require(Role.Galpon); if(string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length>50) throw new ArgumentException("Nombre de flete inválido."); return Transaccion(() => repo.CrearAsync(nombre.Trim(),token),token); }
        public Task<int> DesactivarAsync(int id,CancellationToken token=default)
        { actor.Require(Role.Galpon); if(id<=0) throw new ArgumentOutOfRangeException(nameof(id)); return Transaccion(async () => { await repo.DesactivarAsync(id,token).ConfigureAwait(false); return id; },token); }
    }
    public interface IPrecioEventualEscritura { Task<decimal> ActualizarAsync(int idLinea,decimal precio,CancellationToken token=default); }
    public sealed class ActualizarPrecioEventual
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IPrecioEventualEscritura repo;
        public ActualizarPrecioEventual(Session actor,IUnitOfWork uow,IPrecioEventualEscritura repo) { this.actor=actor;this.uow=uow;this.repo=repo; }
        public async Task<decimal> EjecutarAsync(int id,decimal precio,CancellationToken token=default)
        {
            actor.Require(Role.Admin);
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (precio < 0 || precio > 99999999.99m || decimal.Round(precio, 2) != precio)
                throw new ArgumentOutOfRangeException(nameof(precio), "El precio unitario debe ser no negativo y tener como máximo dos decimales.");
            token.ThrowIfCancellationRequested(); await uow.BeginAsync(token).ConfigureAwait(false);
            try { var total=await repo.ActualizarAsync(id,precio,token).ConfigureAwait(false); token.ThrowIfCancellationRequested(); await uow.CommitAsync(token).ConfigureAwait(false); return total; }
            catch { await uow.RollbackAsync().ConfigureAwait(false); throw; }
        }
    }
}
