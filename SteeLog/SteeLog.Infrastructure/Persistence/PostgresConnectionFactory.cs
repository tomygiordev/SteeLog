using System;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;
namespace SteeLog.Infrastructure.Persistence
{
    public sealed class PostgresConnectionFactory
    {
        private readonly string connectionString;
        private readonly Func<IdentificacionConexion> identificacion;
        public PostgresConnectionFactory(string connectionString) : this(connectionString, null) { }
        /// <param name="identificacion">Quién usa la conexión (usuario/rol/equipo/versión) para la bitácora; se consulta en cada Create().</param>
        public PostgresConnectionFactory(string connectionString, Func<IdentificacionConexion> identificacion)
        {
            this.identificacion = identificacion;
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Falta la cadena ConnectionStrings:DefaultConnection de PostgreSQL en appsettings.json.");
            try { new NpgsqlConnectionStringBuilder(connectionString); }
            catch (ArgumentException) { throw new InvalidOperationException("La cadena ConnectionStrings:DefaultConnection de PostgreSQL en appsettings.json tiene un formato inválido."); }
            this.connectionString = connectionString;
        }
        private readonly SemaphoreSlim esquemaLock = new SemaphoreSlim(1, 1);
        private bool? tieneImpactoTipo;
        // La capacidad queda fijada por factory: reiniciar la app después de migrar.
        internal async Task<bool> TieneImpactoTipoAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken token)
        {
            await esquemaLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (!tieneImpactoTipo.HasValue)
                    using (var cmd = new NpgsqlCommand(@"SELECT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_schema='public' AND table_name='tipo_movimiento' AND column_name='impacto')", connection, transaction))
                        tieneImpactoTipo = (bool)await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
                return tieneImpactoTipo.Value;
            }
            finally { esquemaLock.Release(); }
        }
        public NpgsqlConnection Create() { return new NpgsqlConnection(CadenaIdentificada()); }

        // La identidad viaja como parámetros de arranque (Options = "-c app.usuario=...") y no como set_config tras abrir:
        // 1) es atómica con la apertura y alcanza también a las transacciones; 2) Npgsql agrupa los pools por cadena de
        // conexión, así que un usuario nunca reutiliza una conexión identificada como otro; 3) si la identificación falla
        // al armarse se usa la cadena original y la conexión sirve igual.
        internal string CadenaIdentificada()
        {
            if (identificacion == null) return connectionString;
            try { return IdentificacionConexion.Aplicar(connectionString, identificacion()); }
            catch { return connectionString; }
        }
    }
}
