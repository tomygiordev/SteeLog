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
            string evidence = Environment.GetEnvironmentVariable("STEELOG_F8_PDF_EVIDENCE");
            string carpeta = string.IsNullOrEmpty(evidence) ? Path.GetTempPath() : evidence;
            Directory.CreateDirectory(carpeta);
            string ruta = Path.Combine(carpeta, "stock-" + (admin ? "admin" : "galpon") + ".pdf");
            var snapshot = new ReporteStockSnapshot { TotalMateriales = "120", TotalStockBajo = "1" };
            snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "nombre", Titulo = "Material" });
            snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "stockActual", Titulo = "Stock" });
            if (admin) snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "precioUnitario", Titulo = "Precio" });
            snapshot.Filas.Add(new ReporteStockFilaPdf { Seccion = "Perfiles" });
            for (int i = 0; i < 120; i++) {
                var fila = new ReporteStockFilaPdf { StockBajo = i == 0 };
                fila.Valores.Add("Material " + i); fila.Valores.Add(admin ? "12" : "Disponible");
                if (admin) fila.Valores.Add("98765,43"); snapshot.Filas.Add(fila);
            }
            try {
                new ReporteStockPDF(new Recursos()).Generar(ruta, snapshot);
                using (var pdf = new PdfReader(ruta)) {
                    Assert.True(pdf.NumberOfPages > 1);
                    string texto = string.Join("\n", Enumerable.Range(1, pdf.NumberOfPages).Select(i => PdfTextExtractor.GetTextFromPage(pdf, i)));
                    Assert.Contains("PERFILES", texto); Assert.Contains("Material 119", texto); Assert.Contains("Total Materiales", texto);
                    if (admin) Assert.Contains("98765,43", texto); else { Assert.DoesNotContain("98765,43", texto); Assert.DoesNotContain("Precio", texto); }
                }
            } finally { if (string.IsNullOrEmpty(evidence) && File.Exists(ruta)) File.Delete(ruta); }
        }
        [Fact] public void ErrorDeImagenLiberaArchivo()
        {
            string ruta = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            var snapshot = new ReporteStockSnapshot(); snapshot.Columnas.Add(new ReporteStockColumnaPdf { Nombre = "nombre" });
            try { Assert.ThrowsAny<Exception>(() => new ReporteStockPDF(new Recursos { Invalido = true }).Generar(ruta, snapshot)); using (var archivo = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) Assert.True(archivo.CanWrite); }
            finally { if (File.Exists(ruta)) File.Delete(ruta); }
        }
    }
}
