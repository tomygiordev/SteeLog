using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Actualizaciones;

namespace SteeLog.Infrastructure.DemoUpdates
{
    public sealed class LocalDemoUpdateSource : IDemoUpdateSource
    {
        private readonly string sourceDirectory;
        private readonly string stagingDirectory;
        public LocalDemoUpdateSource(string sourceDirectory, string stagingDirectory)
        {
            this.sourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
            this.stagingDirectory = stagingDirectory ?? throw new ArgumentNullException(nameof(stagingDirectory));
        }
        public Task<UpdateInfo> ReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using (var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(sourceDirectory, "manifiesto.json"))))
                {
                    var root = json.RootElement;
                    var result = new UpdateInfo {
                        Version = root.GetProperty("version").GetString(),
                        PackageName = root.GetProperty("paquete").GetString(),
                        Sha256 = root.GetProperty("sha256").GetString(),
                        ReleaseNotes = root.GetProperty("notas").GetString()
                    };
                    Validate(result);
                    return Task.FromResult(result);
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
            { throw new InvalidOperationException("Manifiesto de actualización demo inválido."); }
        }
        private static void Validate(UpdateInfo update)
        {
            if (update == null || !Version.TryParse(update.Version, out _) || string.IsNullOrWhiteSpace(update.PackageName)
                || Path.GetFileName(update.PackageName) != update.PackageName || update.PackageName == "." || update.PackageName == ".."
                || string.IsNullOrWhiteSpace(update.Sha256) || update.Sha256.Length != 64)
                throw new InvalidOperationException("Manifiesto de actualización demo inválido.");
            foreach (char c in update.Sha256) if (!Uri.IsHexDigit(c)) throw new InvalidOperationException("Hash demo inválido.");
        }
        public async Task<string> PrepareAsync(UpdateInfo update, IProgress<int> progress, CancellationToken cancellationToken)
        {
            Validate(update);
            cancellationToken.ThrowIfCancellationRequested();
            var input = Path.Combine(sourceDirectory, update.PackageName);
            Directory.CreateDirectory(stagingDirectory);
            var destination = Path.Combine(stagingDirectory, "steelog-demo-" + Guid.NewGuid().ToString("N") + ".paquete");
            var temporary = destination + ".part";
            try
            {
                using (var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                using (var target = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                using (var hash = SHA256.Create())
                {
                    var buffer = new byte[81920]; long count = 0; int read;
                    while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
                    {
                        await target.WriteAsync(buffer, 0, read, cancellationToken);
                        hash.TransformBlock(buffer, 0, read, buffer, 0);
                        count += read;
                        progress?.Report(source.Length == 0 ? 100 : (int)(count * 100 / source.Length));
                    }
                    hash.TransformFinalBlock(new byte[0], 0, 0);
                    var actual = BitConverter.ToString(hash.Hash).Replace("-", "");
                    if (!string.Equals(actual, update.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("El hash del paquete demo no coincide.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporary, destination);
                progress?.Report(100);
                return destination;
            }
            catch { if (File.Exists(temporary)) File.Delete(temporary); throw; }
        }
    }
}
