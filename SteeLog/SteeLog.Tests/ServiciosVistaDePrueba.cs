using System;
using System.IO;
using System.Threading.Tasks;
using SteeLog.Core.Application.Borradores;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Core.Application.Reportes;
using SteeLog.Presentation;
using SteeLog.Servicios;

namespace SteeLog.Tests
{
    // No compone infraestructura, no registra globals y no crea archivos.
    public sealed class ServiciosVistaDePrueba : IServiciosVista
    {
        public IUseCaseExecutor Executor { get; }
        public SesionVista Sesion { get; }
        public CredencialesVista Credenciales { get; }
        public AutenticacionVista Autenticacion { get; }
        public ActualizacionesVista Actualizaciones { get; }
        public BorradorMovimientoVista Borrador { get; }
        public IInformeObraPdf InformeObraPdf { get; set; }
        public IKardexPdf KardexPdf { get; set; }
        public IReporteStockPdf ReporteStockPdf { get; set; }
        public IConteoPdf ConteoPdf { get; set; }
        public IOrdenesPendientesPdf OrdenesPdf { get; set; }
        public IRemitoPdf RemitoPdf { get; set; }
        public string ReportsDirectory { get; set; }
        public RutasVista Paths { get; }

        public ServiciosVistaDePrueba(IUseCaseExecutor executor = null, IAuthentication authentication = null,
            ICredentialStore credentials = null, IDraftStore drafts = null, Func<DateTime> ahora = null)
        {
            Executor = executor ?? new ExecutorSinConfigurar();
            Sesion = new SesionVista(new Session());
            Credenciales = new CredencialesVista(credentials ?? new CredencialesEnMemoria());
            Autenticacion = new AutenticacionVista(authentication ?? new AutenticacionDenegada());
            Actualizaciones = new ActualizacionesVista(Executor);
            Borrador = new BorradorMovimientoVista(new BorradorMovimiento(drafts ?? new BorradorEnMemoria(), ahora ?? (() => DateTime.Now)));
            var root = Path.Combine(Path.GetTempPath(), "steelog-vista-test-" + Guid.NewGuid().ToString("N"));
            Paths = new RutasVista(root, Path.Combine(root, "Storage"));
            ReportsDirectory = Path.Combine(root, "Reportes");
        }

        public OrdenesPresenter CrearOrdenesPresenter(IOrdenesVista vista)
            => new OrdenesPresenter(Executor, vista, OrdenesPdf ?? throw new InvalidOperationException("Configurar OrdenesPdf en el test."));
        public MovimientosPresenter CrearMovimientosPresenter(IMovimientosVista vista)
            => new MovimientosPresenter(Executor, vista, RemitoPdf ?? throw new InvalidOperationException("Configurar RemitoPdf en el test."));
        public InsumosPresenter CrearInsumosPresenter(IInsumosVista vista) => new InsumosPresenter(Executor, vista);
        public HerramientasPresenter CrearHerramientasPresenter(IHerramientasVista vista) => new HerramientasPresenter(Executor, vista);

        private sealed class ExecutorSinConfigurar : IUseCaseExecutor
        {
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
                => throw new InvalidOperationException("Inyectar un executor doble para esta operación.");
        }
        private sealed class AutenticacionDenegada : IAuthentication
        {
            public Task<Identity> AuthenticateAsync(string username, string password) => Task.FromResult<Identity>(null);
        }
        private sealed class CredencialesEnMemoria : ICredentialStore
        {
            private (string username, string password) value;
            public void Save(string username, string password) { value = (username, password); }
            public (string username, string password) Load() => value;
            public void Clear() { value = (null, null); }
        }
        private sealed class BorradorEnMemoria : IDraftStore
        {
            private byte[] contenido;
            public byte[] Leer() => contenido == null ? null : (byte[])contenido.Clone();
            public void Guardar(byte[] value) { contenido = (byte[])value.Clone(); }
            public void Eliminar() { contenido = null; }
        }
    }
}
