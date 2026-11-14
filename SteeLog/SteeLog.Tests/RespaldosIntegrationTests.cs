using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Respaldos;
using SteeLog.Infrastructure.Bitacora;
using SteeLog.Infrastructure.Persistence;
using SteeLog.Infrastructure.Respaldos;
using Xunit;

/// <summary>
/// Copias de seguridad con pg_dump / pg_restore reales contra un PostgreSQL local DESCARTABLE (nunca el 5435).
/// Se ejecuta con scripts/tests/respaldos-integracion.ps1.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RespaldosIntegrationTests : IDisposable
{
    sealed class ConfiguracionMemoria : IConfiguracionRespaldos
    {
        public ConfiguracionRespaldo Actual;
        public ConfiguracionRespaldo Cargar() => Actual.Clonar();
        public void Guardar(ConfiguracionRespaldo c) { Actual = c.Clonar(); }
    }
    sealed class ProgresoSincrono : IProgress<ProgresoRespaldo>
    {
        readonly Action<ProgresoRespaldo> accion; public ProgresoSincrono(Action<ProgresoRespaldo> accion) { this.accion = accion; }
        public void Report(ProgresoRespaldo v) => accion(v);
    }

    static string Cadena()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_RESPALDOS_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_RESPALDOS_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Usar el runner aislado de respaldos.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435) throw new InvalidOperationException("Destino no permitido.");
        using (var c = new NpgsqlConnection(cs)) { c.Open(); using (var q = new NpgsqlCommand("SELECT current_database()", c)) if ((string)q.ExecuteScalar() != db) throw new InvalidOperationException("DB incorrecta."); }
        return cs;
    }

    readonly string cadena = Cadena();
    readonly string raiz = Path.Combine(Environment.GetEnvironmentVariable("STEELOG_RESPALDOS_TEST_CARPETA") ?? Path.GetTempPath(), "steelog-resp-int-" + Guid.NewGuid().ToString("N"));
    readonly string carpeta;
    readonly Session sesion = new Session();
    readonly ConfiguracionMemoria config = new ConfiguracionMemoria();
    readonly int pgDumpAntes = Process.GetProcessesByName("pg_dump").Length;

    public RespaldosIntegrationTests()
    {
        carpeta = Path.Combine(raiz, "Copias");
        Directory.CreateDirectory(raiz);
        config.Actual = new ConfiguracionRespaldo { Carpeta = carpeta, AutomaticoActivo = true, HorasEntreAutomaticos = 24, Conservar = 5 };
        sesion.Start(new Identity("admin@steelog", Role.Admin));
    }
    public void Dispose() { NpgsqlConnection.ClearAllPools(); try { Directory.Delete(raiz, true); } catch { } }

    ServicioRespaldos Servicio(int esperaBloqueoMs = 2000)
    {
        var ejecutor = new EjecutorProcesos();
        var bd = new NpgsqlBaseRespaldo(() => cadena);
        var localizador = new LocalizadorHerramientas(ejecutor, Environment.GetEnvironmentVariable("STEELOG_RESPALDOS_TEST_HERRAMIENTAS"), Path.Combine(raiz, "sin-herramientas-propias"), Environment.GetEnvironmentVariable("ProgramFiles"));
        var motor = new MotorPostgresRespaldo(ejecutor, () => cadena, localizador, bd, esperaBloqueoMs);
        var bitacora = new RegistroErroresBitacora(sesion, new NpgsqlBitacoraEscritura(new PostgresConnectionFactory(cadena, null)), new ColaBitacoraArchivo(Path.Combine(raiz, "cola")),
            () => DateTimeOffset.UtcNow, "PC-TEST", "9.9.9", a => a());
        return new ServicioRespaldos(sesion, config, new AlmacenArchivosRespaldos(), motor, bd, bitacora, () => DateTimeOffset.UtcNow, "9.9.9");
    }

    static NpgsqlParameter P(string k, object v) => new NpgsqlParameter(k, v ?? DBNull.Value);
    object Sql(string sql, params NpgsqlParameter[] args)
    {
        using (var c = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(cadena) { Pooling = false }.ConnectionString))
        { c.Open(); using (var q = new NpgsqlCommand(sql, c)) { q.Parameters.AddRange(args); return q.ExecuteScalar(); } }
    }
    string Foto() => (string)Sql("SELECT (SELECT count(*) FROM materiales)||'|'||(SELECT count(*) FROM movimientos)||'|'||(SELECT count(*) FROM obras)||'|'||(SELECT count(*) FROM herramientas)||'|'||" +
        "coalesce((SELECT string_agg(id||':'||nombre, ',' ORDER BY id) FROM materiales), '')");
    void NuevoMaterial(string nombre) => Sql("INSERT INTO materiales (id_categoria, nombre) VALUES ((SELECT min(id_categoria) FROM categorias), @n)", P("n", nombre));
    bool Existe(string nombre) => (long)Sql("SELECT count(*) FROM materiales WHERE nombre = @n", P("n", nombre)) > 0;
    string[] Archivos(string patron) => Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, patron).Select(Path.GetFileName).OrderBy(x => x).ToArray() : new string[0];
    static readonly CancellationToken Nada = CancellationToken.None;

    [Fact]
    public async Task CrearVerificarModificarRestaurarDevuelveLosDatosYGeneraLaPreRestauracion()
    {
        var s = Servicio();
        var a = "MAT-A-" + Guid.NewGuid().ToString("N").Substring(0, 8); var b = "MAT-B-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        NuevoMaterial(a);
        var foto = Foto();

        var estado = await s.ObtenerEstadoAsync(Nada);
        Assert.True(estado.HerramientasDisponibles, estado.MensajeHerramientas); Assert.True(estado.BaseConectada, estado.MensajeBase);
        Assert.Matches(@"^\d+\.\d+", estado.VersionServidor); Assert.Matches(@"^\d+", estado.VersionHerramientas);

        var etapas = new List<string>();
        var creada = await s.CrearAsync("integración", new ProgresoSincrono(p => etapas.Add(p.Etapa)), Nada);
        Assert.True(creada.Ok, creada.Detalle);
        Assert.True(creada.Respaldo.TamanoBytes > 1000); Assert.Matches("^[0-9a-f]{64}$", creada.Respaldo.Sha256);
        Assert.Equal(long.Parse(Foto().Split('|')[0]), creada.Respaldo.ResumenFilas["materiales"]);
        Assert.Contains(etapas, e => e.StartsWith("Exportando")); Assert.Empty(Archivos("*.tmp")); Assert.Empty(Archivos(".parcial_*"));

        var v = await s.VerificarAsync(creada.Respaldo.Id, Nada);
        Assert.True(v.Ok, v.Detalle); Assert.True(v.Objetos > 20);

        // Se modifica la base: se borra un material y se crea otro.
        Sql("DELETE FROM materiales WHERE nombre = @n", P("n", a)); NuevoMaterial(b);
        var modificada = Foto(); Assert.NotEqual(foto, modificada);

        var r = await s.RestaurarAsync(creada.Respaldo.Id, null, Nada);
        Assert.True(r.Ok, r.Detalle);
        Assert.Equal(foto, Foto()); Assert.True(Existe(a)); Assert.False(Existe(b));

        // Se generó la PreRestauracion, con el estado previo a restaurar (el modificado).
        Assert.Equal(TipoRespaldo.PreRestauracion, r.RespaldoPrevio.Tipo);
        Assert.Single(Archivos("*_prerestauracion.dump")); Assert.Single(Archivos("*_prerestauracion.json"));
        var rr = await s.RestaurarAsync(r.RespaldoPrevio.Id, null, Nada);
        Assert.True(rr.Ok, rr.Detalle);
        Assert.Equal(modificada, Foto()); Assert.False(Existe(a)); Assert.True(Existe(b));

        // La bitácora registró la creación y las restauraciones DESPUÉS de restaurar (no las pisó el pg_restore).
        Assert.True((long)Sql("SELECT count(*) FROM bitacora_auditoria WHERE origen='APP' AND accion='RESTAURACION' AND descripcion LIKE '%' || @n || '%'", P("n", r.RespaldoPrevio.NombreArchivo)) >= 1);
        Assert.True((long)Sql("SELECT count(*) FROM bitacora_auditoria WHERE origen='APP' AND accion='RESPALDO'") >= 1);
        var lista = (await s.ObtenerEstadoAsync(Nada)).Respaldos;
        Assert.Equal(3, lista.Count); Assert.All(lista, x => Assert.True(x.TieneFicha));
    }

    [Fact]
    public async Task UnaCopiaCorruptaFallaLaVerificacionYNoSeRestaura()
    {
        var s = Servicio();
        var creada = await s.CrearAsync(null, null, Nada); Assert.True(creada.Ok, creada.Detalle);
        var ruta = Path.Combine(carpeta, creada.Respaldo.NombreArchivo);
        using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite))
        { fs.Position = fs.Length / 2; var bytes = new byte[16]; fs.Read(bytes, 0, 16); for (var i = 0; i < 16; i++) bytes[i] ^= 0xFF; fs.Position = fs.Length / 2; fs.Write(bytes, 0, 16); }

        var v = await s.VerificarAsync(creada.Respaldo.Id, Nada);
        Assert.False(v.Ok); Assert.Contains("SHA-256", v.Detalle);

        var marca = "MARCA-" + Guid.NewGuid().ToString("N").Substring(0, 8); NuevoMaterial(marca);
        var foto = Foto();
        var r = await s.RestaurarAsync(creada.Respaldo.Id, null, Nada);
        Assert.False(r.Ok); Assert.Contains("verificación", r.Detalle); Assert.Null(r.RespaldoPrevio);
        Assert.Equal(foto, Foto()); Assert.True(Existe(marca));
        Assert.Empty(Archivos("*_prerestauracion.*"));
    }

    [Fact]
    public async Task UnaCopiaSinFichaConLaCabeceraDanadaNoPasaPgRestoreList()
    {
        var s = Servicio();
        var creada = await s.CrearAsync(null, null, Nada); Assert.True(creada.Ok, creada.Detalle);
        File.Delete(Path.Combine(carpeta, Path.GetFileNameWithoutExtension(creada.Respaldo.NombreArchivo) + ".json"));
        var ruta = Path.Combine(carpeta, creada.Respaldo.NombreArchivo);
        using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite)) fs.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 8);
        var v = await s.VerificarAsync(creada.Respaldo.Id, Nada);
        Assert.False(v.Ok); Assert.False(string.IsNullOrWhiteSpace(v.Detalle));
        Assert.False((await s.RestaurarAsync(creada.Respaldo.Id, null, Nada)).Ok);
        // Se lista igual, derivando los datos del archivo.
        var item = Assert.Single((await s.ObtenerEstadoAsync(Nada)).Respaldos); Assert.False(item.TieneFicha);
    }

    [Fact]
    public async Task CancelarAMitadDeLaCopiaMataPgDumpYNoDejaArchivosParciales()
    {
        Sql("DROP TABLE IF EXISTS public.relleno_respaldos");
        Sql("CREATE TABLE public.relleno_respaldos AS SELECT g AS id, md5(g::text) || md5((g + 1)::text) || md5((g + 2)::text) AS t FROM generate_series(1, 3000000) g");
        try
        {
            var s = Servicio();
            using (var cts = new CancellationTokenSource())
            {
                var sw = Stopwatch.StartNew();
                var cancelo = false;
                var progreso = new ProgresoSincrono(p => { if (p.Etapa != null && p.Etapa.StartsWith("Exportando:")) { cancelo = true; cts.Cancel(); } });
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => s.CrearAsync(null, progreso, cts.Token));
                Assert.True(cancelo);
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(60));
            }
            Assert.Empty(Archivos("*.tmp")); Assert.Empty(Archivos("*.dump")); Assert.Empty(Archivos("*.json")); Assert.Empty(Archivos(".parcial_*"));
            for (var i = 0; i < 50 && Process.GetProcessesByName("pg_dump").Length > pgDumpAntes; i++) await Task.Delay(100);
            Assert.True(Process.GetProcessesByName("pg_dump").Length <= pgDumpAntes, "quedó un pg_dump corriendo");
            // El bloqueo se liberó: se puede volver a copiar.
            Sql("DROP TABLE public.relleno_respaldos");
            Assert.True((await s.CrearAsync(null, null, Nada)).Ok);
        }
        finally { Sql("DROP TABLE IF EXISTS public.relleno_respaldos"); }
    }

    [Fact]
    public async Task ConOtraSesionBloqueandoLaRestauracionFallaClaroLaBaseQuedaIgualYLuegoSePuedeReintentar()
    {
        var s = Servicio(esperaBloqueoMs: 2000);
        var creada = await s.CrearAsync(null, null, Nada); Assert.True(creada.Ok, creada.Detalle);
        var marca = "MARCA-" + Guid.NewGuid().ToString("N").Substring(0, 8); NuevoMaterial(marca);
        var foto = Foto();
        using (var otra = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(cadena) { Pooling = false }.ConnectionString))
        {
            otra.Open();
            var tx = otra.BeginTransaction();
            using (var q = new NpgsqlCommand("LOCK TABLE public.materiales IN ACCESS SHARE MODE", otra, tx)) q.ExecuteNonQuery();
            var sw = Stopwatch.StartNew();
            var r = await s.RestaurarAsync(creada.Respaldo.Id, null, Nada);
            Assert.False(r.Ok); Assert.Contains("otras sesiones", r.Detalle); Assert.Contains("única transacción", r.Detalle);
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), "la espera no tuvo tope");
            Assert.NotNull(r.RespaldoPrevio);
            tx.Rollback();
        }
        // Single transaction: no se tocó nada.
        Assert.Equal(foto, Foto()); Assert.True(Existe(marca));
        var reintento = await s.RestaurarAsync(creada.Respaldo.Id, null, Nada);
        Assert.True(reintento.Ok, reintento.Detalle); Assert.False(Existe(marca));
    }

    [Fact]
    public async Task ImportarUnDumpExternoLoDejaVerificableYRestaurable()
    {
        var s = Servicio();
        var creada = await s.CrearAsync(null, null, Nada); Assert.True(creada.Ok, creada.Detalle);
        var externo = Path.Combine(raiz, "externo.backup");
        File.Copy(Path.Combine(carpeta, creada.Respaldo.NombreArchivo), externo);
        Assert.True((await s.EliminarAsync(creada.Respaldo.Id, Nada)).Ok);
        Assert.Empty(Archivos("*.dump")); Assert.Empty(Archivos("*.json"));
        var marca = "MARCA-" + Guid.NewGuid().ToString("N").Substring(0, 8); NuevoMaterial(marca);
        var imp = await s.ImportarAsync(externo, Nada);
        Assert.True(imp.Ok, imp.Detalle); Assert.Equal(TipoRespaldo.Importado, imp.Respaldo.Tipo);
        Assert.True((await s.VerificarAsync(imp.Respaldo.Id, Nada)).Ok);
        Assert.True((await s.RestaurarAsync(imp.Respaldo.Id, null, Nada)).Ok);
        Assert.False(Existe(marca));
        // Un archivo que no es un dump no se importa.
        var basura = Path.Combine(raiz, "basura.dump"); File.WriteAllText(basura, "esto no es un dump de postgres");
        var mala = await s.ImportarAsync(basura, Nada);
        Assert.False(mala.Ok); Assert.Empty(Archivos("*.tmp")); Assert.Empty(Archivos(".parcial_*"));
    }

    [Fact]
    public async Task ElAutomaticoCreaUnaCopiaConLaBaseRealYNoRepiteDentroDeLaVentana()
    {
        var s = Servicio();
        var r = await s.EjecutarSiCorrespondeAsync(Nada);
        Assert.True(r.Ejecutado && r.Ok, r.Mensaje); Assert.Equal(TipoRespaldo.Automatico, r.Respaldo.Tipo);
        Assert.False((await s.EjecutarSiCorrespondeAsync(Nada)).Ejecutado);
        Assert.Single(Archivos("*_automatico.dump"));
    }
}
