using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Inventario;
using SteeLog.Presentation;
using Xunit;
namespace SteeLog.Tests
{
    public sealed class ConteoVistaTests
    {
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly Queue<TaskCompletionSource<IReadOnlyList<DetalleConteoLectura>>> Pendientes = new Queue<TaskCompletionSource<IReadOnlyList<DetalleConteoLectura>>>();
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            { Llamadas++; return (TResult)(object)await Pendientes.Dequeue().Task; }
        }
        private sealed class EjecutorMutacion : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly TaskCompletionSource<bool> Pendiente = new TaskCompletionSource<bool>();
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            { Llamadas++; return (TResult)(object)await Pendiente.Task; }
        }
        private sealed class EjecutorAbierto : IUseCaseExecutor
        {
            public int Consultas, Escrituras;
            public readonly Queue<TaskCompletionSource<ConteoLectura>> Pendientes = new Queue<TaskCompletionSource<ConteoLectura>>();
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TUseCase) == typeof(ConsultarConteos)) { Consultas++; return (TResult)(object)await Pendientes.Dequeue().Task; }
                Escrituras++; throw new InvalidOperationException("No debe crear durante carga.");
            }
        }
        private sealed class EjecutorPdf : IUseCaseExecutor
        {
            public Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(IReadOnlyList<DetalleConteoLectura>)) return Task.FromResult((TResult)(object)new List<DetalleConteoLectura>());
                return Task.FromResult((TResult)(object)new Dictionary<int, SeccionConteo>());
            }
        }
        private sealed class PdfBloqueado : IConteoPdf
        {
            public readonly ManualResetEventSlim Inicio = new ManualResetEventSlim();
            public readonly ManualResetEventSlim Continuar = new ManualResetEventSlim();
            public string SugerirNombreArchivo(ConteoLectura conteo) => "conteo-prueba.pdf";
            public void Generar(string ruta, ConteoLectura conteo, IReadOnlyList<DetalleConteoLectura> detalles, IReadOnlyDictionary<int, SeccionConteo> secciones, bool esAdmin)
            { Inicio.Set(); if (!Continuar.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException(); }
        }
        private static void Esperar(Task t)
        {
            var reloj = Stopwatch.StartNew();
            while (!t.IsCompleted && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(t.IsCompleted, "La carga no terminó.");
            Assert.False(t.IsFaulted, t.Exception?.ToString());
        }
        [Fact]
        public void VistaRealDescartaRespuestaAnteriorYCancelacionAlCerrarYDisponer()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Inventario.Vista_Conteo", true);
                    const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
                    var ejecutor = new Ejecutor();
                    using (var vista = (Form)Activator.CreateInstance(tipo, ejecutor, null)) {
                        Assert.Equal(0, ejecutor.Llamadas);
                        var handle = vista.Handle;
                        var grid = (DataGridView)tipo.GetField("dgvEntrada", f).GetValue(vista); var gridHandle = grid.Handle;
                        var actual = tipo.GetField("_conteoActual", f); var cargar = tipo.GetMethod("CargarDetallesEntrada", f);
                        var a = new TaskCompletionSource<IReadOnlyList<DetalleConteoLectura>>(); var b = new TaskCompletionSource<IReadOnlyList<DetalleConteoLectura>>();
                        ejecutor.Pendientes.Enqueue(a); ejecutor.Pendientes.Enqueue(b);
                        actual.SetValue(vista, new ConteoLectura { Id=1 }); var primera = (Task)cargar.Invoke(vista, null);
                        actual.SetValue(vista, new ConteoLectura { Id=2 }); var segunda = (Task)cargar.Invoke(vista, null);
                        b.SetResult(new[] { new DetalleConteoLectura { ConteoId=2, Nombre="Nuevo" } }); Esperar(segunda);
                        a.SetResult(new[] { new DetalleConteoLectura { ConteoId=1, Nombre="Viejo" } }); Esperar(primera);
                        Assert.Equal("Nuevo", ((List<DetalleConteoLectura>)grid.DataSource)[0].Nombre);
                        var tardia = new TaskCompletionSource<IReadOnlyList<DetalleConteoLectura>>(); ejecutor.Pendientes.Enqueue(tardia);
                        var tercera = (Task)cargar.Invoke(vista, null);
                        typeof(Form).GetMethod("OnFormClosed", f).Invoke(vista, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
                        tardia.SetResult(new[] { new DetalleConteoLectura { Nombre="Tardío" } }); Esperar(tercera);
                        Assert.Equal("Nuevo", ((List<DetalleConteoLectura>)grid.DataSource)[0].Nombre);
                    }
                    var otro = new Ejecutor(); var formulario = (Form)Activator.CreateInstance(tipo, otro, null);
                    var token = (CancellationTokenSource)tipo.GetField("cierre", f).GetValue(formulario); formulario.Dispose(); Assert.True(token.IsCancellationRequested);
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void VistaRealIgnoraDobleEnvioYNoAplicaResultadoSobreOtroConteo()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Inventario.Vista_Conteo", true);
                    const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
                    var ejecutor = new EjecutorMutacion();
                    using (var vista = (Form)Activator.CreateInstance(tipo, ejecutor, null)) {
                        var handle = vista.Handle;
                        var actual = tipo.GetField("_conteoActual", f); var material = tipo.GetField("_materialSeleccionado", f);
                        tipo.GetField("entradaLista", f).SetValue(vista, true); actual.SetValue(vista, new ConteoLectura { Id=1 }); material.SetValue(vista, new MaterialInventarioLectura { Id=9, Nombre="A" });
                        var cantidad = (Control)tipo.GetField("txtCantidad", f).GetValue(vista); cantidad.Text = "1";
                        var boton = tipo.GetMethod("BtnAgregar_Click", f);
                        boton.Invoke(vista, new object[] { null, EventArgs.Empty }); boton.Invoke(vista, new object[] { null, EventArgs.Empty });
                        Assert.Equal(1, ejecutor.Llamadas); Assert.True((bool)tipo.GetField("ocupada", f).GetValue(vista));
                        actual.SetValue(vista, new ConteoLectura { Id=2 }); material.SetValue(vista, new MaterialInventarioLectura { Id=10, Nombre="B" });
                        ejecutor.Pendiente.SetResult(true);
                        var reloj = Stopwatch.StartNew();
                        while ((bool)tipo.GetField("ocupada", f).GetValue(vista) && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.False((bool)tipo.GetField("ocupada", f).GetValue(vista));
                        Assert.Equal(2, ((ConteoLectura)actual.GetValue(vista)).Id);
                        Assert.Equal(10, ((MaterialInventarioLectura)material.GetValue(vista)).Id);
                        Assert.Equal("1", cantidad.Text); Assert.Equal(1, ejecutor.Llamadas);
                    }
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void ConsultaInicialBloqueaInicioYRespuestaViejaNoSobrescribeNueva()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Inventario.Vista_Conteo", true);
                    const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
                    var ejecutor = new EjecutorAbierto();
                    using (var vista = (Form)Activator.CreateInstance(tipo, ejecutor, null)) {
                        var handle = vista.Handle;
                        var antigua = new TaskCompletionSource<ConteoLectura>(); var nueva = new TaskCompletionSource<ConteoLectura>();
                        ejecutor.Pendientes.Enqueue(antigua); ejecutor.Pendientes.Enqueue(nueva);
                        var cargar = tipo.GetMethod("CargarConteoActual", f);
                        var a = (Task)cargar.Invoke(vista, null);
                        Assert.False(((Control)tipo.GetField("btnIniciarConteo", f).GetValue(vista)).Enabled);
                        tipo.GetMethod("BtnIniciarConteo_Click", f).Invoke(vista, new object[] { null, EventArgs.Empty });
                        Assert.Equal(0, ejecutor.Escrituras);
                        var b = (Task)cargar.Invoke(vista, null);
                        nueva.SetResult(null); Esperar(b);
                        Assert.True(((Control)tipo.GetField("btnIniciarConteo", f).GetValue(vista)).Enabled);
                        antigua.SetResult(new ConteoLectura { Id=91, Estado="abierto" }); Esperar(a);
                        Assert.Null(tipo.GetField("_conteoActual", f).GetValue(vista)); Assert.Equal(0, ejecutor.Escrituras);
                    }
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void CerrarMientrasRenderizaNoAbreVisorNiPublicaResultadoTardio()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Inventario.Vista_Conteo", true);
                    const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
                    var pdf = new PdfBloqueado(); int abiertos = 0; Action<string> abrir = _ => abiertos++;
                    using (var vista = (Form)Activator.CreateInstance(tipo, new EjecutorPdf(), pdf, abrir)) {
                        var handle = vista.Handle;
                        tipo.GetField("_tabActual", f).SetValue(vista, "historial");
                        tipo.GetField("_conteoSeleccionadoHistorial", f).SetValue(vista, new ConteoLectura { Id=3, Estado="finalizado" });
                        tipo.GetMethod("BtnImprimir_Click", f).Invoke(vista, new object[] { null, EventArgs.Empty });
                        var reloj = Stopwatch.StartNew();
                        while (!pdf.Inicio.IsSet && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.True(pdf.Inicio.IsSet);
                        typeof(Form).GetMethod("OnFormClosed", f).Invoke(vista, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
                        pdf.Continuar.Set(); reloj.Restart();
                        while ((bool)tipo.GetField("ocupada", f).GetValue(vista) && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.False((bool)tipo.GetField("ocupada", f).GetValue(vista)); Assert.Equal(0, abiertos);
                    }
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
    }
}
