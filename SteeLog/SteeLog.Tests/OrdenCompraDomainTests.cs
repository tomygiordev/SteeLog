using System;
using System.Collections.Generic;
using SteeLog.Domain.Inventario;
using SteeLog.Domain.OrdenesCompra;
using SteeLog.Infrastructure.Persistence;
using Xunit;

namespace SteeLog.Tests
{
    public class OrdenCompraDomainTests
    {
        [Fact]
        public void FechaNegocioDistingueArgentinaDeUtc()
        {
            var instante = new DateTimeOffset(2026, 11, 3, 2, 30, 0, TimeSpan.Zero);
            var argentina = new BusinessClock(TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"), () => instante);
            var utc = new BusinessClock(TimeZoneInfo.Utc, () => instante);
            Assert.Equal(new DateTime(2026, 11, 2), argentina.FechaNegocio);
            Assert.Equal(new DateTime(2026, 11, 3), utc.FechaNegocio);
        }
        [Fact]
        public void CabeceraNormalizaYRechazaValoresInvalidos()
        {
            var cabecera = new Cabecera(new DateTime(2026, 1, 2, 15, 30, 0), " OC-1 ", " R ", 3, 2m, " Nota ");
            Assert.Equal("OC-1", cabecera.Numero); Assert.Equal("R", cabecera.Remito); Assert.Equal(new DateTime(2026, 1, 2), cabecera.Fecha);
            Assert.Throws<ArgumentException>(() => new Cabecera(DateTime.MinValue, new string('a', 81), null, 1, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cabecera(DateTime.MinValue, "X", null, 1, -1, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cabecera(DateTime.MinValue, "X", null, 0, 0, null));
            Assert.Equal(0, Cabecera.Restaurar(DateTime.MinValue, "X", null, 0, 0, null).ProveedorId);
        }
        [Fact]
        public void RecepcionAcumulativaCalculaDeltasYEstado()
        {
            var orden = new OrdenCompra(1, new Cabecera(new DateTime(2026, 1, 1), "OC", null, 1, 0, null), EstadoOrden.Pendiente, new[] { new LineaOrden(1, 10, 5, 0) });
            Assert.Equal(2, orden.Recibir(new Dictionary<int, decimal> { [1] = 2 })[1]);
            Assert.Equal(EstadoOrden.RecibidaParcial, orden.Estado);
            Assert.Equal(0, orden.Recibir(new Dictionary<int, decimal> { [1] = 2 })[1]);
            Assert.Equal(3, orden.Recibir(new Dictionary<int, decimal> { [1] = 5 })[1]);
            Assert.Equal(EstadoOrden.RecibidaCompleta, orden.Estado);
            Assert.Throws<InvalidOperationException>(() => orden.Recibir(new Dictionary<int, decimal> { [1] = 5 }));
        }

        [Fact]
        public void RecepcionInvalidaNoMutaNingunaLinea()
        {
            var a = new LineaOrden(1, 10, 5, 1);
            var b = new LineaOrden(2, 20, 4, 0);
            var orden = new OrdenCompra(1, new Cabecera(new DateTime(2026, 1, 1), "OC", null, 1, 0, null), EstadoOrden.RecibidaParcial, new[] { a, b });
            Assert.Throws<InvalidOperationException>(() => orden.Recibir(new Dictionary<int, decimal> { [1] = 4, [2] = 5 }));
            Assert.Equal(1, a.Recibida);
            Assert.Equal(0, b.Recibida);
            Assert.Throws<InvalidOperationException>(() => orden.Recibir(new Dictionary<int, decimal> { [1] = 0, [2] = 0 }));
        }

        [Fact]
        public void RechazaDuplicadosYFracciones()
        {
            Assert.Throws<ArgumentException>(() => new OrdenCompra(1, new Cabecera(new DateTime(2026, 1, 1), "OC", null, 1, 0, null), EstadoOrden.Pendiente, new[] { new LineaOrden(1, 10, 1, 0), new LineaOrden(2, 10, 1, 0) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LineaOrden(1, 10, 1.5m, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LineaOrden(1, 10, (decimal)int.MaxValue + 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LineaOrden(1, 10, 0, 0));
        }

        [Fact]
        public void MaterialYSaldosValidanRestricciones()
        {
            var orden = new OrdenCompra(1, new Cabecera(new DateTime(2026, 1, 1), "OC", null, 1, 0, null), EstadoOrden.RecibidaParcial, new[] { new LineaOrden(1, 10, 5, 2), new LineaOrden(2, 20, 1, 0) });
            Assert.Throws<InvalidOperationException>(() => orden.ExigirPendiente());
            Assert.Throws<InvalidOperationException>(() => orden.CambiarMaterial(1, 20));
            Assert.Equal(2, orden.CambiarMaterial(1, 30));
            Assert.Throws<InvalidOperationException>(() => SaldoInventario.Aplicar(1, -2));
            Assert.Equal(3, SaldoInventario.Aplicar(1, 2));
        }

        [Fact]
        public void EstadosOrdenTodosEsInmutableYContieneLosTresEstadosCanonicos()
        {
            Assert.Equal(3, EstadosOrden.Todos.Count);
            Assert.Equal(new[] { EstadosOrden.Pendiente, EstadosOrden.RecibidaCompleta, EstadosOrden.RecibidaParcial }, EstadosOrden.Todos);

            var listaMutable = Assert.IsAssignableFrom<IList<string>>(EstadosOrden.Todos);
            Assert.Throws<NotSupportedException>(() => listaMutable[0] = "Otro");
        }
    }
}
