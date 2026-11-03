using SteeLog.Domain.Inventario;
using Xunit;

namespace SteeLog.Tests
{
    public class ClasificadorMovimientoTests
    {
        [Theory]
        [InlineData("Compra eventual", "OBRA", ImpactoStock.SinImpacto)]
        [InlineData("Aporte del cliente", "OBRA", ImpactoStock.SinImpacto)]
        [InlineData("Entrada por devolución", null, ImpactoStock.Entrada)]
        [InlineData("Envío especial", null, ImpactoStock.Salida)]
        [InlineData("nombre histórico desconocido", "obra", ImpactoStock.Salida)]
        [InlineData(null, null, ImpactoStock.SinImpacto)]
        public void ClasificaConCompatibilidadHistorica(string tipo, string destino, ImpactoStock esperado)
        {
            Assert.Equal(esperado, ClasificadorMovimiento.Clasificar(tipo, destino));
        }

        [Fact]
        public void RegistroInternoAplicadoNoVuelveAAcreditar()
        {
            Assert.Equal(ImpactoStock.SinImpacto, ClasificadorMovimiento.Clasificar("REGISTRO INTERNO", "OBRA", true));
            Assert.Equal(ImpactoStock.Entrada, ClasificadorMovimiento.Clasificar("REGISTRO INTERNO", "OBRA"));
        }
    }
}
