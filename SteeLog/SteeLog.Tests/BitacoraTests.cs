using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Identidad;
using SteeLog.Infrastructure.Bitacora;
using SteeLog.Infrastructure.Persistence;
using Xunit;

public sealed class BitacoraConsultaTests
{
    sealed class ConsultaFalsa : IBitacoraConsulta
    {
        public int Llamadas; public BitacoraFiltro Ultimo;
        public Task<BitacoraResultado> BuscarAsync(BitacoraFiltro filtro, CancellationToken token = default) { Llamadas++; Ultimo = filtro; return Task.FromResult(new BitacoraResultado()); }
        public Task<BitacoraOpciones> OpcionesAsync(CancellationToken token = default) { Llamadas++; return Task.FromResult(new BitacoraOpciones { Usuarios = { "a@b" } }); }
    }

    static Session Sesion(Role? rol) { var s = new Session(); if (rol.HasValue) s.Start(new Identity("x@tesis.local", rol.Value)); return s; }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Galpon)]
    public async Task SinAdminNoConsultaNiCargaOpciones(Role? rol)
    {
        var consulta = new ConsultaFalsa(); var caso = new ConsultarBitacora(Sesion(rol), consulta);
        Action buscar = () => caso.BuscarAsync(new BitacoraFiltro(), CancellationToken.None); Assert.Throws<UnauthorizedAccessException>(buscar);
        Action opciones = () => caso.OpcionesAsync(CancellationToken.None); Assert.Throws<UnauthorizedAccessException>(opciones);
        Assert.Equal(0, consulta.Llamadas);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AdminConsultaYObtieneOpciones()
    {
        var consulta = new ConsultaFalsa(); var caso = new ConsultarBitacora(Sesion(Role.Admin), consulta);
        Assert.True((await caso.BuscarAsync(null, CancellationToken.None)).Instalada);
        Assert.NotNull(consulta.Ultimo);
        Assert.Equal(new[] { "a@b" }, (await caso.OpcionesAsync(CancellationToken.None)).Usuarios);
    }

    [Fact]
    public void ResultadoNoInstaladaTraeMensaje()
    {
        Assert.Null(new BitacoraResultado().Mensaje);
        Assert.Contains("no está instalada", new BitacoraResultado { Instalada = false }.Mensaje);
        Assert.Contains("no está instalada", new BitacoraOpciones { Instalada = false }.Mensaje);
    }

    [Fact]
    public void LimiteEfectivoTieneDefectoYTope()
    {
        Assert.Equal(2000, new BitacoraFiltro().LimiteEfectivo);
        Assert.Equal(2000, new BitacoraFiltro { Limite = 0 }.LimiteEfectivo);
        Assert.Equal(50, new BitacoraFiltro { Limite = 50 }.LimiteEfectivo);
        Assert.Equal(10000, new BitacoraFiltro { Limite = 999999 }.LimiteEfectivo);
    }

    [Fact]
    public void SqlSinFiltrosSoloPideLimite()
    {
        var sql = ConsultaBitacoraSql.Construir(null);
        Assert.DoesNotContain("WHERE", sql.Texto);
        Assert.Contains("ORDER BY fecha DESC, id DESC LIMIT @limite", sql.Texto);
        Assert.Equal(2001, sql.Parametros.Single(p => p.Nombre == "limite").Valor);
    }

    [Fact]
    public void SqlConFiltrosNuncaConcatenaTextoDelUsuario()
    {
        var peligro = "x'; DROP TABLE bitacora_auditoria; --";
        var f = new BitacoraFiltro { Desde = new DateTime(2026, 11, 1, 15, 0, 0), Hasta = new DateTime(2026, 11, 3), Usuario = peligro, Entidad = peligro, Accion = peligro, Texto = peligro + "%_", Limite = 10 };
        var sql = ConsultaBitacoraSql.Construir(f);
        Assert.DoesNotContain("DROP", sql.Texto);
        foreach (var n in new[] { "@desde", "@hasta", "@usuario", "@entidad", "@accion", "@texto", "@limite" }) Assert.Contains(n, sql.Texto);
        Assert.Equal(peligro, sql.Parametros.Single(p => p.Nombre == "usuario").Valor);
        Assert.Equal(@"%x'; DROP TABLE bitacora\_auditoria; --\%\_%", sql.Parametros.Single(p => p.Nombre == "texto").Valor);
        Assert.Equal(11, sql.Parametros.Single(p => p.Nombre == "limite").Valor);
        // Desde: inicio del día local; Hasta: inicio del día siguiente (inclusive el día elegido).
        Assert.Equal(new DateTime(2026, 11, 1).ToUniversalTime(), (DateTime)sql.Parametros.Single(p => p.Nombre == "desde").Valor);
        Assert.Equal(new DateTime(2026, 11, 4).ToUniversalTime(), (DateTime)sql.Parametros.Single(p => p.Nombre == "hasta").Valor);
    }

    [Fact]
    public void SqlIgnoraFiltrosEnBlanco()
    {
        var sql = ConsultaBitacoraSql.Construir(new BitacoraFiltro { Usuario = " ", Texto = "" });
        Assert.DoesNotContain("WHERE", sql.Texto);
    }
}

