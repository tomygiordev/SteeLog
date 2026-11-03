using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Infrastructure.Identity
{
    public sealed class DpapiCredentialStore : ICredentialStore
    {
        private readonly string path;
        public DpapiCredentialStore(string path) { this.path = path ?? throw new ArgumentNullException(nameof(path)); }
        public void Save(string username, string password)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes = Encoding.UTF8.GetBytes(username + "\n" + password);
            File.WriteAllBytes(path, ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
        }
        public (string username, string password) Load()
        {
            if (!File.Exists(path)) return (null, null);
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            var parts = Encoding.UTF8.GetString(bytes).Split(new[] { '\n' }, 2);
            return parts.Length == 2 ? (parts[0], parts[1]) : (null, null);
        }
        public void Clear() { if (File.Exists(path)) File.Delete(path); }
    }
}
