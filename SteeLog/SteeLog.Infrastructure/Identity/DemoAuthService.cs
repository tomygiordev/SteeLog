using System;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using UserIdentity = SteeLog.Core.Application.Identidad.Identity;

namespace SteeLog.Infrastructure.Identity
{
    public sealed class DemoAuthService : IAuthentication
    {
        public Task<UserIdentity> AuthenticateAsync(string username, string password)
        {
            var user = (username ?? "").Trim().ToLowerInvariant();
            var pass = (password ?? "").Trim();
            var admin = user == "admin" || user == "admin@tesis.local" || user == "admin@steelog.local" || user == "admin@gmail.com";
            var galpon = user == "galpon" || user == "galpon@tesis.local" || user == "operador" || user == "operador@tesis.local" || user == "invitado@tesis.local";
            UserIdentity result = null;
            if (admin && (pass == "123456" || pass == "admin")) result = new UserIdentity("admin@tesis.local", Role.Admin);
            if (galpon && (pass == "123456" || pass == "galpon" || pass == "operador")) result = new UserIdentity("galpon@tesis.local", Role.Galpon);
            return Task.FromResult(result);
        }
    }
}
