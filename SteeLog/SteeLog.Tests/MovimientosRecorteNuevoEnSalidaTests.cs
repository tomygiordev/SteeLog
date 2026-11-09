using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Movimientos;
using Xunit;

namespace SteeLog.Tests
{
    // Regla de negocio: en una salida, un recorte que todavía no existe (mismo material y medidas) se crea
    // con stock igual a lo cargado y las líneas lo consumen por el camino de siempre. El stock del material no se toca.
    public sealed class MovimientosRecorteNuevoEnSalidaTests
    {
        private const int Envio = 1, Compra = 2, Traslado = 3;

        private sealed class UowFake : IUnitOfWork
        {
            public Task BeginAsync(CancellationToken t = default) => Task.CompletedTask;
            public Task CommitAsync(CancellationToken t = default) => Task.CompletedTask;
            public Task RollbackAsync() => Task.CompletedTask;
        }

        private sealed class InventarioEnMemoria : IInventarioEscritura
        {
            public readonly Dictionary<int, decimal> Stock = new Dictionary<int, decimal>();
            public Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> ids, CancellationToken t = default)
            { var d = new Dictionary<int, decimal>(); foreach (var id in ids) d[id] = Stock[id]; return Task.FromResult<IDictionary<int, decimal>>(d); }
            public Task GuardarSaldoAsync(int id, decimal saldo, CancellationToken t = default) { Stock[id] = saldo; return Task.CompletedTask; }
        }

        private sealed class RecorteFila { public int Id, MaterialId; public decimal Ancho, Altura, Medida, Cantidad; }

        private sealed class MovimientosEnMemoria : IMovimientosEscritura
        {
            public readonly Dictionary<int, RecorteFila> Recortes = new Dictionary<int, RecorteFila>();
            public readonly List<decimal> InicialesAlCrear = new List<decimal>();
            public readonly List<decimal> SaldosRegistrados = new List<decimal>();
            public readonly Dictionary<int, MovimientoPersistido> Movimientos = new Dictionary<int, MovimientoPersistido>();
            private int siguienteRecorte = 100, siguienteMovimiento = 1;

            public int NuevoRecorte(int material, decimal ancho, decimal altura, decimal medida, decimal cantidad)
            { var id = siguienteRecorte++; Recortes[id] = new RecorteFila { Id = id, MaterialId = material, Ancho = ancho, Altura = altura, Medida = medida, Cantidad = cantidad }; return id; }

