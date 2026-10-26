using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Npgsql;

using System.Reflection;

namespace WP_Inventario.Datos
{
    public class ConexionDB
    {
        private readonly string _connectionString;

        public ConexionDB()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

            // Cargar el appsettings.json embebido como base para que el EXE sea portable
            var assembly = Assembly.GetExecutingAssembly();
            // El nombre del recurso suele ser Namespace.NombreArchivo.json
            var resourceName = "WP_Inventario.appsettings.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    builder.AddJsonStream(stream);
                }

                // Permitir que un appsettings.json físico en la misma carpeta sobrescriba la configuración (opcional)
                // Se agrega después del stream para que tenga prioridad
                builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                // IMPORTANTE: Build() debe llamarse dentro del bloque using para que el stream no esté cerrado
                IConfiguration configuration = builder.Build();
                _connectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }

        public NpgsqlConnection GetConnection()
        {
            var conexion = new NpgsqlConnection(_connectionString);

            try
            {
                if (conexion.State == ConnectionState.Closed)
                {
                    conexion.Open();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error de conexión a Supabase: " + ex.Message);
            }

            return conexion;
        }
    }
}