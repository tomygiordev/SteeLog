using System.Threading.Tasks;
using SteeLog.CompositionRoot;

namespace SteeLog.Servicios
{
    // Puente temporal para formularios legacy; la autenticación es exclusivamente local.
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Email { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class AutenticacionLocal
    {
        public static async Task<AuthResult> SignInAsync(string email, string password)
        {
            var identity = await LocalServices.Current.Authentication.AuthenticateAsync(email, password);
            return identity == null
                ? new AuthResult { ErrorMessage = "Usuario o contraseña incorrectos." }
                : new AuthResult { Success = true, Email = identity.Email };
        }
    }
}
