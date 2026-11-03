using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SteeLog.Servicios;

namespace SteeLog.Servicios
{
    public static class SupabaseStorageService
    {
        public const string BucketCanaletasFotos = "canaletas-fotos";

        private const long TamanoMaximoBytes = 8L * 1024L * 1024L; // 8 MB

        public static bool SuperaTamanoMaximo(long tamanioBytes)
        {
            return tamanioBytes <= 0 || tamanioBytes > TamanoMaximoBytes;
        }

        public static string ObtenerContentType(string rutaArchivo)
        {
            string ext = Path.GetExtension(rutaArchivo)?.ToLowerInvariant() ?? "";
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".webp":
                    return "image/webp";
                case ".bmp":
                case ".dib":
                    return "image/bmp";
                case ".gif":
                    return "image/gif";
                case ".tif":
                case ".tiff":
                    return "image/tiff";
                case ".ico":
                    return "image/x-icon";
                default:
                    return "application/octet-stream";
            }
        }

        public static string SanitizarNombreArchivo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "foto";

            string nombreSinEspacios = nombre.Trim();
            var builder = new StringBuilder(nombreSinEspacios.Length);
            foreach (char c in nombreSinEspacios)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')
                    builder.Append(c);
                else if (c == ' ')
                    builder.Append('_');
            }

            string resultado = builder.ToString();
            if (string.IsNullOrWhiteSpace(resultado))
                resultado = "foto";

            string extension = Path.GetExtension(resultado);
            if (string.IsNullOrWhiteSpace(extension))
                return resultado;

            string nombreSinExtension = resultado.Substring(0, resultado.Length - extension.Length);
            return nombreSinExtension + extension.ToLowerInvariant();
        }

        public static string ConstruirObjectPath(int canaletaPlegadoId, string nombreArchivoSanitizado)
        {
            string guid = Guid.NewGuid().ToString("N");
            return $"canaletas/{canaletaPlegadoId}/{guid}_{nombreArchivoSanitizado}";
        }

        public static bool EsErrorObjetoNoEncontrado(Exception ex)
        {
            string mensaje = ex?.Message ?? "";
            return mensaje.IndexOf("not_found", StringComparison.OrdinalIgnoreCase) >= 0
                || mensaje.IndexOf("\"404\"", StringComparison.OrdinalIgnoreCase) >= 0
                || mensaje.IndexOf("HTTP NotFound", StringComparison.OrdinalIgnoreCase) >= 0
                || mensaje.IndexOf("Object not found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Compatibilidad temporal: el almacenamiento local no requiere token.
        public static bool TokenDisponible() => true;

        public static Task<bool> SubirAsync(string objectPath, byte[] bytes, string contentType)
            => SteeLog.CompositionRoot.LocalServices.Current.Photos.WriteAsync(objectPath, bytes);
        public static Task<byte[]> DescargarAsync(string objectPath)
            => SteeLog.CompositionRoot.LocalServices.Current.Photos.ReadAsync(objectPath);
        public static Task<bool> EliminarAsync(string objectPath)
            => SteeLog.CompositionRoot.LocalServices.Current.Photos.DeleteAsync(objectPath);
    }
}
