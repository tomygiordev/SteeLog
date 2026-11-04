using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Xunit;

public sealed class DemoDisplayTests
{
    [Theory]
    [InlineData("sin-demo")]
    [InlineData("inexistente")]
    [InlineData("valida")]
    public void PantallaDemoEsOptativaYSeConfiguraAntesDeMostrar(string caso)
    {
        Exception error = null;
        var thread = new Thread(() => {
            try
            {
                var host = Assembly.LoadFrom(Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY"));
                var configurar = host.GetType("SteeLog.Program", true).GetMethod("ConfigurarPantallaDemo", BindingFlags.Static | BindingFlags.NonPublic);
                using (var form = new Form { StartPosition = FormStartPosition.CenterScreen, WindowState = FormWindowState.Maximized })
                {
                    if (caso == "inexistente")
                    {
                        var failure = Assert.Throws<TargetInvocationException>(() => configurar.Invoke(null, new object[] { form, "demo", "monitor-inexistente-f9" }));
                        Assert.IsType<InvalidOperationException>(failure.InnerException);
                    }
                    else
                    {
                        var pantalla = Screen.AllScreens[0];
                        configurar.Invoke(null, new object[] { form, caso == "valida" ? "demo" : null, pantalla.DeviceName });
                        Assert.Equal(caso == "valida" ? FormStartPosition.Manual : FormStartPosition.CenterScreen, form.StartPosition);
                        Assert.Equal(FormWindowState.Maximized, form.WindowState);
                        if (caso == "valida") Assert.True(pantalla.WorkingArea.Contains(form.Location));
                    }
                    Assert.False(form.Visible);
                    Assert.False(form.IsHandleCreated);
                }
            }
            catch (Exception e) { error = e; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        Assert.Null(error);
    }
}
