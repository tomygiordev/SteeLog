using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Inventario;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class VistaGeneralStockTests
    {
        private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Sta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); } catch (Exception ex) { error = ex; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private static void Esperar(Func<bool> condicion)
        {
            var reloj = Stopwatch.StartNew();
            while (!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }
        private static Form Crear(Func<CancellationToken, Task<IReadOnlyList<MaterialInventarioLectura>>> consulta,
            Action<int, Action> aviso)
        {
            var tipo = typeof(SteeLog.VistaGeneral);
            return (Form)Activator.CreateInstance(tipo, Privado, null,
                new object[] { consulta, new Func<CancellationToken, Task>(_ => Task.CompletedTask), aviso }, null);
        }
        private static void Iniciar(Form vista) => vista.GetType().GetMethod("VistaGeneral_Shown", Privado).Invoke(vista, new object[] { vista, EventArgs.Empty });

        [Fact]
        public void ConsultaPendienteAlDisponerNoMuestraAviso()
        {
            Sta(() => {
                var pendiente = new TaskCompletionSource<IReadOnlyList<MaterialInventarioLectura>>();
                int consultas = 0, avisos = 0; CancellationToken token = default;
                var vista = Crear(t => { consultas++; token = t; return pendiente.Task; }, (n, a) => avisos++);
                Assert.Equal(0, consultas);
                Iniciar(vista); Assert.Equal(1, consultas);
                vista.Dispose(); Assert.True(token.IsCancellationRequested);
                pendiente.SetResult(new[] { new MaterialInventarioLectura() });
                Application.DoEvents(); Assert.Equal(0, avisos);
            });
        }
        [Fact]
        public void AvisoCapturadoNoAbreModalTrasDisponer()
        {
            Sta(() => {
                Action abrir = null; int cantidad = 0;
                var vista = Crear(_ => Task.FromResult<IReadOnlyList<MaterialInventarioLectura>>(new[] { new MaterialInventarioLectura(), new MaterialInventarioLectura() }),
                    (n, a) => { cantidad = n; abrir = a; });
                Iniciar(vista); Esperar(() => abrir != null); Assert.Equal(2, cantidad);
                vista.Dispose(); abrir();
            });
        }
        [Fact]
        public void MostrarCerrarYDisponerDosVecesNoLanzaNiPublicaResultadosTardios()
        {
            Sta(() => {
                var pendiente = new TaskCompletionSource<IReadOnlyList<MaterialInventarioLectura>>();
                int consultas = 0, avisos = 0; CancellationToken token = default;
                var vista = Crear(t => { consultas++; token = t; return pendiente.Task; }, (n, a) => avisos++);
                vista.Show();
                Esperar(() => consultas == 1);
                vista.Close();
                vista.Dispose();
                vista.Dispose();
                Assert.True(token.IsCancellationRequested);
                pendiente.SetResult(new[] { new MaterialInventarioLectura() });
                Application.DoEvents();
                Assert.Equal(0, avisos);
            });
        }
    }
}
