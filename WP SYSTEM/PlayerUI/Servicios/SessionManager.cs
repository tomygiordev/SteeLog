namespace WP_Inventario.Servicios
{
    using System;

    public enum UserRole
    {
        None = 0,
        Galpon = 1,
        Admin = 2
    }

    public static class SessionManager
    {
        // Lista de emails con rol Admin - el resto es Galpon
        private static readonly string[] AdminEmails =
        {
            "admin@steelog.local",
            "admin@gmail.com",
            "tomasroldangiorgi@gmail.com",
            "admin@tesis.local",
            "admin"
        };

        public static string Email { get; private set; }
        public static string Username { get; private set; }
        public static UserRole Role { get; private set; } = UserRole.None;
        public static bool IsLoggedIn { get; private set; }
        public static bool DebeOcultarCantidadesStockMateriales => Role != UserRole.Admin;

        public static string AccessToken { get; private set; }
        public static string RefreshToken { get; private set; }
        public static int ExpiresIn { get; private set; }
        public static DateTime TokenExpiresAtUtc { get; private set; }

        public static void SetSession(string email)
        {
            SetSession(email, null, null, 0);
        }

        public static void SetSession(string email, string accessToken, string refreshToken, int expiresIn)
        {
            Email = (email ?? string.Empty).ToLower().Trim();
            Username = Email.Contains("@") ? Email.Split('@')[0] : Email;
            Role = IsAdmin(Email) ? UserRole.Admin : UserRole.Galpon;
            IsLoggedIn = true;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
            TokenExpiresAtUtc = expiresIn > 0 ? DateTime.UtcNow.AddSeconds(expiresIn) : DateTime.MinValue;
        }

        public static bool TokenPorVencer()
        {
            return string.IsNullOrWhiteSpace(AccessToken)
                || TokenExpiresAtUtc == DateTime.MinValue
                || DateTime.UtcNow >= TokenExpiresAtUtc.AddMinutes(-2);
        }

        public static void ActualizarTokens(string accessToken, string refreshToken, int expiresIn)
        {
            AccessToken = accessToken;
            RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? RefreshToken : refreshToken;
            ExpiresIn = expiresIn;
            TokenExpiresAtUtc = expiresIn > 0 ? DateTime.UtcNow.AddSeconds(expiresIn) : DateTime.MinValue;
        }

        public static void Clear()
        {
            Email = null;
            Username = null;
            Role = UserRole.None;
            IsLoggedIn = false;
            AccessToken = null;
            RefreshToken = null;
            ExpiresIn = 0;
            TokenExpiresAtUtc = DateTime.MinValue;
        }

        private static bool IsAdmin(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string clean = email.ToLower().Trim();
            foreach (var adminEmail in AdminEmails)
            {
                if (adminEmail.ToLower().Trim() == clean)
                    return true;
            }
            return false;
        }

        public static string ObtenerEstadoStockMaterial(decimal stockActual)
        {
            return stockActual > 0 ? "Hay stock" : "Sin stock";
        }
    }
}
