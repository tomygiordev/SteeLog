using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SteeLog.Servicios;
using Xunit;

public sealed class RegistroErroresTests : IDisposable
{
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "steelog-errores-" + Guid.NewGuid().ToString("N"));
    private string Archivo => Path.Combine(carpeta, "logs", "errores.log");
    private RegistroErrores Crear() => new RegistroErrores(() => carpeta);

    [Fact]
    public void CreaCarpetaYAgregaExcepcionesSinSobrescribir()
    {
        var registro = Crear();
        registro.Registrar(new InvalidOperationException("primero", new Exception("detalle interno")));
        registro.Registrar(new Exception("segundo"));
        var texto = File.ReadAllText(Archivo);
        Assert.Contains("InvalidOperationException", texto);
        Assert.Contains("primero", texto);
        Assert.Contains("detalle interno", texto);
        Assert.Contains("segundo", texto);
        Assert.True(DateTimeOffset.TryParse(texto.Split(' ')[0], out _));
    }

    [Theory]
    [InlineData("Password=secreto;", "secreto")]
    [InlineData("pWd = secreto;", "secreto")]
    [InlineData("PASSWORD=\"clave;oculta\";", "oculta")]
    [InlineData("Pwd='clave;oculta';", "oculta")]
    [InlineData("Password=sinSeparador", "sinSeparador")]
    [InlineData("Host=servidor;Database=base;Username=usuario;Password=secreto;", "servidor")]
    public void SaneaCredencialesYDatosDeConexion(string mensaje, string secreto)
    {
        Crear().Registrar(new Exception(mensaje));
        var texto = File.ReadAllText(Archivo);
        Assert.DoesNotContain(secreto, texto);
        Assert.Contains("[omitido]", texto);
        Assert.DoesNotContain("Password=secreto", texto);
    }

    [Fact]
    public void SerializaEscriturasInclusoEntreInstancias()
    {
        Parallel.For(0, 200, i => Crear().Registrar("entrada-" + i));
        var lineas = File.ReadAllLines(Archivo);
        Assert.Equal(200, lineas.Length);
        Assert.Equal(200, lineas.Select(l => l.Substring(l.IndexOf(' ') + 1)).Distinct().Count());
    }

    [Fact]
    public void RotaAlSuperarUnMegabyteYConservaUnSoloRespaldo()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Archivo));
        File.WriteAllText(Archivo, new string('x', 1024 * 1024 + 1));
        File.WriteAllText(Archivo + ".1", "respaldo viejo");
        Crear().Registrar("nuevo");
        Assert.True(new FileInfo(Archivo + ".1").Length > 1024 * 1024);
        Assert.Contains("nuevo", File.ReadAllText(Archivo));
        Assert.DoesNotContain("xxx", File.ReadAllText(Archivo));
        Assert.Equal(2, Directory.GetFiles(Path.GetDirectoryName(Archivo)).Length);
    }

    [Fact]
    public void NoLanzaSiLaRutaNoEsUnDirectorio()
    {
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(Path.Combine(carpeta, "logs"), "obstaculo");
        Assert.Null(Record.Exception(() => Crear().Registrar(new Exception("error"))));
    }

    [Fact]
    public void NoLanzaSiFallaLaResolucionDeLaCarpeta()
    {
        var registro = new RegistroErrores(() => { throw new IOException("sin ruta"); });
        Assert.Null(Record.Exception(() => registro.Registrar(new Exception("error"))));
    }

    [Fact]
    public void NoLanzaSiFallaToStringONoHayDetalle()
    {
        Assert.Null(Record.Exception(() => Crear().Registrar(new ErrorNoConvertible())));
        Crear().Registrar(null);
        Assert.Contains("Error sin detalle", File.ReadAllText(Archivo));
    }

    private sealed class ErrorNoConvertible
    {
        public override string ToString() { throw new InvalidOperationException(); }
    }

    public void Dispose()
    {
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
    }
}
