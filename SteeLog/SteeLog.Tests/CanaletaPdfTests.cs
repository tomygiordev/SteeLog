using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text.pdf;
using PdfTextExtractor = iTextSharp.text.pdf.parser.PdfTextExtractor;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Pdf;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CanaletaPdfTests
    {
        private sealed class Recursos : IRecursosPdf
        {
            public bool Invalidos;
            public MarcaPdf Capturar()
            {
                byte[] logo;
                using (var stream = typeof(SteeLog.VistaGeneral).Assembly.GetManifestResourceStream("SteeLog.Resources.Branding.steelog-logo-dark.png"))
                using (var bytes = new MemoryStream())
                {
                    if (stream == null) throw new InvalidOperationException("Falta el logo de SteeLog.");
                    stream.CopyTo(bytes);
                    logo = bytes.ToArray();
                }
                return new MarcaPdf { FechaLocal = new DateTime(2025, 1, 1), LogoPng = Invalidos ? new byte[] { 1, 2, 3 } : logo };
            }
        }
        private static InformeObraSnapshot Datos(bool valor) => new InformeObraSnapshot {
            NombreObra = "Obra de prueba", MostrarValorizacion = valor,
            Resumen = new InformeObraResumenPdf { CantRemitos = 1, TotalPendiente = 123m },
            Movimientos = new List<InformeMovimientoDetallePdf> { new InformeMovimientoDetallePdf { id_movimiento=1, fecha=new DateTime(2025,1,1), tipo_movimiento="ENVIO", nroremito="R1", material="Chapa", cantidad=1, precio_unitario=100 } },
            Herramientas = new List<InformeHerramientaDetallePdf>(), Insumos = new List<InformeInsumoDetallePdf>(),
            Canaletas = new List<InformeCanaletaPlegadoDetallePdf> { new InformeCanaletaPlegadoDetallePdf { id=1, descripcion="Canaleta especial", fecha_creacion=new DateTime(2025,1,1), precio=23m } }
        };
        [Theory]
        [InlineData(true)] [InlineData(false)]
        public void RenderizaSinBaseYRespetaValorizacion(bool valor)
        {
            var ruta = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try {
                var datos = Datos(valor);
                datos.Movimientos[0].costoflete = 5m;
                datos.Herramientas.Add(new InformeHerramientaDetallePdf { nombre = "Herramienta demo", estado = "EN OBRA", en_obra = true });
                datos.Insumos.Add(new InformeInsumoDetallePdf { nombre = "Insumo demo", en_obra = true });
                for (int i = 0; i < 70; i++) datos.Canaletas.Add(new InformeCanaletaPlegadoDetallePdf { id = i + 2, descripcion = "Plegado de prueba " + i + " descripción extensa para verificar el corte de página y las tablas", fecha_creacion = new DateTime(2025, 1, 1), precio = 10m });
                datos.Resumen.TotalPendiente = 828m;
                new InformeObraPDF(new Recursos()).Generar(ruta, datos);
                string evidencia = Environment.GetEnvironmentVariable("STEELOG_CANALETAS_PDF_EVIDENCE");
                if (!string.IsNullOrWhiteSpace(evidencia)) {
                    string temp = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
                    string destino = System.IO.Path.GetFullPath(evidencia);
                    if (!destino.StartsWith(temp, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("La evidencia debe estar dentro de TEMP.");
                    Directory.CreateDirectory(destino);
                    File.Copy(ruta, System.IO.Path.Combine(destino, valor ? "informe-admin.pdf" : "informe-galpon.pdf"), true);
                }
                using (var pdf = new PdfReader(ruta)) {
                    Assert.True(pdf.NumberOfPages > 1);
                    string texto = "";
                    for(int i=1;i<=pdf.NumberOfPages;i++) {
                        string pagina = PdfTextExtractor.GetTextFromPage(pdf, i); texto += pagina;
                        if (pagina.Contains("Plegado de prueba")) {
                            Assert.Contains("CANALETAS / PLEGADOS", pagina);
                            Assert.Contains("Descripción", pagina); Assert.Contains("Pagado", pagina);
                        }
                        if (pagina.Contains("HERRAMIENTAS E INSUMOS")) Assert.Contains("Herramienta demo", pagina);
                    }
                    Assert.Contains("INFORME DE OBRA", texto); Assert.Contains("SteeLog", texto);
                    var resources = pdf.GetPageN(1).GetAsDict(PdfName.RESOURCES);
                    var imagenes = resources.GetAsDict(PdfName.XOBJECT);
                    Assert.NotNull(imagenes);
                    Assert.Contains(imagenes.Keys, key => PdfName.IMAGE.Equals(imagenes.GetAsStream(key)?.GetAsName(PdfName.SUBTYPE)));
                    Assert.Contains("Canaleta especial", texto); Assert.Contains("Chapa", texto);
                    Assert.Contains("Herramienta demo", texto); Assert.Contains("Insumo demo", texto);
                    if (valor) { Assert.Contains("TOTAL PENDIENTE", texto); Assert.Contains("828", texto); Assert.Contains("FLETES DE LA OBRA", texto); }
                    else { Assert.DoesNotContain("TOTAL PENDIENTE", texto); Assert.DoesNotContain("FLETES DE LA OBRA", texto); }
                }
                using (File.Open(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            } finally { if (File.Exists(ruta)) File.Delete(ruta); }
        }
        [Fact]
        public void ErrorDeRecursoLiberaArchivo()
        {
            var ruta = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try {
                Assert.ThrowsAny<Exception>(new Action(() => new InformeObraPDF(new Recursos { Invalidos=true }).Generar(ruta, Datos(true))));
                Assert.False(File.Exists(ruta));
            }
            finally { if (File.Exists(ruta)) File.Delete(ruta); }
        }
    }
}
