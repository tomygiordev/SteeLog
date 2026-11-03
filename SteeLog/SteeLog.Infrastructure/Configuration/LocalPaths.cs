using System;
using System.IO;
namespace SteeLog.Infrastructure.Configuration
{
    public sealed class LocalPaths
    {
        public string ExecutableDirectory { get; }
        public string StorageDirectory { get; }
        public LocalPaths(string executableDirectory, string localApplicationData)
        {
            if (string.IsNullOrWhiteSpace(executableDirectory) || !Path.IsPathRooted(executableDirectory)) throw new ArgumentException("Se requiere un directorio absoluto.", nameof(executableDirectory));
            if (string.IsNullOrWhiteSpace(localApplicationData) || !Path.IsPathRooted(localApplicationData)) throw new ArgumentException("Se requiere un directorio absoluto.", nameof(localApplicationData));
            ExecutableDirectory = executableDirectory;
            StorageDirectory = Path.Combine(localApplicationData, "SteeLog", "Storage");
        }
    }
}
