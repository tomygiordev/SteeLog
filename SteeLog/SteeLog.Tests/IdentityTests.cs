using System;
using System.IO;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Infrastructure.Identity;
using SteeLog.Infrastructure.Files;
using Xunit;

public class IdentityTests
{
    [Theory]
    [InlineData(" admin ", "admin", Role.Admin)]
    [InlineData("ADMIN@STE ELOG.LOCAL", "admin", Role.None)]
    [InlineData("ADMIN@STEELOG.LOCAL", "123456", Role.Admin)]
    [InlineData("admin@tesis.local", "admin", Role.Admin)]
    [InlineData("admin@gmail.com", "123456", Role.Admin)]
    [InlineData("galpon@tesis.local", "galpon", Role.Galpon)]
    [InlineData("operador@tesis.local", "operador", Role.Galpon)]
    [InlineData("operador", "operador", Role.Galpon)]
    [InlineData("invitado@tesis.local", "galpon", Role.Galpon)]
    [InlineData("galpon", "wrong", Role.None)]
    [InlineData("other@example.invalid", "123456", Role.None)]
    public async Task Authenticates_only_explicit_demo_accounts(string user, string password, Role expected)
    {
        var identity = await new DemoAuthService().AuthenticateAsync(user, password);
        Assert.Equal(expected, identity?.Role ?? Role.None);
    }
    [Fact] public void Session_denies_none_and_enforces_roles_and_logout()
    {
        Assert.Throws<ArgumentException>(() => new Identity("bad@demo", (Role)99));
        var session = new Session();
        Assert.Equal(Role.None, session.Role);
        Assert.Throws<UnauthorizedAccessException>(() => session.Require(Role.Galpon));
        session.Start(new Identity("galpon@tesis.local", Role.Galpon));
        session.Require(Role.Galpon);
        Assert.Throws<UnauthorizedAccessException>(() => session.Require(Role.Admin));
        session.Start(new Identity("admin@tesis.local", Role.Admin)); session.Require(Role.Admin);
        session.Clear(); Assert.Null(session.Current); Assert.Equal(Role.None, session.Role);
        Assert.Throws<UnauthorizedAccessException>(() => session.Require(Role.Galpon));
    }
    [Fact] public void Dpapi_roundtrips_and_clears_compatible_session_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-f2-credentials-" + Guid.NewGuid().ToString("N"));
        try {
            var path = Path.Combine(root, "session.dat"); var store = new DpapiCredentialStore(path);
            store.Save("admin", "demo"); Assert.DoesNotContain("demo", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(path)));
            Assert.Equal(("admin", "demo"), store.Load()); store.Clear(); Assert.False(File.Exists(path));
        } finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact] public async Task Optional_credentials_failures_do_not_block_login_or_logout()
    {
        var store = new OptionalCredentialStore(new FailingCredentialStore());
        Assert.Equal((null, null), store.Load());
        var identity = await new DemoAuthService().AuthenticateAsync("admin", "admin");
        var session = new Session();
        session.Start(identity);
        store.Save("admin", "admin");
        Assert.True(session.IsAuthenticated);
        store.Clear();
        session.Clear();
        Assert.False(session.IsAuthenticated);
        Assert.Equal(Role.None, session.Role);
    }
    [Fact] public void Corrupt_dpapi_session_is_treated_as_missing_without_touching_other_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-f2-corrupt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var path = Path.Combine(root, "session.dat");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
            var store = new OptionalCredentialStore(new DpapiCredentialStore(path));
            Assert.Equal((null, null), store.Load());
            Assert.True(File.Exists(path));
        } finally { Directory.Delete(root, true); }
    }
    sealed class FailingCredentialStore : ICredentialStore
    {
        public void Save(string username, string password) { throw new IOException("secret_marker"); }
        public (string username, string password) Load() { throw new IOException("secret_marker"); }
        public void Clear() { throw new UnauthorizedAccessException("secret_marker"); }
    }
    [Fact] public async Task Local_storage_reads_existing_files_without_network_and_blocks_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "steelog-f2-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "canaletas", "7"));
        try {
            var path = Path.Combine(root, "canaletas", "7", "existing.jpg"); File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            var store = new LocalPhotoStorage(root);
            Assert.Equal(new byte[] { 1, 2, 3 }, await store.ReadAsync("canaletas/7/existing.jpg"));
            await store.WriteAsync("canaletas/7/new.jpg", new byte[] { 4 });
            Assert.Equal(new byte[] { 4 }, await store.ReadAsync("canaletas/7/new.jpg"));
            await store.DeleteAsync("canaletas/7/new.jpg"); Assert.True(File.Exists(path));
            await Assert.ThrowsAsync<ArgumentException>(() => store.ReadAsync("../outside.jpg"));
        } finally { Directory.Delete(root, true); }
    }
}
