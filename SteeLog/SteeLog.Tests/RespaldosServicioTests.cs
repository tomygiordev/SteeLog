using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Respaldos;
using SteeLog.Infrastructure.Respaldos;
using Xunit;

/// <summary>Servicio de copias de seguridad con motor y base falsos (sin PostgreSQL ni procesos) sobre una carpeta temporal real.</summary>
public sealed class RespaldosServicioTests : IDisposable
{
    // ---------- dobles ----------
    sealed class MotorFalso : IMotorRespaldo
    {
        public List<string> Llamadas { get; } = new List<string>();
        public EstadoHerramientas Herramientas = new EstadoHerramientas { Disponibles = true, Version = "18.0" };
        public Func<string, CancellationToken, Task<ResultadoMotor>> AlCrear;
        public bool ListarOk = true; public int Objetos = 12; public bool RestaurarOk = true; public string DetalleRestaurar = "falló";
        public Task<EstadoHerramientas> DetectarAsync(CancellationToken token) => Task.FromResult(Herramientas);
        public async Task<ResultadoMotor> CrearAsync(string destino, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        {
            Llamadas.Add("crear");
            if (AlCrear != null) return await AlCrear(destino, token);
            File.WriteAllBytes(destino, new byte[] { 1, 2, 3, 4, 5 });
            return new ResultadoMotor { Ok = true };
        }
        public Task<ResultadoMotor> ListarAsync(string archivo, CancellationToken token)
        { Llamadas.Add("listar"); return Task.FromResult(new ResultadoMotor { Ok = ListarOk, Objetos = ListarOk ? Objetos : 0, Detalle = ListarOk ? null : "formato inválido" }); }
        public Task<ResultadoMotor> RestaurarAsync(string archivo, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        { Llamadas.Add("restaurar"); return Task.FromResult(new ResultadoMotor { Ok = RestaurarOk, Detalle = RestaurarOk ? null : DetalleRestaurar }); }
    }

    sealed class BaseFalsa : IBaseRespaldo
    {
        public List<string> Llamadas;
        public bool Conectada = true;
        public Task<InfoBase> ConsultarAsync(CancellationToken token) => Task.FromResult(Conectada
            ? new InfoBase { Conectada = true, Nombre = "inventario_db", Version = "17.2", VersionMayor = 17 }
            : new InfoBase { Conectada = false, Mensaje = "No se pudo conectar a la base de datos: sin red" });
        public Task<IReadOnlyDictionary<string, long>> ContarFilasAsync(IReadOnlyList<string> tablas, CancellationToken token)
            => Task.FromResult<IReadOnlyDictionary<string, long>>(tablas.ToDictionary(t => t, t => (long)t.Length));
        public void LiberarConexiones() => Llamadas?.Add("liberar");
    }

    sealed class ConfiguracionMemoria : IConfiguracionRespaldos
    {
        public ConfiguracionRespaldo Actual;
        public ConfiguracionRespaldo Cargar() => Actual.Clonar();
        public void Guardar(ConfiguracionRespaldo c) { Actual = c.Clonar(); }
    }

    sealed class BitacoraFalsa : IRegistroErroresBitacora
    {
        public List<string> Eventos { get; } = new List<string>();
        public List<string> Orden;
        public void Error(string d, string e = null) { }
        public void ErrorNoControlado(object error, string e = null) { }
        public void Evento(string accion, string descripcion, string entidad = null) { Eventos.Add(accion + "|" + descripcion); Orden?.Add("evento:" + accion); }
        public void ReintentarPendientes() { }
    }

    // ---------- fixture ----------
    readonly string raiz = Path.Combine(Path.GetTempPath(), "steelog-resp-" + Guid.NewGuid().ToString("N"));
    readonly string carpeta;
    readonly MotorFalso motor = new MotorFalso();
    readonly BaseFalsa bd = new BaseFalsa();
    readonly ConfiguracionMemoria config = new ConfiguracionMemoria();
    readonly BitacoraFalsa bitacora = new BitacoraFalsa();
    readonly AlmacenArchivosRespaldos almacen = new AlmacenArchivosRespaldos();
    readonly Session sesion = new Session();
    readonly List<string> orden = new List<string>();
    DateTimeOffset reloj = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

    public RespaldosServicioTests()
    {
        carpeta = Path.Combine(raiz, "Copias");
        Directory.CreateDirectory(raiz);
        config.Actual = new ConfiguracionRespaldo { Carpeta = carpeta, AutomaticoActivo = true, HorasEntreAutomaticos = 24, Conservar = 3 };
        sesion.Start(new Identity("admin@steelog", Role.Admin));
        motor.Llamadas.Clear(); bd.Llamadas = orden; bitacora.Orden = orden;
    }
    public void Dispose() { try { Directory.Delete(raiz, true); } catch { } }

    ServicioRespaldos Servicio(string raizPermitida = null) => new ServicioRespaldos(sesion, config, almacen, motor, bd, bitacora, () => reloj, "9.9.9", raizPermitida);
    static readonly CancellationToken Nada = CancellationToken.None;
    string[] Archivos() => Directory.Exists(carpeta) ? Directory.GetFileSystemEntries(carpeta).Select(Path.GetFileName).OrderBy(x => x).ToArray() : new string[0];

    /// <summary>Copia con ficha puesta a mano en la carpeta (para armar escenarios).</summary>
    string Sembrar(TipoRespaldo tipo, DateTimeOffset fecha, bool conFicha = true, string contenido = "datos")
    {
        Directory.CreateDirectory(carpeta);
        var nombre = NombresRespaldo.Nombre(fecha.ToLocalTime().DateTime, tipo);
        var ruta = Path.Combine(carpeta, nombre);
        File.WriteAllText(ruta, contenido);
        File.SetLastWriteTimeUtc(ruta, fecha.UtcDateTime);
        if (conFicha)
            almacen.EscribirFicha(carpeta, nombre, new FichaRespaldo { FechaUtc = fecha.ToUniversalTime(), Tipo = tipo, TamanoBytes = new FileInfo(ruta).Length, Sha256 = almacen.CalcularSha256(ruta, Nada), Usuario = "x" });
        return nombre;
    }

    // ---------- permisos ----------
    [Fact]
    public async Task TodasLasOperacionesExigenAdminSalvoElAutomatico()
    {
        foreach (var rol in new Role?[] { null, Role.Galpon })
        {
            var s = new Session(); if (rol != null) s.Start(new Identity("galpon@steelog", rol.Value));
            var sv = new ServicioRespaldos(s, config, almacen, motor, bd, bitacora, () => reloj, "1");
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.ObtenerEstadoAsync(Nada));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.CrearAsync(null, null, Nada));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.VerificarAsync("a.dump", Nada));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.RestaurarAsync("a.dump", null, Nada));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.ImportarAsync("a.dump", Nada));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.EliminarAsync("a.dump", Nada));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sv.GuardarConfiguracionAsync(config.Actual, Nada));
        }
        Assert.Empty(motor.Llamadas);
        Assert.False(Directory.Exists(carpeta));
        // El automático no pide Admin: un usuario de galpón con sesión lo dispara.
        var galpon = new Session(); galpon.Start(new Identity("galpon@steelog", Role.Galpon));
        var r = await new ServicioRespaldos(galpon, config, almacen, motor, bd, bitacora, () => reloj, "1").EjecutarSiCorrespondeAsync(Nada);
        Assert.True(r.Ejecutado && r.Ok, r.Mensaje);
    }

    // ---------- crear ----------
    [Fact]
    public async Task CrearGeneraDumpFichaConShaYRegistraEnBitacora()
    {
        var r = await Servicio().CrearAsync("  antes del cierre  ", null, Nada);
        Assert.True(r.Ok, r.Detalle);
        Assert.Equal("steelog_" + reloj.ToLocalTime().DateTime.ToString("yyyyMMdd_HHmmss") + "_manual.dump", r.Respaldo.NombreArchivo);
        Assert.Equal(new[] { r.Respaldo.NombreArchivo, Path.GetFileNameWithoutExtension(r.Respaldo.NombreArchivo) + ".json" }.OrderBy(x => x), Archivos());
        var ruta = Path.Combine(carpeta, r.Respaldo.NombreArchivo);
        string esperado; using (var sha = SHA256.Create()) esperado = string.Concat(sha.ComputeHash(File.ReadAllBytes(ruta)).Select(b => b.ToString("x2")));
        var info = r.Respaldo;
        Assert.Equal(esperado, info.Sha256);
        Assert.Equal(TipoRespaldo.Manual, info.Tipo); Assert.Equal("admin@steelog", info.Usuario); Assert.Equal("9.9.9", info.VersionApp);
        Assert.Equal("17.2", info.VersionServidor); Assert.Equal("antes del cierre", info.Nota); Assert.Equal(5, info.TamanoBytes); Assert.True(info.TieneFicha);
        Assert.Equal(new[] { "herramientas", "materiales", "movimientos", "obras" }, info.ResumenFilas.Keys.OrderBy(x => x));
        var json = File.ReadAllText(Path.Combine(carpeta, Path.GetFileNameWithoutExtension(info.NombreArchivo) + ".json"));
        Assert.Contains("\"sha256\"", json); Assert.Contains("\"base\"", json); Assert.Contains("inventario_db", json); Assert.Contains("\"materiales\"", json);
        var evento = Assert.Single(bitacora.Eventos);
        Assert.StartsWith("RESPALDO|", evento); Assert.Contains(info.NombreArchivo, evento); Assert.Contains("Manual", evento);
        // Una segunda copia en el mismo segundo no pisa a la primera.
        var r2 = await Servicio().CrearAsync(null, null, Nada);
        Assert.True(r2.Ok); Assert.NotEqual(r.Respaldo.NombreArchivo, r2.Respaldo.NombreArchivo); Assert.True(File.Exists(ruta));
    }

    [Fact]
    public async Task CrearSinHerramientasOSinBaseInformaYNoDejaArchivos()
    {
        motor.Herramientas = new EstadoHerramientas { Disponibles = false, Mensaje = "No se encontraron pg_dump y pg_restore." };
        var r = await Servicio().CrearAsync(null, null, Nada);
        Assert.False(r.Ok); Assert.Contains("No se encontraron pg_dump", r.Detalle);
        motor.Herramientas = new EstadoHerramientas { Disponibles = true, Version = "18.0" }; bd.Conectada = false;
        r = await Servicio().CrearAsync(null, null, Nada);
        Assert.False(r.Ok); Assert.Contains("No se pudo conectar", r.Detalle);
        Assert.DoesNotContain(Archivos(), a => a.EndsWith(".dump") || a.EndsWith(".tmp") || a.EndsWith(".json"));
        Assert.Equal(2, bitacora.Eventos.Count(e => e.StartsWith("RESPALDO_ERROR|")));
        Assert.DoesNotContain("crear", motor.Llamadas);
    }

    [Fact]
    public async Task SiPgDumpFallaSeBorraElArchivoParcial()
    {
        string parcial = null;
        motor.AlCrear = (destino, t) => { parcial = destino; File.WriteAllBytes(destino, new byte[100]); return Task.FromResult(new ResultadoMotor { Ok = false, Detalle = "pg_dump falló (código 1)." }); };
        var r = await Servicio().CrearAsync(null, null, Nada);
        Assert.False(r.Ok); Assert.Contains("pg_dump falló", r.Detalle);
        Assert.NotNull(parcial); Assert.False(File.Exists(parcial));
        Assert.DoesNotContain(Archivos(), a => a.EndsWith(".dump") || a.EndsWith(".tmp") || a.EndsWith(".json"));
        Assert.Contains(bitacora.Eventos, e => e.StartsWith("RESPALDO_ERROR|") && e.Contains("Manual"));
    }

    [Fact]
    public async Task SiSeCancelaSeBorraElArchivoParcialYSeLiberaElBloqueo()
    {
        string parcial = null;
        using (var cts = new CancellationTokenSource())
        {
            motor.AlCrear = async (destino, t) => { parcial = destino; File.WriteAllBytes(destino, new byte[100]); cts.Cancel(); await Task.Delay(5000, t); return null; };
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Servicio().CrearAsync(null, null, cts.Token));
        }
        Assert.False(File.Exists(parcial));
        Assert.DoesNotContain(Archivos(), a => a.EndsWith(".dump") || a.EndsWith(".tmp") || a.EndsWith(".json"));
        motor.AlCrear = null;
        Assert.True((await Servicio().CrearAsync(null, null, Nada)).Ok);
    }

    [Fact]
    public async Task DosCopiasSimultaneasNoSePisanElSegundoRecibeMensajeDeOcupado()
    {
        var entro = new TaskCompletionSource<bool>(); var seguir = new TaskCompletionSource<bool>();
        motor.AlCrear = async (destino, t) => { File.WriteAllBytes(destino, new byte[3]); entro.SetResult(true); await seguir.Task; return new ResultadoMotor { Ok = true }; };
        var primera = Servicio().CrearAsync(null, null, Nada);
        await entro.Task;
        var segunda = await Servicio().CrearAsync(null, null, Nada);
        Assert.False(segunda.Ok); Assert.Equal(ServicioRespaldos.MensajeOcupado, segunda.Detalle);
        seguir.SetResult(true);
        Assert.True((await primera).Ok);
        Assert.Single(Archivos().Where(a => a.EndsWith(".dump")));
    }

    // ---------- retención ----------
    [Fact]
    public async Task LaRetencionConservaLasUltimasAutomaticasYNuncaTocaOtrosTipos()
    {
        var ahora = reloj;
        var autos = Enumerable.Range(1, 5).Select(i => Sembrar(TipoRespaldo.Automatico, ahora.AddDays(-10 + i))).ToList(); // más viejas que 24 h
        var manual = Sembrar(TipoRespaldo.Manual, ahora.AddDays(-20));
        var pre = Sembrar(TipoRespaldo.PreRestauracion, ahora.AddDays(-21));
        var importada = Sembrar(TipoRespaldo.Importado, ahora.AddDays(-22));
        var sinFicha = Sembrar(TipoRespaldo.Automatico, ahora.AddDays(-30), conFicha: false);
        config.Actual.Conservar = 3;
        var r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.True(r.Ejecutado && r.Ok, r.Mensaje);
        var presentes = Archivos().Where(a => a.EndsWith(".dump")).ToList();
        // Quedan: la nueva + las 2 automáticas más recientes con ficha; las 3 más viejas se borran (y sus fichas).
        Assert.Contains(r.Respaldo.NombreArchivo, presentes);
        Assert.Contains(autos[4], presentes); Assert.Contains(autos[3], presentes);
        Assert.DoesNotContain(autos[2], presentes); Assert.DoesNotContain(autos[1], presentes); Assert.DoesNotContain(autos[0], presentes);
        Assert.DoesNotContain(Archivos(), a => a.StartsWith(Path.GetFileNameWithoutExtension(autos[0])));
        foreach (var intacto in new[] { manual, pre, importada, sinFicha }) Assert.Contains(intacto, presentes);
        Assert.Contains("eliminaron 3", r.Mensaje);
    }

    [Fact]
    public async Task LaCopiaManualNoAplicaRetencion()
    {
        for (var i = 1; i <= 5; i++) Sembrar(TipoRespaldo.Automatico, reloj.AddDays(-i));
        config.Actual.Conservar = 1;
        Assert.True((await Servicio().CrearAsync(null, null, Nada)).Ok);
        Assert.Equal(6, Archivos().Count(a => a.EndsWith(".dump")));
    }

    // ---------- ids y rutas ----------
    [Theory]
    [InlineData(@"..\afuera.dump")]
    [InlineData("../afuera.dump")]
    [InlineData(@"sub\x.dump")]
    [InlineData(@"C:\Windows\x.dump")]
    [InlineData("x.txt")]
    [InlineData(".dump")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    [InlineData("a..b.dump")]
    [InlineData("x.dump ")]
    public async Task LosIdInvalidosNuncaSeTratanComoRutas(string id)
    {
        File.WriteAllText(Path.Combine(raiz, "afuera.dump"), "no tocar");
        Directory.CreateDirectory(carpeta);
        Assert.False(NombresRespaldo.IdValido(id));
        Assert.Null(almacen.RutaDe(carpeta, id));
        var sv = Servicio();
        Assert.False((await sv.EliminarAsync(id, Nada)).Ok);
        Assert.False((await sv.VerificarAsync(id, Nada)).Ok);
        Assert.False((await sv.RestaurarAsync(id, null, Nada)).Ok);
        Assert.Equal("no tocar", File.ReadAllText(Path.Combine(raiz, "afuera.dump")));
        Assert.DoesNotContain("restaurar", motor.Llamadas);
    }

    [Fact]
    public async Task EliminarBorraDumpYFichaYRegistra()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj);
        var r = await Servicio().EliminarAsync(n, Nada);
        Assert.True(r.Ok, r.Detalle);
        Assert.DoesNotContain(Archivos(), a => a.StartsWith(Path.GetFileNameWithoutExtension(n)));
        Assert.Contains(bitacora.Eventos, e => e.StartsWith("RESPALDO|") && e.Contains(n));
        Assert.False((await Servicio().EliminarAsync(n, Nada)).Ok);
    }

    // ---------- verificar ----------
    [Fact]
    public async Task VerificarCompruebaShaYLista()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj);
        var v = await Servicio().VerificarAsync(n, Nada);
        Assert.True(v.Ok, v.Detalle); Assert.Equal(12, v.Objetos); Assert.Contains("SHA-256", v.Detalle);
        File.AppendAllText(Path.Combine(carpeta, n), "alterado");
        v = await Servicio().VerificarAsync(n, Nada);
        Assert.False(v.Ok); Assert.Contains("SHA-256", v.Detalle);
    }

    [Fact]
    public async Task VerificarSinFichaSoloUsaPgRestoreList()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj, conFicha: false);
        var v = await Servicio().VerificarAsync(n, Nada);
        Assert.True(v.Ok); Assert.Contains("Sin ficha", v.Detalle);
        motor.ListarOk = false;
        Assert.False((await Servicio().VerificarAsync(n, Nada)).Ok);
    }

    // ---------- restaurar ----------
    [Fact]
    public async Task RestaurarCreaPrimeroLaPreRestauracionYRegistraDespues()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj.AddDays(-3));
        var r = await Servicio().RestaurarAsync(n, null, Nada);
        Assert.True(r.Ok, r.Detalle);
        Assert.NotNull(r.RespaldoPrevio); Assert.Equal(TipoRespaldo.PreRestauracion, r.RespaldoPrevio.Tipo);
        Assert.True(File.Exists(Path.Combine(carpeta, r.RespaldoPrevio.NombreArchivo)));
        // Orden: listar (verificación), crear (previa), restaurar.
        Assert.Equal(new[] { "listar", "crear", "restaurar" }, motor.Llamadas);
        // Las conexiones agrupadas se liberan antes de restaurar y el evento de bitácora se escribe DESPUÉS.
        var iRestaurado = orden.IndexOf("evento:RESTAURACION");
        Assert.True(iRestaurado > orden.LastIndexOf("liberar"));
        Assert.True(iRestaurado > orden.IndexOf("evento:RESPALDO"));
        Assert.Contains(bitacora.Eventos, e => e.StartsWith("RESTAURACION|") && e.Contains(n) && e.Contains(r.RespaldoPrevio.NombreArchivo));
    }

    [Fact]
    public async Task RestaurarAbortaSinTocarNadaSiFallaLaCopiaPrevia()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj.AddDays(-3));
        motor.AlCrear = (d, t) => Task.FromResult(new ResultadoMotor { Ok = false, Detalle = "disco lleno" });
        var r = await Servicio().RestaurarAsync(n, null, Nada);
        Assert.False(r.Ok); Assert.Null(r.RespaldoPrevio); Assert.Contains("disco lleno", r.Detalle); Assert.Contains("No se restauró nada", r.Detalle);
        Assert.DoesNotContain("restaurar", motor.Llamadas);
        Assert.DoesNotContain("liberar", orden);
        Assert.Contains(bitacora.Eventos, e => e.StartsWith("RESPALDO_ERROR|"));
        Assert.DoesNotContain(bitacora.Eventos, e => e.StartsWith("RESTAURACION|"));
    }

    [Fact]
    public async Task RestaurarUnaCopiaAlteradaNoRestauraNiCreaPrevia()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj.AddDays(-3));
        File.AppendAllText(Path.Combine(carpeta, n), "bytes de más");
        var r = await Servicio().RestaurarAsync(n, null, Nada);
        Assert.False(r.Ok); Assert.Contains("verificación", r.Detalle);
        Assert.DoesNotContain("restaurar", motor.Llamadas); Assert.DoesNotContain("crear", motor.Llamadas);
        Assert.Single(Archivos().Where(a => a.EndsWith(".dump")));
    }

    [Fact]
    public async Task SiPgRestoreFallaSeInformaQueLaTransaccionDejoLaBaseComoEstaba()
    {
        var n = Sembrar(TipoRespaldo.Manual, reloj.AddDays(-3));
        motor.RestaurarOk = false; motor.DetalleRestaurar = "Hay otras sesiones conectadas que bloquean tablas.";
        var r = await Servicio().RestaurarAsync(n, null, Nada);
        Assert.False(r.Ok); Assert.Contains("única transacción", r.Detalle); Assert.Contains("otras sesiones", r.Detalle);
        Assert.NotNull(r.RespaldoPrevio); // la previa existe y queda disponible
        Assert.DoesNotContain(bitacora.Eventos, e => e.StartsWith("RESTAURACION|"));
        Assert.Contains(bitacora.Eventos, e => e.StartsWith("RESPALDO_ERROR|"));
    }

    // ---------- importar ----------
    [Fact]
    public async Task ImportarCopiaElArchivoYCreaFichaImportado()
    {
        var externo = Path.Combine(raiz, "viejo.backup"); File.WriteAllBytes(externo, new byte[] { 9, 9, 9 });
        var r = await Servicio().ImportarAsync(externo, Nada);
        Assert.True(r.Ok, r.Detalle);
        Assert.Equal(TipoRespaldo.Importado, r.Respaldo.Tipo); Assert.EndsWith("_importado.dump", r.Respaldo.NombreArchivo); Assert.Contains("viejo.backup", r.Respaldo.Nota);
        Assert.True(File.Exists(externo));
        Assert.Equal(new byte[] { 9, 9, 9 }, File.ReadAllBytes(Path.Combine(carpeta, r.Respaldo.NombreArchivo)));
        Assert.Contains(motor.Llamadas, l => l == "listar");
    }

    [Fact]
    public async Task ImportarUnArchivoInvalidoNoDejaNada()
    {
        var externo = Path.Combine(raiz, "basura.txt"); File.WriteAllText(externo, "no soy un dump");
        motor.ListarOk = false;
        var r = await Servicio().ImportarAsync(externo, Nada);
        Assert.False(r.Ok); Assert.Contains("no es una copia válida", r.Detalle);
        Assert.DoesNotContain(Archivos(), a => a.EndsWith(".dump") || a.EndsWith(".tmp") || a.EndsWith(".json"));
        Assert.False((await Servicio().ImportarAsync(Path.Combine(raiz, "no-existe.dump"), Nada)).Ok);
    }

    // ---------- automático ----------
    [Fact]
    public async Task ElAutomaticoCorreSoloSiEstaActivoYLaUltimaEsVieja()
    {
        // Sin copias: corre.
        var r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.True(r.Ejecutado && r.Ok); Assert.Equal(TipoRespaldo.Automatico, r.Respaldo.Tipo);
        // Recién hecha: no corre.
        reloj = reloj.AddHours(23);
        r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.False(r.Ejecutado); Assert.True(r.Ok); Assert.Contains("Todavía no corresponde", r.Mensaje);
        // Pasadas las 24 h: corre de nuevo.
        reloj = reloj.AddHours(2);
        r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.True(r.Ejecutado && r.Ok);
        Assert.Equal(2, Archivos().Count(a => a.EndsWith(".dump")));
        // Cualquier copia reciente (aunque sea manual) posterga el automático.
        reloj = reloj.AddHours(1);
        Assert.True((await Servicio().CrearAsync(null, null, Nada)).Ok);
        reloj = reloj.AddHours(23);
        Assert.False((await Servicio().EjecutarSiCorrespondeAsync(Nada)).Ejecutado);
        // Desactivado: nunca corre.
        config.Actual.AutomaticoActivo = false; reloj = reloj.AddDays(10);
        var d = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.False(d.Ejecutado); Assert.Contains("desactivadas", d.Mensaje);
    }

    [Fact]
    public async Task ElAutomaticoNoCorreSinSesionNiConOtraOperacionEnCurso()
    {
        var sinSesion = new ServicioRespaldos(new Session(), config, almacen, motor, bd, bitacora, () => reloj, "1");
        var r = await sinSesion.EjecutarSiCorrespondeAsync(Nada);
        Assert.False(r.Ejecutado); Assert.Contains("sesión", r.Mensaje);
        using (almacen.Bloquear(carpeta))
        {
            r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
            Assert.False(r.Ejecutado); Assert.Equal(ServicioRespaldos.MensajeOcupado, r.Mensaje);
        }
        Assert.Empty(motor.Llamadas);
    }

    [Fact]
    public async Task ElAutomaticoNuncaLanzaAunqueTodoFalle()
    {
        motor.AlCrear = (d, t) => throw new InvalidOperationException("explotó");
        var r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.True(r.Ejecutado); Assert.False(r.Ok); Assert.Contains("explotó", r.Mensaje);
        bd.Conectada = false; motor.AlCrear = null;
        r = await Servicio().EjecutarSiCorrespondeAsync(Nada);
        Assert.False(r.Ok);
        Assert.DoesNotContain(Archivos(), a => a.EndsWith(".tmp") || a.EndsWith(".dump"));
        // Cancelado: devuelve resultado en vez de lanzar.
        using (var cts = new CancellationTokenSource())
        {
            bd.Conectada = true; motor.AlCrear = async (d, t) => { cts.Cancel(); await Task.Delay(5000, t); return null; };
            r = await Servicio().EjecutarSiCorrespondeAsync(cts.Token);
            Assert.False(r.Ejecutado);
        }
    }

    // ---------- estado y configuración ----------
    [Fact]
    public async Task EstadoListaMasNuevosPrimeroYCalculaProximoAutomaticoYEspacio()
    {
        var vieja = Sembrar(TipoRespaldo.Manual, reloj.AddDays(-5));
        var nueva = Sembrar(TipoRespaldo.Automatico, reloj.AddHours(-2));
        var manoSinFicha = Sembrar(TipoRespaldo.Manual, reloj.AddDays(-9), conFicha: false);
        var e = await Servicio().ObtenerEstadoAsync(Nada);
        Assert.Equal(new[] { nueva, vieja, manoSinFicha }, e.Respaldos.Select(r => r.NombreArchivo));
        Assert.Equal(nueva, e.Ultimo.NombreArchivo);
        Assert.False(e.Respaldos.Single(r => r.NombreArchivo == manoSinFicha).TieneFicha);
        Assert.Equal(reloj.AddHours(-2).ToLocalTime().DateTime.AddHours(24), e.ProximoAutomatico);
        Assert.True(e.HerramientasDisponibles && e.BaseConectada); Assert.Equal("17.2", e.VersionServidor); Assert.Equal("18.0", e.VersionHerramientas);
        Assert.Equal(e.Respaldos.Sum(r => r.TamanoBytes), e.EspacioUsadoBytes);
        Assert.True(e.EspacioLibreBytes > 0);
        config.Actual.AutomaticoActivo = false; bd.Conectada = false;
        e = await Servicio().ObtenerEstadoAsync(Nada);
        Assert.Null(e.ProximoAutomatico); Assert.False(e.BaseConectada); Assert.Contains("sin red", e.MensajeBase);
    }

    [Fact]
    public async Task EstadoConCarpetaInexistenteNoLanzaNiLaCrea()
    {
        var e = await Servicio().ObtenerEstadoAsync(Nada);
        Assert.Empty(e.Respaldos); Assert.Null(e.Ultimo); Assert.False(Directory.Exists(carpeta));
        Assert.Equal(reloj.ToLocalTime().DateTime, e.ProximoAutomatico);
    }

    [Theory]
    [InlineData("relativa\\carpeta", 24, 10)]
    [InlineData("", 24, 10)]
    [InlineData("X", 0, 10)]
    [InlineData("X", 24, 0)]
    [InlineData("X", 24, 5000)]
    [InlineData("X", 999999, 10)]
    public async Task GuardarConfiguracionRechazaValoresInvalidos(string carpetaTexto, int horas, int conservar)
    {
        var antes = config.Actual.Clonar();
        var c = new ConfiguracionRespaldo { Carpeta = carpetaTexto == "X" ? Path.Combine(raiz, "otra") : carpetaTexto, HorasEntreAutomaticos = horas, Conservar = conservar };
        var r = await Servicio().GuardarConfiguracionAsync(c, Nada);
        Assert.False(r.Ok); Assert.Equal(antes.Carpeta, config.Actual.Carpeta); Assert.Equal(antes.Conservar, config.Actual.Conservar);
    }

    [Fact]
    public async Task GuardarConfiguracionCreaLaCarpetaYEnDemoNoSaleDeLaRaiz()
    {
        var nueva = Path.Combine(raiz, "demo", "SteeLog", "Copias");
        var c = new ConfiguracionRespaldo { Carpeta = nueva, AutomaticoActivo = false, HorasEntreAutomaticos = 6, Conservar = 4 };
        var r = await Servicio(Path.Combine(raiz, "demo")).GuardarConfiguracionAsync(c, Nada);
        Assert.True(r.Ok, r.Detalle); Assert.True(Directory.Exists(nueva));
        Assert.False(config.Actual.AutomaticoActivo); Assert.Equal(6, config.Actual.HorasEntreAutomaticos);
        foreach (var fuera in new[] { Path.Combine(raiz, "otra"), Path.Combine(raiz, "demo", "..", "escape"), Path.Combine(raiz, "demo-hermana") })
        {
            r = await Servicio(Path.Combine(raiz, "demo")).GuardarConfiguracionAsync(new ConfiguracionRespaldo { Carpeta = fuera }, Nada);
            Assert.False(r.Ok, fuera); Assert.Contains("demo", r.Detalle);
            Assert.False(Directory.Exists(Path.GetFullPath(fuera)));
        }
        Assert.Equal(nueva, config.Actual.Carpeta);
    }

    // ---------- nombres ----------
    [Fact]
    public void NombresDeArchivoYTipoDesdeNombre()
    {
        var f = new DateTime(2026, 11, 14, 9, 5, 7);
        Assert.Equal("steelog_20261114_090507_prerestauracion.dump", NombresRespaldo.Nombre(f, TipoRespaldo.PreRestauracion));
        Assert.Equal("steelog_20261114_090507_manual_2.dump", NombresRespaldo.Nombre(f, TipoRespaldo.Manual, 2));
        Assert.Equal("steelog_20261114_090507_manual_2.json", NombresRespaldo.NombreFicha("steelog_20261114_090507_manual_2.dump"));
        Assert.Equal(TipoRespaldo.Automatico, NombresRespaldo.TipoDesdeNombre("steelog_20261114_090507_automatico.dump"));
        Assert.Equal(TipoRespaldo.Importado, NombresRespaldo.TipoDesdeNombre("steelog_20261114_090507_importado_3.dump"));
        Assert.Null(NombresRespaldo.TipoDesdeNombre("mi copia.dump"));
    }
}
