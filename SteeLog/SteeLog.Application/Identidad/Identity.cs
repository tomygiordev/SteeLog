using System;
using System.Threading.Tasks;

namespace SteeLog.Core.Application.Identidad
{
    public enum Role { None, Galpon, Admin }

    public sealed class Identity
    {
        public string Email { get; }
        public Role Role { get; }
        public Identity(string email, Role role)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Identidad inválida.", nameof(email));
            if (role != Role.Admin && role != Role.Galpon) throw new ArgumentException("Rol inválido.", nameof(role));
            Email = email;
            Role = role;
        }
    }

    public interface IAuthentication
    {
        Task<Identity> AuthenticateAsync(string username, string password);
    }

    public interface ICredentialStore
    {
        void Save(string username, string password);
        (string username, string password) Load();
        void Clear();
    }

    public sealed class Session
    {
        public Identity Current { get; private set; }
        public bool IsAuthenticated => Current != null;
        public Role Role => Current?.Role ?? Role.None;
        public void Start(Identity identity) { Current = identity ?? throw new ArgumentNullException(nameof(identity)); }
        public void Clear() { Current = null; }
        public void Require(Role minimum)
        {
            if ((minimum != Role.Admin && minimum != Role.Galpon) || !IsAuthenticated || Role < minimum)
                throw new UnauthorizedAccessException("Acceso denegado.");
        }
    }
}
