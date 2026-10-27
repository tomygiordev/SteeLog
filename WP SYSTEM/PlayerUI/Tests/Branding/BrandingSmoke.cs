using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfTextExtractor = iTextSharp.text.pdf.parser.PdfTextExtractor;
using WP_Inventario;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Reportes;
using WP_Inventario.Vistas;

internal static class BrandingSmoke
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    [STAThread] static void Main(string[] args)
    {
        string output = args[0]; Directory.CreateDirectory(output);
        var asm = typeof(VistaGeneral).Assembly;
        foreach (string file in new[] { "steelog-logo-dark.png", "steelog-logo-light.png", "steelog-symbol.png", "steelog-background.png", "steelog.ico" })
            using (var stream = asm.GetManifestResourceStream("WP_Inventario.Resources.Branding." + file))
            { Check(stream != null, "Missing " + file); if (file.EndsWith(".png")) using (var image = System.Drawing.Image.FromStream(stream)) { Check(image.Width >= 256 && image.Height >= 256, file); Console.WriteLine(file + " " + image.Width + "x" + image.Height); } }
        using (var icon = asm.GetManifestResourceStream("WP_Inventario.Resources.Branding.steelog.ico"))
        using (var reader = new BinaryReader(icon))
        {
            Check(reader.ReadUInt16() == 0 && reader.ReadUInt16() == 1 && reader.ReadUInt16() == 7, "ICO header");
            foreach (int size in new[] {16,24,32,48,64,128,256}) { int w = reader.ReadByte(), h = reader.ReadByte(); Check((w == 0 ? 256 : w) == size && (h == 0 ? 256 : h) == size, "ICO size " + size); reader.ReadBytes(14); }
        }
        foreach (Form form in new Form[] { new Vista_Login(), new VistaGeneral() })
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
            var resources = asm.GetType("WP_Inventario.Properties.Resources", true);
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
        var cases = new[] {
            new object[] { typeof(MovimientoPDF), new object[] { new InformeMovimientoDetalle { tipo_movimiento="ENTRADA", fecha=now } } },
            new object[] { typeof(OrdenesPendientesPDF), new object[0] },
            new object[] { typeof(KardexPDF), new object[] { "Material demo", now, now } },
            new object[] { typeof(ConteoInventarioPDF), new object[] { new ConteoInventario { estado="DEMO" }, false, false } },
            new object[] { typeof(InformeObraPDF), new object[] { "Obra demo", 0, 0m, false, null, null } },
            new object[] { asm.GetType("WP_Inventario.Vistas.Vista_ReporteStock", true), new object[0] }
        };
        foreach (var item in cases)
        {
            var type = (Type)item[0]; var method = type.GetMethod("AgregarEncabezado", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Check(method != null, type.Name); string path = Path.Combine(output, type.Name + ".pdf");
            using (var doc = new Document(PageSize.A4)) using (var stream = File.Create(path))
            { PdfWriter.GetInstance(doc, stream); doc.Open(); var parameters = new List<object> { doc }; parameters.AddRange((object[])item[1]); method.Invoke(method.IsStatic ? null : Activator.CreateInstance(type), parameters.ToArray()); doc.Close(); }
            VerifyPdf(path);
        }
        string kardex = Path.Combine(output, "Kardex-real.pdf");
        KardexPDF.Generar(kardex, new List<KardexItem>(), "Material demo", now, now, false, false); VerifyPdf(kardex);
        Console.WriteLine("OK: 6 headers, Kardex, resources, icon and forms");
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
