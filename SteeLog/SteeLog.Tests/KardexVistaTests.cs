using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Reportes;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public class KardexVistaTests
    {
        private const BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Executor : IUseCaseExecutor
        {
            public readonly Queue<TaskCompletionSource<List<KardexItem>>> Pendientes = new Queue<TaskCompletionSource<List<KardexItem>>>();
            public int Llamadas;
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
            {
                Llamadas++;
                return (TResult)(object)await Pendientes.Dequeue().Task;
            }
        }
        private sealed class DeleteExecutor : IUseCaseExecutor
        {
            public int Escrituras, Lecturas;
            public readonly TaskCompletionSource<int> Pendiente = new TaskCompletionSource<int>();
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int)) { Escrituras++; return (Task<TResult>)(object)Pendiente.Task; }
                Lecturas++;
                return Task.FromResult((TResult)(object)new List<KardexItem>());
            }
        }
        private sealed class Pdf : IKardexPdf { public void Generar(string ruta, KardexSnapshot snapshot) { } }
        private sealed class PdfPendiente : IKardexPdf
        {
            public int Llamadas; public string Ruta;
            public readonly TaskCompletionSource<bool> Inicio = new TaskCompletionSource<bool>();
            public readonly TaskCompletionSource<bool> Seguir = new TaskCompletionSource<bool>();
            public void Generar(string ruta, KardexSnapshot snapshot) { Interlocked.Increment(ref Llamadas); Ruta = ruta; System.IO.File.WriteAllText(ruta, "pdf"); Inicio.TrySetResult(true); Seguir.Task.GetAwaiter().GetResult(); }
        }
        private static void Esperar(Func<bool> condicion)
        {
            var reloj = Stopwatch.StartNew();
            while (!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }
        [Fact]
        public void VistaDescartaRespuestaAnteriorYCancelaAlDisponer()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var ejecutor = new Executor();
                    var a = new TaskCompletionSource<List<KardexItem>>(); var b = new TaskCompletionSource<List<KardexItem>>(); var c = new TaskCompletionSource<List<KardexItem>>();
                    ejecutor.Pendientes.Enqueue(a); ejecutor.Pendientes.Enqueue(b); ejecutor.Pendientes.Enqueue(c);
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Kardex", true);
                    var identidad = new object();
                    var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new KardexPresenter(ejecutor, new Pdf()), (Func<object>)(() => identidad), (Func<bool>)(() => false), (Action<string>)(_ => { }) }, null);
                    Assert.Equal(0, ejecutor.Llamadas); var handle = vista.Handle;
                    var cargar = tipo.GetMethod("CargarKardex", Privado);
                    cargar.Invoke(vista, new object[] { null }); cargar.Invoke(vista, new object[] { null });
                    b.SetResult(new List<KardexItem> { new KardexItem { id_movimiento = 2, nombre_material = "B" } });
                    var grid = (DataGridView)tipo.GetField("dgvKardex", Privado).GetValue(vista);
                    Esperar(() => grid.DataSource != null && grid.Rows.Count > 0);
                    a.SetResult(new List<KardexItem> { new KardexItem { id_movimiento = 1, nombre_material = "A" } });
                    Application.DoEvents(); Assert.Equal(2, ((KardexItem)grid.Rows[0].DataBoundItem).id_movimiento);
                    cargar.Invoke(vista, new object[] { null });
                    var vida = (CancellationTokenSource)tipo.GetField("vida", Privado).GetValue(vista);
                    vista.Dispose(); Assert.True(vida.IsCancellationRequested);
                    c.SetResult(new List<KardexItem>()); Application.DoEvents();
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void ExportacionNoSeDuplicaYNuncaAbreTrasDispose()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var ejecutor = new Executor(); var carga = new TaskCompletionSource<List<KardexItem>>(); ejecutor.Pendientes.Enqueue(carga);
                    var pdf = new PdfPendiente(); int aperturas = 0; var identidad = new object();
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Kardex", true);
                    var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new KardexPresenter(ejecutor, pdf), (Func<object>)(() => identidad), (Func<bool>)(() => false), (Action<string>)(_ => aperturas++) }, null);
                    var handle = vista.Handle; tipo.GetMethod("CargarKardex", Privado).Invoke(vista, new object[] { null });
                    carga.SetResult(new List<KardexItem> { new KardexItem { id_movimiento = 1, fecha = DateTime.Today, nombre_material = "A" } });
                    var grid = (DataGridView)tipo.GetField("dgvKardex", Privado).GetValue(vista);
                    Esperar(() => grid.DataSource != null && grid.Rows.Count > 0);
                    var click = tipo.GetMethod("BtnDescargarPDF_Click", Privado);
                    click.Invoke(vista, new object[] { null, EventArgs.Empty }); Esperar(() => pdf.Inicio.Task.IsCompleted);
                    click.Invoke(vista, new object[] { null, EventArgs.Empty }); Assert.Equal(1, pdf.Llamadas);
                    vista.Dispose(); pdf.Seguir.SetResult(true);
                    Esperar(() => !System.IO.File.Exists(pdf.Ruta)); Assert.Equal(0, aperturas);
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Theory]
        [InlineData("dispose", false)]
        [InlineData("dispose", true)]
        [InlineData("identidad", false)]
        [InlineData("filtro", false)]
        public void EliminacionNoSeDuplicaNiActualizaContextoObsoleto(string cambio, bool falla)
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var ejecutor = new DeleteExecutor(); object identidad = new object(); int exitos = 0, errores = 0, confirmaciones = 0;
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Kardex", true);
                    var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new KardexPresenter(ejecutor, new Pdf()), (Func<object>)(() => identidad), (Func<bool>)(() => false), (Action<string>)(_ => { }) }, null);
                    var handle = vista.Handle;
                    tipo.GetField("confirmarEliminacion", Privado).SetValue(vista, (Func<bool>)(() => { confirmaciones++; return true; }));
                    tipo.GetField("avisarEliminacionExitosa", Privado).SetValue(vista, (Action<string>)(_ => exitos++));
                    tipo.GetField("avisarEliminacionFallida", Privado).SetValue(vista, (Action<string>)(_ => errores++));
                    var eliminar = tipo.GetMethod("EliminarAsync", Privado);
                    var item = new KardexItem { id_movimiento = 42 };
                    var primera = (Task)eliminar.Invoke(vista, new object[] { item });
                    var segunda = (Task)eliminar.Invoke(vista, new object[] { item });
                    Assert.True(segunda.IsCompleted); Assert.Equal(1, ejecutor.Escrituras); Assert.Equal(1, confirmaciones);
                    if (cambio == "dispose") vista.Dispose();
                    else if (cambio == "identidad") identidad = new object();
                    else tipo.GetMethod("CargarKardex", Privado).Invoke(vista, new object[] { null });
                    int lecturas = ejecutor.Lecturas;
                    if (falla) ejecutor.Pendiente.SetException(new InvalidOperationException("falló")); else ejecutor.Pendiente.SetResult(1);
                    Esperar(() => primera.IsCompleted);
                    Assert.Equal(0, exitos); Assert.Equal(0, errores); Assert.Equal(lecturas, ejecutor.Lecturas);
                    vista.Dispose();
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
    }
}
