using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Conteos;
using Xunit;

namespace SteeLog.Tests
{
    public class ConteoApplicationTests
    {
        private sealed class Uow : IUnitOfWork
        {
            public int Commits, Rollbacks;
            public Task BeginAsync(CancellationToken t = default) => Task.CompletedTask;
            public Task CommitAsync(CancellationToken t = default) { Commits++; return Task.CompletedTask; }
            public Task RollbackAsync() { Rollbacks++; return Task.CompletedTask; }
        }
        private sealed class Repo : IConteosEscritura
        {
            public Conteo Conteo; public int Guardados; public decimal Cantidad;
            public Task<int> CrearAsync(string p, CancellationToken t) => Task.FromResult(1);
            public Task<Conteo> BloquearAsync(int id, CancellationToken t) => Task.FromResult(Conteo);
            public Task GuardarCantidadAsync(int id, int m, decimal c, CancellationToken t) { Guardados++; Cantidad = c; return Task.CompletedTask; }
            public Task QuitarDetalleAsync(int id, int d, CancellationToken t) { Guardados++; return Task.CompletedTask; }
            public Task GuardarCierreAsync(int id, string u, string o, IDictionary<int, decimal> s, IReadOnlyDictionary<int, decimal> d, CancellationToken t) { Guardados++; return Task.CompletedTask; }
            public Task GuardarCancelacionAsync(int id, CancellationToken t) { Guardados++; return Task.CompletedTask; }
            public Task EliminarAsync(int id, CancellationToken t) { Guardados++; return Task.CompletedTask; }
        }
        private sealed class Inventario : IInventarioEscritura
        {
            public int Escrituras;
            public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken t = default) => Task.FromResult<IDictionary<int, decimal>>(new Dictionary<int, decimal> { [1] = 3m });
            public Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken t = default) { Escrituras++; return Task.CompletedTask; }
        }
        private static Session Actor(string email, Role role = Role.Galpon) { var s = new Session(); s.Start(new Identity(email, role)); return s; }
        private static Conteo Abierto(string propietario) => new Conteo(1, propietario, EstadoConteo.Abierto, new[] { new DetalleConteo(1, 1, 1.25m) });
        [Fact]
        public async Task CompatibilidadHistoricaEsExplicitaYNoPermiteSuplantacion()
        {
            var repo = new Repo { Conteo = Abierto(" GALPON ") }; var uow = new Uow(); var inv = new Inventario();
            await new OperacionesConteo(Actor("galpon@tesis.local"), uow, repo, inv).CargarAsync(1, 1, .25m);
            Assert.Equal(1.50m, repo.Cantidad); Assert.Equal(1, uow.Commits);
            repo.Conteo = Abierto("galpon");
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new OperacionesConteo(Actor("galpon@otro.local"), uow, repo, inv).CargarAsync(1, 1, 1));
            Assert.Equal(1, repo.Guardados); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact]
        public async Task CierreNoAjustaStockYAdminNoPuedeEditarAjeno()
        {
            var repo = new Repo { Conteo = Abierto("galpon@tesis.local") }; var uow = new Uow(); var inv = new Inventario();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new OperacionesConteo(Actor("admin@tesis.local", Role.Admin), uow, repo, inv).CorregirAsync(1, 1, 2));
            await new OperacionesConteo(Actor("galpon@tesis.local"), uow, repo, inv).FinalizarAsync(1, "nota");
            Assert.Equal(0, inv.Escrituras); Assert.Equal(1, repo.Guardados); Assert.Equal(1, uow.Rollbacks); Assert.Equal(1, uow.Commits);
        }
    }
}
