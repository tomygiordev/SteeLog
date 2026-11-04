using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SteeLog.Core.Application.Reportes;

namespace SteeLog.Infrastructure.Pdf
{
    public sealed class ReporteStockPDF : IReporteStockPdf
    {
        private readonly IRecursosPdf recursos;
        public ReporteStockPDF(IRecursosPdf recursos) { this.recursos = recursos ?? throw new ArgumentNullException(nameof(recursos)); }
        public void Generar(string ruta, ReporteStockSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Columnas == null || snapshot.Columnas.Count == 0 || snapshot.Filas == null) throw new ArgumentException("Reporte incompleto.", nameof(snapshot));
            var marca = recursos.Capturar();
            using (var stream = new FileStream(ruta, FileMode.Create))
            using (var doc = new Document(PageSize.A4, 40, 40, 40, 60))
            {
                var writer = PdfWriter.GetInstance(doc, stream);
                writer.PageEvent = new PieReporteStock(marca);
                doc.Open();
                var header = new PdfPTable(2) { WidthPercentage = 100 };
                header.SetWidths(new float[] { 45, 55 });
                var izq = new PdfPCell { Border = Rectangle.NO_BORDER };
                var logo = Image.GetInstance(marca.LogoPng); logo.ScaleToFit(150, 62); izq.AddElement(logo);
                izq.AddElement(new Paragraph("SteeLog — Sistema de gestión de inventario", Fuente(8)) { SpacingBefore = 8 });
                izq.AddElement(new Paragraph("Proyecto académico · Datos de demostración", Fuente(7)));
                izq.AddElement(new Paragraph("SteeLog - Logística y Stock", Fuente(7)));
                header.AddCell(izq);
                var der = new PdfPCell { Border = Rectangle.NO_BORDER };
                der.AddElement(new Paragraph("REPORTE DE STOCK", Fuente(20, true)) { Alignment = Element.ALIGN_RIGHT });
                der.AddElement(new Paragraph("Inventario al " + marca.FechaLocal.ToString("dd/MM/yyyy HH:mm"), Fuente(11)) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });
                header.AddCell(der); doc.Add(header);
                var linea = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 5, SpacingAfter = 15 };
                linea.AddCell(new PdfPCell { Border = Rectangle.BOTTOM_BORDER, BorderColor = new BaseColor(25, 135, 84), BorderWidth = 2, FixedHeight = 3 }); doc.Add(linea);
                var info = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 20 };
                info.SetWidths(new float[] { 22, 28, 22, 28 });
                foreach (var texto in new[] { "Total Materiales:", snapshot.TotalMateriales, "Stock Bajo:", snapshot.TotalStockBajo })
                    info.AddCell(new PdfPCell(new Phrase(texto ?? "", Fuente(10))) { Padding = 8, BorderColor = new BaseColor(230, 230, 230) });
                doc.Add(info);
                var tabla = new PdfPTable(snapshot.Columnas.Count) { WidthPercentage = 100, HeaderRows = 1 };
                foreach (var columna in snapshot.Columnas)
                    tabla.AddCell(new PdfPCell(new Phrase(columna.Titulo ?? "", new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.WHITE))) { BackgroundColor = new BaseColor(35, 63, 43), Border = Rectangle.NO_BORDER, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER });
                bool alterna = false;
                foreach (var fila in snapshot.Filas)
                {
                    if (fila.Seccion != null)
                    {
                        tabla.AddCell(new PdfPCell(new Phrase(fila.Seccion.ToUpperInvariant(), new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.WHITE))) { Colspan = snapshot.Columnas.Count, BackgroundColor = new BaseColor(24, 74, 53), Border = Rectangle.NO_BORDER, Padding = 7 });
                        alterna = false; continue;
                    }
                    if (fila.Valores == null || fila.Valores.Count != snapshot.Columnas.Count) throw new ArgumentException("Fila de reporte incompleta.", nameof(snapshot));
                    for (int i = 0; i < snapshot.Columnas.Count; i++)
                    {
                        string nombre = snapshot.Columnas[i].Nombre ?? "";
                        bool bajo = nombre == "stockActual" && fila.StockBajo;
                        var color = bajo ? new BaseColor(200, 30, 30) : new BaseColor(33, 37, 41);
                        var celda = new PdfPCell(new Phrase(fila.Valores[i] ?? "", new Font(Font.FontFamily.HELVETICA, 9, bajo ? Font.BOLD : Font.NORMAL, color))) {
                            BackgroundColor = bajo ? new BaseColor(255, 235, 235) : alterna ? new BaseColor(245, 248, 246) : BaseColor.WHITE,
                            BorderColor = new BaseColor(230, 230, 230), BorderWidth = 0.5f, Padding = 5,
                            HorizontalAlignment = nombre == "precioUnitario" || nombre == "valorTotal" ? Element.ALIGN_RIGHT : nombre.Contains("stock") ? Element.ALIGN_CENTER : Element.ALIGN_LEFT };
                        tabla.AddCell(celda);
                    }
                    alterna = !alterna;
                }
                doc.Add(tabla);
            }
        }
        private static Font Fuente(float tamano, bool negrita = false) => new Font(Font.FontFamily.HELVETICA, tamano, negrita ? Font.BOLD : Font.NORMAL, new BaseColor(80, 80, 80));
        private sealed class PieReporteStock : PdfPageEventHelper
        {
            private readonly MarcaPdf marca;
            public PieReporteStock(MarcaPdf marca) { this.marca = marca; }
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContent;
                cb.SetColorStroke(new BaseColor(25, 135, 84)); cb.SetLineWidth(1); cb.MoveTo(document.LeftMargin, document.Bottom - 15); cb.LineTo(document.PageSize.Width - document.RightMargin, document.Bottom - 15); cb.Stroke();
                cb.BeginText(); cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_OBLIQUE, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 8); cb.SetColorFill(new BaseColor(120, 120, 120));
                cb.ShowTextAligned(Element.ALIGN_LEFT, (marca.Pie ?? "") + " — Generado " + marca.FechaLocal.ToString("dd/MM/yyyy HH:mm"), document.LeftMargin, document.Bottom - 28, 0);
                cb.ShowTextAligned(Element.ALIGN_RIGHT, "Página " + writer.PageNumber, document.PageSize.Width - document.RightMargin, document.Bottom - 28, 0); cb.EndText();
            }
        }
    }
}
