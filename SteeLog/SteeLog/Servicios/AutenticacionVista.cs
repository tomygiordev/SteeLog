using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Servicios
{
    public sealed class AutenticacionVista
    {
        private readonly IAuthentication authentication;
        public AutenticacionVista(IAuthentication authentication) { this.authentication = authentication ?? throw new System.ArgumentNullException(nameof(authentication)); }
        public async Task<AuthResult> SignInAsync(string email, string password)
        {
            var identity = await authentication.AuthenticateAsync(email, password);
            return identity == null
                ? new AuthResult { ErrorMessage = "Usuario o contraseña incorrectos." }
                : new AuthResult { Success = true, Email = identity.Email };
        }
    }
}
