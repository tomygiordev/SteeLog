using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Respaldos;

namespace SteeLog.Infrastructure.Respaldos
{
    /// <summary>Datos de conexión tomados de la cadena de conexión configurada.</summary>
    public sealed class ConexionRespaldo
    {
        public string Host { get; private set; }
        public int Puerto { get; private set; }
        public string Base { get; private set; }
        public string Usuario { get; private set; }
        public string Clave { get; private set; }
        /// <summary>Valor de PGSSLMODE equivalente, o null.</summary>
        public string ModoSsl { get; private set; }

        public static ConexionRespaldo Desde(string cadenaConexion)
        {
            var b = new NpgsqlConnectionStringBuilder(cadenaConexion);
            string ssl = null;
            switch (b.SslMode)
            {
                case SslMode.Disable: ssl = "disable"; break;
                case SslMode.Prefer: ssl = "prefer"; break;
                case SslMode.Require: ssl = "require"; break;
            }
            return new ConexionRespaldo
            {
                Host = string.IsNullOrWhiteSpace(b.Host) ? "localhost" : b.Host.Split(',')[0].Trim(),
                Puerto = b.Port > 0 ? b.Port : 5432, Base = b.Database, Usuario = b.Username, Clave = b.Password, ModoSsl = ssl
            };
        }
    }

    /// <summary>Crea, lista y restaura copias con los pg_dump / pg_restore oficiales. La clave viaja solo por PGPASSWORD.</summary>
    public sealed class MotorPostgresRespaldo : IMotorRespaldo
    {
        /// <summary>Tiempo que pg_restore espera un bloqueo de otra sesión antes de fallar (milisegundos).</summary>
        public const int EsperaBloqueoMs = 20000;
        public static readonly TimeSpan TiempoMaximoCopia = TimeSpan.FromMinutes(60);

        private readonly IEjecutorProcesos ejecutor; private readonly Func<string> cadenaConexion;
        private readonly LocalizadorHerramientas localizador; private readonly IBaseRespaldo bd;

        public MotorPostgresRespaldo(IEjecutorProcesos ejecutor, Func<string> cadenaConexion, LocalizadorHerramientas localizador, IBaseRespaldo bd)
        {
            this.ejecutor = ejecutor ?? throw new ArgumentNullException(nameof(ejecutor));
            this.cadenaConexion = cadenaConexion ?? throw new ArgumentNullException(nameof(cadenaConexion));
            this.localizador = localizador ?? throw new ArgumentNullException(nameof(localizador));
            this.bd = bd ?? throw new ArgumentNullException(nameof(bd));
        }

        // ---------- Argumentos y entorno (públicos para poder probarlos) ----------

        public static IReadOnlyList<string> ArgumentosDump(ConexionRespaldo c, string destino) => new[]
        {
            "--format=custom", "--no-password", "--verbose", "--file=" + destino,
            "--host=" + c.Host, "--port=" + c.Puerto.ToString(CultureInfo.InvariantCulture), "--username=" + c.Usuario, "--dbname=" + c.Base
        };

        public static IReadOnlyList<string> ArgumentosRestore(ConexionRespaldo c, string archivo) => new[]
        {
            "--clean", "--if-exists", "--no-owner", "--no-privileges", "--single-transaction", "--exit-on-error", "--no-password", "--verbose",
            "--host=" + c.Host, "--port=" + c.Puerto.ToString(CultureInfo.InvariantCulture), "--username=" + c.Usuario, "--dbname=" + c.Base, archivo
        };

        public static IReadOnlyList<string> ArgumentosListar(string archivo) => new[] { "--list", archivo };

        /// <summary>Variables del proceso hijo: la contraseña va solo acá, nunca en los argumentos.</summary>
        public static IReadOnlyDictionary<string, string> Entorno(ConexionRespaldo c, bool esperarBloqueos = false)
        {
            var d = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(c.Clave)) d["PGPASSWORD"] = c.Clave;
            if (!string.IsNullOrEmpty(c.ModoSsl)) d["PGSSLMODE"] = c.ModoSsl;
            // Evita que pg_restore espere sin límite si otra sesión tiene bloqueadas las tablas.
            if (esperarBloqueos) d["PGOPTIONS"] = "-c lock_timeout=" + EsperaBloqueoMs.ToString(CultureInfo.InvariantCulture);
            return d;
        }

        // ---------- IMotorRespaldo ----------

        public async Task<EstadoHerramientas> DetectarAsync(CancellationToken token)
        {
            var h = await ResolverAsync(token).ConfigureAwait(false);
            return new EstadoHerramientas { Disponibles = h.Disponibles, Mensaje = h.Disponibles ? null : h.Mensaje, Version = h.Version };
        }

        private async Task<HerramientasResueltas> ResolverAsync(CancellationToken token)
        {
            var info = await bd.ConsultarAsync(token).ConfigureAwait(false);
            return await localizador.ResolverAsync(info.Conectada ? info.VersionMayor : 0, token).ConfigureAwait(false);
        }

