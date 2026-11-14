using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Respaldos;

namespace SteeLog.Infrastructure.Respaldos
{
    /// <summary>Consultas de apoyo a las copias: versión del servidor, conteo de filas y cierre de conexiones agrupadas.</summary>
    public sealed class NpgsqlBaseRespaldo : IBaseRespaldo
    {
        private static readonly Regex NombreTabla = new Regex("^[a-z_][a-z0-9_]*$", RegexOptions.CultureInvariant);
        private readonly Func<string> cadenaConexion;
        public NpgsqlBaseRespaldo(Func<string> cadenaConexion) { this.cadenaConexion = cadenaConexion ?? throw new ArgumentNullException(nameof(cadenaConexion)); }

        // Sin agrupación ni identificación de sesión: son consultas puntuales y no dejan conexiones abiertas.
        private NpgsqlConnection Crear()
        {
            var b = new NpgsqlConnectionStringBuilder(cadenaConexion()) { Pooling = false, Timeout = 8, CommandTimeout = 30 };
            return new NpgsqlConnection(b.ConnectionString);
        }

        public async Task<InfoBase> ConsultarAsync(CancellationToken token)
        {
            try
            {
                using (var c = Crear())
                {
                    await c.OpenAsync(token).ConfigureAwait(false);
                    using (var q = new NpgsqlCommand("SELECT current_setting('server_version_num'), current_setting('server_version'), current_database()", c))
                    using (var r = await q.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        await r.ReadAsync(token).ConfigureAwait(false);
                        var numero = int.Parse(r.GetString(0), CultureInfo.InvariantCulture);
                        var version = r.GetString(1); var espacio = version.IndexOf(' ');
                        return new InfoBase
                        {
                            Conectada = true, Nombre = r.GetString(2), Version = espacio > 0 ? version.Substring(0, espacio) : version,
                            VersionMayor = numero / 10000
                        };
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return new InfoBase { Conectada = false, Mensaje = "No se pudo conectar a la base de datos: " + SaneadorTexto.Sanear(ex.Message) };
            }
        }

        public async Task<IReadOnlyDictionary<string, long>> ContarFilasAsync(IReadOnlyList<string> tablas, CancellationToken token)
        {
            var resultado = new Dictionary<string, long>();
            using (var c = Crear())
            {
                await c.OpenAsync(token).ConfigureAwait(false);
                foreach (var tabla in tablas)
                {
                    if (!NombreTabla.IsMatch(tabla)) throw new ArgumentException("Nombre de tabla no permitido.", nameof(tablas));
                    try
                    {
                        using (var q = new NpgsqlCommand("SELECT count(*) FROM public.\"" + tabla + "\"", c))
                            resultado[tabla] = Convert.ToInt64(await q.ExecuteScalarAsync(token).ConfigureAwait(false), CultureInfo.InvariantCulture);
                    }
                    catch (PostgresException ex) when (ex.SqlState == "42P01") { /* la tabla no existe: se omite */ }
                }
            }
            return resultado;
        }

        public async Task<bool> RestauracionEsperaBloqueoAsync(CancellationToken token)
        {
            using (var c = Crear())
            {
                await c.OpenAsync(token).ConfigureAwait(false);
                using (var q = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid() " +
                    "AND application_name = 'pg_restore' AND wait_event_type = 'Lock')", c))
                    return (bool)await q.ExecuteScalarAsync(token).ConfigureAwait(false);
            }
        }

        public void LiberarConexiones() => NpgsqlConnection.ClearAllPools();
    }
}
