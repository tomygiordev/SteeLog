using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Servicios;

namespace WP_Inventario.Servicios
{
    public static class SupabaseStorageService
    {
        public const string BucketCanaletasFotos = "canaletas-fotos";

        private static readonly string LocalStorageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SteeLog", "Storage");

        private static bool EsModoLocal =>
            SessionManager.AccessToken == "local-tesis-token" ||
            SupabaseAuthService.SupabaseUrl.Contains("localhost");

        private const string StorageBaseUrl = SupabaseAuthService.SupabaseUrl + "/storage/v1/object";
        private static readonly TimeSpan TimeoutHttp = TimeSpan.FromSeconds(45);

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

        public static bool TokenDisponible()
        {
            if (EsModoLocal)
                return true;

            return !string.IsNullOrWhiteSpace(SessionManager.AccessToken)
                || !string.IsNullOrWhiteSpace(SessionManager.RefreshToken);
        }

        public static async Task<bool> SubirAsync(string objectPath, byte[] bytes, string contentType)
        {
            if (EsModoLocal)
            {
                if (bytes == null || bytes.Length == 0)
                    throw new ArgumentException("El archivo está vacío.");
                if (SuperaTamanoMaximo(bytes.Length))
                    throw new ArgumentException("El archivo supera el tamaño máximo permitido (8 MB).");

                string fullPath = Path.Combine(LocalStorageDir, objectPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, bytes);
                return await Task.FromResult(true);
            }

            string accessToken = await ObtenerAccessTokenVigenteAsync();

            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("El archivo está vacío.");

            if (SuperaTamanoMaximo(bytes.Length))
                throw new ArgumentException("El archivo supera el tamaño máximo permitido (8 MB).");

            string url = ConstruirUrlObjeto(objectPath);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeoutHttp;
                client.DefaultRequestHeaders.Add("apikey", SupabaseAuthService.AnonKey);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("x-upsert", "false");

                using (var content = new ByteArrayContent(bytes))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    var response = await client.PostAsync(url, content);
                    var body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception(MensajeErrorStorage("subir", response.StatusCode.ToString(), body));

                    return true;
                }
            }
        }

        public static async Task<byte[]> DescargarAsync(string objectPath)
        {
            if (EsModoLocal)
            {
                string fullPath = Path.Combine(LocalStorageDir, objectPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath))
                    return await Task.FromResult(File.ReadAllBytes(fullPath));
                return await Task.FromResult(new byte[0]);
            }

            string accessToken = await ObtenerAccessTokenVigenteAsync();

            string url = ConstruirUrlObjeto(objectPath);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeoutHttp;
                client.DefaultRequestHeaders.Add("apikey", SupabaseAuthService.AnonKey);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsByteArrayAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string textoError = Encoding.UTF8.GetString(body);
                    throw new Exception(MensajeErrorStorage("descargar", response.StatusCode.ToString(), textoError));
                }

                return body;
            }
        }

        public static async Task<bool> EliminarAsync(string objectPath)
        {
            if (EsModoLocal)
            {
                string fullPath = Path.Combine(LocalStorageDir, objectPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                return await Task.FromResult(true);
            }

            string accessToken = await ObtenerAccessTokenVigenteAsync();

            string url = ConstruirUrlObjeto(objectPath);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeoutHttp;
                client.DefaultRequestHeaders.Add("apikey", SupabaseAuthService.AnonKey);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.DeleteAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(MensajeErrorStorage("eliminar", response.StatusCode.ToString(), body));

                return true;
            }
        }

        private static async Task<string> ObtenerAccessTokenVigenteAsync()
        {
            if (EsModoLocal)
                return "local-tesis-token";

            if (!SessionManager.TokenPorVencer())
                return SessionManager.AccessToken;

            if (string.IsNullOrWhiteSpace(SessionManager.RefreshToken))
                throw new InvalidOperationException("La sesión venció. Inicie sesión nuevamente para gestionar fotos.");

            var refresh = await SupabaseAuthService.RefreshSessionAsync(SessionManager.RefreshToken);
            if (!refresh.Success)
                throw new InvalidOperationException(refresh.ErrorMessage ?? "La sesión venció. Inicie sesión nuevamente.");

            SessionManager.ActualizarTokens(refresh.AccessToken, refresh.RefreshToken, refresh.ExpiresIn);
            return SessionManager.AccessToken;
        }

        private static string MensajeErrorStorage(string accion, string statusCode, string body)
        {
            string detalle = string.IsNullOrWhiteSpace(body) ? "" : body;
            if (detalle.Length > 300)
                detalle = detalle.Substring(0, 300) + "...";

            return $"No se pudo {accion} la foto en Supabase Storage (HTTP {statusCode}). {detalle}";
        }

        private static string ConstruirUrlObjeto(string objectPath)
        {
            string pathNormalizado = NormalizarObjectPath(objectPath);
            return $"{StorageBaseUrl}/{BucketCanaletasFotos}/{CodificarPath(pathNormalizado)}";
        }

        private static string NormalizarObjectPath(string objectPath)
        {
            if (string.IsNullOrWhiteSpace(objectPath))
                throw new ArgumentException("La ruta del archivo en Storage está vacía.");

            string path = objectPath.Trim().Replace('\\', '/').TrimStart('/');
            string prefijoBucket = BucketCanaletasFotos + "/";
            if (path.StartsWith(prefijoBucket, StringComparison.OrdinalIgnoreCase))
                path = path.Substring(prefijoBucket.Length);

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("La ruta del archivo en Storage está vacía.");

            return path;
        }

        private static string CodificarPath(string objectPath)
        {
            string[] segmentos = objectPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segmentos.Length; i++)
                segmentos[i] = Uri.EscapeDataString(segmentos[i]);

            return string.Join("/", segmentos);
        }
    }
}
