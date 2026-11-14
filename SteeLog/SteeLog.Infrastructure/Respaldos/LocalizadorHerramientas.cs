using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SteeLog.Infrastructure.Respaldos
{
    public sealed class HerramientasResueltas
    {
        public bool Disponibles { get; set; }
        public string Mensaje { get; set; }
        /// <summary>Ej. "18.0".</summary>
        public string Version { get; set; }
        public int VersionMayor { get; set; }
        public string PgDump { get; set; }
        public string PgRestore { get; set; }
    }

    /// <summary>
    /// Busca pg_dump / pg_restore: (1) carpeta configurada, (2) herramientas\postgresql junto al ejecutable,
    /// (3) C:\Program Files\PostgreSQL\N\bin (versión mayor primero). Exige versión de pg_dump >= versión mayor del servidor.
    /// </summary>
    public sealed class LocalizadorHerramientas
    {
        public const string NombrePgDump = "pg_dump.exe";
        public const string NombrePgRestore = "pg_restore.exe";
        private static readonly Regex Version = new Regex(@"\)\s*(?<mayor>\d+)(?:\.(?<menor>\d+))?", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        private readonly IEjecutorProcesos ejecutor; private readonly string carpetaConfigurada; private readonly string carpetaEjecutable;
        private readonly string carpetaProgramFiles; private readonly Func<string, bool> existeArchivo; private readonly Func<string, IEnumerable<string>> subcarpetas;

        public LocalizadorHerramientas(IEjecutorProcesos ejecutor, string carpetaConfigurada, string carpetaEjecutable, string carpetaProgramFiles,
            Func<string, bool> existeArchivo = null, Func<string, IEnumerable<string>> subcarpetas = null)
        {
            this.ejecutor = ejecutor ?? throw new ArgumentNullException(nameof(ejecutor));
            this.carpetaConfigurada = carpetaConfigurada; this.carpetaEjecutable = carpetaEjecutable; this.carpetaProgramFiles = carpetaProgramFiles;
            this.existeArchivo = existeArchivo ?? File.Exists;
            this.subcarpetas = subcarpetas ?? (d => Directory.Exists(d) ? Directory.GetDirectories(d) : new string[0]);
        }

        /// <summary>Carpetas candidatas, en orden de preferencia.</summary>
        public IReadOnlyList<string> Candidatas()
        {
            var lista = new List<string>();
            void Agregar(string d) { if (!string.IsNullOrWhiteSpace(d) && !lista.Contains(d, StringComparer.OrdinalIgnoreCase)) lista.Add(d); }
            if (!string.IsNullOrWhiteSpace(carpetaConfigurada)) { Agregar(carpetaConfigurada); Agregar(Path.Combine(carpetaConfigurada, "bin")); }
            if (!string.IsNullOrWhiteSpace(carpetaEjecutable))
            {
                var propia = Path.Combine(carpetaEjecutable, "herramientas", "postgresql");
                Agregar(propia); Agregar(Path.Combine(propia, "bin"));
            }
            if (!string.IsNullOrWhiteSpace(carpetaProgramFiles))
            {
                var raiz = Path.Combine(carpetaProgramFiles, "PostgreSQL");
                IEnumerable<string> versiones;
                try { versiones = subcarpetas(raiz).ToList(); } catch { versiones = new string[0]; }
                foreach (var v in versiones
                    .Select(d => new { d, n = int.TryParse(Path.GetFileName(d), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : -1 })
                    .Where(x => x.n > 0).OrderByDescending(x => x.n))
                    Agregar(Path.Combine(v.d, "bin"));
            }
            return lista;
        }

        /// <param name="versionMayorServidor">Versión mayor del servidor (0 si se desconoce).</param>
        public async Task<HerramientasResueltas> ResolverAsync(int versionMayorServidor, CancellationToken token)
        {
            var encontradas = new List<HerramientasResueltas>();
            foreach (var carpeta in Candidatas())
            {
                token.ThrowIfCancellationRequested();
                var dump = Path.Combine(carpeta, NombrePgDump); var restore = Path.Combine(carpeta, NombrePgRestore);
                if (!existeArchivo(dump) || !existeArchivo(restore)) continue;
                var version = await LeerVersionAsync(dump, token).ConfigureAwait(false);
                if (version == null) continue;
                var h = new HerramientasResueltas { PgDump = dump, PgRestore = restore, Version = version.Item1, VersionMayor = version.Item2, Disponibles = true };
                if (versionMayorServidor <= 0 || h.VersionMayor >= versionMayorServidor) return h;
                encontradas.Add(h);
            }
            if (encontradas.Count > 0)
            {
                var mejor = encontradas.OrderByDescending(h => h.VersionMayor).First();
                return new HerramientasResueltas
                {
                    Disponibles = false, Version = mejor.Version, VersionMayor = mejor.VersionMayor,
                    Mensaje = "Las herramientas de PostgreSQL encontradas son de una versión menor (" + mejor.Version + ") que la del servidor (" + versionMayorServidor +
                              "). Instalá PostgreSQL " + versionMayorServidor + " o superior, o copiá sus herramientas en " + CarpetaPropia() + "."
                };
            }
            return new HerramientasResueltas
            {
                Disponibles = false,
                Mensaje = "No se encontraron pg_dump y pg_restore. Instalá las herramientas de PostgreSQL o copialas en " + CarpetaPropia() +
                          " (también se puede indicar la carpeta en appsettings.json: Respaldos:CarpetaHerramientas)."
            };
        }

        private string CarpetaPropia() => string.IsNullOrWhiteSpace(carpetaEjecutable) ? "herramientas\\postgresql" : Path.Combine(carpetaEjecutable, "herramientas", "postgresql");

        private async Task<Tuple<string, int>> LeerVersionAsync(string pgDump, CancellationToken token)
        {
            try
            {
                var r = await ejecutor.EjecutarAsync(new SolicitudProceso
                { Ejecutable = pgDump, Argumentos = new[] { "--version" }, TiempoMaximo = TimeSpan.FromSeconds(20) }, token).ConfigureAwait(false);
                if (r.CodigoSalida != 0 || r.TiempoAgotado) return null;
                return Interpretar(r.Salida);
            }
            catch (OperationCanceledException) { throw; }
            catch { return null; }
        }

        /// <summary>Interpreta "pg_dump (PostgreSQL) 18.0" -> ("18.0", 18).</summary>
        public static Tuple<string, int> Interpretar(string salida)
        {
            if (string.IsNullOrWhiteSpace(salida)) return null;
            try
            {
                var m = Version.Match(salida);
                if (!m.Success) return null;
                var mayor = int.Parse(m.Groups["mayor"].Value, CultureInfo.InvariantCulture);
                return Tuple.Create(m.Groups["mayor"].Value + (m.Groups["menor"].Success ? "." + m.Groups["menor"].Value : string.Empty), mayor);
            }
            catch (Exception) { return null; }
        }
    }
}
