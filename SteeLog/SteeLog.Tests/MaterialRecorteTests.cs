using System;
using SteeLog.Domain.Inventario;
using Xunit;

namespace SteeLog.Tests
{
    public class MaterialRecorteTests
    {
        [Fact]
        public void DatosNoAjustanStockYAjusteExigeSnapshotVigente()
        {
            var material = new Material(1, "Perfil", 2, 10m, 1m);
            material.CambiarDatos("Perfil nuevo", 3, 2m);
            Assert.Equal(10m, material.StockActual);
            Assert.Throws<InvalidOperationException>(() => material.AjustarStock(11m, 9m));
            Assert.Equal(10m, material.StockActual);
            Assert.Equal(1m, material.AjustarStock(11m, 10m));
        }

        [Fact]
        public void StockNoAceptaPrecisionQueLaBaseRedondearia()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Material(1, "Perfil", 2, 1.005m, 0));
            var material = new Material(1, "Perfil", 2, 1m, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => material.AjustarStock(1.001m, 1m));
            Assert.Equal(1m, material.StockActual);
        }

        [Fact]
        public void RecorteIdentificaEstadoYDimensionesYNoAdmiteSaldoNegativo()
        {
            var recorte = new Recorte(1, 2, 1m, 2m, 0, 3m, null);
            Assert.True(recorte.CoincideCon(new Recorte(0, 2, 1m, 2m, 0, 1m, null)));
            Assert.False(recorte.CoincideCon(new Recorte(0, 2, 1m, 2m, 0, 1m, "Regular")));
            Assert.Throws<InvalidOperationException>(() => recorte.Aplicar(-4m));
            Assert.Equal(3m, recorte.Cantidad);
            recorte.Aplicar(-3m);
            Assert.Equal(0m, recorte.Cantidad);
            Assert.Throws<ArgumentException>(() => new Recorte(0, 2, 1, 0, 2, 1, "Regular"));
            Assert.Throws<ArgumentException>(() => new Recorte(0, 2, 0, 0, 1, 1, "Roto"));
        }

        [Fact]
        public void RecorteValidaEscalaYLimiteSuperiorEnDimensiones()
        {
            var exAnchoEscala = Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 1.234m, 2m, 0m, 1m, "Buen estado"));
            Assert.Equal("ancho", exAnchoEscala.ParamName);

            var exAlturaEscala = Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 1m, 2.345m, 0m, 1m, "Buen estado"));
            Assert.Equal("altura", exAlturaEscala.ParamName);

            var exAnchoLimite = Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 100000000m, 2m, 0m, 1m, "Buen estado"));
            Assert.Equal("ancho", exAnchoLimite.ParamName);

            var exAlturaLimite = Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 1m, 100000000m, 0m, 1m, "Buen estado"));
            Assert.Equal("altura", exAlturaLimite.ParamName);

            var exMedidaEscala = Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 0m, 0m, 1.234m, 1m, "Buen estado"));
            Assert.Equal("medida", exMedidaEscala.ParamName);

            var exMedidaLimite = Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 0m, 0m, 100000000m, 1m, "Buen estado"));
            Assert.Equal("medida", exMedidaLimite.ParamName);

            var recorteValido2D = new Recorte(0, 1, 99999999.99m, 0.01m, 0m, 1m, "Buen estado");
            Assert.Equal(99999999.99m, recorteValido2D.Ancho);
            Assert.Equal(0.01m, recorteValido2D.Altura);

            var recorteValido1D = new Recorte(0, 1, 0m, 0m, 99999999.99m, 1m, "Buen estado");
            Assert.Equal(99999999.99m, recorteValido1D.Medida);
        }
    }
}