public sealed class ResumenBitacoraTests
{
    static BitacoraRegistro R(string accion, string antes = null, string despues = null, string desc = null, string ent = "materiales", string id = "7")
        => new BitacoraRegistro { Accion = accion, Entidad = ent, EntidadId = id, DatosAntes = antes, DatosDespues = despues, Descripcion = desc };

    [Fact] public void UsaLaDescripcionPropiaEnUnaLinea() => Assert.Equal("a  b", R("ERROR", desc: "a\r\nb").Resumen);
    [Fact] public void AltaMuestraNombre() => Assert.Equal("Alta en materiales #7 (nombre: Chapa)", R("INSERT", despues: "{\"id\":7,\"nombre\":\"Chapa\"}").Resumen);
    [Fact] public void BajaMuestraNombreAnterior() => Assert.Equal("Baja en materiales #7 (nombre: Perfil)", R("DELETE", antes: "{\"nombre\":\"Perfil\"}").Resumen);

    [Fact]
    public void ModificacionMuestraTresCambiosYElResto()
    {
        var r = R("UPDATE", "{\"a\":1,\"b\":null,\"c\":\"x\",\"d\":4}", "{\"a\":2,\"b\":3,\"c\":\"y\",\"d\":5}");
        Assert.Equal("materiales #7: a: 1 → 2, b: NULL → 3, c: x → y (+1 más)", r.Resumen);
    }

    [Fact] public void ModificacionSinDatos() => Assert.Equal("materiales #7 modificado", R("UPDATE").Resumen);
    [Fact] public void AccionDesconocida() => Assert.Equal("LOGIN - materiales", R("LOGIN").Resumen);
    [Fact] public void ValorLargoSeAcorta() => Assert.Contains("...", R("UPDATE", "{\"a\":\"" + new string('z', 80) + "\"}", "{\"a\":1}").Resumen);

    [Fact]
    public void LeerJsonSoportaEscapesAnidadosYNulos()
    {
        var d = ResumenBitacora.LeerJson("{\"a\":\"h\\\"ol\\u00e1\\n\",\"b\":{\"x\":[1,\"}\"]},\"c\":null,\"d\":true,\"e\":1.5}");
        Assert.Equal("h\"olá\n", d[0].Value);
        Assert.Equal("{\"x\":[1,\"}\"]}", d[1].Value);
        Assert.Equal("NULL", d[2].Value); Assert.Equal("true", d[3].Value); Assert.Equal("1.5", d[4].Value);
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("{\"a\":")]
    [InlineData("{a:1}")]
    public void JsonInvalidoNoLanza(string json)
    {
        var d = ResumenBitacora.LeerJson(json);
        Assert.Single(d); Assert.Equal("(json)", d[0].Key);
    }

    [Fact]
    public void CambiosRespetanAccion()
    {
        var alta = ResumenBitacora.Cambios(R("INSERT", "{\"a\":1}", "{\"a\":2}"));
        Assert.Equal("", alta.Single().Antes); Assert.Equal("2", alta.Single().Despues);
        var baja = ResumenBitacora.Cambios(R("DELETE", "{\"a\":1}", "{\"a\":2}"));
        Assert.Equal("1", baja.Single().Antes); Assert.Equal("", baja.Single().Despues);
        var mod = ResumenBitacora.Cambios(R("UPDATE", "{\"a\":1}", "{\"a\":2,\"b\":3}"));
        Assert.Equal(new[] { "a", "b" }, mod.Select(c => c.Campo));
        Assert.Empty(ResumenBitacora.Cambios(null));
    }
}

