using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Domain.Catalogos;
using Xunit;
namespace SteeLog.Tests
{
    public sealed class CatalogosCoreTests
    {
        [Fact] public void NombresRespetanLimitesDelEsquema()
        {
            Assert.Equal("A", new Categoria(0, " A ", true).Nombre);
            Assert.Throws<ArgumentException>(() => new Categoria(0, new string('a', 51), false));
            Assert.Throws<ArgumentException>(() => new Proveedor(0, " "));
            Assert.Throws<ArgumentException>(() => new Flete(0, new string('a', 51), true));
            Assert.Throws<ArgumentException>(() => new Obra(0, new string('a', 101), true));
            Assert.Throws<ArgumentException>(() => new Ubicacion(0, new string('a', 101)));
            Assert.Equal(100, new Ubicacion(0, new string('a', 100)).Nombre.Length);
        }
        [Fact] public void SinSesionSeRechazaAntesDeTransaccion()
        {
            var uow = new Uow(); var servicio = new GestionarCatalogos(new Session(), uow, new Repo(), new Fletes(uow));
            Assert.Throws<UnauthorizedAccessException>(() => { _ = servicio.CrearCategoriaAsync("A", true); });
            Assert.Equal(0, uow.Begin);
        }
        [Fact] public async Task AltaInactivaReutilizaFletesEnUnaTransaccion()
        {
            var uow = new Uow(); var fletes = new Fletes(uow); var servicio = new GestionarCatalogos(Sesion(), uow, new Repo(), fletes);
            Assert.Equal(42, await servicio.CrearFleteAsync(" X ", false));
            Assert.Equal("X", fletes.Nombre); Assert.Equal(42, fletes.Desactivado); Assert.Equal(1, uow.Begin); Assert.Equal(1, uow.Commit); Assert.Equal(0, uow.Rollback);
        }
        [Fact] public async Task ErrorEnSegundaEscrituraRevierteAlta()
        {
            var uow = new Uow(); var fletes = new Fletes(uow) { Fallar = true }; var servicio = new GestionarCatalogos(Sesion(), uow, new Repo(), fletes);
            await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearFleteAsync("X", false));
            Assert.Equal(1, uow.Rollback); Assert.Equal(0, uow.Commit);
        }
        [Fact] public void ValoresInvalidosNoAbrenTransaccion()
        {
            var uow = new Uow(); var servicio = new GestionarCatalogos(Sesion(), uow, new Repo(), new Fletes(uow));
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = servicio.EliminarObraAsync(0); });
            Assert.Throws<ArgumentException>(() => { _ = servicio.CrearUbicacionAsync(" "); });
            Assert.Equal(0, uow.Begin);
        }
        [Fact] public async Task CancelacionPreviaNoAbreTransaccion()
        {
            var uow = new Uow(); var servicio = new GestionarCatalogos(Sesion(), uow, new Repo(), new Fletes(uow));
            using (var fuente = new CancellationTokenSource())
            {
                fuente.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.CrearCategoriaAsync("A", false, fuente.Token));
            }
            Assert.Equal(0, uow.Begin);
        }
        private static Session Sesion() { var s = new Session(); s.Start(new Identity("a", Role.Galpon)); return s; }
        private sealed class Uow : IUnitOfWork
        {
            public int Begin, Commit, Rollback; public bool Activa;
            public Task BeginAsync(CancellationToken t=default) { Begin++; Activa=true; return Task.CompletedTask; }
            public Task CommitAsync(CancellationToken t=default) { Commit++; Activa=false; return Task.CompletedTask; }
            public Task RollbackAsync() { Rollback++; Activa=false; return Task.CompletedTask; }
        }
        private sealed class Fletes : IFletesEscritura
        {
            private readonly Uow uow; public string Nombre; public int Desactivado; public bool Fallar;
            public Fletes(Uow uow) { this.uow=uow; }
            public Task<int> CrearAsync(string nombre, CancellationToken t=default) { Assert.True(uow.Activa); Nombre=nombre; return Task.FromResult(42); }
            public Task DesactivarAsync(int id, CancellationToken t=default) { Assert.True(uow.Activa); Desactivado=id; if(Fallar) throw new InvalidOperationException(); return Task.CompletedTask; }
        }
        private sealed class Repo : ICatalogosEscritura
        {
            public Task<int> CrearCategoriaAsync(Categoria x,CancellationToken t=default) => Task.FromResult(1);
            public Task EditarCategoriaAsync(Categoria x,CancellationToken t=default) => Task.CompletedTask;
            public Task EliminarCategoriaAsync(int x,CancellationToken t=default) => Task.CompletedTask;
            public Task<int> CrearObraAsync(Obra x,CancellationToken t=default) => Task.FromResult(1);
            public Task EditarObraAsync(Obra x,CancellationToken t=default) => Task.CompletedTask;
            public Task EliminarObraAsync(int x,CancellationToken t=default) => Task.CompletedTask;
            public Task<int> CrearProveedorAsync(Proveedor x,CancellationToken t=default) => Task.FromResult(1);
            public Task EditarProveedorAsync(Proveedor x,CancellationToken t=default) => Task.CompletedTask;
            public Task EliminarProveedorAsync(int x,CancellationToken t=default) => Task.CompletedTask;
            public Task EditarFleteAsync(Flete x,CancellationToken t=default) => Task.CompletedTask;
            public Task<int> CrearUbicacionAsync(Ubicacion x,CancellationToken t=default) => Task.FromResult(1);
            public Task EditarUbicacionAsync(Ubicacion x,CancellationToken t=default) => Task.CompletedTask;
            public Task EliminarUbicacionAsync(int x,CancellationToken t=default) => Task.CompletedTask;
        }
    }
}
