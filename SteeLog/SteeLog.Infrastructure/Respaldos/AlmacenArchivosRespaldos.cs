using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SteeLog.Core.Application.Respaldos;

namespace SteeLog.Infrastructure.Respaldos
{
    [DataContract]
    internal sealed class FichaJson
    {
        [DataMember(Name = "fecha", Order = 1)] public string Fecha { get; set; }
        [DataMember(Name = "tipo", Order = 2)] public string Tipo { get; set; }
        [DataMember(Name = "usuario", Order = 3)] public string Usuario { get; set; }
        [DataMember(Name = "versionApp", Order = 4)] public string VersionApp { get; set; }
        [DataMember(Name = "versionServidor", Order = 5)] public string VersionServidor { get; set; }
        [DataMember(Name = "base", Order = 6)] public string Base { get; set; }
        [DataMember(Name = "tamanoBytes", Order = 7)] public long TamanoBytes { get; set; }
        [DataMember(Name = "sha256", Order = 8)] public string Sha256 { get; set; }
        [DataMember(Name = "nota", Order = 9)] public string Nota { get; set; }
        [DataMember(Name = "filas", Order = 10)] public Dictionary<string, long> Filas { get; set; }
    }

    /// <summary>JSON con sangría (legible a mano) usando solo el .NET Framework.</summary>
    internal static class JsonArchivo
    {
        private static readonly DataContractJsonSerializerSettings Ajustes = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };

