using System;
using System.IO;
using System.Runtime.Serialization;
using SteeLog.Core.Application.Respaldos;

namespace SteeLog.Infrastructure.Respaldos
{
    [DataContract]
    internal sealed class ConfiguracionJson
    {
        [DataMember(Name = "carpeta", Order = 1)] public string Carpeta { get; set; }
        [DataMember(Name = "automaticoActivo", Order = 2)] public bool AutomaticoActivo { get; set; }
        [DataMember(Name = "horasEntreAutomaticos", Order = 3)] public int Horas { get; set; }
        [DataMember(Name = "conservar", Order = 4)] public int Conservar { get; set; }
    }

    /// <summary>Configuración de copias en un JSON local (carpeta de datos local o raíz demo).</summary>
    public sealed class ConfiguracionRespaldoArchivo : IConfiguracionRespaldos
    {
        private readonly string archivo; private readonly string carpetaPorDefecto;
        public ConfiguracionRespaldoArchivo(string archivo, string carpetaPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(archivo) || !Path.IsPathRooted(archivo)) throw new ArgumentException("Se requiere una ruta absoluta.", nameof(archivo));
            if (string.IsNullOrWhiteSpace(carpetaPorDefecto) || !Path.IsPathRooted(carpetaPorDefecto)) throw new ArgumentException("Se requiere una carpeta absoluta.", nameof(carpetaPorDefecto));
            this.archivo = archivo; this.carpetaPorDefecto = carpetaPorDefecto;
        }

        /// <summary>Valores por defecto si el archivo no existe o está dañado; los valores fuera de rango se corrigen.</summary>
        public ConfiguracionRespaldo Cargar()
        {
            var cfg = new ConfiguracionRespaldo { Carpeta = carpetaPorDefecto };
            try
            {
                if (!File.Exists(archivo)) return cfg;
                var j = JsonArchivo.Leer<ConfiguracionJson>(archivo);
                if (j == null) return cfg;
                if (!string.IsNullOrWhiteSpace(j.Carpeta) && Path.IsPathRooted(j.Carpeta)) cfg.Carpeta = j.Carpeta;
                cfg.AutomaticoActivo = j.AutomaticoActivo;
                cfg.HorasEntreAutomaticos = j.Horas >= 1 && j.Horas <= ConfiguracionRespaldo.HorasMaximo ? j.Horas : ConfiguracionRespaldo.HorasPorDefecto;
                cfg.Conservar = j.Conservar >= 1 && j.Conservar <= ConfiguracionRespaldo.ConservarMaximo ? j.Conservar : ConfiguracionRespaldo.ConservarPorDefecto;
            }
            catch (Exception) { return new ConfiguracionRespaldo { Carpeta = carpetaPorDefecto }; }
            return cfg;
        }

        public void Guardar(ConfiguracionRespaldo c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            Directory.CreateDirectory(Path.GetDirectoryName(archivo));
            JsonArchivo.Escribir(archivo, new ConfiguracionJson { Carpeta = c.Carpeta, AutomaticoActivo = c.AutomaticoActivo, Horas = c.HorasEntreAutomaticos, Conservar = c.Conservar });
        }
    }
}
