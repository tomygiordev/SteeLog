using SteeLog.Core.Application.Activos;
using SteeLog.Infrastructure.Activos;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Infrastructure.OrdenesCompra;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Movimientos;

using System;
using System.Text.RegularExpressions;
using Npgsql;
using SteeLog.Infrastructure.Persistence;
using Xunit;

[Trait("Category", "Integration")]
public sealed class InventarioIntegrationTests
{
    private static string ConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_INVENTARIO_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_INVENTARIO_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$"))
            throw new InvalidOperationException("Ejecutar scripts/tests/inventario-integracion.ps1 con DB efímera.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435)
            throw new InvalidOperationException("Destino de pruebas no permitido.");
        using (var c = new NpgsqlConnection(cs)) {
            c.Open(); using (var cmd = new NpgsqlCommand("SELECT current_database()", c))
                if ((string)cmd.ExecuteScalar() != db) throw new InvalidOperationException("Identidad de DB inesperada.");
        }
        return cs;
    }

    [Fact]
    public void RelojNegocioUsaArgentinaAunqueServidorSeaUtc()
    {
        var utc = new DateTimeOffset(2025, 1, 2, 1, 0, 0, TimeSpan.Zero);
        var argentina = TimeZoneInfo.CreateCustomTimeZone("Argentina test", TimeSpan.FromHours(-3), "Argentina test", "Argentina test");
        var clock = new BusinessClock(argentina, () => utc);
        using (var c = new NpgsqlConnection(ConnectionString())) {
            c.Open(); using (var cmd = new NpgsqlCommand("SELECT current_setting('TimeZone'), (@utc::timestamptz AT TIME ZONE 'UTC')::date", c)) {
                cmd.Parameters.AddWithValue("utc", utc.UtcDateTime);
                using (var reader = cmd.ExecuteReader()) {
                    Assert.True(reader.Read()); Assert.Contains(reader.GetString(0), new[] { "UTC", "Etc/UTC" });
                    Assert.Equal(new DateTime(2025, 1, 2), reader.GetDateTime(1));
                }
            }
        }
        Assert.Equal(new DateTime(2025, 1, 1), clock.FechaNegocio);
    }

    [Theory]
    [InlineData("1.2345", "1.23")]
    [InlineData("1.2350", "1.24")]
    public void PrecisionDeDetallesYSaldosEsLaDePostgres(string cantidad, string saldo)
    {
        using (var c = new NpgsqlConnection(ConnectionString())) {
            c.Open(); using (var cmd = new NpgsqlCommand("SELECT CAST(@valor AS numeric(10,4)), CAST(@valor AS numeric(10,2))", c)) {
                cmd.Parameters.AddWithValue("valor", decimal.Parse(cantidad, System.Globalization.CultureInfo.InvariantCulture));
                using (var reader = cmd.ExecuteReader()) {
                    Assert.True(reader.Read());
                    Assert.Equal(decimal.Parse(cantidad, System.Globalization.CultureInfo.InvariantCulture), reader.GetDecimal(0));
                    Assert.Equal(decimal.Parse(saldo, System.Globalization.CultureInfo.InvariantCulture), reader.GetDecimal(1));
                }
            }
        }
    }
    [Fact]
    public void MigracionIdempotenteConservaNulosYExigeEstadosSiSnapshotCompleto()
    {
        using (var c = new NpgsqlConnection(ConnectionString())) {
            c.Open(); using (var tx = c.BeginTransaction()) {
                using (var cmd = new NpgsqlCommand("INSERT INTO public.insumo_movimientos(obra_origen,obra_destino) VALUES ('origen','destino') RETURNING snapshot_completo,id_obra_origen_snapshot,consumido_origen_snapshot", c, tx))
                using (var reader = cmd.ExecuteReader()) {
                    Assert.True(reader.Read()); Assert.False(reader.GetBoolean(0)); Assert.True(reader.IsDBNull(1)); Assert.True(reader.IsDBNull(2));
                }
                using (var cmd = new NpgsqlCommand("SELECT numeric_precision,numeric_scale,is_nullable FROM information_schema.columns WHERE table_schema='public' AND table_name='movimientomaterial' AND column_name='impacto_stock_real'", c, tx))
                using (var reader = cmd.ExecuteReader()) {
                    Assert.True(reader.Read()); Assert.Equal(10, Convert.ToInt32(reader.GetValue(0))); Assert.Equal(2, Convert.ToInt32(reader.GetValue(1))); Assert.Equal("YES", reader.GetString(2)); Assert.False(reader.Read());
                }
                tx.Rollback();
            }
            using (var tx = c.BeginTransaction()) {
                using (var cmd = new NpgsqlCommand("INSERT INTO public.insumo_movimientos(obra_origen,obra_destino,snapshot_completo) VALUES ('origen','destino',true)", c, tx)) {
                    var error = Assert.Throws<PostgresException>(() => cmd.ExecuteNonQuery()); Assert.Equal("23514", error.SqlState);
                }
                tx.Rollback();
            }
        }
    }
    private static object Sql(string sql, params NpgsqlParameter[] args)
    {
        using (var c = new NpgsqlConnection(ConnectionString())) { c.Open(); using (var cmd = new NpgsqlCommand(sql, c)) { cmd.CommandTimeout = 10; cmd.Parameters.AddRange(args); return cmd.ExecuteScalar(); } }
    }
    private static int Material(decimal stock) => Convert.ToInt32(Sql("INSERT INTO public.materiales(nombre,preciounitario,stockactual,stockminimo,id_categoria) VALUES (@nombre,0,@stock,0,1) RETURNING id", new NpgsqlParameter("nombre", Guid.NewGuid().ToString("N")), new NpgsqlParameter("stock", stock)));
    private static decimal Stock(int id) => Convert.ToDecimal(Sql("SELECT stockactual FROM public.materiales WHERE id=@id", new NpgsqlParameter("id", id)));
    private static Session Actor(Role role = Role.Admin) { var s = new Session(); if (role != Role.None) s.Start(new Identity("test@example.invalid", role)); return s; }
    private static MovimientoSolicitud Solicitud(int material, int tipo, params decimal[] cantidades) => new MovimientoSolicitud {
        Cabecera = new CabeceraMovimiento { Fecha = new DateTime(2025, 1, 1), Remito = Guid.NewGuid().ToString("N"), OrigenId = 2, TipoOrigen = "UBICACION", DestinoId = 4, TipoDestino = "UBICACION", TipoMovimientoId = tipo },
        Lineas = cantidades.Select(x => new LineaMovimiento { MaterialId = material, Cantidad = x }).ToList(), OrigenNombre = "Deposito", DestinoNombre = "Destino"
    };
    private static PostgresConnectionFactory Factory() => new PostgresConnectionFactory(ConnectionString());
    private static async Task<int> Crear(MovimientoSolicitud solicitud) { using (var u = new NpgsqlUnitOfWork(Factory())) return await new RegistrarMovimiento(Actor(), u, new NpgsqlMovimientos(u), new NpgsqlInventario(u)).EjecutarAsync(solicitud); }
    private static async Task<int> Editar(int id, MovimientoSolicitud solicitud) { using (var u = new NpgsqlUnitOfWork(Factory())) return await new EditarMovimiento(Actor(), u, new NpgsqlMovimientos(u), new NpgsqlInventario(u)).EjecutarAsync(id, solicitud); }
    private static async Task<int> Eliminar(int id) { using (var u = new NpgsqlUnitOfWork(Factory())) return await new EliminarMovimiento(Actor(), u, new NpgsqlMovimientos(u), new NpgsqlInventario(u)).EjecutarAsync(id); }

