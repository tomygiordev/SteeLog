using System;
using System.IO;
using SteeLog.Core.Application.Borradores;

namespace SteeLog.Infrastructure.Files
{
    public sealed class LocalDraftStore : IDraftStore
    {
        private readonly string ruta;
        public LocalDraftStore(string ruta) { this.ruta = Path.GetFullPath(ruta ?? throw new ArgumentNullException(nameof(ruta))); }
        public byte[] Leer() => File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
        public void Guardar(byte[] contenido)
        {
            if (contenido == null) throw new ArgumentNullException(nameof(contenido));
            var carpeta = Path.GetDirectoryName(ruta);
            Directory.CreateDirectory(carpeta);
            var temporal = Path.Combine(carpeta, "." + Path.GetFileName(ruta) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var archivo = new FileStream(temporal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    archivo.Write(contenido, 0, contenido.Length);
                    archivo.Flush(true);
                }
                if (File.Exists(ruta)) File.Replace(temporal, ruta, null);
                else File.Move(temporal, ruta);
            }
            finally { if (File.Exists(temporal)) File.Delete(temporal); }
        }
        public void Eliminar() { if (File.Exists(ruta)) File.Delete(ruta); }
    }
}
