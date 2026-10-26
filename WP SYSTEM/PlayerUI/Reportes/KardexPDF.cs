using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using WP_Inventario.Modelos;

namespace WP_Inventario.Reportes
{
    public class KardexPDF
    {
        // ── Paleta corporativa (igual que MovimientoPDF) ──────────────────────
        private static readonly BaseColor COLOR_VERDE_OSCURO  = new BaseColor(25,  50,  35);
        private static readonly BaseColor COLOR_VERDE_HEADER  = new BaseColor(35,  63,  43);
        private static readonly BaseColor COLOR_VERDE_ACCENT  = new BaseColor(78,  91,  63);
        private static readonly BaseColor COLOR_FONDO_FILA    = new BaseColor(245, 248, 246);
        private static readonly BaseColor COLOR_BLANCO        = BaseColor.WHITE;
        private static readonly BaseColor COLOR_GRIS_CLARO    = new BaseColor(220, 220, 220);
        private static readonly BaseColor COLOR_TEXTO         = new BaseColor(33,  37,  41);

        // ── Fuentes ───────────────────────────────────────────────────────────
        private static Font fntTitulo;
        private static Font fntSubtitulo;
        private static Font fntEtiqueta;
        private static Font fntDato;
        private static Font fntHeaderTabla;
        private static Font fntCelda;
        private static Font fntCeldaBold;
        private static Font fntDir;
        private static Font fntTributo;
        private static Font fntSeccion;

        static KardexPDF()
        {
            var bfn  = BaseFont.CreateFont(BaseFont.HELVETICA,      BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            var bfb  = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            fntTitulo      = new Font(bfb,  15, Font.NORMAL, COLOR_VERDE_OSCURO);
            fntSubtitulo   = new Font(bfb,  10, Font.NORMAL, new BaseColor(80, 80, 80));
            fntEtiqueta    = new Font(bfb,  10, Font.NORMAL, new BaseColor(80, 80, 80));
            fntDato        = new Font(bfn,  10, Font.NORMAL, COLOR_TEXTO);
            fntHeaderTabla = new Font(bfb,   9, Font.NORMAL, COLOR_BLANCO);
            fntCelda       = new Font(bfn,   9, Font.NORMAL, COLOR_TEXTO);
            fntCeldaBold   = new Font(bfb,   9, Font.NORMAL, COLOR_TEXTO);
            fntDir         = new Font(bfn,   8, Font.NORMAL, new BaseColor(80, 80, 80));
            fntTributo     = new Font(bfn,   7, Font.NORMAL, new BaseColor(80, 80, 80));
            fntSeccion     = new Font(bfb,  10, Font.NORMAL, COLOR_BLANCO);
        }

        // ─────────────────────────────────────────────────────────────────────
        public static void Generar(
            string rutaDestino,
            List<KardexItem> historial,
            string materialNombre,
            DateTime fechaInicio,
            DateTime fechaFin,
            bool mostrarColumnaMaterial,
            bool mostrarColumnaSaldo)
        {
            Document doc = new Document(PageSize.A4, 40, 40, 40, 55);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));
            writer.PageEvent = new PieKardex();
            doc.Open();

            // ─── ENCABEZADO ───────────────────────────────────────────────────
            AgregarEncabezado(doc, materialNombre, fechaInicio, fechaFin);

            // ─── TABLA DE MOVIMIENTOS ─────────────────────────────────────────
            AgregarTablaMovimientos(doc, historial, mostrarColumnaMaterial, mostrarColumnaSaldo);

            // ─── RESUMEN / PIE ────────────────────────────────────────────────
            AgregarResumen(doc, historial, mostrarColumnaSaldo);

            doc.Close();
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void AgregarEncabezado(Document doc, string materialNombre, DateTime inicio, DateTime fin)
        {
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 45f, 55f });

