using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Domain.OrdenesCompra;
using SteeLog.Infrastructure.OrdenesCompra;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Persistence;
using Xunit;

[Trait("Category", "Integration")]
public sealed class OrdenCompraIntegrationTests
{
    private static string ConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_ORDENES_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_ORDENES_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$"))
            throw new InvalidOperationException("Falta configuracion de DB efimera steelog_test_<GUID>; ejecutar scripts/tests/ordenes-integracion.ps1.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435)
            throw new InvalidOperationException("Destino de pruebas no permitido.");
        using (var c = new NpgsqlConnection(cs)) {
            c.Open(); using (var cmd = new NpgsqlCommand("SELECT current_database()", c))
                if ((string)cmd.ExecuteScalar() != db) throw new InvalidOperationException("Identidad de DB inesperada.");
        }
        return cs;
    }
    private static PostgresConnectionFactory Factory() => new PostgresConnectionFactory(ConnectionString());
    private static Session Actor(Role role) { var s = new Session(); if (role != Role.None) s.Start(new Identity("test@example.invalid", role)); return s; }
    private static CabeceraOrden Header(string number = null) => new CabeceraOrden { Fecha = new DateTime(2025, 1, 2), Numero = number ?? Guid.NewGuid().ToString("N"), ProveedorId = 1, Flete = 3 };
    private static LineaNueva[] Lines(int material, int quantity = 10) => new[] { new LineaNueva { MaterialId = material, Pedida = quantity } };
    private static object Sql(string sql, params NpgsqlParameter[] parameters) {
        using (var c = new NpgsqlConnection(ConnectionString())) { c.Open(); using (var cmd = new NpgsqlCommand(sql, c)) { cmd.CommandTimeout = 10; cmd.Parameters.AddRange(parameters); return cmd.ExecuteScalar(); } }
    }
    private static int Material(decimal stock) => Convert.ToInt32(Sql("INSERT INTO public.materiales(nombre,preciounitario,stockactual,stockminimo,id_categoria) VALUES (@nombre,0,@stock,0,1) RETURNING id", new NpgsqlParameter("nombre", Guid.NewGuid().ToString("N")), new NpgsqlParameter("stock", stock)));
    private static decimal Stock(int id) => Convert.ToDecimal(Sql("SELECT stockactual FROM public.materiales WHERE id=@id", new NpgsqlParameter("id", id)));
    private static int Line(int id) => Convert.ToInt32(Sql("SELECT id_detalle FROM public.ordenes_compra_detalle WHERE id_orden_compra=@id", new NpgsqlParameter("id", id)));
    private static async Task<int> Create(int material, int qty = 10) { using (var u = new NpgsqlUnitOfWork(Factory())) { var r = new NpgsqlOrdenes(u); return await new CrearOrden(Actor(Role.Admin), u, r).EjecutarAsync(Header(), Lines(material, qty)); } }
    private static async Task<EstadoOrden> Receive(int id, int line, int quantity, Role role = Role.Galpon) { using (var u = new NpgsqlUnitOfWork(Factory())) { var r = new NpgsqlOrdenes(u); return await new ConfirmarRecepcion(Actor(role), u, r, new NpgsqlInventario(u)).EjecutarAsync(id, new Dictionary<int, decimal> { [line] = quantity }, null, null); } }
    private static async Task Change(int id, int line, int material) { using (var u = new NpgsqlUnitOfWork(Factory())) { var r = new NpgsqlOrdenes(u); await new CambiarMaterialOrden(Actor(Role.Admin), u, r, new NpgsqlInventario(u)).EjecutarAsync(id, line, material); } }

