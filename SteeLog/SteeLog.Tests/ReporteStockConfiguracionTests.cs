using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Reportes;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class ReporteStockConfiguracionTests
    {
        private sealed class Destino : IReporteStockConfiguracion
        {
            public int Llamadas;
            public Task GuardarAsync(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, CancellationToken token = default)
            { Llamadas++; return Task.CompletedTask; }
        }
        private sealed class Unidad : IUnitOfWork
        {
            public Task BeginAsync(CancellationToken token = default) => Task.CompletedTask;
            public Task CommitAsync(CancellationToken token = default) => Task.CompletedTask;
            public Task RollbackAsync() => Task.CompletedTask;
        }
        private sealed class UnidadCancelada : IUnitOfWork
        {
            public bool Revertida;
            public Task BeginAsync(CancellationToken token = default) => Task.CompletedTask;
            public Task CommitAsync(CancellationToken token = default) => Task.FromException(new OperationCanceledException());
            public Task RollbackAsync() { Revertida = true; return Task.CompletedTask; }
        }
        [Fact]
        public async Task CancelacionAlEntrarCommitNoEsResultadoIncierto()
        {
            var session = new Session(); session.Start(new Identity("test@example.test", Role.Admin));
            var unidad = new UnidadCancelada();
            var caso = new GuardarReporteStockConfiguracion(session, unidad, new Destino());
            await Assert.ThrowsAsync<OperationCanceledException>(() => caso.EjecutarAsync(new ReporteStockSeccion[0], new int[0]));
            Assert.True(unidad.Revertida);
        }
        private static GuardarReporteStockConfiguracion Crear(Role rol, Destino destino)
        {
            var session = new Session(); session.Start(new Identity("test@example.test", rol));
            return new GuardarReporteStockConfiguracion(session, new Unidad(), destino);
        }
        [Fact]
        public async Task RechazaRolGalponAntesDeEscribir()
        {
            var destino = new Destino();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Crear(Role.Galpon, destino).EjecutarAsync(new ReporteStockSeccion[0], new int[0]));
            Assert.Equal(0, destino.Llamadas);
        }
        [Fact]
        public async Task RechazaMaterialRepetidoEntreSecciones()
        {
            var destino = new Destino();
            var secciones = new[] { new ReporteStockSeccion { Nombre = "A", Materiales = new List<int> { 7 } }, new ReporteStockSeccion { Nombre = "B", Materiales = new List<int> { 7 } } };
            await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(Role.Admin, destino).EjecutarAsync(secciones, new int[0]));
            Assert.Equal(0, destino.Llamadas);
        }
        [Fact]
        public async Task RechazaNombreRepetidoSinDistinguirMayusculas()
        {
            var destino = new Destino();
            var secciones = new[] { new ReporteStockSeccion { Nombre = " A " }, new ReporteStockSeccion { Nombre = "a" } };
            await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(Role.Admin, destino).EjecutarAsync(secciones, new int[0]));
            Assert.Equal(0, destino.Llamadas);
        }
        [Fact]
        public async Task AdminPuedeGuardarConfiguracionVacia()
        {
            var destino = new Destino();
            await Crear(Role.Admin, destino).EjecutarAsync(new ReporteStockSeccion[0], new int[0]);
            Assert.Equal(1, destino.Llamadas);
        }
    }
}
