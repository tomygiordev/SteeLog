using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class InformeObraLifecycleTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Ejecutar : IUseCaseExecutor
        {
            public Queue<TaskCompletionSource<byte[]>> Fotos = new Queue<TaskCompletionSource<byte[]>>();
            public Queue<TaskCompletionSource<IReadOnlyList<CanaletaLectura>>> Canaletas = new Queue<TaskCompletionSource<IReadOnlyList<CanaletaLectura>>>();
            public async Task<TResult> ExecuteAsync<T, TResult>(Func<T, Task<TResult>> f) where T : class
            {
                if (typeof(TResult) == typeof(byte[])) return (TResult)(object)await Fotos.Dequeue().Task;
                if (typeof(TResult) == typeof(IReadOnlyList<CanaletaLectura>)) return (TResult)(object)await Canaletas.Dequeue().Task;
                throw new InvalidOperationException("Consulta inesperada");
            }
        }
        private sealed class Lector<T>
        {
            public readonly Queue<TaskCompletionSource<T>> Pendientes = new Queue<TaskCompletionSource<T>>();
            public Task<T> Leer(int id, string nombre, CancellationToken token) => Pendientes.Dequeue().Task;
        }
        private static byte[] Imagen(Color color)
        {
            using (var b = new Bitmap(2, 2)) using (var m = new MemoryStream()) { b.SetPixel(0, 0, color); b.Save(m, ImageFormat.Png); return m.ToArray(); }
        }
        private static void Sta(Action action)
        {
            Exception error = null; var thread = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); } catch (Exception ex) { error = ex; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private static void Esperar(Task task) { var sw=Stopwatch.StartNew(); while (!task.IsCompleted && sw.ElapsedMilliseconds < 5000) { Application.DoEvents(); Thread.Sleep(5); } Assert.True(task.IsCompleted); Assert.False(task.IsFaulted, task.Exception?.ToString()); }
        [Fact]
        public void LecturaAnteriorNoReemplazaObraActual()
        {
            Sta(() => {
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Reportes.Vista_InformeObra", true);
                var lectura = tipo.GetNestedType("LecturasObra", BindingFlags.NonPublic);
                var lectorTipo = typeof(Lector<>).MakeGenericType(lectura);
                var lector = Activator.CreateInstance(lectorTipo);
                var tareaTipo = typeof(TaskCompletionSource<>).MakeGenericType(lectura);
                var a = Activator.CreateInstance(tareaTipo); var b = Activator.CreateInstance(tareaTipo);
                var cola = lectorTipo.GetField("Pendientes").GetValue(lector);
                cola.GetType().GetMethod("Enqueue").Invoke(cola, new[] { a });
                cola.GetType().GetMethod("Enqueue").Invoke(cola, new[] { b });
                var delegadoTipo = typeof(Func<,,,>).MakeGenericType(typeof(int), typeof(string), typeof(CancellationToken), typeof(Task<>).MakeGenericType(lectura));
                var cargar = Delegate.CreateDelegate(delegadoTipo, lector, lectorTipo.GetMethod("Leer"));
                var ejecutor = new Ejecutar();
                var ca = new TaskCompletionSource<IReadOnlyList<CanaletaLectura>>(); var cb = new TaskCompletionSource<IReadOnlyList<CanaletaLectura>>();
                ejecutor.Canaletas.Enqueue(ca); ejecutor.Canaletas.Enqueue(cb);
                using (var form = (Form)Activator.CreateInstance(tipo, Private, null, new object[] { new CanaletasPresenter(ejecutor), cargar }, null)) {
                    var handle = form.Handle;
                    var combo = (ComboBox)tipo.GetField("cmbObras", Private).GetValue(form);
                    var dt = new DataTable(); dt.Columns.Add("id", typeof(int)); dt.Columns.Add("nombre", typeof(string)); dt.Rows.Add(1, "A"); dt.Rows.Add(2, "B");
                    tipo.GetField("cargando", Private).SetValue(form, true);
                    combo.DisplayMember = "nombre"; combo.ValueMember = "id"; combo.DataSource = dt; combo.SelectedIndex = -1;
                    tipo.GetField("cargando", Private).SetValue(form, false);
                    combo.SelectedIndex = 0; combo.SelectedIndex = 1;
                    Assert.False(((Control)tipo.GetField("btnDescargar", Private).GetValue(form)).Enabled);
                    Func<object> vacia = () => {
                        var v = Activator.CreateInstance(lectura, true);
                        foreach (var nombre in new[] { "Movimientos", "Herramientas", "Insumos" }) {
                            var campo = lectura.GetField(nombre, Private); campo.SetValue(v, Activator.CreateInstance(campo.FieldType));
                        }
                        return v;
                    };
                    tareaTipo.GetMethod("SetResult").Invoke(b, new[] { vacia() });
                    cb.SetResult(new[] { new CanaletaLectura { Id = 2, Descripcion = "vigente" } });
                    var reloj = Stopwatch.StartNew(); var boton = (Control)tipo.GetField("btnDescargar", Private).GetValue(form);
                    while (!boton.Enabled && reloj.ElapsedMilliseconds < 5000) { Application.DoEvents(); Thread.Sleep(5); }
                    Assert.True(boton.Enabled);
                    tareaTipo.GetMethod("SetResult").Invoke(a, new[] { vacia() });
                    ca.SetResult(new[] { new CanaletaLectura { Id = 1, Descripcion = "obsoleta" } });
                    Application.DoEvents();
                    var resultado = (List<CanaletaLectura>)tipo.GetField("canaletasPlegadosActuales", Private).GetValue(form);
                    Assert.Single(resultado); Assert.Equal("vigente", resultado[0].Descripcion);
                }
            });
        }
        [Fact]
        public void VisorDescartaRespuestaAnteriorYRespuestaTrasDispose()
        {
            Sta(() => {
                var ejecutor = new Ejecutar(); var a=new TaskCompletionSource<byte[]>(); var b=new TaskCompletionSource<byte[]>(); ejecutor.Fotos.Enqueue(a); ejecutor.Fotos.Enqueue(b);
                var vista = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Reportes.Vista_InformeObra", true);
                var tipo = vista.GetNestedType("VisorFotosCanaletaForm", BindingFlags.NonPublic);
                var fotos = new List<FotoCanaletaLectura> { new FotoCanaletaLectura { NombreArchivo="A", ObjectPath="a" }, new FotoCanaletaLectura { NombreArchivo="B", ObjectPath="b" } };
                var form = (Form)Activator.CreateInstance(tipo, BindingFlags.Instance | BindingFlags.Public, null, new object[] { fotos, new CanaletasPresenter(ejecutor) }, null);
                var handle=form.Handle; var lista=(ListBox)tipo.GetField("lstFotos",Private).GetValue(form); var picture=(PictureBox)tipo.GetField("picFoto",Private).GetValue(form);
                lista.SelectedIndex=1; b.SetResult(Imagen(Color.Blue)); var sw=Stopwatch.StartNew(); while(picture.Image==null && sw.ElapsedMilliseconds<5000) { Application.DoEvents(); Thread.Sleep(5); }
                Assert.NotNull(picture.Image); a.SetResult(Imagen(Color.Red)); Application.DoEvents(); Assert.Equal(Color.Blue.ToArgb(), ((Bitmap)picture.Image).GetPixel(0,0).ToArgb());
                var c = new TaskCompletionSource<byte[]>(); ejecutor.Fotos.Enqueue(c);
                var pendiente = (Task)tipo.GetMethod("MostrarFotoAsync", Private).Invoke(form, new object[] { fotos[0] });
                var vida = (CancellationTokenSource)tipo.GetField("vidaVisor", Private).GetValue(form);
                form.Dispose(); Assert.True(vida.IsCancellationRequested);
                c.SetResult(Imagen(Color.Green)); Esperar(pendiente); Assert.Null(picture.Image);
            });
        }
        [Theory] [InlineData(true)] [InlineData(false)]
        public void ReporteTerminadoTrasCierreOCambioNoAbreVisor(bool cerrar)
        {
            Sta(() => {
                var tipo=Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Reportes.Vista_InformeObra",true); var form=(Form)Activator.CreateInstance(tipo); var handle=form.Handle;
                var tcs=new TaskCompletionSource<bool>(); bool abierto=false;
                Func<string,int,string,DateTime?,DateTime?,CancellationToken,Task> generar=(ruta,id,nombre,desde,hasta,token)=>tcs.Task;
                tipo.GetField("GenerarInforme",Private).SetValue(form,generar); tipo.GetField("AbrirInforme",Private).SetValue(form,new Action<string>(_=>abierto=true));
                var task=(Task)tipo.GetMethod("GenerarInformeAsync",Private).Invoke(form,new object[] { 1,"Obra",null,null });
                if(cerrar) form.Dispose(); else tipo.GetField("versionObra",Private).SetValue(form,1);
                tcs.SetResult(true); Esperar(task); Assert.False(abierto); if(!cerrar) form.Dispose();
            });
        }
    }
}
