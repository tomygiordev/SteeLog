using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Respaldos;
using Xunit;

/// <summary>En modo demo la carpeta de copias y su configuración quedan dentro de la raíz demo.</summary>
public sealed class RespaldosDemoTests
{
    static object Crear(string root)
    {
        var host = Assembly.LoadFrom(Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY"));
        var type = host.GetType("SteeLog.CompositionRoot.LocalServices", true);
        return type.GetMethod("CreateForEnvironment", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { root, root });
    }
    static T Prop<T>(object o, string nombre) => (T)o.GetType().GetProperty(nombre, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);

    [Fact]
    public async System.Threading.Tasks.Task ElModoDemoUsaLaRaizDemoParaCarpetaYConfiguracion()
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-demo-respaldos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, ".steelog-demo"), "steelog-demo-v1");
            File.WriteAllText(Path.Combine(root, "appsettings.json"),
                "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=127.0.0.1;Port=1;Database=steelog_test_sin_conexion;Username=demo;Password=demo;Timeout=1\"}}");
            using (var servicios = (IDisposable)Crear(root))
            {
                var carpeta = Prop<string>(servicios, "RespaldosDirectory");
                Assert.StartsWith(Path.GetFullPath(root), Path.GetFullPath(carpeta), StringComparison.OrdinalIgnoreCase);
                Assert.Equal(root, Prop<string>(servicios, "RespaldosRaizPermitida"));
                var provider = (IServiceProvider)servicios.GetType().GetField("provider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(servicios);
                var sesion = Prop<Session>(servicios, "Session");
                sesion.Start(new Identity("admin@demo", Role.Admin));
                using (var scope = provider.CreateScope())
                {
                    var servicio = scope.ServiceProvider.GetRequiredService<ServicioRespaldos>();
                    var estado = await servicio.ObtenerEstadoAsync(CancellationToken.None); // sin base ni herramientas: informa, no lanza
                    Assert.Equal(carpeta, estado.Configuracion.Carpeta);
                    Assert.False(estado.BaseConectada);
                    // La configuración no puede apuntar fuera de la raíz demo.
                    var fuera = await servicio.GuardarConfiguracionAsync(new ConfiguracionRespaldo { Carpeta = Path.Combine(Path.GetTempPath(), "fuera-de-demo-" + Guid.NewGuid().ToString("N")) }, CancellationToken.None);
                    Assert.False(fuera.Ok);
                    var dentro = await servicio.GuardarConfiguracionAsync(new ConfiguracionRespaldo { Carpeta = Path.Combine(root, "otra copias"), Conservar = 2, HorasEntreAutomaticos = 3 }, CancellationToken.None);
                    Assert.True(dentro.Ok, dentro.Detalle);
                }
                // El JSON de configuración quedó dentro de la raíz demo.
                Assert.True(File.Exists(Path.Combine(root, "SteeLog", "respaldos.json")));
            }
        }
        finally { Directory.Delete(root, true); }
    }
}
