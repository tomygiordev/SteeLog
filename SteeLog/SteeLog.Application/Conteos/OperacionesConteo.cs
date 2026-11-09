using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Conteos;

namespace SteeLog.Core.Application.Conteos
{
    // El repositorio y el inventario comparten la transacción del UoW de la operación.
    public interface IConteosEscritura
    {
        Task<int> CrearAsync(string propietario, CancellationToken token);
        Task<Conteo> BloquearAsync(int id, CancellationToken token);
        Task GuardarCantidadAsync(int id, int materialId, decimal cantidad, CancellationToken token);
        Task QuitarDetalleAsync(int id, int detalleId, CancellationToken token);
        Task GuardarCierreAsync(int id, string usuario, string observaciones, IDictionary<int, decimal> stock, IReadOnlyDictionary<int, decimal> diferencias, CancellationToken token);
        Task GuardarCancelacionAsync(int id, CancellationToken token);
        Task EliminarAsync(int id, CancellationToken token);
    }
    public sealed class OperacionesConteo
    {
        private readonly Session session; private readonly IUnitOfWork uow; private readonly IConteosEscritura repo; private readonly IInventarioEscritura inventario;
        public OperacionesConteo(Session session, IUnitOfWork uow, IConteosEscritura repo, IInventarioEscritura inventario)
        { this.session = session; this.uow = uow; this.repo = repo; this.inventario = inventario; }
        private string Actor() { session.Require(Role.Galpon); return session.Current.Email.Trim(); }
        private static void Propietario(Conteo conteo, string actor)
        {
            var almacenado = conteo.Propietario?.Trim();
            var coincide = string.Equals(almacenado, actor, StringComparison.OrdinalIgnoreCase)
                || (string.Equals(almacenado, "admin", StringComparison.OrdinalIgnoreCase) && string.Equals(actor, "admin@tesis.local", StringComparison.OrdinalIgnoreCase))
                || (string.Equals(almacenado, "galpon", StringComparison.OrdinalIgnoreCase) && string.Equals(actor, "galpon@tesis.local", StringComparison.OrdinalIgnoreCase));
            if (!coincide) throw new UnauthorizedAccessException("El conteo pertenece a otro usuario.");
        }
        private Task<T> Transaccion<T>(Func<Task<T>> trabajo, CancellationToken token)
        {
            return Common.Transaccion.EjecutarAsync(uow, _ => trabajo(), token);
        }
        public Task<int> CrearAsync(CancellationToken token = default)
        { var actor = Actor(); return Transaccion(() => repo.CrearAsync(actor, token), token); }
        public Task<bool> CargarAsync(int id, int material, decimal cantidad, CancellationToken token = default)
        {
            var actor = Actor(); CantidadConteo.Validar(cantidad);
            return Transaccion(async () => { var c = await repo.BloquearAsync(id, token).ConfigureAwait(false); Propietario(c, actor); var total = c.Acumular(material, cantidad); await repo.GuardarCantidadAsync(id, material, total, token).ConfigureAwait(false); return true; }, token);
        }
        public Task<bool> CorregirAsync(int id, int material, decimal cantidad, CancellationToken token = default)
        {
            var actor = Actor(); CantidadConteo.Validar(cantidad);
            return Transaccion(async () => { var c = await repo.BloquearAsync(id, token).ConfigureAwait(false); Propietario(c, actor); c.Corregir(material, cantidad); await repo.GuardarCantidadAsync(id, material, cantidad, token).ConfigureAwait(false); return true; }, token);
        }
        public Task<bool> QuitarAsync(int id, int detalle, CancellationToken token = default)
        {
            var actor = Actor(); return Transaccion(async () => { var c = await repo.BloquearAsync(id, token).ConfigureAwait(false); Propietario(c, actor); c.Quitar(detalle); await repo.QuitarDetalleAsync(id, detalle, token).ConfigureAwait(false); return true; }, token);
        }
        public Task<bool> FinalizarAsync(int id, string observaciones, CancellationToken token = default)
        {
            var actor = Actor(); return Transaccion(async () => { var c = await repo.BloquearAsync(id, token).ConfigureAwait(false); Propietario(c, actor); c.ExigirAbierto(); if (c.Detalles.Count == 0) throw new InvalidOperationException("No se puede finalizar un conteo sin materiales cargados.");
                var stock = await inventario.BloquearAsync(c.Detalles.Select(x => x.MaterialId).OrderBy(x => x).ToArray(), token).ConfigureAwait(false);
                var dif = c.Finalizar(stock); await repo.GuardarCierreAsync(id, actor, observaciones, stock, dif, token).ConfigureAwait(false); return true; }, token);
        }
        public Task<bool> CancelarAsync(int id, CancellationToken token = default)
        {
            var actor = Actor(); var admin = session.Role == Role.Admin;
            return Transaccion(async () => { var c = await repo.BloquearAsync(id, token).ConfigureAwait(false); if (!admin) Propietario(c, actor); c.Cancelar(); await repo.GuardarCancelacionAsync(id, token).ConfigureAwait(false); return true; }, token);
        }
        public Task<bool> EliminarAsync(int id, CancellationToken token = default)
        {
            session.Require(Role.Admin); return Transaccion(async () => { var c = await repo.BloquearAsync(id, token).ConfigureAwait(false); c.ExigirEliminable(); await repo.EliminarAsync(id, token).ConfigureAwait(false); return true; }, token);
        }
    }
}
