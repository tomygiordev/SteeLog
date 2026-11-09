namespace SteeLog.Servicios
{
    public enum UserRole { None = 0, Galpon = 1, Admin = 2 }

    // Puente temporal para formularios que todavía leen el rol legacy.
    [System.Obsolete("Fase DI: recibir IServiciosVista por constructor")]
    public static class SessionManager
    {
        private static SesionVista Servicio => SteeLog.CompositionRoot.ServiciosCompatibles.Obtener().Sesion;
        public static string Email => Servicio.Email;
        public static string Username => Servicio.Username;
        public static UserRole Role => Servicio.Role;
        public static bool IsLoggedIn => Servicio.IsLoggedIn;
        public static bool DebeOcultarCantidadesStockMateriales => Servicio.DebeOcultarCantidadesStockMateriales;
        public static void SetSession(string email) => Servicio.SetSession(email);
        public static void Clear() => Servicio.Clear();
        public static string ObtenerEstadoStockMaterial(decimal stockActual) => Servicio.ObtenerEstadoStockMaterial(stockActual);
    }
}
