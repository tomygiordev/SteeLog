using System;
using System.Collections.Generic;

namespace SteeLog.Core.Application.Respaldos
{
    public enum TipoRespaldo { Manual, Automatico, PreRestauracion, Importado }

    /// <summary>Una copia de seguridad (.dump) de la carpeta de copias, con su ficha si la tiene.</summary>
    public sealed class RespaldoInfo
    {
        /// <summary>Identificador estable: el nombre del archivo (no es una ruta).</summary>
        public string Id { get; set; }
        public string NombreArchivo { get; set; }
        /// <summary>Hora local.</summary>
        public DateTime Fecha { get; set; }
        public TipoRespaldo Tipo { get; set; }
        public long TamanoBytes { get; set; }
        public string Usuario { get; set; }
        public string VersionApp { get; set; }
        public string VersionServidor { get; set; }
        public string Sha256 { get; set; }
        public string Nota { get; set; }
        /// <summary>Tabla -> cantidad de filas al momento de la copia (vacío si no hay ficha).</summary>
        public IReadOnlyDictionary<string, long> ResumenFilas { get; set; } = new Dictionary<string, long>();
        /// <summary>False si el .dump se puso a mano sin su ficha JSON: los datos se derivan del archivo.</summary>
        public bool TieneFicha { get; set; }
    }

    public sealed class ConfiguracionRespaldo
    {
        public const int HorasPorDefecto = 24;
        public const int ConservarPorDefecto = 10;
        public const int HorasMaximo = 24 * 365;
        public const int ConservarMaximo = 1000;
        public string Carpeta { get; set; }
        public bool AutomaticoActivo { get; set; } = true;
        public int HorasEntreAutomaticos { get; set; } = HorasPorDefecto;
        /// <summary>Cantidad de copias automáticas que se conservan (los otros tipos nunca se borran solos).</summary>
        public int Conservar { get; set; } = ConservarPorDefecto;

        public ConfiguracionRespaldo Clonar() => new ConfiguracionRespaldo
        { Carpeta = Carpeta, AutomaticoActivo = AutomaticoActivo, HorasEntreAutomaticos = HorasEntreAutomaticos, Conservar = Conservar };
    }

    public sealed class EstadoRespaldos
    {
        /// <summary>Más nuevos primero.</summary>
        public IReadOnlyList<RespaldoInfo> Respaldos { get; set; } = new List<RespaldoInfo>();
        public RespaldoInfo Ultimo { get; set; }
        public ConfiguracionRespaldo Configuracion { get; set; }
        public bool HerramientasDisponibles { get; set; }
        public string MensajeHerramientas { get; set; }
        public string VersionHerramientas { get; set; }
        public string VersionServidor { get; set; }
        public bool BaseConectada { get; set; }
        /// <summary>Motivo si no hay conexión con la base.</summary>
        public string MensajeBase { get; set; }
        /// <summary>Hora local; null si el automático está desactivado.</summary>
        public DateTime? ProximoAutomatico { get; set; }
        /// <summary>Null si no se pudo determinar (p. ej. ruta de red).</summary>
        public long? EspacioLibreBytes { get; set; }
        public long EspacioUsadoBytes { get; set; }
    }

    public sealed class ProgresoRespaldo
    {
        public string Etapa { get; set; }
        /// <summary>Null cuando la etapa no tiene un avance medible.</summary>
        public int? Porcentaje { get; set; }
        public ProgresoRespaldo() { }
        public ProgresoRespaldo(string etapa, int? porcentaje = null) { Etapa = etapa; Porcentaje = porcentaje; }
    }

    public sealed class ResultadoVerificacion
    {
        public bool Ok { get; set; }
        public string Detalle { get; set; }
        /// <summary>Cantidad de objetos que lista pg_restore --list.</summary>
        public int Objetos { get; set; }
    }

    public sealed class ResultadoRespaldo
    {
        public bool Ok { get; set; }
        public string Detalle { get; set; }
        public RespaldoInfo Respaldo { get; set; }
    }

    public sealed class ResultadoRestauracion
    {
        public bool Ok { get; set; }
        public string Detalle { get; set; }
        /// <summary>Copia PreRestauracion creada antes de tocar la base (null si no llegó a crearse).</summary>
        public RespaldoInfo RespaldoPrevio { get; set; }
    }

    public sealed class ResultadoOperacion
    {
        public bool Ok { get; set; }
        public string Detalle { get; set; }
    }

    public sealed class ResultadoAutomatico
    {
        /// <summary>True si se intentó crear la copia (puede haber fallado: ver Ok).</summary>
        public bool Ejecutado { get; set; }
        public bool Ok { get; set; }
        public string Mensaje { get; set; }
        public RespaldoInfo Respaldo { get; set; }
    }

    // ---- Datos que intercambia el servicio con la infraestructura ----

    /// <summary>Contenido de la ficha JSON que acompaña a cada copia.</summary>
    public sealed class FichaRespaldo
    {
        public DateTimeOffset FechaUtc { get; set; }
        public TipoRespaldo Tipo { get; set; }
        public string Usuario { get; set; }
        public string VersionApp { get; set; }
        public string VersionServidor { get; set; }
        public string Base { get; set; }
        public long TamanoBytes { get; set; }
        public string Sha256 { get; set; }
        public string Nota { get; set; }
        public IReadOnlyDictionary<string, long> ResumenFilas { get; set; } = new Dictionary<string, long>();
    }

    public sealed class ArchivoRespaldo
    {
        public string Nombre { get; set; }
        public long TamanoBytes { get; set; }
        public DateTimeOffset FechaModificacion { get; set; }
        /// <summary>Null si no hay ficha o no se pudo leer.</summary>
        public FichaRespaldo Ficha { get; set; }
    }

    public sealed class InfoBase
    {
        public bool Conectada { get; set; }
        public string Mensaje { get; set; }
        public string Nombre { get; set; }
        /// <summary>Ej. "17.2". Null si no conectó.</summary>
        public string Version { get; set; }
        /// <summary>Versión mayor (17). 0 si se desconoce.</summary>
        public int VersionMayor { get; set; }
    }

    public sealed class EstadoHerramientas
    {
        public bool Disponibles { get; set; }
        public string Mensaje { get; set; }
        /// <summary>Ej. "18.0".</summary>
        public string Version { get; set; }
    }

    public sealed class ResultadoMotor
    {
        public bool Ok { get; set; }
        public string Detalle { get; set; }
        public int Objetos { get; set; }
    }
}