public sealed class ColaBitacoraArchivoTests : IDisposable
{
    readonly string carpeta = Path.Combine(Path.GetTempPath(), "steelog-cola-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(carpeta, true); } catch { } }
    static EventoBitacoraApp Ev(string desc = "e") => new EventoBitacoraApp { Id = Guid.NewGuid(), Fecha = DateTimeOffset.UtcNow, Usuario = "u@x", Rol = "Admin", Equipo = "PC", Version = "1", Accion = "ERROR", Entidad = "Pantalla", Descripcion = desc };

    [Fact]
    public void EscribeUnaLineaPorEventoYLosRecupera()
    {
        var cola = new ColaBitacoraArchivo(carpeta); var a = Ev("línea\ncon salto \"y comillas\""); var b = Ev("b");
        cola.Agregar(a); cola.Agregar(b);
        Assert.Equal(2, File.ReadAllLines(cola.Ruta).Length);
        var p = cola.Pendientes();
        Assert.Equal(new[] { a.Id, b.Id }, p.Select(x => x.Id));
        Assert.Equal(a.Descripcion, p[0].Descripcion); Assert.Equal(a.Fecha, p[0].Fecha); Assert.Equal("Pantalla", p[0].Entidad);
    }

    [Fact]
    public void QuitarEliminaSoloLosSubidosYBorraElArchivoVacio()
    {
        var cola = new ColaBitacoraArchivo(carpeta); var a = Ev(); var b = Ev();
        cola.Agregar(a); cola.Agregar(b);
        cola.Quitar(new[] { a.Id });
        Assert.Equal(b.Id, cola.Pendientes().Single().Id);
        cola.Quitar(new[] { b.Id });
        Assert.False(File.Exists(cola.Ruta)); Assert.Empty(cola.Pendientes());
    }

    [Fact]
    public void LineasCorruptasSeIgnoranYNoSeDuplicaElMismoId()
    {
        var cola = new ColaBitacoraArchivo(carpeta); var a = Ev();
        cola.Agregar(a);
        File.AppendAllText(cola.Ruta, "basura{{\r\n\r\n{\"id\":\"no-guid\",\"accion\":\"ERROR\",\"fecha\":\"x\"}\r\n");
        cola.Agregar(a);
        Assert.Equal(a.Id, cola.Pendientes().Single().Id);
    }

    [Fact]
    public void ColaInexistenteEsVacia() => Assert.Empty(new ColaBitacoraArchivo(carpeta).Pendientes());

    [Fact]
    public void ElTopeDescartaLosMasViejosYConservaElNuevo()
    {
        var cola = new ColaBitacoraArchivo(carpeta, 4096); var todos = new List<EventoBitacoraApp>();
        for (int i = 0; i < 40; i++) { var e = Ev(new string('x', 200) + i); todos.Add(e); cola.Agregar(e); }
        Assert.True(new FileInfo(cola.Ruta).Length <= 4096);
        var p = cola.Pendientes();
        Assert.Equal(todos.Last().Id, p.Last().Id);
        Assert.DoesNotContain(todos.First().Id, p.Select(x => x.Id));
        Assert.True(p.Count > 5);
    }
}

public sealed class RegistroErroresBitacoraTests
{
    sealed class EscrituraFalsa : IBitacoraEscritura
    {
        public bool Disponible = true; public bool Lanza; public List<EventoBitacoraApp> Guardados = new List<EventoBitacoraApp>(); public int Intentos;
        public Task<IReadOnlyCollection<Guid>> InsertarAsync(IReadOnlyList<EventoBitacoraApp> eventos, CancellationToken token = default)
        {
            Intentos++;
            if (Lanza) throw new InvalidOperationException("boom");
            if (!Disponible) return Task.FromResult<IReadOnlyCollection<Guid>>(new Guid[0]);
            Guardados.AddRange(eventos);
            return Task.FromResult<IReadOnlyCollection<Guid>>(eventos.Select(e => e.Id).ToList());
        }
    }
    sealed class ColaMemoria : IColaBitacora
    {
        public List<EventoBitacoraApp> Items = new List<EventoBitacoraApp>(); public bool Rota;
        public void Agregar(EventoBitacoraApp e) { if (Rota) throw new IOException("disco"); Items.Add(e); }
        public IReadOnlyList<EventoBitacoraApp> Pendientes() => Items.ToList();
        public void Quitar(IReadOnlyCollection<Guid> ids) => Items.RemoveAll(i => ids.Contains(i.Id));
    }

