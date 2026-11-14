using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Respaldos;
namespace SteeLog.Presentation
{
    /// <summary>
    /// Copias de seguridad de la base. Todo exige Admin salvo EjecutarAutomaticoAsync (tarea del sistema: pide solo sesión).
    /// Uso: servicios.CrearRespaldosPresenter().
    /// </summary>
    public sealed class RespaldosPresenter
    {
        private readonly IUseCaseExecutor executor;
        public RespaldosPresenter(IUseCaseExecutor executor) { this.executor = executor; }
        public Task<EstadoRespaldos> ObtenerEstadoAsync(CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, EstadoRespaldos>(x => x.ObtenerEstadoAsync(token));
        public Task<ResultadoRespaldo> CrearAsync(string nota, IProgress<ProgresoRespaldo> progreso, CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoRespaldo>(x => x.CrearAsync(nota, progreso, token));
        public Task<ResultadoVerificacion> VerificarAsync(string id, CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoVerificacion>(x => x.VerificarAsync(id, token));
        public Task<ResultadoRestauracion> RestaurarAsync(string id, IProgress<ProgresoRespaldo> progreso, CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoRestauracion>(x => x.RestaurarAsync(id, progreso, token));
        public Task<ResultadoRespaldo> ImportarAsync(string ruta, CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoRespaldo>(x => x.ImportarAsync(ruta, token));
        public Task<ResultadoOperacion> EliminarAsync(string id, CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoOperacion>(x => x.EliminarAsync(id, token));
        public Task<ResultadoOperacion> GuardarConfiguracionAsync(ConfiguracionRespaldo configuracion, CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoOperacion>(x => x.GuardarConfiguracionAsync(configuracion, token));
        /// <summary>Para llamar (sin esperar) después del login. No lanza; si falla, lo informa en el resultado.</summary>
        public Task<ResultadoAutomatico> EjecutarAutomaticoAsync(CancellationToken token) => executor.ExecuteAsync<ServicioRespaldos, ResultadoAutomatico>(x => x.EjecutarSiCorrespondeAsync(token));
    }
}
