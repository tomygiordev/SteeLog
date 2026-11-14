using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Bitacora
{
    /// <summary>Fila de public.bitacora_auditoria (quién, qué y cuándo).</summary>
    public sealed class BitacoraRegistro
    {
        public long Id { get; set; }
        /// <summary>Hora local.</summary>
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Rol { get; set; }
        public string Equipo { get; set; }
        public string VersionApp { get; set; }
        /// <summary>BD = cambio de datos capturado por trigger; APP = evento de la aplicación.</summary>
        public string Origen { get; set; }
        public string Accion { get; set; }
        public string Entidad { get; set; }
        public string EntidadId { get; set; }
        public string Descripcion { get; set; }
        /// <summary>JSON crudo (en UPDATE solo las columnas que cambiaron).</summary>
        public string DatosAntes { get; set; }
        public string DatosDespues { get; set; }
        /// <summary>Descripción propia o, si no tiene, un resumen legible del cambio de datos.</summary>
        public string Resumen => ResumenBitacora.De(this);
    }

    public sealed class BitacoraFiltro
    {
        public const int LimitePorDefecto = 2000;
        public const int LimiteMaximo = 10000;
        /// <summary>Día inicial (hora local), inclusive.</summary>
        public DateTime? Desde { get; set; }
        /// <summary>Día final (hora local), inclusive: llega hasta el fin de ese día.</summary>
        public DateTime? Hasta { get; set; }
        public string Usuario { get; set; }
        public string Entidad { get; set; }
        public string Accion { get; set; }
        public string Texto { get; set; }
        public int Limite { get; set; } = LimitePorDefecto;

        public int LimiteEfectivo => Limite <= 0 ? LimitePorDefecto : Math.Min(Limite, LimiteMaximo);
    }

    public sealed class BitacoraResultado
    {
        public List<BitacoraRegistro> Registros { get; set; } = new List<BitacoraRegistro>();
        /// <summary>False si la tabla no existe (migración sin aplicar).</summary>
        public bool Instalada { get; set; } = true;
        /// <summary>True si había más filas que el límite pedido.</summary>
        public bool Truncado { get; set; }
        public string Mensaje => Instalada ? null : ConsultarBitacora.MensajeNoInstalada;
    }

    public sealed class BitacoraOpciones
    {
        public List<string> Usuarios { get; set; } = new List<string>();
        public List<string> Entidades { get; set; } = new List<string>();
        public List<string> Acciones { get; set; } = new List<string>();
        public bool Instalada { get; set; } = true;
        public string Mensaje => Instalada ? null : ConsultarBitacora.MensajeNoInstalada;
    }

    public interface IBitacoraConsulta
    {
        /// <summary>Tolera que la tabla no exista: devuelve vacío con Instalada=false.</summary>
        Task<BitacoraResultado> BuscarAsync(BitacoraFiltro filtro, CancellationToken token = default);
        Task<BitacoraOpciones> OpcionesAsync(CancellationToken token = default);
    }

    /// <summary>Consulta de la bitácora: solo para Admin.</summary>
    public sealed class ConsultarBitacora
    {
        public const string MensajeNoInstalada = "La bitácora no está instalada en la base de datos (falta aplicar la migración).";
        private readonly Session actor; private readonly IBitacoraConsulta consulta;
        public ConsultarBitacora(Session actor, IBitacoraConsulta consulta)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta));
        }
        public Task<BitacoraResultado> BuscarAsync(BitacoraFiltro filtro, CancellationToken token)
        { actor.Require(Role.Admin); return consulta.BuscarAsync(filtro ?? new BitacoraFiltro(), token); }
        /// <summary>Usuarios, entidades y acciones distintos para llenar los combos.</summary>
        public Task<BitacoraOpciones> OpcionesAsync(CancellationToken token)
        { actor.Require(Role.Admin); return consulta.OpcionesAsync(token); }
    }
}
