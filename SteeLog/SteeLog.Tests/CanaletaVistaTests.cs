using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Activos;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CanaletaVistaTests
    {
        private sealed class EjecutorApertura : IUseCaseExecutor
        {
            public int Conciliaciones, HiloConciliacion;
            public readonly TaskCompletionSource<bool> Conciliacion = new TaskCompletionSource<bool>();
            public readonly TaskCompletionSource<IReadOnlyList<ObraInsumoLectura>> Obras = new TaskCompletionSource<IReadOnlyList<ObraInsumoLectura>>();
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TUseCase) == typeof(OperacionesCanaleta)) { Interlocked.Increment(ref Conciliaciones); HiloConciliacion = Thread.CurrentThread.ManagedThreadId; return (TResult)(object)await Conciliacion.Task; }
                if (typeof(TResult) == typeof(IReadOnlyList<ObraInsumoLectura>)) return (TResult)(object)await Obras.Task;
                if (typeof(TResult) == typeof(IReadOnlyList<CanaletaLectura>)) return (TResult)(object)new List<CanaletaLectura>();
                throw new InvalidOperationException();
            }
        }
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly Queue<TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>> Fotos = new Queue<TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>>();
            public readonly Queue<TaskCompletionSource<byte[]>> Previews = new Queue<TaskCompletionSource<byte[]>>();
            public readonly TaskCompletionSource<int> Guardado = new TaskCompletionSource<int>();
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            {
                Llamadas++;
                if (typeof(TResult) == typeof(IReadOnlyList<FotoCanaletaLectura>)) return (TResult)(object)await Fotos.Dequeue().Task;
                if (typeof(TResult) == typeof(byte[])) return (TResult)(object)await Previews.Dequeue().Task;
                if (typeof(TResult) == typeof(int)) return (TResult)(object)await Guardado.Task;
                throw new InvalidOperationException("Consulta inesperada: " + typeof(TResult));
            }
        }
        private sealed class EjecutorLimpieza : IUseCaseExecutor
        {
            public int Adjuntos;
            public Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int)) {
                    Adjuntos++;
                    if (Adjuntos == 1) throw new CanaletaLimpiezaPendienteException("foto", 123, new IOException());
                    throw new IOException("falló el segundo adjunto");
                }
                if (typeof(TResult) == typeof(IReadOnlyList<FotoCanaletaLectura>))
                    return Task.FromResult((TResult)(object)new List<FotoCanaletaLectura> { new FotoCanaletaLectura { Id=123, NombreArchivo="confirmada" } });
                throw new InvalidOperationException();
            }
        }
        private sealed class EjecutorRecargaFallida : IUseCaseExecutor
        {
            public int Escrituras, Lecturas;
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int)) { Escrituras++; return Task.FromResult((TResult)(object)17); }
                if (typeof(TResult) == typeof(bool)) { Escrituras++; return Task.FromResult((TResult)(object)true); }
                if (typeof(TResult) == typeof(IReadOnlyList<CanaletaLectura>)) { Lecturas++; throw new IOException("lectura fallida"); }
                throw new InvalidOperationException();
            }
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OperacionConfirmadaConRecargaFallidaAdvierteSinRepetir(bool eliminar)
        {
            Sta(() => {
                var ejecutor = new EjecutorRecargaFallida();
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_CanaletasPlegados", true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new ServiciosVistaDePrueba(), new CanaletasPresenter(ejecutor) }, null)) {
                    var avisos = new List<string>(); var exitos = new List<string>();
                    tipo.GetField("AdvertirAdjuntos", Privado).SetValue(vista, new Action<string>(avisos.Add));
                    tipo.GetField("InformarOperacion", Privado).SetValue(vista, new Action<string>(exitos.Add));
                    tipo.GetField("_inicializando", Privado).SetValue(vista, false);
                    if (eliminar) tipo.GetField("_idSeleccionado", Privado).SetValue(vista, 17);
                    var tarea = (Task)tipo.GetMethod(eliminar ? "EliminarCoreAsync" : "GuardarCoreAsync", Privado)
                        .Invoke(vista, eliminar ? null : new object[] { 1, "trabajo", 10m, false });
                    tarea.GetAwaiter().GetResult();
                    Assert.Equal(1, ejecutor.Escrituras); Assert.Equal(1, ejecutor.Lecturas);
                    Assert.Single(exitos); Assert.Contains("Operación confirmada", Assert.Single(avisos));
                    Assert.Equal(-1, tipo.GetField("_idSeleccionado", Privado).GetValue(vista));
                    Assert.False(vista.Visible);
                }
            });
        }
        private const BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;
        [Fact]
        public void AperturaConciliaUnaVezFueraDeUiSinBloquearCarga()
        {
            Sta(() => {
                var e = new EjecutorApertura(); int hiloUi = Thread.CurrentThread.ManagedThreadId;
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_CanaletasPlegados", true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new ServiciosVistaDePrueba(), new CanaletasPresenter(e) }, null)) {
                    string aviso = null; tipo.GetField("AdvertirConciliacion", Privado).SetValue(vista, new Action<string>(mensaje => aviso = mensaje));
                    var handle = vista.Handle;
                    tipo.GetMethod("OnLoad", Privado).Invoke(vista, new object[] { EventArgs.Empty });
                    var guardar = (Task)tipo.GetMethod("GuardarCoreAsync", Privado).Invoke(vista, new object[] { 1, "trabajo", 10m, false });
                    Esperar(guardar);
                    e.Obras.SetResult(new List<ObraInsumoLectura>());
                    var reloj = Stopwatch.StartNew();
                    while (((bool)tipo.GetField("_inicializando", Privado).GetValue(vista) || Volatile.Read(ref e.Conciliaciones) == 0) && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                    Assert.False((bool)tipo.GetField("_inicializando", Privado).GetValue(vista));
                    Assert.Equal(1, e.Conciliaciones); Assert.NotEqual(hiloUi, e.HiloConciliacion);
                    Assert.False(e.Conciliacion.Task.IsCompleted);
                    tipo.GetMethod("OnLoad", Privado).Invoke(vista, new object[] { EventArgs.Empty });
                    Assert.Equal(1, e.Conciliaciones);
                    e.Conciliacion.SetException(new IOException("disco"));
                    reloj.Restart(); while (aviso == null && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                    Assert.Contains("disco", aviso); Assert.False((bool)tipo.GetField("_inicializando", Privado).GetValue(vista));
                }
            });
        }
        private static void Esperar(Task task)
        {
            var reloj = Stopwatch.StartNew();
            while (!task.IsCompleted && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(task.IsCompleted); Assert.False(task.IsFaulted, task.Exception?.ToString());
        }
        private static void Sta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); accion(); } catch (Exception ex) { error = ex; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private static Form Crear(Ejecutor e, out Type tipo)
        {
            tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_CanaletasPlegados", true);
            return (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new ServiciosVistaDePrueba(), new CanaletasPresenter(e) }, null);
        }
        [Fact]
        public void CargaDeFotosDescartaRespuestaObsoletaYCierreCancela()
        {
            Sta(() => {
                var e = new Ejecutor(); var vista = Crear(e, out Type tipo);
                Assert.Equal(0, e.Llamadas); var handle = vista.Handle;
                var lista = (ListBox)tipo.GetField("lstFotos", Privado).GetValue(vista); var lh = lista.Handle;
                var a = new TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>(); var b = new TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>();
                e.Fotos.Enqueue(a); e.Fotos.Enqueue(b); var cargar = tipo.GetMethod("CargarFotosExistenteAsync", Privado);
                var primera = (Task)cargar.Invoke(vista, new object[] { 1 }); var segunda = (Task)cargar.Invoke(vista, new object[] { 2 });
                b.SetResult(new[] { new FotoCanaletaLectura { Id=2, NombreArchivo="nueva" } }); Esperar(segunda);
                a.SetResult(new[] { new FotoCanaletaLectura { Id=1, NombreArchivo="vieja" } }); Esperar(primera);
                Assert.Single(lista.Items); Assert.Equal("nueva", lista.Items[0].ToString());
                var c = new TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>(); e.Fotos.Enqueue(c);
                var tercera = (Task)cargar.Invoke(vista, new object[] { 3 });
                typeof(Form).GetMethod("OnFormClosed", Privado).Invoke(vista, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
                c.SetResult(new[] { new FotoCanaletaLectura { NombreArchivo="tardía" } }); Esperar(tercera);
                Assert.Empty(lista.Items); vista.Dispose();
                var otra = Crear(new Ejecutor(), out tipo); var cierre = (CancellationTokenSource)tipo.GetField("_cierre", Privado).GetValue(otra);
                otra.Dispose(); Assert.True(cierre.IsCancellationRequested);
            });
        }
        private static byte[] Imagen(Color color)
        {
            using (var imagen = new Bitmap(2, 2)) using (var stream = new MemoryStream())
            { imagen.SetPixel(0, 0, color); imagen.Save(stream, ImageFormat.Png); return stream.ToArray(); }
        }
        [Fact]
        public void PreviewObsoletoNoReemplazaFotoActualNiActualizaTrasDispose()
        {
            Sta(() => {
                var e = new Ejecutor(); var vista = Crear(e, out Type tipo); var handle = vista.Handle;
                var lista = (ListBox)tipo.GetField("lstFotos", Privado).GetValue(vista); var lh = lista.Handle;
                var preview = (PictureBox)tipo.GetField("picPreview", Privado).GetValue(vista);
                var fotos = new TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>(); e.Fotos.Enqueue(fotos);
                var a = new TaskCompletionSource<byte[]>(); var b = new TaskCompletionSource<byte[]>(); e.Previews.Enqueue(a); e.Previews.Enqueue(b);
                var carga = (Task)tipo.GetMethod("CargarFotosExistenteAsync", Privado).Invoke(vista, new object[] { 1 });
                fotos.SetResult(new[] { new FotoCanaletaLectura { Id=1, NombreArchivo="a", ObjectPath="a" }, new FotoCanaletaLectura { Id=2, NombreArchivo="b", ObjectPath="b" } }); Esperar(carga);
                lista.SelectedIndex = 1; b.SetResult(Imagen(Color.Blue));
                var reloj = Stopwatch.StartNew(); while (preview.Image == null && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                Assert.NotNull(preview.Image); a.SetResult(Imagen(Color.Red)); Application.DoEvents();
                Assert.Equal(Color.Blue.ToArgb(), ((Bitmap)preview.Image).GetPixel(0,0).ToArgb());
                vista.Dispose();
            });
        }
        [Fact]
        public void DisposeDurantePreviewPendienteNoActualizaControl()
        {
            Sta(() => {
                var e = new Ejecutor(); var vista = Crear(e, out Type tipo); var handle = vista.Handle;
                var fotos = new TaskCompletionSource<IReadOnlyList<FotoCanaletaLectura>>(); var preview = new TaskCompletionSource<byte[]>();
                e.Fotos.Enqueue(fotos); e.Previews.Enqueue(preview);
                var carga = (Task)tipo.GetMethod("CargarFotosExistenteAsync", Privado).Invoke(vista, new object[] { 1 });
                fotos.SetResult(new[] { new FotoCanaletaLectura { Id=1, NombreArchivo="a", ObjectPath="a" } }); Esperar(carga);
                var cierre = (CancellationTokenSource)tipo.GetField("_cierre", Privado).GetValue(vista);
                vista.Dispose(); Assert.True(cierre.IsCancellationRequested); preview.SetResult(Imagen(Color.Red));
                for (int i=0; i<10; i++) { Application.DoEvents(); Thread.Sleep(5); }
            });
        }
        [Fact]
        public void LecturaRechazaArchivoExcesivoAntesDeCargarlo()
        {
            var ruta = Path.Combine(Path.GetTempPath(), "steelog-foto-test-" + Guid.NewGuid().ToString("N"));
            try {
                using (var archivo = new FileStream(ruta, FileMode.CreateNew)) archivo.SetLength(8 * 1024 * 1024 + 1);
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_CanaletasPlegados", true);
                var leer = tipo.GetMethod("LeerArchivoFotoAsync", BindingFlags.Static | BindingFlags.NonPublic);
                var tarea = (Task<byte[]>)leer.Invoke(null, new object[] { ruta, CancellationToken.None });
                Assert.Throws<InvalidDataException>(() => tarea.GetAwaiter().GetResult());
            } finally { File.Delete(ruta); }
        }
        [Fact]
        public void LimpiezaPendienteConfirmadaNoSeOfreceParaReintentar()
        {
            Sta(() => {
                var e = new EjecutorLimpieza(); var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_CanaletasPlegados", true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new ServiciosVistaDePrueba(), new CanaletasPresenter(e) }, null)) {
                    var avisos = new List<string>(); tipo.GetField("AdvertirAdjuntos", Privado).SetValue(vista, new Action<string>(avisos.Add));
                    var handle = vista.Handle; tipo.GetField("_inicializando", Privado).SetValue(vista, false);
                    var lista = (ListBox)tipo.GetField("lstFotos", Privado).GetValue(vista); var lh = lista.Handle;
                    var tipoFoto = tipo.GetNestedType("FotoListItem", BindingFlags.NonPublic);
                    foreach (var nombre in new[] { "confirmada", "fallida" }) {
                        var foto = Activator.CreateInstance(tipoFoto, true);
                        tipoFoto.GetProperty("NombreMostrar").SetValue(foto, nombre);
                        tipoFoto.GetProperty("BytesLocal").SetValue(foto, Imagen(Color.Blue));
                        lista.Items.Add(foto);
                    }
                    Esperar((Task)tipo.GetMethod("AdjuntarPendientesAsync", Privado).Invoke(vista, new object[] { 1, 0 }));
                    Assert.Equal(2, e.Adjuntos); Assert.Equal(2, lista.Items.Count);
                    Assert.Equal("confirmada", lista.Items[0].ToString());
                    Assert.Equal("[pendiente] fallida", lista.Items[1].ToString());
                    Assert.Contains(avisos, x => x.Contains("1 foto(s) pendientes para reintentar"));
                    Assert.Contains(avisos, x => x.Contains("queda limpieza de archivos pendiente"));
                }
            });
        }
        [Fact]
        public void NoEscribeMientrasCargaYNoAplicaGuardadoTrasCerrar()
        {
            Sta(() => {
                var e = new Ejecutor(); var vista = Crear(e, out Type tipo); var handle = vista.Handle;
                var guardar = tipo.GetMethod("GuardarCoreAsync", Privado);
                Esperar((Task)guardar.Invoke(vista, new object[] { 1, "trabajo", 10m, false }));
                Assert.Equal(0, e.Llamadas);
                tipo.GetField("_inicializando", Privado).SetValue(vista, false);
                tipo.GetField("_cargandoFotos", Privado).SetValue(vista, true);
                Esperar((Task)guardar.Invoke(vista, new object[] { 1, "trabajo", 10m, false }));
                Assert.Equal(0, e.Llamadas);
                tipo.GetField("_cargandoFotos", Privado).SetValue(vista, false);
                var pendiente = (Task)guardar.Invoke(vista, new object[] { 1, "trabajo", 10m, false });
                typeof(Form).GetMethod("OnFormClosed", Privado).Invoke(vista, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
                e.Guardado.SetResult(42); Esperar(pendiente);
                Assert.Equal(-1, tipo.GetField("_idSeleccionado", Privado).GetValue(vista)); vista.Dispose();
            });
        }
        [Fact]
        public void GuardadoIgnoraDobleEnvioYRespuestaTrasCambioOCierre()
        {
            Sta(() => {
                var e = new Ejecutor(); using (var vista = Crear(e, out Type tipo)) {
                    var handle = vista.Handle; tipo.GetField("_inicializando", Privado).SetValue(vista, false);
                    var guardar = tipo.GetMethod("GuardarCoreAsync", Privado);
                    var primera = (Task)guardar.Invoke(vista, new object[] { 1, "trabajo", 10m, false });
                    var segunda = (Task)guardar.Invoke(vista, new object[] { 1, "trabajo", 10m, false });
                    Esperar(segunda); Assert.Equal(1, e.Llamadas);
                    tipo.GetField("_versionSeleccion", Privado).SetValue(vista, 99);
                    e.Guardado.SetResult(42); Esperar(primera);
                    Assert.Equal(-1, tipo.GetField("_idSeleccionado", Privado).GetValue(vista));
                }
            });
        }
        [Fact]
        public void ErrorFotosNoBloqueaCursorYSeRecuperaConLimpiar()
        {
            Sta(() => {
                var e = new Ejecutor();
                using (var vista = Crear(e, out Type tipo)) {
                    var handle = vista.Handle;
                    tipo.GetField("_inicializando", Privado).SetValue(vista, false); tipo.GetField("AdvertirAdjuntos", Privado).SetValue(vista, new Action<string>(_ => { }));
                    tipo.GetField("_errorFotos", Privado).SetValue(vista, true);
                    tipo.GetMethod("CambiarEstadoGuardado", Privado).Invoke(vista, new object[] { false });

                    var btnGuardar = (Control)tipo.GetField("btnGuardar", Privado).GetValue(vista);
                    Assert.False(btnGuardar.Enabled);
                    Assert.Equal(Cursors.Default, vista.Cursor);

                    var btnLimpiar = (Control)tipo.GetField("btnLimpiar", Privado).GetValue(vista);
                    tipo.GetMethod("btnLimpiar_Click", Privado).Invoke(vista, new object[] { btnLimpiar, EventArgs.Empty });

                    Assert.False((bool)tipo.GetField("_errorFotos", Privado).GetValue(vista));
                    Assert.True(btnGuardar.Enabled);
                }
            });
        }
        [Fact]
        public void FotoAusenteEnDiscoLimpiaPreviewYNoLanzaCodec()
        {
            Sta(() => {
                var e = new Ejecutor();
                using (var vista = Crear(e, out Type tipo)) {
                    var handle = vista.Handle;
                    var preview = (PictureBox)tipo.GetField("picPreview", Privado).GetValue(vista);
                    preview.Image = new Bitmap(2, 2);
                    var tcs = new TaskCompletionSource<byte[]>();
                    e.Previews.Enqueue(tcs);
                    var tipoFoto = tipo.GetNestedType("FotoListItem", BindingFlags.NonPublic);
                    var item = Activator.CreateInstance(tipoFoto, true);
                    tipoFoto.GetProperty("ObjectPath").SetValue(item, "inexistente.jpg"); string aviso = null; tipo.GetField("AdvertirAdjuntos", Privado).SetValue(vista, new Action<string>(m => aviso = m)); var lista = (ListBox)tipo.GetField("lstFotos", Privado).GetValue(vista); lista.Items.Add(item); lista.SelectedItem = item; int vPrev = (int)tipo.GetField("_versionPreview", Privado).GetValue(vista);
                    var tarea = (Task)tipo.GetMethod("MostrarPreviewAsync", Privado).Invoke(vista, new object[] { item, vPrev });
                    tcs.SetResult(new byte[0]);
                    Esperar(tarea);
                    Assert.Null(preview.Image);
                }
            });
        }
        private sealed class EjecutorAltaAdjuntoIncierto : IUseCaseExecutor
        {
            public int CanaletasConsultadas;
            public int AdjuntosIntentados;
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int))
                {
                    AdjuntosIntentados++;
                    if (AdjuntosIntentados == 1)
                        return Task.FromResult((TResult)(object)100);
                    throw new CanaletaResultadoInciertoException("falla incierta en adjunto", new IOException());
                }
                if (typeof(TResult) == typeof(IReadOnlyList<CanaletaLectura>))
                {
                    Interlocked.Increment(ref CanaletasConsultadas);
                    return Task.FromResult((TResult)(object)new List<CanaletaLectura>
                    {
                        new CanaletaLectura { Id = 100, ObraId = 1, Descripcion = "nueva", Precio = 50m }
                    });
                }
                if (typeof(TResult) == typeof(IReadOnlyList<FotoCanaletaLectura>))
                    return Task.FromResult((TResult)(object)new List<FotoCanaletaLectura>());
                throw new InvalidOperationException();
            }
        }
        [Fact]
        public void AltaConAdjuntoInciertoRefrescaGrilla()
        {
            Sta(() => {
                var e = new EjecutorAltaAdjuntoIncierto();
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_CanaletasPlegados", true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new ServiciosVistaDePrueba(), new CanaletasPresenter(e) }, null)) {
                    var handle = vista.Handle;
                    tipo.GetField("_inicializando", Privado).SetValue(vista, false); tipo.GetField("AdvertirAdjuntos", Privado).SetValue(vista, new Action<string>(_ => { }));
                    var lista = (ListBox)tipo.GetField("lstFotos", Privado).GetValue(vista);
                    var tipoFoto = tipo.GetNestedType("FotoListItem", BindingFlags.NonPublic);
                    var foto = Activator.CreateInstance(tipoFoto, true);
                    tipoFoto.GetProperty("NombreMostrar").SetValue(foto, "foto1.png");
                    tipoFoto.GetProperty("BytesLocal").SetValue(foto, Imagen(Color.Blue));
                    lista.Items.Add(foto);

                    var tarea = (Task)tipo.GetMethod("GuardarCoreAsync", Privado).Invoke(vista, new object[] { 1, "nueva", 50m, false });
                    Esperar(tarea);

                    Assert.True((bool)tipo.GetField("_errorFotos", Privado).GetValue(vista));
                    Assert.True(e.CanaletasConsultadas > 0);
                    Assert.Equal(Cursors.Default, vista.Cursor);
                }
            });
        }
    }
}
