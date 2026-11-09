using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Inventario;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Movimientos;
using SteeLog.Infrastructure.Persistence;
using SteeLog.Infrastructure.Reportes;
using Xunit;

[Trait("Category", "Integration")]
public sealed class EsquemaLecturaIntegrationTests
{
    private static PostgresConnectionFactory Factory()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_ESQUEMA_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_ESQUEMA_TEST_DB");
        if (!Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$") || string.IsNullOrEmpty(cs))
            throw new InvalidOperationException("Usar esquema-copia-real.ps1 con copia descartable.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435)
            throw new InvalidOperationException("Destino no permitido.");
        return new PostgresConnectionFactory(cs);
    }

    [Fact]
    public async Task LeeCatalogosYMaterialesMedianteInfrastructure()
    {
        var factory = Factory();
        var catalogos = await new NpgsqlCatalogosMovimientoConsulta(factory).ObtenerAsync();
        Assert.NotEmpty(catalogos.Tipos);
        Assert.NotEmpty(await new NpgsqlMaterialesConsulta(factory).ListarAsync(new FiltroMaterialLectura()));
    }

    [Fact]
    public async Task LeeTiposEHistorialConOSinImpacto()
    {
        var factory = Factory();
        var catalogos = await new NpgsqlCatalogosMovimientoConsulta(factory).ObtenerAsync();
        using (var uow = new NpgsqlUnitOfWork(factory))
        {
            await uow.BeginAsync();
            var repo = new NpgsqlMovimientos(uow);
            foreach (var opcion in catalogos.Tipos)
            {
                var tipo = await repo.TipoAsync(opcion.Id);
                Assert.Equal(opcion.Nombre, tipo.Nombre);
                foreach (var destino in new[] { "OBRA", "UBICACION", null })
                    Assert.Equal(ClasificadorMovimiento.Clasificar(tipo.Nombre, destino),
                        ClasificadorMovimiento.Clasificar(tipo.Nombre, destino, impactoExplicito: tipo.Impacto));
            }
            await uow.RollbackAsync();
        }
        Assert.NotEmpty(await new NpgsqlHistorialMovimientoConsulta(factory).ListarAsync(null, null, null));
    }

    [Fact]
    public async Task LeeKardexRepetidamenteSinCambiarResultados()
    {
        var consulta = new NpgsqlKardexConsulta(Factory());
        var primera = await consulta.HistorialAsync(null, null, null);
        var segunda = await consulta.HistorialAsync(null, null, null);
        Assert.NotEmpty(primera);
        Assert.Equal(primera.Select(x => new { x.id_movimiento, x.id_material, x.cantidad, x.delta, x.saldo }),
            segunda.Select(x => new { x.id_movimiento, x.id_material, x.cantidad, x.delta, x.saldo }));
    }
}
