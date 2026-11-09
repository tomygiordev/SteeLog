namespace SteeLog.Servicios
{
    [System.Obsolete("Fase DI: recibir IServiciosVista por constructor")]
    public static class CredentialStore
    {
        private static CredencialesVista Servicio => SteeLog.CompositionRoot.ServiciosCompatibles.Obtener().Credenciales;
        public static void Save(string email, string password)
        {
            try { Servicio.Save(email, password); }
            catch { }
        }
        public static (string email, string password) Load()
        {
            try { return Servicio.Load(); }
            catch { return (null, null); }
        }
        public static void Clear()
        {
            try { Servicio.Clear(); }
            catch { }
        }
    }
}
