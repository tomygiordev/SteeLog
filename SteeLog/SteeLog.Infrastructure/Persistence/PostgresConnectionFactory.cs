using System;
using Npgsql;
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
        public NpgsqlConnection Create() { return new NpgsqlConnection(connectionString); }
    }
}
