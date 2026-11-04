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
        private static Form Modal(ReporteStockPresenter presenter, Func<object> identidad, Action<string> error, Action<string> exito)
        {
            return (Form)Activator.CreateInstance(Tipo("Reportes.ModalConfigReporteStock"), Privado, null, new object[] { presenter, identidad, error, exito }, null);
        }
        private static Form Modal(ReporteStockPresenter presenter, Func<object> identidad, Action<string> error) => Modal(presenter, identidad, error, (Action<string>)(_ => { }));
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
        [Fact]
        public void ModalCargaInicialPendienteYFallidaBloqueanEditorYValidaHabilita()
        {
            EnSta(() => {
                var executor = new Executor();
                var mat1 = executor.Agregar();
                var mat2 = executor.Agregar();
                var sec2 = executor.Agregar();
                var presenter = new ReporteStockPresenter(executor, new Pdf());
                object identidad = new object();
                int errores = 0;

                using (var modal = Modal(presenter, () => identidad, _ => errores++)) {
                    var handle = modal.Handle;
                    var btnNueva = (Button)Campo(modal, "btnNuevaSeccion");
                    var txtNombre = (TextBox)Campo(modal, "txtNombreSeccion");
                    var btnGuardar = (Button)Campo(modal, "btnGuardar");
                    var secciones = (System.Collections.IList)Campo(modal, "secciones");

                    Assert.False(btnNueva.Enabled);
                    Assert.False(txtNombre.Enabled);
                    Assert.False(btnGuardar.Enabled);

                    Llamar(modal, "CargarDatos", (object)null);
                    Assert.False(btnNueva.Enabled);
                    Assert.False(txtNombre.Enabled);
                    Assert.False(btnGuardar.Enabled);

                    Llamar(modal, "NuevaSeccion");
                    Assert.Empty(secciones);

                    mat1.SetException(new Exception("fallo inicial"));
                    Esperar(() => errores == 1);
                    Assert.False((bool)Campo(modal, "cargado"));
                    Assert.False(btnNueva.Enabled);
                    Assert.False(txtNombre.Enabled);
                    Assert.False(btnGuardar.Enabled);

                    Llamar(modal, "NuevaSeccion");
                    Assert.Empty(secciones);

                    Llamar(modal, "CargarDatos", (object)null);
                    mat2.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                    Esperar(() => executor.Llamadas == 3);
                    sec2.SetResult((IReadOnlyList<ReporteStockSeccion>)new ReporteStockSeccion[0]);
                    Esperar(() => (bool)Campo(modal, "cargado"));

                    Assert.True(btnNueva.Enabled);
                    Assert.True(txtNombre.Enabled);
                    Assert.True(btnGuardar.Enabled);

                    Llamar(modal, "NuevaSeccion");
                    Assert.Single(secciones);
                }
            });
        }
        [Theory] [InlineData(false)] [InlineData(true)]
        public void ModalGuardarConfirmadoMantieneBloqueoHastaRecargaValidaYSeparaSuFallo(bool fallaRecarga)
        {
            EnSta(() => {
                var executor = new Executor();
                var matInicial = executor.Agregar();
                var secInicial = executor.Agregar();
                var guardar = executor.Agregar();
                var matRecarga = executor.Agregar();
                var secRecarga = executor.Agregar();
                var presenter = new ReporteStockPresenter(executor, new Pdf());
                object identidad = new object();
                var listaErrores = new List<string>();
                var listaExitos = new List<string>();

                using (var modal = Modal(presenter, () => identidad, msg => listaErrores.Add(msg), msg => listaExitos.Add(msg))) {
                    var handle = modal.Handle;
                    var btnNueva = (Button)Campo(modal, "btnNuevaSeccion");
                    var btnGuardar = (Button)Campo(modal, "btnGuardar");
                    var txtNombre = (TextBox)Campo(modal, "txtNombreSeccion");
                    var secciones = (System.Collections.IList)Campo(modal, "secciones");

                    Llamar(modal, "CargarDatos", (object)null);
                    matInicial.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                    secInicial.SetResult((IReadOnlyList<ReporteStockSeccion>)new[] { new ReporteStockSeccion { Id = 10, Nombre = "Perfiles", Materiales = new List<int>() } });
                    Esperar(() => (bool)Campo(modal, "cargado"));
                    Assert.True(btnNueva.Enabled);
                    Assert.True(btnGuardar.Enabled);
                    Assert.Single(secciones);

                    Llamar(modal, "GuardarTodo");
                    Assert.False(modal.Enabled);
                    Assert.False(btnGuardar.Enabled);

                    guardar.SetResult(0);
                    Esperar(() => listaExitos.Count == 1);
                    Assert.Contains("guardada", listaExitos[0], StringComparison.OrdinalIgnoreCase);

                    Esperar(() => executor.Llamadas >= 4);
                    Assert.False((bool)Campo(modal, "cargado"));
                    Assert.False(btnNueva.Enabled);
                    Assert.False(btnGuardar.Enabled);
                    Assert.False(txtNombre.Enabled);

                    Llamar(modal, "NuevaSeccion");
                    Assert.Single(secciones);

                    if (fallaRecarga)
                    {
                        matRecarga.SetException(new Exception("error en recarga"));
                        Esperar(() => listaErrores.Count == 1);
                        Assert.Contains("cargar", listaErrores[0], StringComparison.OrdinalIgnoreCase);
                        Assert.DoesNotContain("guardar", listaErrores[0], StringComparison.OrdinalIgnoreCase);
                        Assert.Equal(1, listaExitos.Count);

                        Assert.False((bool)Campo(modal, "cargado"));
                        Assert.False(btnNueva.Enabled);
                        Assert.False(btnGuardar.Enabled);
                        Assert.False(txtNombre.Enabled);

                        Llamar(modal, "NuevaSeccion");
                        Assert.Single(secciones);
                    }
                    else
                    {
                        matRecarga.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                        secRecarga.SetResult((IReadOnlyList<ReporteStockSeccion>)new[] {
                            new ReporteStockSeccion { Id = 10, Nombre = "Perfiles confirmados" }
                        });
                        Esperar(() => (bool)Campo(modal, "cargado"));
                        Assert.True(modal.Enabled);
                        Assert.True(btnNueva.Enabled);
                        Assert.True(btnGuardar.Enabled);
                        Assert.True(txtNombre.Enabled);
                        Assert.Empty(listaErrores);
                        Assert.Single(listaExitos);
                        Assert.Contains("Perfiles confirmados", ((ListBox)Campo(modal, "lstSecciones")).Items[0].ToString());
                    }

                }
            });
        }
        [Fact]
        public void ModalErroresCargaYGuardadoIdentidadAnteriorNoAvisanNiRecarganContextoNuevo()
        {
            EnSta(() => {
                var executor = new Executor();
                var cargaMat1 = executor.Agregar();
                var matValido = executor.Agregar();
                var secValido = executor.Agregar();
                var guardar = executor.Agregar();
                var presenter = new ReporteStockPresenter(executor, new Pdf());
                object identidad = new object();
                int errores = 0;
                int exitos = 0;

                using (var modal = Modal(presenter, () => identidad, _ => errores++, _ => exitos++)) {
                    var handle = modal.Handle;

                    Llamar(modal, "CargarDatos", (object)null);
                    identidad = new object();
                    cargaMat1.SetException(new Exception("error carga previa"));
                    Application.DoEvents();
                    Thread.Sleep(20);
                    Application.DoEvents();

                    Assert.Equal(0, errores);
                    Assert.False((bool)Campo(modal, "cargado"));
                    Assert.Null(Campo(modal, "identidadDatos"));

                    Llamar(modal, "CargarDatos", (object)null);
                    matValido.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                    Esperar(() => executor.Llamadas == 3);
                    secValido.SetResult((IReadOnlyList<ReporteStockSeccion>)new[] { new ReporteStockSeccion { Id = 1, Nombre = "Barras", Materiales = new List<int>() } });
                    Esperar(() => (bool)Campo(modal, "cargado"));

                    Llamar(modal, "GuardarTodo");
                    identidad = new object();
                    guardar.SetException(new Exception("error guardar previo"));
                    Esperar(() => !(bool)Campo(modal, "guardando"));

                    Assert.Equal(0, errores);
                    Assert.Equal(0, exitos);
                    Assert.Equal(4, executor.Llamadas);
                }
            });
        }
        [Fact]
        public void VistaErroresCargaYRenderIdentidadAnteriorNoAvisanNiRecarganContextoNuevo()
        {
            EnSta(() => {
                var executor = new Executor();
                var carga1 = executor.Agregar();
                var cargaValida = executor.Agregar();
                var pdf = new Pdf();
                object identidad = new object();
                int errores = 0;
                int aperturas = 0;
                string destino = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");

                try {
                    using (var vista = Vista(executor, pdf, () => identidad, () => true, () => destino, _ => aperturas++, _ => errores++)) {
                        var handle = vista.Handle;

                        Cargar(vista);
                        identidad = new object();
                        carga1.SetException(new Exception("falla de carga vista"));
                        Application.DoEvents();
                        Thread.Sleep(20);
                        Application.DoEvents();

                        Assert.Equal(0, errores);
                        Assert.Null(Campo(vista, "identidadDatos"));
                        var grid = (DataGridView)Campo(vista, "dgvMateriales");
                        Assert.Null(grid.DataSource);

                        Cargar(vista);
                        cargaValida.SetResult(Filas("Perfil W"));
                        Esperar(() => grid.DataSource != null && grid.Rows.Count > 0);
                        Assert.NotNull(Campo(vista, "identidadDatos"));

                        Exportar(vista);
                        Esperar(() => pdf.Inicio.Task.IsCompleted);

                        identidad = new object();
                        pdf.Seguir.SetException(new Exception("fallo de render"));
                        Esperar(() => !(bool)Campo(vista, "exportando"));

                        Assert.Equal(0, errores);
                        Assert.Equal(0, aperturas);
                        Assert.False(File.Exists(destino));
                    }
                }
                finally {
                    try { if (File.Exists(destino)) File.Delete(destino); } catch { }
                }
            });
        }
        [Fact]
        public void ModalCargasFueraDeOrdenDescartaCargaObsoleta()
        {
            EnSta(() => {
                var executor = new Executor();
                var matA = executor.Agregar();
                var matB = executor.Agregar();
                var presenter = new ReporteStockPresenter(executor, new Pdf());
                object identidad = new object();

                using (var modal = Modal(presenter, () => identidad, _ => {})) {
                    var handle = modal.Handle;

                    Llamar(modal, "CargarDatos", (object)null);
                    Assert.Equal(1, executor.Llamadas);

                    Llamar(modal, "CargarDatos", (object)null);
                    Assert.Equal(2, executor.Llamadas);

                    var secB = executor.Agregar();
                    matB.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                    secB.SetResult((IReadOnlyList<ReporteStockSeccion>)new[] { new ReporteStockSeccion { Id = 20, Nombre = "SeccionB", Materiales = new List<int>() } });
                    Esperar(() => (bool)Campo(modal, "cargado"));

                    var lstSecciones = (ListBox)Campo(modal, "lstSecciones");
                    var secciones = (System.Collections.IList)Campo(modal, "secciones");
                    Assert.Single(lstSecciones.Items);
                    Assert.Contains("SeccionB", lstSecciones.Items[0].ToString());

                    matA.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                    Application.DoEvents();
                    Thread.Sleep(30);
                    Application.DoEvents();

                    Assert.Single(lstSecciones.Items);
                    Assert.Contains("SeccionB", lstSecciones.Items[0].ToString());
                    Assert.DoesNotContain("SeccionA", lstSecciones.Items[0].ToString());
                    Assert.Single(secciones);
                }
            });
        }
        [Fact]
        public void ModalYVistaDisposeConPendientesNoLanzanExcepcionNiHabilitan()
        {
            EnSta(() => {
                var executor = new Executor();
                var mat1 = executor.Agregar();
                var presenter = new ReporteStockPresenter(executor, new Pdf());
                int erroresModal = 0;
                object identidad = new object();

                using (var modal = Modal(presenter, () => identidad, _ => erroresModal++)) {
                    var handle = modal.Handle;
                    Llamar(modal, "CargarDatos", (object)null);
                    Assert.False(modal.IsDisposed);
                    modal.Dispose();
                    bool habilitadoTrasDispose = modal.Enabled;
                    Assert.True(modal.IsDisposed);
                    mat1.SetException(new Exception("error carga cancelada"));
                    Application.DoEvents();
                    Assert.Equal(0, erroresModal);
                    Assert.Equal(habilitadoTrasDispose, modal.Enabled);
                }

                var mat2 = executor.Agregar();
                var sec2 = executor.Agregar();
                var guardar = executor.Agregar();
                using (var modal2 = Modal(presenter, () => identidad, _ => erroresModal++)) {
                    var handle = modal2.Handle;
                    Llamar(modal2, "CargarDatos", (object)null);
                    mat2.SetResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]);
                    sec2.SetResult((IReadOnlyList<ReporteStockSeccion>)new[] { new ReporteStockSeccion { Id = 1, Nombre = "Sec", Materiales = new List<int>() } });
                    Esperar(() => (bool)Campo(modal2, "cargado"));

                    Llamar(modal2, "GuardarTodo");
                    Assert.False(modal2.Enabled);
                    modal2.Dispose();
                    Assert.True(modal2.IsDisposed);
                    guardar.SetResult(0);
                    Application.DoEvents();
                    Assert.Equal(0, erroresModal);
                    Assert.False(modal2.Enabled);
                }

                var cargaVista = executor.Agregar();
                int erroresVista = 0;
                var pdf = new Pdf();
                using (var vista = Vista(executor, pdf, () => identidad, () => true, () => "no.pdf", _ => {}, _ => erroresVista++)) {
                    var handle = vista.Handle;
                    Cargar(vista);
                    vista.Dispose();
                    bool habilitadoTrasDispose = vista.Enabled;
                    Assert.True(vista.IsDisposed);
                    cargaVista.SetException(new Exception("error carga vista cancelada"));
                    Application.DoEvents();
                    Assert.Equal(0, erroresVista);
                    Assert.Equal(habilitadoTrasDispose, vista.Enabled);
                }
            });
        }
    }
}