    DateTimeOffset reloj = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);
    readonly Session sesion = new Session(); readonly EscrituraFalsa escritura = new EscrituraFalsa(); readonly ColaMemoria cola = new ColaMemoria();
    RegistroErroresBitacora Crear() => new RegistroErroresBitacora(sesion, escritura, cola, () => reloj, "PC-1", "9.9.9", a => a());

    [Fact]
    public void SubeElErrorConIdentidadYLoQuitaDeLaCola()
    {
        sesion.Start(new Identity("admin@tesis.local", Role.Admin));
        Crear().Error("falló algo", "Kardex");
        var e = Assert.Single(escritura.Guardados);
        Assert.Equal("ERROR", e.Accion); Assert.Equal("Kardex", e.Entidad); Assert.Equal("admin@tesis.local", e.Usuario);
        Assert.Equal("Admin", e.Rol); Assert.Equal("PC-1", e.Equipo); Assert.Equal("9.9.9", e.Version);
        Assert.Empty(cola.Items);
    }

    [Fact]
    public void ErrorNoControladoSinSesionYSaneaCredenciales()
    {
        Crear().ErrorNoControlado(new Exception("conexión: Host=srv;Password=secreta;Database=x"));
        var e = Assert.Single(escritura.Guardados);
        Assert.Equal("ERROR_NO_CONTROLADO", e.Accion); Assert.Null(e.Usuario); Assert.Null(e.Rol);
        Assert.DoesNotContain("secreta", e.Descripcion); Assert.DoesNotContain("srv", e.Descripcion);
        Assert.Contains("Password=[omitido]", e.Descripcion);
    }

    [Fact]
    public void LaDescripcionSeAcota() { Crear().Error(new string('a', 20000)); Assert.True(escritura.Guardados.Single().Descripcion.Length <= SaneadorTexto.MaximoCaracteres + 3); }

    [Fact]
    public void SinConexionQuedaEnLaColaYSeReintentaSinDuplicar()
    {
        escritura.Disponible = false; var r = Crear();
        r.Error("uno");
        Assert.Single(cola.Items); Assert.Empty(escritura.Guardados);
        escritura.Disponible = true;
        r.ReintentarPendientes();
        Assert.Equal("uno", Assert.Single(escritura.Guardados).Descripcion); Assert.Empty(cola.Items);
        r.ReintentarPendientes();
        Assert.Single(escritura.Guardados);
    }

    [Fact]
    public void TrasUnFalloNoGolpeaLaBaseHastaPasarLaEspera()
    {
        escritura.Disponible = false; var r = Crear();
        r.Error("a"); Assert.Equal(1, escritura.Intentos);
        reloj = reloj.AddSeconds(5); r.Error("b"); Assert.Equal(1, escritura.Intentos);
        escritura.Disponible = true; reloj = reloj.AddSeconds(40); r.Error("c");
        Assert.Equal(new[] { "a", "b", "c" }, escritura.Guardados.Select(e => e.Descripcion));
        Assert.Empty(cola.Items);
    }

    [Fact]
    public void NuncaLanzaAunqueTodoFalle()
    {
        escritura.Lanza = true; cola.Rota = true; var r = Crear();
        r.Error("x"); r.ErrorNoControlado(null); r.ErrorNoControlado(new Exception("y")); r.ReintentarPendientes();
        var sinSesionNiDestino = new RegistroErroresBitacora(sesion, escritura, cola, () => throw new InvalidOperationException(), null, null, a => a());
        sinSesionNiDestino.Error("z");
    }

    [Fact]
    public void SiFallaElDiscoElEventoSeSubeDesdeMemoria()
    {
        cola.Rota = true; Crear().Error("sin disco");
        Assert.Equal("sin disco", Assert.Single(escritura.Guardados).Descripcion);
    }

    [Fact]
    public void ElMismoErrorEnBucleSeRegistraUnaVez()
    {
        var r = Crear();
        r.Error("loop"); r.Error("loop"); reloj = reloj.AddMilliseconds(500); r.Error("loop");
        Assert.Single(escritura.Guardados);
        reloj = reloj.AddSeconds(3); r.Error("loop");
        Assert.Equal(2, escritura.Guardados.Count);
    }

    [Fact]
    public void LaFechaSeRedondeaAMicrosegundos()
    {
        reloj = reloj.AddTicks(7); Crear().Error("t");
        Assert.Equal(0, escritura.Guardados.Single().Fecha.Ticks % 10);
    }

