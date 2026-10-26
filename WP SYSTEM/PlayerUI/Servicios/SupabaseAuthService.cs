using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WP_Inventario.Servicios
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Email { get; set; }
        public string ErrorMessage { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }

    public static class SupabaseAuthService
    {
        public const string SupabaseUrl = "http://localhost:54321";
        public const string AnonKey = "steelog-local-anon-key";

        public static async Task<AuthResult> SignInAsync(string email, string password)
        {
            // Soporte offline / demo local para tesis
            string emailLower = (email ?? "").ToLower().Trim();
            string pass = (password ?? "").Trim();

            bool esAdmin = emailLower == "admin" || emailLower == "admin@tesis.local" || emailLower == "admin@steelog.local" || emailLower == "admin@gmail.com";
            bool esGalpon = emailLower == "galpon" || emailLower == "galpon@tesis.local" || emailLower == "operador" || emailLower == "invitado@tesis.local";

            if (esAdmin || esGalpon || emailLower.Contains("tesis"))
            {
                if (string.IsNullOrEmpty(pass))
                {
                    return new AuthResult { Success = false, ErrorMessage = "Ingrese una contraseña." };
                }

                string emailFinal = emailLower.Contains("@") ? emailLower : (esAdmin ? "admin@tesis.local" : "galpon@tesis.local");

                return new AuthResult
                {
                    Success = true,
                    Email = emailFinal,
                    AccessToken = "local-tesis-token",
                    RefreshToken = "local-tesis-refresh-token",
                    ExpiresIn = 86400
                };
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.Add("apikey", AnonKey);

                    var body = JsonSerializer.Serialize(new { email = email, password = password });
                    var content = new StringContent(body, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        SupabaseUrl + "/auth/v1/token?grant_type=password", content);

                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMsg = "Credenciales incorrectas.";
                        try
                        {
                            using (var json = JsonDocument.Parse(responseBody))
                            {
                                if (json.RootElement.TryGetProperty("error_description", out var desc))
                                    errorMsg = desc.GetString();
                                else if (json.RootElement.TryGetProperty("msg", out var msg))
                                    errorMsg = msg.GetString();
                            }
                        }
                        catch { }

                        return new AuthResult { Success = false, ErrorMessage = errorMsg };
                    }

                    using (var json = JsonDocument.Parse(responseBody))
                    {
                        var root = json.RootElement;
                        string userEmail = "";
                        if (root.TryGetProperty("user", out var user))
                            user.TryGetProperty("email", out var emailProp);

                        if (root.TryGetProperty("user", out var userObj) &&
                            userObj.TryGetProperty("email", out var emailEl))
                        {
                            userEmail = emailEl.GetString() ?? email;
                        }
                        else
                        {
                            userEmail = email;
                        }

                        string accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
                        string refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
                        int expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 0;

                        return new AuthResult
                        {
                            Success = true,
                            Email = userEmail,
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            ExpiresIn = expiresIn
                        };
                    }
                }
            }
            catch (Exception)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "No se pudo contactar el servicio de autenticación. Utilice credenciales demo locales (admin / galpon)."
                };
            }
        }

        public static async Task<AuthResult> RefreshSessionAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "No hay refresh token disponible. Inicie sesión nuevamente."
                };
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.Add("apikey", AnonKey);

                    var body = JsonSerializer.Serialize(new { refresh_token = refreshToken });
                    var content = new StringContent(body, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        SupabaseUrl + "/auth/v1/token?grant_type=refresh_token", content);

                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMsg = "La sesión venció. Inicie sesión nuevamente.";
                        try
                        {
                            using (var json = JsonDocument.Parse(responseBody))
                            {
                                if (json.RootElement.TryGetProperty("error_description", out var desc))
                                    errorMsg = desc.GetString();
                                else if (json.RootElement.TryGetProperty("msg", out var msg))
                                    errorMsg = msg.GetString();
                            }
                        }
                        catch { }

                        return new AuthResult { Success = false, ErrorMessage = errorMsg };
                    }

                    using (var json = JsonDocument.Parse(responseBody))
                    {
                        var root = json.RootElement;
                        string accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
                        string nuevoRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : refreshToken;
                        int expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 0;

                        return new AuthResult
                        {
                            Success = true,
                            Email = SessionManager.Email,
                            AccessToken = accessToken,
                            RefreshToken = nuevoRefreshToken,
                            ExpiresIn = expiresIn
                        };
                    }
                }
            }
            catch (Exception)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "No se pudo refrescar la sesión contra el servidor."
                };
            }
        }
    }
}
