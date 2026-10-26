using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Reportes
{
    public class ConteoInventarioPDF
    {
        private static readonly BaseColor COLOR_VERDE_OSCURO  = new BaseColor(25,  50,  35);
        private static readonly BaseColor COLOR_VERDE_HEADER  = new BaseColor(35,  63,  43);
        private static readonly BaseColor COLOR_VERDE_ACCENT  = new BaseColor(78,  91,  63);
        private static readonly BaseColor COLOR_FONDO_SECCION = new BaseColor(240, 247, 244);
        private static readonly BaseColor COLOR_FONDO_FILA    = new BaseColor(245, 248, 246);
        private static readonly BaseColor COLOR_BLANCO        = BaseColor.WHITE;
        private static readonly BaseColor COLOR_GRIS_CLARO    = new BaseColor(220, 220, 220);
        private static readonly BaseColor COLOR_TEXTO         = new BaseColor(33,  37,  41);
        private static readonly BaseColor COLOR_SOBRANTE      = new BaseColor(255, 168, 0);
        private static readonly BaseColor COLOR_FALTANTE      = new BaseColor(192, 57,  43);
        private static readonly BaseColor COLOR_OK            = new BaseColor(39,  120, 70);
        private static readonly BaseColor COLOR_SEPARADOR     = new BaseColor(24,  74,  53);

        private static Font fntTitulo;
        private static Font fntSubtitulo;
        private static Font fntSeccion;
        private static Font fntHeaderTabla;
        private static Font fntCelda;
        private static Font fntCeldaBold;
        private static Font fntDato;
        private static Font fntEtiqueta;
        private static Font fntDir;
        private static Font fntTributo;
        private static Font fntPie;

        static ConteoInventarioPDF()
        {
            var bfn = BaseFont.CreateFont(BaseFont.HELVETICA,      BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            var bfb = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            fntTitulo      = new Font(bfb, 15, Font.NORMAL, COLOR_VERDE_OSCURO);
            fntSubtitulo   = new Font(bfb, 10, Font.NORMAL, new BaseColor(80, 80, 80));
            fntSeccion     = new Font(bfb, 10, Font.NORMAL, COLOR_BLANCO);
            fntHeaderTabla = new Font(bfb,  9, Font.NORMAL, COLOR_BLANCO);
            fntCelda       = new Font(bfn,  9, Font.NORMAL, COLOR_TEXTO);
            fntCeldaBold   = new Font(bfb,  9, Font.NORMAL, COLOR_TEXTO);
            fntDato        = new Font(bfn, 10, Font.NORMAL, COLOR_TEXTO);
            fntEtiqueta    = new Font(bfb, 10, Font.NORMAL, new BaseColor(80, 80, 80));
            fntDir         = new Font(bfn,  8, Font.NORMAL, new BaseColor(80, 80, 80));
            fntTributo     = new Font(bfn,  7, Font.NORMAL, new BaseColor(80, 80, 80));
            fntPie         = new Font(bfn,  8, Font.NORMAL, new BaseColor(120, 120, 120));
        }

        public static string SugerirNombreArchivo(ConteoInventario conteo)
        {
            if (conteo == null) return $"Conteo_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return $"Conteo_{conteo.id_conteo}_{conteo.estado}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        }

        public static void Generar(
            string rutaDestino,
            ConteoInventario conteo,
            List<ConteoInventarioDiferencia> detalle)
        {
            if (conteo == null) throw new ArgumentNullException(nameof(conteo));
            detalle = detalle ?? new List<ConteoInventarioDiferencia>();

            bool esAdmin = SessionManager.Role == UserRole.Admin;
            bool finalizado = string.Equals(conteo.estado, "finalizado", StringComparison.OrdinalIgnoreCase);

            Document doc = new Document(PageSize.A4.Rotate(), 35, 35, 40, 55);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));
            writer.PageEvent = new PieConteo();
            doc.Open();

            PdfAction action = PdfAction.JavaScript("this.print(true);", writer);
            writer.AddJavaScript(action);

            AgregarEncabezado(doc, conteo, esAdmin, finalizado);

            var mapeoSecciones = new ReporteStockDatos().ObtenerMapeoSecciones();
            AgregarTablaDetalle(doc, detalle, esAdmin, finalizado, mapeoSecciones);

            AgregarResumen(doc, detalle, esAdmin, finalizado);
            AgregarObservaciones(doc, conteo);

            doc.Close();
        }

        private static void AgregarEncabezado(Document doc, ConteoInventario conteo, bool esAdmin, bool finalizado)
        {
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 45f, 55f });

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

            PdfPCell cellDer = new PdfPCell { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
            cellDer.AddElement(new Paragraph("CONTEO FÍSICO DE INVENTARIO", fntTitulo) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
            cellDer.AddElement(new Paragraph($"Comprobante de conteo N° {conteo.id_conteo.ToString("D6")}", fntSubtitulo) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });

            string estadoLabel = (conteo.estado ?? "-").ToUpper();
            cellDer.AddElement(new Paragraph(
                $"Estado: {estadoLabel}",
                new Font(Font.FontFamily.HELVETICA, 11, Font.BOLD, COLOR_TEXTO))
                { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });

            cellDer.AddElement(new Paragraph(
                $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
                new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, new BaseColor(120, 120, 120)))
                { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 2 });
            headerTable.AddCell(cellDer);

            doc.Add(headerTable);

            PdfPTable sep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 8, SpacingAfter = 10 };
            sep.AddCell(new PdfPCell { Border = Rectangle.BOTTOM_BORDER, BorderColor = COLOR_VERDE_ACCENT, BorderWidth = 2, FixedHeight = 3 });
            doc.Add(sep);
        }

        private static void AgregarTablaDetalle(Document doc, List<ConteoInventarioDiferencia> detalle, bool esAdmin, bool finalizado, Dictionary<int, ReporteStockMapeo> mapeoSecciones)
        {
            int cols = esAdmin && finalizado ? 7 : 4;
            PdfPTable tabla = new PdfPTable(cols);
            tabla.WidthPercentage = 100;
            tabla.SpacingAfter = 12;

            if (esAdmin && finalizado)
                tabla.SetWidths(new float[] { 6f, 35f, 17f, 12f, 12f, 10f, 8f });
            else
                tabla.SetWidths(new float[] { 8f, 52f, 22f, 18f });

            PdfPCell cSec = new PdfPCell(new Phrase(finalizado ? "DETALLE DE DIFERENCIAS" : "DETALLE DE MATERIALES CARGADOS", fntSeccion))
            {
                BackgroundColor = COLOR_VERDE_ACCENT,
                Border = Rectangle.NO_BORDER,
                Padding = 6,
                Colspan = cols
            };
            tabla.AddCell(cSec);

            AgregarHeader(tabla, "#");
            AgregarHeader(tabla, "Material");
            AgregarHeader(tabla, "Categoría");
            AgregarHeader(tabla, "Contado");
            if (esAdmin && finalizado)
            {
                AgregarHeader(tabla, "Stock sistema");
                AgregarHeader(tabla, "Diferencia");
                AgregarHeader(tabla, "Estado");
            }
            else
            {
                AgregarHeader(tabla, "Hora carga");
            }

            var culture = new System.Globalization.CultureInfo("es-AR");

            var grupos = ConstruirGruposPorSeccion(detalle, mapeoSecciones);

            int index = 1;
            foreach (var grupo in grupos)
            {
                if (!string.IsNullOrEmpty(grupo.nombreSeccion))
                {
                    tabla.AddCell(new PdfPCell(new Phrase(grupo.nombreSeccion.ToUpperInvariant(), fntSeccion))
                    {
                        BackgroundColor = COLOR_SEPARADOR,
                        Border = Rectangle.NO_BORDER,
                        Padding = 7,
                        Colspan = cols,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    });
                }

                bool alt = false;
                foreach (var d in grupo.detalles)
                {
                    BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;
                    AgregarDato(tabla, index.ToString(), bg, Element.ALIGN_CENTER);
                    AgregarDato(tabla, d.material_nombre_snapshot ?? "-", bg, Element.ALIGN_LEFT);
                    AgregarDato(tabla, string.IsNullOrWhiteSpace(d.material_categoria_snapshot) ? "-" : d.material_categoria_snapshot, bg, Element.ALIGN_LEFT);
                    AgregarDato(tabla, d.cantidad_contada.ToString("N2", culture), bg, Element.ALIGN_CENTER, fntCeldaBold);

                    if (esAdmin && finalizado)
                    {
                        AgregarDato(tabla, d.stock_sistema.HasValue ? d.stock_sistema.Value.ToString("N2", culture) : "-", bg, Element.ALIGN_CENTER);

                        string difTexto = "-";
                        BaseColor difColor = COLOR_TEXTO;
                        if (d.diferencia.HasValue)
                        {
                            decimal dif = d.diferencia.Value;
                            difTexto = (dif > 0 ? "+" : "") + dif.ToString("N2", culture);
                            if (dif > 0) difColor = COLOR_SOBRANTE;
                            else if (dif < 0) difColor = COLOR_FALTANTE;
                            else difColor = COLOR_OK;
                        }
                        var fntDif = new Font(
                            BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED),
                            9, Font.NORMAL, difColor);
                        AgregarDato(tabla, difTexto, bg, Element.ALIGN_CENTER, fntDif);

                        string estado = (d.estado_diferencia ?? "Sin diferencia").ToUpper();
                        BaseColor estadoColor = estado == "SOBRANTE" ? COLOR_SOBRANTE
                                              : estado == "FALTANTE" ? COLOR_FALTANTE
                                              : COLOR_OK;
                        var fntEst = new Font(
                            BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED),
                            8, Font.NORMAL, estadoColor);
                        AgregarDato(tabla, estado, bg, Element.ALIGN_CENTER, fntEst);
                    }
                    else
                    {
                        string hora = d.fecha_carga != default(DateTime) ? d.fecha_carga.ToString("dd/MM/yyyy HH:mm") : "-";
                        AgregarDato(tabla, hora, bg, Element.ALIGN_CENTER);
                    }

                    alt = !alt;
                    index++;
                }
            }

            if (detalle.Count == 0)
            {
                PdfPCell cellVacio = new PdfPCell(new Phrase("Sin registros para mostrar.", fntCelda))
                {
                    BackgroundColor = COLOR_BLANCO,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 12,
                    Colspan = cols,
                    BorderColor = COLOR_GRIS_CLARO
                };
                tabla.AddCell(cellVacio);
            }

            doc.Add(tabla);
        }

        private class GrupoSeccion
        {
            public string nombreSeccion;
            public List<ConteoInventarioDiferencia> detalles;
        }

        private static List<GrupoSeccion> ConstruirGruposPorSeccion(List<ConteoInventarioDiferencia> detalle, Dictionary<int, ReporteStockMapeo> mapeoSecciones)
        {
            var grupos = new List<GrupoSeccion>();
            if (detalle == null || detalle.Count == 0) return grupos;

            mapeoSecciones = mapeoSecciones ?? new Dictionary<int, ReporteStockMapeo>();

            var porSeccion = new Dictionary<string, GrupoSeccion>(StringComparer.Ordinal);
            var ordenSeccion = new Dictionary<string, int>(StringComparer.Ordinal);
            var sinSeccion = new GrupoSeccion { nombreSeccion = null, detalles = new List<ConteoInventarioDiferencia>() };

            foreach (var d in detalle)
            {
                if (mapeoSecciones.TryGetValue(d.id_material, out var mapeo))
                {
                    if (!porSeccion.ContainsKey(mapeo.nombreSeccion))
                    {
                        porSeccion[mapeo.nombreSeccion] = new GrupoSeccion { nombreSeccion = mapeo.nombreSeccion, detalles = new List<ConteoInventarioDiferencia>() };
                        ordenSeccion[mapeo.nombreSeccion] = mapeo.ordenSeccion;
                    }
                    porSeccion[mapeo.nombreSeccion].detalles.Add(d);
                }
                else
                {
                    sinSeccion.detalles.Add(d);
                }
            }

            foreach (var kv in porSeccion.OrderBy(g => ordenSeccion[g.Key]).ThenBy(g => g.Key))
                grupos.Add(kv.Value);

            if (sinSeccion.detalles.Count > 0)
                grupos.Add(sinSeccion);

            return grupos;
        }

        private static void AgregarHeader(PdfPTable tabla, string texto)
        {
            tabla.AddCell(new PdfPCell(new Phrase(texto, fntHeaderTabla))
            {
                BackgroundColor = COLOR_VERDE_HEADER,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 6,
                BorderColor = COLOR_BLANCO
            });
        }

        private static void AgregarDato(PdfPTable tabla, string texto, BaseColor bg, int align, Font font = null)
        {
            tabla.AddCell(new PdfPCell(new Phrase(texto ?? "-", font ?? fntCelda))
            {
                BackgroundColor = bg,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5,
                BorderColor = COLOR_GRIS_CLARO
            });
        }

        private static void AgregarResumen(Document doc, List<ConteoInventarioDiferencia> detalle, bool esAdmin, bool finalizado)
        {
            if (detalle.Count == 0) return;
            if (!esAdmin || !finalizado) return;

            int totalSobrantes = 0;
            int totalFaltantes = 0;
            int totalSinDif = 0;
            decimal totalContado = 0m;
            decimal totalSistema = 0m;
            decimal totalDif = 0m;

            foreach (var d in detalle)
            {
                totalContado += d.cantidad_contada;
                if (d.stock_sistema.HasValue) totalSistema += d.stock_sistema.Value;
                if (d.diferencia.HasValue)
                {
                    decimal dif = d.diferencia.Value;
                    totalDif += dif;
                    if (dif > 0) totalSobrantes++;
                    else if (dif < 0) totalFaltantes++;
                    else totalSinDif++;
                }
            }

            var culture = new System.Globalization.CultureInfo("es-AR");

            PdfPTable tRes = new PdfPTable(2);
            tRes.WidthPercentage = 60;
            tRes.HorizontalAlignment = Element.ALIGN_RIGHT;
            tRes.SetWidths(new float[] { 60f, 40f });
            tRes.SpacingBefore = 6;

            PdfPCell hRes = new PdfPCell(new Phrase("RESUMEN DE DIFERENCIAS", fntSeccion))
            {
                BackgroundColor = COLOR_VERDE_ACCENT,
                Border = Rectangle.NO_BORDER,
                Padding = 5,
                Colspan = 2
            };
            tRes.AddCell(hRes);

            AgCeldaRes(tRes, "Total contado:", totalContado.ToString("N2", culture), COLOR_TEXTO);
            AgCeldaRes(tRes, "Total stock sistema:", totalSistema.ToString("N2", culture), COLOR_TEXTO);
            AgCeldaRes(tRes, "Diferencia neta:", (totalDif > 0 ? "+" : "") + totalDif.ToString("N2", culture), totalDif > 0 ? COLOR_SOBRANTE : (totalDif < 0 ? COLOR_FALTANTE : COLOR_OK));
            AgCeldaRes(tRes, "Sobrantes:", totalSobrantes.ToString(), COLOR_SOBRANTE);
            AgCeldaRes(tRes, "Faltantes:", totalFaltantes.ToString(), COLOR_FALTANTE);
            AgCeldaRes(tRes, "Sin diferencia:", totalSinDif.ToString(), COLOR_OK);

            doc.Add(tRes);
        }

        private static void AgCeldaRes(PdfPTable t, string label, string valor, BaseColor colorValor)
        {
            t.AddCell(new PdfPCell(new Phrase(label, fntEtiqueta))
            {
                BackgroundColor = COLOR_FONDO_FILA,
                BorderColor = COLOR_GRIS_CLARO,
                Padding = 6,
                HorizontalAlignment = Element.ALIGN_RIGHT
            });

            var fnt = new Font(
                BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED),
                10, Font.NORMAL, colorValor);

            t.AddCell(new PdfPCell(new Phrase(valor, fnt))
            {
                BackgroundColor = COLOR_BLANCO,
                BorderColor = COLOR_GRIS_CLARO,
                Padding = 6,
                HorizontalAlignment = Element.ALIGN_CENTER
            });
        }

        private static void AgregarObservaciones(Document doc, ConteoInventario conteo)
        {
            if (string.IsNullOrWhiteSpace(conteo.observaciones)) return;

            doc.Add(new Paragraph(" ") { SpacingBefore = 4 });

            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 6;
            tituloTabla.SpacingAfter = 4;

            PdfPCell tituloCell = new PdfPCell(new Phrase("OBSERVACIONES", fntSeccion))
            {
                BackgroundColor = COLOR_FONDO_SECCION,
                Border = Rectangle.LEFT_BORDER,
                BorderColorLeft = COLOR_VERDE_ACCENT,
                BorderWidthLeft = 4f,
                Padding = 6,
                PaddingLeft = 10,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(1);
            tabla.WidthPercentage = 100;
            tabla.SpacingAfter = 6;

            PdfPCell cellVal = new PdfPCell(new Phrase(conteo.observaciones, fntCelda))
            {
                BackgroundColor = COLOR_BLANCO,
                BorderColor = COLOR_GRIS_CLARO,
                Padding = 8,
                MinimumHeight = 30
            };
            tabla.AddCell(cellVal);
            doc.Add(tabla);
        }
    }

    public class PieConteo : PdfPageEventHelper
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
                "SteeLog - Sistema de Gestión de Inventario — Conteo físico generado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                document.LeftMargin, document.Bottom - 28, 0);
            cb.ShowTextAligned(Element.ALIGN_RIGHT,
                "Pág. " + writer.PageNumber,
                document.PageSize.Width - document.RightMargin, document.Bottom - 28, 0);
            cb.EndText();
        }
    }
}
