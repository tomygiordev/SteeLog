using System;
using System.IO;
using System.Reflection;

namespace WP_Inventario
{
    internal static class Branding
    {
        private static byte[] Read(string name)
        {
            string resource = "WP_Inventario.Resources.Branding." + name;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (stream == null) throw new InvalidOperationException("Falta el recurso de identidad obligatorio: " + resource);
                using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return buffer.ToArray(); }
            }
        }

        internal static iTextSharp.text.Image PdfLogo(float width, float height)
        {
            var logo = iTextSharp.text.Image.GetInstance(Read("steelog-logo-dark.png"));
            logo.ScaleToFit(width, height);
            logo.Alignment = iTextSharp.text.Element.ALIGN_LEFT;
            return logo;
        }
    }
}
