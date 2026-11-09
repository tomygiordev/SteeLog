using System;
using System.IO;
using System.Threading;
using System.Reflection;
using Path = System.IO.Path;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Pdf;
using Xunit;

namespace SteeLog.Tests
{
    public class RemitoPdfTests
    {
        private sealed class Consulta : IRemitoConsulta
        {
            public int Llamadas;
            public Task<RemitoLectura> ObtenerAsync(int idMovimiento, CancellationToken token = default)
            { Llamadas++; return Task.FromResult<RemitoLectura>(null); }
        }
        private sealed class RecursosConLogoInvalido : IRecursosPdf
        {
            public MarcaPdf Capturar()
            {
                return new MarcaPdf { LogoPng = new byte[] { 1, 2, 3 }, FechaLocal = new DateTime(2026, 1, 2), Pie = "SteeLog - Test" };
            }
        }
        private sealed class Recursos : IRecursosPdf
        {
            public MarcaPdf Capturar()
            {
                using (var input = Assembly.LoadFrom(Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY") ?? typeof(SteeLog.BusquedaGlobal).Assembly.Location).GetManifestResourceStream("SteeLog.Resources.Branding.steelog-logo-dark.png"))
                using (var buffer = new MemoryStream())
                {
                    if (input == null) throw new InvalidOperationException("Falta el logo embebido.");
                    input.CopyTo(buffer);
                    return new MarcaPdf { LogoPng = buffer.ToArray(), FechaLocal = new DateTime(2026, 1, 2), Pie = "SteeLog - Sistema de Gestión de Inventario" };
                }
            }
        }
        [Fact]
        public async Task PermisoSeCompruebaAntesDeConsultar()
        {
            var session = new Session(); var consulta = new Consulta(); var caso = new ConsultarRemito(session, consulta);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Task.Run(async () => await caso.ObtenerAsync(1)));
            Assert.Equal(0, consulta.Llamadas);
            session.Start(new Identity("galpon@test", Role.Galpon));
            await caso.ObtenerAsync(1);
            Assert.Equal(1, consulta.Llamadas);
        }
        [Fact]
        public void GeneraTresCopiasConInsumosYNombrePuro()
        {
            var dto = new RemitoLectura { Fecha = new DateTime(2026, 1, 1), FechaArchivo = new DateTime(2026, 1, 2), NroRemito = "33", TipoMovimiento = "Salida", Origen = "Galpon", Destino = "Obra", Observaciones = "Prueba", CostoFlete = 120 };
            dto.Lineas.Add(new RemitoLinea { Material = "Taladro", Cantidad = 1, EsInsumo = true });
            var pdf = new MovimientoPDF(new Recursos());
            Assert.Equal("Comprobante_Salida_Remito_33_20260102.pdf", pdf.SugerirNombreArchivo(dto));
            dto.NroRemito = "../33\\otro";
            Assert.DoesNotContain("/", pdf.SugerirNombreArchivo(dto));
            Assert.DoesNotContain("\\", pdf.SugerirNombreArchivo(dto));
            dto.NroRemito = "33";
            var archivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try
            {
                pdf.Generar(archivo, dto);
                using (var reader = new PdfReader(archivo))
                {
                    Assert.Equal(3, reader.NumberOfPages);
                    for (int i = 1; i <= 3; i++)
                    {
                        var texto = PdfTextExtractor.GetTextFromPage(reader, i);
                        Assert.Contains("Taladro", texto);
                        Assert.Contains("INSUMO", texto);
                        Assert.Contains("FLETE", texto);
                        Assert.Contains("Observaciones", texto);
                    }
                }
            }
            finally { if (File.Exists(archivo)) File.Delete(archivo); }
        }
        [Fact]
        public void CadaCopiaLlevaSuRotuloYConservaElContenido()
        {
            var dto = new RemitoLectura { Fecha = new DateTime(2026, 1, 1), FechaArchivo = new DateTime(2026, 1, 2), NroRemito = "77", TipoMovimiento = "Salida", Origen = "Galpon", Destino = "Obra" };
            dto.Lineas.Add(new RemitoLinea { Material = "Perfil", Cantidad = 2 });
            var archivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try
            {
                new MovimientoPDF(new Recursos()).Generar(archivo, dto);
                using (var reader = new PdfReader(archivo))
                {
                    Assert.Equal(3, reader.NumberOfPages);
                    var rotulos = new[] { "ORIGINAL", "DUPLICADO", "TRIPLICADO" };
                    for (int i = 1; i <= 3; i++)
                    {
                        var texto = PdfTextExtractor.GetTextFromPage(reader, i);
                        Assert.Contains(rotulos[i - 1], texto);
                        foreach (var otro in rotulos) if (otro != rotulos[i - 1]) Assert.DoesNotContain(otro, texto);
                        Assert.Contains("00000077", texto);
                        Assert.Contains("Perfil", texto);
                    }
                }
            }
            finally { if (File.Exists(archivo)) File.Delete(archivo); }
        }
        [Fact]
        public void GeneraMixtoConCantidadesTiposYFlete()
        {
            var dto = new RemitoLectura { Fecha = new DateTime(2026, 1, 1), FechaArchivo = new DateTime(2026, 1, 2), NroRemito = "55", TipoMovimiento = "Salida", Origen = "Galpon", Destino = "Obra", Observaciones = "Control", CostoFlete = 1250.50m, PrecioTotal = 9000m };
            dto.Lineas.Add(new RemitoLinea { Material = "Perfil", Cantidad = 2.5m, PrecioUnitario = 500m });
            dto.Lineas.Add(new RemitoLinea { Material = "Chapa", Cantidad = 3, EsRecorte = true, MedidaRecorte = "20x30" });
            dto.Lineas.Add(new RemitoLinea { Material = "Bulon", Cantidad = 4, EsCompraEventual = true, PrecioUnitario = 100m });
            dto.Lineas.Add(new RemitoLinea { Material = "Taladro", Cantidad = 1, EsInsumo = true });
            var archivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try
            {
                new MovimientoPDF(new Recursos()).Generar(archivo, dto);
                using (var reader = new PdfReader(archivo))
                {
                    Assert.Equal(3, reader.NumberOfPages);
                    string texto = PdfTextExtractor.GetTextFromPage(reader, 1);
                    foreach (var esperado in new[] { "Perfil", "2.5", "Chapa", "20x30", "Bulon", "COMPRA EVENTUAL", "Taladro", "INSUMO", "FLETE", "1.250,50", "Control" })
                        Assert.Contains(esperado, texto);
                    // El remito legacy no imprime precio unitario ni total; solo flete.
                    Assert.DoesNotContain("9.000,00", texto);
                }
                var muestra = Environment.GetEnvironmentVariable("STEELOG_REMITO_MUESTRA");
                if (!string.IsNullOrWhiteSpace(muestra)) File.Copy(archivo, muestra, true);
            }
            finally { if (File.Exists(archivo)) File.Delete(archivo); }
        }
        [Fact]
        public void OrdenesPendientesPdfValidaRutaItemsVaciosYGeneraArchivo()
        {
            var pdf = new OrdenesPendientesPDF(new Recursos());
            var items = new[]
            {
                new PendienteLectura
                {
                    Numero = "OC-001",
                    Proveedor = "Acindar",
                    Material = "Perfil UPN 100",
                    Pedida = 10,
                    Recibida = 4,
                    Faltante = 6,
                    Estado = "Pendiente"
                }
            };
            var ruta = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");

            Assert.Throws<ArgumentException>(() => pdf.Generar(" ", items));
            Assert.Throws<ArgumentNullException>(() => pdf.Generar(ruta, null));
            Assert.Throws<InvalidOperationException>(() => pdf.Generar(ruta, new PendienteLectura[0]));

            try
            {
                pdf.Generar(ruta, items);
                Assert.True(File.Exists(ruta));
                var bytes = File.ReadAllBytes(ruta);
                Assert.True(bytes.Length > 4);
                Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
            }
            finally
            {
                if (File.Exists(ruta)) File.Delete(ruta);
            }
        }

        [Fact]
        public void OrdenesPendientesPdfLiberaDescriptorSiFallaRenderizadoDeLogo()
        {
            var pdf = new OrdenesPendientesPDF(new RecursosConLogoInvalido());
            var items = new[]
            {
                new PendienteLectura
                {
                    Numero = "OC-001",
                    Proveedor = "Acindar",
                    Material = "Perfil UPN 100",
                    Pedida = 10,
                    Recibida = 4,
                    Faltante = 6,
                    Estado = "Pendiente"
                }
            };
            var ruta = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");

            try
            {
                Assert.ThrowsAny<Exception>(() => pdf.Generar(ruta, items));
                using (var fs = File.Open(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.NotNull(fs);
                }
            }
            finally
            {
                if (File.Exists(ruta)) File.Delete(ruta);
            }
        }
    }
}
