using System;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Servicios;

namespace SteeLog
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var services = LocalServices.Current;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var culture = new System.Globalization.CultureInfo("es-AR");
                culture.NumberFormat.CurrencySymbol = "$";
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

                var saved = CredentialStore.Load();
                bool loggedIn = false;
                if (saved.email != null && saved.password != null)
                {
                    var result = SupabaseAuthService.SignInAsync(saved.email, saved.password).GetAwaiter().GetResult();
                    if (result.Success) { SessionManager.SetSession(result.Email); loggedIn = true; }
                    else CredentialStore.Clear();
                }
                if (!loggedIn)
                {
                    using (var login = new SteeLog.Vistas.Vista_Login())
                    {
                        ConfigurarPantallaDemo(login, Environment.GetEnvironmentVariable("STEELOG_DEMO_ROOT"), Environment.GetEnvironmentVariable("STEELOG_DEMO_DISPLAY"));
                        if (login.ShowDialog() != DialogResult.OK) return;
                    }
                }
                using (var principal = new VistaGeneral())
                {
                    ConfigurarPantallaDemo(principal, Environment.GetEnvironmentVariable("STEELOG_DEMO_ROOT"), Environment.GetEnvironmentVariable("STEELOG_DEMO_DISPLAY"));
                    Application.Run(principal);
                }
            }
            finally { services.Dispose(); }
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
