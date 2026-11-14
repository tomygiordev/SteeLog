using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Bitacora
{
    /// <summary>Guarda eventos de la app (origen 'APP') en public.bitacora_auditoria. No lanza excepciones.</summary>
    public sealed class NpgsqlBitacoraEscritura : IBitacoraEscritura
    {
        // No se insertan dos veces los mismos datos: protege de reenviar un evento ya guardado si la app se cortó antes de quitarlo de la cola.
        public const string SqlInsertar =
            "INSERT INTO public.bitacora_auditoria (fecha, usuario, rol, equipo, version_app, origen, accion, entidad, descripcion) " +
            "SELECT @fecha, @usuario, @rol, @equipo, @version, 'APP', @accion, @entidad, @descripcion " +
            "WHERE NOT EXISTS (SELECT 1 FROM public.bitacora_auditoria WHERE origen = 'APP' AND fecha = @fecha AND accion = @accion " +
            "AND usuario IS NOT DISTINCT FROM @usuario AND descripcion IS NOT DISTINCT FROM @descripcion)";

        private readonly Func<PostgresConnectionFactory> factory;
        public NpgsqlBitacoraEscritura(PostgresConnectionFactory factory) : this(() => factory) { if (factory == null) throw new ArgumentNullException(nameof(factory)); }
        /// <summary>La fábrica se obtiene recién al escribir (la configuración puede cargarse de forma diferida).</summary>
        public NpgsqlBitacoraEscritura(Func<PostgresConnectionFactory> factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }

        public async Task<IReadOnlyCollection<Guid>> InsertarAsync(IReadOnlyList<EventoBitacoraApp> eventos, CancellationToken token = default)
        {
            var hechos = new List<Guid>();
            if (eventos == null || eventos.Count == 0) return hechos;
            try
            {
                using (var c = factory().Create())
                {
                    await c.OpenAsync(token).ConfigureAwait(false);
                    foreach (var e in eventos)
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand(SqlInsertar, c))
                            {
                                Parametro(cmd, "fecha", NpgsqlDbType.TimestampTz, e.Fecha.UtcDateTime);
                                Parametro(cmd, "usuario", NpgsqlDbType.Text, e.Usuario);
                                Parametro(cmd, "rol", NpgsqlDbType.Text, e.Rol);
                                Parametro(cmd, "equipo", NpgsqlDbType.Text, e.Equipo);
                                Parametro(cmd, "version", NpgsqlDbType.Text, e.Version);
                                Parametro(cmd, "accion", NpgsqlDbType.Text, e.Accion ?? "");
                                Parametro(cmd, "entidad", NpgsqlDbType.Text, e.Entidad);
                                Parametro(cmd, "descripcion", NpgsqlDbType.Text, e.Descripcion);
                                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                            }
                            hechos.Add(e.Id);
                        }
                        // Dato que la base rechaza: se descarta para no trabar la cola. Tabla/columna ausente: se conserva y se corta.
                        catch (PostgresException ex) when (ex.SqlState != "42P01" && ex.SqlState != "42703" && !EsTransitorio(ex.SqlState)) { hechos.Add(e.Id); }
                    }
                }
            }
            catch (Exception) { /* sin conexión o tabla ausente: quedan pendientes los que no figuren en hechos */ }
            return hechos;
        }

        // Clases 08 (conexión), 53 (recursos), 57 (apagado/cancelación), 40 (rollback/deadlock), 25 (transacción inválida).
        private static bool EsTransitorio(string sqlState) => sqlState != null && sqlState.Length >= 2 &&
            (sqlState.StartsWith("08") || sqlState.StartsWith("53") || sqlState.StartsWith("57") || sqlState.StartsWith("40") || sqlState.StartsWith("25"));

        private static void Parametro(NpgsqlCommand cmd, string nombre, NpgsqlDbType tipo, object valor)
            => cmd.Parameters.Add(new NpgsqlParameter(nombre, tipo) { Value = valor ?? DBNull.Value });
    }
}