            // ── IZQUIERDA: logo + datos empresa ───────────────────────────────
            PdfPCell cellIzq = new PdfPCell { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };

            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string res = asm.GetManifestResourceNames().FirstOrDefault(n => n.ToLower().EndsWith("wp_horizontal.png"));
                if (res != null)
                {
                    using (var stream = asm.GetManifestResourceStream(res))
                    {
                        if (stream != null)
                        {
                            byte[] buf = new byte[stream.Length];
                            stream.Read(buf, 0, buf.Length);
                            Image logo = Image.GetInstance(buf);
                            logo.ScaleToFit(150, 62);
                            logo.Alignment = Element.ALIGN_LEFT;
                            cellIzq.AddElement(logo);
                        }
                    }
                }
            }
            catch { }

            cellIzq.AddElement(new Paragraph("Av. Tecnología 1000 - Parque Tecnológico", fntDir) { SpacingBefore = 8 });
            cellIzq.AddElement(new Paragraph("CUIT: 30-75001234-9   •   Ingresos Brutos: 30-75001234-9", fntTributo) { SpacingBefore = 2 });
            cellIzq.AddElement(new Paragraph("SteeLog - Logística y Stock", fntTributo) { SpacingBefore = 1 });
            headerTable.AddCell(cellIzq);

            // ── DERECHA: identificación del reporte ───────────────────────────
            PdfPCell cellDer = new PdfPCell { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
            cellDer.AddElement(new Paragraph("KARDEX DE MATERIALES", fntTitulo)     { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
            cellDer.AddElement(new Paragraph(materialNombre,          fntSubtitulo) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });
            cellDer.AddElement(new Paragraph(
                $"Período: {inicio:dd/MM/yyyy}  →  {fin:dd/MM/yyyy}",
                new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, COLOR_TEXTO))
                { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 2 });
            cellDer.AddElement(new Paragraph(
                $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, new BaseColor(120, 120, 120)))
                { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 2 });
            headerTable.AddCell(cellDer);

            doc.Add(headerTable);

            // ── Línea separadora accent ───────────────────────────────────────
            PdfPTable sep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 10, SpacingAfter = 15 };
            sep.AddCell(new PdfPCell { Border = Rectangle.BOTTOM_BORDER, BorderColor = COLOR_VERDE_ACCENT, BorderWidth = 2, FixedHeight = 3 });
            doc.Add(sep);
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void AgregarTablaMovimientos(Document doc, List<KardexItem> historial, bool mostrarMaterial, bool mostrarSaldo)
        {
            int cols = (mostrarMaterial ? 6 : 5) + (mostrarSaldo ? 1 : 0);
            PdfPTable tabla = new PdfPTable(cols);
            tabla.WidthPercentage = 100;
            tabla.SpacingAfter = 15;

            if (mostrarMaterial && mostrarSaldo)
                tabla.SetWidths(new float[] { 10f, 22f, 15f, 10f, 14f, 14f, 15f });
            else if (mostrarMaterial)
                tabla.SetWidths(new float[] { 11f, 25f, 18f, 12f, 17f, 17f });
            else if (mostrarSaldo)
                tabla.SetWidths(new float[] { 12f, 22f, 12f, 16f, 16f, 22f });
            else
                tabla.SetWidths(new float[] { 14f, 26f, 14f, 23f, 23f });

            // Cabecera de sección
            PdfPCell cSec = new PdfPCell(new Phrase("DETALLE DE MOVIMIENTOS", fntSeccion))
            {
                BackgroundColor = COLOR_VERDE_ACCENT,
                Border = Rectangle.NO_BORDER,
                Padding = 6,
                Colspan = cols
            };
            tabla.AddCell(cSec);

            // Headers de columna
            string[] headers = mostrarMaterial
                ? (mostrarSaldo
                    ? new string[] { "Fecha", "Material", "Movimiento", "Cant.", "Origen", "Destino", "Stock Res." }
                    : new string[] { "Fecha", "Material", "Movimiento", "Cant.", "Origen", "Destino" })
                : (mostrarSaldo
                    ? new string[] { "Fecha", "Movimiento", "Cant.", "Origen", "Destino", "Stock Res." }
                    : new string[] { "Fecha", "Movimiento", "Cant.", "Origen", "Destino" });

            foreach (string h in headers)
            {
                tabla.AddCell(new PdfPCell(new Phrase(h, fntHeaderTabla))
                {
                    BackgroundColor      = COLOR_VERDE_HEADER,
                    HorizontalAlignment  = Element.ALIGN_CENTER,
                    VerticalAlignment    = Element.ALIGN_MIDDLE,
                    Padding              = 6,
                    BorderColor          = COLOR_BLANCO
                });
            }

            var culture = new System.Globalization.CultureInfo("es-AR");
            bool alt = false;

            foreach (var item in historial)
            {
                BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;

                tabla.AddCell(FilaCelda(item.fecha.ToString("dd/MM/yyyy"), Element.ALIGN_CENTER, bg, fntCelda));

                if (mostrarMaterial)
                    tabla.AddCell(FilaCelda(item.nombre_material ?? "-", Element.ALIGN_LEFT, bg, fntCelda));

                tabla.AddCell(FilaCelda(item.tipo_movimiento, Element.ALIGN_LEFT, bg, fntCelda));

                // Cantidad: positivo en verde, negativo en rojo
                BaseColor colorCant = item.cantidad < 0
                    ? new BaseColor(180, 60, 60)
                    : new BaseColor(39, 120, 70);
                var fntCant = new Font(
                    BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED),
                    9, Font.NORMAL, colorCant);
                tabla.AddCell(FilaCelda(item.cantidad.ToString("G29"), Element.ALIGN_CENTER, bg, fntCant));

                tabla.AddCell(FilaCelda(item.origen  ?? "-", Element.ALIGN_LEFT, bg, fntCelda));
                tabla.AddCell(FilaCelda(item.destino ?? "-", Element.ALIGN_LEFT, bg, fntCelda));
                if (mostrarSaldo)
                    tabla.AddCell(FilaCelda(item.saldo.ToString("N2", culture), Element.ALIGN_CENTER, bg, fntCeldaBold));

                alt = !alt;
            }

            doc.Add(tabla);
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void AgregarResumen(Document doc, List<KardexItem> historial, bool mostrarSaldo)
        {
            if (historial.Count == 0) return;

            var culture = new System.Globalization.CultureInfo("es-AR");

            decimal totalEntradas = historial.Where(h => h.cantidad > 0).Sum(h => h.cantidad);
            decimal totalSalidas  = historial.Where(h => h.cantidad < 0).Sum(h => h.cantidad);
            decimal stockFinal = historial.FirstOrDefault()?.saldo ?? 0;  // descending order — primero = más reciente

            PdfPTable tRes = new PdfPTable(mostrarSaldo ? 3 : 2);
            tRes.WidthPercentage = 60;
            tRes.HorizontalAlignment = Element.ALIGN_RIGHT;
            tRes.SetWidths(mostrarSaldo ? new float[] { 40f, 30f, 30f } : new float[] { 50f, 50f });
            tRes.SpacingBefore = 8;

            // Header
            PdfPCell hRes = new PdfPCell(new Phrase("RESUMEN DEL PERÍODO", fntSeccion))
            {
                BackgroundColor = COLOR_VERDE_ACCENT,
                Border = Rectangle.NO_BORDER,
                Padding = 5,
                Colspan = 3
            };
            tRes.AddCell(hRes);

            // Valores
            AgCeldaRes(tRes, "Total Entradas:",  "+" + totalEntradas.ToString("G29"), new BaseColor(39, 120, 70));
            AgCeldaRes(tRes, "Total Salidas:",   totalSalidas.ToString("G29"), new BaseColor(180, 60, 60));
            if (mostrarSaldo)
                AgCeldaRes(tRes, "Stock Actual:", stockFinal.ToString("N2", culture), COLOR_VERDE_OSCURO);

            doc.Add(tRes);
        }

        // ─────────────────────────────────────────────────────────────────────
        private static PdfPCell FilaCelda(string texto, int align, BaseColor bg, Font font)
        {
            return new PdfPCell(new Phrase(texto, font))
            {
                BackgroundColor     = bg,
                HorizontalAlignment = align,
                VerticalAlignment   = Element.ALIGN_MIDDLE,
                Padding             = 5,
                BorderColor         = COLOR_GRIS_CLARO
            };
        }

        private static void AgCeldaRes(PdfPTable t, string label, string valor, BaseColor colorValor)
        {
            t.AddCell(new PdfPCell(new Phrase(label, fntEtiqueta))
            {
                BackgroundColor = COLOR_FONDO_FILA,
                BorderColor     = COLOR_GRIS_CLARO,
                Padding         = 6,
                Colspan         = 2,
                HorizontalAlignment = Element.ALIGN_RIGHT
            });
            var fnt = new Font(
                BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED),
                10, Font.NORMAL, colorValor);
            t.AddCell(new PdfPCell(new Phrase(valor, fnt))
            {
                BackgroundColor     = COLOR_BLANCO,
                BorderColor         = COLOR_GRIS_CLARO,
                Padding             = 6,
                HorizontalAlignment = Element.ALIGN_CENTER
            });
        }
    }

    // ── PIE DE PÁGINA ─────────────────────────────────────────────────────────
    public class PieKardex : PdfPageEventHelper
    {
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            cb.SetColorStroke(new BaseColor(78, 91, 63));
            cb.SetLineWidth(1f);
            cb.MoveTo(document.LeftMargin, document.Bottom - 15);
            cb.LineTo(document.PageSize.Width - document.RightMargin, document.Bottom - 15);
            cb.Stroke();

            cb.BeginText();
            cb.SetFontAndSize(bf, 8);
            cb.SetColorFill(new BaseColor(120, 120, 120));
            cb.ShowTextAligned(Element.ALIGN_LEFT,
                "SteeLog - Sistema de Gestión de Inventario — Kardex generado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                document.LeftMargin, document.Bottom - 28, 0);
            cb.ShowTextAligned(Element.ALIGN_RIGHT,
                "Pág. " + writer.PageNumber,
                document.PageSize.Width - document.RightMargin, document.Bottom - 28, 0);
            cb.EndText();
        }
    }
}
