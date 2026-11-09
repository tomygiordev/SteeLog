using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Inventario;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class VistaGeneralBarraVentanaTests
    {
        private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;

        private static void Sta(Action action)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); } catch (Exception ex) { error = ex; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(20)));
            Assert.Null(error);
        }

        private static Form Crear()
            => (Form)Activator.CreateInstance(typeof(SteeLog.VistaGeneral), Privado, null,
                new object[] {
                    new ServiciosVistaDePrueba(),
                    new Func<CancellationToken, Task<IReadOnlyList<MaterialInventarioLectura>>>(_ => Task.FromResult<IReadOnlyList<MaterialInventarioLectura>>(new MaterialInventarioLectura[0])),
                    new Func<CancellationToken, Task>(_ => Task.CompletedTask),
                    new Action<int, Action>((n, a) => { }) }, null);

        private static Control Barra(Form f) => f.Controls.Find("panelMaximizar", true).Single();

        private static bool Enter(Form f)
            => (bool)typeof(Form).GetMethod("ProcessDialogKey", Privado).Invoke(f, new object[] { Keys.Enter });

        [Fact]
        public void BotonesDeBarraDeVentanaNoSonTabulables()
        {
            Sta(() => {
                using (var vista = Crear())
                {
                    var botones = Barra(vista).Controls.Cast<Control>().Where(c => c is IButtonControl).ToList();
                    Assert.Equal(3, botones.Count);
                    Assert.All(botones, b => Assert.False(b.TabStop));
                    Assert.All(botones, b => Assert.False((bool)typeof(Control).GetMethod("GetStyle", Privado).Invoke(b, new object[] { ControlStyles.Selectable })));
                }
            });
        }

        [Fact]
        public void EnterSinControlQueLoConsumaNoMinimizaLaVentana()
        {
            Sta(() => {
                using (var vista = Crear())
                {
                    vista.WindowState = FormWindowState.Normal; vista.Opacity = 0; vista.ShowInTaskbar = false;
                    vista.Show(); Application.DoEvents();
                    var barra = Barra(vista);
                    var activo = vista.ActiveControl;
                    Assert.False(activo != null && barra.Contains(activo) && activo.GetType().Name == "BotonBarraVentana", "El foco inicial cayo en un boton de la barra");
                    for (int i = 0; i < 40; i++) { vista.SelectNextControl(vista.ActiveControl, true, true, true, true); Assert.False(vista.ActiveControl != null && vista.ActiveControl.GetType().Name == "BotonBarraVentana"); }
                    Enter(vista);
                    Assert.Equal(FormWindowState.Normal, vista.WindowState);
                }
            });
        }
    }
}
