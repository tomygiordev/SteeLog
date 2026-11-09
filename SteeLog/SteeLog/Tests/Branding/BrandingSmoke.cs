using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfTextExtractor = iTextSharp.text.pdf.parser.PdfTextExtractor;
using SteeLog;
using SteeLog.Vistas;

internal static class BrandingSmoke
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    [STAThread] static void Main(string[] args)
    {
        string output = args[0]; Directory.CreateDirectory(output);
        var asm = typeof(VistaGeneral).Assembly;
        foreach (string file in new[] { "steelog-logo-dark.png", "steelog-logo-light.png", "steelog-symbol.png", "steelog-background.png", "steelog.ico" })
            using (var stream = asm.GetManifestResourceStream("SteeLog.Resources.Branding." + file))
            {
                Check(stream != null, "Missing " + file);
                if (file.EndsWith(".png")) using (var image = new Bitmap(stream))
                {
                    Check(image.Width >= 256 && image.Height >= 256, file);
                    Check(image.GetPixel(0, 0).A == 0 && image.GetPixel(image.Width - 1, image.Height - 1).A == 0, file + " opaque background");
                    bool visible = false;
                    for (int y = 0; y < image.Height && !visible; y += 5)
                        for (int x = 0; x < image.Width; x += 5)
                            if (image.GetPixel(x, y).A > 200) { visible = true; break; }
                    Check(visible, file + " empty artwork");
                    Console.WriteLine(file + " " + image.Width + "x" + image.Height + " transparent artwork");
                }
            }
        using (var icon = asm.GetManifestResourceStream("SteeLog.Resources.Branding.steelog.ico"))
        using (var reader = new BinaryReader(icon))
        {
            Check(reader.ReadUInt16() == 0 && reader.ReadUInt16() == 1 && reader.ReadUInt16() == 7, "ICO header");
            foreach (int size in new[] {16,24,32,48,64,128,256}) { int w = reader.ReadByte(), h = reader.ReadByte(); Check((w == 0 ? 256 : w) == size && (h == 0 ? 256 : h) == size, "ICO size " + size); reader.ReadBytes(14); }
        }
        var servicesType = asm.GetType("SteeLog.CompositionRoot.LocalServices", true);
        // Raíz demo temporal: nunca se lee ni se escribe el perfil real del usuario.
        string demoRoot = Path.Combine(Path.GetTempPath(), "steelog-demo-branding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(demoRoot);
        File.WriteAllText(Path.Combine(demoRoot, ".steelog-demo"), "steelog-demo-v1");
        object services = null;
        try {
        services = servicesType.GetMethod("CreateForEnvironment", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { AppDomain.CurrentDomain.BaseDirectory, demoRoot });
        var serviciosVista = (SteeLog.Presentation.IServiciosVista)services;
        foreach (Form form in new Form[] { new Vista_Login(serviciosVista), new VistaGeneral(serviciosVista) })
        using (form)
        {
            if (form is VistaGeneral)
            {
                var shown = typeof(VistaGeneral).GetMethod("VistaGeneral_Shown", BindingFlags.Instance | BindingFlags.NonPublic);
                Check(shown != null, "Updater handler not found");
                form.Shown -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), form, shown);
            }
            form.WindowState = FormWindowState.Normal;
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            form.Show();
            form.PerformLayout();
            Application.DoEvents();
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
            form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
            bitmap.Save(Path.Combine(output, form.GetType().Name + ".png"));
            var first = bitmap.GetPixel(0, 0); bool varied = false;
            for (int y = 0; y < bitmap.Height && !varied; y += 5)
                for (int x = 0; x < bitmap.Width; x += 5)
                    if (bitmap.GetPixel(x, y) != first) { varied = true; break; }
            Check(varied, "Empty form rendering");
            }
            var resources = asm.GetType("SteeLog.Properties.Resources", true);
            var iconProperty = resources.GetProperty("SteeLogIcon", BindingFlags.Static | BindingFlags.NonPublic);
            Check(iconProperty != null, "SteeLog icon resource property missing");
            var expectedIcon = (Icon)iconProperty.GetValue(null, null);
            Check(expectedIcon != null && form.Icon != null, "Missing icon");
            using (var actualBytes = new MemoryStream())
            using (var expectedBytes = new MemoryStream())
            {
                form.Icon.Save(actualBytes); expectedIcon.Save(expectedBytes);
                var actual = actualBytes.ToArray(); var expected = expectedBytes.ToArray();
                Check(System.Linq.Enumerable.SequenceEqual(actual, expected), form.GetType().Name + " has wrong icon");
            }
            var field = form.GetType().GetField(form is VistaGeneral ? "pictureBox1" : "picLogo", BindingFlags.Instance | BindingFlags.NonPublic);
            var logo = (PictureBox)field.GetValue(form);
            Check(logo.Image != null && logo.SizeMode == PictureBoxSizeMode.Zoom, "Form logo");
            Check(logo.Parent.ClientRectangle.Contains(logo.Bounds), "Logo outside parent");
            if (form is VistaGeneral)
            {
                var panel = (Control)typeof(VistaGeneral).GetField("panelChildForm", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                Console.WriteLine("Child panel bounds " + panel.Bounds + "; image " + (panel.BackgroundImage != null));
                using (var panelImage = new Bitmap(panel.Width, panel.Height))
                { panel.DrawToBitmap(panelImage, new System.Drawing.Rectangle(0, 0, panel.Width, panel.Height)); panelImage.Save(Path.Combine(output, "VistaGeneral-panel.png")); }
            }
            Console.WriteLine(form.GetType().Name + " logo bounds " + logo.Bounds);
            form.Close();
        }
        var now = DateTime.Today;
        string ordenes = Path.Combine(output, "Ordenes-pendientes-real.pdf");
        foreach (var property in new[] { "KardexPdf", "ReporteStockPdf" })
        {
            var report = servicesType.GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services, null);
            var method = report.GetType().GetMethod("Generar");
            var data = Activator.CreateInstance(method.GetParameters()[1].ParameterType);
            string collection = property == "KardexPdf" ? "Historial" : "Filas";
            var listProperty = data.GetType().GetProperty(collection);
            var rows = (System.Collections.IList)Activator.CreateInstance(listProperty.PropertyType);
            listProperty.SetValue(data, rows, null);
            if (property == "KardexPdf")
            {
                Fill(data, "MaterialNombre", "Perfil demo", "FechaInicio", now, "FechaFin", now, "MostrarColumnaSaldo", true);
                var row = Activator.CreateInstance(listProperty.PropertyType.GetGenericArguments()[0]);
                Fill(row, "fecha", now, "tipo_movimiento", "Entrada", "cantidad", 2m, "saldo", 4m, "origen", "Galpón", "destino", "Obra demo", "nombre_material", "Perfil demo");
                rows.Add(row);
            }
            else
            {
                Fill(data, "TotalMateriales", "1", "TotalStockBajo", "0");
                var columnsProperty = data.GetType().GetProperty("Columnas");
                var columns = (System.Collections.IList)Activator.CreateInstance(columnsProperty.PropertyType);
                columnsProperty.SetValue(data, columns, null);
                var column = Activator.CreateInstance(columnsProperty.PropertyType.GetGenericArguments()[0]);
                Fill(column, "Nombre", "nombre", "Titulo", "Material"); columns.Add(column);
                var row = Activator.CreateInstance(listProperty.PropertyType.GetGenericArguments()[0]);
                Fill(row, "Valores", new List<string> { "Perfil demo" }); rows.Add(row);
            }
            string path = Path.Combine(output, property + "-real.pdf");
            method.Invoke(report, new[] { (object)path, data }); VerifyPdf(path);
            using (var reader = new PdfReader(path))
                Check(PdfTextExtractor.GetTextFromPage(reader, 1).Contains("Perfil demo"), property + " sin datos reales del fixture");
        }
        var renderer = servicesType.GetProperty("OrdenesPdf", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services, null);
        var generarOrdenes = renderer.GetType().GetMethod("Generar");
        var itemType = generarOrdenes.GetParameters()[1].ParameterType.GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(itemType);
        var pendientes = (System.Collections.IList)Activator.CreateInstance(listType);
        foreach (var values in new[] {
            new object[] { "OC-101", "Proveedor A", "Chapa zinc", 12m, 5m, 7m },
            new object[] { "OC-102", "Proveedor B", "Tornillos", 40m, 10m, 30m }
        }) {
            var row = Activator.CreateInstance(itemType);
            var fields = new[] { "Numero", "Proveedor", "Material", "Pedida", "Recibida", "Faltante" };
            for (int i = 0; i < fields.Length; i++) itemType.GetProperty(fields[i]).SetValue(row, values[i], null);
            pendientes.Add(row);
        }
        generarOrdenes.Invoke(renderer, new object[] { ordenes, pendientes });
        VerifyPdf(ordenes);
        using (var reader = new PdfReader(ordenes)) {
            var text = PdfTextExtractor.GetTextFromPage(reader, 1);
            foreach (var value in new[] { "OC-101", "Proveedor A", "Chapa zinc", "12", "5", "7", "OC-102", "Proveedor B", "Tornillos", "40", "10", "30" })
                Check(text.Contains(value), "Ordenes pendientes: falta " + value + ": " + text);
        }
        string conteo = Path.Combine(output, "Conteo-real.pdf");
        var informeRenderer = servicesType.GetProperty("InformeObraPdf", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services, null);
        var informeGenerar = informeRenderer.GetType().GetMethod("Generar");
        var snapshotType = informeGenerar.GetParameters()[1].ParameterType;
        var snapshot = Activator.CreateInstance(snapshotType);
        snapshotType.GetProperty("NombreObra").SetValue(snapshot, "Obra demo", null);
        foreach (string field in new[] { "Movimientos", "Herramientas", "Insumos", "Canaletas", "Resumen" })
        {
            var property = snapshotType.GetProperty(field);
            property.SetValue(snapshot, Activator.CreateInstance(property.PropertyType), null);
        }
        string informe = Path.Combine(output, "Informe-obra-real.pdf");
        informeGenerar.Invoke(informeRenderer, new[] { (object)informe, snapshot });
        VerifyPdf(informe);
        using (var reader = new PdfReader(informe)) {
            string content = PdfTextExtractor.GetTextFromPage(reader, 1);
            Check(content.Contains("INFORME DE OBRA") && content.Contains("SteeLog"), "Informe de obra sin marca o título");
        }
        var rendererConteo = servicesType.GetProperty("ConteoPdf", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services, null);
        var generarConteo = rendererConteo.GetType().GetMethod("Generar");
        var parametrosConteo = generarConteo.GetParameters();
        var cabecera = Activator.CreateInstance(parametrosConteo[1].ParameterType);
        parametrosConteo[1].ParameterType.GetProperty("Id").SetValue(cabecera, 123, null);
        parametrosConteo[1].ParameterType.GetProperty("Estado").SetValue(cabecera, "finalizado", null);
        var tipoDetalle = parametrosConteo[2].ParameterType.GetGenericArguments()[0];
        var detalles = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tipoDetalle));
        var detalle = Activator.CreateInstance(tipoDetalle);
        tipoDetalle.GetProperty("Nombre").SetValue(detalle, "Material demo", null);
        tipoDetalle.GetProperty("Cantidad").SetValue(detalle, 3m, null);
        tipoDetalle.GetProperty("Stock").SetValue(detalle, 5m, null);
        tipoDetalle.GetProperty("Diferencia").SetValue(detalle, -2m, null);
        detalles.Add(detalle);
        generarConteo.Invoke(rendererConteo, new object[] { conteo, cabecera, detalles, null, true });
        VerifyPdf(conteo);
        using (var reader = new PdfReader(conteo)) {
            string texto = PdfTextExtractor.GetTextFromPage(reader, 1);
            Check(texto.Contains("Material demo") && texto.Contains("DIFERENCIAS"), "Conteo sin detalle esperado: " + texto);
        }
        string remito = Path.Combine(output, "Remito-real.pdf");
        var rendererRemito = servicesType.GetProperty("RemitoPdf", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services, null);
        var generarRemito = rendererRemito.GetType().GetMethod("Generar");
        var remitoType = generarRemito.GetParameters()[1].ParameterType;
        var remitoDto = Activator.CreateInstance(remitoType);
        remitoType.GetProperty("Fecha").SetValue(remitoDto, now, null);
        remitoType.GetProperty("FechaArchivo").SetValue(remitoDto, now, null);
        remitoType.GetProperty("NroRemito").SetValue(remitoDto, "R-123", null);
        remitoType.GetProperty("TipoMovimiento").SetValue(remitoDto, "Salida", null);
        remitoType.GetProperty("Origen").SetValue(remitoDto, "Galpón", null);
        remitoType.GetProperty("Destino").SetValue(remitoDto, "Obra demo", null);
        var lineas = (System.Collections.IList)remitoType.GetProperty("Lineas").GetValue(remitoDto, null);
        var linea = Activator.CreateInstance(lineas.GetType().GetGenericArguments()[0]);
        linea.GetType().GetProperty("Material").SetValue(linea, "Insumo demo", null);
        linea.GetType().GetProperty("Cantidad").SetValue(linea, 1m, null);
        linea.GetType().GetProperty("EsInsumo").SetValue(linea, true, null);
        lineas.Add(linea);
        generarRemito.Invoke(rendererRemito, new object[] { remito, remitoDto });
        VerifyPdf(remito);
        using (var reader = new PdfReader(remito)) {
            var texto = PdfTextExtractor.GetTextFromPage(reader, 1);
            Check(texto.Contains("Insumo demo") && texto.Contains("R-123"), "Remito sin datos esperados: " + texto);
        }
        Console.WriteLine("OK: headers, Kardex, conteo, remito y ordenes con datos, resources, icon and forms");
        } finally {
            var disposable = services as IDisposable; if (disposable != null) disposable.Dispose();
            try { Directory.Delete(demoRoot, true); } catch { }
        }
    }
    static void Fill(object target, params object[] pairs)
    {
        for (int i = 0; i < pairs.Length; i += 2)
            target.GetType().GetProperty((string)pairs[i]).SetValue(target, pairs[i + 1], null);
    }
    static void VerifyPdf(string path)
    {
        using (var reader = new PdfReader(path))
        {
            string text = PdfTextExtractor.GetTextFromPage(reader, 1);
            Check(text.Contains("SteeLog") && text.Contains("Proyecto académico") && text.Contains("Datos de demostración"), path + " text: " + text);
            Check(!text.Contains("75001234") && !text.Contains("Av. Tecnología") && !text.Contains("WP"), path + " old identity");
            var resources = reader.GetPageN(1).GetAsDict(PdfName.RESOURCES); var images = resources.GetAsDict(PdfName.XOBJECT); Check(images != null && images.Size > 0, path + " image missing");
            bool foundImage = false; foreach (PdfName key in images.Keys) { var image = images.GetAsStream(key); if (image != null && PdfName.IMAGE.Equals(image.GetAsName(PdfName.SUBTYPE))) foundImage = true; }
            Check(foundImage, path + " image XObject missing");
            Console.WriteLine("OK " + path);
        }
    }
}
