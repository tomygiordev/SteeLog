using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using SteeLog.UI;
using SteeLog.CompositionRoot;
using SteeLog.Servicios;

namespace SteeLog
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Los servicios se crean más abajo; el registro resuelve la carpeta de datos recién al primer error.
            LocalServices services = null;
            var registro = new RegistroErrores(() => services.DataDirectory);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                registro.Registrar(e.Exception);
                RegistrarEnBitacora(services, e.Exception);
                try { Dialogo.ErrorYaRegistrado("Ocurrió un error inesperado. Revisá la operación antes de continuar."); }
                catch (Exception errorDialogo) { registro.Registrar(errorDialogo); }
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                registro.Registrar(e.ExceptionObject);
                RegistrarEnBitacora(services, e.ExceptionObject);
            };
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                registro.Registrar(e.Exception);
                e.SetObserved();
            };
            // Cada error mostrado al usuario (diálogo o aviso) queda en la bitácora con la pantalla en uso.
            Dialogo.AlMostrarError = (descripcion, pantalla) => services?.ErroresBitacora?.Error(descripcion, pantalla);
            services = LocalServices.Create();
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var culture = new System.Globalization.CultureInfo("es-AR");
                culture.NumberFormat.CurrencySymbol = "$";
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

                var saved = services.Credenciales.Load();
                bool loggedIn = false;
                if (saved.email != null && saved.password != null)
                {
                    var result = services.Autenticacion.SignInAsync(saved.email, saved.password).GetAwaiter().GetResult();
                    if (result.Success) { services.Sesion.SetSession(result.Email); services.ErroresBitacora.ReintentarPendientes(); loggedIn = true; }
                    else services.Credenciales.Clear();
                }
                if (!loggedIn)
                {
                    using (var login = new SteeLog.Vistas.Vista_Login(services))
                    {
                        ConfigurarPantallaDemo(login, Environment.GetEnvironmentVariable("STEELOG_DEMO_ROOT"), Environment.GetEnvironmentVariable("STEELOG_DEMO_DISPLAY"));
                        if (login.ShowDialog() != DialogResult.OK) return;
                    }
                }
                using (var principal = new VistaGeneral(services))
                {
                    ConfigurarPantallaDemo(principal, Environment.GetEnvironmentVariable("STEELOG_DEMO_ROOT"), Environment.GetEnvironmentVariable("STEELOG_DEMO_DISPLAY"));
                    Application.Run(principal);
                }
            }
            finally { services.Dispose(); }
        }

        /// <summary>Deja el error no controlado en la bitácora. Puede llamarse antes de crear los servicios y nunca lanza.</summary>
        private static void RegistrarEnBitacora(LocalServices services, object error)
        {
            try { services?.ErroresBitacora?.ErrorNoControlado(error, Dialogo.PantallaActiva()); }
            catch { }
        }

        internal static void ConfigurarPantallaDemo(Form ventana, string raizDemo, string nombrePantalla)
        {
            if (string.IsNullOrWhiteSpace(raizDemo) || string.IsNullOrWhiteSpace(nombrePantalla)) return;
            Screen pantalla = null;
            foreach (var candidata in Screen.AllScreens)
                if (string.Equals(candidata.DeviceName, nombrePantalla, StringComparison.OrdinalIgnoreCase)) pantalla = candidata;
            if (pantalla == null) throw new InvalidOperationException("No se encontró el monitor solicitado para la demo.");
            var estado = ventana.WindowState;
            ventana.WindowState = FormWindowState.Normal;
            ventana.StartPosition = FormStartPosition.Manual;
            var area = pantalla.WorkingArea;
            ventana.Location = new System.Drawing.Point(area.Left + Math.Max(0, (area.Width - ventana.Width) / 2), area.Top + Math.Max(0, (area.Height - ventana.Height) / 2));
            ventana.WindowState = estado;
        }
    }
}
