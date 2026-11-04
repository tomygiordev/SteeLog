using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Reportes;
using SteeLog.Domain.Inventario;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class KardexTests
    {
        private sealed class Consulta : IKardexConsulta
        {
            public int Calls; public int? Material; public DateTime? Desde, Hasta;
            public List<KardexItem> Retorno = new List<KardexItem>();
            public Task<List<KardexMaterial>> MaterialesAsync(CancellationToken token = default) { Calls++; return Task.FromResult(new List<KardexMaterial>()); }
            public Task<List<KardexItem>> HistorialAsync(int? material, DateTime? desde, DateTime? hasta, CancellationToken token = default) { Calls++; Material=material; Desde=desde; Hasta=hasta; return Task.FromResult(Retorno); }
        }
        [Fact] public async Task SinSesionNoConsulta()
        {
            var query = new Consulta(); var caso = new ConsultarKardex(new Session(), query);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await caso.MaterialesAsync(CancellationToken.None));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await caso.HistorialAsync(null,null,null,CancellationToken.None));
            Assert.Equal(0, query.Calls);
        }
        [Theory] [InlineData(Role.Galpon)] [InlineData(Role.Admin)]
        public async Task ConservaFiltrosParaRolesHabilitados(Role rol)
        {
            var sesion = new Session(); sesion.Start(new Identity("test@tesis.local", rol)); var query = new Consulta();
            var desde = new DateTime(2024,1,1); var hasta = new DateTime(2024,1,31).AddDays(1).AddTicks(-1);
            await new ConsultarKardex(sesion, query).HistorialAsync(42, desde, hasta, CancellationToken.None);
            Assert.Equal(42, query.Material); Assert.Equal(desde, query.Desde); Assert.Equal(hasta, query.Hasta); Assert.Equal(1, query.Calls);
        }
        [Fact]
        public async Task HistorialAsync_TransportaCamposTipadosSinMutacion()
        {
            var sesion = new Session(); sesion.Start(new Identity("test@tesis.local", Role.Admin));
            var query = new Consulta();
            query.Retorno = new List<KardexItem>
            {
                new KardexItem
                {
                    id_movimiento = 10,
                    fecha = new DateTime(2026, 10, 15),
                    tipo_movimiento = "ENVÍO",
                    cantidad = 2m,
                    saldo = 8m,
                    tipo_destino = "OBRA",
                    impacto = ImpactoStock.Salida,
                    delta = -2m
                }
            };
            var resultado = await new ConsultarKardex(sesion, query).HistorialAsync(null, null, null, CancellationToken.None);
            var item = Assert.Single(resultado);
            Assert.Equal(2m, item.cantidad);
            Assert.Equal(8m, item.saldo);
            Assert.Equal("OBRA", item.tipo_destino);
            Assert.Equal(ImpactoStock.Salida, item.impacto);
            Assert.Equal(-2m, item.delta);
        }
    }
}
