using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SteeLog.Core.Application.Bitacora;

namespace SteeLog.Infrastructure.Bitacora
{
    /// <summary>
    /// Cola local en JSONL (una línea por evento) de los eventos que no se pudieron subir a la bitácora.
    /// Tiene tope de tamaño: al superarlo se descartan los más viejos. Nunca lanza desde Pendientes/Quitar.
    /// </summary>
    public sealed class ColaBitacoraArchivo : IColaBitacora
    {
        public const string NombreArchivo = "bitacora_pendiente.jsonl";
        public const long TopeBytesPorDefecto = 1024 * 1024;
        // Una sola cola por archivo en el proceso, aunque haya varias instancias.
        private static readonly object candado = new object();
        private readonly string ruta; private readonly long topeBytes;

        public ColaBitacoraArchivo(string carpetaDatos, long topeBytes = TopeBytesPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(carpetaDatos)) throw new ArgumentException("Falta la carpeta de datos.", nameof(carpetaDatos));
            if (topeBytes < 1024) throw new ArgumentOutOfRangeException(nameof(topeBytes));
            ruta = Path.Combine(carpetaDatos, NombreArchivo); this.topeBytes = topeBytes;
        }

        public string Ruta => ruta;

        public void Agregar(EventoBitacoraApp evento)
        {
            if (evento == null) throw new ArgumentNullException(nameof(evento));
            var linea = Serializar(evento);
            lock (candado)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                var info = new FileInfo(ruta);
                if (info.Exists && info.Length + linea.Length > topeBytes) Recortar(linea.Length);
                File.AppendAllText(ruta, linea + Environment.NewLine, new UTF8Encoding(false));
            }
        }

        public IReadOnlyList<EventoBitacoraApp> Pendientes()
        {
            lock (candado)
            {
                var res = new List<EventoBitacoraApp>(); var vistos = new HashSet<Guid>();
                foreach (var linea in Leer())
                {
                    var e = Deserializar(linea);
                    if (e != null && vistos.Add(e.Id)) res.Add(e);
                }
                return res;
            }
        }

        public void Quitar(IReadOnlyCollection<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return;
            var quitar = new HashSet<Guid>(ids);
            lock (candado)
            {
                if (!File.Exists(ruta)) return;
                var quedan = Leer().Where(l => { var e = Deserializar(l); return e != null && !quitar.Contains(e.Id); }).ToList();
                Reescribir(quedan);
            }
        }

        private List<string> Leer()
        {
            try { return File.Exists(ruta) ? File.ReadAllLines(ruta, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).ToList() : new List<string>(); }
            catch (IOException) { return new List<string>(); }
        }

        private void Reescribir(List<string> lineas)
        {
            if (lineas.Count == 0) { File.Delete(ruta); return; }
            var temporal = ruta + ".tmp";
            File.WriteAllLines(temporal, lineas, new UTF8Encoding(false));
            if (File.Exists(ruta)) File.Delete(ruta);
            File.Move(temporal, ruta);
        }

        // Descarta los más viejos hasta dejar lugar (y un margen del 25%) para el evento nuevo.
        private void Recortar(int bytesNuevo)
        {
            var lineas = Leer();
            long limite = topeBytes * 3 / 4 - bytesNuevo; long total = 0; int desde = lineas.Count;
            while (desde > 0 && total + Encoding.UTF8.GetByteCount(lineas[desde - 1]) + 2 <= limite) { desde--; total += Encoding.UTF8.GetByteCount(lineas[desde]) + 2; }
            Reescribir(lineas.Skip(desde).ToList());
        }

        [DataContract]
        private sealed class Linea
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "fecha")] public string Fecha { get; set; }
            [DataMember(Name = "usuario")] public string Usuario { get; set; }
            [DataMember(Name = "rol")] public string Rol { get; set; }
            [DataMember(Name = "equipo")] public string Equipo { get; set; }
            [DataMember(Name = "version")] public string Version { get; set; }
            [DataMember(Name = "accion")] public string Accion { get; set; }
            [DataMember(Name = "entidad")] public string Entidad { get; set; }
            [DataMember(Name = "descripcion")] public string Descripcion { get; set; }
        }

        private static readonly DataContractJsonSerializer serializador = new DataContractJsonSerializer(typeof(Linea));

        private static string Serializar(EventoBitacoraApp e)
        {
            var dto = new Linea
            {
                Id = e.Id.ToString("D"), Fecha = e.Fecha.ToString("O", CultureInfo.InvariantCulture), Usuario = e.Usuario, Rol = e.Rol,
                Equipo = e.Equipo, Version = e.Version, Accion = e.Accion, Entidad = e.Entidad, Descripcion = e.Descripcion
            };
            using (var ms = new MemoryStream())
            {
                serializador.WriteObject(ms, dto);
                return Encoding.UTF8.GetString(ms.ToArray()); // JSON compacto: sin saltos de línea
            }
        }

        // Línea corrupta o incompleta: se descarta (devuelve null).
        private static EventoBitacoraApp Deserializar(string linea)
        {
            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(linea)))
                {
                    var d = (Linea)serializador.ReadObject(ms);
                    if (d == null || !Guid.TryParse(d.Id, out var id) || string.IsNullOrEmpty(d.Accion)) return null;
                    if (!DateTimeOffset.TryParse(d.Fecha, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fecha)) return null;
                    return new EventoBitacoraApp { Id = id, Fecha = fecha.ToUniversalTime(), Usuario = d.Usuario, Rol = d.Rol, Equipo = d.Equipo, Version = d.Version, Accion = d.Accion, Entidad = d.Entidad, Descripcion = d.Descripcion };
                }
            }
            catch (Exception) { return null; }
        }
    }
}
