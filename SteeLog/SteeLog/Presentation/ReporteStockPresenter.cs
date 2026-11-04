using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Reportes;
namespace SteeLog.Presentation
{
    public sealed class ReporteStockPresenter
    {
        private readonly IUseCaseExecutor executor;
        private readonly IReporteStockPdf pdf;
        public ReporteStockPresenter(IUseCaseExecutor executor, IReporteStockPdf pdf) { this.executor = executor; this.pdf = pdf; }
        public Task<IReadOnlyList<ReporteStockItem>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token) => executor.ExecuteAsync<ConsultarReporteStock, IReadOnlyList<ReporteStockItem>>(x => x.ListarAsync(filtro, token));
        public Task<IReadOnlyList<ReporteStockSeccion>> SeccionesAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarReporteStock, IReadOnlyList<ReporteStockSeccion>>(x => x.SeccionesAsync(token));
        public Task<IReadOnlyList<MaterialInventarioLectura>> MaterialesAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarReporteStock, IReadOnlyList<MaterialInventarioLectura>>(x => x.MaterialesAsync(token));
        public Task GuardarAsync(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, CancellationToken token) => executor.ExecuteAsync<GuardarReporteStockConfiguracion, int>(async x => { await x.EjecutarAsync(secciones, eliminadas, token); return 0; });
        public Task GenerarAsync(string ruta, ReporteStockSnapshot snapshot, CancellationToken token) => Task.Run(() => { token.ThrowIfCancellationRequested(); pdf.Generar(ruta, snapshot); token.ThrowIfCancellationRequested(); }, token);
    }
}
