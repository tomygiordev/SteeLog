using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;

namespace WP_Inventario.Reportes
{
    public class OrdenesPendientesPDF
    {
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

        public void Generar(string rutaDestino)
        {
            var datos = new OrdenCompraDatos();
            var items = datos.ObtenerReporteOrdenesPendientes();

            if (items.Count == 0) throw new Exception("No hay órdenes pendientes para imprimir.");

            Document doc = new Document(PageSize.A4, 40, 40, 40, 60);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));
            
            // Footer
            writer.PageEvent = new PieMovimiento();

            doc.Open();

            // Auto open print dialog when opened in viewer
            PdfAction action = PdfAction.JavaScript("this.print(true);", writer);
            writer.AddJavaScript(action);

            // =================== ENCABEZADO ===================
            AgregarEncabezado(doc);

            // =================== TABLA DE ÓRDENES ===================
            AgregarTablaOrdenes(doc, items);

            doc.Close();
        }

        public string SugerirNombreArchivo()
        {
            return $"Reporte_Ordenes_Pendientes_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }

        private void AgregarEncabezado(Document doc)
        {
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 45f, 55f });

            // ======= IZQUIERDA: LOGO Y DATOS =======
            PdfPCell cellIzq = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };

            try {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.ToLower().EndsWith("wp_horizontal.png"));
                if (resourceName != null) {
                    using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                        if (stream != null) {
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            Image logo = Image.GetInstance(buffer);
                            logo.ScaleToFit(150, 62);
                            logo.Alignment = Element.ALIGN_LEFT;
                            cellIzq.AddElement(logo);
                        }
                    }
                }
            } catch { }

            var fntDir = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, new BaseColor(80, 80, 80));
            var fntTributo = new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, new BaseColor(80, 80, 80));

            Paragraph pDireccion = new Paragraph("Av. Tecnología 1000 - Parque Tecnológico", fntDir) { SpacingBefore = 8, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pDireccion);

            string separadorTributos = "   \u2022   ";
            Paragraph pTributos = new Paragraph($"CUIT: 30-75001234-9{separadorTributos}Ingresos Brutos: 30-75001234-9", fntTributo) { SpacingBefore = 2, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pTributos);

            Paragraph pInicio = new Paragraph("SteeLog - Logística y Stock", fntTributo) { SpacingBefore = 1, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pInicio);

            headerTable.AddCell(cellIzq);

            // ======= DERECHA: TITULO =======
            PdfPCell cellDer = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
            
            cellDer.AddElement(new Paragraph("ÓRDENES PENDIENTES", fntTituloPrincipal) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
            cellDer.AddElement(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, COLOR_TEXTO)) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 2 });

            headerTable.AddCell(cellDer);

            doc.Add(headerTable);

            // Línea accent
            PdfPTable lineaSep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 10, SpacingAfter = 15 };
            lineaSep.AddCell(new PdfPCell { Border = Rectangle.BOTTOM_BORDER, BorderColor = COLOR_VERDE_ACCENT, BorderWidth = 2, FixedHeight = 3 });
            doc.Add(lineaSep);
        }

        private void AgregarTablaOrdenes(Document doc, List<ReporteOrdenPendienteItem> items)
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
                pOrden.Add(new Chunk(item.nro_orden + "\n", fntCeldaBold));
                pOrden.Add(new Chunk(item.proveedor, new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, new BaseColor(80, 80, 80))));
                PdfPCell cOrden = new PdfPCell(pOrden) { BackgroundColor = bg, Padding = 6, BorderColor = COLOR_GRIS_CLARO, VerticalAlignment = Element.ALIGN_MIDDLE };
                tabla.AddCell(cOrden);

                // Material
                Phrase pMat = new Phrase(item.material, fntCelda);
                PdfPCell cMat = new PdfPCell(pMat) { BackgroundColor = bg, Padding = 6, BorderColor = COLOR_GRIS_CLARO, VerticalAlignment = Element.ALIGN_MIDDLE };
                tabla.AddCell(cMat);

                // Pedido
                tabla.AddCell(new PdfPCell(new Phrase(item.cantidad_pedida.ToString(), fntCelda)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE, BorderColor = COLOR_GRIS_CLARO });

                // Recibido
                tabla.AddCell(new PdfPCell(new Phrase(item.cantidad_recibida.ToString(), fntCelda)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE, BorderColor = COLOR_GRIS_CLARO });

                // Faltante
                tabla.AddCell(new PdfPCell(new Phrase(item.cantidad_faltante.ToString(), fntCeldaBold)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE, BorderColor = COLOR_GRIS_CLARO });

                alt = !alt;
            }

            doc.Add(tabla);
        }
    }
}
