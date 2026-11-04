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
using SteeLog.Infrastructure.Conteos;
using SteeLog.Core.Application.Conteos;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Pdf;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Borradores;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Movimientos;
using SteeLog.Core.Application.Activos;
using SteeLog.Infrastructure.Activos;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Canaletas;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Infrastructure.Catalogos;
using SteeLog.Infrastructure.Reportes;
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
        internal IDraftStore MovimientoDrafts { get; }
        internal BorradorMovimiento BorradorMovimiento { get; }
        internal IDemoUpdateSource Updates { get; }
        internal IUseCaseExecutor Executor { get; }
        internal IOrdenesPendientesPdf OrdenesPdf { get; }
        internal IRemitoPdf RemitoPdf { get; }
        internal IConteoPdf ConteoPdf { get; }
        internal IInformeObraPdf InformeObraPdf { get; }
        internal IKardexPdf KardexPdf { get; }
        internal IReporteStockPdf ReporteStockPdf { get; }
        private readonly ServiceProvider provider;
        internal LocalPaths Paths { get; }
        internal string ReportsDirectory { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SteeLog", "Reportes");
        private readonly Lazy<LocalConfiguration> configuration;
        private readonly Lazy<PostgresConnectionFactory> connections;
        internal LocalConfiguration Configuration => configuration.Value;
        internal PostgresConnectionFactory Connections => connections.Value;
        internal LocalServices(string executableDirectory, string localApplicationData)
            : this(executableDirectory, localApplicationData, localApplicationData) { }
        internal LocalServices(string executableDirectory, string localApplicationData, string roamingApplicationData)
        {
            Paths = new LocalPaths(executableDirectory, localApplicationData);
            Photos = new LocalPhotoStorage(Paths.StorageDirectory);
            MovimientoDrafts = new LocalDraftStore(Path.Combine(roamingApplicationData, "SteeLog", "movimiento_draft.json"));
            BorradorMovimiento = new BorradorMovimiento(MovimientoDrafts, () => DateTime.Now);
            Credentials = new OptionalCredentialStore(new DpapiCredentialStore(Path.Combine(localApplicationData, "SteeLog", "session.dat")));
            Updates = new LocalDemoUpdateSource(Path.Combine(executableDirectory, "demo", "actualizaciones"), Path.Combine(localApplicationData, "SteeLog", "ActualizacionesDemo"));
            configuration = new Lazy<LocalConfiguration>(() => LocalConfiguration.Load(Paths.ExecutableDirectory, () => typeof(LocalServices).Assembly.GetManifestResourceStream("SteeLog.appsettings.json")));
            connections = new Lazy<PostgresConnectionFactory>(() => new PostgresConnectionFactory(Configuration.ConnectionString));
            var registrations = new ServiceCollection();
            OrdenesPdf = new OrdenesPendientesPDF(new RecursosPdfLocales());
            RemitoPdf = new SteeLog.Infrastructure.Pdf.MovimientoPDF(new RecursosPdfLocales());
            ConteoPdf = new SteeLog.Infrastructure.Pdf.ConteoInventarioPDF(new RecursosPdfLocales());
            InformeObraPdf = new SteeLog.Infrastructure.Pdf.InformeObraPDF(new RecursosPdfLocales());
            KardexPdf = new SteeLog.Infrastructure.Pdf.KardexPDF(new RecursosPdfLocales());
            ReporteStockPdf = new SteeLog.Infrastructure.Pdf.ReporteStockPDF(new RecursosPdfLocales());
            registrations.AddSingleton(Updates);
            registrations.AddSingleton<IArchivosCanaleta>(new LocalCanaletaStorage(Paths.StorageDirectory));
            registrations.AddScoped<ICanaletasConsulta>(_ => new NpgsqlCanaletasConsulta(Connections));
            registrations.AddScoped<ICanaletasEscritura, NpgsqlCanaletas>();
            registrations.AddScoped<ConsultarCanaletas>();
            registrations.AddScoped<OperacionesCanaleta>();
            registrations.AddSingleton(Session);
            registrations.AddSingleton<IClock>(_ => new BusinessClock(TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"), () => DateTimeOffset.UtcNow));
            registrations.AddScoped(_ => new NpgsqlUnitOfWork(Connections));
            registrations.AddScoped<IUnitOfWork>(scope => scope.GetRequiredService<NpgsqlUnitOfWork>());
            registrations.AddScoped<IConteosConsulta>(_ => new NpgsqlConteosConsulta(Connections));
            registrations.AddScoped<IConteosEscritura, NpgsqlConteos>();
            registrations.AddScoped<ConsultarConteos>();
            registrations.AddScoped<OperacionesConteo>();
            registrations.AddScoped<IOrdenesConsulta>(_ => new NpgsqlOrdenesConsulta(Connections));
            registrations.AddScoped<ConsultarOrdenes>();
            registrations.AddScoped<NpgsqlOrdenes>();
            registrations.AddScoped<IOrdenesEscritura>(scope => scope.GetRequiredService<NpgsqlOrdenes>());
            registrations.AddScoped<IInventarioEscritura, NpgsqlInventario>();
            registrations.AddScoped<IInventarioConsulta>(_ => new NpgsqlInventarioConsulta(Connections));
            registrations.AddScoped<ConsultarInventario>();
            registrations.AddScoped<IMaterialesEscritura, NpgsqlMateriales>();
            registrations.AddScoped<IRecortesEscritura, NpgsqlRecortes>();
            registrations.AddScoped<IRecortesConsulta>(_ => new NpgsqlRecortesConsulta(Connections));
            registrations.AddScoped<CrearMaterial>();
            registrations.AddScoped<EditarMaterial>();
            registrations.AddScoped<EliminarMaterial>();
            registrations.AddScoped<AgregarRecorte>();
            registrations.AddScoped<EliminarRecorte>();
            registrations.AddScoped<ConsultarRecortes>();
            registrations.AddScoped<IInsumosEscritura, NpgsqlInsumos>();
            registrations.AddScoped<IInsumosConsulta, NpgsqlInsumosConsulta>();
            registrations.AddScoped<ConsultarInsumos>();
            registrations.AddScoped<IHistorialMovimientoConsulta>(_ => new NpgsqlHistorialMovimientoConsulta(Connections));
            registrations.AddScoped<ConsultarHistorialMovimientos>();
            registrations.AddScoped<GestionarInsumos>();
            registrations.AddScoped<IHerramientasConsulta>(_ => new NpgsqlHerramientasConsulta(Connections));
            registrations.AddScoped<IHerramientasEscritura, NpgsqlHerramientas>();
            registrations.AddScoped<ConsultarHerramientas>();
            registrations.AddScoped<GestionarHerramientas>();
            registrations.AddScoped<ICatalogosConsulta>(_ => new NpgsqlCatalogosConsulta(Connections));
            registrations.AddScoped<ICatalogosEscritura, NpgsqlCatalogos>();
            registrations.AddScoped<ConsultarCatalogos>();
            registrations.AddScoped<GestionarCatalogos>();
            registrations.AddScoped<IKardexConsulta>(_ => new NpgsqlKardexConsulta(Connections));
            registrations.AddScoped<ConsultarKardex>();
            registrations.AddScoped<IInformeObraConsulta>(_ => new NpgsqlInformeObraConsulta(Connections));
            registrations.AddScoped<ConsultarInformeObra>();
            registrations.AddScoped<IReporteStockConsulta>(_ => new NpgsqlReporteStockConsulta(Connections));
            registrations.AddScoped<IReporteStockConfiguracion, NpgsqlReporteStockConfiguracion>();
            registrations.AddScoped<ConsultarReporteStock>();
            registrations.AddScoped<GuardarReporteStockConfiguracion>();
            registrations.AddScoped<IRemitoConsulta>(_ => new NpgsqlRemitoConsulta(Connections));
            registrations.AddScoped<ConsultarRemito>();
            registrations.AddScoped<ICatalogosMovimientoConsulta>(_ => new NpgsqlCatalogosMovimientoConsulta(Connections));
            registrations.AddScoped<ConsultarCatalogosMovimiento>();
            registrations.AddScoped<IMaterialesConsulta>(_ => new NpgsqlMaterialesConsulta(Connections));
            registrations.AddScoped<ConsultarMateriales>();
            registrations.AddScoped<IMovimientoEdicionConsulta>(_ => new NpgsqlMovimientoEdicionConsulta(Connections));
            registrations.AddScoped<ConsultarMovimientoEdicion>();
            registrations.AddScoped<IFletesEscritura, NpgsqlFletes>();
            registrations.AddScoped<GestionarFletes>();
            registrations.AddScoped<IPrecioEventualEscritura, NpgsqlPrecioEventual>();
            registrations.AddScoped<ActualizarPrecioEventual>();
            registrations.AddScoped<IMovimientosEscritura, NpgsqlMovimientos>();
            registrations.AddScoped<RegistrarMovimiento>();
            registrations.AddScoped<EditarMovimiento>();
            registrations.AddScoped<EliminarMovimiento>();
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
        internal InsumosPresenter CrearInsumosPresenter(IInsumosVista vista) => new InsumosPresenter(Executor, vista);
        internal HerramientasPresenter CrearHerramientasPresenter(IHerramientasVista vista) => new HerramientasPresenter(Executor, vista);
        internal MovimientosPresenter CrearMovimientosPresenter(IMovimientosVista vista) => new MovimientosPresenter(Executor, vista, RemitoPdf);
        internal OrdenesPresenter CrearOrdenesPresenter(IOrdenesVista vista) => new OrdenesPresenter(Executor, vista, OrdenesPdf);
        internal string ConnectionString => Configuration.ConnectionString;
        public void Dispose() => provider.Dispose();
        internal static LocalServices Create() => CreateForEnvironment(AppDomain.CurrentDomain.BaseDirectory, Environment.GetEnvironmentVariable("STEELOG_DEMO_ROOT"));
        internal static LocalServices CreateForEnvironment(string executableDirectory, string demoRoot)
        {
            if (string.IsNullOrWhiteSpace(demoRoot))
                return new LocalServices(executableDirectory, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            if (!Path.IsPathRooted(demoRoot) || Path.GetPathRoot(demoRoot).Length <= 2) throw new InvalidOperationException("STEELOG_DEMO_ROOT debe ser una ruta absoluta de una instalación demo preparada.");
            var root = Path.GetFullPath(demoRoot);
            var marker = Path.Combine(root, ".steelog-demo");
            if (!File.Exists(marker) || File.ReadAllText(marker).Trim() != "steelog-demo-v1")
                throw new InvalidOperationException("Falta el marcador del directorio de datos demo. No se usará el perfil personal como fallback.");
            return new LocalServices(executableDirectory, root, root) { ReportsDirectory = Path.Combine(root, "SteeLog", "Reportes") };
        }
    }
}
