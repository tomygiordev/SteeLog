using SteeLog.CompositionRoot;

namespace SteeLog.Servicios
{
    public static class CredentialStore
    {
        public static void Save(string email, string password)
        {
            try { LocalServices.Current.Credentials.Save(email, password); }
            catch { }
        }
        public static (string email, string password) Load()
        {
            try { var value = LocalServices.Current.Credentials.Load(); return (value.username, value.password); }
            catch { return (null, null); }
        }
        public static void Clear()
        {
            try { LocalServices.Current.Credentials.Clear(); }
            catch { }
        }
    }
}
