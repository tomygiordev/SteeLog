using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Presentation;

namespace SteeLog.Reportes
{
    public sealed class InformeObraPDF
    {
        private readonly IServiciosVista servicios;
        [Obsolete("Fase DI: recibir IServiciosVista por constructor")]
        public InformeObraPDF() : this(CompositionRoot.ServiciosCompatibles.Obtener()) { }
        public InformeObraPDF(IServiciosVista servicios) { this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios)); }
        public Task GenerarAsync(string ruta, int idObra, string nombreObra, DateTime? desde, DateTime? hasta, CancellationToken token)
            => new InformeObraPresenter(servicios.Executor, servicios.InformeObraPdf).GenerarAsync(ruta, idObra, nombreObra, desde, hasta, token);
    }
}
