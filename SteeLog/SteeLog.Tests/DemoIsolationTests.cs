using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SteeLog.Core.Application.Activos;
using SteeLog.Core.Application.Canaletas;
using Xunit;

public sealed class DemoIsolationTests
{
    [Theory]
    [InlineData(typeof(ConsultarInsumos))]
    [InlineData(typeof(GestionarInsumos))]
    [InlineData(typeof(ConsultarCanaletas))]
    [InlineData(typeof(OperacionesCanaleta))]
    public void CasosDeCanaletasEInsumosSeResuelvenDesdeElContenedorRealSinAbrirConexion(Type caso)
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-demo-resolution-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, ".steelog-demo"), "steelog-demo-v1");
            File.WriteAllText(Path.Combine(root, "appsettings.json"),
                "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=127.0.0.1;Port=1;Database=steelog_test_sin_conexion;Username=demo;Password=demo;Timeout=1\"}}");
            using (var services = (IDisposable)Create(root, root))
            {
                var provider = (IServiceProvider)services.GetType().GetField("provider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services);
                using (var scope = provider.CreateScope())
                    Assert.IsType(caso, scope.ServiceProvider.GetRequiredService(caso));
            }
            Assert.False(File.Exists(Path.Combine(root, "SteeLog", "session.dat")));
        }
        finally { Directory.Delete(root, true); }
    }

    private static object Create(string executable, string root)
    {
        var host = Assembly.LoadFrom(Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY"));
        var type = host.GetType("SteeLog.CompositionRoot.LocalServices", true);
        return type.GetMethod("CreateForEnvironment", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { executable, root });
    }
    [Theory]
    [InlineData("relativa")]
    [InlineData("C:relativa")]
    [InlineData("\\relativa")]
    [InlineData("sin-marcador")]
    [InlineData("marcador-invalido")]
    public void PerfilDemoInvalidoNoRetrocedeAlPerfilPersonal(string caso)
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-demo-isolation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            if (caso == "marcador-invalido") File.WriteAllText(Path.Combine(root, ".steelog-demo"), "otro-directorio");
            var error = Assert.Throws<TargetInvocationException>(() => Create(root, caso.EndsWith("relativa", StringComparison.Ordinal) ? caso : root));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public void PerfilDemoValidoUsaRaizInyectadaSinCargarSesionNiConfiguracion()
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-demo-isolation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, ".steelog-demo"), "steelog-demo-v1");
            using (var services = (IDisposable)Create(root, root))
            {
                var paths = services.GetType().GetProperty("Paths", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services);
                Assert.Equal(Path.Combine(root, "SteeLog", "Storage"), paths.GetType().GetProperty("StorageDirectory").GetValue(paths));
                Assert.Equal(Path.Combine(root, "SteeLog", "Reportes"), services.GetType().GetProperty("ReportsDirectory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services));
                Assert.False(File.Exists(Path.Combine(root, "SteeLog", "session.dat")));
                Assert.False(File.Exists(Path.Combine(root, "appsettings.json")));
            }
        }
        finally { Directory.Delete(root, true); }
    }
}
