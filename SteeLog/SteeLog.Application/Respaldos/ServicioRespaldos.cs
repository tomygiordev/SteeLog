using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Core.Application.Identidad;

namespace SteeLog.Core.Application.Respaldos
{
    /// <summary>
    /// Copias de seguridad de la base (pg_dump / pg_restore). Todas las operaciones exigen Admin salvo
    /// <see cref="EjecutarSiCorrespondeAsync"/>, que es una tarea del sistema (pide solo sesión iniciada).
    /// </summary>
    public sealed class ServicioRespaldos
    {
        public const string AccionRespaldo = "RESPALDO";
        public const string AccionRestauracion = "RESTAURACION";
        public const string AccionError = "RESPALDO_ERROR";
        public const string Entidad = "respaldos";
        public const string MensajeOcupado = "Ya hay otra operación de copias de seguridad en curso (en este equipo u otra instancia de la aplicación). Esperá a que termine.";
        public static readonly string[] TablasResumen = { "materiales", "movimientos", "obras", "herramientas" };

        private readonly Session sesion; private readonly IConfiguracionRespaldos configuracion; private readonly IAlmacenRespaldos almacen;
        private readonly IMotorRespaldo motor; private readonly IBaseRespaldo bd; private readonly IRegistroErroresBitacora bitacora;
        private readonly Func<DateTimeOffset> ahora; private readonly string versionApp; private readonly string raizPermitida;

        /// <param name="raizPermitida">Si no es null (modo demo), la carpeta de copias debe quedar dentro de esa raíz.</param>
        public ServicioRespaldos(Session sesion, IConfiguracionRespaldos configuracion, IAlmacenRespaldos almacen, IMotorRespaldo motor,
            IBaseRespaldo bd, IRegistroErroresBitacora bitacora, Func<DateTimeOffset> ahora, string versionApp, string raizPermitida = null)
        {
            this.sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            this.configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
            this.almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
            this.motor = motor ?? throw new ArgumentNullException(nameof(motor));
            this.bd = bd ?? throw new ArgumentNullException(nameof(bd));
            this.bitacora = bitacora ?? RegistroErroresBitacoraNulo.Instancia;
            this.ahora = ahora ?? throw new ArgumentNullException(nameof(ahora));
            this.versionApp = versionApp; this.raizPermitida = raizPermitida;
        }

        // ------------------------------------------------------------------ Estado

