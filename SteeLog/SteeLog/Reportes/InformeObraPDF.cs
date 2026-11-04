using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;

namespace SteeLog.Reportes
{
    public sealed class InformeObraPDF
    {
        public Task GenerarAsync(string ruta, int idObra, string nombreObra, DateTime? desde, DateTime? hasta, CancellationToken token)
            => new InformeObraPresenter(LocalServices.Current.Executor, LocalServices.Current.InformeObraPdf).GenerarAsync(ruta, idObra, nombreObra, desde, hasta, token);
    }
}
