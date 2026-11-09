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

        [Theory]
        [InlineData("registro_interno", "Compra eventual", "OBRA", false, ImpactoStock.Entrada)]
        [InlineData("registro_interno", "Compra", null, true, ImpactoStock.SinImpacto)]
        [InlineData("entrada", "Venta", "OBRA", false, ImpactoStock.Entrada)]
        [InlineData("salida", "Compra", null, false, ImpactoStock.Salida)]
        [InlineData("sin_impacto", "Compra", "OBRA", false, ImpactoStock.SinImpacto)]
        [InlineData("segun_destino", "Compra", "obra", false, ImpactoStock.Salida)]
        [InlineData("segun_destino", "Venta", "UBICACION", false, ImpactoStock.SinImpacto)]
        [InlineData("segun_destino", null, null, false, ImpactoStock.SinImpacto)]
        [InlineData(null, "Compra", "OBRA", false, ImpactoStock.Entrada)]
        [InlineData(null, "Desconocido", "obra", false, ImpactoStock.Salida)]
        [InlineData(null, "REGISTRO INTERNO", "OBRA", true, ImpactoStock.SinImpacto)]
        public void ImpactoExplicitoPrevaleceYNullConservaFallback(string impacto, string nombre, string destino, bool aplicado, ImpactoStock esperado)
        {
            Assert.Equal(esperado, ClasificadorMovimiento.Clasificar(nombre, destino, aplicado, impacto));
        }

        [Theory]
        [InlineData("REGISTRO INTERNO COMPRA EVENTUAL VENTA", ImpactoStock.Entrada)]
        [InlineData("COMPRA EVENTUAL ENTRADA", ImpactoStock.SinImpacto)]
        [InlineData("APORTE DE CLIENTE COMPRA", ImpactoStock.SinImpacto)]
        [InlineData("APORTE DEL CLIENTE VENTA", ImpactoStock.SinImpacto)]
        [InlineData("ENTRADA SALIDA", ImpactoStock.Entrada)]
        [InlineData("INGRESO", ImpactoStock.Entrada)]
        [InlineData("CARGA INICIAL", ImpactoStock.Entrada)]
        [InlineData("DEVOLUCION", ImpactoStock.Entrada)]
        [InlineData("DEVOLUCIÓN", ImpactoStock.Entrada)]
        [InlineData("AJUSTE POSITIVO", ImpactoStock.Entrada)]
        [InlineData("EGRESO", ImpactoStock.Salida)]
        [InlineData("CONSUMO", ImpactoStock.Salida)]
        [InlineData("VENTA", ImpactoStock.Salida)]
        [InlineData("ENVIO", ImpactoStock.Salida)]
        [InlineData("USO INTERNO", ImpactoStock.Salida)]
        [InlineData("AJUSTE NEGATIVO", ImpactoStock.Salida)]
        public void ConservaOrdenYVocabularioHistorico(string nombre, ImpactoStock esperado)
        {
            Assert.Equal(esperado, ClasificadorMovimiento.Clasificar(nombre));
        }

        [Fact]
        public void ImpactoDesconocidoNoSeIgnora()
        {
            Assert.Throws<System.ArgumentException>(() => ClasificadorMovimiento.Clasificar("Compra", impactoExplicito: "invalido"));
        }

        [Fact]
        public void RegistroInternoExplicitoNoEsEditableAunqueCambieNombreEId()
        {
            Assert.Throws<System.InvalidOperationException>(() => SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(99, "Renombrado", "registro_interno"));
            Assert.Throws<System.InvalidOperationException>(() => SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(SteeLog.Domain.Movimientos.Movimiento.TipoRegistroInternoId, "Renombrado"));
        }

        [Fact]
        public void RegistroInternoAplicadoNoVuelveAAcreditar()
        {
            Assert.Equal(ImpactoStock.SinImpacto, ClasificadorMovimiento.Clasificar("REGISTRO INTERNO", "OBRA", true));
            Assert.Equal(ImpactoStock.Entrada, ClasificadorMovimiento.Clasificar("REGISTRO INTERNO", "OBRA"));
        }
    }
}