    [Theory]
    [InlineData("0.005", "1.01")]
    [InlineData("0.004", "1.00")]
    public async Task FraccionConImpactoRealSeRevierteExactamente(string valor, string saldo)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        decimal cantidad = decimal.Parse(valor, culture), esperado = decimal.Parse(saldo, culture);
        int material = Material(1m); int id = await Crear(Solicitud(material, 2, cantidad));
        Assert.True(id > 0); Assert.Equal(esperado, Stock(material));
        Assert.Equal(esperado - 1m, Convert.ToDecimal(Sql("SELECT impacto_stock_real FROM public.movimientomaterial WHERE id_movimiento=@id", new NpgsqlParameter("id", id))));
        Assert.Equal(id, await Eliminar(id)); Assert.Equal(1m, Stock(material));
        Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_movimiento=@id", new NpgsqlParameter("id", id))));
    }

    [Fact]
    public async Task MultiplesLineasYEdicionReviertenCadaImpactoSinRedondeoAcumulado()
    {
        int a = Material(1m), b = Material(4m);
        int id = await Crear(Solicitud(a, 2, .005m, .004m, .005m)); Assert.Equal(1.02m, Stock(a));
        Assert.Equal(id, await Editar(id, Solicitud(b, 2, .005m, .004m)));
        Assert.Equal(1m, Stock(a)); Assert.Equal(4.01m, Stock(b));
        Assert.Equal(id, await Eliminar(id)); Assert.Equal(1m, Stock(a)); Assert.Equal(4m, Stock(b));
    }
    private sealed class FallarDespuesStock : IInventarioEscritura
    {
        private readonly IInventarioEscritura inner;
        public FallarDespuesStock(IInventarioEscritura inner) { this.inner = inner; }
        public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken token = default) => inner.BloquearAsync(ids, token);
        public async Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken token = default) { await inner.GuardarSaldoAsync(id, saldo, token); throw new InvalidOperationException("Fallo posterior al UPDATE real"); }
    }
    [Fact]
    public async Task FalloDespuesDeStockRevierteMovimientoYSaldoEnPostgres()
    {
        int material = Material(10m); var solicitud = Solicitud(material, 2, 2m);
        using (var u = new NpgsqlUnitOfWork(Factory())) {
            var op = new RegistrarMovimiento(Actor(), u, new NpgsqlMovimientos(u), new FallarDespuesStock(new NpgsqlInventario(u)));
            await Assert.ThrowsAsync<InvalidOperationException>(() => op.EjecutarAsync(solicitud));
        }
        Assert.Equal(10m, Stock(material));
        Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE nroremito=@remito", new NpgsqlParameter("remito", solicitud.Cabecera.Remito))));
        Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_material=@id", new NpgsqlParameter("id", material))));
    }
    private static int Obra() => Convert.ToInt32(Sql("INSERT INTO public.obras(nombre) VALUES (@nombre) RETURNING id", new NpgsqlParameter("nombre", Guid.NewGuid().ToString("N"))));
    private static int Insumo(int obra, bool consumido = false) => Convert.ToInt32(Sql("INSERT INTO public.insumos(nombre,id_obra,consumido) VALUES (@nombre,@obra,@consumido) RETURNING id_insumo", new NpgsqlParameter("nombre", Guid.NewGuid().ToString("N")), new NpgsqlParameter("obra", obra), new NpgsqlParameter("consumido", consumido)));
    private static int ObraInsumo(int id) => Convert.ToInt32(Sql("SELECT id_obra FROM public.insumos WHERE id_insumo=@id", new NpgsqlParameter("id", id)));
    private static long Historial(int insumo) => Convert.ToInt64(Sql("SELECT count(*) FROM public.insumo_movimientos WHERE id_insumo=@id", new NpgsqlParameter("id", insumo)));
    private static MovimientoSolicitud ConInsumo(int material, int insumo, int destino)
    {
        var solicitud = Solicitud(material, 4, 2m); solicitud.Cabecera.DestinoId = destino; solicitud.Cabecera.TipoDestino = "OBRA"; solicitud.Insumos.Add(insumo); return solicitud;
    }
    private sealed class FallarDespuesTraslado : IMovimientosEscritura
    {
        private readonly IMovimientosEscritura inner;
        public FallarDespuesTraslado(IMovimientosEscritura inner) { this.inner = inner; }
        public Task<MovimientoPersistido> BloquearAsync(int id, CancellationToken t = default) => inner.BloquearAsync(id,t);
        public Task<string> NombreTipoAsync(int id, CancellationToken t = default) => inner.NombreTipoAsync(id,t);
        public Task<IDictionary<int,int>> ObtenerMaterialesRecortesAsync(IReadOnlyList<int> ids, CancellationToken t = default) => inner.ObtenerMaterialesRecortesAsync(ids,t);
        public Task<IDictionary<int,decimal>> BloquearRecortesAsync(IReadOnlyList<int> ids, CancellationToken t = default) => inner.BloquearRecortesAsync(ids,t);
        public Task<IDictionary<int,InsumoBloqueado>> BloquearInsumosAsync(IReadOnlyList<int> ids, CancellationToken t = default) => inner.BloquearInsumosAsync(ids,t);
        public Task<decimal> GuardarRecorteAsync(int id, decimal saldo, CancellationToken t = default) => inner.GuardarRecorteAsync(id,saldo,t);
        public Task<int> CrearCabeceraAsync(CabeceraMovimiento c, CancellationToken t = default) => inner.CrearCabeceraAsync(c,t);
        public Task ActualizarCabeceraAsync(int id, CabeceraMovimiento c, CancellationToken t = default) => inner.ActualizarCabeceraAsync(id,c,t);
        public Task AgregarLineaAsync(int id, LineaMovimiento l, decimal saldo, CancellationToken t = default) => inner.AgregarLineaAsync(id,l,saldo,t);
        public async Task RegistrarTrasladoAsync(int id, TrasladoInsumo x, CancellationToken t = default) { await inner.RegistrarTrasladoAsync(id,x,t); throw new InvalidOperationException("Fallo posterior al traslado SQL"); }
        public Task RevertirTrasladoAsync(TrasladoInsumo x, CancellationToken t = default) => inner.RevertirTrasladoAsync(x,t);
        public Task EliminarLineasYTrasladosAsync(int id, CancellationToken t = default) => inner.EliminarLineasYTrasladosAsync(id,t);
        public Task EliminarCabeceraAsync(int id, CancellationToken t = default) => inner.EliminarCabeceraAsync(id,t);
    }
    [Fact]
    public async Task FalloPosteriorAInsumoRevierteHistorialEstadoStockYCabecera()
    {
        int origen = Obra(), destino = Obra(), insumo = Insumo(origen), material = Material(10m); var solicitud = ConInsumo(material, insumo, destino);
        using (var u = new NpgsqlUnitOfWork(Factory())) {
            var op = new RegistrarMovimiento(Actor(), u, new FallarDespuesTraslado(new NpgsqlMovimientos(u)), new NpgsqlInventario(u));
            await Assert.ThrowsAsync<InvalidOperationException>(() => op.EjecutarAsync(solicitud));
        }
        Assert.Equal(10m, Stock(material)); Assert.Equal(origen, ObraInsumo(insumo)); Assert.Equal(0L, Historial(insumo));
        Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE nroremito=@remito", new NpgsqlParameter("remito", solicitud.Cabecera.Remito))));
    }
    [Fact]
    public async Task EditarYEliminarRestauranInsumoPorIdAunqueCambieNombreDeObra()
    {
        int origen = Obra(), destino = Obra(), otro = Obra(), insumo = Insumo(origen), material = Material(10m);
        int id = await Crear(ConInsumo(material, insumo, destino)); Assert.Equal(destino, ObraInsumo(insumo)); Assert.Equal(8m, Stock(material)); Assert.Equal(1L, Historial(insumo));
        Sql("UPDATE public.obras SET nombre='renombrada' WHERE id=@id RETURNING id", new NpgsqlParameter("id", origen));
        Assert.Equal(id, await Editar(id, ConInsumo(material, insumo, otro))); Assert.Equal(otro, ObraInsumo(insumo)); Assert.Equal(8m, Stock(material)); Assert.Equal(1L, Historial(insumo));
        Assert.Equal(id, await Eliminar(id)); Assert.Equal(origen, ObraInsumo(insumo)); Assert.Equal(10m, Stock(material)); Assert.Equal(0L, Historial(insumo));
    }
    [Fact]
    public async Task TrasladoPosteriorYConsumidoImpidenReversionSinDejarResiduos()
    {
        int origen = Obra(), destino = Obra(), otro = Obra(), insumo = Insumo(origen), material = Material(10m);
        int id = await Crear(ConInsumo(material, insumo, destino)); int posterior = await Crear(ConInsumo(material, insumo, otro));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Eliminar(id)); Assert.Equal(6m, Stock(material)); Assert.Equal(otro, ObraInsumo(insumo)); Assert.Equal(2L, Historial(insumo));
        await Eliminar(posterior); await Eliminar(id); Assert.Equal(10m, Stock(material)); Assert.Equal(origen, ObraInsumo(insumo));
        int consumido = Insumo(origen, true); var solicitud = ConInsumo(material, consumido, destino);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(solicitud)); Assert.Equal(10m, Stock(material)); Assert.Equal(origen, ObraInsumo(consumido)); Assert.Equal(0L, Historial(consumido));
    }
    [Fact]
    public async Task DosEliminacionesConcurrentesReviertenUnaSolaVez()
    {
        int material = Material(10m), id = await Crear(Solicitud(material, 7, 2m));
        async Task<bool> Intentar() { try { await Eliminar(id); return true; } catch (InvalidOperationException) { return false; } }
        using (var blocker = new NpgsqlConnection(ConnectionString())) { blocker.Open(); using (var tx = blocker.BeginTransaction()) {
            using (var cmd = new NpgsqlCommand("SELECT id_movimiento FROM public.movimientos WHERE id_movimiento=@id FOR UPDATE", blocker, tx)) { cmd.Parameters.AddWithValue("id", id); cmd.ExecuteScalar(); }
            var tasks = new[] { Task.Run((Func<Task<bool>>)Intentar), Task.Run((Func<Task<bool>>)Intentar) };
            try {
                var deadline = DateTime.UtcNow.AddSeconds(15); int waiting = 0;
                while (DateTime.UtcNow < deadline) { waiting = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%FOR UPDATE OF m%'")); if (waiting >= 2) break; await Task.Delay(30); }
                Assert.True(waiting >= 2, "Ambas eliminaciones deben esperar la cabecera antes de liberar el bloqueo.");
            } finally { tx.Rollback(); }
            var outcomes = await Task.WhenAll(tasks); Assert.Single(outcomes, x => x); Assert.Equal(10m, Stock(material));
        } }
    }
    [Fact]
    public async Task HistoricoFraccionSinSnapshotRechazaReversionYNoCambiaStock()
    {
        int material = Material(1m), id = await Crear(Solicitud(material, 2, .005m));
        Sql("UPDATE public.movimientomaterial SET impacto_stock_real=NULL WHERE id_movimiento=@id RETURNING id_movimientomaterial", new NpgsqlParameter("id", id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Eliminar(id)); Assert.Equal(1.01m, Stock(material));
        Assert.Equal(1L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE id_movimiento=@id", new NpgsqlParameter("id", id))));
    }
    [Fact]
    public void PermisoRechazadoAntesDeAbrirConexion()
    {
        using (var u = new NpgsqlUnitOfWork(Factory())) {
            Assert.Throws<UnauthorizedAccessException>(() => { _ = new RegistrarMovimiento(Actor(Role.None), u, new NpgsqlMovimientos(u), new NpgsqlInventario(u)).EjecutarAsync(Solicitud(1, 2, 1m)); });
            var connection = typeof(NpgsqlUnitOfWork).GetField("connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.Null(connection.GetValue(u));
        }
    }
    [Fact]
    public async Task RegistroInternoNoSeEditaNiEliminaYVentaPuedeEliminarsePorGalpon()
    {
        int material = Material(10m); var solicitudInterna = Solicitud(material, 11, 2m);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(solicitudInterna));
        Assert.Equal(10m, Stock(material));
        Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE nroremito=@remito",new NpgsqlParameter("remito",solicitudInterna.Cabecera.Remito))));
        await EditarDatos(material, Datos("Ajuste legítimo"), 12m, 10m);
        int id = Convert.ToInt32(Sql("SELECT m.id_movimiento FROM public.movimientos m JOIN public.movimientomaterial mm ON mm.id_movimiento=m.id_movimiento WHERE m.id_tipomovimiento=11 AND mm.id_material=@id",new NpgsqlParameter("id",material)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Editar(id, Solicitud(material, 2, 4m)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Eliminar(id));
        Assert.Equal(12m, Stock(material));
        Assert.Equal(1L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_movimiento=@id AND id_material=@material", new NpgsqlParameter("id", id),new NpgsqlParameter("material",material))));
        int venta = await Crear(Solicitud(material, 7, 2m));
        using (var u = new NpgsqlUnitOfWork(Factory()))
            Assert.Equal(venta, await new EliminarMovimiento(Actor(Role.Galpon), u, new NpgsqlMovimientos(u), new NpgsqlInventario(u)).EjecutarAsync(venta));
        Assert.Equal(12m, Stock(material));
    }
    [Fact]
    public async Task RecepcionOrdenYSalidaCompartenBloqueoSinPerderSaldo()
    {
        int material = Material(10m), orden;
        using (var u = new NpgsqlUnitOfWork(Factory())) {
            var cab = new CabeceraOrden { Fecha = new DateTime(2025,1,1), Numero = Guid.NewGuid().ToString("N"), ProveedorId = 1 };
            orden = await new CrearOrden(Actor(), u, new NpgsqlOrdenes(u)).EjecutarAsync(cab, new[] { new LineaNueva { MaterialId = material, Pedida = 4 } });
        }
        int linea = Convert.ToInt32(Sql("SELECT id_detalle FROM public.ordenes_compra_detalle WHERE id_orden_compra=@id", new NpgsqlParameter("id", orden)));
        async Task Recibir() { using (var u = new NpgsqlUnitOfWork(Factory())) await new ConfirmarRecepcion(Actor(Role.Galpon), u, new NpgsqlOrdenes(u), new NpgsqlInventario(u)).EjecutarAsync(orden, new Dictionary<int,decimal> { [linea] = 4m }, null, null); }
        using (var blocker = new NpgsqlConnection(ConnectionString())) { blocker.Open(); using (var tx = blocker.BeginTransaction()) {
            using (var cmd = new NpgsqlCommand("SELECT stockactual FROM public.materiales WHERE id=@id FOR UPDATE", blocker, tx)) { cmd.Parameters.AddWithValue("id", material); cmd.ExecuteScalar(); }
            var tasks = new Task[] { Task.Run((Func<Task>)Recibir), Task.Run(async () => { await Crear(Solicitud(material,7,2m)); }) };
            try {
                var deadline = DateTime.UtcNow.AddSeconds(15); int waiting = 0;
                while (DateTime.UtcNow < deadline) { waiting = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%stockactual%'")); if (waiting >= 2) break; await Task.Delay(30); }
                Assert.True(waiting >= 2, "Recepción y movimiento deben esperar el mismo stock bloqueado.");
            } finally { tx.Rollback(); }
            await Task.WhenAll(tasks); Assert.Equal(12m, Stock(material));
        } }
    }
    [Fact]
    public async Task SalidasConcurrentesDelMismoRecorteNoProducenSaldoNegativo()
    {
        int material = Material(10m);
        int recorte = Convert.ToInt32(Sql("INSERT INTO public.recortes(id_material,medida,cantidad,estado) VALUES (@id,1,3,'Buen estado') RETURNING id", new NpgsqlParameter("id",material)));
        MovimientoSolicitud SolicitudRecorte() { var s = Solicitud(material, 7, 2m); s.Lineas[0].RecorteId = recorte; s.Lineas[0].MedidaRecorte = 1m; return s; }
        async Task<bool> Intentar() { try { await Crear(SolicitudRecorte()); return true; } catch (InvalidOperationException) { return false; } }
        using (var blocker = new NpgsqlConnection(ConnectionString())) { blocker.Open(); using (var tx = blocker.BeginTransaction()) {
            using (var cmd = new NpgsqlCommand("SELECT cantidad FROM public.recortes WHERE id=@id FOR UPDATE", blocker, tx)) { cmd.Parameters.AddWithValue("id", recorte); cmd.ExecuteScalar(); }
            var tasks = new[] { Task.Run((Func<Task<bool>>)Intentar), Task.Run((Func<Task<bool>>)Intentar) };
            try {
                var deadline = DateTime.UtcNow.AddSeconds(15); int waiting = 0;
                while (DateTime.UtcNow < deadline) { waiting = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND (query LIKE '%SELECT cantidad FROM public.recortes%' OR query LIKE '%stockactual%FOR UPDATE%')")); if (waiting >= 2) break; await Task.Delay(30); }
                Assert.True(waiting >= 2, "Ambas salidas deben esperar los bloqueos coordinados de material y recorte.");
            } finally { tx.Rollback(); }
            var outcomes = await Task.WhenAll(tasks); Assert.Single(outcomes, x => x);
            Assert.Equal(1m, Convert.ToDecimal(Sql("SELECT cantidad FROM public.recortes WHERE id=@id", new NpgsqlParameter("id",recorte)))); Assert.Equal(10m, Stock(material));
        } }
    }
    private sealed class CancelarDespuesStock : IInventarioEscritura
    {
        private readonly IInventarioEscritura inner; private readonly CancellationTokenSource cancel;
        public CancelarDespuesStock(IInventarioEscritura inner, CancellationTokenSource cancel) { this.inner = inner; this.cancel = cancel; }
        public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken t = default) => inner.BloquearAsync(ids, t);
        public async Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken t = default) { await inner.GuardarSaldoAsync(id, saldo, t); cancel.Cancel(); }
    }
    [Fact]
    public async Task CancelacionTrasStockRevierteTransaccionYLiberaBloqueo()
    {
        int material = Material(10m); var solicitud = Solicitud(material, 2, 2m);
        using (var source = new CancellationTokenSource()) using (var u = new NpgsqlUnitOfWork(Factory())) {
            var op = new RegistrarMovimiento(Actor(), u, new NpgsqlMovimientos(u), new CancelarDespuesStock(new NpgsqlInventario(u), source));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => op.EjecutarAsync(solicitud, source.Token));
        }
        Assert.Equal(10m, Stock(material));
        Assert.Equal(material, Convert.ToInt32(Sql("UPDATE public.materiales SET stockactual=stockactual WHERE id=@id RETURNING id", new NpgsqlParameter("id",material))));
        Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE nroremito=@remito", new NpgsqlParameter("remito", solicitud.Cabecera.Remito))));
    }
    [Fact]
    public async Task EditarBloqueaRecursoNuevoAntesDeRevertirElAnterior()
    {
        int anterior = Material(10m), nuevo = Material(20m); int id = await Crear(Solicitud(anterior, 7, 2m));
        using (var blocker = new NpgsqlConnection(ConnectionString())) { blocker.Open(); using (var tx = blocker.BeginTransaction()) {
            using (var cmd = new NpgsqlCommand("SELECT stockactual FROM public.materiales WHERE id=@id FOR UPDATE", blocker, tx)) { cmd.Parameters.AddWithValue("id", nuevo); cmd.ExecuteScalar(); }
            var edit = Task.Run(async () => await Editar(id, Solicitud(nuevo, 7, 3m)));
            try {
                var deadline = DateTime.UtcNow.AddSeconds(15); int waiting = 0;
                while (DateTime.UtcNow < deadline) { waiting = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%stockactual%FOR UPDATE%'")); if (waiting >= 1) break; await Task.Delay(30); }
                Assert.True(waiting >= 1, "La edición debe bloquear el material nuevo antes de escribir.");
                Assert.Equal(8m, Stock(anterior)); Assert.Equal(20m, Stock(nuevo));
            } finally { tx.Rollback(); }
            Assert.Equal(id, await edit); Assert.Equal(10m, Stock(anterior)); Assert.Equal(17m, Stock(nuevo));
        } }
    }
    [Fact]
    public async Task TipoNoCanonicoConDestinoObraConservaFallbackDeSalida()
    {
        int material = Material(10m), obra = Obra();
        int tipo = Convert.ToInt32(Sql("INSERT INTO public.tipo_movimiento(nombre) VALUES (@nombre) RETURNING id_tipomovimiento", new NpgsqlParameter("nombre", "Transferencia especial " + Guid.NewGuid().ToString("N"))));
        var s = Solicitud(material, tipo, 2m); s.Cabecera.TipoDestino = "OBRA"; s.Cabecera.DestinoId = obra;
        int id = await Crear(s); Assert.Equal(8m, Stock(material)); await Eliminar(id); Assert.Equal(10m, Stock(material));
    }
    [Fact]
    public async Task TrasladoPosteriorDesdeGestionInsumosImpideRevertirMovimiento()
    {
        int origen = Obra(), destino = Obra(), otro = Obra(), insumo = Insumo(origen), material = Material(10m);
        int id = await Crear(ConInsumo(material,insumo,destino));
        using (var u = new NpgsqlUnitOfWork(Factory()))
            Assert.Equal(insumo, await new GestionarInsumos(Actor(),u,new NpgsqlInsumos(u)).EditarAsync(insumo,"Insumo",otro,false,null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Eliminar(id));
        Assert.Equal(8m, Stock(material)); Assert.Equal(otro, ObraInsumo(insumo)); Assert.Equal(2L, Historial(insumo));
        using (var u = new NpgsqlUnitOfWork(Factory()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => new GestionarInsumos(Actor(),u,new NpgsqlInsumos(u)).EliminarAsync(insumo));
        Assert.Equal(otro, ObraInsumo(insumo)); Assert.Equal(2L, Historial(insumo));
    }
    [Fact]
    public async Task EdicionInsumoYMovimientoEsperanLaMismaFila()
    {
        int origen=Obra(), destino=Obra(), otro=Obra(), insumo=Insumo(origen), material=Material(10m);
        async Task<int> EditarInsumo() { using(var u=new NpgsqlUnitOfWork(Factory())) return await new GestionarInsumos(Actor(),u,new NpgsqlInsumos(u)).EditarAsync(insumo,"Insumo",otro,false,null); }
        using(var blocker=new NpgsqlConnection(ConnectionString())) { blocker.Open(); using(var tx=blocker.BeginTransaction()) {
            using(var cmd=new NpgsqlCommand("SELECT id_insumo FROM public.insumos WHERE id_insumo=@id FOR UPDATE",blocker,tx)) { cmd.Parameters.AddWithValue("id",insumo); cmd.ExecuteScalar(); }
            var tareas=new[] { Task.Run((Func<Task<int>>)EditarInsumo), Task.Run(async()=>await Crear(ConInsumo(material,insumo,destino))) };
            try {
                var deadline=DateTime.UtcNow.AddSeconds(15); int waiting=0;
                while(DateTime.UtcNow<deadline) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%FROM public.insumos WHERE id_insumo=%FOR UPDATE%'")); if(waiting>=2) break; await Task.Delay(30); }
                Assert.True(waiting>=2,"Ambos escritores deben esperar el insumo bloqueado.");
            } finally { tx.Rollback(); }
            var results=await Task.WhenAll(tareas); Assert.Equal(insumo,results[0]); Assert.True(results[1]>0); Assert.Equal(8m,Stock(material)); Assert.Equal(2L,Historial(insumo));
            Assert.Contains(ObraInsumo(insumo),new[]{destino,otro});
        } }
    }
    [Fact]
    public async Task AltaConcurrenteRecorteCoincidenteConservaCantidadYUnicaFila()
    {
        int material = Material(10m);
        async Task<int> Agregar(decimal cantidad) { using(var u=new NpgsqlUnitOfWork(Factory())) return await new AgregarRecorte(Actor(),u,new NpgsqlInventario(u),new NpgsqlRecortes(u)).EjecutarAsync(material,2m,3m,0m,cantidad,"Buen estado"); }
        int previo = await Agregar(3m);
        using(var blocker=new NpgsqlConnection(ConnectionString())) { blocker.Open(); using(var tx=blocker.BeginTransaction()) {
            using(var cmd=new NpgsqlCommand("SELECT stockactual FROM public.materiales WHERE id=@id FOR UPDATE",blocker,tx)) { cmd.Parameters.AddWithValue("id",material); cmd.ExecuteScalar(); }
            var tareas=new[] { Task.Run(async()=>await Agregar(2m)), Task.Run(async()=>await Agregar(4m)) };
            try {
                var deadline=DateTime.UtcNow.AddSeconds(15); int waiting=0;
                while(DateTime.UtcNow<deadline) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%stockactual%FOR UPDATE%'")); if(waiting>=2) break; await Task.Delay(30); }
                Assert.True(waiting>=2,"Las dos altas deben esperar el mismo material.");
            } finally { tx.Rollback(); }
            var ids=await Task.WhenAll(tareas); Assert.All(ids,x=>Assert.Equal(previo,x));
            Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.recortes WHERE id_material=@id",new NpgsqlParameter("id",material))));
            Assert.Equal(9m,Convert.ToDecimal(Sql("SELECT cantidad FROM public.recortes WHERE id=@id",new NpgsqlParameter("id",previo)))); Assert.Equal(10m,Stock(material));
        } }
    }
    private static DatosMaterial Datos(string nombre, params int[] proveedores) => new DatosMaterial { Nombre=nombre, CategoriaId=1, StockMinimo=1m, Proveedores=proveedores };
    private static BusinessClock Reloj() => new BusinessClock(TimeZoneInfo.CreateCustomTimeZone("Argentina test",TimeSpan.FromHours(-3),"AR","AR"),()=>new DateTimeOffset(2040,1,2,1,0,0,TimeSpan.Zero));
    private static async Task EditarDatos(int id, DatosMaterial datos, decimal? nuevo, decimal? esperado) { using(var u=new NpgsqlUnitOfWork(Factory())) await new EditarMaterial(Actor(),u,new NpgsqlMateriales(u),new NpgsqlInventario(u),Reloj()).EjecutarAsync(id,datos,nuevo,esperado); }
    [Fact]
    public async Task MaterialStaleYProveedorInvalidoReviertenTodoMetadataNoPisaStock()
    {
        int material=Material(10m); await Crear(Solicitud(material,7,2m));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>EditarDatos(material,Datos("Stale"),11m,10m)); Assert.Equal(8m,Stock(material));
        await EditarDatos(material,Datos("Metadata",1),null,null); Assert.Equal(8m,Stock(material));
        Assert.Equal("Metadata",Convert.ToString(Sql("SELECT nombre FROM public.materiales WHERE id=@id",new NpgsqlParameter("id",material))));
        var antes=Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_material=@id",new NpgsqlParameter("id",material)));
        await Assert.ThrowsAnyAsync<Exception>(()=>EditarDatos(material,Datos("Invalido",int.MaxValue),12m,8m));
        Assert.Equal(8m,Stock(material)); Assert.Equal("Metadata",Convert.ToString(Sql("SELECT nombre FROM public.materiales WHERE id=@id",new NpgsqlParameter("id",material))));
        Assert.Equal(antes,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_material=@id",new NpgsqlParameter("id",material))));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.materiales_proveedores WHERE id_material=@id AND id_proveedor=1",new NpgsqlParameter("id",material))));
    }
    [Fact]
    public async Task AjustesConcurrentesUnificanCabeceraPorFechaArgentinaYSerializanSnapshot()
    {
        int a=Material(10m),b=Material(10m);
        using(var blocker=new NpgsqlConnection(ConnectionString())) { blocker.Open(); using(var tx=blocker.BeginTransaction()) {
            using(var cmd=new NpgsqlCommand("SELECT pg_advisory_xact_lock(141414,20400101)",blocker,tx)) cmd.ExecuteNonQuery();
            var tareas=new[]{Task.Run(()=>EditarDatos(a,Datos("A"),11m,10m)),Task.Run(()=>EditarDatos(b,Datos("B"),12m,10m))};
            try {
                var deadline=DateTime.UtcNow.AddSeconds(15); int waiting=0;
                while(DateTime.UtcNow<deadline) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%pg_advisory_xact_lock(141414%'")); if(waiting>=2) break; await Task.Delay(30); }
                Assert.True(waiting>=2,"Ambos ajustes deben participar del advisory por fecha AR.");
            } finally { tx.Rollback(); }
            await Task.WhenAll(tareas);
        } }
        Assert.Equal(11m,Stock(a)); Assert.Equal(12m,Stock(b));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE id_tipomovimiento=11 AND fecha='2040-01-01'")));
        Assert.Equal(2L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial mm JOIN public.movimientos m ON m.id_movimiento=mm.id_movimiento WHERE m.id_tipomovimiento=11 AND m.fecha='2040-01-01' AND mm.id_material IN (@a,@b)",new NpgsqlParameter("a",a),new NpgsqlParameter("b",b))));
        async Task<bool> Intentar(decimal saldo) { try { await EditarDatos(a,Datos("Carrera"),saldo,11m); return true; } catch(InvalidOperationException) { return false; } }
        var resultados=await Task.WhenAll(Task.Run(()=>Intentar(13m)),Task.Run(()=>Intentar(14m))); Assert.Single(resultados,x=>x);
        Assert.Contains(Stock(a),new[]{13m,14m});
        Assert.Equal(Stock(a)-10m,Convert.ToDecimal(Sql("SELECT COALESCE(sum(mm.cantidad),0) FROM public.movimientomaterial mm JOIN public.movimientos m ON m.id_movimiento=mm.id_movimiento WHERE m.id_tipomovimiento=11 AND m.fecha='2040-01-01' AND mm.id_material=@id",new NpgsqlParameter("id",a))));
    }
    [Fact]
    public async Task ConsumoYDesconsumoPosterioresDejanTrazaYBloqueanReversion()
    {
        int origen=Obra(),destino=Obra(),insumo=Insumo(origen),material=Material(10m);
        int id=await Crear(ConInsumo(material,insumo,destino));
        async Task Cambiar(bool consumido) { using(var u=new NpgsqlUnitOfWork(Factory())) await new GestionarInsumos(Actor(),u,new NpgsqlInsumos(u)).EditarAsync(insumo,"Insumo",destino,consumido,null); }
        await Cambiar(true); await Cambiar(false); Assert.Equal(3L,Historial(insumo));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Eliminar(id)); Assert.Equal(8m,Stock(material)); Assert.Equal(destino,ObraInsumo(insumo));
    }
    [Fact]
    public async Task RecorteReferenciadoNoSeEliminaHastaRevertirMovimiento()
    {
        int material=Material(10m),recorte;
        using(var u=new NpgsqlUnitOfWork(Factory())) recorte=await new AgregarRecorte(Actor(),u,new NpgsqlInventario(u),new NpgsqlRecortes(u)).EjecutarAsync(material,0m,0m,1m,3m,"Buen estado");
        var solicitud=Solicitud(material,7,2m); solicitud.Lineas[0].RecorteId=recorte; solicitud.Lineas[0].MedidaRecorte=1m;
        int movimiento=await Crear(solicitud);
        async Task Borrar() { using(var u=new NpgsqlUnitOfWork(Factory())) await new EliminarRecorte(Actor(),u,new NpgsqlInventario(u),new NpgsqlRecortes(u)).EjecutarAsync(recorte,material); }
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Borrar());
        Assert.Equal(1m,Convert.ToDecimal(Sql("SELECT cantidad FROM public.recortes WHERE id=@id",new NpgsqlParameter("id",recorte))));
        await Eliminar(movimiento); await Borrar(); Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.recortes WHERE id=@id",new NpgsqlParameter("id",recorte))));
    }
    [Fact]
    public async Task AltaRecorteModalConCeroEstadoNuloYGalponEsCompatible()
    {
        int material=Material(10m), id;
        using(var u=new NpgsqlUnitOfWork(Factory())) id=await new AgregarRecorte(Actor(Role.Galpon),u,new NpgsqlInventario(u),new NpgsqlRecortes(u)).EjecutarAsync(material,2m,0m,2m,0m,null);
        Assert.True(id>0); Assert.Equal(0m,Convert.ToDecimal(Sql("SELECT cantidad FROM public.recortes WHERE id=@id",new NpgsqlParameter("id",id))));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.recortes WHERE id=@id AND estado IS NULL",new NpgsqlParameter("id",id))));
    }
    [Fact]
    public async Task EdicionesCruzadasBloqueanAmbosMaterialesEnOrdenComun()
    {
        int a=Material(10m),b=Material(10m); int ma=await Crear(Solicitud(a,7,2m)),mb=await Crear(Solicitud(b,7,2m));
        using(var blocker=new NpgsqlConnection(ConnectionString())) { blocker.Open(); using(var tx=blocker.BeginTransaction()) {
            using(var cmd=new NpgsqlCommand("SELECT stockactual FROM public.materiales WHERE id=@id FOR UPDATE",blocker,tx)) { cmd.Parameters.AddWithValue("id",Math.Min(a,b)); cmd.ExecuteScalar(); }
            var tareas=new[]{Task.Run(()=>Editar(ma,Solicitud(b,7,2m))),Task.Run(()=>Editar(mb,Solicitud(a,7,2m)))};
            try {
                var deadline=DateTime.UtcNow.AddSeconds(15); int waiting=0;
                while(DateTime.UtcNow<deadline) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%stockactual%FOR UPDATE%'")); if(waiting>=2) break; await Task.Delay(30); }
                Assert.True(waiting>=2,"Ambas ediciones deben esperar el primer material del orden común.");
            } finally { tx.Rollback(); }
            var resultados=await Task.WhenAll(tareas); Assert.Contains(ma,resultados); Assert.Contains(mb,resultados);
        } }
        Assert.Equal(8m,Stock(a)); Assert.Equal(8m,Stock(b));
    }
    [Fact]
    public async Task HistorialInsumoAntiguoSinSnapshotRechazaReversionSinTocarStock()
    {
        int origen=Obra(),destino=Obra(),insumo=Insumo(origen),material=Material(10m); int id=await Crear(ConInsumo(material,insumo,destino));
        Sql("UPDATE public.insumo_movimientos SET snapshot_completo=false,id_obra_origen_snapshot=NULL,id_obra_destino_snapshot=NULL,consumido_origen_snapshot=NULL,consumido_destino_snapshot=NULL WHERE id_movimiento=@id RETURNING id",new NpgsqlParameter("id",id));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Eliminar(id));
        Assert.Equal(8m,Stock(material)); Assert.Equal(destino,ObraInsumo(insumo)); Assert.Equal(1L,Historial(insumo));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE id_movimiento=@id",new NpgsqlParameter("id",id))));
    }
    [Fact]
    public async Task MaterialCreacionYValidacionesDeSnapshotNegativoYNoExiste()
    {
        int material;
        using(var u=new NpgsqlUnitOfWork(Factory())) material=await new CrearMaterial(Actor(),u,new NpgsqlMateriales(u)).EjecutarAsync(Datos("Creado",1),10.5m);
        Assert.True(material>0); Assert.Equal(10.5m,Stock(material));
        await EditarDatos(material,Datos("Ajustado",1),11.75m,10.5m); Assert.Equal(11.75m,Stock(material));
        await Assert.ThrowsAsync<ArgumentException>(()=>EditarDatos(material,Datos("Sin snapshot"),12m,null));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>EditarDatos(material,Datos("Negativo"),-1m,11.75m));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>EditarDatos(int.MaxValue,Datos("Ausente"),null,null));
        Assert.Equal(11.75m,Stock(material));
    }
    [Fact]
    public async Task EliminarAntesDeEditarSerializaCabeceraYRevierteUnaVez()
    {
        int material=Material(10m), id=await Crear(Solicitud(material,7,2m));
        using(var blocker=new NpgsqlConnection(ConnectionString())) { blocker.Open(); using(var tx=blocker.BeginTransaction()) {
            using(var cmd=new NpgsqlCommand("SELECT stockactual FROM public.materiales WHERE id=@id FOR UPDATE",blocker,tx)) { cmd.Parameters.AddWithValue("id",material); cmd.ExecuteScalar(); }
            var borrar=Task.Run(()=>Eliminar(id)); Task<int> editar=null;
            try {
                var deadline=DateTime.UtcNow.AddSeconds(15); int waiting=0;
                while(DateTime.UtcNow<deadline) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%stockactual%FOR UPDATE%'")); if(waiting>=1) break; await Task.Delay(30); }
                Assert.True(waiting>=1,"El delete debe retener la cabecera mientras espera stock.");
                editar=Task.Run(()=>Editar(id,Solicitud(material,7,3m))); deadline=DateTime.UtcNow.AddSeconds(15); waiting=0;
                while(DateTime.UtcNow<deadline) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%FOR UPDATE OF m%'")); if(waiting>=1) break; await Task.Delay(30); }
                Assert.True(waiting>=1,"La edición debe esperar la cabecera retenida por delete.");
            } finally { tx.Rollback(); }
            Assert.Equal(id,await borrar); await Assert.ThrowsAsync<InvalidOperationException>(()=>editar); Assert.Equal(10m,Stock(material));
        } }
    }
    [Fact]
    public async Task ConsultasMaterialesFiltranYCalculanPrioridadEnConexionIndependiente()
    {
        string prefijo="Consulta"+Guid.NewGuid().ToString("N"); int a,b;
        using(var u=new NpgsqlUnitOfWork(Factory())) a=await new CrearMaterial(Actor(),u,new NpgsqlMateriales(u)).EjecutarAsync(new DatosMaterial{Nombre=prefijo+"A",CategoriaId=1,StockMinimo=10m,Proveedores=new[]{1}},8m);
        using(var u=new NpgsqlUnitOfWork(Factory())) b=await new CrearMaterial(Actor(),u,new NpgsqlMateriales(u)).EjecutarAsync(new DatosMaterial{Nombre=prefijo+"B",CategoriaId=1,StockMinimo=10m,Proveedores=new[]{1}},8m);
        await Crear(Solicitud(a,7,3m)); await Crear(Solicitud(b,7,1m));
        var q=new ConsultarMateriales(Actor(),new NpgsqlMaterialesConsulta(Factory()));
        var filtro=new FiltroMaterialLectura{Nombre=prefijo,Desde=4m,Hasta=8m,Categorias=new[]{1},Proveedores=new[]{1},SoloStockBajo=true,Priorizados=true};
        var lista=await q.ListarAsync(filtro); Assert.Equal(2,lista.Count); Assert.Equal(a,lista[0].Id); Assert.Equal(3m,lista[0].CantidadUsada); Assert.Equal(1,lista[0].CantidadMovimientos); Assert.Equal(b,lista[1].Id);
        Assert.Contains(await q.CategoriasAsync(),x=>x.Id==1); Assert.Contains(1,await q.ProveedoresAsync(a));
        filtro.Proveedores=new[]{int.MaxValue}; Assert.Empty(await q.ListarAsync(filtro));
        var sinActor=new ConsultarMateriales(Actor(Role.None),new NpgsqlMaterialesConsulta(Factory())); Assert.Throws<UnauthorizedAccessException>(()=>{_ = sinActor.ListarAsync(filtro);});
    }
    [Fact]
    public async Task VistaInventarioResumenCuatroTabsFiltrosYPermisosContraPostgres()
    {
        string nombre="Vista"+Guid.NewGuid().ToString("N"), obraNombre="Obra"+Guid.NewGuid().ToString("N");
        int obra=Convert.ToInt32(Sql("INSERT INTO public.obras(nombre,activa) VALUES (@nombre,true) RETURNING id",new NpgsqlParameter("nombre",obraNombre)));
        int material=Material(3m); Sql("UPDATE public.materiales SET nombre=@nombre,stockminimo=5 WHERE id=@id RETURNING id",new NpgsqlParameter("nombre",nombre),new NpgsqlParameter("id",material));
        int recorte=Convert.ToInt32(Sql("INSERT INTO public.recortes(id_material,medida,cantidad) VALUES (@id,2,2) RETURNING id",new NpgsqlParameter("id",material)));
        int agotado=Convert.ToInt32(Sql("INSERT INTO public.recortes(id_material,medida,cantidad) VALUES (@id,3,0) RETURNING id",new NpgsqlParameter("id",material)));
        int herramienta=Convert.ToInt32(Sql("INSERT INTO public.herramientas(nombre,estado,id_obra) VALUES (@nombre,'Óptima',@obra) RETURNING id_herramienta",new NpgsqlParameter("nombre",nombre),new NpgsqlParameter("obra",obra)));
        int insumo=Insumo(obra); Sql("UPDATE public.insumos SET nombre=@nombre WHERE id_insumo=@id RETURNING id_insumo",new NpgsqlParameter("nombre",nombre),new NpgsqlParameter("id",insumo));
        var q=new ConsultarInventario(Actor(Role.Galpon),new NpgsqlInventarioConsulta(Factory())); var ct=CancellationToken.None;
        var resumen=await q.ResumenAsync(ct);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.materiales")),resumen.total_materiales);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.materiales WHERE stockactual<=0")),resumen.materiales_sin_stock);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.materiales WHERE stockactual>0 AND stockminimo>0 AND stockactual<=stockminimo")),resumen.materiales_stock_bajo);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.recortes WHERE cantidad>0")),resumen.total_recortes);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.herramientas")),resumen.total_herramientas);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.herramientas WHERE id_obra IS NOT NULL")),resumen.herramientas_en_obra);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.insumos")),resumen.total_insumos);
        Assert.Equal(Convert.ToInt32(Sql("SELECT count(*) FROM public.insumos WHERE id_obra IS NOT NULL AND consumido=false")),resumen.insumos_en_obra);
        string categoria=Convert.ToString(Sql("SELECT nombre FROM public.categorias WHERE id_categoria=1"));
        Assert.Contains(await q.MaterialesAsync(nombre,categoria,"STOCK BAJO",ct),x=>x.id==material && x.stock_actual==3m);
        Assert.DoesNotContain(await q.MaterialesAsync(nombre,categoria,"SIN STOCK",ct),x=>x.id==material);
        var recortes=await q.RecortesAsync(nombre,categoria,ct); Assert.Contains(recortes,x=>x.id==recorte && x.cantidad==2m); Assert.DoesNotContain(recortes,x=>x.id==agotado);
        Assert.Contains(await q.HerramientasAsync(nombre,"EN OBRA",obraNombre,ct),x=>x.id==herramienta); Assert.Empty(await q.HerramientasAsync(nombre,"DISPONIBLE",obraNombre,ct));
        Assert.Contains(await q.InsumosAsync(nombre,"EN OBRA",obraNombre,ct),x=>x.id==insumo); Assert.Empty(await q.InsumosAsync(nombre,"CONSUMIDO",obraNombre,ct));
        Assert.Contains(categoria,await q.CategoriasAsync(ct)); Assert.Contains(obraNombre,await q.ObrasActivasAsync(ct)); Assert.Contains(obraNombre,await q.ObrasConHerramientasAsync(ct));
        var prohibido=new ConsultarInventario(Actor(Role.None),new NpgsqlInventarioConsulta(Factory())); Assert.Throws<UnauthorizedAccessException>(()=>{_ = prohibido.ResumenAsync(ct);});
        Assert.Equal(3m,Stock(material)); Assert.Equal(obra,ObraInsumo(insumo)); Assert.Equal(2m,Convert.ToDecimal(Sql("SELECT cantidad FROM public.recortes WHERE id=@id",new NpgsqlParameter("id",recorte))));
    }
    [Fact]
    public async Task ConsultasMovimientoEInsumosDevuelvenDetalleHistorialYCatalogos()
    {
        int origen=Obra(), destino=Obra(), insumo=Insumo(origen), material=Material(10m); var solicitud=ConInsumo(material,insumo,destino); int movimiento=await Crear(solicitud);
        var historial=new ConsultarHistorialMovimientos(Actor(Role.Galpon),new NpgsqlHistorialMovimientoConsulta(Factory()));
        var filas=(await historial.ListarAsync(new DateTime(2025,1,1),new DateTime(2025,1,1),"Envio a Obra")).Where(x=>x.IdMovimiento==movimiento).ToList();
        Assert.Equal(2,filas.Count); Assert.Contains(filas,x=>!x.EsInsumo && x.IdMaterial==material && x.Cantidad==2m && x.IdLinea.HasValue); Assert.Contains(filas,x=>x.EsInsumo && x.IdMaterial==insumo);
        Assert.DoesNotContain(await historial.ListarAsync(new DateTime(2025,1,2),null,null),x=>x.IdMovimiento==movimiento);
        var editar=new ConsultarMovimientoEdicion(Actor(),new NpgsqlMovimientoEdicionConsulta(Factory())); var detalle=await editar.ObtenerAsync(movimiento);
        Assert.Equal(solicitud.Cabecera.Remito,detalle.Cabecera.Remito); Assert.Contains(detalle.Lineas,x=>x.Linea?.MaterialId==material && x.Linea.Cantidad==2m); Assert.Contains(detalle.Lineas,x=>x.InsumoId==insumo);
        var insumos=new ConsultarInsumos(Actor(Role.Galpon),new NpgsqlInsumosConsulta(Factory())); Assert.Contains(await insumos.InsumosAsync(),x=>x.Id==insumo && x.ObraId==destino); Assert.Contains(await insumos.ObrasAsync(),x=>x.Id==destino);
        string nombre=Convert.ToString(Sql("SELECT nombre FROM public.insumos WHERE id_insumo=@id",new NpgsqlParameter("id",insumo))); Assert.Contains(await insumos.HistorialAsync(),x=>x.Insumo==nombre);
        int flete; using(var u=new NpgsqlUnitOfWork(Factory())) flete=await new GestionarFletes(Actor(Role.Galpon),u,new NpgsqlFletes(u)).CrearAsync("Flete "+Guid.NewGuid().ToString("N"));
        var catalogos=new ConsultarCatalogosMovimiento(Actor(Role.Galpon),new NpgsqlCatalogosMovimientoConsulta(Factory())); var cat=await catalogos.ObtenerAsync();
        Assert.Contains(cat.Tipos,x=>x.Id==4); Assert.Contains(cat.Destinos,x=>x.Tipo=="OBRA" && x.Id==destino); Assert.Contains(cat.Fletes,x=>x.Id==flete && x.Activo);
        using(var u=new NpgsqlUnitOfWork(Factory())) await new GestionarFletes(Actor(Role.Galpon),u,new NpgsqlFletes(u)).DesactivarAsync(flete);
        Assert.Contains((await catalogos.ObtenerAsync()).Fletes,x=>x.Id==flete && !x.Activo);
        var denegado=new ConsultarMovimientoEdicion(Actor(Role.Galpon),new NpgsqlMovimientoEdicionConsulta(Factory())); Assert.Throws<UnauthorizedAccessException>(()=>{_ = denegado.ObtenerAsync(movimiento);});
        Assert.Equal(8m,Stock(material)); Assert.Equal(destino,ObraInsumo(insumo));
    }
    [Fact]
    public async Task PrecioEventualActualizaTotalSoloLineaEventualYControlaPermiso()
    {
        var solicitud=Solicitud(1,3,1m); solicitud.Lineas=new List<LineaMovimiento>{new LineaMovimiento{CompraEventual=true,MaterialEventual="Eventual",Cantidad=2m,PrecioUnitario=3m}};
        int id=await Crear(solicitud); int linea=Convert.ToInt32(Sql("SELECT id_movimientomaterial FROM public.movimientomaterial WHERE id_movimiento=@id",new NpgsqlParameter("id",id)));
        using(var u=new NpgsqlUnitOfWork(Factory())) Assert.Equal(9m,await new ActualizarPrecioEventual(Actor(),u,new NpgsqlPrecioEventual(u)).EjecutarAsync(linea,4.5m));
        Assert.Equal(9m,Convert.ToDecimal(Sql("SELECT precio_total FROM public.movimientos WHERE id_movimiento=@id",new NpgsqlParameter("id",id))));
        using(var u=new NpgsqlUnitOfWork(Factory())) await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new ActualizarPrecioEventual(Actor(Role.Galpon),u,new NpgsqlPrecioEventual(u)).EjecutarAsync(linea,5m));
        int material=Material(5m), normal=await Crear(Solicitud(material,7,1m)); int lineaNormal=Convert.ToInt32(Sql("SELECT id_movimientomaterial FROM public.movimientomaterial WHERE id_movimiento=@id",new NpgsqlParameter("id",normal)));
        using(var u=new NpgsqlUnitOfWork(Factory())) await Assert.ThrowsAsync<InvalidOperationException>(()=>new ActualizarPrecioEventual(Actor(),u,new NpgsqlPrecioEventual(u)).EjecutarAsync(lineaNormal,5m));
        Assert.Equal(4m,Stock(material)); Assert.Equal(9m,Convert.ToDecimal(Sql("SELECT precio_total FROM public.movimientos WHERE id_movimiento=@id",new NpgsqlParameter("id",id))));
    }
    private static void AssertRemitoDuplicado(InvalidOperationException error,string remito)
    {
        Assert.Equal($"Ya existe un movimiento con el remito '{remito.Trim()}'.",error.Message);
        var pg=Assert.IsType<PostgresException>(error.InnerException); Assert.Equal("23505",pg.SqlState); Assert.Equal("movimientos_nroremito_key",pg.ConstraintName);
    }
    [Fact]
    public async Task AltaConRemitoDuplicadoDevuelveMensajeLegacyYSinResiduos()
    {
        int origen=Obra(),destino=Obra(),insumo=Insumo(origen),material=Material(10m);
        var existente=Solicitud(Material(5m),7,1m); int id=await Crear(existente);
        var duplicado=ConInsumo(material,insumo,destino); duplicado.Cabecera.Remito="  "+existente.Cabecera.Remito+"  ";
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>Crear(duplicado)); AssertRemitoDuplicado(error,duplicado.Cabecera.Remito);
        Assert.Equal(10m,Stock(material)); Assert.Equal(origen,ObraInsumo(insumo)); Assert.Equal(0L,Historial(insumo));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos WHERE nroremito=@remito",new NpgsqlParameter("remito",existente.Cabecera.Remito))));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_movimiento=@id",new NpgsqlParameter("id",id))));
        Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_material=@id",new NpgsqlParameter("id",material))));
    }
    [Fact]
    public async Task EdicionConRemitoDuplicadoRevierteStockEInsumosAnterioresYNuevos()
    {
        int origen=Obra(),destino=Obra(),otro=Obra(),insumo=Insumo(origen),nuevoInsumo=Insumo(origen),material=Material(10m),nuevoMaterial=Material(20m);
        var original=ConInsumo(material,insumo,destino); int id=await Crear(original);
        var existente=Solicitud(Material(5m),7,1m); await Crear(existente);
        var duplicado=ConInsumo(nuevoMaterial,nuevoInsumo,otro); duplicado.Cabecera.Remito=existente.Cabecera.Remito;
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>Editar(id,duplicado)); AssertRemitoDuplicado(error,duplicado.Cabecera.Remito);
        Assert.Equal(8m,Stock(material)); Assert.Equal(20m,Stock(nuevoMaterial)); Assert.Equal(destino,ObraInsumo(insumo)); Assert.Equal(origen,ObraInsumo(nuevoInsumo));
        Assert.Equal(1L,Historial(insumo)); Assert.Equal(0L,Historial(nuevoInsumo));
        Assert.Equal(original.Cabecera.Remito,Convert.ToString(Sql("SELECT nroremito FROM public.movimientos WHERE id_movimiento=@id",new NpgsqlParameter("id",id))));
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientomaterial WHERE id_movimiento=@id AND id_material=@material",new NpgsqlParameter("id",id),new NpgsqlParameter("material",material))));
    }
    [Fact]
    public async Task OtraViolacionUniqueNoSePresentaComoRemitoDuplicado()
    {
        string suffix=Guid.NewGuid().ToString("N"), index="test_unique_"+suffix, valor="otra-"+suffix;
        try {
            Sql("CREATE UNIQUE INDEX "+index+" ON public.movimientos(observaciones) WHERE observaciones='"+valor+"'; SELECT 1");
            var primero=Solicitud(Material(5m),7,1m); primero.Cabecera.Observaciones=valor; await Crear(primero);
            int material=Material(10m); var segundo=Solicitud(material,7,1m); segundo.Cabecera.Observaciones=valor;
            var error=await Assert.ThrowsAsync<PostgresException>(()=>Crear(segundo)); Assert.Equal("23505",error.SqlState); Assert.Equal(index,error.ConstraintName); Assert.Equal(10m,Stock(material));
        } finally { Sql("DROP INDEX IF EXISTS public."+index+"; SELECT 1"); }
    }
}
