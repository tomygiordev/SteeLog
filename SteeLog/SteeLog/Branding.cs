using System;
using System.IO;
using System.Reflection;

namespace SteeLog
{
    internal static class Branding
    {
        private static byte[] Read(string name)
        {
            string resource = "SteeLog.Resources.Branding." + name;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (stream == null) throw new InvalidOperationException("Falta el recurso de identidad obligatorio: " + resource);
                using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return buffer.ToArray(); }
            }
        }

    }
}
