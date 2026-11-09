using SteeLog.Core.Application.Identidad;

namespace SteeLog.Servicios
{
    public enum UserRole { None = 0, Galpon = 1, Admin = 2 }

    public sealed class SesionVista
    {
        private readonly Session Session;
        public SesionVista(Session session) { Session = session ?? throw new System.ArgumentNullException(nameof(session)); }
        public string Email => Session.Current?.Email;
        // Misma instancia que Session.Current: permite detectar cambios de sesión por referencia.
        public object Identidad => Session.Current;
        public string Username => Email == null ? null : Email.Split('@')[0];
        public UserRole Rol => Role;
        public UserRole Role => (UserRole)Session.Role;
        public bool IsLoggedIn => Session.IsAuthenticated;
        public bool DebeOcultarCantidadesStockMateriales => Role != UserRole.Admin;
        public void SetSession(string email)
        {
            var normalized = (email ?? "").Trim().ToLowerInvariant();
            if (normalized != "admin@tesis.local" && normalized != "galpon@tesis.local")
                throw new System.InvalidOperationException("Identidad demo desconocida.");
            Session.Start(new Identity(normalized, normalized == "admin@tesis.local" ? SteeLog.Core.Application.Identidad.Role.Admin : SteeLog.Core.Application.Identidad.Role.Galpon));
        }
        public void Clear() => Session.Clear();
        public string ObtenerEstadoStockMaterial(decimal stockActual) => stockActual > 0 ? "Hay stock" : "Sin stock";
    }
}
