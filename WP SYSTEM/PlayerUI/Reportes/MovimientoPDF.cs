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
    public class MovimientoPDF
    {
        // Colores corporativos (mismos que InformeObraPDF)
        private static readonly BaseColor COLOR_VERDE_OSCURO = new BaseColor(25, 50, 35);
        private static readonly BaseColor COLOR_VERDE_HEADER = new BaseColor(35, 63, 43);
        private static readonly BaseColor COLOR_VERDE_ACCENT = new BaseColor(78, 91, 63);  // #4E5B3F
        private static readonly BaseColor COLOR_FONDO_FILA = new BaseColor(245, 248, 246);
        private static readonly BaseColor COLOR_BLANCO = BaseColor.WHITE;
        private static readonly BaseColor COLOR_GRIS_CLARO = new BaseColor(230, 230, 230);
        private static readonly BaseColor COLOR_TEXTO = new BaseColor(33, 37, 41);
        private static readonly BaseColor COLOR_ETIQUETA_RECORTE = new BaseColor(230, 126, 34);

        // Fuentes
        private static Font fntTituloPrincipal;
        private static Font fntSeccion;
        private static Font fntHeaderTabla;
        private static Font fntCelda;
        private static Font fntCeldaBold;
        private static Font fntDato;
        private static Font fntEtiqueta;
        private static Font fntPie;

        static MovimientoPDF()
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
            fntPie = new Font(baseFontNormal, 8, Font.NORMAL, new BaseColor(120, 120, 120));
        }

        public void Generar(string rutaDestino, int idMovimiento)
        {
            var datos = new InformeObraDatos();
            var items = datos.ObtenerDetalleMovimiento(idMovimiento);
            
            if (items.Count == 0) return;

            var primerItem = items[0];

            Document doc = new Document(PageSize.A4, 40, 40, 40, 60);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));
            
            // Footer
            writer.PageEvent = new PieMovimiento();

            doc.Open();

            // Auto open print dialog when opened in viewer
            PdfAction action = PdfAction.JavaScript("this.print(true);", writer);
            writer.AddJavaScript(action);

            for (int i = 0; i < 3; i++)
            {
                if (i > 0) doc.NewPage();

                // =================== ENCABEZADO ===================
                AgregarEncabezado(doc, primerItem);

                // =================== DATOS PRINCIPALES ===================
                AgregarBloqueInfo(doc, primerItem);

                // =================== TABLA DE MATERIALES ===================
                AgregarTablaMateriales(doc, items, primerItem.costoflete);

                // =================== OBSERVACIONES & FIRMA ===================
                AgregarSeccionFirma(doc, primerItem);
            }

            doc.Close();
        }

        public string SugerirNombreArchivo(int idMovimiento)
        {
            var datos = new InformeObraDatos();
            var items = datos.ObtenerDetalleMovimiento(idMovimiento);
            if (items.Count == 0) return "Movimiento.pdf";

            var mov = items[0];
            string nro = !string.IsNullOrWhiteSpace(mov.nroremito) ? mov.nroremito : "SN";
            string tipo = mov.tipo_movimiento.Replace(" ", "_");
            return $"Comprobante_{tipo}_Remito_{nro}_{DateTime.Now:yyyyMMdd}.pdf";
        }

        private void AgregarEncabezado(Document doc, InformeMovimientoDetalle mov)
        {
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            // Modificamos a 45/55 para que los tributos respiren un poco más en la izquierda
            headerTable.SetWidths(new float[] { 45f, 55f });

            // ======= IZQUIERDA: LOGO Y DATOS =======
            // VerticalAlignment a TOP para que ambas columnas empiecen a la misma altura
            PdfPCell cellIzq = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };

            // Logo superpuesto (cargado de recurso embebido)
            try {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.ToLower().EndsWith("wp_horizontal.png"));
                if (resourceName != null) {
                    using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                        if (stream != null) {
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            Image logo = Image.GetInstance(buffer);
                            // Ajustamos el tamaño del logo a un punto medio (antes 120x50, original 230x95)
                            logo.ScaleToFit(150, 62);
                            logo.Alignment = Element.ALIGN_LEFT;
                            cellIzq.AddElement(logo);
                        }
                    }
                }
            } catch { }

            // Datos de la empresa perfectamente apilados debajo del logo
            var fntDir = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, new BaseColor(80, 80, 80));
            // Mismo color que la dirección, pero un punto más chico para diferenciar jerarquía
            var fntTributo = new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, new BaseColor(80, 80, 80));

            Paragraph pDireccion = new Paragraph("Av. Tecnología 1000 - Parque Tecnológico", fntDir) { SpacingBefore = 8, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pDireccion);

            // CUIT y ATER en la misma línea separados por un círculo rellenito
            string separadorTributos = "   \u2022   "; // Tres espacios de cada lado para armonía
            Paragraph pTributos = new Paragraph($"CUIT: 30-75001234-9{separadorTributos}Ingresos Brutos: 30-75001234-9", fntTributo) { SpacingBefore = 2, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pTributos);

            // Inicio Actividades en un nuevo renglón para que no se corte
            Paragraph pInicio = new Paragraph("SteeLog - Logística y Stock", fntTributo) { SpacingBefore = 1, Alignment = Element.ALIGN_LEFT };
            cellIzq.AddElement(pInicio);

            headerTable.AddCell(cellIzq);

            // ======= DERECHA: DATOS DE REMITO =======
            // Alineamos al tope quitando el PaddingTop para que coincida con el borde superior del logo
            PdfPCell cellDer = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
            
            string tituloRemito = "REMITO DE " + mov.tipo_movimiento.ToUpper();
            // Le saco el SpacingBefore extra que podría empujarlo
            cellDer.AddElement(new Paragraph(tituloRemito, fntTituloPrincipal) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
            
            string nro = !string.IsNullOrWhiteSpace(mov.nroremito) ? mov.nroremito.PadLeft(8, '0') : "--------";
            cellDer.AddElement(new Paragraph($"Nº 0001 - {nro}", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, COLOR_TEXTO)) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });
            cellDer.AddElement(new Paragraph($"Fecha: {mov.fecha:dd/MM/yyyy HH:mm}", new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, COLOR_TEXTO)) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 2 });

            headerTable.AddCell(cellDer);

            doc.Add(headerTable);

            // Línea accent
            PdfPTable lineaSep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 10, SpacingAfter = 15 };
            lineaSep.AddCell(new PdfPCell { Border = Rectangle.BOTTOM_BORDER, BorderColor = COLOR_VERDE_ACCENT, BorderWidth = 2, FixedHeight = 3 });
            doc.Add(lineaSep);
        }

        private void AgregarBloqueInfo(Document doc, InformeMovimientoDetalle mov)
        {
            PdfPTable infoTable = new PdfPTable(4);
            infoTable.WidthPercentage = 100;
            infoTable.SetWidths(new float[] { 15f, 35f, 15f, 35f });
            infoTable.SpacingAfter = 15;

            AgregarCeldaInfo(infoTable, "Origen:", mov.origen);
            AgregarCeldaInfo(infoTable, "Destino:", mov.destino);

            doc.Add(infoTable);
        }

        private void AgregarCeldaInfo(PdfPTable tabla, string label, string valor)
        {
            PdfPCell cellLabel = new PdfPCell(new Phrase(label, fntEtiqueta)) { BackgroundColor = COLOR_FONDO_FILA, BorderColor = COLOR_GRIS_CLARO, Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT };
            tabla.AddCell(cellLabel);
            PdfPCell cellValor = new PdfPCell(new Phrase(valor, fntDato)) { BackgroundColor = COLOR_BLANCO, BorderColor = COLOR_GRIS_CLARO, Padding = 8 };
            tabla.AddCell(cellValor);
        }

        private void AgregarTablaMateriales(Document doc, List<InformeMovimientoDetalle> items, decimal flete)
        {
            PdfPTable tabla = new PdfPTable(3);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 70f, 15f, 15f });

            // Headers
            string[] headers = { "MATERIAL / INSUMO", "CANTIDAD", "TIPO" };
            foreach (var h in headers) {
                PdfPCell cell = new PdfPCell(new Phrase(h, fntHeaderTabla)) { BackgroundColor = COLOR_VERDE_ACCENT, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, Border = Rectangle.NO_BORDER };
                tabla.AddCell(cell);
            }

            bool alt = false;
            foreach (var item in items) {
                BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;
                
                // Celda Material
                Phrase pMat = new Phrase();
                pMat.Add(new Chunk(item.material + " ", fntCelda));
                if (item.esrecorte) {
                    pMat.Add(new Chunk("[RECORTE" + (string.IsNullOrEmpty(item.medida_recorte) ? "" : ": " + item.medida_recorte) + "]", 
                             new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD, COLOR_ETIQUETA_RECORTE)));
                } else if (item.esinsumo) {
                    pMat.Add(new Chunk("[INSUMO]", new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD, new BaseColor(0, 188, 212))));
                } else if (item.es_compra_eventual) {
                    pMat.Add(new Chunk("[COMPRA EVENTUAL]", new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD, new BaseColor(230, 120, 0))));
                }
                PdfPCell cMat = new PdfPCell(pMat) { BackgroundColor = bg, Padding = 6, BorderColor = COLOR_GRIS_CLARO };
                tabla.AddCell(cMat);

                // Celda Cantidad
                tabla.AddCell(new PdfPCell(new Phrase(item.cantidad.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), fntCeldaBold)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, BorderColor = COLOR_GRIS_CLARO });

                // Celda Tipo
                string tipo = item.esrecorte ? "Recorte" : (item.esinsumo ? "Insumo" : (item.es_compra_eventual ? "Eventual" : "Estándar"));
                tabla.AddCell(new PdfPCell(new Phrase(tipo, fntCelda)) { BackgroundColor = bg, Padding = 6, HorizontalAlignment = Element.ALIGN_CENTER, BorderColor = COLOR_GRIS_CLARO });

                alt = !alt;
            }

            // Fila de Costo Flete (Integrada a la tabla para diseño profesional)
            if (flete > 0)
            {
                var culture = new System.Globalization.CultureInfo("es-AR");
                
                PdfPCell cellFleteLbl = new PdfPCell(new Phrase("FLETE:", fntEtiqueta)) 
                { 
                    BackgroundColor = COLOR_FONDO_FILA, 
                    BorderColor = COLOR_GRIS_CLARO, 
                    HorizontalAlignment = Element.ALIGN_RIGHT, 
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 6, 
                    Colspan = 2 
                };
                tabla.AddCell(cellFleteLbl);

                PdfPCell cellFleteVal = new PdfPCell(new Phrase(flete.ToString("C2", culture), fntCeldaBold)) 
                { 
                    BackgroundColor = COLOR_BLANCO, 
                    BorderColor = COLOR_GRIS_CLARO, 
                    HorizontalAlignment = Element.ALIGN_CENTER, 
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 6 
                };
                tabla.AddCell(cellFleteVal);
            }

            doc.Add(tabla);
        }

        private void AgregarSeccionFirma(Document doc, InformeMovimientoDetalle mov)
        {
            // Reducimos el espacio inicial
            doc.Add(new Paragraph(" ") { SpacingBefore = 2 });

            // 1. Observaciones - Más compacto
            PdfPTable tObs = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 6 };
            
            PdfPCell cObsHdr = new PdfPCell(new Phrase("Observaciones", fntSeccion)) { BackgroundColor = COLOR_VERDE_ACCENT, Padding = 4, Border = Rectangle.NO_BORDER };
            tObs.AddCell(cObsHdr);
            
            string textoObs = string.IsNullOrWhiteSpace(mov.observaciones) ? " " : mov.observaciones;
            // Reducimos MinimumHeight de 40 a 20
            PdfPCell cObsVal = new PdfPCell(new Phrase(textoObs, fntCelda)) { BorderColor = COLOR_GRIS_CLARO, Padding = 6, MinimumHeight = 20 };
            tObs.AddCell(cObsVal);
            
            doc.Add(tObs);

            // 2. Firmas - Reducimos de 70 a 45 para ganar espacio
            PdfPTable tFirma = new PdfPTable(2);
            tFirma.WidthPercentage = 100;
            tFirma.SetWidths(new float[] { 50f, 50f });
            tFirma.SpacingAfter = 6;

            PdfPCell cRecibi = new PdfPCell() { BorderColor = COLOR_GRIS_CLARO, Padding = 6, MinimumHeight = 45 };
            cRecibi.AddElement(new Paragraph("RECIBI CONFORME:", fntEtiqueta));
            tFirma.AddCell(cRecibi);

            PdfPCell cSello = new PdfPCell() { BorderColor = COLOR_GRIS_CLARO, Padding = 6, MinimumHeight = 45, VerticalAlignment = Element.ALIGN_BOTTOM };
            Paragraph pSello = new Paragraph("FIRMA Y SELLO", fntEtiqueta) { Alignment = Element.ALIGN_CENTER };
            cSello.AddElement(new Paragraph("\n")); // Menos espacio para firma
            cSello.AddElement(pSello);
            tFirma.AddCell(cSello);

            doc.Add(tFirma);

            // 3. Transportista - Más pequeño y directo
            PdfPTable tTrans = new PdfPTable(3);
            tTrans.WidthPercentage = 100;
            tTrans.SetWidths(new float[] { 40f, 30f, 30f });

            PdfPCell cTransHdr = new PdfPCell(new Phrase("DATOS DEL TRANSPORTISTA", fntSeccion)) { BackgroundColor = COLOR_VERDE_ACCENT, Padding = 4, Border = Rectangle.NO_BORDER, Colspan = 3 };
            tTrans.AddCell(cTransHdr);

            // Reducimos padding y quitamos saltos de línea innecesarios
            tTrans.AddCell(new PdfPCell(new Phrase("Nombre: ", fntEtiqueta)) { BorderColor = COLOR_GRIS_CLARO, Padding = 5, MinimumHeight = 25 });
            tTrans.AddCell(new PdfPCell(new Phrase("Fecha: ", fntEtiqueta)) { BorderColor = COLOR_GRIS_CLARO, Padding = 5, MinimumHeight = 25 });
            tTrans.AddCell(new PdfPCell(new Phrase("Hora: ", fntEtiqueta)) { BorderColor = COLOR_GRIS_CLARO, Padding = 5, MinimumHeight = 25 });

            doc.Add(tTrans);
        }
    }

    public class PieMovimiento : PdfPageEventHelper
    {
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            
            cb.SetColorStroke(new BaseColor(78, 91, 63));  // #4E5B3F
            cb.SetLineWidth(1f);
            cb.MoveTo(document.LeftMargin, document.Bottom - 15);
            cb.LineTo(document.PageSize.Width - document.RightMargin, document.Bottom - 15);
            cb.Stroke();

            cb.BeginText();
            cb.SetFontAndSize(bf, 8);
            cb.SetColorFill(new BaseColor(120, 120, 120));
            cb.ShowTextAligned(Element.ALIGN_LEFT, "SteeLog - Sistema de Gestión de Inventario — Generado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), document.LeftMargin, document.Bottom - 28, 0);
            cb.EndText();
        }
    }
}
