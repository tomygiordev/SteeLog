using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CatalogosVistaTests
    {
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly Queue<TaskCompletionSource<IReadOnlyList<CategoriaCatalogo>>> Consultas = new Queue<TaskCompletionSource<IReadOnlyList<CategoriaCatalogo>>>();
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                Llamadas++;
                if (typeof(TResult) == typeof(IReadOnlyList<CategoriaCatalogo>)) return (TResult)(object)await Consultas.Dequeue().Task;
                throw new InvalidOperationException("No debe ejecutarse una escritura");
            }
        }
        private sealed class EjecutorGenerico : IUseCaseExecutor
        {
            public int Lecturas, Escrituras;
            public readonly Queue<TaskCompletionSource<object>> Consultas = new Queue<TaskCompletionSource<object>>();
            public readonly TaskCompletionSource<int> Escritura = new TaskCompletionSource<int>();
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int)) { Escrituras++; return (TResult)(object)await Escritura.Task; }
                Lecturas++; return (TResult)await Consultas.Dequeue().Task;
            }
        }
        private static object ListaVacia(string singular)
        {
            var dto = typeof(CategoriaCatalogo).Assembly.GetType("SteeLog.Core.Application.Catalogos." + singular + "Catalogo", true);
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(dto));
        }
        private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Sta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); } catch (Exception ex) { error = ex; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private static void Esperar(Func<bool> condicion)
        {
            var reloj = Stopwatch.StartNew();
            while (!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }
        [Fact]
        public void ConstructorSinConsultaCargaPendienteBloqueaYDisposeCancela()
        {
            Sta(() => {
                var ejecutor = new Ejecutor();
                var pendiente = new TaskCompletionSource<IReadOnlyList<CategoriaCatalogo>>(); ejecutor.Consultas.Enqueue(pendiente);
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Categorias", true);
                var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new CatalogosPresenter(ejecutor) }, null);
                Assert.Equal(0, ejecutor.Llamadas);
                var boton = (Control)tipo.GetField("guna2Button1", Privado).GetValue(vista);
                Assert.False(boton.Enabled);
                var handle = vista.Handle;
                tipo.GetMethod("OnLoad", Privado).Invoke(vista, new object[] { EventArgs.Empty });
                Assert.Equal(1, ejecutor.Llamadas); Assert.False(boton.Enabled);
                var cierre = (CancellationTokenSource)tipo.GetField("_cierre", Privado).GetValue(vista);
                vista.Dispose(); Assert.True(cierre.IsCancellationRequested);
                pendiente.SetResult(new List<CategoriaCatalogo> { new CategoriaCatalogo { Id = 1, Nombre = "A" } });
                Application.DoEvents(); Assert.Equal(1, ejecutor.Llamadas);
            });
        }
        [Fact]
        public void CargaAnteriorNoReemplazaResultadoMasReciente()
        {
            Sta(() => {
                var ejecutor = new Ejecutor();
                var a = new TaskCompletionSource<IReadOnlyList<CategoriaCatalogo>>(); var b = new TaskCompletionSource<IReadOnlyList<CategoriaCatalogo>>();
                ejecutor.Consultas.Enqueue(a); ejecutor.Consultas.Enqueue(b);
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Categorias", true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new CatalogosPresenter(ejecutor) }, null)) {
                    var handle = vista.Handle;
                    var cargar = tipo.GetMethod("CargarAsync", Privado);
                    var primera = (Task)cargar.Invoke(vista, null); var segunda = (Task)cargar.Invoke(vista, null);
                    b.SetResult(new List<CategoriaCatalogo> { new CategoriaCatalogo { Id = 2, Nombre = "B" } }); Esperar(() => segunda.IsCompleted);
                    a.SetResult(new List<CategoriaCatalogo> { new CategoriaCatalogo { Id = 1, Nombre = "A" } }); Esperar(() => primera.IsCompleted);
                    var items = (List<CategoriaCatalogo>)tipo.GetField("_items", Privado).GetValue(vista);
                    Assert.Single(items); Assert.Equal("B", items[0].Nombre);
                }
            });
        }
        [Theory]
        [InlineData("Categorias", "Categoria", "guna2Button1_Click_1")]
        [InlineData("Obras", "Obra", "guna2Button1_Click")]
        [InlineData("Proveedores", "Proveedor", "guna2Button1_Click")]
        [InlineData("Ubicaciones", "Ubicacion", "guna2Button1_Click")]
        [InlineData("Fletes", "Flete", "guna2Button1_Click")]
        public void CadaVistaBloqueaDobleEnvioYConservaContextoNuevo(string plural, string singular, string handler)
        {
            Sta(() => {
                var ejecutor = new EjecutorGenerico();
                var carga = new TaskCompletionSource<object>(); var recarga = new TaskCompletionSource<object>();
                ejecutor.Consultas.Enqueue(carga); ejecutor.Consultas.Enqueue(recarga);
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_" + plural, true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new CatalogosPresenter(ejecutor) }, null)) {
                    tipo.GetField("Confirmar", Privado).SetValue(vista, new Func<string, MessageBoxIcon, bool>((_, __) => true));
                    Assert.Equal(0, ejecutor.Lecturas);
                    var handle = vista.Handle;
                    tipo.GetMethod("OnLoad", Privado).Invoke(vista, new object[] { EventArgs.Empty });
                    var boton = (Control)tipo.GetField("guna2Button1", Privado).GetValue(vista);
                    var grid = (Control)tipo.GetField("dgv" + plural, Privado).GetValue(vista);
                    var texto = (Control)tipo.GetField("txt" + singular, Privado).GetValue(vista);
                    Assert.False(boton.Enabled); Assert.False(grid.Enabled);
                    tipo.GetMethod(handler, Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                    Assert.Equal(0, ejecutor.Escrituras);
                    carga.SetResult(ListaVacia(singular)); Esperar(() => boton.Enabled);
                    texto.Text = "Primer contexto";
                    tipo.GetMethod(handler, Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                    Assert.Equal(1, ejecutor.Escrituras); Assert.False(boton.Enabled); Assert.False(grid.Enabled); Assert.False(texto.Enabled);
                    tipo.GetMethod(handler, Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                    Assert.Equal(1, ejecutor.Escrituras);
                    texto.Text = "Contexto nuevo";
                    ejecutor.Escritura.SetResult(1); Esperar(() => ejecutor.Lecturas == 2);
                    recarga.SetResult(ListaVacia(singular)); Esperar(() => boton.Enabled);
                    Assert.Equal("Contexto nuevo", texto.Text);
                }
            });
        }
        [Theory]
        [InlineData("Categorias", "Categoria")]
        [InlineData("Obras", "Obra")]
        [InlineData("Proveedores", "Proveedor")]
        [InlineData("Ubicaciones", "Ubicacion")]
        [InlineData("Fletes", "Flete")]
        public void CadaVistaCancelaEnDisposeYDescartaRespuestaTardia(string plural, string singular)
        {
            Sta(() => {
                var ejecutor = new EjecutorGenerico(); var carga = new TaskCompletionSource<object>(); ejecutor.Consultas.Enqueue(carga);
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_" + plural, true);
                var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new CatalogosPresenter(ejecutor) }, null);
                var handle = vista.Handle; tipo.GetMethod("OnLoad", Privado).Invoke(vista, new object[] { EventArgs.Empty });
                var cierre = (CancellationTokenSource)tipo.GetField("_cierre", Privado).GetValue(vista);
                vista.Dispose(); Assert.True(cierre.IsCancellationRequested);
                carga.SetResult(ListaVacia(singular)); Application.DoEvents(); Assert.Equal(1, ejecutor.Lecturas); Assert.Equal(0, ejecutor.Escrituras);
            });
        }
        [Theory]
        [InlineData("Categorias", "Categoria")]
        [InlineData("Obras", "Obra")]
        [InlineData("Proveedores", "Proveedor")]
        [InlineData("Ubicaciones", "Ubicacion")]
        [InlineData("Fletes", "Flete")]
        public void CadaVistaCancelaAlCerrar(string plural, string singular)
        {
            Sta(() => {
                var ejecutor = new EjecutorGenerico(); var carga = new TaskCompletionSource<object>(); ejecutor.Consultas.Enqueue(carga);
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_" + plural, true);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new CatalogosPresenter(ejecutor) }, null)) {
                    var handle = vista.Handle; tipo.GetMethod("OnLoad", Privado).Invoke(vista, new object[] { EventArgs.Empty });
                    var cierre = (CancellationTokenSource)tipo.GetField("_cierre", Privado).GetValue(vista);
                    tipo.GetMethod("OnFormClosed", Privado).Invoke(vista, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
                    Assert.True(cierre.IsCancellationRequested);
                    carga.SetResult(ListaVacia(singular)); Application.DoEvents(); Assert.Equal(0, ejecutor.Escrituras);
                }
            });
        }
    }
}
