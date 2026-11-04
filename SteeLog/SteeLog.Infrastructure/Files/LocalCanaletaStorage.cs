using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Canaletas;
namespace SteeLog.Infrastructure.Files
{
    public sealed class LocalCanaletaStorage : IArchivosCanaleta
    {
        private readonly string root, pendientes;
        private readonly Dictionary<string, FileStream> locks = new Dictionary<string, FileStream>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate = new object();
        public LocalCanaletaStorage(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Falta la raíz de almacenamiento.");
            this.root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            pendientes = Path.Combine(this.root, ".pendientes-canaletas");
        }

        private string Resolver(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.IndexOf(':') >= 0) throw new ArgumentException("Ruta de foto inválida.");
            var partes = path.Replace('\\', '/').Split('/');
            var limpios = new string[partes.Length];
            for (int i = 0; i < partes.Length; i++)
            {
                var limpio = partes[i]; if (limpio.EndsWith(".", StringComparison.Ordinal) || limpio.EndsWith(" ", StringComparison.Ordinal)) throw new ArgumentException("Ruta de foto inválida.");
                if (limpio == ".." || limpio == "." || limpio.Length == 0) throw new ArgumentException("Ruta de foto inválida.");
                limpios[i] = limpio;
            }
            var rutaNormalizada = string.Join(Path.DirectorySeparatorChar.ToString(), limpios);
            var completo = Path.GetFullPath(Path.Combine(root, rutaNormalizada));
            if (!completo.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Ruta de foto inválida.");
            ComprobarReparse(completo);
            return completo;
        }

        private static void ComprobarReparse(string path)
        {
            var actual = path;
            while (!string.IsNullOrEmpty(actual))
            {
                if ((File.Exists(actual) || Directory.Exists(actual)) && (File.GetAttributes(actual) & FileAttributes.ReparsePoint) != 0) throw new IOException("La ruta atraviesa un enlace de archivos.");
                var padre = Path.GetDirectoryName(actual);
                if (padre == actual) break;
                actual = padre;
            }
        }

        private static string Clave(string path) => path.Replace('\\', '/').ToUpperInvariant();

