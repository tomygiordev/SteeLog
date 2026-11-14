using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Bitacora
{
    /// <summary>Evento de la aplicación (origen 'APP') pendiente de guardarse en la bitácora.</summary>
    public sealed class EventoBitacoraApp
    {
        /// <summary>Identifica el evento para no duplicarlo al reintentar.</summary>
        public Guid Id { get; set; }
        /// <summary>Instante UTC del evento.</summary>
        public DateTimeOffset Fecha { get; set; }
        public string Usuario { get; set; }
        public string Rol { get; set; }
        public string Equipo { get; set; }
        public string Version { get; set; }
        public string Accion { get; set; }
        public string Entidad { get; set; }
        public string Descripcion { get; set; }
    }

    public interface IBitacoraEscritura
    {
        /// <summary>
        /// Guarda los eventos en orden. Devuelve los ids que ya no hay que reintentar (guardados o rechazados
        /// definitivamente por la base). Se detiene en el primer fallo transitorio (sin conexión, tabla ausente) y
        /// devuelve solo los anteriores. No lanza excepciones.
        /// </summary>
        Task<IReadOnlyCollection<Guid>> InsertarAsync(IReadOnlyList<EventoBitacoraApp> eventos, CancellationToken token = default);
    }

    /// <summary>Cola local persistente de eventos que todavía no se pudieron subir.</summary>
    public interface IColaBitacora
    {
        void Agregar(EventoBitacoraApp evento);
        IReadOnlyList<EventoBitacoraApp> Pendientes();
        void Quitar(IReadOnlyCollection<Guid> ids);
    }

    /// <summary>Registro de errores de la aplicación en la bitácora. Nunca lanza ni bloquea la UI.</summary>
    public interface IRegistroErroresBitacora
    {
        /// <summary>Error controlado (ya mostrado al usuario). Accion = ERROR.</summary>
        void Error(string descripcion, string entidad = null);
        /// <summary>Excepción no controlada. Accion = ERROR_NO_CONTROLADO.</summary>
        void ErrorNoControlado(object error, string entidad = null);
        /// <summary>Intenta subir en segundo plano los eventos que quedaron en la cola local (p. ej. al iniciar sesión).</summary>
        void ReintentarPendientes();
    }

    public sealed class RegistroErroresBitacoraNulo : IRegistroErroresBitacora
    {
        public static readonly RegistroErroresBitacoraNulo Instancia = new RegistroErroresBitacoraNulo();
        public void Error(string descripcion, string entidad = null) { }
        public void ErrorNoControlado(object error, string entidad = null) { }
        public void ReintentarPendientes() { }
    }

    public static class SaneadorTexto
    {
        public const int MaximoCaracteres = 4000;
        // Incluye valores entre comillas, que pueden contener punto y coma.
        private static readonly Regex Secretos = new Regex(
            @"\b(Password|Pwd|Host|Server|Data Source|Database|Initial Catalog|Username|User ID|UserName|Port)\s*=\s*(?:""(?:[^""]|"""")*""|'(?:[^']|'')*'|[^;\r\n]*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        /// <summary>Oculta datos de conexión/credenciales y acota el largo.</summary>
        public static string Sanear(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            string limpio;
            try { limpio = Secretos.Replace(texto, "$1=[omitido]"); }
            catch (RegexMatchTimeoutException) { limpio = "[texto omitido: no se pudo sanear]"; }
            return limpio.Length > MaximoCaracteres ? limpio.Substring(0, MaximoCaracteres) + "..." : limpio;
        }
    }

    public sealed class RegistroErroresBitacora : IRegistroErroresBitacora
    {
        public const string AccionError = "ERROR";
        public const string AccionErrorNoControlado = "ERROR_NO_CONTROLADO";
        private static readonly TimeSpan EsperaTrasFallo = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan VentanaRepetidos = TimeSpan.FromSeconds(2);

        private readonly Session sesion; private readonly IBitacoraEscritura escritura; private readonly IColaBitacora cola;
        private readonly Func<DateTimeOffset> ahora; private readonly string equipo; private readonly string version;
        private readonly Action<Action> segundoPlano;
        private readonly object candado = new object();
        private readonly List<EventoBitacoraApp> enMemoria = new List<EventoBitacoraApp>();
        private bool subiendo, repetir; private DateTimeOffset noReintentarHasta = DateTimeOffset.MinValue;
        private string ultimaClave; private DateTimeOffset ultimaFecha;

        /// <param name="segundoPlano">Ejecuta la subida fuera del hilo llamador (por defecto, el ThreadPool).</param>
        public RegistroErroresBitacora(Session sesion, IBitacoraEscritura escritura, IColaBitacora cola, Func<DateTimeOffset> ahora,
            string equipo, string version, Action<Action> segundoPlano = null)
        {
            this.sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            this.escritura = escritura ?? throw new ArgumentNullException(nameof(escritura));
            this.cola = cola ?? throw new ArgumentNullException(nameof(cola));
            this.ahora = ahora ?? throw new ArgumentNullException(nameof(ahora));
            this.equipo = equipo; this.version = version;
            this.segundoPlano = segundoPlano ?? (a => Task.Run(a));
        }

        public void Error(string descripcion, string entidad = null) => Registrar(AccionError, descripcion, entidad);

        public void ErrorNoControlado(object error, string entidad = null)
            => Registrar(AccionErrorNoControlado, error == null ? "Error sin detalle." : SafeToString(error), entidad);

        public void ReintentarPendientes() => Subir(forzar: true);

        private static string SafeToString(object o) { try { return o.ToString(); } catch { return "Error sin detalle."; } }

        private void Registrar(string accion, string descripcion, string entidad)
        {
            try
            {
                var fecha = ahora().ToUniversalTime();
                // timestamptz guarda microsegundos: se redondea para que un reenvío coincida exactamente.
                fecha = new DateTimeOffset(fecha.Ticks - fecha.Ticks % 10, TimeSpan.Zero);
                var texto = SaneadorTexto.Sanear(string.IsNullOrWhiteSpace(descripcion) ? "Error sin detalle." : descripcion);
                var entidadLimpia = string.IsNullOrWhiteSpace(entidad) ? null : SaneadorTexto.Sanear(entidad.Trim());
                var clave = accion + "|" + entidadLimpia + "|" + texto;
                lock (candado)
                {
                    // Un error que se repite en bucle no debe inundar la bitácora.
                    if (clave == ultimaClave && fecha - ultimaFecha < VentanaRepetidos) return;
                    ultimaClave = clave; ultimaFecha = fecha;
                }
                var identidad = sesion.Current;
                var evento = new EventoBitacoraApp
                {
                    Id = Guid.NewGuid(), Fecha = fecha,
                    Usuario = identidad?.Email, Rol = identidad == null ? null : identidad.Role.ToString(),
                    Equipo = equipo, Version = version, Accion = accion, Entidad = entidadLimpia, Descripcion = texto
                };
                // Primero a disco (sobrevive si la app se cierra por el error); la subida es aparte.
                try { cola.Agregar(evento); }
                catch { lock (candado) { if (enMemoria.Count < 100) enMemoria.Add(evento); } }
                Subir(forzar: false);
            }
            catch { /* la bitácora nunca debe romper la app */ }
        }

        private void Subir(bool forzar)
        {
            try
            {
                lock (candado)
                {
                    if (subiendo) { repetir = true; return; }
                    if (!forzar && ahora() < noReintentarHasta) return;
                    subiendo = true; repetir = false;
                }
                segundoPlano(Bucle);
            }
            catch { lock (candado) { subiendo = false; } }
        }

        private void Bucle()
        {
            try
            {
                while (true)
                {
                    bool fallo = false;
                    try
                    {
                        List<EventoBitacoraApp> lote;
                        lock (candado) lote = enMemoria.ToList();
                        lote.AddRange(cola.Pendientes().Where(p => lote.All(m => m.Id != p.Id)));
                        if (lote.Count > 0)
                        {
                            var hechos = escritura.InsertarAsync(lote, CancellationToken.None).GetAwaiter().GetResult();
                            if (hechos != null && hechos.Count > 0)
                            {
                                cola.Quitar(hechos);
                                lock (candado) enMemoria.RemoveAll(e => hechos.Contains(e.Id));
                            }
                            fallo = hechos == null || hechos.Count < lote.Count;
                        }
                    }
                    catch { fallo = true; }
                    lock (candado)
                    {
                        if (fallo) noReintentarHasta = ahora() + EsperaTrasFallo; else noReintentarHasta = DateTimeOffset.MinValue;
                        if (!repetir || fallo) { subiendo = false; return; }
                        repetir = false;
                    }
                }
            }
            catch { lock (candado) { subiendo = false; } }
        }
    }
}
