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
            public MarcaPdf Capturar() => new MarcaPdf { LogoPng = Invalido ? new byte[] { 1, 2 } : Logo(), FechaLocal = new DateTime(2024,2,1,12,30,0), Pie = "SteeLog - Sistema de Gestión de Inventario" };
        }
        [Theory] [InlineData(true)] [InlineData(false)]
        public void RenderizaDetalleResumenYRestriccion(bool saldo)
        {
            var evidence = Environment.GetEnvironmentVariable("STEELOG_F8_PDF_EVIDENCE");
            string carpeta = string.IsNullOrEmpty(evidence) ? Path.GetTempPath() : evidence;
            Directory.CreateDirectory(carpeta);
            string ruta = Path.Combine(carpeta, "kardex-" + (saldo ? "admin" : "galpon") + ".pdf");
            var datos = Enumerable.Range(0, 110).Select(i => new KardexItem { fecha = new DateTime(2024,1,31).AddDays(-i), nombre_material="Chapa " + i, tipo_movimiento = "ENVIO", cantidad = i % 2 == 0 ? 2 : -1, origen="Galpon", destino="Obra", saldo=98765.43m }).ToList();
            try {
                new KardexPDF(new Recursos()).Generar(ruta, new KardexSnapshot { Historial=datos, MaterialNombre="Todos los Materiales", FechaInicio=new DateTime(2024,1,1), FechaFin=new DateTime(2024,1,31), MostrarColumnaMaterial=true, MostrarColumnaSaldo=saldo });
                using (var pdf = new PdfReader(ruta)) {
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
                    Assert.Contains("DETALLE DE MOVIMIENTOS", texto); Assert.Contains("RESUMEN DEL PER", texto); Assert.Contains("Total Entradas", texto); Assert.Contains("Total Salidas", texto); Assert.Contains("Chapa 109", texto);
                    if (saldo) { Assert.Contains("Stock Res.", texto); Assert.Contains("Stock Actual", texto); }
                    else { Assert.DoesNotContain("Stock Res.", texto); Assert.DoesNotContain("Stock Actual", texto); Assert.DoesNotContain("98.765,43", texto); }
                }
            } finally { if (string.IsNullOrEmpty(evidence) && File.Exists(ruta)) File.Delete(ruta); }
        }
        [Fact] public void ErrorDeImagenLiberaArchivo()
        {
            string ruta = Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".pdf");
            try {
                Assert.ThrowsAny<Exception>(() => new KardexPDF(new Recursos { Invalido=true }).Generar(ruta, new KardexSnapshot { Historial = new List<KardexItem>() }));
                using (var archivo = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) Assert.True(archivo.CanWrite);
            } finally { if (File.Exists(ruta)) File.Delete(ruta); }
        }
    }
}
