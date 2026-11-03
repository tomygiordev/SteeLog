using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Core.Application.Reportes;

namespace SteeLog.Infrastructure.Pdf
{
    public sealed class OrdenesPendientesPDF : IOrdenesPendientesPdf
    {
        private readonly IRecursosPdf recursos;
        public OrdenesPendientesPDF(IRecursosPdf recursos) { this.recursos = recursos ?? throw new ArgumentNullException(nameof(recursos)); }

        // Colores corporativos
        private static readonly BaseColor COLOR_VERDE_OSCURO = new BaseColor(25, 50, 35);
        private static readonly BaseColor COLOR_VERDE_HEADER = new BaseColor(35, 63, 43);
        private static readonly BaseColor COLOR_VERDE_ACCENT = new BaseColor(78, 91, 63);
        private static readonly BaseColor COLOR_FONDO_FILA = new BaseColor(245, 248, 246);
        private static readonly BaseColor COLOR_BLANCO = BaseColor.WHITE;
        private static readonly BaseColor COLOR_GRIS_CLARO = new BaseColor(230, 230, 230);
        private static readonly BaseColor COLOR_TEXTO = new BaseColor(33, 37, 41);

        // Fuentes
        private static Font fntTituloPrincipal;
        private static Font fntSeccion;
        private static Font fntHeaderTabla;
        private static Font fntCelda;
        private static Font fntCeldaBold;
        private static Font fntDato;
        private static Font fntEtiqueta;

        static OrdenesPendientesPDF()
        {
            var baseFontNormal = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            var baseFontBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            fntTituloPrincipal = new Font(baseFontBold, 15, Font.NORMAL, COLOR_VERDE_OSCURO);
            fntSeccion = new Font(baseFontBold, 10, Font.NORMAL, COLOR_BLANCO);
            fntHeaderTabla = new Font(baseFontBold, 9, Font.NORMAL, COLOR_BLANCO);
            fntCelda = new Font(baseFontNormal, 9, Font.NORMAL, COLOR_TEXTO);
            fntCeldaBold = new Font(baseFontBold, 9, Font.NORMAL, COLOR_TEXTO);
            fntDato = new Font(baseFontNormal, 10, Font.NORMAL, COLOR_TEXTO);
            fntEtiqueta = new Font(baseFontBold, 10, Font.NORMAL, new BaseColor(80, 80, 80));
        }

        public void Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count == 0) throw new Exception("No hay órdenes pendientes para imprimir.");

