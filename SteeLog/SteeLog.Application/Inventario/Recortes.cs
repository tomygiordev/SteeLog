using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Inventario;

namespace SteeLog.Core.Application.Inventario
{
    public interface IRecortesEscritura
    {
        Task<Recorte> BuscarCoincidenteBloqueadoAsync(Recorte clave, CancellationToken token = default);
        Task<int> InsertarAsync(Recorte recorte, CancellationToken token = default);
        Task ActualizarCantidadAsync(int id, decimal cantidad, CancellationToken token = default);
        Task EliminarAsync(int id, int materialId, CancellationToken token = default);
    }

    public sealed class AgregarRecorte
    {
        private readonly Session actor;
        private readonly IUnitOfWork uow;
        private readonly IInventarioEscritura inventario;
        private readonly IRecortesEscritura recortes;

        public AgregarRecorte(Session actor, IUnitOfWork uow, IInventarioEscritura inventario, IRecortesEscritura recortes)
        {
            this.actor = actor; this.uow = uow; this.inventario = inventario; this.recortes = recortes;
        }

        public async Task<int> EjecutarAsync(int materialId, decimal ancho, decimal altura, decimal medida, decimal cantidad, string estado, CancellationToken token = default)
        {
            actor.Require(Role.Galpon);
            var nuevo = new Recorte(0, materialId, ancho, altura, medida, cantidad, estado);
            token.ThrowIfCancellationRequested();
            await uow.BeginAsync(token).ConfigureAwait(false);
            try
            {
                await inventario.BloquearAsync(new[] { materialId }, token).ConfigureAwait(false);
                var existente = await recortes.BuscarCoincidenteBloqueadoAsync(nuevo, token).ConfigureAwait(false);
                int id;
                if (existente == null) id = await recortes.InsertarAsync(nuevo, token).ConfigureAwait(false);
                else
                {
                    existente.Aplicar(cantidad);
                    await recortes.ActualizarCantidadAsync(existente.Id, existente.Cantidad, token).ConfigureAwait(false);
                    id = existente.Id;
                }
                token.ThrowIfCancellationRequested();
                await uow.CommitAsync(token).ConfigureAwait(false);
                return id;
            }
            catch { await uow.RollbackAsync().ConfigureAwait(false); throw; }
        }
    }    public sealed class EliminarRecorte
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IInventarioEscritura inventario; private readonly IRecortesEscritura recortes;
        public EliminarRecorte(Session actor, IUnitOfWork uow, IInventarioEscritura inventario, IRecortesEscritura recortes)
        { this.actor = actor; this.uow = uow; this.inventario = inventario; this.recortes = recortes; }
        public Task EjecutarAsync(int id, int materialId, CancellationToken token = default)
        {
            actor.Require(Role.Admin);
            if (id <= 0 || materialId <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            return MaterialesOperacion.Transaccion(uow, async () => {
                await inventario.BloquearAsync(new[] { materialId }, token).ConfigureAwait(false);
                await recortes.EliminarAsync(id, materialId, token).ConfigureAwait(false);
                return 0;
            }, token);
        }
    }
}