        public async Task<EstadoRespaldos> ObtenerEstadoAsync(CancellationToken token)
        {
            sesion.Require(Role.Admin);
            var cfg = configuracion.Cargar();
            var info = await bd.ConsultarAsync(token).ConfigureAwait(false);
            EstadoHerramientas herramientas;
            try { herramientas = await motor.DetectarAsync(token).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { herramientas = new EstadoHerramientas { Disponibles = false, Mensaje = "No se pudieron detectar las herramientas de PostgreSQL: " + ex.Message }; }
            var lista = ListarInfos(cfg.Carpeta);
            long? libre = null;
            try { libre = almacen.EspacioLibre(cfg.Carpeta); } catch { }
            DateTime? proximo = null;
            if (cfg.AutomaticoActivo)
                proximo = lista.Count == 0 ? ahora().ToLocalTime().DateTime : lista[0].Fecha.AddHours(cfg.HorasEntreAutomaticos);
            return new EstadoRespaldos
            {
                Respaldos = lista, Ultimo = lista.FirstOrDefault(), Configuracion = cfg,
                HerramientasDisponibles = herramientas.Disponibles, MensajeHerramientas = herramientas.Mensaje, VersionHerramientas = herramientas.Version,
                VersionServidor = info.Version, BaseConectada = info.Conectada, MensajeBase = info.Conectada ? null : info.Mensaje,
                ProximoAutomatico = proximo, EspacioLibreBytes = libre, EspacioUsadoBytes = lista.Sum(r => r.TamanoBytes)
            };
        }

        // ------------------------------------------------------------------ Crear

        public Task<ResultadoRespaldo> CrearAsync(string nota, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        {
            sesion.Require(Role.Admin);
            return CrearConBloqueoAsync(TipoRespaldo.Manual, nota, progreso, token);
        }

        private async Task<ResultadoRespaldo> CrearConBloqueoAsync(TipoRespaldo tipo, string nota, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        {
            ConfiguracionRespaldo cfg;
            try { cfg = configuracion.Cargar(); }
            catch (Exception ex) { return FallaCrear(tipo, "No se pudo leer la configuración: " + ex.Message); }
            IDisposable bloqueo;
            try { bloqueo = almacen.Bloquear(cfg.Carpeta); }
            catch (Exception ex) { return FallaCrear(tipo, "No se pudo usar la carpeta de copias: " + ex.Message); }
            if (bloqueo == null) return new ResultadoRespaldo { Ok = false, Detalle = MensajeOcupado };
            using (bloqueo) return await CrearInternoAsync(tipo, nota, progreso, token, cfg).ConfigureAwait(false);
        }

        // El llamador ya tiene el bloqueo de la carpeta.
        private async Task<ResultadoRespaldo> CrearInternoAsync(TipoRespaldo tipo, string nota, IProgress<ProgresoRespaldo> progreso, CancellationToken token, ConfiguracionRespaldo cfg)
        {
            string temporal = null;
            try
            {
                Reportar(progreso, "Comprobando la conexión con la base de datos");
                var info = await bd.ConsultarAsync(token).ConfigureAwait(false);
                if (!info.Conectada) return FallaCrear(tipo, info.Mensaje ?? "No hay conexión con la base de datos.");
                var herramientas = await motor.DetectarAsync(token).ConfigureAwait(false);
                if (!herramientas.Disponibles) return FallaCrear(tipo, herramientas.Mensaje ?? "No se encontraron las herramientas de PostgreSQL.");
                IReadOnlyDictionary<string, long> filas;
                try { filas = await bd.ContarFilasAsync(TablasResumen, token).ConfigureAwait(false) ?? new Dictionary<string, long>(); }
                catch (OperationCanceledException) { throw; }
                catch { filas = new Dictionary<string, long>(); }

                almacen.AsegurarCarpeta(cfg.Carpeta);
                temporal = almacen.NuevaRutaTemporal(cfg.Carpeta);
                Reportar(progreso, "Exportando la base de datos");
                var r = await motor.CrearAsync(temporal, progreso, token).ConfigureAwait(false);
                if (!r.Ok) return FallaCrear(tipo, r.Detalle ?? "pg_dump falló.");
                token.ThrowIfCancellationRequested();
                var tamano = almacen.TamanoDe(temporal);
                if (tamano <= 0) return FallaCrear(tipo, "pg_dump no generó datos (archivo vacío).");
                Reportar(progreso, "Calculando la huella SHA-256");
                var sha = almacen.CalcularSha256(temporal, token);
                token.ThrowIfCancellationRequested();

                var fecha = ahora();
                var nombre = almacen.Publicar(temporal, cfg.Carpeta, fecha.ToLocalTime().DateTime, tipo);
                temporal = null;
                var ficha = new FichaRespaldo
                {
                    FechaUtc = fecha.ToUniversalTime(), Tipo = tipo, Usuario = sesion.Current?.Email, VersionApp = versionApp,
                    VersionServidor = info.Version, Base = info.Nombre, TamanoBytes = tamano, Sha256 = sha,
                    Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(), ResumenFilas = filas
                };
                try { almacen.EscribirFicha(cfg.Carpeta, nombre, ficha); }
                catch { almacen.BorrarSiExiste(almacen.RutaDe(cfg.Carpeta, nombre)); throw; }

                var respaldo = Armar(new ArchivoRespaldo { Nombre = nombre, TamanoBytes = tamano, FechaModificacion = fecha, Ficha = ficha });
                bitacora.Evento(AccionRespaldo, "Copia de seguridad " + tipo + ": " + nombre + " (" + Tamano(tamano) + ")", Entidad);
                var detalle = "Copia creada: " + nombre + " (" + Tamano(tamano) + ").";
                if (tipo == TipoRespaldo.Automatico)
                {
                    try { var borradas = AplicarRetencion(cfg); if (borradas > 0) detalle += " Se eliminaron " + borradas + " automáticas antiguas."; }
                    catch { /* la retención nunca debe invalidar una copia ya hecha */ }
                }
                return new ResultadoRespaldo { Ok = true, Detalle = detalle, Respaldo = respaldo };
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return FallaCrear(tipo, ex.Message); }
            finally { if (temporal != null) almacen.BorrarSiExiste(temporal); }
        }

        private ResultadoRespaldo FallaCrear(TipoRespaldo tipo, string detalle)
        {
            bitacora.Evento(AccionError, "No se pudo crear la copia de seguridad " + tipo + ": " + detalle, Entidad);
            return new ResultadoRespaldo { Ok = false, Detalle = "No se pudo crear la copia de seguridad. " + detalle };
        }

        /// <summary>Conserva las últimas N automáticas CON ficha; no toca ningún otro tipo.</summary>
        private int AplicarRetencion(ConfiguracionRespaldo cfg)
        {
            var conservar = Math.Max(1, cfg.Conservar);
            var sobrantes = ListarInfos(cfg.Carpeta).Where(r => r.Tipo == TipoRespaldo.Automatico && r.TieneFicha).Skip(conservar).ToList();
            var borradas = 0;
            foreach (var r in sobrantes)
            {
                try { almacen.Eliminar(cfg.Carpeta, r.Id); borradas++; } catch { }
            }
            return borradas;
        }

        // ------------------------------------------------------------------ Verificar

        public async Task<ResultadoVerificacion> VerificarAsync(string id, CancellationToken token)
        {
            sesion.Require(Role.Admin);
            var cfg = configuracion.Cargar();
            var ruta = almacen.RutaDe(cfg.Carpeta, id);
            if (ruta == null) return new ResultadoVerificacion { Ok = false, Detalle = "La copia no existe en la carpeta de copias." };
            return await VerificarRutaAsync(cfg, id, ruta, token).ConfigureAwait(false);
        }

        private async Task<ResultadoVerificacion> VerificarRutaAsync(ConfiguracionRespaldo cfg, string id, string ruta, CancellationToken token)
        {
            try
            {
                var herramientas = await motor.DetectarAsync(token).ConfigureAwait(false);
                if (!herramientas.Disponibles) return new ResultadoVerificacion { Ok = false, Detalle = herramientas.Mensaje };
                var ficha = almacen.LeerFicha(cfg.Carpeta, id);
                string sha = null;
                if (ficha != null && !string.IsNullOrEmpty(ficha.Sha256))
                {
                    sha = almacen.CalcularSha256(ruta, token);
                    if (!string.Equals(sha, ficha.Sha256, StringComparison.OrdinalIgnoreCase))
                        return new ResultadoVerificacion { Ok = false, Detalle = "El archivo no coincide con su huella SHA-256: fue modificado o está dañado." };
                }
                var r = await motor.ListarAsync(ruta, token).ConfigureAwait(false);
                if (!r.Ok) return new ResultadoVerificacion { Ok = false, Detalle = r.Detalle ?? "pg_restore no pudo leer el archivo." };
                if (r.Objetos <= 0) return new ResultadoVerificacion { Ok = false, Detalle = "El archivo no contiene objetos de base de datos.", Objetos = 0 };
                return new ResultadoVerificacion
                {
                    Ok = true, Objetos = r.Objetos,
                    Detalle = "Copia válida: " + r.Objetos + " objetos. " + (sha != null ? "La huella SHA-256 coincide con la ficha." : "Sin ficha: solo se comprobó que pg_restore pueda leerla.")
                };
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return new ResultadoVerificacion { Ok = false, Detalle = "No se pudo verificar la copia: " + ex.Message }; }
        }

        // ------------------------------------------------------------------ Restaurar

        /// <summary>
        /// Verifica la copia, crea SIEMPRE antes una copia PreRestauracion y recién entonces restaura en una única
        /// transacción (si falla, la base queda como estaba). Si hay otras sesiones con transacciones abiertas sobre las
        /// tablas, pg_restore espera como máximo el tiempo de bloqueo del motor y falla con un mensaje claro.
        /// </summary>
        public async Task<ResultadoRestauracion> RestaurarAsync(string id, IProgress<ProgresoRespaldo> progreso, CancellationToken token)
        {
            sesion.Require(Role.Admin);
            ConfiguracionRespaldo cfg;
            try { cfg = configuracion.Cargar(); }
            catch (Exception ex) { return FallaRestaurar(id, "No se pudo leer la configuración: " + ex.Message, null); }
            var ruta = almacen.RutaDe(cfg.Carpeta, id);
            if (ruta == null) return FallaRestaurar(id, "La copia no existe en la carpeta de copias.", null);
            IDisposable bloqueo;
            try { bloqueo = almacen.Bloquear(cfg.Carpeta); }
            catch (Exception ex) { return FallaRestaurar(id, "No se pudo usar la carpeta de copias: " + ex.Message, null); }
            if (bloqueo == null) return new ResultadoRestauracion { Ok = false, Detalle = MensajeOcupado };
            using (bloqueo)
            {
                RespaldoInfo previo = null;
                try
                {
                    Reportar(progreso, "Verificando la copia a restaurar");
                    var v = await VerificarRutaAsync(cfg, id, ruta, token).ConfigureAwait(false);
                    if (!v.Ok) return FallaRestaurar(id, "La copia no pasó la verificación y no se restauró. " + v.Detalle, null);

                    Reportar(progreso, "Creando una copia de seguridad previa");
                    var pre = await CrearInternoAsync(TipoRespaldo.PreRestauracion, "Automática antes de restaurar " + id, progreso, token, cfg).ConfigureAwait(false);
                    if (!pre.Ok) return FallaRestaurar(id, "No se restauró nada: no se pudo crear la copia previa. " + pre.Detalle, null);
                    previo = pre.Respaldo;

                    Reportar(progreso, "Restaurando la base de datos");
                    bd.LiberarConexiones();
                    var r = await motor.RestaurarAsync(ruta, progreso, token).ConfigureAwait(false);
                    bd.LiberarConexiones();
                    if (!r.Ok)
                        return FallaRestaurar(id, "La restauración falló. Como se hace en una única transacción, la base quedó como estaba. " + r.Detalle, previo);
                    // Después de restaurar: antes lo habría pisado la propia restauración.
                    bitacora.Evento(AccionRestauracion, "Base restaurada desde " + id + " (" + Tamano(almacen.TamanoDe(ruta)) + "). Copia previa: " + previo.NombreArchivo, Entidad);
                    return new ResultadoRestauracion
                    {
                        Ok = true, RespaldoPrevio = previo,
                        Detalle = "Base restaurada desde " + id + ". Antes se creó la copia " + previo.NombreArchivo + " por si hay que volver atrás."
                    };
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { return FallaRestaurar(id, "No se pudo restaurar: " + ex.Message, previo); }
            }
        }

        private ResultadoRestauracion FallaRestaurar(string id, string detalle, RespaldoInfo previo)
        {
            bitacora.Evento(AccionError, "No se pudo restaurar la copia " + id + ": " + detalle, Entidad);
            return new ResultadoRestauracion { Ok = false, Detalle = detalle, RespaldoPrevio = previo };
        }

        // ------------------------------------------------------------------ Importar / eliminar

        public async Task<ResultadoRespaldo> ImportarAsync(string ruta, CancellationToken token)
        {
            sesion.Require(Role.Admin);
            ConfiguracionRespaldo cfg;
            try { cfg = configuracion.Cargar(); }
            catch (Exception ex) { return FallaCrear(TipoRespaldo.Importado, ex.Message); }
            if (string.IsNullOrWhiteSpace(ruta) || !almacen.ExisteArchivo(ruta))
                return new ResultadoRespaldo { Ok = false, Detalle = "El archivo a importar no existe." };
            IDisposable bloqueo;
            try { bloqueo = almacen.Bloquear(cfg.Carpeta); }
            catch (Exception ex) { return FallaCrear(TipoRespaldo.Importado, "No se pudo usar la carpeta de copias: " + ex.Message); }
            if (bloqueo == null) return new ResultadoRespaldo { Ok = false, Detalle = MensajeOcupado };
            string temporal = null;
            using (bloqueo)
            {
                try
                {
                    var herramientas = await motor.DetectarAsync(token).ConfigureAwait(false);
                    if (!herramientas.Disponibles) return FallaCrear(TipoRespaldo.Importado, herramientas.Mensaje);
                    almacen.AsegurarCarpeta(cfg.Carpeta);
                    temporal = almacen.NuevaRutaTemporal(cfg.Carpeta);
                    almacen.Copiar(ruta, temporal, token);
                    var r = await motor.ListarAsync(temporal, token).ConfigureAwait(false);
                    if (!r.Ok || r.Objetos <= 0)
                        return FallaCrear(TipoRespaldo.Importado, "El archivo no es una copia válida de PostgreSQL (formato custom de pg_dump). " + (r.Detalle ?? string.Empty));
                    var tamano = almacen.TamanoDe(temporal);
                    var sha = almacen.CalcularSha256(temporal, token);
                    var fecha = ahora();
                    var nombre = almacen.Publicar(temporal, cfg.Carpeta, fecha.ToLocalTime().DateTime, TipoRespaldo.Importado);
                    temporal = null;
                    var ficha = new FichaRespaldo
                    {
                        FechaUtc = fecha.ToUniversalTime(), Tipo = TipoRespaldo.Importado, Usuario = sesion.Current?.Email, VersionApp = versionApp,
                        TamanoBytes = tamano, Sha256 = sha, Nota = "Importada desde " + Path.GetFileName(ruta)
                    };
                    try { almacen.EscribirFicha(cfg.Carpeta, nombre, ficha); }
                    catch { almacen.BorrarSiExiste(almacen.RutaDe(cfg.Carpeta, nombre)); throw; }
                    bitacora.Evento(AccionRespaldo, "Copia de seguridad " + TipoRespaldo.Importado + ": " + nombre + " (" + Tamano(tamano) + ")", Entidad);
                    return new ResultadoRespaldo
                    {
                        Ok = true, Detalle = "Copia importada: " + nombre + " (" + Tamano(tamano) + ").",
                        Respaldo = Armar(new ArchivoRespaldo { Nombre = nombre, TamanoBytes = tamano, FechaModificacion = fecha, Ficha = ficha })
                    };
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { return FallaCrear(TipoRespaldo.Importado, ex.Message); }
                finally { if (temporal != null) almacen.BorrarSiExiste(temporal); }
            }
        }

        public Task<ResultadoOperacion> EliminarAsync(string id, CancellationToken token)
        {
            sesion.Require(Role.Admin);
            ConfiguracionRespaldo cfg;
            try { cfg = configuracion.Cargar(); }
            catch (Exception ex) { return Task.FromResult(new ResultadoOperacion { Ok = false, Detalle = "No se pudo leer la configuración: " + ex.Message }); }
            var ruta = almacen.RutaDe(cfg.Carpeta, id);
            if (ruta == null) return Task.FromResult(new ResultadoOperacion { Ok = false, Detalle = "La copia no existe en la carpeta de copias." });
            IDisposable bloqueo;
            try { bloqueo = almacen.Bloquear(cfg.Carpeta); }
            catch (Exception ex) { return Task.FromResult(new ResultadoOperacion { Ok = false, Detalle = "No se pudo usar la carpeta de copias: " + ex.Message }); }
            if (bloqueo == null) return Task.FromResult(new ResultadoOperacion { Ok = false, Detalle = MensajeOcupado });
            using (bloqueo)
            {
                try
                {
                    var tamano = almacen.TamanoDe(ruta);
                    almacen.Eliminar(cfg.Carpeta, id);
                    bitacora.Evento(AccionRespaldo, "Copia de seguridad eliminada: " + id + " (" + Tamano(tamano) + ")", Entidad);
                    return Task.FromResult(new ResultadoOperacion { Ok = true, Detalle = "Copia eliminada: " + id + "." });
                }
                catch (Exception ex)
                {
                    bitacora.Evento(AccionError, "No se pudo eliminar la copia " + id + ": " + ex.Message, Entidad);
                    return Task.FromResult(new ResultadoOperacion { Ok = false, Detalle = "No se pudo eliminar la copia: " + ex.Message });
                }
            }
        }

        // ------------------------------------------------------------------ Configuración

        public Task<ResultadoOperacion> GuardarConfiguracionAsync(ConfiguracionRespaldo nueva, CancellationToken token)
        {
            sesion.Require(Role.Admin);
            return Task.FromResult(Guardar(nueva));
        }

        private ResultadoOperacion Guardar(ConfiguracionRespaldo nueva)
        {
            if (nueva == null) return Rechazo("Falta la configuración.");
            string carpeta;
            try
            {
                if (string.IsNullOrWhiteSpace(nueva.Carpeta)) return Rechazo("Indicá la carpeta de copias.");
                if (!Path.IsPathRooted(nueva.Carpeta) || (Path.GetPathRoot(nueva.Carpeta) ?? string.Empty).Length <= 2)
                    return Rechazo("La carpeta de copias debe ser una ruta absoluta.");
                carpeta = Path.GetFullPath(nueva.Carpeta.Trim());
            }
            catch (Exception) { return Rechazo("La carpeta de copias no es una ruta válida."); }
            if (raizPermitida != null && !EstaDentro(raizPermitida, carpeta))
                return Rechazo("En modo demo las copias deben quedar dentro de la carpeta de datos demo.");
            if (nueva.HorasEntreAutomaticos < 1 || nueva.HorasEntreAutomaticos > ConfiguracionRespaldo.HorasMaximo)
                return Rechazo("Las horas entre copias automáticas deben estar entre 1 y " + ConfiguracionRespaldo.HorasMaximo + ".");
            if (nueva.Conservar < 1 || nueva.Conservar > ConfiguracionRespaldo.ConservarMaximo)
                return Rechazo("La cantidad de copias automáticas a conservar debe estar entre 1 y " + ConfiguracionRespaldo.ConservarMaximo + ".");
            try { almacen.ComprobarEscritura(carpeta); }
            catch (Exception ex) { return Rechazo("No se puede escribir en la carpeta de copias: " + ex.Message); }
            try
            {
                var cfg = nueva.Clonar(); cfg.Carpeta = carpeta;
                configuracion.Guardar(cfg);
            }
            catch (Exception ex) { return Rechazo("No se pudo guardar la configuración: " + ex.Message); }
            return new ResultadoOperacion { Ok = true, Detalle = "Configuración guardada." };
        }

        private static ResultadoOperacion Rechazo(string detalle) => new ResultadoOperacion { Ok = false, Detalle = detalle };

        internal static bool EstaDentro(string raiz, string ruta)
        {
            var r = Path.GetFullPath(raiz).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            var p = Path.GetFullPath(ruta).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            return p.StartsWith(r, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------ Automático

        /// <summary>
        /// Crea una copia Automatica si está activado y la última copia (de cualquier tipo) es más vieja que las horas
        /// configuradas o no hay ninguna. No lanza nunca y no exige Admin (sí sesión iniciada).
        /// </summary>
        public async Task<ResultadoAutomatico> EjecutarSiCorrespondeAsync(CancellationToken token)
        {
            try
            {
                if (!sesion.IsAuthenticated) return NoEjecutado("No hay una sesión iniciada.");
                var cfg = configuracion.Cargar();
                if (!cfg.AutomaticoActivo) return NoEjecutado("Las copias automáticas están desactivadas.");
                var motivo = MotivoNoCorresponde(cfg);
                if (motivo != null) return NoEjecutado(motivo);
                using (var bloqueo = almacen.Bloquear(cfg.Carpeta))
                {
                    if (bloqueo == null) return NoEjecutado(MensajeOcupado);
                    // Otra instancia pudo haber creado la copia mientras esperábamos.
                    motivo = MotivoNoCorresponde(cfg);
                    if (motivo != null) return NoEjecutado(motivo);
                    var r = await CrearInternoAsync(TipoRespaldo.Automatico, null, null, token, cfg).ConfigureAwait(false);
                    return new ResultadoAutomatico { Ejecutado = true, Ok = r.Ok, Mensaje = r.Detalle, Respaldo = r.Respaldo };
                }
            }
            catch (OperationCanceledException) { return NoEjecutado("La copia automática se canceló."); }
            catch (Exception ex) { return new ResultadoAutomatico { Ejecutado = false, Ok = false, Mensaje = "No se pudo ejecutar la copia automática: " + ex.Message }; }
        }

        private string MotivoNoCorresponde(ConfiguracionRespaldo cfg)
        {
            var lista = ListarInfos(cfg.Carpeta);
            if (lista.Count == 0) return null;
            var ultima = lista[0].Fecha;
            var limite = TimeSpan.FromHours(Math.Max(1, cfg.HorasEntreAutomaticos));
            if (ahora().ToLocalTime().DateTime - ultima < limite)
                return "Todavía no corresponde: la última copia es de " + ultima.ToString("g", CultureInfo.CurrentCulture) + ".";
            return null;
        }

        private static ResultadoAutomatico NoEjecutado(string mensaje) => new ResultadoAutomatico { Ejecutado = false, Ok = true, Mensaje = mensaje };

        // ------------------------------------------------------------------ Auxiliares

        private List<RespaldoInfo> ListarInfos(string carpeta)
        {
            try { return almacen.Listar(carpeta).Select(Armar).OrderByDescending(r => r.Fecha).ThenByDescending(r => r.NombreArchivo, StringComparer.Ordinal).ToList(); }
            catch { return new List<RespaldoInfo>(); }
        }

        private static RespaldoInfo Armar(ArchivoRespaldo a)
        {
            var f = a.Ficha;
            return new RespaldoInfo
            {
                Id = a.Nombre, NombreArchivo = a.Nombre, TamanoBytes = a.TamanoBytes, TieneFicha = f != null,
                Fecha = (f != null ? f.FechaUtc : a.FechaModificacion).ToLocalTime().DateTime,
                Tipo = f != null ? f.Tipo : (NombresRespaldo.TipoDesdeNombre(a.Nombre) ?? TipoRespaldo.Importado),
                Usuario = f?.Usuario, VersionApp = f?.VersionApp, VersionServidor = f?.VersionServidor, Sha256 = f?.Sha256, Nota = f?.Nota,
                ResumenFilas = f?.ResumenFilas ?? new Dictionary<string, long>()
            };
        }

        private static void Reportar(IProgress<ProgresoRespaldo> progreso, string etapa, int? porcentaje = null)
        { try { progreso?.Report(new ProgresoRespaldo(etapa, porcentaje)); } catch { } }

        public static string Tamano(long bytes)
        {
            string[] u = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes; var i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return (i == 0 ? v.ToString("0", CultureInfo.CurrentCulture) : v.ToString("0.0", CultureInfo.CurrentCulture)) + " " + u[i];
        }
    }
}
