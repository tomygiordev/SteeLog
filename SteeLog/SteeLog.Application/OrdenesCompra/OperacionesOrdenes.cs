using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Inventario;
using SteeLog.Domain.OrdenesCompra;

namespace SteeLog.Core.Application.OrdenesCompra
{
    public sealed class CabeceraOrden
    {
        public DateTime Fecha { get; set; }
        public string Numero { get; set; }
        public string Remito { get; set; }
        public int ProveedorId { get; set; }
        public decimal Flete { get; set; }
        public string Observaciones { get; set; }
        public Cabecera AEntidad() => new Cabecera(Fecha, Numero, Remito, ProveedorId, Flete, Observaciones);
        internal static CabeceraOrden Desde(Cabecera cabecera) => new CabeceraOrden {
            Fecha = cabecera.Fecha, Numero = cabecera.Numero, Remito = cabecera.Remito,
            ProveedorId = cabecera.ProveedorId, Flete = cabecera.Flete, Observaciones = cabecera.Observaciones
        };
    }

    public sealed class LineaNueva
    {
        public int MaterialId { get; set; }
        public decimal Pedida { get; set; }
        public string Observacion { get; set; }
    }

    // Los métodos de bloqueo deben utilizar la misma conexión y transacción del UoW scoped.
    public interface IOrdenesEscritura
    {
        Task<OrdenCompra> BloquearAsync(int id, CancellationToken cancellationToken = default);
        Task<int> CrearAsync(CabeceraOrden cabecera, IReadOnlyList<LineaNueva> lineas, string usuario, CancellationToken cancellationToken = default);
        Task EditarCabeceraAsync(int id, CabeceraOrden cabecera, CancellationToken cancellationToken = default);
        Task ReemplazarLineasAsync(int id, IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default);
        Task EliminarAsync(int id, CancellationToken cancellationToken = default);
        Task GuardarRecepcionAsync(int id, OrdenCompra orden, IReadOnlyDictionary<int, string> observaciones, IReadOnlyDictionary<int, decimal> snapshots, string usuario, string observacion, CancellationToken cancellationToken = default);
        Task GuardarMaterialAsync(int id, int lineaId, int materialId, decimal stockAntes, decimal? stockDespues, CancellationToken cancellationToken = default);
    }

    internal static class OrdenesOperacion
    {
        public static IReadOnlyList<LineaNueva> ValidarLineas(IEnumerable<LineaNueva> lineas, Cabecera cabecera)
        {
            if (lineas == null) throw new ArgumentNullException(nameof(lineas));
            var lista = lineas.Select(x => x == null ? null : new LineaNueva { MaterialId = x.MaterialId, Pedida = x.Pedida, Observacion = x.Observacion }).ToList();
            new OrdenCompra(0, cabecera, EstadoOrden.Pendiente, lista.Select(x => x == null ? null : new LineaOrden(0, x.MaterialId, x.Pedida, 0)));
            return lista;
        }
        public static Task<T> Transaccion<T>(IUnitOfWork uow, Func<Task<T>> trabajo, CancellationToken cancellationToken)
        {
            return Common.Transaccion.EjecutarAsync(uow, _ => trabajo(), cancellationToken);
        }
    }

