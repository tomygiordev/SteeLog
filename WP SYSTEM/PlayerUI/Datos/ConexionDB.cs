using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WP_Inventario.Datos
{
    public class ConexionDB
    {
        private readonly string _connectionString;

        public ConexionDB()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream("WP_Inventario.appsettings.json"))
            {
                if (stream != null)
                    builder.AddJsonStream(stream);

                builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                IConfigurationRoot configuration;
                try
                {
                    configuration = builder.Build();
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidDataException)
                {
                    throw new InvalidOperationException("No se pudo leer la configuración JSON de PostgreSQL (appsettings.json).");
                }

                try
                {
                    _connectionString = configuration.GetConnectionString("DefaultConnection");
                }
                finally
                {
                    (configuration as IDisposable)?.Dispose();
                }
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Falta la cadena ConnectionStrings:DefaultConnection de PostgreSQL en appsettings.json.");

            try
            {
                new NpgsqlConnectionStringBuilder(_connectionString);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("La cadena ConnectionStrings:DefaultConnection de PostgreSQL en appsettings.json tiene un formato inválido.");
            }
        }

        public NpgsqlConnection GetConnection()
        {
            NpgsqlConnection conexion = null;
            try
            {
                conexion = new NpgsqlConnection(_connectionString);
                conexion.Open();
                return conexion;
            }
            catch (Exception ex)
            {
                conexion?.Dispose();
                throw new InvalidOperationException("Error al abrir la conexión PostgreSQL.", ex);
            }
        }
    }
}
