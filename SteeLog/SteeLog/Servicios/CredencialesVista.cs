namespace SteeLog.Servicios
{
    public sealed class CredencialesVista
    {
        private readonly SteeLog.Core.Application.Identidad.ICredentialStore credentials;
        public CredencialesVista(SteeLog.Core.Application.Identidad.ICredentialStore credentials) { this.credentials = credentials ?? throw new System.ArgumentNullException(nameof(credentials)); }
        public void Save(string email, string password)
        {
            try { credentials.Save(email, password); }
            catch { }
        }
        public (string email, string password) Load()
        {
            try { var value = credentials.Load(); return (value.username, value.password); }
            catch { return (null, null); }
        }
        public void Clear()
        {
            try { credentials.Clear(); }
            catch { }
        }
    }
}
