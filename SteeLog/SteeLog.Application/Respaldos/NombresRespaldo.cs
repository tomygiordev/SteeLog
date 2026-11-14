using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SteeLog.Core.Application.Respaldos
{
    /// <summary>Nombres de archivo de las copias: steelog_yyyyMMdd_HHmmss_tipo.dump</summary>
    public static class NombresRespaldo
    {
        public const string Extension = ".dump";
        public const string ExtensionFicha = ".json";
        private static readonly Regex Patron = new Regex(
            @"^steelog_(?<f>\d{8}_\d{6})_(?<t>manual|automatico|prerestauracion|importado)(_\d+)?\.dump$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        public static string Sufijo(TipoRespaldo tipo) => tipo.ToString().ToLowerInvariant();

        public static string Nombre(DateTime fechaLocal, TipoRespaldo tipo, int repeticion = 1)
            => "steelog_" + fechaLocal.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + Sufijo(tipo)
               + (repeticion > 1 ? "_" + repeticion.ToString(CultureInfo.InvariantCulture) : string.Empty) + Extension;

        /// <summary>Nombre de la ficha: el mismo nombre con extensión .json.</summary>
        public static string NombreFicha(string id) => Path.GetFileNameWithoutExtension(id) + ExtensionFicha;

        /// <summary>Tipo deducido del nombre (copias sin ficha); null si el nombre no sigue el formato propio.</summary>
        public static TipoRespaldo? TipoDesdeNombre(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            try
            {
                var m = Patron.Match(nombre);
                if (!m.Success) return null;
                return (TipoRespaldo)Enum.Parse(typeof(TipoRespaldo), m.Groups["t"].Value, true);
            }
            catch (RegexMatchTimeoutException) { return null; }
        }

        /// <summary>Un Id válido es solo un nombre de archivo .dump: sin carpetas, sin "..", sin caracteres de ruta.</summary>
        public static bool IdValido(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 200) return false;
            if (id != id.Trim() || id.EndsWith(".", StringComparison.Ordinal)) return false;
            if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            if (id.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || id.Contains("..")) return false;
            if (!id.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) || id.Length <= Extension.Length) return false;
            return Path.GetFileName(id) == id;
        }
    }
}