            private static string NombreTipo(int id) => id == Envio ? "Envio a Obra" : id == Compra ? "Compra" : "Traslado";
            public Task<MovimientoPersistido> BloquearAsync(int id, CancellationToken t = default)
                => Task.FromResult(Movimientos.TryGetValue(id, out var m) ? m : null);
            public Task<TipoMovimiento> TipoAsync(int id, CancellationToken t = default) => Task.FromResult(new TipoMovimiento { Nombre = NombreTipo(id) });
            public Task<IDictionary<int, int>> ObtenerMaterialesRecortesAsync(IReadOnlyList<int> ids, CancellationToken t = default)
                => Task.FromResult<IDictionary<int, int>>(ids.Distinct().ToDictionary(i => i, i => Recortes[i].MaterialId));
            public Task<IDictionary<int, decimal>> BloquearRecortesAsync(IReadOnlyList<int> ids, CancellationToken t = default)
                => Task.FromResult<IDictionary<int, decimal>>(ids.Distinct().ToDictionary(i => i, i => Recortes[i].Cantidad));
            public Task<IDictionary<int, InsumoBloqueado>> BloquearInsumosAsync(IReadOnlyList<int> ids, CancellationToken t = default)
                => Task.FromResult<IDictionary<int, InsumoBloqueado>>(new Dictionary<int, InsumoBloqueado>());
            public Task<decimal> GuardarRecorteAsync(int id, decimal saldo, CancellationToken t = default) { Recortes[id].Cantidad = saldo; return Task.FromResult(saldo); }
            public Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, decimal cantidadInicial, CancellationToken t = default)
            {
                var existente = Recortes.Values.OrderBy(r => r.Id).FirstOrDefault(r => r.MaterialId == materialId && r.Ancho == ancho && r.Altura == altura && r.Medida == medida);
                if (existente != null) return Task.FromResult(existente.Id);
                InicialesAlCrear.Add(cantidadInicial);
                return Task.FromResult(NuevoRecorte(materialId, ancho, altura, medida, cantidadInicial));
            }
            public Task<int> CrearCabeceraAsync(CabeceraMovimiento c, CancellationToken t = default)
            {
                var id = siguienteMovimiento++;
                Movimientos[id] = new MovimientoPersistido { Id = id, Cabecera = c, TipoNombre = NombreTipo(c.TipoMovimientoId) };
                return Task.FromResult(id);
            }
            public Task ActualizarCabeceraAsync(int id, CabeceraMovimiento c, CancellationToken t = default)
            { Movimientos[id].Cabecera = c; Movimientos[id].TipoNombre = NombreTipo(c.TipoMovimientoId); return Task.CompletedTask; }
            public Task AgregarLineaAsync(int movimientoId, LineaMovimiento l, decimal saldo, CancellationToken t = default)
            { Movimientos[movimientoId].Lineas.Add(l); SaldosRegistrados.Add(saldo); return Task.CompletedTask; }
            public Task RegistrarTrasladoAsync(int movimientoId, TrasladoInsumo x, CancellationToken t = default) => Task.CompletedTask;
            public Task RevertirTrasladoAsync(TrasladoInsumo x, CancellationToken t = default) => Task.CompletedTask;
            public Task EliminarLineasYTrasladosAsync(int id, CancellationToken t = default) { Movimientos[id].Lineas = new List<LineaMovimiento>(); return Task.CompletedTask; }
            public Task EliminarCabeceraAsync(int id, CancellationToken t = default) { Movimientos.Remove(id); return Task.CompletedTask; }
        }

        private sealed class Entorno
        {
            public readonly UowFake Uow = new UowFake();
            public readonly InventarioEnMemoria Inventario = new InventarioEnMemoria();
            public readonly MovimientosEnMemoria Repo = new MovimientosEnMemoria();
            private readonly Session sesion = new Session();
            public Entorno(decimal stockMaterial) { sesion.Start(new Identity("admin@tesis.local", Role.Admin)); Inventario.Stock[1] = stockMaterial; }
            public Task<int> Registrar(MovimientoSolicitud s) => new RegistrarMovimiento(sesion, Uow, Repo, Inventario).EjecutarAsync(s);
            public Task<int> Editar(int id, MovimientoSolicitud s) => new EditarMovimiento(sesion, Uow, Repo, Inventario).EjecutarAsync(id, s);
            public Task<int> Eliminar(int id) => new EliminarMovimiento(sesion, Uow, Repo, Inventario).EjecutarAsync(id);
        }

        private static LineaMovimiento Recorte(decimal ancho, decimal altura, decimal cantidad)
            => new LineaMovimiento { MaterialId = 1, EsRecorte = true, AnchoRecorte = ancho, AlturaRecorte = altura, MedidaRecorte = 0m, Cantidad = cantidad };

        private static MovimientoSolicitud Solicitud(int tipo, string destino, params LineaMovimiento[] lineas) => new MovimientoSolicitud
        {
            Cabecera = new CabeceraMovimiento { Fecha = new DateTime(2025, 1, 1), Remito = "R-" + Guid.NewGuid().ToString("N"), OrigenId = 2, TipoOrigen = "UBICACION", DestinoId = 4, TipoDestino = destino, TipoMovimientoId = tipo },
            Lineas = lineas.ToList(), OrigenNombre = "Deposito", DestinoNombre = "Destino"
        };
        private static MovimientoSolicitud EnvioAObra(params LineaMovimiento[] lineas) => Solicitud(Envio, "OBRA", lineas);

        [Fact]
        public async Task SalidaConRecorteNuevoLoCreaConLoCargadoLoConsumeYNoTocaElMaterial()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            Assert.Equal(new[] { 3m }, e.Repo.InicialesAlCrear.ToArray());
            var recorte = Assert.Single(e.Repo.Recortes.Values);
            Assert.Equal(0m, recorte.Cantidad);
            var linea = Assert.Single(e.Repo.Movimientos[id].Lineas);
            Assert.Equal(recorte.Id, linea.RecorteId);
            Assert.Equal(-3m, linea.ImpactoReal);
            Assert.Equal(new[] { 0m }, e.Repo.SaldosRegistrados.ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task DosLineasDelMismoRecorteNuevoNacenConLaSumaYLoConsumenEnOrden()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m), Recorte(2m, 3m, 2m)));
            Assert.Equal(new[] { 5m }, e.Repo.InicialesAlCrear.ToArray());
            Assert.Equal(0m, Assert.Single(e.Repo.Recortes.Values).Cantidad);
            Assert.Equal(new[] { -3m, -2m }, e.Repo.Movimientos[id].Lineas.Select(l => l.ImpactoReal.Value).ToArray());
            Assert.Equal(new[] { 2m, 0m }, e.Repo.SaldosRegistrados.ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task SalidaConMedidasDeRecorteExistenteSinStockSuficienteSigueFallandoYNoCreaNada()
        {
            var e = new Entorno(10m);
            int existente = e.Repo.NuevoRecorte(1, 2m, 3m, 0m, 1m);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m))));
            Assert.Contains("Stock insuficiente", error.Message);
            Assert.Empty(e.Repo.InicialesAlCrear);
            Assert.Equal(1m, e.Repo.Recortes[existente].Cantidad);
        }

        [Fact]
        public async Task SalidaConMedidasDeRecorteExistenteConStockDescuentaDelExistenteSinSumarleNada()
        {
            var e = new Entorno(10m);
            int existente = e.Repo.NuevoRecorte(1, 2m, 3m, 0m, 5m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            Assert.Empty(e.Repo.InicialesAlCrear);
            Assert.Equal(2m, e.Repo.Recortes[existente].Cantidad);
            var linea = Assert.Single(e.Repo.Movimientos[id].Lineas);
            Assert.Equal(existente, linea.RecorteId);
            Assert.Equal(-3m, linea.ImpactoReal);
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task EntradaConRecorteNuevoNaceEnCeroYLaEntradaLoSuma()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(Solicitud(Compra, "UBICACION", Recorte(2m, 3m, 3m)));
            Assert.Equal(new[] { 0m }, e.Repo.InicialesAlCrear.ToArray());
            Assert.Equal(3m, Assert.Single(e.Repo.Recortes.Values).Cantidad);
            Assert.Equal(3m, Assert.Single(e.Repo.Movimientos[id].Lineas).ImpactoReal);
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task MovimientoSinImpactoConRecorteNuevoSigueRechazandose()
        {
            var e = new Entorno(10m);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Registrar(Solicitud(Traslado, "UBICACION", Recorte(2m, 3m, 3m))));
            Assert.Equal("Stock insuficiente.", error.Message);
            Assert.Empty(e.Repo.Recortes);
        }

        [Fact]
        public async Task EliminarLaSalidaQueCreoElRecorteLeDevuelveLoConsumidoYNoTocaElMaterial()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            await e.Eliminar(id);
            Assert.Empty(e.Repo.Movimientos);
            Assert.Equal(3m, Assert.Single(e.Repo.Recortes.Values).Cantidad);   // el recorte queda existiendo con lo que se había cargado
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task EditarLaSalidaRevierteLasLineasAnterioresYAplicaLasNuevas()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            // Mismas medidas: ya existe (quedó en 0), se le devuelven 3 al revertir y se consumen 2.
            await e.Editar(id, EnvioAObra(Recorte(2m, 3m, 2m)));
            var original = e.Repo.Recortes.Values.Single();
            Assert.Equal(1m, original.Cantidad);
            Assert.Equal(-2m, Assert.Single(e.Repo.Movimientos[id].Lineas).ImpactoReal);
            // Otras medidas: es un recorte nuevo que nace con 4; el anterior recupera lo que se le había consumido.
            await e.Editar(id, EnvioAObra(Recorte(5m, 5m, 4m)));
            Assert.Equal(2, e.Repo.Recortes.Count);
            Assert.Equal(3m, original.Cantidad);
            Assert.Equal(0m, e.Repo.Recortes.Values.Single(r => r.Ancho == 5m).Cantidad);
            Assert.Equal(new[] { 3m, 4m }, e.Repo.InicialesAlCrear.ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }
    }
}
