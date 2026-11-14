using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Respaldos;
using SteeLog.Infrastructure.Configuration;
using SteeLog.Infrastructure.Respaldos;
using Xunit;

/// <summary>Motor (pg_dump / pg_restore) con ejecutor falso, localizador de herramientas, almacén, configuración y ejecutor real.</summary>
public sealed class RespaldosInfraestructuraTests : IDisposable
{
    const string Cadena = "Server=db.local;Port=5435;Database=inventario_db;User Id=steelog;Password=Cl4ve Secreta;Pooling=true;";
    readonly string raiz = Path.Combine(Path.GetTempPath(), "steelog-resp-infra-" + Guid.NewGuid().ToString("N"));
    public RespaldosInfraestructuraTests() { Directory.CreateDirectory(raiz); }
    public void Dispose() { try { Directory.Delete(raiz, true); } catch { } }

    sealed class EjecutorFalso : IEjecutorProcesos
    {
        public List<SolicitudProceso> Solicitudes { get; } = new List<SolicitudProceso>();
        public Func<SolicitudProceso, ResultadoProceso> Respuesta = s => new ResultadoProceso();
        public Func<SolicitudProceso, CancellationToken, Task<ResultadoProceso>> Asincrona;
        public string VersionPgDump = "pg_dump (PostgreSQL) 18.0";
        public Task<ResultadoProceso> EjecutarAsync(SolicitudProceso s, CancellationToken token)
        {
            Solicitudes.Add(s);
            if (s.Argumentos.Count == 1 && s.Argumentos[0] == "--version") return Task.FromResult(new ResultadoProceso { Salida = VersionPgDump + "\r\n" });
            if (Asincrona != null) return Asincrona(s, token);
            return Task.FromResult(Respuesta(s));
        }
    }
    sealed class BaseFalsa : IBaseRespaldo
    {
        public int VersionMayor = 17; public bool EsperaBloqueo;
        public Task<InfoBase> ConsultarAsync(CancellationToken t) => Task.FromResult(new InfoBase { Conectada = true, VersionMayor = VersionMayor, Version = VersionMayor + ".2", Nombre = "inventario_db" });
        public Task<IReadOnlyDictionary<string, long>> ContarFilasAsync(IReadOnlyList<string> tablas, CancellationToken t) => Task.FromResult<IReadOnlyDictionary<string, long>>(new Dictionary<string, long>());
        public Task<bool> RestauracionEsperaBloqueoAsync(CancellationToken t) => Task.FromResult(EsperaBloqueo);
        public void LiberarConexiones() { }
    }

    MotorPostgresRespaldo Motor(EjecutorFalso ej, int versionServidor = 17, bool esperaBloqueo = false, int esperaMs = 20000)
        => new MotorPostgresRespaldo(ej, () => Cadena, new LocalizadorHerramientas(ej, null, @"C:\App", @"C:\Program Files", f => true, d => new[] { @"C:\Program Files\PostgreSQL\18" }),
            new BaseFalsa { VersionMayor = versionServidor, EsperaBloqueo = esperaBloqueo }, esperaMs);

    // ---------- argumentos y contraseña ----------
    [Fact]
    public void LosDatosDeConexionSalenDeLaCadenaConfigurada()
    {
        var c = ConexionRespaldo.Desde(Cadena);
        Assert.Equal("db.local", c.Host); Assert.Equal(5435, c.Puerto); Assert.Equal("inventario_db", c.Base); Assert.Equal("steelog", c.Usuario); Assert.Equal("Cl4ve Secreta", c.Clave);
    }

    [Fact]
    public void LaContrasenaViajaSoloPorPGPASSWORDNuncaPorArgumentos()
    {
        var c = ConexionRespaldo.Desde(Cadena);
        var todos = MotorPostgresRespaldo.ArgumentosDump(c, @"C:\x\a.tmp").Concat(MotorPostgresRespaldo.ArgumentosRestore(c, @"C:\x\a.dump")).Concat(MotorPostgresRespaldo.ArgumentosListar(@"C:\x\a.dump")).ToList();
        Assert.DoesNotContain(todos, a => a.Contains("Cl4ve") || a.Contains("Secreta") || a.IndexOf("password=", StringComparison.OrdinalIgnoreCase) >= 0);
        Assert.Contains("--no-password", MotorPostgresRespaldo.ArgumentosDump(c, "x"));
        Assert.Equal("Cl4ve Secreta", MotorPostgresRespaldo.Entorno(c)["PGPASSWORD"]);
        Assert.Equal(new[] { "PGPASSWORD", "PGSSLMODE" }, MotorPostgresRespaldo.Entorno(c).Keys.OrderBy(k => k).ToArray());
    }

