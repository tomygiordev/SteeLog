using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WP_Inventario.Servicios
{
    public static class CredentialStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SteeLog", "session.dat");

        public static void Save(string email, string password)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string data = email + "\n" + password;
                byte[] encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(data),
                    null,
                    DataProtectionScope.CurrentUser);
                File.WriteAllBytes(FilePath, encrypted);
            }
            catch { }
        }

        public static (string email, string password) Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return (null, null);
                byte[] encrypted = File.ReadAllBytes(FilePath);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                string data = Encoding.UTF8.GetString(decrypted);
                var parts = data.Split(new[] { '\n' }, 2);
                if (parts.Length == 2)
                    return (parts[0], parts[1]);
            }
            catch { }
            return (null, null);
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch { }
        }
    }
}
