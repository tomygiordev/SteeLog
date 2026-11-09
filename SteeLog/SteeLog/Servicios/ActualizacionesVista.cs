using System;
using System.Threading;
using System.Threading.Tasks;

using SteeLog.Core.Application.Actualizaciones;

namespace SteeLog.Servicios
{
    public sealed class ActualizacionesVista
    {
        private readonly SteeLog.Presentation.IUseCaseExecutor executor;
        public ActualizacionesVista(SteeLog.Presentation.IUseCaseExecutor executor) { this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); }
        public async Task<UpdateInfo> CheckForUpdateAsync(bool throwOnError = false)
        {
            try { return await executor.ExecuteAsync<CheckDemoUpdate, UpdateInfo>(useCase => useCase.ExecuteAsync(AppVersion.Current, CancellationToken.None)); }
            catch when (!throwOnError) { return null; }
        }
        public Task<string> PrepareAsync(UpdateInfo update, IProgress<int> progress, CancellationToken cancellationToken)
            => executor.ExecuteAsync<PrepareDemoUpdate, string>(useCase => useCase.ExecuteAsync(update, progress, cancellationToken));
    }
}
