using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Activos;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Domain.Activos;
using SteeLog.Domain.Catalogos;
using Xunit;
namespace SteeLog.Tests
{
    public sealed class CatalogosResultadoInciertoTests
    {
        [Fact] public async Task CatalogoCommitConfirmadoPeroRespuestaPerdidaNoSeReenvia()
        {
            var uow = new Uow(); var repo = new CatalogosRepo();
            var servicio = new GestionarCatalogos(Sesion(), uow, repo, new Fletes());
            var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => servicio.CrearCategoriaAsync("A", false));
            Assert.Same(uow.ErrorCommit, error.InnerException); Assert.Contains("verificar persistencia antes de reintentar", error.Message);
            Assert.True(uow.Confirmado); Assert.Equal(1, repo.Altas); Assert.Equal(1, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task HerramientaCommitConfirmadoPeroRespuestaPerdidaNoSeReenvia()
        {
            var uow = new Uow(); var repo = new HerramientasRepo();
            var servicio = new GestionarHerramientas(Sesion(), uow, repo);
            var error = await Assert.ThrowsAsync<ResultadoEscrituraInciertoException>(() => servicio.CrearAsync("Taladro", "Óptima", null, ""));
            Assert.Same(uow.ErrorCommit, error.InnerException); Assert.Contains("verificar persistencia antes de reintentar", error.Message);
            Assert.True(uow.Confirmado); Assert.Equal(1, repo.Altas); Assert.Equal(1, repo.Historiales); Assert.Equal(1, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task FalloPrevioACommitConRollbackFallidoConservaErrorOriginal()
        {
            var uow = new Uow(); var repo = new CatalogosRepo { Fallar = true };
            var servicio = new GestionarCatalogos(Sesion(), uow, repo, new Fletes());
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearCategoriaAsync("A", false));
            Assert.Same(repo.Error, error); Assert.Equal(0, uow.Commits); Assert.Equal(1, uow.Rollbacks);
        }
        [Fact] public async Task CancelacionAntesDeCommitNoSeClasificaComoIncierta()
        {
            var uow = new Uow(); using (var cts = new CancellationTokenSource())
            {
                var repo = new HerramientasRepo { AlRegistrar = () => cts.Cancel() };
                var servicio = new GestionarHerramientas(Sesion(), uow, repo);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.CrearAsync("Taladro", "Óptima", null, "", cts.Token));
                Assert.Equal(0, uow.Commits); Assert.Equal(1, uow.Rollbacks);
            }
        }
        private static Session Sesion() { var s = new Session(); s.Start(new Identity("a", Role.Galpon)); return s; }
        private sealed class Uow : IUnitOfWork
        {
            public readonly Exception ErrorCommit = new Exception("respuesta perdida"); public bool Confirmado; public int Commits, Rollbacks;
            public Task BeginAsync(CancellationToken token=default) => Task.CompletedTask;
            public Task CommitAsync(CancellationToken token=default) { Commits++; Confirmado=true; throw ErrorCommit; }
            public Task RollbackAsync() { Rollbacks++; throw new InvalidOperationException("rollback fallido"); }
        }
        private sealed class HerramientasRepo : IHerramientasEscritura
        {
            public int Altas, Historiales; public Action AlRegistrar;
            public Task<int> CrearAsync(Herramienta h, CancellationToken token=default) { Altas++; return Task.FromResult(1); }
            public Task RegistrarHistorialAsync(int id, int? origen, int? destino, CancellationToken token=default) { Historiales++; AlRegistrar?.Invoke(); return Task.CompletedTask; }
            public Task<Herramienta> BloquearAsync(int id, CancellationToken token=default) => throw new NotSupportedException();
            public Task ActualizarAsync(Herramienta h, CancellationToken token=default) => throw new NotSupportedException();
            public Task EliminarAsync(int id, CancellationToken token=default) => throw new NotSupportedException();
        }
        private sealed class Fletes : IFletesEscritura
        {
            public Task<int> CrearAsync(string nombre, CancellationToken token=default) => throw new NotSupportedException();
            public Task DesactivarAsync(int id, CancellationToken token=default) => throw new NotSupportedException();
        }
        private sealed class CatalogosRepo : ICatalogosEscritura
        {
            public int Altas; public bool Fallar; public readonly Exception Error = new InvalidOperationException("fallo previo");
            public Task<int> CrearCategoriaAsync(Categoria x,CancellationToken t=default) { Altas++; if(Fallar) throw Error; return Task.FromResult(1); }
            public Task EditarCategoriaAsync(Categoria x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EliminarCategoriaAsync(int x,CancellationToken t=default) => throw new NotSupportedException();
            public Task<int> CrearObraAsync(Obra x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EditarObraAsync(Obra x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EliminarObraAsync(int x,CancellationToken t=default) => throw new NotSupportedException();
            public Task<int> CrearProveedorAsync(Proveedor x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EditarProveedorAsync(Proveedor x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EliminarProveedorAsync(int x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EditarFleteAsync(Flete x,CancellationToken t=default) => throw new NotSupportedException();
            public Task<int> CrearUbicacionAsync(Ubicacion x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EditarUbicacionAsync(Ubicacion x,CancellationToken t=default) => throw new NotSupportedException();
            public Task EliminarUbicacionAsync(int x,CancellationToken t=default) => throw new NotSupportedException();
        }
    }
}
