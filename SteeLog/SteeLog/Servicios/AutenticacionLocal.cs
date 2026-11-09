using System.Threading.Tasks;

namespace SteeLog.Servicios
{
    // Puente temporal para formularios legacy; la autenticación es exclusivamente local.
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Email { get; set; }
        public string ErrorMessage { get; set; }
    }

    [System.Obsolete("Fase DI: recibir IServiciosVista por constructor")]
    public static class AutenticacionLocal
    {
        private static AutenticacionVista Servicio => SteeLog.CompositionRoot.ServiciosCompatibles.Obtener().Autenticacion;
        public static async Task<AuthResult> SignInAsync(string email, string password) => await Servicio.SignInAsync(email, password);
    }
}
