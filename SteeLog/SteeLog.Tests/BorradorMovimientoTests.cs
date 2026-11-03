using System;
using System.Text;
using SteeLog.Core.Application.Borradores;
using Xunit;

namespace SteeLog.Tests
{
    public class BorradorMovimientoTests
    {
        private sealed class Memoria : IDraftStore
        {
            public byte[] Bytes;
            public byte[] Leer() => Bytes;
            public void Guardar(byte[] contenido) => Bytes = contenido;
            public void Eliminar() => Bytes = null;
        }
        [Fact]
        public void RoundtripConservaInsumosYFleteYExpira()
        {
            var ahora = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
            var store = new Memoria();
            var servicio = new BorradorMovimiento(store, () => ahora);
            var draft = new MovimientoDraft { IdFlete = 77, NroRemito = "123" };
            draft.Materiales.Add(new MovimientoMaterialDraft { EsInsumo = true, IdInsumo = 42 });
            servicio.Guardar(draft);
            Assert.Contains("\"IdFlete\":77", Encoding.UTF8.GetString(store.Bytes));
            var leido = servicio.Leer();
            Assert.Equal(77, leido.IdFlete);
            Assert.Equal(42, Assert.Single(leido.Materiales).IdInsumo);
            servicio = new BorradorMovimiento(store, () => ahora.AddHours(25));
            Assert.Null(servicio.Leer());
            Assert.Null(store.Bytes);
        }
        [Fact]
        public void LeeFixtureGeneradaPorLosDtosDeF3()
        {
            // Serializada con los DTO originales de 8010e36, sin ejecutar el servicio de archivos.
            const string json = @"{""CostoFlete"":1250.50,""Fecha"":""\/Date(1767225600000)\/"",""FechaGuardado"":""\/Date(1767355200000)\/"",""IdDestino"":9,""IdFlete"":77,""IdObra"":9,""IdOrigen"":2,""IdTipoMovimiento"":7,""Materiales"":[{""Cantidad"":1.25,""EsCompraEventual"":false,""EsInsumo"":false,""EsRecorte"":false,""IdInsumo"":null,""IdMaterial"":3,""IdRecorte"":null,""MedidaRecorte"":null,""NombreMaterial"":""Perfil"",""NombreMaterialEventual"":null,""PrecioUnitario"":18.20},{""Cantidad"":1,""EsCompraEventual"":false,""EsInsumo"":true,""EsRecorte"":false,""IdInsumo"":42,""IdMaterial"":null,""IdRecorte"":null,""MedidaRecorte"":null,""NombreMaterial"":""Taladro"",""NombreMaterialEventual"":null,""PrecioUnitario"":0}],""NombreTipoMovimiento"":""Salida"",""NroRemito"":""F3-123"",""Observaciones"":""Fixture anterior a F4"",""PrecioTotal"":22.75,""TipoDestino"":""OBRA"",""TipoOrigen"":""UBICACION""}";
            var store = new Memoria { Bytes = Encoding.UTF8.GetBytes(json) };
            var draft = new BorradorMovimiento(store, () => new DateTime(2026, 1, 2, 13, 0, 0, DateTimeKind.Utc)).Leer();
            Assert.NotNull(draft);
            Assert.Equal("F3-123", draft.NroRemito);
            Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), draft.Fecha);
            Assert.Equal(77, draft.IdFlete);
            Assert.Equal(1250.50m, draft.CostoFlete);
            Assert.Equal(22.75m, draft.PrecioTotal);
            Assert.Equal("UBICACION", draft.TipoOrigen);
            Assert.Equal("OBRA", draft.TipoDestino);
            Assert.Equal(2, draft.Materiales.Count);
            Assert.Equal(3, draft.Materiales[0].IdMaterial);
            Assert.Equal(1.25m, draft.Materiales[0].Cantidad);
            Assert.Equal(18.20m, draft.Materiales[0].PrecioUnitario);
            Assert.True(draft.Materiales[1].EsInsumo);
            Assert.Equal(42, draft.Materiales[1].IdInsumo);
        }
        [Fact]
        public void JsonCorruptoSeDescarta()
        {
            var store = new Memoria { Bytes = Encoding.UTF8.GetBytes("{") };
            Assert.Null(new BorradorMovimiento(store, () => DateTime.Now).Leer());
            Assert.Null(store.Bytes);
        }
    }
}
