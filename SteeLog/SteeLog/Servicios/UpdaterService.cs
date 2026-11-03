using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Actualizaciones;

namespace SteeLog.Servicios
{
    public static class UpdaterService
    {
        public static async Task<UpdateInfo> CheckForUpdateAsync(bool throwOnError = false)
        {
            try { return await LocalServices.Current.Executor.ExecuteAsync<CheckDemoUpdate, UpdateInfo>(useCase => useCase.ExecuteAsync(AppVersion.Current, CancellationToken.None)); }
            catch when (!throwOnError) { return null; }
        }
        public static Task<string> PrepareAsync(UpdateInfo update, IProgress<int> progress, CancellationToken cancellationToken)
            => LocalServices.Current.Executor.ExecuteAsync<PrepareDemoUpdate, string>(useCase => useCase.ExecuteAsync(update, progress, cancellationToken));
    }
}