            MarcaPdf marca = recursos.Capturar();
            Document doc = new Document(PageSize.A4, 40, 40, 40, 60);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));
            
            // Footer
            writer.PageEvent = new PieOrden(marca.Pie);

            doc.Open();

            // Auto open print dialog when opened in viewer
            PdfAction action = PdfAction.JavaScript("this.print(true);", writer);
            writer.AddJavaScript(action);

            // =================== ENCABEZADO ===================
            AgregarEncabezado(doc, marca);

            // =================== TABLA DE ÓRDENES ===================
            AgregarTablaOrdenes(doc, items);

            doc.Close();
        }

        public string SugerirNombreArchivo()
        {
            return $"Reporte_Ordenes_Pendientes_{recursos.Capturar().FechaLocal:yyyyMMdd_HHmmss}.pdf";
        }

        private void AgregarEncabezado(Document doc, MarcaPdf marca)
        {
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 45f, 55f });

            // ======= IZQUIERDA: LOGO Y DATOS =======
            PdfPCell cellIzq = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };

            var logo = iTextSharp.text.Image.GetInstance(marca.LogoPng);
            logo.ScaleToFit(150, 62);
            logo.Alignment = Element.ALIGN_LEFT;
            cellIzq.AddElement(logo);

            var fntDir = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, new BaseColor(80, 80, 80));
            var fntTributo = new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, new BaseColor(80, 80, 80));

            Paragraph pDireccion = new Paragraph("SteeLog — Sistema de gestión de inventario", fntDir) { SpacingBefore = 8, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pDireccion);
            Paragraph pTributos = new Paragraph("Proyecto académico · Datos de demostración", fntTributo) { SpacingBefore = 2, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pTributos);

            Paragraph pInicio = new Paragraph("SteeLog - Logística y Stock", fntTributo) { SpacingBefore = 1, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pInicio);

            headerTable.AddCell(cellIzq);

            // ======= DERECHA: TITULO =======
            PdfPCell cellDer = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
            
            cellDer.AddElement(new Paragraph("ÓRDENES PENDIENTES", fntTituloPrincipal) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
            cellDer.AddElement(new Paragraph($"Fecha: {marca.FechaLocal:dd/MM/yyyy HH:mm}", new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, COLOR_TEXTO)) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 2 });

            headerTable.AddCell(cellDer);

            doc.Add(headerTable);

            // Línea accent
            PdfPTable lineaSep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 10, SpacingAfter = 15 };
            lineaSep.AddCell(new PdfPCell { Border = Rectangle.BOTTOM_BORDER, BorderColor = COLOR_VERDE_ACCENT, BorderWidth = 2, FixedHeight = 3 });
            doc.Add(lineaSep);
        }

        private sealed class PieOrden : PdfPageEventHelper
        {
            private readonly string texto;
            public PieOrden(string texto) { this.texto = texto; }
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContent;
                var fuente = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                cb.SetColorStroke(new BaseColor(78, 91, 63)); cb.SetLineWidth(1f);
                cb.MoveTo(document.LeftMargin, document.Bottom - 15); cb.LineTo(document.PageSize.Width - document.RightMargin, document.Bottom - 15); cb.Stroke();
                cb.BeginText(); cb.SetFontAndSize(fuente, 8); cb.SetColorFill(new BaseColor(120, 120, 120));
                cb.ShowTextAligned(Element.ALIGN_LEFT, texto, document.LeftMargin, document.Bottom - 28, 0); cb.EndText();
            }
        }

        private void AgregarTablaOrdenes(Document doc, IReadOnlyList<PendienteLectura> items)
        {
            PdfPTable tabla = new PdfPTable(5);
            tabla.WidthPercentage = 100;
            // Orden | Material | Pedido | Recibido | Faltante
            tabla.SetWidths(new float[] { 20f, 44f, 12f, 12f, 12f });

            // Headers
            string[] headers = { "ORDEN", "MATERIAL / INSUMO", "PEDIDO", "RECIBIDO", "FALTANTE" };
            foreach (var h in headers) {
                PdfPCell cell = new PdfPCell(new Phrase(h, fntHeaderTabla)) { BackgroundColor = COLOR_VERDE_ACCENT, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, Border = Rectangle.NO_BORDER };
                tabla.AddCell(cell);
            }

            bool alt = false;
            foreach (var item in items) {
                BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;
                
                // Nro Orden y Proveedor
                Phrase pOrden = new Phrase();
                pOrden.Add(new Chunk(item.Numero + "\n", fntCeldaBold));
                pOrden.Add(new Chunk(item.Proveedor, new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, new BaseColor(80, 80, 80))));
                PdfPCell cOrden = new PdfPCell(pOrden) { BackgroundColor = bg, Padding = 6, BorderColor = COLOR_GRIS_CLARO, VerticalAlignment = Element.ALIGN_MIDDLE };
                tabla.AddCell(cOrden);

                // Material
                Phrase pMat = new Phrase(item.Material, fntCelda);
                PdfPCell cMat = new PdfPCell(pMat) { BackgroundColor = bg, Padding = 6, BorderColor = COLOR_GRIS_CLARO, VerticalAlignment = Element.ALIGN_MIDDLE };
                tabla.AddCell(cMat);

                // Pedido
                tabla.AddCell(new PdfPCell(new Phrase(item.Pedida.ToString(), fntCelda)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE, BorderColor = COLOR_GRIS_CLARO });

                // Recibido
                tabla.AddCell(new PdfPCell(new Phrase(item.Recibida.ToString(), fntCelda)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE, BorderColor = COLOR_GRIS_CLARO });

                // Faltante
                tabla.AddCell(new PdfPCell(new Phrase(item.Faltante.ToString(), fntCeldaBold)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE, BorderColor = COLOR_GRIS_CLARO });

                alt = !alt;
            }

            doc.Add(tabla);
        }
    }
}
