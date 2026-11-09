using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SteeLog.Servicios
{
    public sealed class RegistroErrores
    {
        private const long LimiteBytes = 1024 * 1024;
        private static readonly object sincronizacion = new object();
        private readonly Func<string> carpetaDatos;

        public RegistroErrores(Func<string> carpetaDatos)
        {
            this.carpetaDatos = carpetaDatos;
        }

        public void Registrar(object error)
        {
            try
            {
                var texto = Sanear(error == null ? "Error sin detalle." : error.ToString());
                var entrada = DateTimeOffset.Now.ToString("O") + " " + texto + Environment.NewLine;
                lock (sincronizacion)
                {
                    var carpeta = Path.Combine(carpetaDatos(), "logs");
                    Directory.CreateDirectory(carpeta);
                    var archivo = Path.Combine(carpeta, "errores.log");
                    if (File.Exists(archivo) && new FileInfo(archivo).Length > LimiteBytes)
                    {
                        var anterior = archivo + ".1";
                        if (File.Exists(anterior)) File.Delete(anterior);
                        File.Move(archivo, anterior);
                    }
                    File.AppendAllText(archivo, entrada, new UTF8Encoding(false));
                }
            }
            catch
            {
                // Un fallo del registro no debe reemplazar la excepción original.
            }
        }

        private static string Sanear(string texto)
        {
            // Incluye valores entre comillas, que pueden contener punto y coma.
            return Regex.Replace(texto,
                @"\b(Password|Pwd|Host|Server|Data Source|Database|Initial Catalog|Username|User ID|UserName|Port)\s*=\s*(?:""(?:[^""]|"""")*""|'(?:[^']|'')*'|[^;\r\n]*)",
                "$1=[omitido]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));
        }
    }
}
