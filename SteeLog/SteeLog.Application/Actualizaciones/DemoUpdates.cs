using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteeLog.Core.Application.Actualizaciones
{
    public sealed class UpdateInfo
    {
        public string Version { get; set; }
        public string PackageName { get; set; }
        public string Sha256 { get; set; }
        public string ReleaseNotes { get; set; }
    }
    public interface IDemoUpdateSource
    {
        Task<UpdateInfo> ReadAsync(CancellationToken cancellationToken);
        Task<string> PrepareAsync(UpdateInfo update, IProgress<int> progress, CancellationToken cancellationToken);
    }
    public sealed class CheckDemoUpdate
    {
        private readonly IDemoUpdateSource source;
        public CheckDemoUpdate(IDemoUpdateSource source) { this.source = source; }
        public async Task<UpdateInfo> ExecuteAsync(string currentVersion, CancellationToken cancellationToken)
        {
            var update = await source.ReadAsync(cancellationToken);
            if (!System.Version.TryParse(currentVersion, out var current) || !System.Version.TryParse(update.Version, out var proposed))
                throw new InvalidOperationException("VersiÃ³n demo invÃ¡lida.");
            return proposed > current ? update : null;
        }
    }
    public sealed class PrepareDemoUpdate
    {
        private readonly IDemoUpdateSource source;
        public PrepareDemoUpdate(IDemoUpdateSource source) { this.source = source ?? throw new ArgumentNullException(nameof(source)); }
        public Task<string> ExecuteAsync(UpdateInfo update, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));
            return source.PrepareAsync(update, progress, cancellationToken);
        }
    }
}
