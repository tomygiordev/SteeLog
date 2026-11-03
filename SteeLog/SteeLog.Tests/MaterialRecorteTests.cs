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
    }
}
