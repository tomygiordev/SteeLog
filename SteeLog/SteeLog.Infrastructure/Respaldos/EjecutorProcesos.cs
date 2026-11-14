using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteeLog.Infrastructure.Respaldos
{
    public sealed class SolicitudProceso
    {
        public string Ejecutable { get; set; }
        /// <summary>Argumentos ya separados (el ejecutor se ocupa de entrecomillarlos). Nunca incluir contraseñas.</summary>
        public IReadOnlyList<string> Argumentos { get; set; } = new List<string>();
        /// <summary>Variables de entorno adicionales del proceso hijo (acá viaja PGPASSWORD).</summary>
        public IReadOnlyDictionary<string, string> Entorno { get; set; } = new Dictionary<string, string>();
        public TimeSpan TiempoMaximo { get; set; } = TimeSpan.FromMinutes(30);
        /// <summary>Se invoca por cada línea de la salida de error (avance de pg_dump / pg_restore con --verbose).</summary>
        public Action<string> AlRecibirLineaError { get; set; }
        /// <summary>Si es false no se guarda la salida estándar (la salida de error siempre se guarda, acotada).</summary>
        public bool CapturarSalida { get; set; } = true;
    }

    public sealed class ResultadoProceso
    {
        public int CodigoSalida { get; set; }
        public string Salida { get; set; } = string.Empty;
        /// <summary>Final de la salida de error.</summary>
        public string Error { get; set; } = string.Empty;
        public bool TiempoAgotado { get; set; }
    }

    public interface IEjecutorProcesos
    {
        /// <summary>Si se cancela el token, mata el proceso y lanza OperationCanceledException. Si no puede iniciarlo, lanza.</summary>
        Task<ResultadoProceso> EjecutarAsync(SolicitudProceso solicitud, CancellationToken token);
    }

    /// <summary>Ejecuta un proceso sin ventana, capturando la salida; cancelable y con tiempo máximo.</summary>
    public sealed class EjecutorProcesos : IEjecutorProcesos
    {
        private const int MaximoSalida = 8 * 1024 * 1024;
        private const int MaximoError = 16 * 1024;

        public async Task<ResultadoProceso> EjecutarAsync(SolicitudProceso solicitud, CancellationToken token)
        {
            if (solicitud == null || string.IsNullOrWhiteSpace(solicitud.Ejecutable)) throw new ArgumentException("Falta el ejecutable.", nameof(solicitud));
            token.ThrowIfCancellationRequested();
            var info = new ProcessStartInfo
            {
                FileName = solicitud.Ejecutable,
                Arguments = ArmarArgumentos(solicitud.Argumentos),
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            foreach (var par in solicitud.Entorno) info.EnvironmentVariables[par.Key] = par.Value;

            var salida = new StringBuilder(); var error = new StringBuilder();
            var terminado = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var proceso = new Process { StartInfo = info, EnableRaisingEvents = true })
            {
                proceso.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null || !solicitud.CapturarSalida) return;
                    lock (salida) if (salida.Length < MaximoSalida) salida.AppendLine(e.Data);
                };
                proceso.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    lock (error)
                    {
                        error.AppendLine(e.Data);
                        if (error.Length > MaximoError) error.Remove(0, error.Length - MaximoError);
                    }
                    try { solicitud.AlRecibirLineaError?.Invoke(e.Data); } catch { }
                };
                proceso.Exited += (s, e) => terminado.TrySetResult(true);
                proceso.Start();
                try { proceso.StandardInput.Close(); } catch { }
                proceso.BeginOutputReadLine(); proceso.BeginErrorReadLine();

                var cancelado = false; var agotado = false;
                using (token.Register(() => { cancelado = true; Matar(proceso); }))
                using (var limite = new CancellationTokenSource(solicitud.TiempoMaximo))
                using (limite.Token.Register(() => { agotado = true; Matar(proceso); }))
                {
                    if (proceso.HasExited) terminado.TrySetResult(true);
                    await terminado.Task.ConfigureAwait(false);
                    // Vacía las salidas asincrónicas pendientes. Si se mató el proceso no se espera el cierre de las tuberías
                    // (un proceso nieto que las heredó podría retenerlas).
                    var espera = cancelado || agotado ? 2000 : Timeout.Infinite;
                    await Task.Run(() => proceso.WaitForExit(espera)).ConfigureAwait(false);
                }
                if (cancelado) throw new OperationCanceledException(token);
                var resultado = new ResultadoProceso { CodigoSalida = proceso.ExitCode, TiempoAgotado = agotado };
                lock (salida) resultado.Salida = salida.ToString();
                lock (error) resultado.Error = error.ToString();
                return resultado;
            }
        }

        private static void Matar(Process p)
        {
            try { if (!p.HasExited) p.Kill(); } catch { }
        }

        /// <summary>Entrecomilla según las reglas de CommandLineToArgvW.</summary>
        public static string ArmarArgumentos(IReadOnlyList<string> argumentos)
        {
            if (argumentos == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var a in argumentos)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Entrecomillar(a ?? string.Empty));
            }
            return sb.ToString();
        }

        public static string Entrecomillar(string a)
        {
            if (a.Length > 0 && a.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0) return a;
            var sb = new StringBuilder("\"");
            var barras = 0;
            foreach (var c in a)
            {
                if (c == '\\') { barras++; continue; }
                if (c == '"') { sb.Append('\\', barras * 2 + 1).Append('"'); barras = 0; continue; }
                sb.Append('\\', barras).Append(c); barras = 0;
            }
            sb.Append('\\', barras * 2).Append('"');
            return sb.ToString();
        }
    }
}
