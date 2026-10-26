namespace WP_Inventario.Servicios
{
    using System;

    public enum UserRole
    {
        Admin,
        Galpon
    }

    public static class SessionManager
    {
        // Lista de emails con rol Admin â€” el resto es Galpon
        private static readonly string[] AdminEmails =
        {
            "admin@steelog.local",
            "admin@gmail.com",
            "tomasroldangiorgi@gmail.com",
        };

        public static string Email { get; private set; }
        public static string Username { get; private set; }
        public static UserRole Role { get; private set; }
        public static bool IsLoggedIn { get; private set; }
        public static bool DebeOcultarCantidadesStockMateriales => Role == UserRole.Galpon;

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
            Email = email.ToLower().Trim();
            Username = email.Contains("@") ? email.Split('@')[0] : email;
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
            IsLoggedIn = false;
            AccessToken = null;
            RefreshToken = null;
            ExpiresIn = 0;
            TokenExpiresAtUtc = DateTime.MinValue;
        }

        private static bool IsAdmin(string email)
        {
            foreach (var adminEmail in AdminEmails)
            {
                if (adminEmail.ToLower().Trim() == email)
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