        public async Task<ResultadoMotor> CrearAsync(string archivoDestino, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        {
            var h = await ResolverAsync(token).ConfigureAwait(false);
            if (!h.Disponibles) return Falla(h.Mensaje);
            ConexionRespaldo c;
            try { c = ConexionRespaldo.Desde(cadenaConexion()); }
            catch (Exception) { return Falla("La cadena de conexión de la base no es válida."); }
            ResultadoProceso r;
            try
            {
                r = await ejecutor.EjecutarAsync(new SolicitudProceso
                {
                    Ejecutable = h.PgDump, Argumentos = ArgumentosDump(c, archivoDestino), Entorno = Entorno(c), TiempoMaximo = TiempoMaximoCopia, CapturarSalida = false,
                    AlRecibirLineaError = linea => Informar(progreso, linea, "Exportando")
                }, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return Falla("No se pudo ejecutar pg_dump: " + ex.Message); }
            if (r.TiempoAgotado) return Falla("pg_dump superó el tiempo máximo (" + (int)TiempoMaximoCopia.TotalMinutes + " minutos) y se canceló.");
            if (r.CodigoSalida != 0) return Falla("pg_dump falló (código " + r.CodigoSalida + "). " + Cola(r.Error));
            return new ResultadoMotor { Ok = true };
        }

        public async Task<ResultadoMotor> ListarAsync(string archivo, CancellationToken token)
        {
            var h = await ResolverAsync(token).ConfigureAwait(false);
            if (!h.Disponibles) return Falla(h.Mensaje);
            ResultadoProceso r;
            try
            {
                r = await ejecutor.EjecutarAsync(new SolicitudProceso
                { Ejecutable = h.PgRestore, Argumentos = ArgumentosListar(archivo), TiempoMaximo = TimeSpan.FromMinutes(5) }, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return Falla("No se pudo ejecutar pg_restore: " + ex.Message); }
            if (r.TiempoAgotado) return Falla("pg_restore --list superó el tiempo máximo.");
            if (r.CodigoSalida != 0) return Falla("pg_restore no pudo leer el archivo (código " + r.CodigoSalida + "). " + Cola(r.Error));
            return new ResultadoMotor { Ok = true, Objetos = ContarObjetos(r.Salida) };
        }

        public async Task<ResultadoMotor> RestaurarAsync(string archivo, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        {
            var h = await ResolverAsync(token).ConfigureAwait(false);
            if (!h.Disponibles) return Falla(h.Mensaje);
            ConexionRespaldo c;
            try { c = ConexionRespaldo.Desde(cadenaConexion()); }
            catch (Exception) { return Falla("La cadena de conexión de la base no es válida."); }
            ResultadoProceso r;
            try
            {
                r = await ejecutor.EjecutarAsync(new SolicitudProceso
                {
                    Ejecutable = h.PgRestore, Argumentos = ArgumentosRestore(c, archivo), Entorno = Entorno(c, esperarBloqueos: true), TiempoMaximo = TiempoMaximoCopia, CapturarSalida = false,
                    AlRecibirLineaError = linea => Informar(progreso, linea, "Restaurando")
                }, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return Falla("No se pudo ejecutar pg_restore: " + ex.Message); }
            if (r.TiempoAgotado) return Falla("pg_restore superó el tiempo máximo (" + (int)TiempoMaximoCopia.TotalMinutes + " minutos) y se canceló.");
            if (r.CodigoSalida != 0)
            {
                var texto = r.Error ?? string.Empty;
                var bloqueo = texto.IndexOf("lock timeout", StringComparison.OrdinalIgnoreCase) >= 0 || texto.IndexOf("tiempo de espera", StringComparison.OrdinalIgnoreCase) >= 0
                              || texto.IndexOf("55P03", StringComparison.Ordinal) >= 0;
                return Falla((bloqueo
                    ? "Hay otras sesiones conectadas que bloquean tablas de la base (esperó " + (EsperaBloqueoMs / 1000) + " segundos). Cerrá las demás instancias de la aplicación y reintentá. "
                    : "pg_restore falló (código " + r.CodigoSalida + "). ") + Cola(r.Error));
            }
            return new ResultadoMotor { Ok = true };
        }

        // ---------- Auxiliares ----------

        /// <summary>Líneas de la lista de pg_restore que no son comentarios (las que empiezan con ';').</summary>
        public static int ContarObjetos(string salida)
        {
            if (string.IsNullOrEmpty(salida)) return 0;
            return salida.Split('\n').Select(l => l.Trim()).Count(l => l.Length > 0 && !l.StartsWith(";", StringComparison.Ordinal));
        }

        private static ResultadoMotor Falla(string detalle) => new ResultadoMotor { Ok = false, Detalle = detalle };

        // Últimas líneas de error, sin datos de conexión (por si el motor las reprodujera).
        private static string Cola(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return string.Empty;
            var lineas = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            var relevantes = lineas.Where(l => Relevante.IsMatch(l)).ToList();
            var base4 = relevantes.Count > 0 ? relevantes : lineas;
            var texto = SteeLog.Core.Application.Bitacora.SaneadorTexto.Sanear(string.Join(" ", base4.Skip(Math.Max(0, base4.Count - 4))));
            return texto != null && texto.Length > 600 ? texto.Substring(0, 600) + "..." : texto;
        }

        // Con --verbose la salida trae muchas líneas de avance: se priorizan las que hablan de errores.
        private static readonly System.Text.RegularExpressions.Regex Relevante = new System.Text.RegularExpressions.Regex(
            "error|fatal|fehler|detail|hint|lock|tiempo de espera|permission|denied|cancel",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        private static void Informar(IProgress<ProgresoRespaldo> progreso, string linea, string prefijo)
        {
            if (progreso == null || string.IsNullOrWhiteSpace(linea)) return;
            // Ej.: pg_dump: dumping contents of table "public.materiales"
            var i = linea.IndexOf("table ", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return;
            progreso.Report(new ProgresoRespaldo(prefijo + " " + linea.Substring(i).Trim().Replace("\"", string.Empty), null));
        }
    }
}
