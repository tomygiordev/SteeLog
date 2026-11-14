using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Respaldos;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class RespaldosVistaTests
    {
        private static void Sta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); accion(); } catch (Exception ex) { error = ex; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(60)));
            Assert.Null(error);
        }

        private static void Esperar(Func<bool> condicion, int segundos = 10)
        {
            var reloj = Stopwatch.StartNew();
            while (!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(segundos)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }

        private sealed class Ejecutor : IUseCaseExecutor
        {
            public EstadoRespaldos Estado = new EstadoRespaldos { HerramientasDisponibles = true, BaseConectada = true, Configuracion = new ConfiguracionRespaldo { Carpeta = @"C:\copias" } };
            public ResultadoRestauracion Restauracion = new ResultadoRestauracion { Ok = true };
            public readonly List<string> Llamadas = new List<string>();
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                Llamadas.Add(typeof(TResult).Name);
                if (typeof(TResult) == typeof(EstadoRespaldos)) return Task.FromResult((TResult)(object)Estado);
                if (typeof(TResult) == typeof(ResultadoRestauracion)) return Task.FromResult((TResult)(object)Restauracion);
                if (typeof(TResult) == typeof(ResultadoVerificacion)) return Task.FromResult((TResult)(object)new ResultadoVerificacion { Ok = true, Objetos = 42 });
                throw new InvalidOperationException("Operación no esperada: " + typeof(TResult).Name);
            }
            public int Cantidad(string tipo) { lock (Llamadas) return Llamadas.Count(x => x == tipo); }
        }

        private static RespaldoInfo Copia(TipoRespaldo tipo, int horasAtras, string nota = null)
        {
            var fecha = DateTime.Now.AddHours(-horasAtras);
            return new RespaldoInfo
            {
                Id = "steelog_" + tipo + "_" + horasAtras + ".dump", NombreArchivo = "steelog_" + tipo + "_" + horasAtras + ".dump", Fecha = fecha, Tipo = tipo,
                TamanoBytes = 5 * 1024 * 1024 + horasAtras * 1000, Usuario = "admin", VersionApp = "1.0.0", VersionServidor = "17.2", Nota = nota, TieneFicha = true,
                Sha256 = new string('a', 64),
                ResumenFilas = new Dictionary<string, long> { { "materiales", 120 }, { "movimientos", 3400 }, { "obras", 12 }, { "herramientas", 8 } }
            };
        }

        private static Form CrearVista(Ejecutor ejecutor) => new SteeLog.Vistas.Vista_Respaldos(new ServiciosVistaDePrueba(ejecutor));
        private static T Hallar<T>(Control raiz, string nombre) where T : Control => (T)raiz.Controls.Find(nombre, true).Single();
        private static DataGridView Grilla(Form f) => Hallar<DataGridView>(f, "dgvRespaldos");

        [Fact]
        public void SinCopiasMuestraElEstadoVacio()
        {
            Sta(() =>
            {
                var ejecutor = new Ejecutor();
                using (var vista = CrearVista(ejecutor))
                {
                    vista.Show();
                    Esperar(() => Hallar<Panel>(vista, "panelVacio").Visible);
                    Assert.Equal(0, Grilla(vista).Rows.Count);
                    Assert.True(Hallar<Control>(vista, "btnCrearRespaldo").Enabled);
                    Assert.False(Hallar<Control>(vista, "btnVerificar").Enabled);
                }
            });
        }

        [Fact]
        public void ConCopiasLlenaLaGrillaYHabilitaLasAccionesAlSeleccionar()
        {
            Sta(() =>
            {
                var ejecutor = new Ejecutor();
                ejecutor.Estado.Respaldos = new List<RespaldoInfo>
                {
                    Copia(TipoRespaldo.Manual, 1, "antes del cierre"), Copia(TipoRespaldo.Automatico, 30), Copia(TipoRespaldo.PreRestauracion, 80), Copia(TipoRespaldo.Importado, 400)
                };
                ejecutor.Estado.Ultimo = ejecutor.Estado.Respaldos[0];
                using (var vista = CrearVista(ejecutor))
                {
                    vista.Show();
                    Esperar(() => Grilla(vista).Rows.Count == 4);
                    Assert.False(Hallar<Panel>(vista, "panelVacio").Visible);
                    Assert.False(Hallar<Control>(vista, "btnRestaurar").Enabled);
                    Grilla(vista).Rows[1].Selected = true;
                    Assert.True(Hallar<Control>(vista, "btnRestaurar").Enabled);
                    Assert.True(Hallar<Control>(vista, "btnVerificar").Enabled);
                    Assert.True(Hallar<Control>(vista, "btnEliminarRespaldo").Enabled);
                }
            });
        }

        [Fact]
        public void SinHerramientasNoSePuedeCrearNiRestaurarPeroLaVistaSigueViva()
        {
            Sta(() =>
            {
                var ejecutor = new Ejecutor();
                ejecutor.Estado.HerramientasDisponibles = false;
                ejecutor.Estado.MensajeHerramientas = "No se encontraron pg_dump y pg_restore.";
                ejecutor.Estado.Respaldos = new List<RespaldoInfo> { Copia(TipoRespaldo.Manual, 2) };
                using (var vista = CrearVista(ejecutor))
                {
                    vista.Show();
                    Esperar(() => Grilla(vista).Rows.Count == 1);
                    Grilla(vista).Rows[0].Selected = true;
                    Assert.False(Hallar<Control>(vista, "btnCrearRespaldo").Enabled);
                    Assert.False(Hallar<Control>(vista, "btnRestaurar").Enabled);
                    Assert.False(Hallar<Control>(vista, "btnImportarRespaldo").Enabled);
                    Assert.True(Hallar<Control>(vista, "btnEliminarRespaldo").Enabled);
                }
            });
        }

        [Fact]
        public void UnFalloAlConsultarElEstadoSeMuestraEnLaVistaSinLanzar()
        {
            Sta(() =>
            {
                using (var vista = new SteeLog.Vistas.Vista_Respaldos(new ServiciosVistaDePrueba(new EjecutorQueFalla())))
                {
                    vista.Show();
                    Esperar(() => Hallar<Panel>(vista, "panelAviso").Visible);
                    Assert.Contains("boom", Hallar<Label>(vista, "lblAviso").Text);
                }
            });
        }

        private sealed class EjecutorQueFalla : IUseCaseExecutor
        {
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
                => throw new InvalidOperationException("boom");
        }

        // ---- Restaurar ----

        private static Tuple<Ejecutor, SteeLog.Vistas.Vista_Respaldos> VistaConUnaCopia()
        {
            var ejecutor = new Ejecutor();
            ejecutor.Estado.Respaldos = new List<RespaldoInfo> { Copia(TipoRespaldo.Manual, 3) };
            var vista = new SteeLog.Vistas.Vista_Respaldos(new ServiciosVistaDePrueba(ejecutor));
            vista.CerrarSesion = () => { };
            vista.Show();
            Esperar(() => ((DataGridView)vista.Controls.Find("dgvRespaldos", true).Single()).Rows.Count == 1);
            return Tuple.Create(ejecutor, vista);
        }

        [Fact]
        public void RestaurarNoLlamaAlPresenterSiNoSeEscribeRestaurar()
        {
            Sta(() =>
            {
                var par = VistaConUnaCopia();
                var ejecutor = par.Item1; var vista = par.Item2;
                using (vista)
                {
                    bool cerro = false;
                    vista.CerrarSesion = () => cerro = true;
                    foreach (var escrito in new string[] { null, "", "restaurar", "RESTAURA", "si" })
                    {
                        vista.PedirTexto = (m, t) => escrito;
                        var tarea = vista.SolicitarRestauracionAsync("steelog_Manual_3.dump");
                        Esperar(() => tarea.IsCompleted);
                    }
                    Assert.Equal(0, ejecutor.Cantidad("ResultadoRestauracion"));
                    Assert.False(cerro);
                }
            });
        }

        [Fact]
        public void RestaurarConLaPalabraCorrectaRestauraAvisaYCierraLaSesion()
        {
            Sta(() =>
            {
                var par = VistaConUnaCopia();
                var ejecutor = par.Item1; var vista = par.Item2;
                ejecutor.Restauracion = new ResultadoRestauracion { Ok = true, RespaldoPrevio = Copia(TipoRespaldo.PreRestauracion, 0) };
                using (vista)
                {
                    string mensajePedido = null, aviso = null; bool cerro = false;
                    vista.PedirTexto = (m, t) => { mensajePedido = m; return " RESTAURAR "; };
                    vista.Informar = (t, m, error) => { aviso = m; Assert.False(error); };
                    vista.CerrarSesion = () => cerro = true;
                    var tarea = vista.SolicitarRestauracionAsync("steelog_Manual_3.dump");
                    Esperar(() => tarea.IsCompleted);
                    Assert.Equal(1, ejecutor.Cantidad("ResultadoRestauracion"));
                    Assert.Contains("RESTAURAR", mensajePedido);
                    Assert.Contains("steelog_PreRestauracion_0.dump", aviso);
                    Assert.True(cerro);
                }
            });
        }

        [Fact]
        public void UnaRestauracionFallidaMuestraElDetalleYNoCierraLaSesion()
        {
            Sta(() =>
            {
                var par = VistaConUnaCopia();
                var ejecutor = par.Item1; var vista = par.Item2;
                ejecutor.Restauracion = new ResultadoRestauracion { Ok = false, Detalle = "pg_restore devolvió un error." };
                using (vista)
                {
                    string aviso = null; bool esError = false, cerro = false;
                    vista.PedirTexto = (m, t) => "RESTAURAR";
                    vista.Informar = (t, m, error) => { aviso = m; esError = error; };
                    vista.CerrarSesion = () => cerro = true;
                    var tarea = vista.SolicitarRestauracionAsync("steelog_Manual_3.dump");
                    Esperar(() => tarea.IsCompleted);
                    Assert.True(esError);
                    Assert.Contains("pg_restore devolvió un error.", aviso);
                    Assert.Contains("quedó como estaba", aviso);
                    Assert.False(cerro);
                }
            });
        }

        [Fact]
        public void LaConfirmacionExigeLaPalabraExacta()
        {
            Assert.True(SteeLog.Vistas.Vista_Respaldos.ConfirmacionValida("RESTAURAR"));
            Assert.True(SteeLog.Vistas.Vista_Respaldos.ConfirmacionValida("  RESTAURAR\r\n"));
            Assert.False(SteeLog.Vistas.Vista_Respaldos.ConfirmacionValida("restaurar"));
            Assert.False(SteeLog.Vistas.Vista_Respaldos.ConfirmacionValida("RESTAURAR ya"));
            Assert.False(SteeLog.Vistas.Vista_Respaldos.ConfirmacionValida(null));
        }

        [Fact]
        public void FormatosEnLenguajeNaturalYEsAr()
        {
            var ahora = new DateTime(2026, 11, 14, 12, 0, 0);
            Assert.Equal("hace instantes", SteeLog.Vistas.Vista_Respaldos.TextoHace(ahora.AddSeconds(-20), ahora));
            Assert.Equal("hace 5 min", SteeLog.Vistas.Vista_Respaldos.TextoHace(ahora.AddMinutes(-5), ahora));
            Assert.Equal("hace 1 hora", SteeLog.Vistas.Vista_Respaldos.TextoHace(ahora.AddMinutes(-70), ahora));
            Assert.Equal("hace 3 horas", SteeLog.Vistas.Vista_Respaldos.TextoHace(ahora.AddHours(-3), ahora));
            Assert.Equal("ayer", SteeLog.Vistas.Vista_Respaldos.TextoHace(ahora.AddHours(-30), ahora));
            Assert.Equal("hace 5 días", SteeLog.Vistas.Vista_Respaldos.TextoHace(ahora.AddDays(-5), ahora));
            Assert.Equal("1,50 MB", SteeLog.Vistas.Vista_Respaldos.TextoTamano(1572864));
            Assert.Equal("512 KB", SteeLog.Vistas.Vista_Respaldos.TextoTamano(512 * 1024));
        }

        // ---- Menú Sistema ----

        private static Control Buscar(string email, string nombre)
        {
            var servicios = new ServiciosVistaDePrueba();
            servicios.Sesion.SetSession(email);
            using (var vista = new SteeLog.VistaGeneral(servicios))
            {
                var c = vista.Controls.Find(nombre, true).SingleOrDefault();
                return c == null ? null : new Control { Name = c.Parent == null ? string.Empty : c.Parent.Name };
            }
        }

        [Fact]
        public void LaSeccionSistemaEsSoloParaAdmin()
        {
            Sta(() =>
            {
                Assert.NotNull(Buscar("admin@tesis.local", "btnSistema"));
                Assert.Equal("panelSistema", Buscar("admin@tesis.local", "btnRespaldos").Name);
                Assert.Equal("panelSistema", Buscar("admin@tesis.local", "btnBitacora").Name);
                Assert.Null(Buscar("galpon@tesis.local", "btnSistema"));
                Assert.Null(Buscar("galpon@tesis.local", "panelSistema"));
                Assert.Null(Buscar("galpon@tesis.local", "btnRespaldos"));
            });
        }

        [Fact]
        public void SistemaQuedaAlFondoDelMenuJustoArribaDeSalir()
        {
            Sta(() =>
            {
                var servicios = new ServiciosVistaDePrueba();
                servicios.Sesion.SetSession("admin@tesis.local");
                using (var vista = new SteeLog.VistaGeneral(servicios))
                {
                    var menu = vista.Controls.Find("panelSideMenu", true).Single();
                    var boton = menu.Controls.Find("btnSistema", false).Single();
                    var panel = menu.Controls.Find("panelSistema", false).Single();
                    var salir = menu.Controls.Find("btnSalir", false).Single();
                    Assert.Equal(DockStyle.Bottom, boton.Dock);
                    Assert.Equal(DockStyle.Bottom, panel.Dock);
                    var i = menu.Controls.GetChildIndex(boton);
                    Assert.Equal(i + 1, menu.Controls.GetChildIndex(panel));
                    Assert.Equal(i + 2, menu.Controls.GetChildIndex(salir));
                }
            });
        }

        // ---- Respaldo automático ----

        private static readonly MethodInfo MetodoAviso = typeof(SteeLog.VistaGeneral).GetMethod("AvisoRespaldoAutomatico", BindingFlags.Static | BindingFlags.NonPublic);

        private static KeyValuePair<bool, string>? Aviso(ResultadoAutomatico r, bool admin) => (KeyValuePair<bool, string>?)MetodoAviso.Invoke(null, new object[] { r, admin });

        [Fact]
        public void ElAvisoDelAutomaticoSoloAparecePorExitoOFalloParaAdmin()
        {
            Assert.Null(Aviso(new ResultadoAutomatico { Ejecutado = false }, true));
            Assert.Null(Aviso(null, true));
            var ok = Aviso(new ResultadoAutomatico { Ejecutado = true, Ok = true }, false);
            Assert.True(ok.Value.Key);
            Assert.Contains("automática creada", ok.Value.Value);
            Assert.Null(Aviso(new ResultadoAutomatico { Ejecutado = true, Ok = false, Mensaje = "sin espacio" }, false));
            var error = Aviso(new ResultadoAutomatico { Ejecutado = true, Ok = false, Mensaje = "sin espacio" }, true);
            Assert.False(error.Value.Key);
            Assert.Contains("sin espacio", error.Value.Value);
        }

        [Fact]
        public void ElAutomaticoSeDisparaAlMostrarLaVentanaYNoLanzaAunqueFalle()
        {
            Sta(() =>
            {
                int llamadas = 0;
                Func<CancellationToken, Task<ResultadoAutomatico>> falla = t => { Interlocked.Increment(ref llamadas); throw new InvalidOperationException("el presenter falló"); };
                var vista = (Form)Activator.CreateInstance(typeof(SteeLog.VistaGeneral), BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] { new ServiciosVistaDePrueba(), null, new Func<CancellationToken, Task>(t => Task.Delay(Timeout.Infinite, t)), null, falla }, null);
                try
                {
                    vista.Show();
                    Esperar(() => Volatile.Read(ref llamadas) == 1, 10);
                    for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(10); }
                    Assert.Equal(1, llamadas);
                }
                finally { vista.Dispose(); }
            });
        }
    }
}
