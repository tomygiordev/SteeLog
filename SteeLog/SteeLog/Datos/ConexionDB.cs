using System;
using Npgsql;
using SteeLog.CompositionRoot;

namespace SteeLog.Datos
{
    public class ConexionDB
    {
        private readonly string _connectionString;

        public ConexionDB()
        {
            _connectionString = LocalServices.Current.ConnectionString;
            // La validación y el almacenamiento de la fábrica son únicos por proceso.
            LocalServices.Current.EnsureConnectionFactory();
        }

        public NpgsqlConnection GetConnection()
        {
            NpgsqlConnection connection = null;
            try
            {
                connection = LocalServices.Current.CreateConnection();
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                connection?.Dispose();
                throw new InvalidOperationException("Error al abrir la conexión PostgreSQL.", ex);
            }
        }
    }
}
