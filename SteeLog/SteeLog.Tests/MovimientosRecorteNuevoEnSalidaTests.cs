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
    // Regla de negocio: en una salida, una línea de recorte sin id es una pieza creada en el movimiento: la fila de esas medidas
    // (existente o nueva) recibe lo cargado y las líneas la consumen por el camino de siempre (s -> s+n -> s). El stock del material no se toca.
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
            public readonly List<decimal> SaldosRegistrados = new List<decimal>();
            public readonly Dictionary<int, MovimientoPersistido> Movimientos = new Dictionary<int, MovimientoPersistido>();
            public int FilasCreadas, Resoluciones;
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
            public Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken t = default)
            {
                Resoluciones++;
                var existente = Recortes.Values.OrderBy(r => r.Id).FirstOrDefault(r => r.MaterialId == materialId && r.Ancho == ancho && r.Altura == altura && r.Medida == medida);
                if (existente != null) return Task.FromResult(existente.Id);
                FilasCreadas++;
                return Task.FromResult(NuevoRecorte(materialId, ancho, altura, medida, 0m));
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

        private static LineaMovimiento Recorte(decimal ancho, decimal altura, decimal cantidad, int? recorteId = null)
            => new LineaMovimiento { MaterialId = 1, EsRecorte = true, RecorteId = recorteId, AnchoRecorte = ancho, AlturaRecorte = altura, MedidaRecorte = 0m, Cantidad = cantidad };

        private static MovimientoSolicitud Solicitud(int tipo, string destino, params LineaMovimiento[] lineas) => new MovimientoSolicitud
        {
            Cabecera = new CabeceraMovimiento { Fecha = new DateTime(2025, 1, 1), Remito = "R-" + Guid.NewGuid().ToString("N"), OrigenId = 2, TipoOrigen = "UBICACION", DestinoId = 4, TipoDestino = destino, TipoMovimientoId = tipo },
            Lineas = lineas.ToList(), OrigenNombre = "Deposito", DestinoNombre = "Destino"
        };
        private static MovimientoSolicitud EnvioAObra(params LineaMovimiento[] lineas) => Solicitud(Envio, "OBRA", lineas);

        [Fact]
        public async Task SalidaConRecorteNuevoCreaLaFilaLaSumaLaConsumeYNoTocaElMaterial()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            Assert.Equal(1, e.Repo.FilasCreadas);
            var recorte = Assert.Single(e.Repo.Recortes.Values);
            Assert.Equal(0m, recorte.Cantidad);
            var linea = Assert.Single(e.Repo.Movimientos[id].Lineas);
            Assert.Equal(recorte.Id, linea.RecorteId);
            Assert.Equal(-3m, linea.ImpactoReal);
            Assert.Equal(new[] { 0m }, e.Repo.SaldosRegistrados.ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task DosLineasSinIdDelMismoRecorteSeResuelvenUnaVezYSumanUnaSolaVez()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m), Recorte(2m, 3m, 2m)));
            Assert.Equal(1, e.Repo.Resoluciones);
            Assert.Equal(0m, Assert.Single(e.Repo.Recortes.Values).Cantidad);
            Assert.Equal(new[] { -3m, -2m }, e.Repo.Movimientos[id].Lineas.Select(l => l.ImpactoReal.Value).ToArray());
            Assert.Equal(new[] { 2m, 0m }, e.Repo.SaldosRegistrados.ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task CrearYUsarDeNuevoConMedidasDeUnaFilaEnCeroSeGuardaYLaFilaQuedaEnCero()
        {
            var e = new Entorno(10m);
            int fila = e.Repo.NuevoRecorte(1, 2m, 3m, 0m, 0m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            Assert.Equal(0, e.Repo.FilasCreadas);
            Assert.Equal(0m, e.Repo.Recortes[fila].Cantidad);
            var linea = Assert.Single(e.Repo.Movimientos[id].Lineas);
            Assert.Equal(fila, linea.RecorteId);
            Assert.Equal(-3m, linea.ImpactoReal);
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task CrearYUsarConMedidasDeUnaFilaConStockLaDejaIgualPorqueElStockPrevioNoSeConsume()
        {
            var e = new Entorno(10m);
            int fila = e.Repo.NuevoRecorte(1, 2m, 3m, 0m, 5m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            Assert.Equal(5m, e.Repo.Recortes[fila].Cantidad);
            Assert.Equal(-3m, Assert.Single(e.Repo.Movimientos[id].Lineas).ImpactoReal);
            Assert.Equal(new[] { 5m }, e.Repo.SaldosRegistrados.ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task LineaConIdDeRecorteSinStockSuficienteSigueFallandoYNoModificaNada()
        {
            var e = new Entorno(10m);
            int fila = e.Repo.NuevoRecorte(1, 2m, 3m, 0m, 1m);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m, fila))));
            Assert.Contains("Stock insuficiente", error.Message);
            Assert.Equal(0, e.Repo.Resoluciones);
            Assert.Equal(1m, e.Repo.Recortes[fila].Cantidad);
        }

        [Fact]
        public async Task LineaConIdMasLineaSinIdALaMismaFilaSumaSoloLaSinIdYElSaldoFinalEsStockMenosLoDeLaConId()
        {
            var e = new Entorno(10m);
            int fila = e.Repo.NuevoRecorte(1, 2m, 3m, 0m, 5m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 2m, fila), Recorte(2m, 3m, 3m)));
            Assert.Equal(3m, e.Repo.Recortes[fila].Cantidad);   // 5 + 3 (pieza creada) - 2 - 3
            Assert.Equal(new[] { 6m, 3m }, e.Repo.SaldosRegistrados.ToArray());
            Assert.Equal(new[] { -2m, -3m }, e.Repo.Movimientos[id].Lineas.Select(l => l.ImpactoReal.Value).ToArray());
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task EntradaConRecorteSinIdNoSumaDosVecesNiEnFilaNuevaNiExistente()
        {
            var e = new Entorno(10m);
            await e.Registrar(Solicitud(Compra, "UBICACION", Recorte(2m, 3m, 3m)));
            Assert.Equal(1, e.Repo.FilasCreadas);
            var fila = Assert.Single(e.Repo.Recortes.Values);
            Assert.Equal(3m, fila.Cantidad);
            await e.Registrar(Solicitud(Compra, "UBICACION", Recorte(2m, 3m, 2m)));
            Assert.Equal(5m, fila.Cantidad);
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
        public async Task EliminarLaSalidaLeDevuelveLoConsumidoALaFilaCreadaYALaExistente()
        {
            var e = new Entorno(10m);
            int nueva = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            await e.Eliminar(nueva);
            Assert.Equal(3m, Assert.Single(e.Repo.Recortes.Values).Cantidad);   // la fila queda existiendo con lo que se había cargado
            var e2 = new Entorno(10m);
            int fila = e2.Repo.NuevoRecorte(1, 2m, 3m, 0m, 5m);
            int mov = await e2.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            await e2.Eliminar(mov);
            Assert.Equal(8m, e2.Repo.Recortes[fila].Cantidad);
            Assert.Equal(10m, e.Inventario.Stock[1]);
            Assert.Equal(10m, e2.Inventario.Stock[1]);
        }

        [Fact]
        public async Task EditarConLasMismasMedidasRevierteLaAnteriorYAplicaLaNueva()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            await e.Editar(id, EnvioAObra(Recorte(2m, 3m, 2m)));
            Assert.Equal(1, e.Repo.FilasCreadas);
            Assert.Equal(3m, Assert.Single(e.Repo.Recortes.Values).Cantidad);   // 0 +2 (alta) +3 (revierte) -2
            Assert.Equal(-2m, Assert.Single(e.Repo.Movimientos[id].Lineas).ImpactoReal);
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }

        [Fact]
        public async Task EditarConOtrasMedidasCreaLaFilaNuevaYLaAnteriorRecuperaLoConsumido()
        {
            var e = new Entorno(10m);
            int id = await e.Registrar(EnvioAObra(Recorte(2m, 3m, 3m)));
            await e.Editar(id, EnvioAObra(Recorte(5m, 5m, 4m)));
            Assert.Equal(2, e.Repo.FilasCreadas);
            Assert.Equal(3m, e.Repo.Recortes.Values.Single(r => r.Ancho == 2m).Cantidad);
            Assert.Equal(0m, e.Repo.Recortes.Values.Single(r => r.Ancho == 5m).Cantidad);
            Assert.Equal(10m, e.Inventario.Stock[1]);
        }
    }
}
