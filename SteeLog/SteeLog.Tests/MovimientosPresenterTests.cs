using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public class MovimientosPresenterTests
    {
        private sealed class Vista : IMovimientosVista
        {
            public readonly List<bool> Estados = new List<bool>();
            public Exception Error;
            public void SetOcupada(bool valor) => Estados.Add(valor);
            public void MostrarError(Exception error) => Error = error;
        }
        private sealed class Executor : IUseCaseExecutor
        {
            public int Llamadas;
            public TaskCompletionSource<int> Pendiente = new TaskCompletionSource<int>();
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            { Llamadas++; return (TResult)(object)await Pendiente.Task; }
        }
        private sealed class VistaIncierta : IMovimientosVista, IMovimientosResultadoIncierto
        {
            public int Inciertos; public Exception Error;
            public void SetOcupada(bool valor) { }
            public void MostrarError(Exception error) => Error = error;
            public void MarcarResultadoIncierto() => Inciertos++;
        }
        [Fact]
        public async Task ResultadoInciertoConInterfazMarcaLaVistaSinMostrarError()
        {
            var vista = new VistaIncierta(); var executor = new Executor(); var presenter = new MovimientosPresenter(executor, vista);
            var pendiente = presenter.GuardarAsync(null, new SteeLog.Core.Application.Movimientos.MovimientoSolicitud());
            executor.Pendiente.SetException(new SteeLog.Core.Application.Common.ResultadoEscrituraInciertoException(new InvalidOperationException("commit")));
            Assert.Null(await pendiente);
            Assert.Equal(1, vista.Inciertos); Assert.Null(vista.Error);
        }
        [Fact]
        public async Task ResultadoInciertoSinInterfazMuestraError()
        {
            var vista = new Vista(); var executor = new Executor(); var presenter = new MovimientosPresenter(executor, vista);
            var pendiente = presenter.EliminarAsync(4);
            var error = new SteeLog.Core.Application.Common.ResultadoEscrituraInciertoException(new InvalidOperationException("commit"));
            executor.Pendiente.SetException(error);
            Assert.Null(await pendiente); Assert.Same(error, vista.Error);
            Assert.Equal(new[] { true, false }, vista.Estados);
        }
        [Fact]
        public async Task NoEnviaDosVecesYRecuperaEstado()
        {
            var vista = new Vista(); var executor = new Executor(); var presenter = new MovimientosPresenter(executor, vista);
            var primero = presenter.EliminarAsync(4);
            Assert.Null(await presenter.EliminarAsync(4));
            Assert.Equal(1, executor.Llamadas);
            executor.Pendiente.SetResult(4);
            Assert.Equal(4, await primero);
            Assert.Equal(new[] { true, false }, vista.Estados);
        }
        [Fact]
        public async Task ErrorSeComunicaYDesbloquea()
        {
            var vista = new Vista(); var executor = new Executor(); var presenter = new MovimientosPresenter(executor, vista);
            var pendiente = presenter.EliminarAsync(4);
            var error = new InvalidOperationException("falló"); executor.Pendiente.SetException(error);
            Assert.Null(await pendiente); Assert.Same(error, vista.Error);
            Assert.Equal(new[] { true, false }, vista.Estados);
        }
    }
}