        private string Hash(string path)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Clave(path)))).Replace("-", "");
        }

        private string Candado(string path) => Path.Combine(pendientes, Hash(path) + ".lock");

        private void Tomar(string path)
        {
            Resolver(path);
            Directory.CreateDirectory(pendientes);
            ComprobarReparse(pendientes);
            lock (gate)
            {
                if (locks.ContainsKey(Clave(path))) throw new IOException("La foto ya tiene una operación en curso.");
                var f = new FileStream(Candado(path), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                locks.Add(Clave(path), f);
            }
        }

        private void Soltar(string path)
        {
            lock (gate)
            {
                if (locks.TryGetValue(Clave(path), out var f))
                {
                    locks.Remove(Clave(path));
                    try { f.Dispose(); } catch { }
                    try
                    {
                        var c = Candado(path);
                        if (File.Exists(c)) File.Delete(c);
                    }
                    catch { }
                }
            }
        }

        private string Marca(string path) => Path.Combine(pendientes, Hash(path) + ".pending");

        private string Propiedad(string path) => Path.Combine(pendientes, Hash(path) + ".owned");

        private void Acreditar(string path)
        {
            using (var f = new FileStream(Propiedad(path), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var w = new StreamWriter(f, Encoding.UTF8))
            {
                w.Write(path);
                w.Flush();
                f.Flush(true);
            }
        }

        private bool EsPropio(string path)
        {
            var marca = Propiedad(path);
            if (!File.Exists(marca)) return false;
            ComprobarReparse(marca);
            return Clave(File.ReadAllText(marca, Encoding.UTF8).Trim()) == Clave(path);
        }

        public Task PendienteAsync(string path, CancellationToken t)
        {
            t.ThrowIfCancellationRequested();
            Tomar(path);
            try
            {
                var marca = Marca(path);
                if (!File.Exists(marca))
                {
                    using (var f = new FileStream(marca, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) using (var w = new StreamWriter(f, Encoding.UTF8))
                    {
                        w.Write(path);
                        w.Flush();
                        f.Flush(true);
                    }
                }
                return Task.CompletedTask;
            }
            catch
            {
                Soltar(path);
                throw;
            }
        }

        public async Task<string> PrepararAsync(int id, byte[] bytes, string extension, CancellationToken t)
        {
            if (id <= 0) throw new ArgumentException("Canaleta inválida.");
            var path = "canaletas/" + id + "/f6_" + Guid.NewGuid().ToString("N") + extension;
            await PendienteAsync(path, t).ConfigureAwait(false);
            try
            {
                var destino = Resolver(path);
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                ComprobarReparse(destino);
                var stage = Resolver(path + ".stage");
                using (var f = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    try
                    {
                        Acreditar(path + ".stage");
                    }
                    catch
                    {
                        f.Dispose();
                        File.Delete(stage);
                        throw;
                    }
                    await f.WriteAsync(bytes, 0, bytes.Length, t).ConfigureAwait(false);
                    f.Flush(true);
                }
                t.ThrowIfCancellationRequested();
                ComprobarReparse(destino);
                File.Move(stage, destino);
                try
                {
                    Acreditar(path);
                }
                catch
                {
                    // La promoción acaba de crear este destino; nunca borra uno preexistente.
                    File.Delete(destino);
                    throw;
                }
                File.Delete(Propiedad(path + ".stage"));
                return path;
            }
            catch
            {
                try
                {
                    await CompletarAsync(path, true, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    Soltar(path);
                }
                throw;
            }
        }

        public Task<byte[]> LeerAsync(string path, CancellationToken t)
        {
            t.ThrowIfCancellationRequested();
            var destino = Resolver(path);
            return Task.FromResult(File.Exists(destino) ? File.ReadAllBytes(destino) : new byte[0]);
        }

        public Task CompletarAsync(string path, bool eliminar, CancellationToken t)
        {
            t.ThrowIfCancellationRequested();
            bool tiene;
            lock (gate) tiene = locks.ContainsKey(Clave(path));
            if (!tiene) Tomar(path);
            try
            {
                var destino = Resolver(path);
                if (eliminar && File.Exists(destino))
                {
                    if (!EsPropio(path)) throw new IOException("La foto preexistente requiere conciliación manual.");
                    File.Delete(destino);
                    File.Delete(Propiedad(path));
                }
                var stage = Resolver(path + ".stage");
                if (File.Exists(stage))
                {
                    if (!EsPropio(path + ".stage")) throw new IOException("El archivo temporal requiere conciliación manual.");
                    File.Delete(stage);
                    File.Delete(Propiedad(path + ".stage"));
                }
                var marca = Marca(path);
                if (File.Exists(marca)) File.Delete(marca);
                return Task.CompletedTask;
            }
            finally
            {
                Soltar(path);
            }
        }

        public Task<bool> TomarPendienteAsync(string path, CancellationToken t)
        {
            t.ThrowIfCancellationRequested();
            try
            {
                Tomar(path);
                return Task.FromResult(true);
            }
            catch (IOException)
            {
                return Task.FromResult(false);
            }
        }

        public Task LiberarAsync(string path)
        {
            Soltar(path);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> PendientesAsync(CancellationToken t)
        {
            var lista = new List<string>();
            if (!Directory.Exists(pendientes)) return Task.FromResult((IReadOnlyList<string>)lista);
            ComprobarReparse(pendientes);
            foreach (var f in Directory.GetFiles(pendientes, "*.pending"))
            {
                t.ThrowIfCancellationRequested();
                ComprobarReparse(f);
                var p = File.ReadAllText(f, Encoding.UTF8);
                if (Path.GetFileName(f) == Hash(p) + ".pending") lista.Add(p);
            }
            return Task.FromResult((IReadOnlyList<string>)lista);
        }
    }
}
