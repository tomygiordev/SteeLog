using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Core.Application.Reportes;
namespace SteeLog.Presentation
{
    public sealed class KardexPresenter
    {
        private readonly IUseCaseExecutor executor;
        private readonly IKardexPdf pdf;
        public KardexPresenter(IUseCaseExecutor executor, IKardexPdf pdf) { this.executor = executor; this.pdf = pdf; }
        public Task<List<KardexMaterial>> MaterialesAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarKardex, List<KardexMaterial>>(x => x.MaterialesAsync(token));
        public Task<List<KardexItem>> HistorialAsync(int? material, DateTime? desde, DateTime? hasta, CancellationToken token) => executor.ExecuteAsync<ConsultarKardex, List<KardexItem>>(x => x.HistorialAsync(material, desde, hasta, token));
        public Task<int> EliminarAsync(int id, CancellationToken token) => executor.ExecuteAsync<EliminarMovimiento, int>(x => x.EjecutarAsync(id, token));
        public Task GenerarAsync(string ruta, KardexSnapshot snapshot, CancellationToken token) => Task.Run(() => { token.ThrowIfCancellationRequested(); pdf.Generar(ruta, snapshot); token.ThrowIfCancellationRequested(); }, token);
    }
}
