using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Domain.Conteos;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class ConteosMovimientosOrdenesResultadoInciertoTests
    {
        private static Session Sesion(Role rol = Role.Admin) { var s = new Session(); s.Start(new Identity("galpon@tesis.local", rol)); return s; }

        private sealed class UowFake : IUnitOfWork
        {
            public int Begins, Commits, Rollbacks; public bool CancelarEnCommit;
            public Task BeginAsync(CancellationToken t = default) { Begins++; return Task.CompletedTask; }
            public Task CommitAsync(CancellationToken t = default) { Commits++; if (CancelarEnCommit) throw new OperationCanceledException(); return Task.CompletedTask; }
            public Task RollbackAsync() { Rollbacks++; return Task.CompletedTask; }
        }

        private sealed class InventarioFake : IInventarioEscritura
        {
            public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken t = default)
            { var d = new Dictionary<int, decimal>(); foreach (var id in ids) d[id] = 5m; return Task.FromResult<IDictionary<int, decimal>>(d); }
            public Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken t = default) => Task.CompletedTask;
        }

        // ---------- Conteos ----------
        private sealed class ConteosRepo : IConteosEscritura
        {
            public Task<int> CrearAsync(string p, CancellationToken t) => Task.FromResult(1);
            public Task<Conteo> BloquearAsync(int id, CancellationToken t) => throw new NotSupportedException();
            public Task GuardarCantidadAsync(int id, int m, decimal c, CancellationToken t) => throw new NotSupportedException();
            public Task QuitarDetalleAsync(int id, int d, CancellationToken t) => throw new NotSupportedException();
            public Task GuardarCierreAsync(int id, string u, string o, IDictionary<int, decimal> s, IReadOnlyDictionary<int, decimal> d, CancellationToken t) => throw new NotSupportedException();
            public Task GuardarCancelacionAsync(int id, CancellationToken t) => throw new NotSupportedException();
            public Task EliminarAsync(int id, CancellationToken t) => throw new NotSupportedException();
        }

        [Fact] public async Task ConteoCancelacionEnCommitEsResultadoIncierto()
        {
            var uow = new UowFake { CancelarEnCommit = true };
            var servicio = new OperacionesConteo(Sesion(Role.Galpon), uow, new ConteosRepo(), new InventarioFake());
            var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => servicio.CrearAsync());
            Assert.IsType<OperationCanceledException>(error.InnerException);
            Assert.Equal(1, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task ConteoTokenCanceladoAntesDeBeginNoEsIncierto()
        {
            var uow = new UowFake(); using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var servicio = new OperacionesConteo(Sesion(Role.Galpon), uow, new ConteosRepo(), new InventarioFake());
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.CrearAsync(cts.Token));
                Assert.Equal(0, uow.Begins); Assert.Equal(0, uow.Commits); Assert.Equal(0, uow.Rollbacks);
            }
        }

        // ---------- Movimientos ----------
        private sealed class MovimientosRepo : IMovimientosEscritura
        {
            public Exception ErrorEnCrearCabecera; public int CabecerasEliminadas;
            public Task<MovimientoPersistido> BloquearAsync(int id, CancellationToken t = default)
                => Task.FromResult(new MovimientoPersistido { Id = id, TipoNombre = "Ingreso", Cabecera = new CabeceraMovimiento { TipoMovimientoId = 1, TipoDestino = "UBICACION" } });
            public Task<TipoMovimiento> TipoAsync(int id, CancellationToken t = default) => Task.FromResult(new TipoMovimiento { Nombre = "Ingreso" });
            public Task<IDictionary<int, int>> ObtenerMaterialesRecortesAsync(IReadOnlyList<int> ids, CancellationToken t = default) => Task.FromResult<IDictionary<int, int>>(new Dictionary<int, int>());
            public Task<IDictionary<int, decimal>> BloquearRecortesAsync(IReadOnlyList<int> ids, CancellationToken t = default) => Task.FromResult<IDictionary<int, decimal>>(new Dictionary<int, decimal>());
            public Task<IDictionary<int, InsumoBloqueado>> BloquearInsumosAsync(IReadOnlyList<int> ids, CancellationToken t = default) => Task.FromResult<IDictionary<int, InsumoBloqueado>>(new Dictionary<int, InsumoBloqueado>());
            public Task<decimal> GuardarRecorteAsync(int id, decimal saldo, CancellationToken t = default) => throw new NotSupportedException();
            public Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, decimal cantidadInicial, CancellationToken t = default) => throw new NotSupportedException();
            public Task<int> CrearCabeceraAsync(CabeceraMovimiento c, CancellationToken t = default) { if (ErrorEnCrearCabecera != null) throw ErrorEnCrearCabecera; return Task.FromResult(7); }
            public Task ActualizarCabeceraAsync(int id, CabeceraMovimiento c, CancellationToken t = default) => Task.CompletedTask;
            public Task AgregarLineaAsync(int movimientoId, LineaMovimiento l, decimal saldo, CancellationToken t = default) => Task.CompletedTask;
            public Task RegistrarTrasladoAsync(int movimientoId, TrasladoInsumo x, CancellationToken t = default) => Task.CompletedTask;
            public Task RevertirTrasladoAsync(TrasladoInsumo x, CancellationToken t = default) => Task.CompletedTask;
            public Task EliminarLineasYTrasladosAsync(int id, CancellationToken t = default) => Task.CompletedTask;
            public Task EliminarCabeceraAsync(int id, CancellationToken t = default) { CabecerasEliminadas++; return Task.CompletedTask; }
        }
        private static MovimientoSolicitud Solicitud() => new MovimientoSolicitud
        {
            Cabecera = new CabeceraMovimiento { Fecha = new DateTime(2025, 1, 1), Remito = "R-1", OrigenId = 2, TipoOrigen = "UBICACION", DestinoId = 4, TipoDestino = "UBICACION", TipoMovimientoId = 1 },
            Lineas = new List<LineaMovimiento> { new LineaMovimiento { MaterialId = 1, Cantidad = 1m } },
            OrigenNombre = "Deposito", DestinoNombre = "Destino"
        };

        [Fact] public async Task MovimientoCancelacionEnCommitEsResultadoIncierto()
        {
            var uow = new UowFake { CancelarEnCommit = true };
            var servicio = new RegistrarMovimiento(Sesion(Role.Galpon), uow, new MovimientosRepo(), new InventarioFake());
            var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => servicio.EjecutarAsync(Solicitud()));
            Assert.IsType<OperationCanceledException>(error.InnerException);
            Assert.Equal(1, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task MovimientoTokenCanceladoAntesDeBeginNoEsIncierto()
        {
            var uow = new UowFake(); using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var servicio = new RegistrarMovimiento(Sesion(Role.Galpon), uow, new MovimientosRepo(), new InventarioFake());
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.EjecutarAsync(Solicitud(), cts.Token));
                Assert.Equal(0, uow.Begins); Assert.Equal(0, uow.Commits); Assert.Equal(0, uow.Rollbacks);
            }
        }
        [Fact] public async Task MovimientoFalloDeRepositorioAntesDeCommitSePropagaSinMarcarIncierto()
        {
            var uow = new UowFake(); var original = new InvalidOperationException("fallo previo al commit");
            var servicio = new RegistrarMovimiento(Sesion(Role.Galpon), uow, new MovimientosRepo { ErrorEnCrearCabecera = original }, new InventarioFake());
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EjecutarAsync(Solicitud()));
            Assert.Same(original, error);
            Assert.Equal(0, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task MovimientoEliminarCancelacionEnCommitEsResultadoIncierto()
        {
            var uow = new UowFake { CancelarEnCommit = true }; var repo = new MovimientosRepo();
            var servicio = new EliminarMovimiento(Sesion(Role.Galpon), uow, repo, new InventarioFake());
            var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => servicio.EjecutarAsync(3));
            Assert.IsType<OperationCanceledException>(error.InnerException);
            Assert.Equal(1, repo.CabecerasEliminadas); Assert.Equal(1, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }

        // ---------- Órdenes de compra ----------
        private sealed class OrdenesRepo : IOrdenesEscritura
        {
            public Task<SteeLog.Domain.OrdenesCompra.OrdenCompra> BloquearAsync(int id, CancellationToken t = default) => throw new NotSupportedException();
            public Task<int> CrearAsync(CabeceraOrden c, IReadOnlyList<LineaNueva> l, string u, CancellationToken t = default) => Task.FromResult(17);
            public Task EditarCabeceraAsync(int id, CabeceraOrden c, CancellationToken t = default) => throw new NotSupportedException();
            public Task ReemplazarLineasAsync(int id, IReadOnlyList<LineaNueva> l, CancellationToken t = default) => throw new NotSupportedException();
            public Task EliminarAsync(int id, CancellationToken t = default) => throw new NotSupportedException();
            public Task GuardarRecepcionAsync(int id, SteeLog.Domain.OrdenesCompra.OrdenCompra o, IReadOnlyDictionary<int, string> obs, IReadOnlyDictionary<int, decimal> s, string u, string ob, CancellationToken t = default) => throw new NotSupportedException();
            public Task GuardarMaterialAsync(int id, int l, int m, decimal a, decimal? d, CancellationToken t = default) => throw new NotSupportedException();
        }
        private static CabeceraOrden CabeceraValida() => new CabeceraOrden { Fecha = new DateTime(2026, 1, 1), Numero = "OC-1", ProveedorId = 1 };
        private static IEnumerable<LineaNueva> LineasValidas() => new[] { new LineaNueva { MaterialId = 10, Pedida = 2m } };

        [Fact] public async Task OrdenCancelacionEnCommitEsResultadoIncierto()
        {
            var uow = new UowFake { CancelarEnCommit = true };
            var servicio = new CrearOrden(Sesion(Role.Admin), uow, new OrdenesRepo());
            var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => servicio.EjecutarAsync(CabeceraValida(), LineasValidas()));
            Assert.IsType<OperationCanceledException>(error.InnerException);
            Assert.Equal(1, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task OrdenTokenCanceladoAntesDeBeginNoEsIncierto()
        {
            var uow = new UowFake(); using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var servicio = new CrearOrden(Sesion(Role.Admin), uow, new OrdenesRepo());
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.EjecutarAsync(CabeceraValida(), LineasValidas(), cts.Token));
                Assert.Equal(0, uow.Begins); Assert.Equal(0, uow.Commits); Assert.Equal(0, uow.Rollbacks);
            }
        }
    }
}
