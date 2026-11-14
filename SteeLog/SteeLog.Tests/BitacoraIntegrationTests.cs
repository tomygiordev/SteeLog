using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Bitacora;
using SteeLog.Infrastructure.Persistence;
using SteeLog.Infrastructure.Reportes;
using Xunit;

/// <summary>Bitácora de auditoría contra un PostgreSQL real descartable (con F11 aplicada).</summary>
[Trait("Category", "Integration")]
public sealed class BitacoraIntegrationTests
{
    static string ConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_BITACORA_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_BITACORA_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Usar el runner aislado de bitácora.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435) throw new InvalidOperationException("Destino no permitido.");
        using (var c = new NpgsqlConnection(cs)) { c.Open(); using (var q = new NpgsqlCommand("SELECT current_database()", c)) if ((string)q.ExecuteScalar() != db) throw new InvalidOperationException("DB incorrecta."); }
        return cs;
    }
    static PostgresConnectionFactory Factory(Func<IdentificacionConexion> id = null) => new PostgresConnectionFactory(ConnectionString(), id);
    static string Unico() => "t" + Guid.NewGuid().ToString("N").Substring(0, 12);
    static IdentificacionConexion Id(string usuario, string rol = "Admin", string equipo = "PC-1", string version = "1.2.3") =>
        new IdentificacionConexion { Usuario = usuario, Rol = rol, Equipo = equipo, Version = version };
    static NpgsqlParameter P(string k, object v) => new NpgsqlParameter(k, v ?? DBNull.Value);
    static object Sql(string sql, params NpgsqlParameter[] args)
    {
        using (var c = new NpgsqlConnection(ConnectionString())) { c.Open(); using (var q = new NpgsqlCommand(sql, c)) { q.Parameters.AddRange(args); return q.ExecuteScalar(); } }
    }
    static long Cuenta(string usuario) => Convert.ToInt64(Sql("SELECT count(*) FROM public.bitacora_auditoria WHERE usuario=@u", P("u", usuario)));
    static readonly string[] Vacias = { "", "", "", "" };

    // Sin definir la variable, current_setting(.., true) devuelve NULL (o vacío si otra conexión del servidor la definió): ambas son "sin dato" para el trigger (NULLIF).
    static string Txt(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);
    static string[] Variables(PostgresConnectionFactory f)
    {
        using (var c = f.Create())
        {
            c.Open();
            using (var q = new NpgsqlCommand("SELECT current_setting('app.usuario', true), current_setting('app.rol', true), current_setting('app.equipo', true), current_setting('app.version', true)", c))
            using (var r = q.ExecuteReader()) { r.Read(); return new[] { Txt(r, 0), Txt(r, 1), Txt(r, 2), Txt(r, 3) }; }
        }
    }

    [Fact]
    public void LaIdentidadLlegaAlaSesion()
    {
        Assert.Equal(new[] { "ana", "Admin", "PC-1", "1.2.3" }, Variables(Factory(() => Id("ana"))));
    }

    [Fact]
    public void SinSesionLaConexionAbreIgualYLasVariablesVienenVacias()
    {
        Assert.Equal(Vacias, Variables(Factory(() => null)));
        Assert.Equal(Vacias, Variables(Factory()));
        Assert.Equal(Vacias, Variables(Factory(() => new IdentificacionConexion())));
        // Si obtener la identidad falla, se usa la cadena original.
        Assert.Equal(Vacias, Variables(Factory(() => throw new InvalidOperationException("sin sesión"))));
    }

    [Fact]
    public void EquipoConEspaciosAcentosYBarraInvertida()
    {
        foreach (var equipo in new[] { @"PC José\X", @"C:\dir con espacios\", "a  b", "ñandú 'comilla' \"doble\"", @"\\SERVIDOR\recurso" })
            Assert.Equal(equipo, Variables(Factory(() => Id("josé ñ", "Galpon", equipo, "9.0 beta")))[2]);
        Assert.Equal("josé ñ", Variables(Factory(() => Id("josé ñ")))[0]);
        Assert.Equal("9.0 beta", Variables(Factory(() => Id("x", version: "9.0 beta")))[3]);
        // más de 200 caracteres se truncan sin romper la apertura
        Assert.Equal(200, Variables(Factory(() => Id("u", equipo: new string('e', 500))))[2].Length);
    }

    [Fact]
    public void CambiarDeUsuarioConPoolingNuncaVeAlAnterior()
    {
        Assert.Contains("Pooling=true", ConnectionString(), StringComparison.OrdinalIgnoreCase);
        IdentificacionConexion actual = null;
        var f = Factory(() => actual);
        for (int vuelta = 0; vuelta < 3; vuelta++)
        {
            var a = Id("admin_" + vuelta, "Admin"); actual = a;
            Assert.Equal(new[] { a.Usuario, "Admin", "PC-1", "1.2.3" }, Variables(f));
            var g = Id("galpon_" + vuelta, "Galpon"); actual = g;
            Assert.Equal(new[] { g.Usuario, "Galpon", "PC-1", "1.2.3" }, Variables(f));
            actual = null;
            Assert.Equal(Vacias, Variables(f));
        }
    }

    [Fact]
    public async Task AltaModificacionYBajaDeMaterialesQuedanAuditadas()
    {
        var u = "aud_" + Unico(); var equipo = @"PC José\X";
        var f = Factory(() => Id(u, "Galpon", equipo, "2.0.1"));
        int idCat, idMat;
        using (var c = f.Create())
        {
            await c.OpenAsync();
            using (var q = new NpgsqlCommand("INSERT INTO categorias(nombre) VALUES(@n) RETURNING id_categoria", c)) { q.Parameters.AddWithValue("n", Unico()); idCat = Convert.ToInt32(await q.ExecuteScalarAsync()); }
            using (var q = new NpgsqlCommand("INSERT INTO materiales(nombre,id_categoria,stockactual,stockminimo,preciounitario) VALUES(@n,@c,4,5,7.5) RETURNING id", c)) { q.Parameters.AddWithValue("n", "mat " + u); q.Parameters.AddWithValue("c", idCat); idMat = Convert.ToInt32(await q.ExecuteScalarAsync()); }
            using (var q = new NpgsqlCommand("UPDATE materiales SET stockactual=9 WHERE id=@i", c)) { q.Parameters.AddWithValue("i", idMat); await q.ExecuteNonQueryAsync(); }
            using (var q = new NpgsqlCommand("DELETE FROM materiales WHERE id=@i", c)) { q.Parameters.AddWithValue("i", idMat); await q.ExecuteNonQueryAsync(); }
        }
        var res = await new NpgsqlBitacoraConsulta(Factory()).BuscarAsync(new BitacoraFiltro { Usuario = u, Entidad = "materiales" });
        Assert.True(res.Instalada);
        var porAccion = res.Registros.ToDictionary(r => r.Accion);
        Assert.Equal(new[] { "DELETE", "INSERT", "UPDATE" }, porAccion.Keys.OrderBy(x => x).ToArray());
        foreach (var r in res.Registros)
        {
            Assert.Equal("BD", r.Origen); Assert.Equal(u, r.Usuario); Assert.Equal("Galpon", r.Rol);
            Assert.Equal(equipo, r.Equipo); Assert.Equal("2.0.1", r.VersionApp); Assert.Equal(idMat.ToString(), r.EntidadId);
        }
        Assert.Contains("stockactual", porAccion["UPDATE"].DatosAntes); Assert.Contains("stockactual", porAccion["UPDATE"].DatosDespues);
        Assert.Null(porAccion["INSERT"].DatosAntes); Assert.Null(porAccion["DELETE"].DatosDespues);
        // el alta de la categoría también quedó auditada con el mismo usuario
        Assert.Single((await new NpgsqlBitacoraConsulta(Factory()).BuscarAsync(new BitacoraFiltro { Usuario = u, Entidad = "categorias" })).Registros);
        Sql("DELETE FROM categorias WHERE id_categoria=@i", P("i", idCat));
    }

    [Fact]
    public async Task UnaTransaccionDelUnitOfWorkQuedaAuditadaYElRollbackNo()
    {
        var u = "uow_" + Unico(); var nombre = "sec " + u;
        var f = Factory(() => Id(u, "Admin", "PC José\\Y", "3.1"));
        using (var uow = new NpgsqlUnitOfWork(f))
        {
            await uow.BeginAsync();
            await new NpgsqlReporteStockConfiguracion(uow).GuardarAsync(new[] { new ReporteStockSeccion { Nombre = nombre, Materiales = new List<int>() } }, new int[0]);
            await uow.CommitAsync();
        }
        var res = await new NpgsqlBitacoraConsulta(Factory()).BuscarAsync(new BitacoraFiltro { Usuario = u, Entidad = "reporte_stock_secciones" });
        var reg = Assert.Single(res.Registros);
        Assert.Equal("INSERT", reg.Accion); Assert.Equal("Admin", reg.Rol); Assert.Equal("PC José\\Y", reg.Equipo); Assert.Equal("3.1", reg.VersionApp);
        Assert.Contains(nombre, reg.DatosDespues);

        var u2 = "rb_" + Unico();
        using (var uow = new NpgsqlUnitOfWork(Factory(() => Id(u2))))
        {
            await uow.BeginAsync();
            await new NpgsqlReporteStockConfiguracion(uow).GuardarAsync(new[] { new ReporteStockSeccion { Nombre = "rb " + u2, Materiales = new List<int>() } }, new int[0]);
            await uow.RollbackAsync();
        }
        Assert.Equal(0, Cuenta(u2));
        Sql("DELETE FROM reporte_stock_secciones WHERE nombre=@n", P("n", nombre));
    }

    static EventoBitacoraApp Evento(string usuario, string accion = "ERROR", string descripcion = "falló algo", DateTimeOffset? fecha = null) => new EventoBitacoraApp
    {
        Id = Guid.NewGuid(), Fecha = fecha ?? DateTimeOffset.UtcNow, Usuario = usuario, Rol = "Admin", Equipo = @"PC José\X", Version = "1.0",
        Accion = accion, Entidad = "App", Descripcion = descripcion
    };

    [Fact]
    public async Task EscrituraInsertaEventoAppYNoDuplicaAlReinsertar()
    {
        var u = "app_" + Unico(); var escritura = new NpgsqlBitacoraEscritura(Factory());
        var e = Evento(u);
        var hechos = await escritura.InsertarAsync(new[] { e });
        Assert.Equal(new[] { e.Id }, hechos.ToArray());
        Assert.Equal(1, Cuenta(u));
        hechos = await escritura.InsertarAsync(new[] { e });
        Assert.Equal(new[] { e.Id }, hechos.ToArray()); // se considera hecho, pero no se duplica
        Assert.Equal(1, Cuenta(u));
        var r = Assert.Single((await new NpgsqlBitacoraConsulta(Factory()).BuscarAsync(new BitacoraFiltro { Usuario = u })).Registros);
        Assert.Equal("APP", r.Origen); Assert.Equal("ERROR", r.Accion); Assert.Equal(@"PC José\X", r.Equipo); Assert.Equal("falló algo", r.Descripcion);
        Assert.Empty(await escritura.InsertarAsync(new EventoBitacoraApp[0]));
        Assert.Empty(await escritura.InsertarAsync(null));
    }

    [Fact]
    public async Task EscrituraConLoteParcialmenteRepetidoInsertaSoloLoNuevo()
    {
        var u = "lote_" + Unico(); var escritura = new NpgsqlBitacoraEscritura(Factory());
        var a = Evento(u, descripcion: "a"); var b = Evento(u, descripcion: "b");
        await escritura.InsertarAsync(new[] { a });
        var hechos = await escritura.InsertarAsync(new[] { a, b });
        Assert.Equal(2, hechos.Count); Assert.Equal(2, Cuenta(u));
    }

    [Fact]
    public async Task ConsultaFiltraPorUsuarioEntidadAccionFechasYTexto()
    {
        var u = "q_" + Unico(); var otro = "q2_" + Unico(); var esc = new NpgsqlBitacoraEscritura(Factory());
        var hoy = DateTimeOffset.UtcNow;
        var viejo = Evento(u, "ERROR", "viejo 100% real", hoy.AddDays(-10));
        var e1 = Evento(u, "ERROR", "falla 100% real"); var e2 = Evento(u, "AVISO", "falla 100X real");
        var e3 = Evento(u, "AVISO", "guion_bajo literal"); var e4 = Evento(u, "AVISO", "guionXbajo");
        var e5 = Evento(otro, "ERROR", "falla 100% real");
        await esc.InsertarAsync(new[] { viejo, e1, e2, e3, e4, e5 });
        var q = new NpgsqlBitacoraConsulta(Factory());
        Func<Action<BitacoraFiltro>, Task<List<BitacoraRegistro>>> buscar = async cfg => { var fl = new BitacoraFiltro { Usuario = u }; cfg(fl); return (await q.BuscarAsync(fl)).Registros; };

        Assert.Equal(5, (await buscar(f => { })).Count);
        Assert.Equal(2, (await buscar(f => f.Accion = "ERROR")).Count);
        Assert.Equal(5, (await buscar(f => f.Entidad = "App")).Count);
        Assert.Empty(await buscar(f => f.Entidad = "Otra"));
        var hoyLocal = DateTime.Now.Date;
        Assert.Equal(4, (await buscar(f => f.Desde = hoyLocal)).Count);
        Assert.Equal(4, (await buscar(f => { f.Desde = hoyLocal; f.Hasta = hoyLocal; })).Count);
        Assert.Empty(await buscar(f => f.Desde = hoyLocal.AddDays(1)));
        Assert.Equal("viejo 100% real", Assert.Single(await buscar(f => f.Hasta = hoyLocal.AddDays(-5))).Descripcion);
        // '%' y '_' se toman como literales
        Assert.Equal(new[] { "falla 100% real", "viejo 100% real" }, (await buscar(f => f.Texto = "100%")).Select(r => r.Descripcion).OrderBy(x => x).ToArray());
        Assert.Equal("guion_bajo literal", Assert.Single(await buscar(f => f.Texto = "guion_bajo")).Descripcion);
        Assert.Equal(5, (await buscar(f => f.Texto = "  ")).Count);
        Assert.Equal(2, (await buscar(f => f.Texto = "FALLA")).Count); // sin distinguir mayúsculas
        Assert.Single((await q.BuscarAsync(new BitacoraFiltro { Usuario = otro })).Registros);
        var orden = await buscar(f => { });
        Assert.Equal(orden.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.Id).Select(r => r.Id), orden.Select(r => r.Id));
    }

    [Fact]
    public async Task ConsultaRespetaElLimiteYMarcaTruncado()
    {
        var u = "lim_" + Unico(); var esc = new NpgsqlBitacoraEscritura(Factory());
        await esc.InsertarAsync(Enumerable.Range(0, 5).Select(i => Evento(u, descripcion: "d" + i, fecha: DateTimeOffset.UtcNow.AddSeconds(-i))).ToList());
        var q = new NpgsqlBitacoraConsulta(Factory());
        var corto = await q.BuscarAsync(new BitacoraFiltro { Usuario = u, Limite = 2 });
        Assert.True(corto.Truncado); Assert.Equal(2, corto.Registros.Count);
        Assert.Equal(new[] { "d0", "d1" }, corto.Registros.Select(r => r.Descripcion).ToArray());
        var justo = await q.BuscarAsync(new BitacoraFiltro { Usuario = u, Limite = 5 });
        Assert.False(justo.Truncado); Assert.Equal(5, justo.Registros.Count);
        var amplio = await q.BuscarAsync(new BitacoraFiltro { Usuario = u });
        Assert.False(amplio.Truncado); Assert.Equal(5, amplio.Registros.Count);
    }

    [Fact]
    public async Task OpcionesListaValoresDistintos()
    {
        var u = "op_" + Unico(); var u2 = "op2_" + Unico();
        await new NpgsqlBitacoraEscritura(Factory()).InsertarAsync(new[] { Evento(u, "ACCION_X"), Evento(u, "ACCION_X"), Evento(u2, "ACCION_Y") });
        var o = await new NpgsqlBitacoraConsulta(Factory()).OpcionesAsync();
        Assert.True(o.Instalada); Assert.Null(o.Mensaje);
        Assert.Single(o.Usuarios, u); Assert.Contains(u2, o.Usuarios);
        Assert.Single(o.Acciones, "ACCION_X"); Assert.Contains("ACCION_Y", o.Acciones); Assert.Contains("App", o.Entidades);
    }

    [Fact]
    public async Task ConLaTablaAusenteLaConsultaDiceNoInstaladaYLaEscrituraConservaElEvento()
    {
        var u = "aus_" + Unico(); var e = Evento(u);
        Sql("ALTER TABLE public.bitacora_auditoria RENAME TO bitacora_auditoria_oculta");
        try
        {
            var q = new NpgsqlBitacoraConsulta(Factory());
            var r = await q.BuscarAsync(new BitacoraFiltro());
            Assert.False(r.Instalada); Assert.Empty(r.Registros); Assert.Equal(ConsultarBitacora.MensajeNoInstalada, r.Mensaje);
            var o = await q.OpcionesAsync();
            Assert.False(o.Instalada); Assert.Empty(o.Usuarios);
            var hechos = await new NpgsqlBitacoraEscritura(Factory()).InsertarAsync(new[] { e });
            Assert.Empty(hechos); // no figura como hecho: la cola lo conserva para reintentar
        }
        finally { Sql("ALTER TABLE public.bitacora_auditoria_oculta RENAME TO bitacora_auditoria"); }
        var reintento = await new NpgsqlBitacoraEscritura(Factory()).InsertarAsync(new[] { e });
        Assert.Equal(new[] { e.Id }, reintento.ToArray());
        Assert.Equal(1, Cuenta(u));
    }
}
