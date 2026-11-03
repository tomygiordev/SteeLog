using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using SteeLog.Presentation;
using SteeLog.Core.Application.Actualizaciones;
using SteeLog.Infrastructure.DemoUpdates;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Common;
using SteeLog.Infrastructure.Identity;
using SteeLog.Infrastructure.Files;
using SteeLog.Infrastructure.Configuration;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.CompositionRoot
{
    internal sealed class LocalServices : IDisposable
    {
        private static readonly Lazy<LocalServices> instance = new Lazy<LocalServices>(Create);
        internal static LocalServices Current => instance.Value;
        internal IAuthentication Authentication { get; } = new DemoAuthService();
        internal Session Session { get; } = new Session();
        internal ICredentialStore Credentials { get; }
        internal IPhotoStorage Photos { get; }
        internal IDemoUpdateSource Updates { get; }
        internal IUseCaseExecutor Executor { get; }
        private readonly ServiceProvider provider;
        internal LocalPaths Paths { get; }
        private readonly Lazy<LocalConfiguration> configuration;
        private readonly Lazy<PostgresConnectionFactory> connections;
        internal LocalConfiguration Configuration => configuration.Value;
        internal PostgresConnectionFactory Connections => connections.Value;
        internal LocalServices(string executableDirectory, string localApplicationData)
        {
            Paths = new LocalPaths(executableDirectory, localApplicationData);
            Photos = new LocalPhotoStorage(Paths.StorageDirectory);
            Credentials = new OptionalCredentialStore(new DpapiCredentialStore(Path.Combine(localApplicationData, "SteeLog", "session.dat")));
            Updates = new LocalDemoUpdateSource(Path.Combine(executableDirectory, "demo", "actualizaciones"), Path.Combine(localApplicationData, "SteeLog", "ActualizacionesDemo"));
            configuration = new Lazy<LocalConfiguration>(() => LocalConfiguration.Load(Paths.ExecutableDirectory, () => typeof(LocalServices).Assembly.GetManifestResourceStream("SteeLog.appsettings.json")));
            connections = new Lazy<PostgresConnectionFactory>(() => new PostgresConnectionFactory(Configuration.ConnectionString));
            var registrations = new ServiceCollection();
            registrations.AddSingleton(Updates);
            registrations.AddScoped<CheckDemoUpdate>();
            registrations.AddScoped<PrepareDemoUpdate>();
            provider = registrations.BuildServiceProvider();
            Executor = new UseCaseExecutor(provider.GetRequiredService<IServiceScopeFactory>());
        }
        internal string ConnectionString => Configuration.ConnectionString;
        internal void EnsureConnectionFactory() { var unused = Connections; }
        internal Npgsql.NpgsqlConnection CreateConnection() => Connections.Create();
        public void Dispose() => provider.Dispose();
        internal static LocalServices Create() => new LocalServices(AppDomain.CurrentDomain.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }
}
