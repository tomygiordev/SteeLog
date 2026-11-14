using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Bitacora;
namespace SteeLog.Presentation
{
    /// <summary>Consulta de la bitácora de auditoría (solo Admin). Uso: new BitacoraPresenter(servicios.Executor).</summary>
    public sealed class BitacoraPresenter
    {
        private readonly IUseCaseExecutor executor;
        public BitacoraPresenter(IUseCaseExecutor executor) { this.executor = executor; }
        public Task<BitacoraResultado> BuscarAsync(BitacoraFiltro filtro, CancellationToken token) => executor.ExecuteAsync<ConsultarBitacora, BitacoraResultado>(x => x.BuscarAsync(filtro, token));
        public Task<BitacoraOpciones> OpcionesAsync(CancellationToken token) => executor.ExecuteAsync<ConsultarBitacora, BitacoraOpciones>(x => x.OpcionesAsync(token));
    }
}
