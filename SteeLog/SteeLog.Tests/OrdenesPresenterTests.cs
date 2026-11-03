using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using SteeLog.Vistas.Movimientos.Ordenes;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class OrdenesPresenterTests
    {
        private sealed class Vista : IOrdenesVista
        {
            public readonly List<bool> Estados = new List<bool>();
            public Exception Error;
            public void SetOcupada(bool ocupada) => Estados.Add(ocupada);
            public void MostrarError(Exception error) => Error = error;
        }
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly TaskCompletionSource<object> Pendiente = new TaskCompletionSource<object>();
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
            { Llamadas++; return (TResult)await Pendiente.Task; }
        }
        [Fact]
        public async Task CargasFueraDeOrdenSoloAplicanUltimaYSuprimenErroresObsoletos()
        {
            var vista = new Vista(); using (var cargas = new CargaVigente()) {
                var a = cargas.Iniciar(vista, () => false); var esperaA = new TaskCompletionSource<string>();
                var pendienteA = AplicarAlCompletar(a, esperaA.Task);
                var b = cargas.Iniciar(vista, () => false); var esperaB = new TaskCompletionSource<string>();
                var pendienteB = AplicarAlCompletar(b, esperaB.Task);
                a.MostrarError(new InvalidOperationException("obsoleto"));
                esperaB.SetResult("B"); Assert.Equal("B", await pendienteB);
                esperaA.SetResult("A"); Assert.Null(await pendienteA); Assert.Null(vista.Error);
            }
        }
        [Fact]
        public async Task NuevaOrdenYCierreInvalidanCargasPendientes()
        {
            var vista = new Vista(); var cargas = new CargaVigente();
            var espera = new TaskCompletionSource<string>(); var anterior = AplicarAlCompletar(cargas.Iniciar(vista, () => false), espera.Task);
            cargas.Invalidar(); espera.SetResult("A"); Assert.Null(await anterior);
            var esperaCierre = new TaskCompletionSource<string>(); var cierre = AplicarAlCompletar(cargas.Iniciar(vista, () => false), esperaCierre.Task);
            cargas.Dispose(); esperaCierre.SetResult("B"); Assert.Null(await cierre);
        }
        private static async Task<string> AplicarAlCompletar(SolicitudCarga carga, Task<string> operacion)
        {
            using (carga) { var valor = await operacion; return carga.EsActual ? valor : null; }
        }
        [Fact]
        public void VistasBloqueanGuardarYConfirmarMientrasSeCargaNuevoId()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    using (var alta = new Vista_OrdenesAlta()) using (var revision = new Vista_OrdenesRevision()) {
                        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        var altaType = alta.GetType(); var altaEstado = (EstadoModeloOrden)altaType.GetField("_modelo", flags).GetValue(alta);
                        altaType.GetField("_catalogosListos", flags).SetValue(alta, true);
                        var guardar = (Control)altaType.GetField("btnGuardar", flags).GetValue(alta);
                        alta.SetEditMode(1); Assert.True(altaEstado.Aplicar(1)); alta.SetOcupada(false); Assert.True(guardar.Enabled);
                        var a = new TaskCompletionSource<int>(); var b = new TaskCompletionSource<int>();
                        var cargas = (CargaVigente)altaType.GetField("_cargas", flags).GetValue(alta);
                        var cargaA = AplicarModeloAlCompletar(cargas.Iniciar(alta, () => alta.IsDisposed), a.Task, altaEstado);
                        alta.SetEditMode(2); Assert.False(altaEstado.Listo); Assert.False(guardar.Enabled);
                        var cargaB = AplicarModeloAlCompletar(cargas.Iniciar(alta, () => alta.IsDisposed), b.Task, altaEstado);
                        altaType.GetMethod("BtnGuardar_Click", flags).Invoke(alta, new object[] { guardar, EventArgs.Empty });
                        a.SetResult(1); Assert.False(cargaA.GetAwaiter().GetResult()); Assert.False(altaEstado.Listo); Assert.False(guardar.Enabled);
                        b.SetResult(2); Assert.True(cargaB.GetAwaiter().GetResult()); alta.SetOcupada(false); Assert.True(guardar.Enabled);
                        alta.PrepararNuevaOrden(); Assert.True(altaEstado.Listo); Assert.Equal(0, altaEstado.Id);
                        var revType = revision.GetType(); var revEstado = (EstadoModeloOrden)revType.GetField("_modelo", flags).GetValue(revision);
                        var confirmar = (Control)revType.GetField("btnConfirmar", flags).GetValue(revision);
                        revision.SetOrder(1); Assert.True(revEstado.Aplicar(1)); revType.GetField("_permiteRecepcion", flags).SetValue(revision, true);
                        revision.SetOcupada(false); Assert.True(confirmar.Enabled);
                        revision.SetOrder(2); Assert.False(revEstado.Listo); Assert.False(confirmar.Enabled);
                        revType.GetMethod("BtnConfirmar_Click", flags).Invoke(revision, new object[] { confirmar, EventArgs.Empty });
                        Assert.False(revEstado.Aplicar(1)); revision.SetOcupada(false); Assert.False(confirmar.Enabled);
                    }
                } catch (Exception e) { error = e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private static async Task<bool> AplicarModeloAlCompletar(SolicitudCarga carga, Task<int> operacion, EstadoModeloOrden estado)
        {
            using (carga) { var id = await operacion; return carga.EsActual && estado.Aplicar(id); }
        }
        [Fact]
        public void ConstructoresYPreparacionNoRequierenBase()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    using (var alta = new Vista_OrdenesAlta())
                    using (var listado = new Vista_OrdenesListado())
                    using (var revision = new Vista_OrdenesRevision())
                    using (var modulo = new Vista_OrdenesModulo()) {
                        alta.PrepararNuevaOrden(); alta.SetEditMode(12); revision.SetOrder(12); listado.Refrescar(); modulo.Refrescar();
                    }
                } catch (Exception e) { error = e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15)));
            Assert.Null(error);
        }
        [Fact]
        public async Task DeshabilitaMientrasCargaYRechazaDobleEnvio()
        {
            var vista = new Vista(); var executor = new Ejecutor(); var presenter = new OrdenesPresenter(executor, vista);
            var primero = presenter.EliminarAsync(5);
            var segundo = await presenter.EliminarAsync(5);
            Assert.False(segundo.Exitoso); Assert.Equal(1, executor.Llamadas); Assert.Equal(new[] { true }, vista.Estados);
            executor.Pendiente.SetResult(true);
            Assert.True((await primero).Exitoso); Assert.Equal(new[] { true, false }, vista.Estados);
        }
        [Fact]
        public async Task ErrorSeMuestraYDesbloqueaLaVista()
        {
            var vista = new Vista(); var executor = new Ejecutor(); var presenter = new OrdenesPresenter(executor, vista);
            var resultado = presenter.EliminarAsync(5); var error = new InvalidOperationException("fallo");
            executor.Pendiente.SetException(error);
            Assert.False((await resultado).Exitoso); Assert.Same(error, vista.Error); Assert.Equal(new[] { true, false }, vista.Estados);
        }
    }
}