    [Fact]
    public void ArgumentosDeDumpYRestoreSonLosPedidos()
    {
        var c = ConexionRespaldo.Desde(Cadena);
        var dump = MotorPostgresRespaldo.ArgumentosDump(c, @"C:\copias\a.tmp");
        Assert.Contains("--format=custom", dump); Assert.Contains(@"--file=C:\copias\a.tmp", dump); Assert.Contains("--host=db.local", dump);
        Assert.Contains("--port=5435", dump); Assert.Contains("--username=steelog", dump); Assert.Contains("--dbname=inventario_db", dump);
        var restore = MotorPostgresRespaldo.ArgumentosRestore(c, @"C:\copias\a.dump");
        foreach (var f in new[] { "--clean", "--if-exists", "--no-owner", "--no-privileges", "--single-transaction", "--exit-on-error", "--dbname=inventario_db" }) Assert.Contains(f, restore);
        Assert.Equal(@"C:\copias\a.dump", restore.Last());
        Assert.Equal(new[] { "--list", @"C:\a b\x.dump" }, MotorPostgresRespaldo.ArgumentosListar(@"C:\a b\x.dump"));
    }

    [Fact]
    public async Task ElMotorEjecutaLasHerramientasConLaClaveEnElEntornoYSinPegarlaEnLaLinea()
    {
        var ej = new EjecutorFalso { Respuesta = s => new ResultadoProceso { CodigoSalida = 0 } };
        var motor = Motor(ej);
        var r = await motor.CrearAsync(@"C:\x\a.tmp", null, CancellationToken.None);
        Assert.True(r.Ok);
        var dump = ej.Solicitudes.Last();
        Assert.EndsWith("pg_dump.exe", dump.Ejecutable); Assert.Equal("Cl4ve Secreta", dump.Entorno["PGPASSWORD"]);
        Assert.DoesNotContain("Cl4ve", EjecutorProcesos.ArmarArgumentos(dump.Argumentos));
        r = await motor.RestaurarAsync(@"C:\x\a.dump", null, CancellationToken.None);
        Assert.True(r.Ok);
        var restore = ej.Solicitudes.Last();
        Assert.EndsWith("pg_restore.exe", restore.Ejecutable); Assert.Equal("Cl4ve Secreta", restore.Entorno["PGPASSWORD"]); Assert.DoesNotContain("Cl4ve", EjecutorProcesos.ArmarArgumentos(restore.Argumentos));
    }

    [Fact]
    public async Task ErroresDelMotorSeInformanSinCredencialesNiExcepcionCruda()
    {
        var ej = new EjecutorFalso { Respuesta = s => new ResultadoProceso { CodigoSalida = 1, Error = "pg_dump: processing x\r\npg_dump: error: connection to server failed: Host=db.local;Password=Cl4ve Secreta\r\n" } };
        var r = await Motor(ej).CrearAsync("x", null, CancellationToken.None);
        Assert.False(r.Ok); Assert.Contains("código 1", r.Detalle); Assert.DoesNotContain("Cl4ve", r.Detalle); Assert.DoesNotContain("db.local", r.Detalle);
        ej.Respuesta = s => new ResultadoProceso { TiempoAgotado = true };
        Assert.Contains("tiempo máximo", (await Motor(ej).CrearAsync("x", null, CancellationToken.None)).Detalle);
        ej.Respuesta = s => throw new System.ComponentModel.Win32Exception("no se pudo iniciar");
        Assert.Contains("No se pudo ejecutar pg_dump", (await Motor(ej).CrearAsync("x", null, CancellationToken.None)).Detalle);
        ej.Respuesta = s => new ResultadoProceso { CodigoSalida = 1, Error = "pg_restore: error: could not execute query: ERROR:  syntax error" };
        var rest = await Motor(ej).RestaurarAsync("x.dump", null, CancellationToken.None);
        Assert.False(rest.Ok); Assert.Contains("código 1", rest.Detalle); Assert.Contains("syntax error", rest.Detalle);
    }

