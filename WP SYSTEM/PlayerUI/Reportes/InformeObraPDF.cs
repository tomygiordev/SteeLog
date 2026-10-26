using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using WP_Inventario.Datos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Reportes
{
    public class InformeObraPDF
    {
        // Colores corporativos modernos
        private static readonly BaseColor COLOR_VERDE_OSCURO = new BaseColor(30, 64, 50); // Verde profundo sofisticado #1e4032
        private static readonly BaseColor COLOR_VERDE_HEADER = new BaseColor(40, 78, 62); // Verde para encabezados #284e3e
        private static readonly BaseColor COLOR_VERDE_ACCENT = new BaseColor(16, 185, 129); // Esmeralda vibrante #10b981
        private static readonly BaseColor COLOR_FONDO_SECCION = new BaseColor(240, 247, 244); // Fondo sección suave #f0f7f4
        private static readonly BaseColor COLOR_FONDO_FILA = new BaseColor(248, 250, 252); // Fondo alternado de fila sutil #f8fafc
        private static readonly BaseColor COLOR_BLANCO = BaseColor.WHITE;
        private static readonly BaseColor COLOR_GRIS_BORDE = new BaseColor(229, 231, 235); // Borde gris claro sutil #e5e7eb
        private static readonly BaseColor COLOR_TEXTO = new BaseColor(51, 65, 85); // Texto gris oscuro pizarra #334155
        private static readonly BaseColor COLOR_TEXTO_MUTED = new BaseColor(100, 116, 139); // Texto secundario #64748b
        private static readonly BaseColor COLOR_ETIQUETA_RECORTE = new BaseColor(249, 115, 22); // Naranja cálido #f97316

        // Fuentes
        private static Font fntTituloPrincipal;
        private static Font fntSubtitulo;
        private static Font fntSeccion;
        private static Font fntHeaderTabla;
        private static Font fntCelda;
        private static Font fntCeldaBold;
        private static Font fntDatoObra;
        private static Font fntEtiqueta;
        private static Font fntObservacion;
        private static Font fntPie;

        static InformeObraPDF()
        {
            var baseFontNormal = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            var baseFontBold = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            var baseFontItalic = BaseFont.CreateFont(BaseFont.HELVETICA_OBLIQUE, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            fntTituloPrincipal = new Font(baseFontBold, 22, Font.NORMAL, COLOR_VERDE_OSCURO);
            fntSubtitulo = new Font(baseFontNormal, 10, Font.NORMAL, COLOR_TEXTO_MUTED);
            fntSeccion = new Font(baseFontBold, 12, Font.NORMAL, COLOR_VERDE_OSCURO);
            fntHeaderTabla = new Font(baseFontBold, 9, Font.NORMAL, COLOR_BLANCO);
            fntCelda = new Font(baseFontNormal, 8.5f, Font.NORMAL, COLOR_TEXTO);
            fntCeldaBold = new Font(baseFontBold, 8.5f, Font.NORMAL, COLOR_TEXTO);
            fntDatoObra = new Font(baseFontNormal, 9.5f, Font.NORMAL, COLOR_TEXTO);
            fntEtiqueta = new Font(baseFontBold, 9.5f, Font.NORMAL, COLOR_VERDE_HEADER);
            fntObservacion = new Font(baseFontNormal, 8.5f, Font.NORMAL, COLOR_TEXTO);
            fntPie = new Font(baseFontItalic, 8, Font.NORMAL, COLOR_TEXTO_MUTED);
        }

        /// <summary>
        /// Genera el PDF del informe de obra y lo guarda en el path indicado.
        /// </summary>
        public void Generar(string rutaDestino, int idObra, string nombreObra, DateTime? desde = null, DateTime? hasta = null)
        {
            var datos = new InformeObraDatos();
            var movimientos = datos.ObtenerMovimientosObra(idObra);
            var herramientas = datos.ObtenerHerramientasObra(idObra, nombreObra);
            var insumos = datos.ObtenerInsumosObra(idObra, nombreObra);
            var canaletasPlegados = datos.ObtenerCanaletasPlegadosObra(idObra);
            bool mostrarValorizacion = SessionManager.Role != UserRole.Galpon ||
                (!string.IsNullOrEmpty(nombreObra) && nombreObra.IndexOf("galpon", StringComparison.OrdinalIgnoreCase) >= 0);

            if (desde.HasValue)
            {
                movimientos = movimientos.Where(m => m.fecha.Date >= desde.Value.Date).ToList();
                canaletasPlegados = canaletasPlegados.Where(c => c.fecha_creacion.Date >= desde.Value.Date).ToList();
            }
            if (hasta.HasValue)
            {
                movimientos = movimientos.Where(m => m.fecha.Date <= hasta.Value.Date).ToList();
                canaletasPlegados = canaletasPlegados.Where(c => c.fecha_creacion.Date <= hasta.Value.Date).ToList();
            }

            decimal totalCanaletasPendientes = canaletasPlegados
                .Where(c => !c.pagado)
                .Sum(c => c.precio);
            InformeObraResumen resumen = InformeObraDatos.CalcularResumen(movimientos, mostrarValorizacion, totalCanaletasPendientes);

            Document doc = new Document(PageSize.A4, 40, 40, 40, 60);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));

            // Footer con número de página
            writer.PageEvent = new PieInforme();

            doc.Open();

            // =================== ENCABEZADO ===================
            AgregarEncabezado(doc, nombreObra, resumen.CantRemitos, resumen.TotalPendiente, mostrarValorizacion, desde, hasta);


            // =================== DETALLE VALORIZADO POR TIPO ===================
            AgregarSeccionesPorTipoMovimiento(doc, movimientos, mostrarValorizacion);
            AgregarSeccionCanaletasPlegados(doc, canaletasPlegados, mostrarValorizacion);

            // =================== FLETES ===================
            if (mostrarValorizacion)
            {
                AgregarSeccionFletes(doc, movimientos);
            }

            // =================== HERRAMIENTAS E INSUMOS ===================
            if (herramientas.Count > 0 || insumos.Count > 0)
            {
                doc.Add(new Paragraph(" "));
                AgregarSeccionActivos(doc, herramientas, insumos);
            }

            // =================== OBSERVACIONES ===================
            AgregarSeccionObservaciones(doc, movimientos);

            doc.Close();
        }

        private void AgregarEncabezado(Document doc, string nombreObra, int cantRemitos, decimal totalPendiente, bool mostrarValorizacion, DateTime? desde = null, DateTime? hasta = null)
        {
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 50f, 50f });

            // ======= IZQUIERDA: LOGO Y DATOS DE EMPRESA =======
            PdfPCell cellIzq = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.ToLower().EndsWith("wp_horizontal.png"));
                if (resourceName != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            Image logo = Image.GetInstance(buffer);
                            logo.ScaleToFit(140, 58);
                            logo.Alignment = Element.ALIGN_LEFT;
                            cellIzq.AddElement(logo);
                        }
                    }
                }
            }
            catch { /* Silencioso si falla el logo */ }

            var fntDir    = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, COLOR_TEXTO_MUTED);
            var fntTributo = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.NORMAL, COLOR_TEXTO_MUTED);

            cellIzq.AddElement(new Paragraph("Av. Tecnología 1000 - Parque Tecnológico", fntDir) { SpacingBefore = 8, Alignment = Element.ALIGN_LEFT });
            string separador = "   \u2022   ";
            cellIzq.AddElement(new Paragraph($"CUIT: 30-75001234-9{separador}Ingresos Brutos: 30-75001234-9", fntTributo) { SpacingBefore = 2, Alignment = Element.ALIGN_LEFT });
            cellIzq.AddElement(new Paragraph("SteeLog - Logística y Stock", fntTributo) { SpacingBefore = 1, Alignment = Element.ALIGN_LEFT });

            headerTable.AddCell(cellIzq);

            // ======= DERECHA: TÍTULO Y SUBTÍTULO =======
            PdfPCell cellDer = new PdfPCell() { Border = Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
            cellDer.AddElement(new Paragraph("INFORME DE OBRA", fntTituloPrincipal) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
            cellDer.AddElement(new Paragraph("Documento Operativo y Logístico", fntSubtitulo) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 3 });
            
            if (mostrarValorizacion)
            {
                // Tarjeta destacada de "Total Pendiente"
                PdfPTable cardTable = new PdfPTable(1);
                cardTable.WidthPercentage = 75;
                cardTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                cardTable.SpacingBefore = 8;
                
                PdfPCell cardCell = new PdfPCell();
                cardCell.BackgroundColor = COLOR_FONDO_SECCION;
                cardCell.Border = Rectangle.BOX;
                cardCell.BorderColor = COLOR_VERDE_ACCENT;
                cardCell.BorderWidth = 1f;
                cardCell.Padding = 8;
                cardCell.HorizontalAlignment = Element.ALIGN_CENTER;
                cardCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                
                Font fntCardLabel = new Font(Font.FontFamily.HELVETICA, 7.5f, Font.BOLD, COLOR_VERDE_HEADER);
                Font fntCardValue = new Font(Font.FontFamily.HELVETICA, 13, Font.BOLD, COLOR_VERDE_OSCURO);
                
                Paragraph pCardLabel = new Paragraph("TOTAL PENDIENTE DE PAGO", fntCardLabel);
                pCardLabel.Alignment = Element.ALIGN_CENTER;
                pCardLabel.SpacingAfter = 2;
                cardCell.AddElement(pCardLabel);
                
                Paragraph pCardValue = new Paragraph($"$ {totalPendiente.ToString("N2")}", fntCardValue);
                pCardValue.Alignment = Element.ALIGN_CENTER;
                cardCell.AddElement(pCardValue);
                
                cardTable.AddCell(cardCell);
                cellDer.AddElement(cardTable);
            }

            headerTable.AddCell(cellDer);

            doc.Add(headerTable);

            // Separador horizontal sutil
            PdfPTable lineaSep = new PdfPTable(1);
            lineaSep.WidthPercentage = 100;
            lineaSep.SpacingBefore = 10;
            lineaSep.SpacingAfter = 15;
            PdfPCell lineaCell = new PdfPCell();
            lineaCell.Border = Rectangle.BOTTOM_BORDER;
            lineaCell.BorderColor = COLOR_GRIS_BORDE;
            lineaCell.BorderWidth = 1f;
            lineaCell.FixedHeight = 1;
            lineaSep.AddCell(lineaCell);
            doc.Add(lineaSep);

            // Bloque informativo de la obra (Firm Grid Row)
            PdfPTable infoTable = new PdfPTable(3);
            infoTable.WidthPercentage = 100;
            infoTable.SetWidths(new float[] { 48f, 34f, 18f });
            infoTable.SpacingAfter = 15;

            // Celda 1: Obra
            Phrase phraseObra = new Phrase();
            phraseObra.Add(new Chunk("Obra:  ", fntEtiqueta));
            phraseObra.Add(new Chunk(nombreObra, fntDatoObra));
            PdfPCell cellObra = new PdfPCell(phraseObra);
            cellObra.BackgroundColor = COLOR_FONDO_SECCION;
            cellObra.Border = Rectangle.BOX;
            cellObra.BorderColor = COLOR_VERDE_OSCURO;
            cellObra.BorderWidth = 1.2f;
            cellObra.Padding = 8;
            cellObra.VerticalAlignment = Element.ALIGN_MIDDLE;
            infoTable.AddCell(cellObra);

            // Celda 2: Fecha Informe / Rango
            Phrase phraseFecha = new Phrase();
            if (desde.HasValue || hasta.HasValue)
            {
                phraseFecha.Add(new Chunk("Período:  ", fntEtiqueta));
                string rango = (desde?.ToString("dd/MM/yyyy") ?? "—") + "  →  " + (hasta?.ToString("dd/MM/yyyy") ?? "—");
                phraseFecha.Add(new Chunk(rango, fntDatoObra));
            }
            else
            {
                phraseFecha.Add(new Chunk("Fecha Informe:  ", fntEtiqueta));
                phraseFecha.Add(new Chunk(DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fntDatoObra));
            }
            PdfPCell cellFecha = new PdfPCell(phraseFecha);
            cellFecha.BackgroundColor = COLOR_FONDO_SECCION;
            cellFecha.Border = Rectangle.BOX;
            cellFecha.BorderColor = COLOR_VERDE_OSCURO;
            cellFecha.BorderWidth = 1.2f;
            cellFecha.Padding = 8;
            cellFecha.VerticalAlignment = Element.ALIGN_MIDDLE;
            infoTable.AddCell(cellFecha);

            // Celda 3: Cant. Remitos
            Phrase phraseRemitos = new Phrase();
            phraseRemitos.Add(new Chunk("Remitos:  ", fntEtiqueta));
            phraseRemitos.Add(new Chunk(cantRemitos.ToString(), fntDatoObra));
            PdfPCell cellRemitos = new PdfPCell(phraseRemitos);
            cellRemitos.BackgroundColor = COLOR_FONDO_SECCION;
            cellRemitos.Border = Rectangle.BOX;
            cellRemitos.BorderColor = COLOR_VERDE_OSCURO;
            cellRemitos.BorderWidth = 1.2f;
            cellRemitos.Padding = 8;
            cellRemitos.VerticalAlignment = Element.ALIGN_MIDDLE;
            cellRemitos.HorizontalAlignment = Element.ALIGN_CENTER;
            infoTable.AddCell(cellRemitos);

            doc.Add(infoTable);
        }

        private void AgregarSeccionesPorTipoMovimiento(Document doc, List<InformeMovimientoDetalle> movimientos, bool mostrarValorizacion)
        {
            var agrupados = movimientos
                .GroupBy(m => m.tipo_movimiento ?? "OTROS")
                .OrderBy(g => {
                    string key = g.Key.ToUpper();
                    if (key.Contains("ENVÍO") || key.Contains("ENVIO")) return 1;
                    if (key.Contains("COMPRA EVENTUAL") || key.Contains("COMPRA")) return 2;
                    if (key.Contains("DEVOLUCIÓN") || key.Contains("DEVOLUCION")) return 3;
                    return 4;
                })
                .ThenBy(g => g.Key)
                .ToList();

            foreach (var grupo in agrupados)
            {
                doc.Add(new Paragraph(" "));

                // Título de sección moderno
                PdfPTable tituloTabla = new PdfPTable(1);
                tituloTabla.WidthPercentage = 100;
                tituloTabla.SpacingBefore = 8;
                tituloTabla.SpacingAfter = 6;
                
                PdfPCell tituloCell = new PdfPCell(new Phrase(grupo.Key.ToUpper(), fntSeccion));
                tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
                tituloCell.Border = Rectangle.LEFT_BORDER;
                tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
                tituloCell.BorderWidthLeft = 4f;
                tituloCell.Padding = 8;
                tituloCell.PaddingLeft = 12;
                tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                
                tituloTabla.AddCell(tituloCell);
                doc.Add(tituloTabla);

                // Tabla de detalle
                PdfPTable tabla = new PdfPTable(mostrarValorizacion ? 6 : 4);
                tabla.WidthPercentage = 100;
                tabla.SetWidths(mostrarValorizacion
                    ? new float[] { 12f, 13f, 35f, 10f, 15f, 15f }
                    : new float[] { 16f, 16f, 48f, 20f });
                tabla.SpacingAfter = 15;

                AgregarCeldaHeader(tabla, "Fecha", Element.ALIGN_CENTER);
                AgregarCeldaHeader(tabla, "Remito", Element.ALIGN_CENTER);
                AgregarCeldaHeader(tabla, "Material / Recorte", Element.ALIGN_LEFT);
                AgregarCeldaHeader(tabla, "Cant.", Element.ALIGN_CENTER);
                if (mostrarValorizacion)
                {
                    AgregarCeldaHeader(tabla, "Precio Un.", Element.ALIGN_RIGHT);
                    AgregarCeldaHeader(tabla, "Total", Element.ALIGN_RIGHT);
                }

                bool alternar = false;
                decimal subtotal = InformeObraDatos.CalcularValorMovimientos(grupo);

                var itemsAgrupados = grupo
                    .GroupBy(m => new { 
                        Fecha = m.fecha.Date, 
                        NroRemito = (m.nroremito ?? "").Trim(), 
                        Material = (m.material ?? "").Trim(),
                        PrecioUnitario = m.precio_unitario
                    })
                    .Select(g => {
                        bool tieneMaterialCompleto = g.Any(x => !x.esrecorte);
                        string nombreMaterial = g.Key.Material;
                        var recortesConMedida = g.Where(x => x.esrecorte && !string.IsNullOrEmpty(x.medida_recorte)).ToList();

                        if (!tieneMaterialCompleto && recortesConMedida.Count > 0)
                        {
                            string medidas = string.Join(", ", recortesConMedida.Select(x => x.medida_recorte).Distinct());
                            nombreMaterial += $" (R: {medidas})";
                        }

                        decimal cantidadTotal = g.Sum(x => x.cantidad);
                        decimal precioUnitario = g.Key.PrecioUnitario;
                        decimal totalFila = cantidadTotal * precioUnitario;

                        return new {
                            Fecha = g.Key.Fecha,
                            NroRemito = g.Key.NroRemito,
                            Material = nombreMaterial,
                            Cantidad = cantidadTotal,
                            PrecioUnitario = precioUnitario,
                            Total = totalFila
                        };
                    })
                    .OrderByDescending(x => x.Fecha)
                    .ThenByDescending(x => x.NroRemito)
                    .ToList();

                foreach (var item in itemsAgrupados)
                {
                    BaseColor bgColor = alternar ? COLOR_FONDO_FILA : COLOR_BLANCO;
                    AgregarCeldaDato(tabla, item.Fecha.ToString("dd/MM/yyyy"), bgColor, Element.ALIGN_CENTER);
                    AgregarCeldaDato(tabla, !string.IsNullOrWhiteSpace(item.NroRemito) ? item.NroRemito : "-", bgColor, Element.ALIGN_CENTER);
                    AgregarCeldaDato(tabla, item.Material, bgColor);
                    AgregarCeldaDato(tabla, item.Cantidad.ToString("G29"), bgColor, Element.ALIGN_CENTER);
                    if (mostrarValorizacion)
                    {
                        AgregarCeldaDato(tabla, $"$ {item.PrecioUnitario.ToString("N2")}", bgColor, Element.ALIGN_RIGHT);
                        AgregarCeldaDato(tabla, $"$ {item.Total.ToString("N2")}", bgColor, Element.ALIGN_RIGHT);
                    }

                    alternar = !alternar;
                }

                if (mostrarValorizacion)
                {
                    // Fila de Subtotal
                    PdfPCell cellSubtotalLabel = new PdfPCell(new Phrase("Subtotal", fntCeldaBold));
                    cellSubtotalLabel.Colspan = 5;
                    cellSubtotalLabel.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cellSubtotalLabel.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cellSubtotalLabel.Padding = 7;
                    cellSubtotalLabel.BackgroundColor = COLOR_FONDO_SECCION;
                    cellSubtotalLabel.Border = Rectangle.BOTTOM_BORDER;
                    cellSubtotalLabel.BorderColorBottom = COLOR_VERDE_HEADER;
                    cellSubtotalLabel.BorderWidthBottom = 1.5f;
                    tabla.AddCell(cellSubtotalLabel);

                    PdfPCell cellSubtotalValor = new PdfPCell(new Phrase($"$ {subtotal.ToString("N2")}", fntCeldaBold));
                    cellSubtotalValor.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cellSubtotalValor.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cellSubtotalValor.Padding = 7;
                    cellSubtotalValor.BackgroundColor = COLOR_FONDO_SECCION;
                    cellSubtotalValor.Border = Rectangle.BOTTOM_BORDER;
                    cellSubtotalValor.BorderColorBottom = COLOR_VERDE_HEADER;
                    cellSubtotalValor.BorderWidthBottom = 1.5f;
                    tabla.AddCell(cellSubtotalValor);
                }

                doc.Add(tabla);
            }
        }

        private void AgregarSeccionMaterialesAcumulados(Document doc, List<InformeMovimientoDetalle> movimientos, string nombreObra)
        {
            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;
            
            PdfPCell tituloCell = new PdfPCell(new Phrase("DETALLE ACUMULADO DE MATERIALES EN OBRA", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(4);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 45f, 18f, 18f, 19f });
            tabla.SpacingAfter = 15;

            AgregarCeldaHeader(tabla, "Material", Element.ALIGN_LEFT);
            AgregarCeldaHeader(tabla, "Total Enviado", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Total Devuelto", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Neto en Obra", Element.ALIGN_CENTER);

            var resumen = movimientos
                .Where(m => !m.esrecorte && !m.esinsumo)
                .GroupBy(m => m.material)
                .Select(g => new {
                    Material = g.Key,
                    Enviado = g.Where(m => m.destino != null && m.destino.Equals(nombreObra, StringComparison.OrdinalIgnoreCase)).Sum(m => m.cantidad),
                    Devuelto = g.Where(m => m.origen != null && m.origen.Equals(nombreObra, StringComparison.OrdinalIgnoreCase)).Sum(m => m.cantidad)
                })
                .OrderBy(g => g.Material)
                .ToList();

            bool alternar = false;
            foreach (var r in resumen)
            {
                BaseColor bgColor = alternar ? COLOR_FONDO_FILA : COLOR_BLANCO;
                decimal neto = r.Enviado - r.Devuelto;

                AgregarCeldaDato(tabla, r.Material, bgColor);
                AgregarCeldaDato(tabla, r.Enviado.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), bgColor, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, r.Devuelto.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), bgColor, Element.ALIGN_CENTER);

                Font fntNeto = neto > 0 ? new Font(Font.FontFamily.HELVETICA, 8.5f, Font.BOLD, COLOR_VERDE_ACCENT) : fntCeldaBold;
                PdfPCell cellNeto = new PdfPCell(new Phrase(neto.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), fntNeto));
                cellNeto.BackgroundColor = bgColor;
                cellNeto.Padding = 7;
                cellNeto.HorizontalAlignment = Element.ALIGN_CENTER;
                cellNeto.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellNeto.Border = Rectangle.BOTTOM_BORDER;
                cellNeto.BorderColorBottom = COLOR_GRIS_BORDE;
                cellNeto.BorderWidthBottom = 0.5f;
                tabla.AddCell(cellNeto);

                alternar = !alternar;
            }

            doc.Add(tabla);
        }

        private void AgregarSeccionRecortes(Document doc, List<InformeMovimientoDetalle> recortes, string nombreObra)
        {
            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;
            
            PdfPCell tituloCell = new PdfPCell(new Phrase("RECORTES EN OBRA", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_ETIQUETA_RECORTE;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(5);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 35f, 18f, 15f, 17f, 15f });
            tabla.SpacingAfter = 15;

            AgregarCeldaHeader(tabla, "Material", Element.ALIGN_LEFT);
            AgregarCeldaHeader(tabla, "Medida", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Enviado", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Devuelto", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Neto en Obra", Element.ALIGN_CENTER);

            var resumen = recortes
                .GroupBy(m => new { m.material, m.medida_recorte })
                .Select(g => new {
                    Material  = g.Key.material,
                    Medida    = g.Key.medida_recorte,
                    Enviado   = g.Where(m => m.destino != null && m.destino.Equals(nombreObra, StringComparison.OrdinalIgnoreCase)).Sum(m => m.cantidad),
                    Devuelto  = g.Where(m => m.origen  != null && m.origen .Equals(nombreObra, StringComparison.OrdinalIgnoreCase)).Sum(m => m.cantidad)
                })
                .OrderBy(r => r.Material)
                .ThenBy(r => r.Medida)
                .ToList();

            bool alt = false;
            foreach (var r in resumen)
            {
                BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;
                decimal neto = r.Enviado - r.Devuelto;

                AgregarCeldaDato(tabla, r.Material, bg);
                AgregarCeldaDato(tabla, string.IsNullOrEmpty(r.Medida) ? "-" : r.Medida, bg, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, r.Enviado.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), bg, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, r.Devuelto.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), bg, Element.ALIGN_CENTER);

                Font fntNeto = neto > 0 ? new Font(Font.FontFamily.HELVETICA, 8.5f, Font.BOLD, COLOR_VERDE_ACCENT) : fntCeldaBold;
                PdfPCell cellNeto = new PdfPCell(new Phrase(neto.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), fntNeto));
                cellNeto.BackgroundColor = bg;
                cellNeto.Padding = 7;
                cellNeto.HorizontalAlignment = Element.ALIGN_CENTER;
                cellNeto.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellNeto.Border = Rectangle.BOTTOM_BORDER;
                cellNeto.BorderColorBottom = COLOR_GRIS_BORDE;
                cellNeto.BorderWidthBottom = 0.5f;
                tabla.AddCell(cellNeto);

                alt = !alt;
            }

            doc.Add(tabla);
        }

        private void AgregarSeccionEventuales(Document doc, List<InformeMovimientoDetalle> eventuales)
        {
            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;
            
            PdfPCell tituloCell = new PdfPCell(new Phrase("MATERIALES EVENTUALES Y APORTES", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(4);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 45f, 25f, 15f, 15f });
            tabla.SpacingAfter = 15;

            AgregarCeldaHeader(tabla, "Material / Descripción", Element.ALIGN_LEFT);
            AgregarCeldaHeader(tabla, "Tipo", Element.ALIGN_LEFT);
            AgregarCeldaHeader(tabla, "Cantidad", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Fecha", Element.ALIGN_CENTER);

            var ordenados = eventuales.OrderByDescending(m => m.fecha).ToList();
            bool alternar = false;

            foreach (var m in ordenados)
            {
                BaseColor bgColor = alternar ? COLOR_FONDO_FILA : COLOR_BLANCO;

                AgregarCeldaDato(tabla, m.material, bgColor);
                AgregarCeldaDato(tabla, m.tipo_movimiento, bgColor);
                AgregarCeldaDato(tabla, m.cantidad.ToString("G29", System.Globalization.CultureInfo.InvariantCulture), bgColor, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, m.fecha.ToString("dd/MM/yyyy"), bgColor, Element.ALIGN_CENTER);

                alternar = !alternar;
            }

            doc.Add(tabla);
        }

        private void AgregarSeccionCanaletasPlegados(Document doc, List<InformeCanaletaPlegadoDetalle> canaletasPlegados, bool mostrarValorizacion)
        {
            if (canaletasPlegados == null || canaletasPlegados.Count == 0)
                return;

            doc.Add(new Paragraph(" "));

            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;

            PdfPCell tituloCell = new PdfPCell(new Phrase("CANALETAS / PLEGADOS", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;

            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(mostrarValorizacion ? 4 : 3);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(mostrarValorizacion
                ? new float[] { 14f, 48f, 14f, 24f }
                : new float[] { 16f, 68f, 16f });
            tabla.SpacingAfter = 15;

            AgregarCeldaHeader(tabla, "Fecha", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Descripción", Element.ALIGN_LEFT);
            AgregarCeldaHeader(tabla, "Pagado", Element.ALIGN_CENTER);
            if (mostrarValorizacion)
                AgregarCeldaHeader(tabla, "Precio", Element.ALIGN_RIGHT);

            bool alternar = false;
            decimal total = 0;

            foreach (var item in canaletasPlegados.OrderByDescending(c => c.fecha_creacion).ThenByDescending(c => c.id))
            {
                BaseColor bgColor = alternar ? COLOR_FONDO_FILA : COLOR_BLANCO;
                AgregarCeldaDato(tabla, item.fecha_creacion.ToString("dd/MM/yyyy"), bgColor, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, item.descripcion, bgColor, Element.ALIGN_LEFT);
                AgregarCeldaDato(tabla, item.pagado ? "Sí" : "No", bgColor, Element.ALIGN_CENTER);

                if (mostrarValorizacion)
                {
                    if (!item.pagado)
                        total += item.precio;
                    AgregarCeldaDato(tabla, $"$ {item.precio.ToString("N2")}", bgColor, Element.ALIGN_RIGHT);
                }

                alternar = !alternar;
            }

            if (mostrarValorizacion)
            {
                PdfPCell cellTotalLabel = new PdfPCell(new Phrase("Total Canaletas / Plegados pendientes", fntCeldaBold));
                cellTotalLabel.Colspan = 3;
                cellTotalLabel.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellTotalLabel.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellTotalLabel.Padding = 7;
                cellTotalLabel.BackgroundColor = COLOR_FONDO_SECCION;
                cellTotalLabel.Border = Rectangle.BOTTOM_BORDER;
                cellTotalLabel.BorderColorBottom = COLOR_VERDE_HEADER;
                cellTotalLabel.BorderWidthBottom = 1.5f;
                tabla.AddCell(cellTotalLabel);

                PdfPCell cellTotalValor = new PdfPCell(new Phrase($"$ {total.ToString("N2")}", fntCeldaBold));
                cellTotalValor.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellTotalValor.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellTotalValor.Padding = 7;
                cellTotalValor.BackgroundColor = COLOR_FONDO_SECCION;
                cellTotalValor.Border = Rectangle.BOTTOM_BORDER;
                cellTotalValor.BorderColorBottom = COLOR_VERDE_HEADER;
                cellTotalValor.BorderWidthBottom = 1.5f;
                tabla.AddCell(cellTotalValor);
            }

            doc.Add(tabla);
        }

        private void AgregarCeldaHeader(PdfPTable tabla, string texto, int align = Element.ALIGN_CENTER)
        {
            PdfPCell cell = new PdfPCell(new Phrase(texto, fntHeaderTabla));
            cell.BackgroundColor = COLOR_VERDE_HEADER;
            cell.Padding = 7;
            cell.HorizontalAlignment = align;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColorBottom = COLOR_VERDE_ACCENT;
            cell.BorderWidthBottom = 2f;
            tabla.AddCell(cell);
        }

        private void AgregarCeldaDato(PdfPTable tabla, string texto, BaseColor bgColor, int align = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(texto ?? "-", fntCelda));
            cell.BackgroundColor = bgColor;
            cell.Padding = 7;
            cell.HorizontalAlignment = align;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColorBottom = COLOR_GRIS_BORDE;
            cell.BorderWidthBottom = 0.5f;
            tabla.AddCell(cell);
        }

        private void AgregarSeccionActivos(Document doc, List<InformeHerramientaDetalle> herramientas, List<InformeInsumoDetalle> insumos)
        {
            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;
            
            PdfPCell tituloCell = new PdfPCell(new Phrase("HERRAMIENTAS E INSUMOS", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            if (herramientas.Count > 0 || insumos.Count > 0)
            {
                PdfPTable tabla = new PdfPTable(4);
                tabla.WidthPercentage = 100;
                tabla.SetWidths(new float[] { 35f, 20f, 22f, 23f });
                tabla.SpacingBefore = 10;
                tabla.SpacingAfter = 10;

                AgregarCeldaHeader(tabla, "Descripción", Element.ALIGN_LEFT);
                AgregarCeldaHeader(tabla, "Estado", Element.ALIGN_CENTER);
                AgregarCeldaHeader(tabla, "Fecha Ingreso", Element.ALIGN_CENTER);
                AgregarCeldaHeader(tabla, "Fecha Retorno", Element.ALIGN_CENTER);

                bool alt = false;

                foreach (var h in herramientas)
                {
                    BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;

                    Phrase pDesc = new Phrase();
                    pDesc.Add(new Chunk(h.nombre + " ", fntCelda));
                    pDesc.Add(new Chunk("[HERRAMIENTA]", new Font(Font.FontFamily.HELVETICA, 7.5f, Font.BOLD, COLOR_TEXTO_MUTED)));
                    PdfPCell cDesc = new PdfPCell(pDesc) { 
                        BackgroundColor = bg, 
                        Padding = 7, 
                        Border = Rectangle.BOTTOM_BORDER, 
                        BorderColorBottom = COLOR_GRIS_BORDE, 
                        BorderWidthBottom = 0.5f,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    tabla.AddCell(cDesc);

                    string estadoTexto = h.en_obra ? "EN OBRA" : "REGRESÓ";
                    BaseColor estadoColor = h.en_obra ? COLOR_VERDE_ACCENT : COLOR_TEXTO_MUTED;
                    Font fntEstado = new Font(Font.FontFamily.HELVETICA, 8.5f, Font.BOLD, estadoColor);
                    PdfPCell cellEstado = new PdfPCell(new Phrase(estadoTexto, fntEstado));
                    cellEstado.BackgroundColor = bg;
                    cellEstado.Padding = 7;
                    cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                    cellEstado.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cellEstado.Border = Rectangle.BOTTOM_BORDER;
                    cellEstado.BorderColorBottom = COLOR_GRIS_BORDE;
                    cellEstado.BorderWidthBottom = 0.5f;
                    tabla.AddCell(cellEstado);

                    AgregarCeldaDato(tabla, h.fecha_ingreso?.ToString("dd/MM/yyyy") ?? "-", bg, Element.ALIGN_CENTER);
                    AgregarCeldaDato(tabla, h.fecha_retorno?.ToString("dd/MM/yyyy") ?? (h.en_obra ? "EN OBRA" : "-"), bg, Element.ALIGN_CENTER);
                    alt = !alt;
                }

                foreach (var ins in insumos)
                {
                    BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;

                    Phrase pDesc = new Phrase();
                    pDesc.Add(new Chunk(ins.nombre + " ", fntCelda));
                    pDesc.Add(new Chunk("[INSUMO]", new Font(Font.FontFamily.HELVETICA, 7.5f, Font.BOLD, COLOR_TEXTO_MUTED)));
                    PdfPCell cDesc = new PdfPCell(pDesc) { 
                        BackgroundColor = bg, 
                        Padding = 7, 
                        Border = Rectangle.BOTTOM_BORDER, 
                        BorderColorBottom = COLOR_GRIS_BORDE, 
                        BorderWidthBottom = 0.5f,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    tabla.AddCell(cDesc);

                    string estadoTexto = ins.en_obra ? "EN OBRA" : (ins.consumido ? "CONSUMIDO" : "REGRESÓ");
                    BaseColor estadoColor = ins.en_obra ? COLOR_VERDE_ACCENT : (ins.consumido ? COLOR_ETIQUETA_RECORTE : COLOR_TEXTO_MUTED);
                    Font fntEstado = new Font(Font.FontFamily.HELVETICA, 8.5f, Font.BOLD, estadoColor);
                    PdfPCell cellEstado = new PdfPCell(new Phrase(estadoTexto, fntEstado));
                    cellEstado.BackgroundColor = bg;
                    cellEstado.Padding = 7;
                    cellEstado.HorizontalAlignment = Element.ALIGN_CENTER;
                    cellEstado.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cellEstado.Border = Rectangle.BOTTOM_BORDER;
                    cellEstado.BorderColorBottom = COLOR_GRIS_BORDE;
                    cellEstado.BorderWidthBottom = 0.5f;
                    tabla.AddCell(cellEstado);

                    AgregarCeldaDato(tabla, ins.fecha_ingreso?.ToString("dd/MM/yyyy") ?? "-", bg, Element.ALIGN_CENTER);
                    AgregarCeldaDato(tabla, ins.fecha_retorno?.ToString("dd/MM/yyyy") ?? (ins.en_obra ? "EN OBRA" : "-"), bg, Element.ALIGN_CENTER);
                    alt = !alt;
                }

                doc.Add(tabla);
            }
        }

        private void AgregarSeccionFletes(Document doc, List<InformeMovimientoDetalle> movimientos)
        {
            var fletes = movimientos
                .Where(m => m.costoflete > 0)
                .GroupBy(m => m.id_movimiento)
                .Select(g => g.First())
                .OrderBy(m => m.fecha)
                .ToList();

            if (fletes.Count == 0) return;

            doc.Add(new Paragraph(" "));

            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;
            
            PdfPCell tituloCell = new PdfPCell(new Phrase("FLETES DE LA OBRA", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(4);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 15f, 18f, 47f, 20f });
            tabla.SpacingAfter = 15;

            AgregarCeldaHeader(tabla, "Fecha", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Remito", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Observaciones (Flete)", Element.ALIGN_LEFT);
            AgregarCeldaHeader(tabla, "Costo", Element.ALIGN_RIGHT);

            bool alt = false;
            decimal totalFletes = 0;

            foreach (var f in fletes)
            {
                BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;
                totalFletes += f.costoflete;

                AgregarCeldaDato(tabla, f.fecha.ToString("dd/MM/yyyy"), bg, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, !string.IsNullOrWhiteSpace(f.nroremito) ? f.nroremito : "-", bg, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, !string.IsNullOrWhiteSpace(f.observaciones) ? f.observaciones : "-", bg, Element.ALIGN_LEFT);
                AgregarCeldaDato(tabla, $"$ {f.costoflete.ToString("N2")}", bg, Element.ALIGN_RIGHT);

                alt = !alt;
            }

            // Fila de Total de Fletes
            PdfPCell cellTotalLabel = new PdfPCell(new Phrase("Total Fletes", fntCeldaBold));
            cellTotalLabel.Colspan = 3;
            cellTotalLabel.HorizontalAlignment = Element.ALIGN_RIGHT;
            cellTotalLabel.VerticalAlignment = Element.ALIGN_MIDDLE;
            cellTotalLabel.Padding = 7;
            cellTotalLabel.BackgroundColor = COLOR_FONDO_SECCION;
            cellTotalLabel.Border = Rectangle.BOTTOM_BORDER;
            cellTotalLabel.BorderColorBottom = COLOR_VERDE_HEADER;
            cellTotalLabel.BorderWidthBottom = 1.5f;
            tabla.AddCell(cellTotalLabel);

            PdfPCell cellTotalValor = new PdfPCell(new Phrase($"$ {totalFletes.ToString("N2")}", fntCeldaBold));
            cellTotalValor.HorizontalAlignment = Element.ALIGN_RIGHT;
            cellTotalValor.VerticalAlignment = Element.ALIGN_MIDDLE;
            cellTotalValor.Padding = 7;
            cellTotalValor.BackgroundColor = COLOR_FONDO_SECCION;
            cellTotalValor.Border = Rectangle.BOTTOM_BORDER;
            cellTotalValor.BorderColorBottom = COLOR_VERDE_HEADER;
            cellTotalValor.BorderWidthBottom = 1.5f;
            tabla.AddCell(cellTotalValor);

            doc.Add(tabla);
        }

        private void AgregarSeccionObservaciones(Document doc, List<InformeMovimientoDetalle> movimientos)
        {
            var observaciones = movimientos
                .Where(m => !string.IsNullOrWhiteSpace(m.observaciones))
                .GroupBy(m => new { m.id_movimiento, m.nroremito, m.observaciones })
                .Select(g => g.First())
                .ToList();

            if (observaciones.Count == 0) return;

            doc.Add(new Paragraph(" "));

            PdfPTable tituloTabla = new PdfPTable(1);
            tituloTabla.WidthPercentage = 100;
            tituloTabla.SpacingBefore = 10;
            tituloTabla.SpacingAfter = 6;
            
            PdfPCell tituloCell = new PdfPCell(new Phrase("OBSERVACIONES", fntSeccion));
            tituloCell.BackgroundColor = COLOR_FONDO_SECCION;
            tituloCell.Border = Rectangle.LEFT_BORDER;
            tituloCell.BorderColorLeft = COLOR_VERDE_ACCENT;
            tituloCell.BorderWidthLeft = 4f;
            tituloCell.Padding = 8;
            tituloCell.PaddingLeft = 12;
            tituloCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            
            tituloTabla.AddCell(tituloCell);
            doc.Add(tituloTabla);

            PdfPTable tabla = new PdfPTable(3);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 15f, 18f, 67f });
            tabla.SpacingAfter = 15;

            AgregarCeldaHeader(tabla, "Remito", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Fecha", Element.ALIGN_CENTER);
            AgregarCeldaHeader(tabla, "Observación", Element.ALIGN_LEFT);

            bool alt = false;
            foreach (var obs in observaciones)
            {
                BaseColor bg = alt ? COLOR_FONDO_FILA : COLOR_BLANCO;
                AgregarCeldaDato(tabla, !string.IsNullOrWhiteSpace(obs.nroremito) ? obs.nroremito : "-", bg, Element.ALIGN_CENTER);
                AgregarCeldaDato(tabla, obs.fecha.ToString("dd/MM/yyyy"), bg, Element.ALIGN_CENTER);

                PdfPCell cellObs = new PdfPCell(new Phrase(obs.observaciones, fntObservacion));
                cellObs.BackgroundColor = bg;
                cellObs.Padding = 7;
                cellObs.VerticalAlignment = Element.ALIGN_MIDDLE;
                cellObs.Border = Rectangle.BOTTOM_BORDER;
                cellObs.BorderColorBottom = COLOR_GRIS_BORDE;
                cellObs.BorderWidthBottom = 0.5f;
                tabla.AddCell(cellObs);

                alt = !alt;
            }

            doc.Add(tabla);
        }
    }

    /// <summary>
    /// Pie de página con número de página.
    /// </summary>
    public class PieInforme : PdfPageEventHelper
    {
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_OBLIQUE, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            // Línea separadora inferior sutil
            cb.SetColorStroke(new BaseColor(229, 231, 235)); // COLOR_GRIS_BORDE
            cb.SetLineWidth(0.75f);
            cb.MoveTo(document.LeftMargin, document.Bottom - 15);
            cb.LineTo(document.PageSize.Width - document.RightMargin, document.Bottom - 15);
            cb.Stroke();

            // Texto de pie
            cb.BeginText();
            cb.SetFontAndSize(bf, 8);
            cb.SetColorFill(new BaseColor(100, 116, 139)); // COLOR_TEXTO_MUTED
            
            string textoPie = "SteeLog - Sistema de Gestión de Inventario — Documento generado automáticamente";
            cb.ShowTextAligned(Element.ALIGN_LEFT, textoPie, document.LeftMargin, document.Bottom - 28, 0);

            string pagina = "Página " + writer.PageNumber;
            cb.ShowTextAligned(Element.ALIGN_RIGHT, pagina, document.PageSize.Width - document.RightMargin, document.Bottom - 28, 0);
            
            cb.EndText();
        }
    }
}
