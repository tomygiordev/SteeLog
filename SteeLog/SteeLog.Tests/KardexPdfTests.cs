using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Pdf;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class KardexPdfTests
    {
        private sealed class Recursos : IRecursosPdf
        {
            private static byte[] Logo()
            {
                using (var stream = typeof(SteeLog.VistaGeneral).Assembly.GetManifestResourceStream("SteeLog.Resources.Branding.steelog-logo-dark.png"))
                using (var bytes = new MemoryStream())
                {
                    if (stream == null) throw new InvalidOperationException("Falta el logo de SteeLog.");
                    stream.CopyTo(bytes);
                    return bytes.ToArray();
                }
            }
            public bool Invalido;
            public MarcaPdf Capturar() => new MarcaPdf { LogoPng = Invalido ? new byte[] { 1, 2 } : Logo(), FechaLocal = new DateTime(2024, 2, 1, 12, 30, 0), Pie = "SteeLog - Sistema de Gestión de Inventario" };
        }

        [Theory] [InlineData(true)] [InlineData(false)]
        public void RenderizaDetalleResumenYRestriccion(bool saldo)
        {
            EjecutarRenderizado(saldo, null);
        }

        internal void EjecutarRenderizado(bool saldo, string rutaEvidencia)
        {
            using (var scope = PdfTestScope.Crear("kardex-" + (saldo ? "admin" : "galpon"), rutaEvidencia))
            {
                string ruta = scope.ObtenerRuta("kardex-" + (saldo ? "admin" : "galpon") + ".pdf");
                var datos = Enumerable.Range(0, 110).Select(i => new KardexItem
                {
                    fecha = new DateTime(2024, 1, 31).AddDays(-i),
                    nombre_material = "Chapa " + i,
                    tipo_movimiento = i % 2 == 0 ? "ENTRADA" : "ENVIO",
                    cantidad = i % 2 == 0 ? 2 : 1,
                    origen = "Galpon",
                    destino = "Obra",
                    saldo = 98765.43m
                }).ToList();

                new KardexPDF(new Recursos()).Generar(ruta, new KardexSnapshot
                {
                    Historial = datos,
                    MaterialNombre = "Todos los Materiales",
                    FechaInicio = new DateTime(2024, 1, 1),
                    FechaFin = new DateTime(2024, 1, 31),
                    MostrarColumnaMaterial = true,
                    MostrarColumnaSaldo = saldo
                });

                using (var pdf = new PdfReader(ruta))
                {
                    Assert.True(pdf.NumberOfPages > 1);
                    var paginas = Enumerable.Range(1, pdf.NumberOfPages).Select(n => PdfTextExtractor.GetTextFromPage(pdf, n)).ToList();
                    var vistos = new HashSet<int>();
                    foreach (var pagina in paginas)
                    {
                        var movimientos = System.Text.RegularExpressions.Regex.Matches(pagina, @"Chapa (\d+)\b");
                        if (movimientos.Count == 0) continue;
                        Assert.Contains("DETALLE DE MOVIMIENTOS", pagina);
                        Assert.Contains("Fecha", pagina);
                        Assert.Contains("Movimiento", pagina);
                        Assert.Contains("Cant.", pagina);
                        Assert.Contains("Origen", pagina);
                        Assert.Contains("Destino", pagina);
                        foreach (System.Text.RegularExpressions.Match movimiento in movimientos)
                        {
                            int i = int.Parse(movimiento.Groups[1].Value);
                            Assert.Contains(datos[i].fecha.ToString("dd/MM/yyyy"), pagina);
                            vistos.Add(i);
                        }
                    }
                    Assert.Equal(datos.Count, vistos.Count);
                    var texto = string.Join("\n", paginas);
                    Assert.Contains("DETALLE DE MOVIMIENTOS", texto);
                    Assert.Contains("RESUMEN DEL PER", texto);
                    Assert.Contains("Total Entradas", texto);
                    Assert.Contains("Total Salidas", texto);
                    Assert.Contains("+110", texto);
                    Assert.Contains("-55", texto);
                    Assert.Contains("Chapa 109", texto);
                    if (saldo)
                    {
                        Assert.Contains("Stock Res.", texto);
                        Assert.Contains("Stock Actual", texto);
                    }
                    else
                    {
                        Assert.DoesNotContain("Stock Res.", texto);
                        Assert.DoesNotContain("Stock Actual", texto);
                        Assert.DoesNotContain("98.765,43", texto);
                    }
                }
            }
        }

        [Fact]
        public void EnvioConCantidadPositivaSeResumeComoSalidaSinAlterarSaldo()
        {
            using (var scope = PdfTestScope.Crear("kardex-envio"))
            {
                string ruta = scope.ObtenerRuta("kardex-envio.pdf");
                var item = new KardexItem { tipo_movimiento = "ENVÍO", cantidad = 2m, saldo = 8m,
                    fecha = new DateTime(2024, 1, 31), origen = "Galpón", destino = "Obra demo" };
                new KardexPDF(new Recursos()).Generar(ruta, new KardexSnapshot {
                    Historial = new List<KardexItem> { item }, MaterialNombre = "Perfil C",
                    FechaInicio = item.fecha, FechaFin = item.fecha, MostrarColumnaSaldo = true
                });
                using (var pdf = new PdfReader(ruta))
                {
                    string texto = PdfTextExtractor.GetTextFromPage(pdf, 1);
                    Assert.Matches(@"Total Entradas:\s*\+0", texto);
                    Assert.Matches(@"Total Salidas:\s*-2", texto);
                    Assert.Contains("8,00", texto);
                    Assert.Equal(2m, item.cantidad);
                    Assert.Equal(8m, item.saldo);
                }
            }
        }

        [Fact]
        public void ErrorDeImagenLiberaArchivo()
        {
            using (var scope = PdfTestScope.CrearTemporal("kardex-error"))
            {
                string ruta = scope.ObtenerRuta("error.pdf");
                Assert.ThrowsAny<Exception>(() => new KardexPDF(new Recursos { Invalido = true }).Generar(ruta, new KardexSnapshot { Historial = new List<KardexItem>() }));
                using (var archivo = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.True(archivo.CanWrite);
                }
            }
        }

        [Fact]
        public void EjecucionesAisladasPreservanArchivoPreexistente()
        {
            using (var basePropia = PdfTestScope.CrearTemporal("sentinel-kardex"))
            {
                string sentinel = basePropia.ObtenerRuta("kardex-admin.pdf");
                File.WriteAllText(sentinel, "testigo propio");
                EjecutarRenderizado(saldo: true, rutaEvidencia: basePropia.Directorio);
                EjecutarRenderizado(saldo: true, rutaEvidencia: basePropia.Directorio);
                Assert.Equal("testigo propio", File.ReadAllText(sentinel));
                var exportaciones = Directory.GetDirectories(basePropia.Directorio);
                Assert.Equal(2, exportaciones.Length);
                Assert.NotEqual(exportaciones[0], exportaciones[1]);
                foreach (var directorio in exportaciones)
                    Assert.True(File.Exists(Path.Combine(directorio, "kardex-admin.pdf")));
            }
        }

        [Fact]
        public void PreservaExportacionConSubdirectorioPropioCuandoVariableEvidenciaEstaDefinida()
        {
            string baseEvidencia = Path.Combine(Path.GetTempPath(), "steelog-f8-evidencia-kardex-" + Guid.NewGuid().ToString("N"));
            try
            {
                string rutaExportada;
                using (var scope = PdfTestScope.Crear("kardex-admin", baseEvidencia))
                {
                    rutaExportada = scope.ObtenerRuta("kardex-admin.pdf");
                    new KardexPDF(new Recursos()).Generar(rutaExportada, new KardexSnapshot
                    {
                        Historial = new List<KardexItem>(),
                        MaterialNombre = "Material Evidencia",
                        FechaInicio = new DateTime(2024, 1, 1),
                        FechaFin = new DateTime(2024, 1, 31),
                        MostrarColumnaMaterial = true,
                        MostrarColumnaSaldo = true
                    });
                    Assert.True(File.Exists(rutaExportada));
                }

                Assert.True(File.Exists(rutaExportada), "El export de evidencia no debe ser eliminado al finalizar la prueba.");
                string subdirectorio = Path.GetDirectoryName(rutaExportada);
                Assert.NotNull(subdirectorio);
                Assert.StartsWith(Path.GetFullPath(baseEvidencia), Path.GetFullPath(subdirectorio), StringComparison.OrdinalIgnoreCase);
                Assert.NotEqual(Path.GetFullPath(baseEvidencia), Path.GetFullPath(subdirectorio));
            }
            finally
            {
                if (Directory.Exists(baseEvidencia))
                {
                    Directory.Delete(baseEvidencia, recursive: true);
                }
            }
        }
    }
}
