using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Reportes;

namespace SteeLog.Presentation
{
    public sealed class InformeObraPresenter
    {
        private readonly IUseCaseExecutor executor;
        private readonly IInformeObraPdf pdf;
        public InformeObraPresenter(IUseCaseExecutor executor, IInformeObraPdf pdf) { this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); this.pdf = pdf ?? throw new ArgumentNullException(nameof(pdf)); }
        public Task<InformeObraLecturas> LecturasAsync(int id, string nombre, CancellationToken t) => executor.ExecuteAsync<ConsultarInformeObra, InformeObraLecturas>(x => x.LecturasAsync(id, nombre, t));
        public Task<InformeObraSnapshot> SnapshotAsync(int id, string nombre, DateTime? desde, DateTime? hasta, CancellationToken t) => executor.ExecuteAsync<ConsultarInformeObra, InformeObraSnapshot>(x => x.SnapshotAsync(id, nombre, desde, hasta, t));
        public Task<DateTime?> PrimeraFechaAsync(int id, CancellationToken t) => executor.ExecuteAsync<ConsultarInformeObra, DateTime?>(x => x.PrimeraFechaRemitoAsync(id, t));
        public async Task GenerarAsync(string ruta, int id, string nombre, DateTime? desde, DateTime? hasta, CancellationToken t)
        {
            var snapshot = await SnapshotAsync(id, nombre, desde, hasta, t).ConfigureAwait(false);
            await Task.Run(() => { t.ThrowIfCancellationRequested(); pdf.Generar(ruta, snapshot); }, t).ConfigureAwait(false);
        }
    }
}
