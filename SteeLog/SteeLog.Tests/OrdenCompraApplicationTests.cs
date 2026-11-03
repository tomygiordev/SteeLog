using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Domain.OrdenesCompra;
using Xunit;

namespace SteeLog.Tests
{
    public class OrdenCompraApplicationTests
    {
        private sealed class Almacen : IUnitOfWork, IOrdenesEscritura, IInventarioEscritura
        {
            public int Begins, Commits, Rollbacks;
            public bool FallarAlGuardar;
            public Action AlGuardar;
            public readonly List<int> Bloqueados = new List<int>();
            public readonly Dictionary<int, decimal> Saldos = new Dictionary<int, decimal> { [10] = 4, [20] = 8 };
            public OrdenCompra Orden = new OrdenCompra(1, new Cabecera(new DateTime(2026, 1, 1), "OC", null, 1, 0, null), EstadoOrden.RecibidaParcial, new[] { new LineaOrden(1, 10, 5, 2) });
            public Task BeginAsync(CancellationToken cancellationToken = default) { Begins++; return Task.CompletedTask; }
            public Task CommitAsync(CancellationToken cancellationToken = default) { Commits++; return Task.CompletedTask; }
            public Task RollbackAsync() { Rollbacks++; return Task.CompletedTask; }
            public Task<OrdenCompra> BloquearAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Orden);
            public Task<int> CrearAsync(CabeceraOrden c, IReadOnlyList<LineaNueva> l, string u, CancellationToken cancellationToken = default) => Task.FromResult(17);
            public Task EditarCabeceraAsync(int id, CabeceraOrden c, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task ReemplazarLineasAsync(int id, IReadOnlyList<LineaNueva> l, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task EliminarAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task GuardarRecepcionAsync(int id, OrdenCompra o, IReadOnlyDictionary<int, string> obs, IReadOnlyDictionary<int, decimal> snapshots, string u, string observacion, CancellationToken cancellationToken = default) { if (FallarAlGuardar) throw new InvalidOperationException(); AlGuardar?.Invoke(); return Task.CompletedTask; }
            public Task GuardarMaterialAsync(int id, int l, int m, decimal a, decimal? d, CancellationToken cancellationToken = default) { if (FallarAlGuardar) throw new InvalidOperationException(); return Task.CompletedTask; }
            public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default) { Bloqueados.AddRange(ids); return Task.FromResult<IDictionary<int, decimal>>(Saldos); }
            public Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken cancellationToken = default) { Saldos[id] = saldo; return Task.CompletedTask; }
        }
        private static Session Actor(Role role) { var s = new Session(); if (role != Role.None) s.Start(new Identity("test@example.test", role)); return s; }
        [Theory]
        [InlineData(Role.None)]
        [InlineData(Role.Galpon)]
        public void CrearExigeAdminAntesDeAccederABase(Role role)
        {
            var db = new Almacen(); var caso = new CrearOrden(Actor(role), db, db);
            Assert.Throws<UnauthorizedAccessException>(() => { _ = caso.EjecutarAsync(null, null); });
            Assert.Equal(0, db.Begins);
        }
        [Theory]
        [InlineData(Role.None)]
        [InlineData(Role.Galpon)]
        public void OperacionesAdministrativasRechazanAntesDeTransaccion(Role role)
        {
            var db = new Almacen(); var actor = Actor(role);
            Assert.Throws<UnauthorizedAccessException>(() => { _ = new EditarOrden(actor, db, db).EjecutarAsync(1, null); });
            Assert.Throws<UnauthorizedAccessException>(() => { _ = new EliminarOrden(actor, db, db).EjecutarAsync(1); });
            Assert.Throws<UnauthorizedAccessException>(() => { _ = new CambiarMaterialOrden(actor, db, db, db).EjecutarAsync(1, 1, 20); });
            Assert.Equal(0, db.Begins);
        }
        [Fact]
        public void RecepcionNoAutenticadaSeRechazaAntesDeTransaccion()
        {
            var db = new Almacen(); var caso = new ConfirmarRecepcion(Actor(Role.None), db, db, db);
            Assert.Throws<UnauthorizedAccessException>(() => { _ = caso.EjecutarAsync(1, null, null, null); });
            Assert.Equal(0, db.Begins);
        }
        [Fact]
        public async Task RecepcionHaceCommitYAplicaSoloIncremento()
        {
            var db = new Almacen(); var caso = new ConfirmarRecepcion(Actor(Role.Galpon), db, db, db);
            var estado = await caso.EjecutarAsync(9, new Dictionary<int, decimal> { [1] = 3 }, new Dictionary<int, string>(), "");
            Assert.Equal(EstadoOrden.RecibidaParcial, estado); Assert.Equal(5, db.Saldos[10]);
            Assert.Equal(1, db.Begins); Assert.Equal(1, db.Commits); Assert.Equal(0, db.Rollbacks);
        }
        [Fact]
        public async Task FalloDePersistenciaSolicitaRollback()
        {
            var db = new Almacen { FallarAlGuardar = true }; var caso = new ConfirmarRecepcion(Actor(Role.Admin), db, db, db);
            await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(9, new Dictionary<int, decimal> { [1] = 3 }, new Dictionary<int, string>(), ""));
            Assert.Equal(0, db.Commits); Assert.Equal(1, db.Rollbacks);
        }
        [Fact]
        public async Task CancelacionAntesDeCommitSolicitaRollback()
        {
            var token = new CancellationTokenSource();
            var db = new Almacen { AlGuardar = token.Cancel };
            var caso = new ConfirmarRecepcion(Actor(Role.Admin), db, db, db);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caso.EjecutarAsync(9, new Dictionary<int, decimal> { [1] = 3 }, new Dictionary<int, string>(), "", token.Token));
            Assert.Equal(0, db.Commits); Assert.Equal(1, db.Rollbacks);
        }
        [Fact]
        public async Task CancelacionPreviaNoAbreTransaccion()
        {
            var db = new Almacen(); var token = new CancellationTokenSource(); token.Cancel();
            var caso = new EliminarOrden(Actor(Role.Admin), db, db);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caso.EjecutarAsync(9, token.Token));
            Assert.Equal(0, db.Begins);
        }
        [Fact]
        public async Task CambioMaterialBloqueaEnOrdenYVerificaSaldo()
        {
            var db = new Almacen(); var caso = new CambiarMaterialOrden(Actor(Role.Admin), db, db, db);
            await caso.EjecutarAsync(1, 1, 20);
            Assert.Equal(new[] { 10, 20 }, db.Bloqueados); Assert.Equal(2, db.Saldos[10]); Assert.Equal(10, db.Saldos[20]);
            Assert.Equal(1, db.Commits);
        }
        [Fact]
        public async Task CambioConSaldoInsuficienteNoGuarda()
        {
            var db = new Almacen(); db.Saldos[10] = 1;
            var caso = new CambiarMaterialOrden(Actor(Role.Admin), db, db, db);
            await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(1, 1, 20));
            Assert.Equal(1, db.Saldos[10]); Assert.Equal(8, db.Saldos[20]); Assert.Equal(1, db.Rollbacks);
        }
    }
}
