using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Actualizaciones;
using SteeLog.Infrastructure.DemoUpdates;
using Xunit;

public class UpdatesTests
{
    private static string NewDir() { var path = Path.Combine(Path.GetTempPath(), "steelog-f2-update-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
    private static string Hash(byte[] data) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", ""); }
    [Fact] public async Task Newer_fixture_stages_bytes_without_touching_other_files()
    {
        var root = NewDir(); var stage = Path.Combine(root, "stage");
        try {
            var bytes = Encoding.UTF8.GetBytes("offline demo"); File.WriteAllBytes(Path.Combine(root, "package.txt"), bytes);
            File.WriteAllText(Path.Combine(root, "manifiesto.json"), "{\"version\":\"2.0.0\",\"paquete\":\"package.txt\",\"sha256\":\"" + Hash(bytes) + "\",\"notas\":\"demo\"}");
            var source = new LocalDemoUpdateSource(root, stage); var useCase = new CheckDemoUpdate(source);
            var update = await useCase.ExecuteAsync("1.0.0", CancellationToken.None); Assert.NotNull(update);
            Assert.Null(await useCase.ExecuteAsync("2.0.0", CancellationToken.None));
            var progress = new CapturedProgress(); var staged = await new PrepareDemoUpdate(source).ExecuteAsync(update, progress, CancellationToken.None);
            Assert.Equal(bytes, File.ReadAllBytes(staged)); Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(root, "package.txt"))); Assert.Equal(100, progress.Last);
        } finally { Directory.Delete(root, true); }
    }
    [Fact] public async Task Shipped_fixture_is_copied_and_matches_its_hash()
    {
        var root = Path.GetDirectoryName(Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY"));
        var fixture = Path.Combine(root, "demo", "actualizaciones");
        var stage = NewDir();
        try {
            var source = new LocalDemoUpdateSource(fixture, stage);
            var update = await new CheckDemoUpdate(source).ExecuteAsync("1.0.0", CancellationToken.None);
            Assert.NotNull(update);
            var prepared = await source.PrepareAsync(update, null, CancellationToken.None);
            Assert.Equal(update.Sha256.ToUpperInvariant(), Hash(File.ReadAllBytes(prepared)));
        } finally { Directory.Delete(stage, true); }
    }
    [Fact] public async Task Invalid_manifest_missing_package_bad_hash_and_cancellation_fail_without_staged_file()
    {
        var root = NewDir(); var stage = Path.Combine(root, "stage");
        try {
            var source = new LocalDemoUpdateSource(root, stage);
            File.WriteAllText(Path.Combine(root, "manifiesto.json"), "{broken");
            await Assert.ThrowsAsync<InvalidOperationException>(() => source.ReadAsync(CancellationToken.None));
            var update = new UpdateInfo { Version = "2.0", PackageName = "package.txt", Sha256 = new string('0', 64) };
            await Assert.ThrowsAsync<FileNotFoundException>(() => source.PrepareAsync(update, null, CancellationToken.None));
            File.WriteAllText(Path.Combine(root, "package.txt"), "demo");
            await Assert.ThrowsAsync<InvalidDataException>(() => source.PrepareAsync(update, null, CancellationToken.None));
            var cancellation = new CancellationToken(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.PrepareAsync(update, null, cancellation));
            Assert.Empty(Directory.GetFiles(stage));
        } finally { Directory.Delete(root, true); }
    }
    [Fact] public async Task Cancelling_during_copy_removes_partial_file()
    {
        var root = NewDir(); var stage = Path.Combine(root, "stage");
        try {
            var bytes = new byte[1024 * 1024]; new Random(1).NextBytes(bytes);
            File.WriteAllBytes(Path.Combine(root, "package.txt"), bytes);
            var update = new UpdateInfo { Version = "2.0", PackageName = "package.txt", Sha256 = Hash(bytes) };
            using (var cts = new CancellationTokenSource()) {
                var progress = new CancelProgress(cts);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new LocalDemoUpdateSource(root, stage).PrepareAsync(update, progress, cts.Token));
                Assert.True(progress.Called);
                Assert.Empty(Directory.GetFiles(stage));
            }
        } finally { Directory.Delete(root, true); }
    }
    [Fact] public async Task Manifest_rejects_non_string_fields_and_unsafe_paths()
    {
        var root = NewDir();
        try {
            var source = new LocalDemoUpdateSource(root, Path.Combine(root, "stage"));
            var manifest = Path.Combine(root, "manifiesto.json");
            File.WriteAllText(manifest, "{\"version\":123,\"paquete\":\"ok\",\"sha256\":\"" + new string('0', 64) + "\",\"notas\":\"demo\"}");
            await Assert.ThrowsAsync<InvalidOperationException>(() => source.ReadAsync(CancellationToken.None));
            File.WriteAllText(manifest, "{\"version\":\"2.0\",\"paquete\":\"../outside\",\"sha256\":\"" + new string('0', 64) + "\",\"notas\":\"demo\"}");
            await Assert.ThrowsAsync<InvalidOperationException>(() => source.ReadAsync(CancellationToken.None));
        } finally { Directory.Delete(root, true); }
    }
    sealed class CancelProgress : IProgress<int> {
        private readonly CancellationTokenSource cancellation;
        public bool Called;
        public CancelProgress(CancellationTokenSource cancellation) { this.cancellation = cancellation; }
        public void Report(int value) { Called = true; cancellation.Cancel(); }
    }
    sealed class CapturedProgress : IProgress<int> { public int Last; public void Report(int value) { Last = value; } }
}
