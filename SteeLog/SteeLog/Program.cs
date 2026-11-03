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
                        if (login.ShowDialog() != DialogResult.OK) return;
                }
                Application.Run(new VistaGeneral());
            }
            finally { services.Dispose(); }
        }
    }
}
