using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class BitacoraVistaTests
    {
        private const BindingFlags Todos = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static void Sta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); accion(); } catch (Exception ex) { error = ex; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(60)));
            Assert.Null(error);
        }

        private static void Esperar(Func<bool> condicion)
        {
            var reloj = Stopwatch.StartNew();
            while (!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(10)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }

        // Dialogo es internal: se accede por reflexión, como en DialogoTests.
        private static Type TipoDialogo => typeof(SteeLog.VistaGeneral).Assembly.GetType("SteeLog.UI.Dialogo", true);
        private static void AsignarEnganche(Action<string, string> accion) => TipoDialogo.GetField("AlMostrarError", Todos).SetValue(null, accion);
        private static void Llamar(string metodo, params object[] args) => TipoDialogo.GetMethod(metodo, Todos).Invoke(null, args);

        private sealed class Ejecutor : IUseCaseExecutor
        {
            public BitacoraResultado Resultado = new BitacoraResultado();
            public BitacoraOpciones Opciones = new BitacoraOpciones();
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(BitacoraResultado)) return Task.FromResult((TResult)(object)Resultado);
                if (typeof(TResult) == typeof(BitacoraOpciones)) return Task.FromResult((TResult)(object)Opciones);
                throw new InvalidOperationException("Operación no esperada");
            }
        }

        // ---- Enganche de errores de Dialogo ----

        [Fact]
        public void NotificarErrorInvocaElEngancheConLaDescripcion()
        {
            Sta(() =>
            {
                string descripcion = null; int llamadas = 0;
                AsignarEnganche((d, p) => { descripcion = d; llamadas++; });
                try { Llamar("NotificarError", "Se rompió algo", null); }
                finally { AsignarEnganche(null); }
                Assert.Equal(1, llamadas);
                Assert.Equal("Se rompió algo", descripcion);
            });
        }

        [Fact]
        public void NotificarErrorNoRompeSiElEngancheLanza()
        {
            Sta(() =>
            {
                AsignarEnganche((d, p) => { throw new InvalidOperationException("falla la bitácora"); });
                try { Llamar("NotificarError", "x", null); }
                finally { AsignarEnganche(null); }
            });
        }

        [Fact]
        public void NotificarErrorSinEngancheNoHaceNada()
        {
            Sta(() => { AsignarEnganche(null); Llamar("NotificarError", "x", null); });
        }

        [Fact]
        public void PantallaActivaUsaElFormularioDelOwner()
        {
            Sta(() =>
            {
                using (var owner = new Form())
                {
                    var pantalla = (string)TipoDialogo.GetMethod("PantallaActiva", Todos).Invoke(null, new object[] { owner });
                    Assert.Equal("Form", pantalla);
                }
            });
        }

        // Formulario propio del hilo del test: así Dialogo no intenta pasar por formularios abiertos por otros tests.
        private static Form CrearOwner()
        {
            var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-20000, -20000) };
            owner.Show();
            return owner;
        }

        // Cierra el diálogo modal apenas aparece, para que el test no quede bloqueado.
        private static void Mostrar(Action accion)
        {
            using (var cierre = new System.Windows.Forms.Timer { Interval = 100 })
            {
                var inicio = Stopwatch.StartNew();
                cierre.Tick += (s, e) =>
                {
                    foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
                        if (f.GetType().Name == "FormDialogo") f.DialogResult = DialogResult.OK;
                    if (inicio.Elapsed > TimeSpan.FromSeconds(20)) cierre.Stop();
                };
                cierre.Start();
                accion();
            }
        }

        [Fact]
        public void DialogoErrorRegistraElErrorYErrorYaRegistradoNo()
        {
            Sta(() =>
            {
                var descripciones = new List<string>();
                AsignarEnganche((d, p) => descripciones.Add(d));
                try
                {
                    using (var owner = CrearOwner()) Mostrar(() => Llamar("Error", "Falló el guardado", null, owner));
                    Assert.Equal(new[] { "Falló el guardado" }, descripciones);
                    using (var owner = CrearOwner()) Mostrar(() => Llamar("ErrorYaRegistrado", "Ya registrado", null, owner));
                    Assert.Single(descripciones);
                }
                finally { AsignarEnganche(null); }
            });
        }

        [Fact]
        public void DialogoErrorMuestraElDialogoAunqueElEngancheLance()
        {
            Sta(() =>
            {
                AsignarEnganche((d, p) => { throw new InvalidOperationException("falla"); });
                bool abierto = false;
                try
                {
                    using (var vigia = new System.Windows.Forms.Timer { Interval = 50 })
                    {
                        vigia.Tick += (s, e) => { if (Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "FormDialogo")) abierto = true; };
                        vigia.Start();
                        using (var owner = CrearOwner()) Mostrar(() => Llamar("Error", "x", null, owner));
                    }
                }
                finally { AsignarEnganche(null); }
                Assert.True(abierto);
            });
        }

        [Fact]
        public void ToastDeErrorRegistraPeroElDeExitoNo()
        {
            Sta(() =>
            {
                var descripciones = new List<string>();
                AsignarEnganche((d, p) => descripciones.Add(d));
                try
                {
                    // El aviso se registra antes de mostrarse; sin escritorio interactivo la ventana puede no crearse y no importa.
                    foreach (var mostrar in new Action[] { () => ToastNotification.ShowError("Algo falló"), () => ToastNotification.ShowSuccess("Todo bien"), () => ToastNotification.ShowWarning("Ojo") })
                    {
                        try { mostrar(); } catch (System.ComponentModel.Win32Exception) { }
                    }
                }
                finally
                {
                    AsignarEnganche(null);
                    foreach (Form f in Application.OpenForms.Cast<Form>().ToList()) f.Dispose();
                }
                Assert.Equal(new[] { "Algo falló" }, descripciones);
            });
        }

        // ---- Pantalla ----

        private static Form CrearVista(Ejecutor ejecutor) => new SteeLog.Vistas.Vista_Bitacora(new ServiciosVistaDePrueba(ejecutor));
        private static Label Estado(Form f) => (Label)f.Controls.Find("lblEstadoBitacora", true).Single();
        private static DataGridView Grilla(Form f) => (DataGridView)f.Controls.Find("dgvBitacora", true).Single();

        [Fact]
        public void SinMigracionMuestraElMensajeDeNoInstalada()
        {
            Sta(() =>
            {
                var ejecutor = new Ejecutor { Resultado = new BitacoraResultado { Instalada = false }, Opciones = new BitacoraOpciones { Instalada = false } };
                using (var vista = CrearVista(ejecutor))
                {
                    vista.Show();
                    Esperar(() => Estado(vista).Text == ConsultarBitacora.MensajeNoInstalada);
                    Assert.Equal(0, Grilla(vista).Rows.Count);
                }
            });
        }

        [Fact]
        public void MuestraLosRegistrosYAvisaSiElResultadoEstaTruncado()
        {
            Sta(() =>
            {
                var ejecutor = new Ejecutor
                {
                    Resultado = new BitacoraResultado
                    {
                        Truncado = true,
                        Registros = new List<BitacoraRegistro>
                        {
                            new BitacoraRegistro { Id = 2, Fecha = DateTime.Now, Usuario = "admin", Accion = "INSERT", Entidad = "materiales", EntidadId = "7", Origen = "BD", DatosDespues = "{\"nombre\":\"Chapa\"}" },
                            new BitacoraRegistro { Id = 1, Fecha = DateTime.Now, Usuario = "galpon", Accion = "ERROR", Entidad = "Vista_Kardex", Origen = "APP", Descripcion = "No se pudo guardar" }
                        }
                    }
                };
                using (var vista = CrearVista(ejecutor))
                {
                    vista.Show();
                    Esperar(() => Grilla(vista).Rows.Count == 2);
                    Assert.Contains("Refiná los filtros", Estado(vista).Text);
                    Assert.Equal("No se pudo guardar", Grilla(vista).Rows[1].Cells["descripcion"].Value);
                    Assert.Equal(System.Drawing.Color.FromArgb(78, 38, 38), Grilla(vista).Rows[1].DefaultCellStyle.BackColor);
                }
            });
        }

        [Fact]
        public void SinPermisosMuestraUnAvisoEnLugarDeRomper()
        {
            Sta(() =>
            {
                using (var vista = new SteeLog.Vistas.Vista_Bitacora(new ServiciosVistaDePrueba(new ExecutorQueNiega())))
                {
                    vista.Show();
                    Esperar(() => Estado(vista).Text.Contains("administradores"));
                }
            });
        }

        private sealed class ExecutorQueNiega : IUseCaseExecutor
        {
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
                => throw new UnauthorizedAccessException();
        }

        // ---- Menú ----

        private static bool HayBotonBitacora(string email)
        {
            var servicios = new ServiciosVistaDePrueba();
            servicios.Sesion.SetSession(email);
            using (var vista = new SteeLog.VistaGeneral(servicios))
                return vista.Controls.Find("btnBitacora", true).Any();
        }

        [Fact]
        public void ElBotonBitacoraEsSoloParaAdmin()
        {
            Sta(() =>
            {
                Assert.True(HayBotonBitacora("admin@tesis.local"));
                Assert.False(HayBotonBitacora("galpon@tesis.local"));
            });
        }
    }
}
