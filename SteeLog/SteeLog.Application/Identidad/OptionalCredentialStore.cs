using System;

namespace SteeLog.Core.Application.Identidad
{
    // Recordar sesión es opcional: no debe impedir el acceso ni el cierre de sesión.
    public sealed class OptionalCredentialStore : ICredentialStore
    {
        private readonly ICredentialStore inner;

        public OptionalCredentialStore(ICredentialStore inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void Save(string username, string password)
        {
            try { inner.Save(username, password); }
            catch (Exception) { }
        }

        public (string username, string password) Load()
        {
            try { return inner.Load(); }
            catch (Exception) { return (null, null); }
        }

        public void Clear()
        {
            try { inner.Clear(); }
            catch (Exception) { }
        }
    }
}
