using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SteeLog
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Forzar cultura argentina para que el formato de moneda sea el signo $ y no €
            var culture = new System.Globalization.CultureInfo("es-AR");
            culture.NumberFormat.CurrencySymbol = "$"; // Forzar concretamente por si el OS está mal
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            // TEST DE CONEXIÓN EN SEGUNDO PLANO (WARMUP)
            // Se ejecuta de forma asíncrona para no congelar la aparición de la ventana principal
            Task.Run(() =>
            {
                try
                {
                    using (var conexion = new SteeLog.Datos.ConexionDB().GetConnection())
                    {
                        if (conexion.State == System.Data.ConnectionState.Closed) conexion.Open();
                    }
                }
                catch (Exception ex)
                {
                    // Estamos en segundo plano, así que imprimimos en consola
                    Console.WriteLine($"Error al conectar en background: {ex.Message}");
                }
            });

            // Intentar auto-login con credenciales guardadas
            bool autoLogged = false;
            var savedCreds = SteeLog.Servicios.CredentialStore.Load();
            string savedEmail = savedCreds.email;
            string savedPassword = savedCreds.password;
            if (savedEmail != null && savedPassword != null)
            {
                try
                {
                    var result = SteeLog.Servicios.SupabaseAuthService
                        .SignInAsync(savedEmail, savedPassword).GetAwaiter().GetResult();
                    if (result.Success)
                    {
                        SteeLog.Servicios.SessionManager.SetSession(
                            result.Email, result.AccessToken, result.RefreshToken, result.ExpiresIn);
                        autoLogged = true;
                    }
                    else
                    {
                        // Credenciales inválidas (contraseña cambiada, etc.) → limpiar y pedir login
                        if (!result.IsNetworkError)
                        {
                            SteeLog.Servicios.CredentialStore.Clear();
                        }
                    }
                }
                catch { /* Sin internet u otro error → mostrar login igual */ }
            }

            if (!autoLogged)
            {
                var login = new SteeLog.Vistas.Vista_Login();
                if (login.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
            }

            Application.Run(new VistaGeneral());
        }
    }
}
