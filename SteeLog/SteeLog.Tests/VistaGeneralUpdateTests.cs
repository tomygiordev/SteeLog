using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Actualizaciones;
using Xunit;

public sealed class VistaGeneralUpdateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VerificacionPendienteNoPublicaNiAbreModalTrasDisponer(bool falla)
    {
        Sta(() => {
            var vista = new SteeLog.VistaGeneral(new SteeLog.Tests.ServiciosVistaDePrueba());
            using (var boton = new Button())
            {
                var pendiente = new TaskCompletionSource<UpdateInfo>();
                int avisos = 0;
                var tarea = Ejecutar(vista, boton, () => pendiente.Task, () => avisos++);
                Assert.False(boton.Enabled);
                vista.Dispose();
                if (falla) pendiente.SetException(new InvalidOperationException("lectura tardía"));
                else pendiente.SetResult(new UpdateInfo());
                Esperar(tarea);
                Assert.Equal(0, avisos);
            }
        });
    }

    [Fact]
    public void VerificacionEsSingleFlightYPermiteReintentarTrasError()
    {
        Sta(() => {
            using (var vista = new SteeLog.VistaGeneral(new SteeLog.Tests.ServiciosVistaDePrueba()))
            using (var boton = new Button())
            {
                var pendiente = new TaskCompletionSource<UpdateInfo>();
                int consultas = 0, avisos = 0;
                Func<Task<UpdateInfo>> consulta = () => { consultas++; return pendiente.Task; };
                var primera = Ejecutar(vista, boton, consulta, () => avisos++);
                Esperar(Ejecutar(vista, boton, consulta, () => avisos++));
                Assert.Equal(1, consultas);
                pendiente.SetException(new InvalidOperationException("fixture"));
                Esperar(primera);
                Assert.Equal(1, avisos);
                Assert.True(boton.Enabled);
                Esperar(Ejecutar(vista, boton, () => Task.FromResult<UpdateInfo>(null), () => avisos++));
                Assert.Equal(2, avisos);
                Assert.False(vista.IsHandleCreated);
            }
        });
    }

    private static Task Ejecutar(SteeLog.VistaGeneral vista, Button boton, Func<Task<UpdateInfo>> consultar, Action aviso)
        => (Task)typeof(SteeLog.VistaGeneral).GetMethod("VerificarActualizacionesAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(vista, new object[] { boton, consultar, new Action<UpdateInfo>(_ => aviso()), new Action<string>(_ => aviso()), new Action<string>(_ => aviso()) });

    private static void Esperar(Task tarea)
    {
        var fin = DateTime.UtcNow.AddSeconds(5);
        while (!tarea.IsCompleted && DateTime.UtcNow < fin) { Application.DoEvents(); Thread.Sleep(5); }
        Assert.True(tarea.IsCompleted);
        tarea.GetAwaiter().GetResult();
    }

    private static void Sta(Action action)
    {
        Exception error = null;
        var hilo = new Thread(() => {
            try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); }
            catch (Exception ex) { error = ex; }
        });
        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        Assert.True(hilo.Join(TimeSpan.FromSeconds(20)));
        Assert.Null(error);
    }
}
