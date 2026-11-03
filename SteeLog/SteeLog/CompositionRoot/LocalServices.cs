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
using SteeLog.Infrastructure.OrdenesCompra;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Pdf;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.OrdenesCompra;
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
        internal IOrdenesPendientesPdf OrdenesPdf { get; }
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
            OrdenesPdf = new OrdenesPendientesPDF(new RecursosPdfLocales());
            registrations.AddSingleton(Updates);
            registrations.AddSingleton(Session);
            registrations.AddSingleton<IClock>(_ => new BusinessClock(TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"), () => DateTimeOffset.UtcNow));
            registrations.AddScoped(_ => new NpgsqlUnitOfWork(Connections));
            registrations.AddScoped<IUnitOfWork>(scope => scope.GetRequiredService<NpgsqlUnitOfWork>());
            registrations.AddScoped<IOrdenesConsulta>(_ => new NpgsqlOrdenesConsulta(Connections));
            registrations.AddScoped<ConsultarOrdenes>();
            registrations.AddScoped<NpgsqlOrdenes>();
            registrations.AddScoped<IOrdenesEscritura>(scope => scope.GetRequiredService<NpgsqlOrdenes>());
            registrations.AddScoped<IInventarioEscritura, NpgsqlInventario>();
            registrations.AddScoped<CrearOrden>();
            registrations.AddScoped<EditarOrden>();
            registrations.AddScoped<CambiarMaterialOrden>();
            registrations.AddScoped<ConfirmarRecepcion>();
            registrations.AddScoped<EliminarOrden>();
            registrations.AddScoped<CheckDemoUpdate>();
            registrations.AddScoped<PrepareDemoUpdate>();
            provider = registrations.BuildServiceProvider();
            Executor = new UseCaseExecutor(provider.GetRequiredService<IServiceScopeFactory>());
        }
        internal OrdenesPresenter CrearOrdenesPresenter(IOrdenesVista vista) => new OrdenesPresenter(Executor, vista, OrdenesPdf);
        internal string ConnectionString => Configuration.ConnectionString;
        internal void EnsureConnectionFactory() { var unused = Connections; }
        internal Npgsql.NpgsqlConnection CreateConnection() => Connections.Create();
        public void Dispose() => provider.Dispose();
        internal static LocalServices Create() => new LocalServices(AppDomain.CurrentDomain.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }
}
