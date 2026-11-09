using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Reportes;
using SteeLog.Domain.Inventario;
using SteeLog.Domain.Reportes;
using SteeLog.Infrastructure.Canaletas;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Pdf;
using SteeLog.Infrastructure.Persistence;
using SteeLog.Infrastructure.Reportes;
using Xunit;

[Trait("Category", "Integration")]
public sealed class ReportesIntegrationTests
{
    static string ConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_F8_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_F8_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Usar runner aislado F8.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435) throw new InvalidOperationException("Destino no permitido.");
        using (var c = new NpgsqlConnection(cs)) { c.Open(); using (var q = new NpgsqlCommand("SELECT current_database()", c)) if ((string)q.ExecuteScalar() != db) throw new InvalidOperationException("DB incorrecta."); }
        return cs;
    }
    static PostgresConnectionFactory Factory() => new PostgresConnectionFactory(ConnectionString());
    static Session Actor(Role role = Role.Admin) { var s = new Session(); if (role != Role.None) s.Start(new Identity("prueba@example.invalid", role)); return s; }
    static NpgsqlParameter P(string key, object value) => new NpgsqlParameter(key, value);
    static object Sql(string sql, params NpgsqlParameter[] args)
    {
        using (var c = new NpgsqlConnection(ConnectionString())) { c.Open(); using (var q = new NpgsqlCommand(sql, c)) { q.CommandTimeout = 15; q.Parameters.AddRange(args); return q.ExecuteScalar(); } }
    }
    static int Id(string sql, params NpgsqlParameter[] args) => Convert.ToInt32(Sql(sql, args));
    static string Unique() => "f8-" + Guid.NewGuid().ToString("N");
    static int Material(string nombre, decimal stock = 4, decimal minimo = 5)
    {
        var categoria = Id("INSERT INTO categorias(nombre) VALUES(@n) RETURNING id_categoria", P("n", Unique()));
        return Id("INSERT INTO materiales(nombre,id_categoria,stockactual,stockminimo,preciounitario) VALUES(@n,@c,@s,@m,7.50) RETURNING id", P("n", nombre), P("c", categoria), P("s", stock), P("m", minimo));
    }
    static ReporteStockSeccion Seccion(string nombre, params int[] materiales) => new ReporteStockSeccion { Nombre = nombre, Materiales = materiales.ToList() };
    static Task Guardar(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas = null, Role role = Role.Admin, CancellationToken token = default, Func<IReporteStockConfiguracion, IReporteStockConfiguracion> wrap = null)
        => GuardarInterno(secciones, eliminadas ?? new int[0], role, token, wrap);
    static async Task GuardarInterno(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, Role role, CancellationToken token, Func<IReporteStockConfiguracion, IReporteStockConfiguracion> wrap)
    {
        using (var uow = new NpgsqlUnitOfWork(Factory()))
        {
            IReporteStockConfiguracion repo = new NpgsqlReporteStockConfiguracion(uow);
            await new GuardarReporteStockConfiguracion(Actor(role), uow, wrap == null ? repo : wrap(repo)).EjecutarAsync(secciones, eliminadas, token);
        }
    }
    static NpgsqlReporteStockConsulta Stock() => new NpgsqlReporteStockConsulta(Factory());

    private sealed class TestRecursosPdf : IRecursosPdf
    {
        private static readonly byte[] LogoPngValido = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");
        public MarcaPdf Capturar() => new MarcaPdf
        {
            LogoPng = LogoPngValido,
            FechaLocal = new DateTime(2026, 10, 15, 10, 0, 0),
            Pie = "SteeLog - Sistema de Gestión de Inventario"
        };
    }

    private static string ExtraerTextoPdf(string ruta)
    {
        var asm = Assembly.Load("itextsharp");
        var readerType = asm.GetType("iTextSharp.text.pdf.PdfReader");
        var extractorType = asm.GetType("iTextSharp.text.pdf.parser.PdfTextExtractor");
        using (var reader = (IDisposable)Activator.CreateInstance(readerType, ruta))
        {
            var numPages = (int)readerType.GetProperty("NumberOfPages").GetValue(reader);
            var getTextMethod = extractorType.GetMethod("GetTextFromPage", new[] { readerType, typeof(int) });
            var sb = new System.Text.StringBuilder();
            for (int p = 1; p <= numPages; p++)
                sb.AppendLine((string)getTextMethod.Invoke(null, new object[] { reader, p }));
            return sb.ToString();
        }
    }

    [Fact]
    public async Task KardexConservaCantidadSaldoPersistidoYFiltros()
    {
        var nombre = Unique(); var material = Material(nombre); var obra = Id("INSERT INTO obras(nombre) VALUES(@n) RETURNING id", P("n", Unique()));
        var tipo = Id("INSERT INTO tipo_movimiento(nombre) VALUES(@n) RETURNING id_tipomovimiento", P("n", "Envío " + Unique()));
        var fecha = new DateTime(2026, 10, 15);
        int movimiento = Id("INSERT INTO movimientos(fecha,nroremito,id_origen,id_destino,tipo_destino,tipo_origen,id_tipomovimiento,id_obra,costoflete) VALUES(@f,@n,1,@o,'OBRA','UBICACION',@t,@o,3) RETURNING id_movimiento", P("f", fecha), P("n", Unique()), P("o", obra), P("t", tipo));
        Sql("INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual) VALUES(@v,@m,2.1250,8.25)", P("v", movimiento), P("m", material));
        var caso = new ConsultarKardex(Actor(), new NpgsqlKardexConsulta(Factory()));
        var row = Assert.Single(await caso.HistorialAsync(material, fecha, fecha, default));
        Assert.Equal(movimiento, row.id_movimiento);
        Assert.Equal(2.125m, row.cantidad);
        Assert.Equal(8.25m, row.saldo);
        Assert.Equal(nombre, row.nombre_material);
        Assert.Equal("OBRA", row.tipo_destino);
        Assert.Equal(ImpactoStock.Salida, row.impacto);
        Assert.Equal(-2.125m, row.delta);
        Assert.Empty(await caso.HistorialAsync(material, fecha.AddDays(1), null, default));
        Assert.Contains(await caso.MaterialesAsync(default), x => x.id == material);
        var anonimo = new ConsultarKardex(Actor(Role.None), new NpgsqlKardexConsulta(Factory()));
        Assert.Throws<UnauthorizedAccessException>(() => { _ = anonimo.MaterialesAsync(default); });
    }

    [Fact]
    public async Task KardexSinSnapshotConservaClasificacionYAcumulacionDescendente()
    {
        int material = Material(Unique()), obra = Id("INSERT INTO obras(nombre) VALUES(@n) RETURNING id", P("n", Unique()));
        int noCanonico = Id("INSERT INTO tipo_movimiento(nombre) VALUES(@n) RETURNING id_tipomovimiento", P("n", "APORTE " + Unique()));
        int entrada = Id("INSERT INTO tipo_movimiento(nombre) VALUES(@n) RETURNING id_tipomovimiento", P("n", "ENTRADA " + Unique()));
        int salida = Id("INSERT INTO tipo_movimiento(nombre) VALUES(@n) RETURNING id_tipomovimiento", P("n", "ENVÍO " + Unique()));
        foreach (var fila in new[] { new { Tipo = salida, Fecha = new DateTime(2026, 10, 10), Cantidad = 1m }, new { Tipo = noCanonico, Fecha = new DateTime(2026, 10, 11), Cantidad = 3m }, new { Tipo = entrada, Fecha = new DateTime(2026, 10, 12), Cantidad = 5m } })
        {
            int mov = Id("INSERT INTO movimientos(fecha,nroremito,id_origen,id_destino,tipo_destino,tipo_origen,id_tipomovimiento) VALUES(@f,@n,1,@o,'OBRA','UBICACION',@t) RETURNING id_movimiento", P("f", fila.Fecha), P("n", Unique()), P("o", obra), P("t", fila.Tipo));
            Sql("INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual) VALUES(@v,@m,@q,NULL)", P("v", mov), P("m", material), P("q", fila.Cantidad));
        }
        var rows = await new NpgsqlKardexConsulta(Factory()).HistorialAsync(material, null, null);
        Assert.Equal(new[] { 5m, 2m, 1m }, rows.Select(x => x.saldo));
        Assert.True(rows[0].fecha > rows[1].fecha);
    }

    [Fact]
    public async Task KardexConsultaSnapshotRenderer_EnvioPositivo_CalculaEntradasCeroYSalidasNegativasEnPdf()
    {
        var nombre = Unique();
        var material = Material(nombre, stock: 8, minimo: 5);
        var obra = Id("INSERT INTO obras(nombre) VALUES(@n) RETURNING id", P("n", Unique()));
        var tipo = Id("INSERT INTO tipo_movimiento(nombre) VALUES(@n) RETURNING id_tipomovimiento", P("n", "ENVÍO " + Unique()));
        var fecha = new DateTime(2026, 10, 15);
        int movimiento = Id("INSERT INTO movimientos(fecha,nroremito,id_origen,id_destino,tipo_destino,tipo_origen,id_tipomovimiento,id_obra,costoflete) VALUES(@f,@n,1,@o,'OBRA','UBICACION',@t,@o,3) RETURNING id_movimiento", P("f", fecha), P("n", Unique()), P("o", obra), P("t", tipo));
        Sql("INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual) VALUES(@v,@m,2,8)", P("v", movimiento), P("m", material));

        var consulta = new NpgsqlKardexConsulta(Factory());
        var items = await consulta.HistorialAsync(material, fecha, fecha, default);
        var row = Assert.Single(items);

        // Contrato legacy: cantidad original positiva exacta (+2), saldo persistido exacto (+8)
        Assert.Equal(movimiento, row.id_movimiento);
        Assert.Equal(2m, row.cantidad);
        Assert.Equal(8m, row.saldo);
        Assert.Equal("OBRA", row.tipo_destino);
        Assert.Equal(ImpactoStock.Salida, row.impacto);
        Assert.Equal(-2m, row.delta);

        // Snapshot puro: resumen calculado por impacto de negocio (entradas 0, salidas -2, stock actual 8)
        var snapshot = new KardexSnapshot
        {
            Historial = items,
            MaterialNombre = nombre,
            FechaInicio = fecha,
            FechaFin = fecha,
            MostrarColumnaMaterial = true,
            MostrarColumnaSaldo = true
        };
        Assert.Equal(0m, snapshot.Resumen.TotalEntradas);
        Assert.Equal(-2m, snapshot.Resumen.TotalSalidas);
        Assert.Equal(8m, snapshot.Resumen.StockFinal);

        // Renderer real KardexPDF genera documento y libera Document y FileStream
        string rutaPdf = Path.Combine(Path.GetTempPath(), "kardex-integracion-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            new KardexPDF(new TestRecursosPdf()).Generar(rutaPdf, snapshot);
            Assert.True(File.Exists(rutaPdf));

            var texto = ExtraerTextoPdf(rutaPdf);
            Assert.Contains("DETALLE DE MOVIMIENTOS", texto);
            Assert.Contains("RESUMEN DEL PER", texto);
            Assert.Contains("Total Entradas:", texto);
            Assert.Contains("Total Salidas:", texto);
            Assert.Contains("+0", texto);
            Assert.Contains("-2", texto);
            Assert.Contains("8,00", texto);

            using (var fs = new FileStream(rutaPdf, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.True(fs.CanWrite);
            }
        }
        finally
        {
            if (File.Exists(rutaPdf)) File.Delete(rutaPdf);
        }
    }

    [Fact]
    public async Task InformeComponeMovimientosCanaletasFechasYValorizacion()
    {
        string nombre = Unique(); int obra = Id("INSERT INTO obras(nombre) VALUES(@n) RETURNING id", P("n", nombre));
        int material = Material(Unique()), tipo = Id("INSERT INTO tipo_movimiento(nombre) VALUES(@n) RETURNING id_tipomovimiento", P("n", "Aporte " + Unique()));
        var fecha = new DateTime(2026, 10, 16);
        int mov = Id("INSERT INTO movimientos(fecha,nroremito,id_origen,id_destino,tipo_destino,tipo_origen,id_tipomovimiento,id_obra,costoflete) VALUES(@f,@n,1,@o,'OBRA','UBICACION',@t,@o,3) RETURNING id_movimiento", P("f", fecha), P("n", Unique()), P("o", obra), P("t", tipo));
        Sql("INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual) VALUES(@v,@m,2,4)", P("v", mov), P("m", material));
        Sql("INSERT INTO canaletas_plegados(id_obra,descripcion,precio,pagado,fecha_creacion) VALUES(@o,'Canaleta F8',10,false,@f)", P("o", obra), P("f", fecha));
        var f = Factory(); var consulta = new NpgsqlInformeObraConsulta(f);
        var caso = new ConsultarInformeObra(Actor(), consulta, new NpgsqlCanaletasConsulta(f));
        var snapshot = await caso.SnapshotAsync(obra, nombre, fecha, fecha, default);
        Assert.Single(snapshot.Movimientos); Assert.Single(snapshot.Canaletas);
        Assert.Equal(28m, snapshot.Resumen.TotalPendiente); Assert.Equal(3m, snapshot.Resumen.TotalFletes);
        Assert.Equal(fecha, await caso.PrimeraFechaRemitoAsync(obra, default));
        Assert.Empty((await caso.SnapshotAsync(obra, nombre, fecha.AddDays(1), null, default)).Movimientos);
        var galpon = await new ConsultarInformeObra(Actor(Role.Galpon), consulta, new NpgsqlCanaletasConsulta(f)).SnapshotAsync(obra, nombre, null, null, default);
        Assert.False(galpon.MostrarValorizacion); Assert.Equal(0, galpon.Resumen.TotalPendiente);
    }

    [Fact]
    public async Task InformeActivosConservaFechasYPertenenciaHistorica()
    {
        string nombre = Unique(); int obra = Id("INSERT INTO obras(nombre) VALUES(@n) RETURNING id", P("n", nombre));
        int herramienta = Id("INSERT INTO herramientas(nombre,estado,id_obra) VALUES(@n,'Regular',@o) RETURNING id_herramienta", P("n", Unique()), P("o", obra));
        int insumo = Id("INSERT INTO insumos(nombre,id_obra,consumido) VALUES(@n,@o,false) RETURNING id_insumo", P("n", Unique()), P("o", obra));
        var entrada = new DateTime(2026, 10, 17, 12, 0, 0); var salida = entrada.AddDays(1);
        Sql("INSERT INTO herramienta_movimientos(id_herramienta,obra_origen,obra_destino,fecha) VALUES(@h,'Galpón',@n,@f)", P("h", herramienta), P("n", nombre), P("f", entrada));
        Sql("INSERT INTO insumo_movimientos(id_insumo,obra_origen,obra_destino,fecha) VALUES(@i,'Galpón',@n,@f)", P("i", insumo), P("n", nombre), P("f", entrada));
        var consulta = new NpgsqlInformeObraConsulta(Factory());
        var h = Assert.Single(await consulta.HerramientasAsync(obra, nombre, default));
        var i = Assert.Single(await consulta.InsumosAsync(obra, nombre, default));
        Assert.True(h.en_obra); Assert.True(i.en_obra); Assert.Equal(entrada, h.fecha_ingreso); Assert.Equal(entrada, i.fecha_ingreso); Assert.Null(h.fecha_retorno);
        Sql("UPDATE herramientas SET id_obra=NULL WHERE id_herramienta=@h; INSERT INTO herramienta_movimientos(id_herramienta,obra_origen,obra_destino,fecha) VALUES(@h,@n,'Galpón',@f)", P("h", herramienta), P("n", nombre), P("f", salida));
        Sql("UPDATE insumos SET id_obra=NULL,consumido=true WHERE id_insumo=@i; INSERT INTO insumo_movimientos(id_insumo,obra_origen,obra_destino,fecha) VALUES(@i,@n,'Galpón',@f)", P("i", insumo), P("n", nombre), P("f", salida));
        h = Assert.Single(await consulta.HerramientasAsync(obra, nombre, default));
        i = Assert.Single(await consulta.InsumosAsync(obra, nombre, default));
        Assert.False(h.en_obra); Assert.False(i.en_obra); Assert.True(i.consumido); Assert.Equal(salida, h.fecha_retorno); Assert.Equal(salida, i.fecha_retorno);
    }

    [Fact]
    public async Task StockConfiguradoPreservaOrdenFiltrosYBajaDeSeccion()
    {
        string nombre = Unique(); int a = Material(nombre + "-a"), b = Material(nombre + "-b", 10, 5);
        await Guardar(new[] { Seccion(nombre, b, a) });
        var query = Stock(); var seccion = Assert.Single(await query.SeccionesAsync(), x => x.Nombre == nombre);
        Assert.Equal(new[] { b, a }, seccion.Materiales);
        var rows = await query.ListarConfiguradosAsync(new FiltroMaterialLectura { Nombre = nombre });
        Assert.Equal(new[] { b, a }, rows.Select(x => x.Material.Id));
        Assert.Equal(a, Assert.Single(await query.ListarConfiguradosAsync(new FiltroMaterialLectura { Nombre = nombre, SoloStockBajo = true })).Material.Id);
        await Guardar(new[] { new ReporteStockSeccion { Id = seccion.Id, Nombre = nombre + "-editada", Materiales = new List<int> { a } } });
        Assert.Equal(a, Assert.Single(await query.ListarConfiguradosAsync(new FiltroMaterialLectura { Nombre = nombre })).Material.Id);
        await Guardar(new ReporteStockSeccion[0], new[] { seccion.Id });
        Assert.DoesNotContain(await query.SeccionesAsync(), x => x.Id == seccion.Id);
    }

    [Fact]
    public async Task ConfiguracionRechazaPermisosDuplicadosYRevierteFkReal()
    {
        string nombre = Unique(); int material = Material(Unique());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Guardar(new[] { Seccion(nombre, material) }, role: Role.Galpon));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Guardar(new[] { Seccion(nombre, material, material) }));
        var error = await Assert.ThrowsAsync<PostgresException>(() => Guardar(new[] { Seccion(nombre, material, int.MaxValue) }));
        Assert.Equal("23503", error.SqlState);
        Assert.DoesNotContain(await Stock().SeccionesAsync(), x => x.Nombre == nombre);
    }

    sealed class Interceptor : IReporteStockConfiguracion
    {
        readonly IReporteStockConfiguracion inner; readonly Action despues;
        public Interceptor(IReporteStockConfiguracion inner, Action despues) { this.inner = inner; this.despues = despues; }
        public async Task GuardarAsync(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, CancellationToken token = default)
        { await inner.GuardarAsync(secciones, eliminadas, token); despues(); }
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FalloOCancelacionTrasSqlRealRevierteConfiguracion(bool fallo)
    {
        string nombre = Unique(); int material = Material(Unique());
        await Guardar(new[] { Seccion(nombre, material) });
        var previa = Assert.Single(await Stock().SeccionesAsync(), x => x.Nombre == nombre);
        using (var cts = new CancellationTokenSource())
        {
            var nueva = new[] { new ReporteStockSeccion { Id = previa.Id, Nombre = nombre + "-nuevo", Materiales = new List<int>() } };
            Func<IReporteStockConfiguracion, IReporteStockConfiguracion> wrap = r => new Interceptor(r, () => { if (fallo) throw new InvalidOperationException("fallo posterior SQL"); cts.Cancel(); });
            if (fallo) await Assert.ThrowsAsync<InvalidOperationException>(() => Guardar(nueva, token: cts.Token, wrap: wrap));
            else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Guardar(nueva, token: cts.Token, wrap: wrap));
        }
        var actual = Assert.Single(await Stock().SeccionesAsync(), x => x.Id == previa.Id);
        Assert.Equal(nombre, actual.Nombre); Assert.Equal(new[] { material }, actual.Materiales);
    }

    [Fact]
    public async Task SinConfiguracionUsaInventarioConFiltroStockBajo()
    {
        Sql("DELETE FROM reporte_stock_secciones");
        string nombre = Unique(); int bajo = Material(nombre + "-bajo"), alto = Material(nombre + "-alto", 8, 2);
        var f = Factory();
        var caso = new ConsultarReporteStock(Actor(), new NpgsqlReporteStockConsulta(f), new NpgsqlMaterialesConsulta(f));
        Assert.Empty(await caso.ListarAsync(new FiltroMaterialLectura { Nombre = nombre, SoloStockBajo = true }));
        int obra = Id("INSERT INTO obras(nombre) VALUES(@n) RETURNING id", P("n", Unique()));
        int mov = Id("INSERT INTO movimientos(fecha,nroremito,id_origen,id_destino,tipo_destino,tipo_origen,id_tipomovimiento) VALUES(CURRENT_DATE,@n,1,@o,'OBRA','UBICACION',2) RETURNING id_movimiento", P("n", Unique()), P("o", obra));
        Sql("INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual) VALUES(@v,@m,1,4)", P("v", mov), P("m", bajo));
        var rows = await caso.ListarAsync(new FiltroMaterialLectura { Nombre = nombre, SoloStockBajo = true });
        Assert.Equal(bajo, Assert.Single(rows).Material.Id); Assert.DoesNotContain(rows, x => x.Material.Id == alto);
        Assert.False(await Stock().TieneConfiguracionAsync());
    }

    [Fact]
    public async Task ConfiguracionesConcurrentesNoMezclanAsignaciones()
    {
        string nombre = Unique(); int a = Material(Unique()), b = Material(Unique());
        await Guardar(new[] { Seccion(nombre, a) });
        var previa = Assert.Single(await Stock().SeccionesAsync(), x => x.Nombre == nombre);
        using (var c = new NpgsqlConnection(ConnectionString()))
        {
            c.Open(); using (var tx = c.BeginTransaction())
            {
                using (var cmd = new NpgsqlCommand("LOCK TABLE reporte_stock_secciones IN SHARE ROW EXCLUSIVE MODE", c, tx)) cmd.ExecuteNonQuery();
                var primera = Guardar(new[] { new ReporteStockSeccion { Id = previa.Id, Nombre = nombre, Materiales = new List<int> { a, b } } });
                var segunda = Guardar(new[] { new ReporteStockSeccion { Id = previa.Id, Nombre = nombre, Materiales = new List<int> { b, a } } });
                try
                {
                    var limite = DateTime.UtcNow.AddSeconds(15); int esperas;
                    do { esperas = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE 'LOCK TABLE reporte_stock_secciones%'")); if (esperas >= 2) break; await Task.Delay(50); } while (DateTime.UtcNow < limite);
                    Assert.True(esperas >= 2);
                }
                finally { tx.Commit(); }
                await Task.WhenAll(primera, segunda);
            }
        }
        var actual = Assert.Single(await Stock().SeccionesAsync(), x => x.Id == previa.Id);
        Assert.True(actual.Materiales.SequenceEqual(new[] { a, b }) || actual.Materiales.SequenceEqual(new[] { b, a }));
    }

    [Fact]
    public async Task ConfiguracionEsperaBloqueoRealAntesDeEscribir()
    {
        string nombre = Unique(); int material = Material(Unique());
        using (var c = new NpgsqlConnection(ConnectionString()))
        {
            c.Open(); using (var tx = c.BeginTransaction())
            {
                using (var cmd = new NpgsqlCommand("LOCK TABLE reporte_stock_secciones IN SHARE ROW EXCLUSIVE MODE", c, tx)) cmd.ExecuteNonQuery();
                var guardar = Guardar(new[] { Seccion(nombre, material) });
                try
                {
                    var limite = DateTime.UtcNow.AddSeconds(15); bool observado = false;
                    do { observado = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE 'LOCK TABLE reporte_stock_secciones%'") ) > 0; if (observado) break; await Task.Delay(50); } while (DateTime.UtcNow < limite);
                    Assert.True(observado); Assert.False(guardar.IsCompleted);
                }
                finally { tx.Commit(); }
                await guardar;
            }
        }
        Assert.Single(await Stock().SeccionesAsync(), x => x.Nombre == nombre);
    }
}
