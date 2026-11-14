using System;
using System.IO;
using Microsoft.Extensions.Configuration;
namespace SteeLog.Infrastructure.Configuration
{
    public sealed class LocalConfiguration
    {
        public string ConnectionString { get; }
        /// <summary>Carpeta opcional con pg_dump/pg_restore (clave Respaldos:CarpetaHerramientas); null si no se configuró.</summary>
        public string CarpetaHerramientasRespaldo { get; }
        private LocalConfiguration(string connectionString, string carpetaHerramientas) { ConnectionString = connectionString; CarpetaHerramientasRespaldo = carpetaHerramientas; }
        public static LocalConfiguration Load(string executableDirectory)
        {
            return Load(executableDirectory, null);
        }

        public static LocalConfiguration Load(string executableDirectory, Func<Stream> fallback)
        {
            if (string.IsNullOrWhiteSpace(executableDirectory) || !Path.IsPathRooted(executableDirectory))
                throw new ArgumentException("Se requiere un directorio absoluto.", nameof(executableDirectory));
            IConfigurationRoot root;
            try
            {
                using (var stream = fallback?.Invoke())
                {
                    var builder = new ConfigurationBuilder().SetBasePath(executableDirectory);
                    if (stream != null) builder.AddJsonStream(stream);
                    builder.AddJsonFile("appsettings.json", optional: stream != null, reloadOnChange: false);
                    root = builder.Build();
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidDataException || ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                throw new InvalidOperationException("No se pudo leer la configuración JSON de PostgreSQL (appsettings.json).");
            }
            try { return new LocalConfiguration(root.GetConnectionString("DefaultConnection"), root["Respaldos:CarpetaHerramientas"]); }
            finally { (root as IDisposable)?.Dispose(); }
        }
    }
}
