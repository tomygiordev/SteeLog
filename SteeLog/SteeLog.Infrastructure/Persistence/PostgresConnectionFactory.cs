using System;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;
namespace SteeLog.Infrastructure.Persistence
{
    public sealed class PostgresConnectionFactory
    {
        private readonly string connectionString;
        public PostgresConnectionFactory(string connectionString)
        {
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
        public NpgsqlConnection Create() { return new NpgsqlConnection(connectionString); }
    }
}
