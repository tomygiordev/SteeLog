using System;
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
    internal sealed class PdfTestScope : IDisposable
    {
        private readonly string _idExclusivo;
        private readonly string _directorio;
        private readonly bool _esEvidencia;

        public string Directorio => _directorio;
        public bool EsEvidencia => _esEvidencia;

        private PdfTestScope(string prefijo, string rutaEvidencia)
        {
            _idExclusivo = Guid.NewGuid().ToString("N");
            string prefijoLimpio = string.IsNullOrWhiteSpace(prefijo) ? "pdf-test" : prefijo.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                prefijoLimpio = prefijoLimpio.Replace(c, '_');
            }

            // Si rutaEvidencia es provista (incluso "" para forzar modo temp aislado), se respeta.
            // Si es null, se consulta la variable de entorno STEELOG_F8_PDF_EVIDENCE.
            string evidenciaEfectiva = rutaEvidencia ?? Environment.GetEnvironmentVariable("STEELOG_F8_PDF_EVIDENCE");

            if (!string.IsNullOrWhiteSpace(evidenciaEfectiva))
            {
                _esEvidencia = true;
                string baseFolder = Path.GetFullPath(evidenciaEfectiva.Trim());
                _directorio = Path.Combine(baseFolder, $"{prefijoLimpio}-{_idExclusivo}");
            }
            else
            {
                _esEvidencia = false;
                string baseFolder = Path.GetFullPath(Path.GetTempPath());
                _directorio = Path.Combine(baseFolder, $"steelog-{prefijoLimpio}-{_idExclusivo}");
            }

            Directory.CreateDirectory(_directorio);
        }

        public static PdfTestScope Crear(string prefijo, string rutaEvidencia = null)
        {
            return new PdfTestScope(prefijo, rutaEvidencia);
        }

        public static PdfTestScope CrearTemporal(string prefijo)
        {
            return new PdfTestScope(prefijo, string.Empty);
        }

        public string ObtenerRuta(string nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("El nombre del archivo no puede ser nulo ni vacío.", nameof(nombreArchivo));

            return Path.Combine(_directorio, nombreArchivo);
        }

        public void Dispose()
        {
            if (_esEvidencia)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_directorio))
                return;

            try
            {
                if (Directory.Exists(_directorio))
                {
                    string fullDirectorio = Path.GetFullPath(_directorio).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                    if (fullDirectorio.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) &&
                        fullDirectorio.Length > tempRoot.Length &&
                        fullDirectorio.Contains(_idExclusivo))
                    {
                        Directory.Delete(fullDirectorio, recursive: true);
                    }
                }
            }
            catch
            {
                // Evitar que excepciones de limpieza enmascaren fallos legítimos de la prueba
            }
        }
    }

    public sealed class ReporteStockPdfTests
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
        public void RenderizaMultipaginaYColumnasPermitidas(bool admin)
        {
            EjecutarRenderizado(admin, null);
        }

        internal void EjecutarRenderizado(bool admin, string rutaEvidencia)
        {
            using (var scope = PdfTestScope.Crear("stock-" + (admin ? "admin" : "galpon"), rutaEvidencia))
            {
                string ruta = scope.ObtenerRuta("stock-" + (admin ? "admin" : "galpon") + ".pdf");
                var snapshot = new ReporteStockSnapshot { TotalMateriales = "120", TotalStockBajo = "1" };
                snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "nombre", Titulo = "Material" });
                snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "stockActual", Titulo = "Stock" });
                if (admin) snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "precioUnitario", Titulo = "Precio" });
                snapshot.Filas.Add(new ReporteStockFilaPdf { Seccion = "Perfiles" });
                for (int i = 0; i < 120; i++)
                {
                    var fila = new ReporteStockFilaPdf { StockBajo = i == 0 };
                    fila.Valores.Add("Material " + i);
                    fila.Valores.Add(admin ? "12" : "Disponible");
                    if (admin) fila.Valores.Add("98765,43");
                    snapshot.Filas.Add(fila);
                }

                new ReporteStockPDF(new Recursos()).Generar(ruta, snapshot);
                using (var pdf = new PdfReader(ruta))
                {
                    Assert.True(pdf.NumberOfPages > 1);
                    string texto = string.Join("\n", Enumerable.Range(1, pdf.NumberOfPages).Select(i => PdfTextExtractor.GetTextFromPage(pdf, i)));
                    Assert.Contains("PERFILES", texto);
                    Assert.Contains("Material 119", texto);
                    Assert.Contains("Total Materiales", texto);
                    if (admin)
                        Assert.Contains("98765,43", texto);
                    else
                    {
                        Assert.DoesNotContain("98765,43", texto);
                        Assert.DoesNotContain("Precio", texto);
                    }
                }
            }
        }

        [Fact]
        public void ErrorDeImagenLiberaArchivo()
        {
            using (var scope = PdfTestScope.CrearTemporal("stock-error"))
            {
                string ruta = scope.ObtenerRuta("error.pdf");
                var snapshot = new ReporteStockSnapshot();
                snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "nombre" });
                Assert.ThrowsAny<Exception>(() => new ReporteStockPDF(new Recursos { Invalido = true }).Generar(ruta, snapshot));
                using (var archivo = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.True(archivo.CanWrite);
                }
            }
        }

        [Fact]
        public void EjecucionesAisladasPreservanArchivoPreexistente()
        {
            using (var basePropia = PdfTestScope.CrearTemporal("sentinel-stock"))
            {
                string sentinel = basePropia.ObtenerRuta("stock-admin.pdf");
                File.WriteAllText(sentinel, "testigo propio");
                EjecutarRenderizado(admin: true, rutaEvidencia: basePropia.Directorio);
                EjecutarRenderizado(admin: true, rutaEvidencia: basePropia.Directorio);
                Assert.Equal("testigo propio", File.ReadAllText(sentinel));
                var exportaciones = Directory.GetDirectories(basePropia.Directorio);
                Assert.Equal(2, exportaciones.Length);
                Assert.NotEqual(exportaciones[0], exportaciones[1]);
                foreach (var directorio in exportaciones)
                    Assert.True(File.Exists(Path.Combine(directorio, "stock-admin.pdf")));
            }
        }

        [Fact]
        public void PreservaExportacionConSubdirectorioPropioCuandoVariableEvidenciaEstaDefinida()
        {
            string baseEvidencia = Path.Combine(Path.GetTempPath(), "steelog-f8-evidencia-stock-" + Guid.NewGuid().ToString("N"));
            try
            {
                string rutaExportada;
                using (var scope = PdfTestScope.Crear("stock-admin", baseEvidencia))
                {
                    rutaExportada = scope.ObtenerRuta("stock-admin.pdf");
                    var snapshot = new ReporteStockSnapshot { TotalMateriales = "1", TotalStockBajo = "0" };
                    snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "nombre", Titulo = "Material" });
                    snapshot.Filas.Add(new ReporteStockFilaPdf { Valores = { "Material Test" } });

                    new ReporteStockPDF(new Recursos()).Generar(rutaExportada, snapshot);
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