    public sealed class CrearOrden
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IOrdenesEscritura repo;
        public CrearOrden(Session actor, IUnitOfWork uow, IOrdenesEscritura repo) { this.actor = actor; this.uow = uow; this.repo = repo; }
        public Task<int> EjecutarAsync(CabeceraOrden cabecera, IEnumerable<LineaNueva> lineas, CancellationToken cancellationToken = default)
        {
            actor.Require(Role.Admin);
            if (cabecera == null) throw new ArgumentNullException(nameof(cabecera));
            var entidad = cabecera.AEntidad();
            cabecera = CabeceraOrden.Desde(entidad);
            var validadas = OrdenesOperacion.ValidarLineas(lineas, entidad);
            var usuario = actor.Current.Email.Split('@')[0];
            return OrdenesOperacion.Transaccion(uow, () => repo.CrearAsync(cabecera, validadas, usuario, cancellationToken), cancellationToken);
        }
    }

    public sealed class EditarOrden
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IOrdenesEscritura repo;
        public EditarOrden(Session actor, IUnitOfWork uow, IOrdenesEscritura repo) { this.actor = actor; this.uow = uow; this.repo = repo; }
        public Task<int> EjecutarAsync(int id, CabeceraOrden cabecera, IEnumerable<LineaNueva> reemplazo = null, CancellationToken cancellationToken = default)
        {
            actor.Require(Role.Admin);
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (cabecera == null) throw new ArgumentNullException(nameof(cabecera));
            var entidad = cabecera.AEntidad();
            cabecera = CabeceraOrden.Desde(entidad);
            var lineas = reemplazo == null ? null : OrdenesOperacion.ValidarLineas(reemplazo, entidad);
            return OrdenesOperacion.Transaccion(uow, async () => {
                var orden = await repo.BloquearAsync(id, cancellationToken).ConfigureAwait(false);
                orden.EditarCabecera(entidad);
                if (lineas != null) orden.ExigirPendiente();
                await repo.EditarCabeceraAsync(id, cabecera, cancellationToken).ConfigureAwait(false);
                if (lineas != null) await repo.ReemplazarLineasAsync(id, lineas, cancellationToken).ConfigureAwait(false);
                return id;
            }, cancellationToken);
        }
    }

    public sealed class EliminarOrden
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IOrdenesEscritura repo;
        public EliminarOrden(Session actor, IUnitOfWork uow, IOrdenesEscritura repo) { this.actor = actor; this.uow = uow; this.repo = repo; }
        public Task<bool> EjecutarAsync(int id, CancellationToken cancellationToken = default)
        {
            actor.Require(Role.Admin);
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            return OrdenesOperacion.Transaccion(uow, async () => {
                var orden = await repo.BloquearAsync(id, cancellationToken).ConfigureAwait(false);
                orden.ExigirPendiente();
                await repo.EliminarAsync(id, cancellationToken).ConfigureAwait(false);
                return true;
            }, cancellationToken);
        }
    }

    public sealed class ConfirmarRecepcion
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IOrdenesEscritura repo; private readonly IInventarioEscritura inventario;
        public ConfirmarRecepcion(Session actor, IUnitOfWork uow, IOrdenesEscritura repo, IInventarioEscritura inventario) { this.actor = actor; this.uow = uow; this.repo = repo; this.inventario = inventario; }
        public Task<EstadoOrden> EjecutarAsync(int id, IReadOnlyDictionary<int, decimal> acumulados, IReadOnlyDictionary<int, string> observaciones, string observacion, CancellationToken cancellationToken = default)
        {
            actor.Require(Role.Galpon);
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            var usuario = actor.Current.Email.Split('@')[0];
            var cantidades = acumulados == null ? null : acumulados.ToDictionary(x => x.Key, x => x.Value);
            var notas = observaciones == null ? null : observaciones.ToDictionary(x => x.Key, x => x.Value);
            return OrdenesOperacion.Transaccion(uow, async () => {
                var orden = await repo.BloquearAsync(id, cancellationToken).ConfigureAwait(false);
                var delta = orden.Recibir(cantidades);
                var materiales = orden.Lineas.Where(x => delta[x.Id] > 0).Select(x => x.MaterialId).OrderBy(x => x).ToArray();
                var saldos = await inventario.BloquearAsync(materiales, cancellationToken).ConfigureAwait(false);
                var snapshots = new Dictionary<int, decimal>();
                foreach (var linea in orden.Lineas.Where(x => delta[x.Id] > 0).OrderBy(x => x.MaterialId)) {
                    var saldo = SaldoInventario.Aplicar(saldos[linea.MaterialId], delta[linea.Id]);
                    await inventario.GuardarSaldoAsync(linea.MaterialId, saldo, cancellationToken).ConfigureAwait(false);
                    snapshots.Add(linea.Id, saldo);
                }
                await repo.GuardarRecepcionAsync(id, orden, notas, snapshots, usuario, observacion, cancellationToken).ConfigureAwait(false);
                return orden.Estado;
            }, cancellationToken);
        }
    }

    public sealed class CambiarMaterialOrden
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IOrdenesEscritura repo; private readonly IInventarioEscritura inventario;
        public CambiarMaterialOrden(Session actor, IUnitOfWork uow, IOrdenesEscritura repo, IInventarioEscritura inventario) { this.actor = actor; this.uow = uow; this.repo = repo; this.inventario = inventario; }
        public Task<bool> EjecutarAsync(int id, int lineaId, int materialNuevo, CancellationToken cancellationToken = default)
        {
            actor.Require(Role.Admin);
            if (id <= 0 || lineaId <= 0 || materialNuevo <= 0) throw new ArgumentOutOfRangeException();
            return OrdenesOperacion.Transaccion(uow, async () => {
                var orden = await repo.BloquearAsync(id, cancellationToken).ConfigureAwait(false);
                var linea = orden.Lineas.Single(x => x.Id == lineaId);
                int anterior = linea.MaterialId;
                if (anterior == materialNuevo) return true;
                int recibido = orden.CambiarMaterial(lineaId, materialNuevo);
                var saldos = await inventario.BloquearAsync(new[] { anterior, materialNuevo }.OrderBy(x => x).ToArray(), cancellationToken).ConfigureAwait(false);
                var previo = saldos[materialNuevo];
                var restante = SaldoInventario.Aplicar(saldos[anterior], -recibido);
                var destino = SaldoInventario.Aplicar(previo, recibido);
                if (recibido > 0) {
                    await inventario.GuardarSaldoAsync(anterior, restante, cancellationToken).ConfigureAwait(false);
                    await inventario.GuardarSaldoAsync(materialNuevo, destino, cancellationToken).ConfigureAwait(false);
                }
                await repo.GuardarMaterialAsync(id, lineaId, materialNuevo, previo, recibido > 0 || linea.TieneSnapshotRecepcion ? (decimal?)destino : null, cancellationToken).ConfigureAwait(false);
                return true;
            }, cancellationToken);
        }
    }
}