        public static byte[] Serializar<T>(T valor)
        {
            using (var ms = new MemoryStream())
            {
                using (var w = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, false, true, "  "))
                { new DataContractJsonSerializer(typeof(T), Ajustes).WriteObject(w, valor); w.Flush(); }
                return ms.ToArray();
            }
        }

        public static T Leer<T>(string ruta) where T : class
        {
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return (T)new DataContractJsonSerializer(typeof(T), Ajustes).ReadObject(fs);
        }

        /// <summary>Escribe en un temporal y reemplaza: nunca queda un JSON a medias.</summary>
        public static void Escribir<T>(string ruta, T valor)
        {
            var temporal = ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporal, Serializar(valor));
                if (File.Exists(ruta)) File.Replace(temporal, ruta, null); else File.Move(temporal, ruta);
            }
            finally { try { if (File.Exists(temporal)) File.Delete(temporal); } catch { } }
        }
    }

    /// <summary>Carpeta de copias en disco: .dump + ficha .json, temporales, bloqueo y borrado seguro.</summary>
    public sealed class AlmacenArchivosRespaldos : IAlmacenRespaldos
    {
        public const string NombreBloqueo = ".respaldos.lock";
        public const string PrefijoTemporal = ".parcial_";
        public const string ExtensionTemporal = ".tmp";

        public void AsegurarCarpeta(string carpeta) { ValidarCarpeta(carpeta); Directory.CreateDirectory(carpeta); }

        public void ComprobarEscritura(string carpeta)
        {
            AsegurarCarpeta(carpeta);
            var prueba = Path.Combine(carpeta, PrefijoTemporal + Guid.NewGuid().ToString("N") + ExtensionTemporal);
            File.WriteAllText(prueba, "prueba");
            File.Delete(prueba);
        }

        private static void ValidarCarpeta(string carpeta)
        {
            if (string.IsNullOrWhiteSpace(carpeta) || !Path.IsPathRooted(carpeta)) throw new ArgumentException("La carpeta de copias debe ser una ruta absoluta.");
        }

        public IReadOnlyList<ArchivoRespaldo> Listar(string carpeta)
        {
            var lista = new List<ArchivoRespaldo>();
            if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta)) return lista;
            foreach (var ruta in Directory.GetFiles(carpeta, "*" + NombresRespaldo.Extension))
            {
                var nombre = Path.GetFileName(ruta);
                if (!NombresRespaldo.IdValido(nombre)) continue;
                try
                {
                    var f = new FileInfo(ruta);
                    lista.Add(new ArchivoRespaldo { Nombre = nombre, TamanoBytes = f.Length, FechaModificacion = new DateTimeOffset(f.LastWriteTimeUtc, TimeSpan.Zero), Ficha = LeerFicha(carpeta, nombre) });
                }
                catch (IOException) { /* desapareció mientras se listaba */ }
            }
            return lista;
        }

        public FichaRespaldo LeerFicha(string carpeta, string id)
        {
            if (!NombresRespaldo.IdValido(id)) return null;
            var ruta = Path.Combine(carpeta, NombresRespaldo.NombreFicha(id));
            if (!File.Exists(ruta)) return null;
            try
            {
                var j = JsonArchivo.Leer<FichaJson>(ruta);
                if (j == null || string.IsNullOrWhiteSpace(j.Fecha)) return null;
                if (!DateTimeOffset.TryParse(j.Fecha, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var fecha)) return null;
                if (!Enum.TryParse<TipoRespaldo>(j.Tipo, true, out var tipo)) return null;
                return new FichaRespaldo
                {
                    FechaUtc = fecha.ToUniversalTime(), Tipo = tipo, Usuario = j.Usuario, VersionApp = j.VersionApp, VersionServidor = j.VersionServidor, Base = j.Base,
                    TamanoBytes = j.TamanoBytes, Sha256 = j.Sha256, Nota = j.Nota, ResumenFilas = j.Filas ?? new Dictionary<string, long>()
                };
            }
            catch (Exception) { return null; }
        }

        public string RutaDe(string carpeta, string id)
        {
            if (string.IsNullOrWhiteSpace(carpeta) || !NombresRespaldo.IdValido(id)) return null;
            var ruta = Path.GetFullPath(Path.Combine(carpeta, id));
            var esperado = Path.GetFullPath(carpeta).TrimEnd('\\', '/');
            if (!string.Equals(Path.GetDirectoryName(ruta), esperado, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(ruta) ? ruta : null;
        }

        public string NuevaRutaTemporal(string carpeta) => Path.Combine(carpeta, PrefijoTemporal + Guid.NewGuid().ToString("N") + ExtensionTemporal);

        public string Publicar(string temporal, string carpeta, DateTime fechaLocal, TipoRespaldo tipo)
        {
            for (var n = 1; n < 1000; n++)
            {
                var nombre = NombresRespaldo.Nombre(fechaLocal, tipo, n);
                var destino = Path.Combine(carpeta, nombre);
                if (File.Exists(destino) || File.Exists(Path.Combine(carpeta, NombresRespaldo.NombreFicha(nombre)))) continue;
                try { File.Move(temporal, destino); return nombre; }
                catch (IOException) when (File.Exists(destino)) { /* otro proceso lo creó: probar con el siguiente número */ }
            }
            throw new IOException("No se pudo elegir un nombre libre para la copia.");
        }

        public void EscribirFicha(string carpeta, string id, FichaRespaldo ficha)
        {
            if (!NombresRespaldo.IdValido(id)) throw new ArgumentException("Identificador inválido.", nameof(id));
            JsonArchivo.Escribir(Path.Combine(carpeta, NombresRespaldo.NombreFicha(id)), new FichaJson
            {
                Fecha = ficha.FechaUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture), Tipo = ficha.Tipo.ToString(), Usuario = ficha.Usuario,
                VersionApp = ficha.VersionApp, VersionServidor = ficha.VersionServidor, Base = ficha.Base, TamanoBytes = ficha.TamanoBytes, Sha256 = ficha.Sha256,
                Nota = ficha.Nota, Filas = ficha.ResumenFilas == null ? new Dictionary<string, long>() : new Dictionary<string, long>(ficha.ResumenFilas.ToDictionary(p => p.Key, p => p.Value))
            });
        }

        public void Eliminar(string carpeta, string id)
        {
            var ruta = RutaDe(carpeta, id);
            if (ruta == null) throw new FileNotFoundException("La copia no existe en la carpeta de copias.");
            File.Delete(ruta);
            var ficha = Path.Combine(carpeta, NombresRespaldo.NombreFicha(id));
            if (File.Exists(ficha)) File.Delete(ficha);
        }

        public void BorrarSiExiste(string ruta)
        {
            try { if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta)) File.Delete(ruta); } catch { }
        }

        public bool ExisteArchivo(string ruta) { try { return !string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta); } catch { return false; } }

        public long TamanoDe(string ruta) => new FileInfo(ruta).Length;

        public string CalcularSha256(string ruta, CancellationToken token)
        {
            using (var sha = SHA256.Create())
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
            {
                var buffer = new byte[1 << 20]; int n;
                while ((n = fs.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); sha.TransformBlock(buffer, 0, n, null, 0); }
                sha.TransformFinalBlock(buffer, 0, 0);
                return string.Concat(sha.Hash.Select(b => b.ToString("x2")));
            }
        }

        public void Copiar(string origen, string destino, CancellationToken token)
        {
            try
            {
                using (var entrada = new FileStream(origen, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
                using (var salida = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
                {
                    var buffer = new byte[1 << 20]; int n;
                    while ((n = entrada.Read(buffer, 0, buffer.Length)) > 0) { token.ThrowIfCancellationRequested(); salida.Write(buffer, 0, n); }
                }
            }
            catch { BorrarSiExiste(destino); throw; }
        }

        public long? EspacioLibre(string carpeta)
        {
            try
            {
                var raiz = Path.GetPathRoot(Path.GetFullPath(carpeta));
                if (string.IsNullOrEmpty(raiz) || raiz.StartsWith("\\\\", StringComparison.Ordinal)) return null;
                return new DriveInfo(raiz).AvailableFreeSpace;
            }
            catch { return null; }
        }

        public IDisposable Bloquear(string carpeta)
        {
            AsegurarCarpeta(carpeta);
            try
            {
                return new FileStream(Path.Combine(carpeta, NombreBloqueo), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException) { return null; }
        }
    }
}
