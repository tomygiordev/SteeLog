using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Reportes;
using SteeLog.Domain.Reportes;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class ValorizacionObraTests
    {
        private sealed class Consultas : IInformeObraConsulta, ICanaletasConsulta
        {
            public int Movimientos, Herramientas, Insumos, Canaletas;
            public Task<IReadOnlyList<InformeMovimientoDetallePdf>> MovimientosAsync(int id, CancellationToken t) { Movimientos++; return Task.FromResult<IReadOnlyList<InformeMovimientoDetallePdf>>(new InformeMovimientoDetallePdf[0]); }
            public Task<IReadOnlyList<InformeHerramientaDetallePdf>> HerramientasAsync(int id, string nombre, CancellationToken t) { Herramientas++; return Task.FromResult<IReadOnlyList<InformeHerramientaDetallePdf>>(new InformeHerramientaDetallePdf[0]); }
            public Task<IReadOnlyList<InformeInsumoDetallePdf>> InsumosAsync(int id, string nombre, CancellationToken t) { Insumos++; return Task.FromResult<IReadOnlyList<InformeInsumoDetallePdf>>(new InformeInsumoDetallePdf[0]); }
            public Task<DateTime?> PrimeraFechaRemitoAsync(int id, CancellationToken t) => Task.FromResult<DateTime?>(null);
            public Task<IReadOnlyList<CanaletaLectura>> ListarAsync(int? id, CancellationToken t) { Canaletas++; return Task.FromResult<IReadOnlyList<CanaletaLectura>>(new CanaletaLectura[0]); }
            public Task<CanaletaLectura> ObtenerAsync(int id, CancellationToken t) => throw new NotSupportedException();
            public Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t) => throw new NotSupportedException();
            public Task<bool> ReferenciadaAsync(string path, CancellationToken t) => throw new NotSupportedException();
            public Task<FotoCanaletaLectura> FotoPorPathAsync(string path, CancellationToken t) => throw new NotSupportedException();
        }

        [Fact]
        public async Task LecturasUiNoConsultaCanaletasYSnapshotConsultaUnaVez()
        {
            var sesion = new Session(); sesion.Start(new Identity("test@example.invalid", Role.Galpon));
            var consulta = new Consultas(); var caso = new ConsultarInformeObra(sesion, consulta, consulta);
            var lecturas = await caso.LecturasAsync(1, "Obra", default);
            Assert.False(lecturas.MostrarValorizacion);
            Assert.Equal(0, consulta.Canaletas);
            Assert.Equal(1, consulta.Movimientos); Assert.Equal(1, consulta.Herramientas); Assert.Equal(1, consulta.Insumos);
            var snapshot = await caso.SnapshotAsync(1, "Galpon", null, null, default);
            Assert.True(snapshot.MostrarValorizacion);
            Assert.Equal(1, consulta.Canaletas);
            Assert.Equal(2, consulta.Movimientos); Assert.Equal(2, consulta.Herramientas); Assert.Equal(2, consulta.Insumos);
        }

        [Fact]
        public void FilasEventualesMantienenOrdenFallbackYDeteccionIndividualPorUniverso()
        {
            var filas = new[] {
                new MovimientoValorizacion { Id = 1, Tipo = "Compra eventual", PrecioTotal = 90 },
                new MovimientoValorizacion { Id = 1, Tipo = "Compra eventual", PrecioTotal = 90 },
                new MovimientoValorizacion { Id = 2, Tipo = "Compra eventual", PrecioTotal = 80, Cantidad = 2, PrecioUnitario = 0 },
                new MovimientoValorizacion { Id = 2, Tipo = "Compra eventual", PrecioTotal = 80, Cantidad = 2, PrecioUnitario = 3, EsCompraEventual = true },
                new MovimientoValorizacion { Id = 3, Tipo = "Envio", Cantidad = 4, PrecioUnitario = 2 }
            };
            Assert.Equal(new decimal[] { 90, 0, 0, 6, 8 }, ValorizacionObra.ValoresFilas(filas, filas));
            Assert.Equal(104m, ValorizacionObra.CalcularValor(filas));
            Assert.Equal(new decimal[] { 0 }, ValorizacionObra.ValoresFilas(new[] { filas[2] }, filas));
            Assert.Equal(new decimal[] { 80 }, ValorizacionObra.ValoresFilas(new[] { filas[2] }, new[] { filas[2] }));
        }

        [Fact]
        public void BalanceConservaPreciosDeCadaMovimientoYUltimoPrecio()
        {
            var datos = new[] {
                new MovimientoValorizacion { Fecha = new DateTime(2026, 1, 1), Destino = "OBRA", Cantidad = 3, PrecioUnitario = 10 },
                new MovimientoValorizacion { Fecha = new DateTime(2026, 1, 2), Origen = "obra", Cantidad = 1, PrecioUnitario = 4 },
                new MovimientoValorizacion { Fecha = new DateTime(2026, 1, 3), Origen = "Obra", Destino = "obra", Cantidad = 2, PrecioUnitario = 7 }
            };
            var balance = ValorizacionObra.BalanceMaterial(datos, "Obra");
            Assert.Equal(5m, balance.Enviado); Assert.Equal(3m, balance.Devuelto);
            Assert.Equal(2m, balance.Neto); Assert.Equal(7m, balance.PrecioUnitario); Assert.Equal(26m, balance.ValorNeto);
        }

        [Theory]
        [InlineData(true, 5, 118)]
        [InlineData(false, 0, 0)]
        public void ResumenConservaReglasHistoricas(bool mostrar, int fletes, int pendiente)
        {
            var items = new[] {
                new MovimientoValorizacion { Id = 1, Remito = " A ", Tipo = "ENVÍO", Material = "Chapa", Cantidad = 2, PrecioUnitario = 10, CostoFlete = 5 },
                new MovimientoValorizacion { Id = 1, Remito = "a", Tipo = "ENVÍO", Material = "chapa", Cantidad = 1, PrecioUnitario = 10, CostoFlete = 5 },
                new MovimientoValorizacion { Id = 2, Tipo = "COMPRA EVENTUAL", Material = "Recorte", EsRecorte = true, PrecioTotal = 80 },
                new MovimientoValorizacion { Id = 3, Tipo = "DEVOLUCION", Material = "Chapa", Cantidad = 1, PrecioUnitario = 10 },
                new MovimientoValorizacion { Id = 4, Tipo = "APORTE", Cantidad = 2, PrecioUnitario = 3 }
            };
            var actual = ValorizacionObra.CalcularResumen(items, mostrar, 7);
            Assert.Equal(1, actual.CantRemitos);
            Assert.Equal(4, actual.CantMovimientos);
            Assert.Equal(1, actual.TotalMateriales);
            Assert.Equal((decimal)fletes, actual.TotalFletes);
            Assert.Equal((decimal)pendiente, actual.TotalPendiente);
            Assert.Equal(126m, ValorizacionObra.CalcularValor(items));
        }

        [Fact]
        public void CompraEventualConDetalleNoSumaTotalComoSegundoPrecio()
        {
            var items = new[] { new MovimientoValorizacion { Id = 1, Tipo = "COMPRA EVENTUAL", Cantidad = 2, PrecioUnitario = 4, PrecioTotal = 100 } };
            Assert.Equal(8m, ValorizacionObra.CalcularValor(items));
            Assert.Equal(0m, ValorizacionObra.CalcularValor(null));
        }
    }
}
