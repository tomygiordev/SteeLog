using System;
using System.Collections.Generic;
using SteeLog.Domain.Conteos;
using Xunit;

namespace SteeLog.Tests
{
    public class ConteoDomainTests
    {
        [Fact]
        public void CargaAcumulaYCorreccionReemplazaSinPerderPrecision()
        {
            var c = new Conteo(1, "operador@example.com", EstadoConteo.Abierto, new[] { new DetalleConteo(3, 10, 1.25m) });
            Assert.Equal(1.55m, c.Acumular(10, .30m));
            Assert.Equal(1.85m, c.Acumular(10, .30m));
            c.Corregir(10, 7.11m);
            Assert.Equal(7.11m, c.Detalles[0].Cantidad);
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Acumular(10, .001m));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Corregir(10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CantidadConteo.Validar(CantidadConteo.Maximo + .01m));
        }
        [Fact]
        public void AltaNuevaPuedeFinalizarEnElMismoAgregado()
        {
            var c = new Conteo(1, "a", EstadoConteo.Abierto, Array.Empty<DetalleConteo>());
            c.Acumular(7, 2.25m); c.Acumular(7, .25m);
            Assert.Equal(0, c.Detalles[0].Id);
            Assert.Equal(.50m, c.Finalizar(new Dictionary<int, decimal> { [7] = 2m })[7]);
        }
        [Fact]
        public void FinalizaConDiferenciasExactasYBloqueaMutaciones()
        {
            var c = new Conteo(1, "a", EstadoConteo.Abierto, new[] { new DetalleConteo(3, 10, 1.25m) });
            Assert.Equal(-.05m, c.Finalizar(new Dictionary<int, decimal> { [10] = 1.30m })[10]);
            Assert.Equal(EstadoConteo.Finalizado, c.Estado);
            Assert.Throws<InvalidOperationException>(() => c.Acumular(10, 1));
            Assert.Throws<InvalidOperationException>(() => c.Corregir(10, 1));
            Assert.Throws<InvalidOperationException>(() => c.Quitar(3));
            Assert.Throws<InvalidOperationException>(() => c.Cancelar());
        }
        [Fact]
        public void CierreFallidoNoCambiaEstadoYCancelacionRechazaCambios()
        {
            var vacio = new Conteo(1, "a", EstadoConteo.Abierto, Array.Empty<DetalleConteo>());
            Assert.Throws<InvalidOperationException>(() => vacio.Finalizar(new Dictionary<int, decimal>()));
            Assert.Equal(EstadoConteo.Abierto, vacio.Estado);
            var c = new Conteo(1, "a", EstadoConteo.Abierto, new[] { new DetalleConteo(1, 1, CantidadConteo.Maximo) });
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Finalizar(new Dictionary<int, decimal> { [1] = -1 }));
            Assert.Equal(EstadoConteo.Abierto, c.Estado);
            c.Cancelar(); Assert.Throws<InvalidOperationException>(() => c.Quitar(1)); c.ExigirEliminable();
        }
    }
}
