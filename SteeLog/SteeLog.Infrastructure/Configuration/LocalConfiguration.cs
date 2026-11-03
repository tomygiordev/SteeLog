using System;
using System.IO;
using Microsoft.Extensions.Configuration;
namespace SteeLog.Infrastructure.Configuration
{
    public sealed class LocalConfiguration
    {
        public string ConnectionString { get; }
        private LocalConfiguration(string connectionString) { ConnectionString = connectionString; }
        public static LocalConfiguration Load(string executableDirectory)
        {
            if (string.IsNullOrWhiteSpace(executableDirectory) || !Path.IsPathRooted(executableDirectory)) throw new ArgumentException("Se requiere un directorio absoluto.", nameof(executableDirectory));
            IConfigurationRoot root;
            try { root = new ConfigurationBuilder().SetBasePath(executableDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build(); }
            catch (Exception ex) when (ex is FormatException || ex is InvalidDataException || ex is FileNotFoundException || ex is DirectoryNotFoundException) { throw new InvalidOperationException("No se pudo leer la configuración JSON de PostgreSQL (appsettings.json)."); }
            try { return new LocalConfiguration(root.GetConnectionString("DefaultConnection")); }
            finally { (root as IDisposable)?.Dispose(); }
        }
    }
}