    [Fact] public async Task CrearEditarYRecibirEsAcumulativoYCompletoNoDuplica()
    {
        int material = Material(20); int id = await Create(material); Assert.True(id > 0);
        using (var u = new NpgsqlUnitOfWork(Factory())) { var r = new NpgsqlOrdenes(u); Assert.Equal(id, await new EditarOrden(Actor(Role.Admin), u, r).EjecutarAsync(id, Header(" editada "), Lines(material))); }
        int line = Line(id); Assert.Equal(EstadoOrden.RecibidaParcial, await Receive(id, line, 4)); Assert.Equal(24m, Stock(material));
        Assert.Equal(EstadoOrden.RecibidaParcial, await Receive(id, line, 4)); Assert.Equal(24m, Stock(material));
        var q = new ConsultarOrdenes(Actor(Role.Admin), new NpgsqlOrdenesConsulta(Factory()));
        var partial = await q.ObtenerAsync(id); Assert.Equal(10m, partial.Lineas.Single().Pedida); Assert.Equal(4m, partial.Lineas.Single().Recibida); Assert.Equal(6m, partial.Lineas.Single().Faltante);
        var list = (await q.ListarAsync(null, false, false)).Single(x => x.Id == id); Assert.Equal(10m, list.TotalPedido); Assert.Equal(4m, list.TotalRecibido);
        var pending = (await q.PendientesAsync()).Single(x => x.Numero == "editada"); Assert.Equal(10m, pending.Pedida); Assert.Equal(4m, pending.Recibida); Assert.Equal(6m, pending.Faltante);
        Assert.Contains(await q.ProveedoresAsync(), x => x.Id == 1); Assert.Contains(await q.MaterialesAsync(), x => x.Id == material && x.Stock == 24m); Assert.False(string.IsNullOrWhiteSpace(await q.DestinoAsync()));
        Assert.Equal(EstadoOrden.RecibidaCompleta, await Receive(id, line, 10)); Assert.Equal(30m, Stock(material));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Receive(id, line, 10)); Assert.Equal(30m, Stock(material));
        var order = await q.ObtenerAsync(id);
        Assert.Equal("editada", order.Numero); Assert.Equal(10m, order.Lineas.Single().Recibida); Assert.Equal(30m, order.Lineas.Single().StockDespues);
        Assert.Contains(await q.ListarAsync(null, false, true), x => x.Id == id);
        var galpon = new ConsultarOrdenes(Actor(Role.Galpon), new NpgsqlOrdenesConsulta(Factory())); Assert.DoesNotContain(await galpon.ListarAsync(null, false, false), x => x.Id == id); Assert.DoesNotContain(await galpon.ListarAsync(null, false, true), x => x.Id == id); Assert.DoesNotContain(await q.PendientesAsync(), x => x.Numero == "editada");
        using (var u = new NpgsqlUnitOfWork(Factory())) { var r = new NpgsqlOrdenes(u); await Assert.ThrowsAsync<InvalidOperationException>(() => new EditarOrden(Actor(Role.Admin), u, r).EjecutarAsync(id, Header(), Lines(material))); }
        using (var u = new NpgsqlUnitOfWork(Factory())) { var r = new NpgsqlOrdenes(u); await Assert.ThrowsAsync<InvalidOperationException>(() => new EliminarOrden(Actor(Role.Admin), u, r).EjecutarAsync(id)); }
    }

    [Fact] public async Task RecepcionesConcurrentesEnConexionesDistintasNoDuplican()
    {
        int material = Material(50); int id = await Create(material); int line = Line(id);
        async Task<bool> TryReceive() { try { await Receive(id, line, 10); return true; } catch (InvalidOperationException) { return false; } }
        using (var blocker = new NpgsqlConnection(ConnectionString())) {
            blocker.Open(); using (var tx = blocker.BeginTransaction()) {
                using (var command = new NpgsqlCommand("SELECT id_orden_compra FROM public.ordenes_compra WHERE id_orden_compra=@id FOR UPDATE", blocker, tx)) { command.Parameters.AddWithValue("id", id); command.ExecuteScalar(); }
                var tasks = new[] { Task.Run((Func<Task<bool>>)TryReceive), Task.Run((Func<Task<bool>>)TryReceive) };
                try {
                    var deadline = DateTime.UtcNow.AddSeconds(15); int waiting = 0;
                    while (DateTime.UtcNow < deadline) {
                        waiting = Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%SELECT estado,fecha,nro_orden%'"));
                        if (waiting >= 2) break; await Task.Delay(30);
                    }
                    Assert.True(waiting >= 2, "Ambas recepciones deben esperar el bloqueo de cabecera antes de liberarlo.");
                } finally { tx.Rollback(); }
                var outcomes = await Task.WhenAll(tasks); Assert.Single(outcomes, x => x); Assert.Equal(60m, Stock(material));
            }
        }
    }

    [Fact] public async Task CambioTransfiereSaldoYSnapshotsYRechazaSaldoInsuficiente()
    {
        int a = Material(10), b = Material(20); int id = await Create(a); int line = Line(id); await Receive(id, line, 4); await Change(id, line, b);
        Assert.Equal(10m, Stock(a)); Assert.Equal(24m, Stock(b));
        var q = new ConsultarOrdenes(Actor(Role.Admin), new NpgsqlOrdenesConsulta(Factory())); var detail = (await q.ObtenerAsync(id)).Lineas.Single();
        Assert.Equal(b, detail.MaterialId); Assert.Equal(20m, detail.StockAlCrear); Assert.Equal(24m, detail.StockDespues); Assert.Equal(4m, detail.Recibida);
        Sql("UPDATE public.materiales SET stockactual=2 WHERE id=@id RETURNING id", new NpgsqlParameter("id", b));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Change(id, line, a));
        Assert.Equal(10m, Stock(a)); Assert.Equal(2m, Stock(b)); detail = (await q.ObtenerAsync(id)).Lineas.Single();
        Assert.Equal(b, detail.MaterialId); Assert.Equal(4m, detail.Recibida); Assert.Equal(20m, detail.StockAlCrear); Assert.Equal(24m, detail.StockDespues); Assert.Equal("Recibida Parcial", (await q.ObtenerAsync(id)).Estado);
    }

    private sealed class FailAfterStock : IInventarioEscritura
    {
        private readonly IInventarioEscritura inner; private readonly CancellationTokenSource cancel;
        public FailAfterStock(IInventarioEscritura inner, CancellationTokenSource cancel = null) { this.inner = inner; this.cancel = cancel; }
        public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken ct = default) => inner.BloquearAsync(ids, ct);
        public async Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken ct = default) { await inner.GuardarSaldoAsync(id, saldo, ct); if (cancel != null) { cancel.Cancel(); return; } throw new InvalidOperationException("fallo inyectado despues de SQL"); }
    }
    [Theory] [InlineData(false)] [InlineData(true)] public async Task FalloOCancelacionDespuesDeUpdateRevierteTransaccion(bool cancel)
    {
        int material = Material(10); int id = await Create(material); int line = Line(id); using (var source = new CancellationTokenSource()) using (var u = new NpgsqlUnitOfWork(Factory())) {
            var r = new NpgsqlOrdenes(u); var operation = new ConfirmarRecepcion(Actor(Role.Galpon), u, r, new FailAfterStock(new NpgsqlInventario(u), cancel ? source : null));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.EjecutarAsync(id, new Dictionary<int, decimal> { [line] = 4 }, null, null, source.Token));
            else await Assert.ThrowsAsync<InvalidOperationException>(() => operation.EjecutarAsync(id, new Dictionary<int, decimal> { [line] = 4 }, null, null, source.Token));
        }
        Assert.Equal(10m, Stock(material));
        Assert.Equal(material, Convert.ToInt32(Sql("UPDATE public.materiales SET stockactual=stockactual WHERE id=@id RETURNING id", new NpgsqlParameter("id", material))));
        var q = new ConsultarOrdenes(Actor(Role.Admin), new NpgsqlOrdenesConsulta(Factory())); var o = await q.ObtenerAsync(id);
        Assert.Equal("Pendiente", o.Estado); Assert.Equal(0m, o.Lineas.Single().Recibida); Assert.Null(o.Lineas.Single().StockDespues);
    }

    [Fact] public async Task PermisosNoAbrenConexionYTransaccionesSonIndependientes()
    {
        var factory = Factory(); using (var u = new NpgsqlUnitOfWork(factory)) { var r = new NpgsqlOrdenes(u); Assert.Throws<UnauthorizedAccessException>(() => { _ = new CrearOrden(Actor(Role.None), u, r).EjecutarAsync(Header(), Lines(1)); }); Assert.Throws<UnauthorizedAccessException>(() => { _ = new CrearOrden(Actor(Role.Galpon), u, r).EjecutarAsync(Header(), Lines(1)); }); Assert.Throws<UnauthorizedAccessException>(() => { _ = new ConfirmarRecepcion(Actor(Role.None), u, r, new NpgsqlInventario(u)).EjecutarAsync(1, new Dictionary<int, decimal>(), null, null); }); await u.BeginAsync(); await u.RollbackAsync(); }
        int material = Material(8); using (var first = new NpgsqlUnitOfWork(factory)) using (var second = new NpgsqlUnitOfWork(factory)) {
            var a = new NpgsqlOrdenes(first); var b = new NpgsqlOrdenes(second); await first.BeginAsync(); await second.BeginAsync();
            var property = typeof(NpgsqlUnitOfWork).GetProperty("Connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var firstConnection = (NpgsqlConnection)property.GetValue(first); var secondConnection = (NpgsqlConnection)property.GetValue(second);
            Assert.NotEqual(firstConnection.ProcessID, secondConnection.ProcessID);
            int id = await ((IOrdenesEscritura)a).CrearAsync(Header(), Lines(material), "test");
            await new NpgsqlInventario(first).GuardarSaldoAsync(material, 9);
            Assert.Equal(8m, Stock(material));
            Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.ordenes_compra WHERE id_orden_compra=@id", new NpgsqlParameter("id", id))));
            Assert.Empty(await new NpgsqlInventario(second).BloquearAsync(Array.Empty<int>()));
            await first.RollbackAsync(); await second.RollbackAsync(); Assert.Equal(8m, Stock(material));
            Assert.Equal(0L, Convert.ToInt64(Sql("SELECT count(*) FROM public.ordenes_compra WHERE id_orden_compra=@id", new NpgsqlParameter("id", id))));
        }
    }
}
