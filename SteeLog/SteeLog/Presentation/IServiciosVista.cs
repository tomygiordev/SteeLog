using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Borradores;
using SteeLog.Core.Application.Reportes;
using SteeLog.Core.Application.Conteos;
using SteeLog.Servicios;

namespace SteeLog.Presentation
{
    public sealed class RutasVista
    {
        public string ExecutableDirectory { get; }
        public string StorageDirectory { get; }
        public RutasVista(string executableDirectory, string storageDirectory)
        {
            ExecutableDirectory = executableDirectory;
            StorageDirectory = storageDirectory;
        }
    }

    public interface IServiciosVista
    {
        IUseCaseExecutor Executor { get; }
        SesionVista Sesion { get; }
        CredencialesVista Credenciales { get; }
        AutenticacionVista Autenticacion { get; }
        ActualizacionesVista Actualizaciones { get; }
        BorradorMovimientoVista Borrador { get; }
        IInformeObraPdf InformeObraPdf { get; }
        IKardexPdf KardexPdf { get; }
        IReporteStockPdf ReporteStockPdf { get; }
        IConteoPdf ConteoPdf { get; }
        string ReportsDirectory { get; }
        RutasVista Paths { get; }
        OrdenesPresenter CrearOrdenesPresenter(IOrdenesVista vista);
        MovimientosPresenter CrearMovimientosPresenter(IMovimientosVista vista);
        InsumosPresenter CrearInsumosPresenter(IInsumosVista vista);
        HerramientasPresenter CrearHerramientasPresenter(IHerramientasVista vista);
    }
}
