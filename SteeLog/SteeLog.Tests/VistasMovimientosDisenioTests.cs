using System;
using System.Threading;
using System.Windows.Forms;
using SteeLog.Vistas.Movimientos;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class VistasMovimientosDisenioTests
    {
        [Fact]
        public void VistasDeMovimientos_SeInstancianYDisponenSinLanzarExcepcionEnEntornoDiseniador()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try
                {
                    var creadores = new Func<Form>[] {
                        () => new Vista_Movimientos(new ServiciosVistaDePrueba()),
                        () => new Vista_HistorialMovimientos(new ServiciosVistaDePrueba())
                    };
                    foreach (var crear in creadores)
                    {
                        using (var form = crear())
                        {
                            Assert.NotNull(form);
                            Assert.False(form.IsDisposed);
                            var handle = form.Handle;
                            Assert.NotEqual(IntPtr.Zero, handle);
                        }
                    }
                }
                catch (Exception e) { error = e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(30))); Assert.Null(error);
        }
    }
}
