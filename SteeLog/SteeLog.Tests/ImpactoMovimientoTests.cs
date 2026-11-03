using System;
using SteeLog.Domain.Inventario;
using Xunit;

namespace SteeLog.Tests
{
    public class ImpactoMovimientoTests
    {
        [Fact]
        public void ReversionUsaDiferenciaRealNoCantidad()
        {
            var efecto = ImpactoMovimiento.EfectoReal(1m, 1.01m);
            Assert.Equal(-0.01m, ImpactoMovimiento.ParaReversion(efecto, 0.005m, ImpactoStock.Entrada));
            Assert.Equal(0m, ImpactoMovimiento.ParaReversion(0m, 0.005m, ImpactoStock.Salida));
        }
        [Theory]
        [InlineData(ImpactoStock.Entrada, -1.25)]
        [InlineData(ImpactoStock.Salida, 1.25)]
        [InlineData(ImpactoStock.SinImpacto, 0)]
        public void HistoricoExactoConservaCompatibilidad(ImpactoStock tipo, double esperado)
        {
            Assert.Equal((decimal)esperado, ImpactoMovimiento.ParaReversion(null, 1.25m, tipo));
        }
        [Fact]
        public void HistoricoFraccionarioSinSnapshotNoSeRevierte()
        {
            Assert.Throws<InvalidOperationException>(() => ImpactoMovimiento.ParaReversion(null, 0.005m, ImpactoStock.Entrada));
            Assert.Equal(0m, ImpactoMovimiento.ParaReversion(null, 0.005m, ImpactoStock.SinImpacto));
        }
    }
}
