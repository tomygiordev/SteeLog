using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using SteeLog.Core.Application.Reportes;
using SteeLog.Domain.Inventario;
using SteeLog.Domain.Reportes;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class KardexResumenTests
    {
        [Fact]
        public void EntradaYSalidaCanonicas_CalculanTotalesCorrectosSegunImpacto()
        {
            // ENTRADA canónica (+10) y SALIDA canónica (+4) y ENVÍO canónico (+2)
            var items = new List<KardexItem>
            {
                new KardexItem { tipo_movimiento = "ENVÍO", cantidad = 2m, saldo = 14m },
                new KardexItem { tipo_movimiento = "SALIDA", cantidad = 4m, saldo = 16m },
                new KardexItem { tipo_movimiento = "ENTRADA", cantidad = 10m, saldo = 20m },
            };

            var resumen = ResumenKardexApp.Calcular(items);

            Assert.Equal(10m, resumen.TotalEntradas);
            Assert.Equal(-6m, resumen.TotalSalidas); // -4m de SALIDA y -2m de ENVÍO
            Assert.Equal(14m, resumen.StockFinal);   // primer saldo (más reciente)
        }

        [Fact]
        public void EnvioPositivo_LegacyContract_ProduceEntradasCeroYSalidasNegativas()
        {
            // Caso exacto del bug reportado: ENVÍO con cantidad=+2 positiva y saldo=8
            var items = new List<KardexItem>
            {
                new KardexItem { tipo_movimiento = "ENVÍO", cantidad = 2m, saldo = 8m, tipo_destino = "OBRA", impacto = ImpactoStock.Salida, delta = -2m }
            };

            var resumen = ResumenKardexApp.Calcular(items);

            Assert.Equal(0m, resumen.TotalEntradas);
            Assert.Equal(-2m, resumen.TotalSalidas);
            Assert.Equal(8m, resumen.StockFinal);
        }

        [Theory]
        [InlineData("OBRA", true)]
        [InlineData("PROVEEDOR", false)]
        [InlineData("UBICACION", false)]
        [InlineData(null, false)]
        public void NoCanonico_ClasificaSegunTipoDestinoOriginalNoPorNombre(string tipoDestino, bool esSalida)
        {
            // Reutiliza ClasificadorMovimiento establecido con tipo y TIPO_DESTINO original ('OBRA' etc.),
            // NO clasificar con nombre traducido destino ('Obra demo').
            var lineas = new List<LineaKardex>
            {
                new LineaKardex
                {
                    TipoMovimiento = "TRASLADO ESPECIAL",
                    TipoDestino = tipoDestino,
                    Cantidad = 5m,
                    Saldo = 25m
                }
            };

            var resumen = TotalesKardex.Calcular(lineas);

            if (esSalida)
            {
                Assert.Equal(0m, resumen.TotalEntradas);
                Assert.Equal(-5m, resumen.TotalSalidas);
            }
            else
            {
                Assert.Equal(0m, resumen.TotalEntradas);
                Assert.Equal(0m, resumen.TotalSalidas);
            }
        }

        [Fact]
        public void NoCanonico_NombreTraducidoDestinoNoFuerzaSalidaSiTipoDestinoNoEsObra()
        {
            // Si el nombre traducido en 'destino' es "Obra Central" pero tipo_destino es null o UBICACION,
            // el clasificador no debe interpretar texto arbitrario como OBRA.
            var item = new KardexItem
            {
                tipo_movimiento = "MOVIMIENTO GENERAL",
                destino = "Obra Central",
                tipo_destino = "UBICACION",
                cantidad = 7m,
                saldo = 30m
            };

            var resumen = ResumenKardexApp.Calcular(new[] { item });

            Assert.Equal(0m, resumen.TotalEntradas);
            Assert.Equal(0m, resumen.TotalSalidas);
        }

        [Theory]
        [InlineData("COMPRA EVENTUAL")]
        [InlineData("APORTE DEL CLIENTE")]
        [InlineData("APORTE DE CLIENTE")]
        public void SinImpacto_NoAfectaEntradasNiSalidasDeStock(string tipoSinImpacto)
        {
            // Movimiento SinImpacto no debe contarse como entrada/salida de stock
            var items = new List<KardexItem>
            {
                new KardexItem { tipo_movimiento = tipoSinImpacto, cantidad = 15m, saldo = 50m }
            };

            var resumen = ResumenKardexApp.Calcular(items);

            Assert.Equal(0m, resumen.TotalEntradas);
            Assert.Equal(0m, resumen.TotalSalidas);
            Assert.Equal(50m, resumen.StockFinal);
        }

        [Fact]
        public void RegistroInterno_RespetaClasificadorSegunAplicacionPrevia()
        {
            // Registro interno sin aplicar clasifica a Entrada; si ya fue aplicado, es SinImpacto
            var sinAplicar = ClasificadorMovimiento.Clasificar("REGISTRO INTERNO", registroInternoYaAplicado: false);
            var yaAplicado = ClasificadorMovimiento.Clasificar("REGISTRO INTERNO", registroInternoYaAplicado: true);

            Assert.Equal(ImpactoStock.Entrada, sinAplicar);
            Assert.Equal(ImpactoStock.SinImpacto, yaAplicado);

            var lineas = new List<LineaKardex>
            {
                new LineaKardex { TipoMovimiento = "REGISTRO INTERNO", Impacto = yaAplicado, Cantidad = 12m, Saldo = 100m }
            };

            var resumen = TotalesKardex.Calcular(lineas);
            Assert.Equal(0m, resumen.TotalEntradas);
            Assert.Equal(0m, resumen.TotalSalidas);
        }

        [Fact]
        public void CantidadesFraccionarias_CalculanTotalesConPrecisionExacta()
        {
            var items = new List<KardexItem>
            {
                new KardexItem { tipo_movimiento = "ENVÍO", cantidad = 1.375m, saldo = 8.625m },
                new KardexItem { tipo_movimiento = "ENTRADA", cantidad = 2.125m, saldo = 10m }
            };

            var resumen = ResumenKardexApp.Calcular(items);

            Assert.Equal(2.125m, resumen.TotalEntradas);
            Assert.Equal(-1.375m, resumen.TotalSalidas);
            Assert.Equal(8.625m, resumen.StockFinal);
        }

        [Fact]
        public void CantidadHistoricaNegativa_CaracterizaEfectoSinNormalizarSilenciosamenteConAbs()
        {
            // Regla: Para cantidades históricas negativas no normalizar silenciosamente con Abs:
            // caracterizar el efecto usando la misma dirección*{cantidad original} que ya usa acumulación de respaldo.
            // Dirección Entrada: +1 * (-3m) = -3m
            // Dirección Salida:  -1 * (-2m) = +2m
            var items = new List<KardexItem>
            {
                new KardexItem { tipo_movimiento = "ENTRADA", cantidad = -3m, saldo = 10m },
                new KardexItem { tipo_movimiento = "SALIDA", cantidad = -2m, saldo = 13m }
            };

            var resumen = ResumenKardexApp.Calcular(items);

            // TotalEntradas debe ser -3m (NO Math.Abs(-3) = +3m)
            Assert.Equal(-3m, resumen.TotalEntradas);
            // TotalSalidas debe ser +2m: (-1) * (-2m) = +2m (NO Math.Abs(-2m) = -2m ni 2m positivo sin dirección)
            Assert.Equal(2m, resumen.TotalSalidas);
        }

        [Fact]
        public void SaldoPersistidoNoSeAltera_YNoSeMutanCantidadesOriginales()
        {
            var item = new KardexItem
            {
                tipo_movimiento = "ENVÍO",
                cantidad = 2.5m,
                saldo = 8.25m,
                tipo_destino = "OBRA",
                impacto = ImpactoStock.Salida,
                delta = -2.5m
            };

            var snapshot = new KardexSnapshot
            {
                Historial = new List<KardexItem> { item },
                MaterialNombre = "Perfil C",
                FechaInicio = new DateTime(2026, 1, 1),
                FechaFin = new DateTime(2026, 1, 31),
                MostrarColumnaMaterial = true,
                MostrarColumnaSaldo = true
            };

            var resumen = snapshot.Resumen;

            // Saldo persistido intacto
            Assert.Equal(8.25m, item.saldo);
            Assert.Equal(8.25m, resumen.StockFinal);
            // Cantidad original intacta
            Assert.Equal(2.5m, item.cantidad);
            // Salidas correctas
            Assert.Equal(-2.5m, resumen.TotalSalidas);
            Assert.Equal(0m, resumen.TotalEntradas);
        }

        [Fact]
        public void SnapshotNoConservaUnResumenObsoletoSiSeReemplazaElHistorial()
        {
            var snapshot = new KardexSnapshot { Historial = new List<KardexItem> {
                new KardexItem { tipo_movimiento = "ENVIO", cantidad = 2m, saldo = 8m }
            } };
            Assert.Equal(-2m, snapshot.Resumen.TotalSalidas);
            snapshot.Historial = new List<KardexItem> {
                new KardexItem { tipo_movimiento = "ENTRADA", cantidad = 5m, saldo = 13m }
            };
            Assert.Equal(5m, snapshot.Resumen.TotalEntradas);
            Assert.Equal(0m, snapshot.Resumen.TotalSalidas);
            Assert.Equal(13m, snapshot.Resumen.StockFinal);
        }

        [Theory]
        [InlineData(ImpactoStock.Entrada, 5, 5)]
        [InlineData(ImpactoStock.Entrada, -3, -3)]
        [InlineData(ImpactoStock.Salida, 4, -4)]
        [InlineData(ImpactoStock.Salida, -2, 2)]
        [InlineData(ImpactoStock.SinImpacto, 10, 0)]
        public void Delta_CalculaDireccionPorCantidadOriginal(ImpactoStock impacto, decimal cantidad, decimal deltaEsperado)
        {
            var delta = TotalesKardex.Delta(impacto, cantidad);
            Assert.Equal(deltaEsperado, delta);
        }

        [Fact]
        public void KardexItem_NuevasPropiedadesTienenBrowsableFalseParaNoAlterarUI()
        {
            // Comprobar que tipo_destino, impacto y delta tienen [Browsable(false)]
            // para que DataGridView no genere columnas automáticas en WinForms
            var props = new[] { "tipo_destino", "impacto", "delta" };
            foreach (var propName in props)
            {
                var prop = typeof(KardexItem).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(prop);
                var attr = prop.GetCustomAttribute<BrowsableAttribute>();
                Assert.NotNull(attr);
                Assert.False(attr.Browsable);
            }
        }
    }
}
