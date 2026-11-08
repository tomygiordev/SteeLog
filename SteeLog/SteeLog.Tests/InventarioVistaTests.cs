using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Inventario;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public class InventarioVistaTests
    {
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public readonly Queue<TaskCompletionSource<List<InventarioRecorte>>> Respuestas = new Queue<TaskCompletionSource<List<InventarioRecorte>>>();
            public int Llamadas;
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                Llamadas++;
                return (TResult)(object)await Respuestas.Dequeue().Task;
            }
        }

        private static void Esperar(Task tarea)
        {
            var reloj = Stopwatch.StartNew();
            while (!tarea.IsCompleted && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(tarea.IsCompleted, "La carga de la vista no terminó.");
            tarea.GetAwaiter().GetResult();
        }

        [Fact]
        public void ComboDeCategoriaBuscableNoLanzaConElEstiloQueImponeGuna()
        {
            Exception error = null;
            var hilo = new Thread(() =>
            {
                try
                {
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Inventario.Vista_Inventario", true);
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    using (var vista = (Form)Activator.CreateInstance(tipo, new Ejecutor()))
                    {
                        var combo = (ComboBox)tipo.GetField("cmbCategoria", flags).GetValue(vista);
                        var handle = combo.Handle;
                        combo.Items.Add("PERFILERIA"); combo.Items.Add("PLACAS");
                        // Recorrido F9: en la demo real esta configuración lanzaba InvalidOperationException
                        // y CargarFiltros abandonaba obras/estado de filtros sin avisar.
                        tipo.GetMethod("ConfigurarComboBuscable", flags).Invoke(vista, new object[] { combo });
                        Assert.Equal(AutoCompleteSource.ListItems, combo.AutoCompleteSource);
                        Assert.Equal(AutoCompleteMode.SuggestAppend, combo.AutoCompleteMode);
                        Assert.False(vista.Visible);
                    }
                }
                catch (Exception e) { error = e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); hilo.Join();
            if (error != null) throw new Xunit.Sdk.XunitException(error.ToString());
        }

        [Fact]
        public void VistaRealDescartaCargaViejaYCierreCancelaSinConsultaEnConstructor()
        {
            Exception error = null;
            var hilo = new Thread(() =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Inventario.Vista_Inventario", true);
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var ejecutor = new Ejecutor();
                    using (var vista = (Form)Activator.CreateInstance(tipo, ejecutor))
                    {
                        Assert.Equal(0, ejecutor.Llamadas);
                        var grid = (DataGridView)tipo.GetField("dgvInventario", flags).GetValue(vista);
                        var formularioHandle = vista.Handle;
                        var gridHandle = grid.Handle;
                        var version = tipo.GetField("versionLista", flags);
                        var mostrar = tipo.GetMethod("MostrarRecortes", flags);
                        var antigua = new TaskCompletionSource<List<InventarioRecorte>>();
                        var nueva = new TaskCompletionSource<List<InventarioRecorte>>();
                        ejecutor.Respuestas.Enqueue(antigua); ejecutor.Respuestas.Enqueue(nueva);
                        version.SetValue(vista, 1);
                        var primera = (Task)mostrar.Invoke(vista, new object[] { "", "", 1 });
                        version.SetValue(vista, 2);
                        var segunda = (Task)mostrar.Invoke(vista, new object[] { "", "", 2 });
                        nueva.SetResult(new List<InventarioRecorte> { new InventarioRecorte { material = "Nuevo", categoria = "C", cantidad = 1 } });
                        Esperar(segunda);
                        antigua.SetResult(new List<InventarioRecorte> { new InventarioRecorte { material = "Viejo", categoria = "C", cantidad = 1 } });
                        Esperar(primera);
                        Assert.Equal("Nuevo", grid.Rows[0].Cells["Material"].Value);

                        var tardia = new TaskCompletionSource<List<InventarioRecorte>>();
                        ejecutor.Respuestas.Enqueue(tardia); version.SetValue(vista, 3);
                        var tercera = (Task)mostrar.Invoke(vista, new object[] { "", "", 3 });
                        typeof(Form).GetMethod("OnFormClosed", flags).Invoke(vista, new object[] { new FormClosedEventArgs(CloseReason.UserClosing) });
                        tardia.SetResult(new List<InventarioRecorte> { new InventarioRecorte { material = "Tardío", categoria = "C", cantidad = 1 } });
                        Esperar(tercera);
                        Assert.Equal("Nuevo", grid.Rows[0].Cells["Material"].Value);
                    }

                    var ejecutorDispose = new Ejecutor();
                    using (var vista = (Form)Activator.CreateInstance(tipo, ejecutorDispose))
                    {
                        var pendiente = new TaskCompletionSource<List<InventarioRecorte>>();
                        ejecutorDispose.Respuestas.Enqueue(pendiente);
                        tipo.GetField("versionLista", flags).SetValue(vista, 1);
                        var tarea = (Task)tipo.GetMethod("MostrarRecortes", flags).Invoke(vista, new object[] { "", "", 1 });
                        var token = (CancellationTokenSource)tipo.GetField("cierre", flags).GetValue(vista);
                        vista.Dispose();
                        Assert.True(token.IsCancellationRequested);
                        pendiente.SetResult(new List<InventarioRecorte>());
                        Esperar(tarea);
                    }
                }
                catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(15)));
            Assert.Null(error);
        }
    }
}
