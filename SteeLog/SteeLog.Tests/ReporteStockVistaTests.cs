using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Reportes;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class ReporteStockVistaTests
    {
        private const BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Executor : IUseCaseExecutor
        {
            public readonly Queue<TaskCompletionSource<object>> Pendientes = new Queue<TaskCompletionSource<object>>();
            public int Llamadas;
            public TaskCompletionSource<object> Agregar() { var t = new TaskCompletionSource<object>(); Pendientes.Enqueue(t); return t; }
            public async Task<TResult> ExecuteAsync<T, TResult>(Func<T, Task<TResult>> operacion) where T : class { Llamadas++; return (TResult)await Pendientes.Dequeue().Task; }
        }
        private sealed class Pdf : IReporteStockPdf
        {
            public int Llamadas; public string Ruta; public ReporteStockSnapshot Snapshot;
            public readonly TaskCompletionSource<bool> Inicio = new TaskCompletionSource<bool>();
            public readonly TaskCompletionSource<bool> Seguir = new TaskCompletionSource<bool>();
            public void Generar(string ruta, ReporteStockSnapshot snapshot) { Interlocked.Increment(ref Llamadas); Ruta = ruta; Snapshot = snapshot; File.WriteAllText(ruta, "pdf"); Inicio.TrySetResult(true); Seguir.Task.GetAwaiter().GetResult(); }
        }
        private static void Esperar(Func<bool> condicion)
        {
            var reloj = Stopwatch.StartNew();
            while (!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }
        private static void EnSta(Action prueba)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); prueba(); } catch (Exception ex) { error = ex; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(20))); Assert.Null(error);
        }
        private static Type Tipo(string nombre) => Assembly.Load("SteeLog").GetType("SteeLog.Vistas." + nombre, true);
        private static object Campo(object o, string nombre) => o.GetType().GetField(nombre, Privado).GetValue(o);
        private static void Llamar(object o, string nombre, params object[] args) => o.GetType().GetMethod(nombre, Privado).Invoke(o, args);
        private static IReadOnlyList<ReporteStockItem> Filas(string nombre) => new[] { new ReporteStockItem { Material = new MaterialInventarioLectura { Id = 1, Nombre = nombre, Categoria = "Acero", Stock = 73, Minimo = 2, Precio = 98765 } } };
        private static Form Vista(Executor executor, Pdf pdf, Func<object> identidad, Func<bool> admin, Func<string> ruta, Action<string> abrir, Action<string> error)
        {
            return (Form)Activator.CreateInstance(Tipo("Vista_ReporteStock"), Privado, null, new object[] { new ReporteStockPresenter(executor, pdf), identidad, admin, ruta, abrir, error, (Action<string>)(_ => { }) }, null);
        }
        private static void Cargar(Form vista) => Llamar(vista, "CargarMateriales", null, false, null);
        private static void Exportar(Form vista) => Llamar(vista, "btnExportarPDF_Click", null, EventArgs.Empty);
        [Fact]
        public void CargaPendienteFallidaObsoletaYDisposeNoExportan()
        {
            EnSta(() => {
                var executor = new Executor(); var a = executor.Agregar(); var b = executor.Agregar(); var c = executor.Agregar(); var d = executor.Agregar();
                var pdf = new Pdf(); object identidad = new object(); int dialogos = 0; int errores = 0;
                using (var vista = Vista(executor, pdf, () => identidad, () => true, () => { dialogos++; return "no.pdf"; }, _ => { }, _ => errores++)) {
                    var handle = vista.Handle; Cargar(vista); Exportar(vista); Assert.Equal(0, dialogos);
                    Cargar(vista); b.SetResult(Filas("B")); var grid = (DataGridView)Campo(vista, "dgvMateriales"); Esperar(() => grid.DataSource != null && grid.Rows.Count > 0);
                    a.SetResult(Filas("A")); Application.DoEvents(); Assert.Equal("B", grid.Rows[0].Cells["nombre"].Value);
                    var identidadOriginal = identidad; identidad = new object(); Exportar(vista); Assert.Equal(0, dialogos); identidad = identidadOriginal;
                    Cargar(vista); Exportar(vista); Assert.Equal(0, dialogos); c.SetException(new Exception("fallo")); Esperar(() => errores == 1); Exportar(vista); Assert.Equal(0, dialogos);
                    Cargar(vista); var vida = (CancellationTokenSource)Campo(vista, "vida"); vista.Dispose(); Assert.True(vida.IsCancellationRequested); d.SetResult(Filas("D")); Application.DoEvents(); Assert.Equal(0, pdf.Llamadas);
                }
            });
        }
        [Theory] [InlineData(false)] [InlineData(true)]
        public void RenderPendienteNoAbreNiEscribeTrasDisposeOCambioIdentidad(bool cambioIdentidad)
        {
            EnSta(() => {
                var executor = new Executor(); var carga = executor.Agregar(); var pdf = new Pdf(); object identidad = new object(); int aperturas = 0;
                string destino = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
                using (var vista = Vista(executor, pdf, () => identidad, () => false, () => destino, _ => aperturas++, _ => { })) {
                    var handle = vista.Handle; Cargar(vista); carga.SetResult(Filas("Perfil")); var grid = (DataGridView)Campo(vista, "dgvMateriales"); Esperar(() => grid.DataSource != null && grid.Rows.Count > 0);
                    Exportar(vista); Esperar(() => pdf.Inicio.Task.IsCompleted); Exportar(vista); Assert.Equal(1, pdf.Llamadas);
                    Assert.DoesNotContain(pdf.Snapshot.Columnas, c => c.Nombre == "precioUnitario");
                    Assert.DoesNotContain(pdf.Snapshot.Filas, f => f.Valores.Contains("73"));
                    if (cambioIdentidad) identidad = new object(); else vista.Dispose();
                    pdf.Seguir.SetResult(true); Esperar(() => !File.Exists(pdf.Ruta)); Assert.False(File.Exists(destino)); Assert.Equal(0, aperturas);
                }
            });
        }
        [Fact]
        public void ModalCargaFallidaBloqueaGuardadoYDobleEnvio()
        {
            EnSta(() => {
                var executor = new Executor(); var primera = executor.Agregar(); var materiales = executor.Agregar(); var secciones = executor.Agregar(); var guardar = executor.Agregar();
                var presenter = new ReporteStockPresenter(executor, new Pdf()); object identidad = new object(); int errores = 0;
                var tipo = Tipo("Reportes.ModalConfigReporteStock");
                using (var modal = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { presenter, (Func<object>)(() => identidad), (Action<string>)(_ => errores++), (Action<string>)(_ => { }) }, null)) {
                    var handle = modal.Handle; Llamar(modal, "CargarDatos", (object)null); Llamar(modal, "GuardarTodo"); Assert.Equal(1, executor.Llamadas);
                    primera.SetException(new Exception("fallo")); Esperar(() => errores == 1); Llamar(modal, "GuardarTodo"); Assert.Equal(1, executor.Llamadas);
                    Llamar(modal, "CargarDatos", (object)null); materiales.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]); Esperar(() => executor.Llamadas == 3);
                    secciones.SetResult((IReadOnlyList<ReporteStockSeccion>)new ReporteStockSeccion[0]); Esperar(() => (bool)Campo(modal, "cargado"));
                    var identidadOriginal = identidad; identidad = new object(); Llamar(modal, "GuardarTodo"); Assert.Equal(3, executor.Llamadas); identidad = identidadOriginal;
                    Llamar(modal, "GuardarTodo"); Llamar(modal, "GuardarTodo"); Assert.Equal(4, executor.Llamadas); Assert.False(modal.Enabled);
                    modal.Dispose(); guardar.SetResult(0); Application.DoEvents(); Assert.Equal(4, executor.Llamadas);
                }
            });
        }
    }
}
