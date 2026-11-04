using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Domain.Canaletas;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Infrastructure.Files;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CanaletaCoreTests
    {
        [Fact]
        public void AgregadoValidaObraDescripcionPrecioYOrden()
        {
            Assert.Throws<ArgumentException>(() => new CanaletaPlegado(0, 0, "Canaleta", 0m, false));
            Assert.Throws<ArgumentException>(() => new CanaletaPlegado(0, 1, " ", 0m, false));
            Assert.Throws<ArgumentException>(() => new CanaletaPlegado(0, 1, "Canaleta", 1.001m, false));
            Assert.Throws<ArgumentException>(() => new CanaletaPlegado(0, 1, "Canaleta", 1000000000000000000m, false));
            var canaleta = new CanaletaPlegado(7, 1, " A ", 100m, false);
            canaleta.Modificar(2, " B ", 200.50m, true);
            Assert.Equal("B", canaleta.Descripcion);
            Assert.Equal(2, canaleta.ObraId);
            Assert.Equal(200.50m, canaleta.Precio);
            Assert.True(canaleta.Pagado);
            Assert.Equal(4, canaleta.SiguienteOrden(3));
            Assert.Throws<InvalidOperationException>(() => canaleta.SiguienteOrden(int.MaxValue));
        }

        [Fact]
        public void MetadataValidaSuContrato()
        {
            var foto = new FotoCanaleta("canaletas/1/a.png", "a.png", "image/png", 4, 2);
            Assert.Equal(2, foto.Orden);
            Assert.Throws<ArgumentException>(() => new FotoCanaleta("a", "a", "image/gif", 1, 1));
            Assert.Throws<ArgumentException>(() => new FotoCanaleta("a", "a", "image/png", 8388609, 1));
            Assert.Throws<ArgumentException>(() => new FotoCanaleta("a", "a", "image/png", 1, 0));
        }

        [Fact]
        public async Task StorageBloqueaPendienteActivoYRechazaRutasExternas()
        {
            var root = Path.Combine(Path.GetTempPath(), "SteeLog-canaleta-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var storage = new LocalCanaletaStorage(root);
                var otro = new LocalCanaletaStorage(root);
                var path = await storage.PrepararAsync(5, new byte[] { 1, 2, 3 }, ".png", CancellationToken.None);
                Assert.False(await otro.TomarPendienteAsync(path, CancellationToken.None));
                Assert.False(await storage.TomarPendienteAsync(path, CancellationToken.None));
                Assert.False(await otro.TomarPendienteAsync(path.ToUpperInvariant().Replace('/', '\\'), CancellationToken.None));
                Assert.Equal(new byte[] { 1, 2, 3 }, await storage.LeerAsync(path, CancellationToken.None));
                await Assert.ThrowsAsync<ArgumentException>(() => storage.LeerAsync("../fuera.png", CancellationToken.None));
                await Assert.ThrowsAsync<ArgumentException>(() => storage.LeerAsync("canaletas /5/foto.png", CancellationToken.None));
                await Assert.ThrowsAsync<ArgumentException>(() => storage.LeerAsync("canaletas/5/foto.png.", CancellationToken.None));
                await Assert.ThrowsAsync<ArgumentException>(() => storage.LeerAsync(Path.GetFullPath(Path.Combine(root, "foto.png")), CancellationToken.None));
                await storage.CompletarAsync(path, false, CancellationToken.None);
                Assert.Empty(await storage.PendientesAsync(CancellationToken.None));
                await otro.PendienteAsync(path, CancellationToken.None);
                await otro.CompletarAsync(path, true, CancellationToken.None);
                Assert.False(File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))));
            }
            finally { EliminarRaizTemporal(root); }
        }

        [Fact]
        public async Task StorageConservaFotosPreexistentesSinCertificado()
        {
            var root = Path.Combine(Path.GetTempPath(), "SteeLog-canaleta-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "canaletas", "1"));
            var path = "canaletas/1/f6_preexistente.png";
            var archivo = Path.Combine(root, "canaletas", "1", "f6_preexistente.png");
            File.WriteAllBytes(archivo, new byte[] { 4 });
            try
            {
                var storage = new LocalCanaletaStorage(root);
                await storage.PendienteAsync(path, CancellationToken.None);
                await Assert.ThrowsAsync<IOException>(() => storage.CompletarAsync(path, true, CancellationToken.None));
                Assert.True(File.Exists(archivo));
                Assert.Contains(path, await storage.PendientesAsync(CancellationToken.None));
            }
            finally { EliminarRaizTemporal(root); }
        }
        [Fact]
        public async Task StorageCompensaFalloAlCrearDirectorioDestino()
        {
            var root = Path.Combine(Path.GetTempPath(), "SteeLog-canaleta-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "canaletas"));
            File.WriteAllText(Path.Combine(root, "canaletas", "5"), "ajeno");
            try
            {
                var storage = new LocalCanaletaStorage(root);
                await Assert.ThrowsAnyAsync<IOException>(() => storage.PrepararAsync(5, new byte[] { 1 }, ".png", CancellationToken.None));
                Assert.Empty(await storage.PendientesAsync(CancellationToken.None));
                Assert.Equal("ajeno", File.ReadAllText(Path.Combine(root, "canaletas", "5")));
            }
            finally { EliminarRaizTemporal(root); }
        }

        private static void EliminarRaizTemporal(string root)
        {
            var parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(root);
            var name = Path.GetFileName(full);
            if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) ||
                !System.Text.RegularExpressions.Regex.IsMatch(name, "^SteeLog-canaleta-test-[0-9a-f]{32}$"))
                throw new InvalidOperationException("Directorio de prueba inválido.");
            Directory.Delete(full, true);
        }

        [Fact]
        public void ValidadorAceptaTiposYRechazaTruncadosOTamaniosInvalidos()
        {
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/aXQAAAAASUVORK5CYII=");
            var jpeg = new byte[] { 255,216,255,192,0,8,8,0,1,0,1,1,255,218,0,2,0,255,217 };
            var webp = new byte[] { 82,73,70,70,18,0,0,0,87,69,66,80,86,80,56,76,5,0,0,0,47,0,0,0,0,0 };
            Assert.Equal("image/png", OperacionesCanaleta.ValidarTipoFoto(png));
            Assert.Equal("image/jpeg", OperacionesCanaleta.ValidarTipoFoto(jpeg));
            Assert.Equal("image/webp", OperacionesCanaleta.ValidarTipoFoto(webp));
            Assert.Throws<ArgumentException>(() => OperacionesCanaleta.ValidarTipoFoto(null));
            Assert.Throws<ArgumentException>(() => OperacionesCanaleta.ValidarTipoFoto(new byte[0]));
            Assert.Throws<ArgumentException>(() => OperacionesCanaleta.ValidarTipoFoto(new byte[8388609]));
            Assert.Throws<ArgumentException>(() => OperacionesCanaleta.ValidarTipoFoto(new byte[] { 71,73,70,56,57,97 }));
            Array.Resize(ref png, png.Length - 1);
            Assert.Throws<ArgumentException>(() => OperacionesCanaleta.ValidarTipoFoto(png));
        }
    }
}
