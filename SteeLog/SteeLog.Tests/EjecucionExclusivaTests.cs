using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public class EjecucionExclusivaTests
    {
        [Fact]
        public async Task SegundaLlamadaConcurrenteSeRechazaSinTocarLaVista()
        {
            var estados = new List<bool>();
            var ejecucion = new EjecucionExclusiva(estados.Add, error => Assert.Fail(error.Message));
            var pendiente = new TaskCompletionSource<int?>();
            var primera = ejecucion.Ejecutar(() => pendiente.Task);
            int llamadas = 0;
            try
            {
                Assert.Null(await Task.Run(() => ejecucion.Ejecutar<decimal?>(() => {
                    llamadas++;
                    return Task.FromResult<decimal?>(5m);
                })));
                Assert.Equal(0, llamadas);
                Assert.Equal(new[] { true }, estados);
            }
            finally { pendiente.SetResult(7); }
            Assert.Equal(7, await primera);
            Assert.Equal(new[] { true, false }, estados);
            Assert.Equal(8, await ejecucion.Ejecutar(() => Task.FromResult<int?>(8)));
        }

        [Theory]
        [InlineData("cancelacion")]
        [InlineData("incierto-con-handler")]
        [InlineData("incierto-sin-handler")]
        [InlineData("error")]
        public async Task FallosNotificanYSiempreLiberanEstadoYFlag(string caso)
        {
            var eventos = new List<string>();
            Exception mostrado = null;
            ResultadoEscrituraInciertoException notificado = null;
            Exception fallo = caso == "cancelacion" ? new OperationCanceledException()
                : caso == "error" ? new InvalidOperationException("operacion")
                : (Exception)new ResultadoEscrituraInciertoException(new Exception("commit"));
            var ejecucion = new EjecucionExclusiva(
                valor => eventos.Add(valor ? "ocupada" : "libre"),
                error => { mostrado = error; eventos.Add("error"); });
            Action<ResultadoEscrituraInciertoException> handler = caso == "incierto-con-handler"
                ? e => { notificado = e; eventos.Add("incierto"); }
                : (Action<ResultadoEscrituraInciertoException>)null;

            Assert.Null(await ejecucion.Ejecutar(() => Task.FromException<int?>(fallo), handler));

            if (caso == "cancelacion")
            {
                Assert.Null(mostrado);
                Assert.Null(notificado);
                Assert.Equal(new[] { "ocupada", "libre" }, eventos);
            }
            else if (caso == "incierto-con-handler")
            {
                Assert.Same(fallo, notificado);
                Assert.Null(mostrado);
                Assert.Equal(new[] { "ocupada", "incierto", "libre" }, eventos);
            }
            else
            {
                Assert.Same(fallo, mostrado);
                Assert.Null(notificado);
                Assert.Equal(new[] { "ocupada", "error", "libre" }, eventos);
            }
            Assert.Equal(9, await ejecucion.Ejecutar(() => Task.FromResult<int?>(9)));
            Assert.Equal("libre", eventos[eventos.Count - 1]);
        }

        [Fact]
        public async Task OperacionQueLanzaSinDevolverTaskTambienLibera()
        {
            var estados = new List<bool>();
            var fallo = new InvalidOperationException("sin task");
            Exception mostrado = null;
            var ejecucion = new EjecucionExclusiva(estados.Add, e => mostrado = e);
            Assert.Null(await ejecucion.Ejecutar<string>(() => throw fallo));
            Assert.Same(fallo, mostrado);
            Assert.Equal(new[] { true, false }, estados);
            Assert.Equal("ok", await ejecucion.Ejecutar(() => Task.FromResult("ok")));
        }

        [Fact]
        public async Task ErrorAlMarcarOcupadaNotificaYLiberaSinEjecutarOperacion()
        {
            var estados = new List<bool>();
            var fallo = new InvalidOperationException("vista");
            Exception mostrado = null;
            bool lanzar = true;
            int llamadas = 0;
            var ejecucion = new EjecucionExclusiva(valor => {
                estados.Add(valor);
                if (valor && lanzar) throw fallo;
            }, e => mostrado = e);
            Func<Task<int?>> operacion = () => { llamadas++; return Task.FromResult<int?>(3); };
            Assert.Null(await ejecucion.Ejecutar(operacion));
            Assert.Same(fallo, mostrado);
            Assert.Equal(0, llamadas);
            Assert.Equal(new[] { true, false }, estados);
            lanzar = false;
            Assert.Equal(3, await ejecucion.Ejecutar(operacion));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallbackQueLanzaSePropagaPeroLiberaEstadoYFlag(bool incierto)
        {
            var estados = new List<bool>();
            var falloCallback = new InvalidOperationException("callback");
            Exception fallo = incierto
                ? new ResultadoEscrituraInciertoException(new Exception("commit"))
                : (Exception)new Exception("operacion");
            var ejecucion = new EjecucionExclusiva(estados.Add, e => throw falloCallback);
            Action<ResultadoEscrituraInciertoException> handler = incierto
                ? e => throw falloCallback
                : (Action<ResultadoEscrituraInciertoException>)null;
            Assert.Same(falloCallback, await Assert.ThrowsAsync<InvalidOperationException>(
                () => ejecucion.Ejecutar(() => Task.FromException<int?>(fallo), handler)));
            Assert.Equal(new[] { true, false }, estados);
            Assert.Equal(2, await ejecucion.Ejecutar(() => Task.FromResult<int?>(2)));
        }

        [Fact]
        public async Task ErrorAlLiberarLaVistaSePropagaConFlagYaLiberado()
        {
            var fallo = new InvalidOperationException("liberar vista");
            var estados = new List<bool>();
            var ejecucion = new EjecucionExclusiva(valor => {
                estados.Add(valor);
                if (!valor) throw fallo;
            }, error => Assert.Fail(error.Message));
            int llamadas = 0;
            Func<Task<int?>> operacion = () => { llamadas++; return Task.FromResult<int?>(1); };
            Assert.Same(fallo, await Assert.ThrowsAsync<InvalidOperationException>(() => ejecucion.Ejecutar(operacion)));
            Assert.Same(fallo, await Assert.ThrowsAsync<InvalidOperationException>(() => ejecucion.Ejecutar(operacion)));
            Assert.Equal(2, llamadas);
            Assert.Equal(new[] { true, false, true, false }, estados);
        }

        [Fact]
        public async Task LiberaFlagAntesDeNotificarEstadoLibre()
        {
            EjecucionExclusiva ejecucion = null;
            Task<int?> reentrada = null;
            bool reentrar = true;
            var estados = new List<bool>();
            ejecucion = new EjecucionExclusiva(valor => {
                estados.Add(valor);
                if (!valor && reentrar)
                {
                    reentrar = false;
                    reentrada = ejecucion.Ejecutar(() => Task.FromResult<int?>(4));
                }
            }, error => Assert.Fail(error.Message));
            Assert.Equal(3, await ejecucion.Ejecutar(() => Task.FromResult<int?>(3)));
            Assert.NotNull(reentrada);
            Assert.Equal(4, await reentrada);
            Assert.Equal(new[] { true, false, true, false }, estados);
        }

        [Fact]
        public async Task SoportaRetornosNullableYDeReferencia()
        {
            var estados = new List<bool>();
            var ejecucion = new EjecucionExclusiva(estados.Add, error => Assert.Fail(error.Message));
            Assert.Equal(1, await ejecucion.Ejecutar(() => Task.FromResult<int?>(1)));
            Assert.Equal(2.5m, await ejecucion.Ejecutar(() => Task.FromResult<decimal?>(2.5m)));
            Assert.Equal("remito.pdf", await ejecucion.Ejecutar(() => Task.FromResult("remito.pdf")));
            Assert.Null(await ejecucion.Ejecutar<string>(() => Task.FromException<string>(new OperationCanceledException())));
            Assert.Equal(new[] { true, false, true, false, true, false, true, false }, estados);
        }

        private sealed class VistaMovimientos : IMovimientosVista, IMovimientosResultadoIncierto
        {
            public readonly List<bool> Estados = new List<bool>();
            public Exception Error;
            public int Inciertos;
            public void SetOcupada(bool valor) => Estados.Add(valor);
            public void MostrarError(Exception error) => Error = error;
            public void MarcarResultadoIncierto() => Inciertos++;
        }

        private sealed class ExecutorPendiente : IUseCaseExecutor
        {
            public TaskCompletionSource<object> Pendiente = new TaskCompletionSource<object>();
            public int Llamadas;
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                Llamadas++;
                return (TResult)await Pendiente.Task;
            }
        }

        [Theory]
        [InlineData("escritura")]
        [InlineData("precio")]
        [InlineData("remito")]
        public async Task MovimientosComparteExclusionEntreTodosLosTiposDeRetorno(string operacion)
        {
            var executor = new ExecutorPendiente();
            var vista = new VistaMovimientos();
            var presenter = new MovimientosPresenter(executor, vista);
            Task primera;
            if (operacion == "escritura") primera = presenter.EliminarAsync(1);
            else if (operacion == "precio") primera = presenter.ActualizarPrecioAsync(1, 3m);
            else primera = presenter.GenerarRemitoAsync(1);
            try
            {
                Assert.Null(await presenter.EliminarAsync(1));
                Assert.Null(await presenter.ActualizarPrecioAsync(1, 3m));
                Assert.Null(await presenter.GenerarRemitoAsync(1));
                Assert.Equal(1, executor.Llamadas);
                Assert.Equal(new[] { true }, vista.Estados);
            }
            finally { executor.Pendiente.SetCanceled(); }
            await primera;
            Assert.Null(vista.Error);
            Assert.Equal(new[] { true, false }, vista.Estados);
            executor.Pendiente = new TaskCompletionSource<object>();
            executor.Pendiente.SetResult(3m);
            Assert.Equal(3m, await presenter.ActualizarPrecioAsync(1, 3m));
            Assert.Equal(2, executor.Llamadas);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PrecioYRemitoConservanErrorGenericoAnteResultadoIncierto(bool remito)
        {
            var executor = new ExecutorPendiente();
            var vista = new VistaMovimientos();
            var presenter = new MovimientosPresenter(executor, vista);
            var error = new ResultadoEscrituraInciertoException(new Exception("commit"));
            executor.Pendiente.SetException(error);
            if (remito) Assert.Null(await presenter.GenerarRemitoAsync(1));
            else Assert.Null(await presenter.ActualizarPrecioAsync(1, 3m));
            Assert.Same(error, vista.Error);
            Assert.Equal(0, vista.Inciertos);
            Assert.Equal(new[] { true, false }, vista.Estados);
        }
    }
}