    [Fact]
    public void NoBloqueaAlLlamador()
    {
        Action pendiente = null;
        var r = new RegistroErroresBitacora(sesion, escritura, cola, () => reloj, "PC", "1", a => pendiente = a);
        r.Error("diferido");
        Assert.Empty(escritura.Guardados); Assert.Single(cola.Items); // quedó a disco y la subida espera al segundo plano
        pendiente(); Assert.Single(escritura.Guardados);
    }
}

public sealed class IdentificacionConexionTests
{
    const string Base = "Host=localhost;Database=demo;Username=demo;Password=demo";
    static string Opciones(string cadena) => new NpgsqlConnectionStringBuilder(cadena).Options;

    [Fact]
    public void SinSesionVaVacioPeroLaConexionSirve()
    {
        var fabrica = new PostgresConnectionFactory(Base, () => new IdentificacionConexion { Equipo = "PC", Version = "1.0.49" });
        var o = Opciones(fabrica.Create().ConnectionString);
        Assert.Equal("-c app.usuario= -c app.rol= -c app.equipo=PC -c app.version=1.0.49", o);
        Assert.Equal("demo", new NpgsqlConnectionStringBuilder(fabrica.Create().ConnectionString).Password);
    }

    [Fact]
    public void ConSesionIncluyeUsuarioYRol()
    {
        var fabrica = new PostgresConnectionFactory(Base, () => new IdentificacionConexion { Usuario = "admin@tesis.local", Rol = "Admin", Equipo = "PC", Version = "1.0.49" });
        Assert.Equal("-c app.usuario=admin@tesis.local -c app.rol=Admin -c app.equipo=PC -c app.version=1.0.49", Opciones(fabrica.Create().ConnectionString));
    }

    [Fact]
    public void CambioDeUsuarioCambiaLaCadenaYPorEndeElPool()
    {
        var sesion = new Session();
        var fabrica = new PostgresConnectionFactory(Base, () => new IdentificacionConexion { Usuario = sesion.Current?.Email, Rol = sesion.Current?.Role.ToString() });
        var sin = fabrica.Create().ConnectionString;
        sesion.Start(new Identity("admin@tesis.local", Role.Admin)); var admin = fabrica.Create().ConnectionString;
        sesion.Start(new Identity("galpon@tesis.local", Role.Galpon)); var galpon = fabrica.Create().ConnectionString;
        sesion.Clear(); var denuevoSin = fabrica.Create().ConnectionString;
        Assert.Equal(3, new[] { sin, admin, galpon }.Distinct().Count());
        Assert.Contains("admin@tesis.local", admin); Assert.DoesNotContain("admin@tesis.local", galpon);
        Assert.Equal(sin, denuevoSin); // no queda rastro del usuario anterior
    }

    [Theory]
    [InlineData("a b", "a\\ b")]
    [InlineData("a\\b", "a\\\\b")]
    [InlineData("x;y='z\"", "x;y='z\"")]
    [InlineData("a\nb", "a\\ b")]
    public void EscapaEspaciosBarrasYCaracteresEspeciales(string usuario, string esperado)
    {
        var cadena = IdentificacionConexion.Aplicar(Base, new IdentificacionConexion { Usuario = usuario });
        Assert.StartsWith("-c app.usuario=" + esperado + " -c app.rol=", Opciones(cadena));
        Assert.Equal("demo", new NpgsqlConnectionStringBuilder(cadena).Database);
    }

    [Fact]
    public void ConservaOptionsPreexistentesYAcotaValoresLargos()
    {
        var cadena = IdentificacionConexion.Aplicar(Base + ";Options=-c search_path=public", new IdentificacionConexion { Usuario = new string('u', 1000) });
        var o = Opciones(cadena);
        Assert.StartsWith("-c search_path=public -c app.usuario=", o);
        Assert.True(o.Length < 400);
    }

    [Fact]
    public void SiLaIdentificacionFallaUsaLaCadenaOriginal()
    {
        var fabrica = new PostgresConnectionFactory(Base, () => throw new InvalidOperationException("sin sesión"));
        Assert.Null(Opciones(fabrica.Create().ConnectionString));
    }

    [Fact]
    public void SinProveedorNoCambiaNada() => Assert.Null(Opciones(new PostgresConnectionFactory(Base).Create().ConnectionString));
}