    [Fact]
    public async Task SiPgRestoreQuedaEsperandoUnBloqueoDeOtraSesionSeCortaConUnMensajeClaro()
    {
        var ej = new EjecutorFalso { Asincrona = async (s, t) => { await Task.Delay(30000, t); return new ResultadoProceso(); } };
        var sw = Stopwatch.StartNew();
        var r = await Motor(ej, esperaBloqueo: true, esperaMs: 300).RestaurarAsync("x.dump", null, CancellationToken.None);
        Assert.False(r.Ok); Assert.Contains("otras sesiones", r.Detalle); Assert.Contains("transacciones abiertas", r.Detalle);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), "tardó " + sw.Elapsed);
        // Si el usuario cancela por su cuenta, se propaga la cancelación (no se confunde con el bloqueo).
        using (var cts = new CancellationTokenSource(200))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Motor(ej).RestaurarAsync("x.dump", null, cts.Token));
    }

    [Fact]
    public async Task CancelarPropagaLaCancelacion()
    {
        var ej = new EjecutorFalso { Asincrona = (s, t) => { t.ThrowIfCancellationRequested(); return Task.FromResult(new ResultadoProceso()); } };
        using (var cts = new CancellationTokenSource())
        {
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Motor(ej).CrearAsync("x", null, cts.Token));
        }
    }

    [Fact]
    public void ListarContandoObjetosSinLosComentarios()
    {
        var salida = ";\r\n; Archive created at 2026-11-14\r\n;     dbname: inventario_db\r\n;\r\n; Selected TOC Entries:\r\n;\r\n217; 1259 16401 TABLE public materiales postgres\r\n218; 1259 16402 TABLE public obras postgres\r\n\r\n219; 0 0 TABLE DATA public obras postgres\r\n";
        Assert.Equal(3, MotorPostgresRespaldo.ContarObjetos(salida));
        Assert.Equal(0, MotorPostgresRespaldo.ContarObjetos(";\r\n; solo comentarios"));
        Assert.Equal(0, MotorPostgresRespaldo.ContarObjetos(null));
    }

    // ---------- herramientas ----------
    sealed class Escenario
    {
        public HashSet<string> Existen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Versiones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string[] PostgresInstalados = new string[0];
    }

    async Task<HerramientasResueltas> Resolver(Escenario e, int servidor, string configurada = null, string ejecutable = @"C:\App")
    {
        var ej = new EjecutorFalso();
        ej.Asincrona = null;
        var versiones = e.Versiones;
        var ejecutor = new VersionPorRuta(versiones);
        var loc = new LocalizadorHerramientas(ejecutor, configurada, ejecutable, @"C:\Program Files", f => e.Existen.Contains(f),
            d => e.PostgresInstalados.Select(n => Path.Combine(d, n)));
        return await loc.ResolverAsync(servidor, CancellationToken.None);
    }

    sealed class VersionPorRuta : IEjecutorProcesos
    {
        readonly Dictionary<string, string> v; public VersionPorRuta(Dictionary<string, string> v) { this.v = v; }
        public Task<ResultadoProceso> EjecutarAsync(SolicitudProceso s, CancellationToken t)
            => Task.FromResult(v.TryGetValue(s.Ejecutable, out var texto) ? new ResultadoProceso { Salida = texto } : new ResultadoProceso { CodigoSalida = 1 });
    }

    static void Poner(Escenario e, string carpeta, string version)
    {
        e.Existen.Add(Path.Combine(carpeta, "pg_dump.exe")); e.Existen.Add(Path.Combine(carpeta, "pg_restore.exe"));
        e.Versiones[Path.Combine(carpeta, "pg_dump.exe")] = "pg_dump (PostgreSQL) " + version;
    }

    [Fact]
    public async Task LasHerramientasSeBuscanEnElOrdenConfigurada_Propia_ProgramFilesConLaVersionMayorPrimero()
    {
        var e = new Escenario { PostgresInstalados = new[] { "15", "18", "17", "doc" } };
        Poner(e, @"C:\Program Files\PostgreSQL\15\bin", "15.4"); Poner(e, @"C:\Program Files\PostgreSQL\18\bin", "18.1"); Poner(e, @"C:\Program Files\PostgreSQL\17\bin", "17.5");
        var h = await Resolver(e, 17);
        Assert.True(h.Disponibles); Assert.Equal(@"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe", h.PgDump); Assert.Equal("18.1", h.Version); Assert.Equal(18, h.VersionMayor);
        // Las herramientas propias (junto al ejecutable) ganan a Program Files.
        Poner(e, @"C:\App\herramientas\postgresql\bin", "17.2");
        Assert.Equal(@"C:\App\herramientas\postgresql\bin\pg_dump.exe", (await Resolver(e, 17)).PgDump);
        // La carpeta configurada en appsettings gana a todas.
        Poner(e, @"D:\pgtools", "17.0");
        Assert.Equal(@"D:\pgtools\pg_dump.exe", (await Resolver(e, 17, configurada: @"D:\pgtools")).PgDump);
        // También se acepta la carpeta de instalación (con \bin dentro).
        Poner(e, @"D:\pgroot\bin", "17.0");
        Assert.Equal(@"D:\pgroot\bin\pg_dump.exe", (await Resolver(e, 17, configurada: @"D:\pgroot")).PgDump);
    }

    [Fact]
    public async Task Proceso_de_32_bits_encuentra_PostgreSQL_en_la_raiz_de_64_bits()
    {
        // En 32 bits "ProgramFiles" apunta a (x86); la raíz real está en ProgramW6432.
        var e = new Escenario();
        Poner(e, @"C:\Program Files\PostgreSQL\18\bin", "18.0");
        var loc = new LocalizadorHerramientas(new VersionPorRuta(e.Versiones), null, @"C:\App",
            new[] { @"C:\Program Files (x86)", @"C:\Program Files", @"C:\Program Files (x86)" }, f => e.Existen.Contains(f),
            d => d == @"C:\Program Files\PostgreSQL" ? new[] { Path.Combine(d, "18") } : new string[0]);
        var r = await loc.ResolverAsync(18, CancellationToken.None);
        Assert.True(r.Disponibles);
        Assert.Equal(@"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe", r.PgDump);
    }

    [Fact]
    public async Task Varias_raices_prefieren_la_version_mayor_sin_duplicados()
    {
        var e = new Escenario();
        Poner(e, @"C:\Program Files\PostgreSQL\15\bin", "15.4"); Poner(e, @"C:\Program Files (x86)\PostgreSQL\17\bin", "17.5");
        var loc = new LocalizadorHerramientas(new VersionPorRuta(e.Versiones), null, @"C:\App", new[] { @"C:\Program Files", @"C:\Program Files (x86)", @"c:\program files" },
            f => e.Existen.Contains(f), d => d == @"C:\Program Files (x86)\PostgreSQL" ? new[] { Path.Combine(d, "17") } : new[] { Path.Combine(d, "15") });
        var c = loc.Candidatas();
        Assert.Equal(@"C:\Program Files (x86)\PostgreSQL\17\bin", c[c.Count - 2]);
        Assert.Equal(c.Count, c.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(17, (await loc.ResolverAsync(0, CancellationToken.None)).VersionMayor);
    }

    [Fact]
    public async Task Sin_herramientas_el_mensaje_nombra_la_carpeta_propia_junto_al_ejecutable()
    {
        var loc = new LocalizadorHerramientas(new VersionPorRuta(new Dictionary<string, string>()), null, @"C:\App", new[] { @"C:\Program Files" }, f => false, d => new string[0]);
        var r = await loc.ResolverAsync(18, CancellationToken.None);
        Assert.False(r.Disponibles);
        Assert.Contains(@"C:\App\herramientas\postgresql", r.Mensaje);
    }

    [Fact]
    public async Task SiLaVersionDeLasHerramientasEsMenorQueLaDelServidorSeBuscaOtraOSeInforma()
    {
        var e = new Escenario { PostgresInstalados = new[] { "15", "18" } };
        Poner(e, @"C:\App\herramientas\postgresql", "15.4"); Poner(e, @"C:\Program Files\PostgreSQL\18\bin", "18.0");
        var h = await Resolver(e, 17);
        Assert.True(h.Disponibles); Assert.Equal(18, h.VersionMayor); // salta la 15 y usa la 18
        e = new Escenario { PostgresInstalados = new[] { "15" } };
        Poner(e, @"C:\Program Files\PostgreSQL\15\bin", "15.4");
        h = await Resolver(e, 17);
        Assert.False(h.Disponibles); Assert.Contains("versión menor", h.Mensaje); Assert.Contains("15.4", h.Mensaje); Assert.Contains("17", h.Mensaje);
        // Si no se conoce la versión del servidor, sirve la primera válida.
        Assert.True((await Resolver(e, 0)).Disponibles);
    }

    [Fact]
    public async Task SinHerramientasElMensajeEsClaroYNoLanza()
    {
        var h = await Resolver(new Escenario(), 17);
        Assert.False(h.Disponibles); Assert.Contains("No se encontraron pg_dump y pg_restore", h.Mensaje); Assert.Contains(@"herramientas\postgresql", h.Mensaje); Assert.Contains("Respaldos:CarpetaHerramientas", h.Mensaje);
        // Existen los archivos pero --version falla: no se considera válido.
        var e = new Escenario(); e.Existen.Add(@"C:\App\herramientas\postgresql\pg_dump.exe"); e.Existen.Add(@"C:\App\herramientas\postgresql\pg_restore.exe");
        Assert.False((await Resolver(e, 17)).Disponibles);
    }

    [Theory]
    [InlineData("pg_dump (PostgreSQL) 18.0", "18.0", 18)]
    [InlineData("pg_dump (PostgreSQL) 17.5\r\n", "17.5", 17)]
    [InlineData("pg_dump (PostgreSQL) 18devel", "18", 18)]
    [InlineData("pg_dump (PostgreSQL) 9.6.24", "9.6", 9)]
    public void InterpretaLaVersionDePgDump(string salida, string version, int mayor)
    {
        var r = LocalizadorHerramientas.Interpretar(salida);
        Assert.Equal(version, r.Item1); Assert.Equal(mayor, r.Item2);
        Assert.Null(LocalizadorHerramientas.Interpretar("basura")); Assert.Null(LocalizadorHerramientas.Interpretar(null));
    }

    [Fact]
    public void LaCarpetaDeHerramientasPuedeVenirDeAppsettings()
    {
        File.WriteAllText(Path.Combine(raiz, "appsettings.json"), "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=x\"},\"Respaldos\":{\"CarpetaHerramientas\":\"D:\\\\pg\\\\bin\"}}");
        Assert.Equal(@"D:\pg\bin", LocalConfiguration.Load(raiz).CarpetaHerramientasRespaldo);
        File.WriteAllText(Path.Combine(raiz, "appsettings.json"), "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=x\"}}");
        Assert.Null(LocalConfiguration.Load(raiz).CarpetaHerramientasRespaldo);
    }

    // ---------- ejecutor real ----------
    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("con espacio", "\"con espacio\"")]
    [InlineData("", "\"\"")]
    [InlineData("a\"b", "\"a\\\"b\"")]
    [InlineData(@"C:\dir con espacio\", "\"C:\\dir con espacio\\\\\"")]
    public void EntrecomillaComoLoEsperaWindows(string entrada, string esperado) => Assert.Equal(esperado, EjecutorProcesos.Entrecomillar(entrada));

    static string Cmd => Environment.GetEnvironmentVariable("ComSpec") ?? @"C:\Windows\System32\cmd.exe";

    [Fact]
    public async Task ElEjecutorRealCapturaSalidaCodigoYEntornoSinVentana()
    {
        var r = await new EjecutorProcesos().EjecutarAsync(new SolicitudProceso
        { Ejecutable = Cmd, Argumentos = new[] { "/c", "echo %STEELOG_PRUEBA%& echo err 1>&2& exit 3" }, Entorno = new Dictionary<string, string> { ["STEELOG_PRUEBA"] = "valor-del-entorno" } }, CancellationToken.None);
        Assert.Equal(3, r.CodigoSalida); Assert.Contains("valor-del-entorno", r.Salida); Assert.Contains("err", r.Error); Assert.False(r.TiempoAgotado);
        // La variable solo existe en el proceso hijo.
        Assert.Null(Environment.GetEnvironmentVariable("STEELOG_PRUEBA"));
    }

    [Fact]
    public async Task ElEjecutorRealMataElProcesoAlCancelarOAlSuperarElTiempo()
    {
        var largo = new[] { "/c", "ping -n 30 127.0.0.1 >nul 2>&1" };
        var sw = Stopwatch.StartNew();
        using (var cts = new CancellationTokenSource(400))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new EjecutorProcesos().EjecutarAsync(new SolicitudProceso { Ejecutable = Cmd, Argumentos = largo }, cts.Token));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(15), "tardó " + sw.Elapsed);
        var r = await new EjecutorProcesos().EjecutarAsync(new SolicitudProceso { Ejecutable = Cmd, Argumentos = largo, TiempoMaximo = TimeSpan.FromMilliseconds(400) }, CancellationToken.None);
        Assert.True(r.TiempoAgotado);
        await Assert.ThrowsAnyAsync<Exception>(() => new EjecutorProcesos().EjecutarAsync(new SolicitudProceso { Ejecutable = Path.Combine(raiz, "no-existe.exe") }, CancellationToken.None));
    }

    // ---------- almacén y configuración ----------
    [Fact]
    public void ElAlmacenNoSaleDeLaCarpetaYElBloqueoEsExclusivo()
    {
        var a = new AlmacenArchivosRespaldos(); var carpeta = Path.Combine(raiz, "c");
        a.AsegurarCarpeta(carpeta);
        File.WriteAllText(Path.Combine(raiz, "fuera.dump"), "x"); File.WriteAllText(Path.Combine(carpeta, "dentro.dump"), "y");
        Assert.NotNull(a.RutaDe(carpeta, "dentro.dump"));
        Assert.Null(a.RutaDe(carpeta, @"..\fuera.dump")); Assert.Null(a.RutaDe(carpeta, "inexistente.dump"));
        Assert.Throws<FileNotFoundException>(() => a.Eliminar(carpeta, @"..\fuera.dump"));
        Assert.True(File.Exists(Path.Combine(raiz, "fuera.dump")));
        using (var b1 = a.Bloquear(carpeta))
        {
            Assert.NotNull(b1); Assert.Null(a.Bloquear(carpeta));
        }
        using (var b2 = a.Bloquear(carpeta)) Assert.NotNull(b2);
        Assert.False(File.Exists(Path.Combine(carpeta, AlmacenArchivosRespaldos.NombreBloqueo)));
    }

    [Fact]
    public void ElAlmacenIgnoraTemporalesYBloqueoYTolerarFichasRotas()
    {
        var a = new AlmacenArchivosRespaldos(); var carpeta = Path.Combine(raiz, "c"); a.AsegurarCarpeta(carpeta);
        File.WriteAllText(Path.Combine(carpeta, ".parcial_abc.tmp"), "p"); File.WriteAllText(Path.Combine(carpeta, "steelog_20261114_100000_manual.dump"), "d");
        File.WriteAllText(Path.Combine(carpeta, "steelog_20261114_100000_manual.json"), "{esto no es json");
        var l = a.Listar(carpeta);
        var unico = Assert.Single(l); Assert.Null(unico.Ficha);
    }

    [Fact]
    public void LaFichaJsonSeEscribeYSeLeeCompleta()
    {
        var a = new AlmacenArchivosRespaldos(); var carpeta = Path.Combine(raiz, "c"); a.AsegurarCarpeta(carpeta);
        var f = new FichaRespaldo
        {
            FechaUtc = new DateTimeOffset(2026, 11, 14, 15, 0, 1, TimeSpan.Zero), Tipo = TipoRespaldo.PreRestauracion, Usuario = "ana@x", VersionApp = "1.0.49", VersionServidor = "17.2", Base = "inventario_db",
            TamanoBytes = 1234, Sha256 = "abc", Nota = "ñandú \"comillas\"", ResumenFilas = new Dictionary<string, long> { ["materiales"] = 10, ["obras"] = 3 }
        };
        var id = "steelog_20261114_120001_prerestauracion.dump";
        File.WriteAllText(Path.Combine(carpeta, id), "d");
        a.EscribirFicha(carpeta, id, f);
        var g = a.LeerFicha(carpeta, id);
        Assert.Equal(f.FechaUtc, g.FechaUtc); Assert.Equal(f.Tipo, g.Tipo); Assert.Equal("ana@x", g.Usuario); Assert.Equal("1.0.49", g.VersionApp); Assert.Equal("17.2", g.VersionServidor);
        Assert.Equal("inventario_db", g.Base); Assert.Equal(1234, g.TamanoBytes); Assert.Equal("abc", g.Sha256); Assert.Equal(f.Nota, g.Nota);
        Assert.Equal(10, g.ResumenFilas["materiales"]); Assert.Equal(3, g.ResumenFilas["obras"]);
        Assert.Equal(new[] { Path.ChangeExtension(id, ".json") }, Directory.GetFiles(carpeta, "*.json").Select(Path.GetFileName));
        Assert.Empty(Directory.GetFiles(carpeta, "*.tmp"));
    }

    [Fact]
    public void LaConfiguracionSeGuardaYSeLeeYCaeEnDefectosSiEstaRota()
    {
        var archivo = Path.Combine(raiz, "datos", "respaldos.json"); var defecto = Path.Combine(raiz, "Copias");
        var s = new ConfiguracionRespaldoArchivo(archivo, defecto);
        var d = s.Cargar();
        Assert.Equal(defecto, d.Carpeta); Assert.True(d.AutomaticoActivo); Assert.Equal(24, d.HorasEntreAutomaticos); Assert.Equal(10, d.Conservar);
        s.Guardar(new ConfiguracionRespaldo { Carpeta = Path.Combine(raiz, "otra"), AutomaticoActivo = false, HorasEntreAutomaticos = 8, Conservar = 3 });
        var c = new ConfiguracionRespaldoArchivo(archivo, defecto).Cargar();
        Assert.Equal(Path.Combine(raiz, "otra"), c.Carpeta); Assert.False(c.AutomaticoActivo); Assert.Equal(8, c.HorasEntreAutomaticos); Assert.Equal(3, c.Conservar);
        File.WriteAllText(archivo, "no es json");
        Assert.Equal(defecto, s.Cargar().Carpeta);
        File.WriteAllText(archivo, "{\"carpeta\":\"relativa\",\"automaticoActivo\":true,\"horasEntreAutomaticos\":-4,\"conservar\":0}");
        var corregida = s.Cargar();
        Assert.Equal(defecto, corregida.Carpeta); Assert.Equal(24, corregida.HorasEntreAutomaticos); Assert.Equal(10, corregida.Conservar);
        Assert.Throws<ArgumentException>(() => new ConfiguracionRespaldoArchivo("relativo.json", defecto));
    }

    // ---------- bitácora ----------
    sealed class EscrituraMemoria : IBitacoraEscritura
    {
        public List<EventoBitacoraApp> Guardados = new List<EventoBitacoraApp>();
        public Task<IReadOnlyCollection<Guid>> InsertarAsync(IReadOnlyList<EventoBitacoraApp> e, CancellationToken t) { Guardados.AddRange(e); return Task.FromResult<IReadOnlyCollection<Guid>>(e.Select(x => x.Id).ToList()); }
    }
    sealed class ColaMemoria : IColaBitacora
    {
        public List<EventoBitacoraApp> Lista = new List<EventoBitacoraApp>();
        public void Agregar(EventoBitacoraApp e) => Lista.Add(e);
        public IReadOnlyList<EventoBitacoraApp> Pendientes() => Lista.ToList();
        public void Quitar(IReadOnlyCollection<Guid> ids) => Lista.RemoveAll(x => ids.Contains(x.Id));
    }

    [Fact]
    public void ElEventoGenericoDeBitacoraUsaLaMismaColaYEscrituraQueLosErrores()
    {
        var sesion = new Session(); sesion.Start(new Identity("admin@x", Role.Admin));
        var escritura = new EscrituraMemoria(); var cola = new ColaMemoria();
        var reg = new RegistroErroresBitacora(sesion, escritura, cola, () => new DateTimeOffset(2026, 11, 14, 1, 2, 3, TimeSpan.Zero), "PC", "1.0", a => a());
        reg.Evento("respaldo", "Copia de seguridad Manual: x.dump (1 KB)", "respaldos");
        reg.Evento("RESTAURACION", "Base restaurada desde x Database=secreta;Password=abc", "respaldos");
        reg.Evento("  ", "ignorado");
        reg.Error("boom");
        Assert.Equal(new[] { "RESPALDO", "RESTAURACION", "ERROR" }, escritura.Guardados.Select(e => e.Accion));
        Assert.All(escritura.Guardados, e => Assert.Equal("admin@x", e.Usuario));
        Assert.Equal("respaldos", escritura.Guardados[0].Entidad);
        Assert.DoesNotContain("abc", escritura.Guardados[1].Descripcion); // se sanea igual que los errores
        Assert.Empty(cola.Lista);
        RegistroErroresBitacoraNulo.Instancia.Evento("X", "y");
        // ResumenBitacora muestra la descripción propia de las acciones nuevas.
        foreach (var accion in new[] { "RESPALDO", "RESTAURACION", "RESPALDO_ERROR" })
            Assert.Equal("Copia x.dump", new BitacoraRegistro { Origen = "APP", Accion = accion, Descripcion = "Copia x.dump" }.Resumen);
    }
}
