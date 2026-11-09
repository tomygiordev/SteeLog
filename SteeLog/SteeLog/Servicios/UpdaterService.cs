using System;
using System.Threading;
using System.Threading.Tasks;

using SteeLog.Core.Application.Actualizaciones;

namespace SteeLog.Servicios
{
    [System.Obsolete("Fase DI: recibir IServiciosVista por constructor")]
    public static class UpdaterService
    {
        private static ActualizacionesVista Servicio => SteeLog.CompositionRoot.ServiciosCompatibles.Obtener().Actualizaciones;
        public static async Task<UpdateInfo> CheckForUpdateAsync(bool throwOnError = false)
        {
            try { return await Servicio.CheckForUpdateAsync(throwOnError); }
            catch when (!throwOnError) { return null; }
        }
        public static Task<string> PrepareAsync(UpdateInfo update, IProgress<int> progress, CancellationToken cancellationToken)
            => Servicio.PrepareAsync(update, progress, cancellationToken);
    }
}
