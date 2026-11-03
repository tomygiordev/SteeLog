using System;
using System.IO;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Common;
namespace SteeLog.Infrastructure.Files
{
    public sealed class LocalPhotoStorage : IPhotoStorage
    {
        private readonly string root;
        public LocalPhotoStorage(string root) { this.root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; }
        private string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("La ruta de la foto está vacía.");
            var resolved = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
            if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Ruta de foto inválida.");
            return resolved;
        }
        public Task<bool> WriteAsync(string path, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > 8L * 1024 * 1024) throw new ArgumentException("Tamaño de foto inválido.");
            var target = Resolve(path);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.WriteAllBytes(target, bytes);
            return Task.FromResult(true);
        }
        public Task<byte[]> ReadAsync(string path) { var target = Resolve(path); return Task.FromResult(File.Exists(target) ? File.ReadAllBytes(target) : new byte[0]); }
        public Task<bool> DeleteAsync(string path) { var target = Resolve(path); if (File.Exists(target)) File.Delete(target); return Task.FromResult(true); }
    }
}
