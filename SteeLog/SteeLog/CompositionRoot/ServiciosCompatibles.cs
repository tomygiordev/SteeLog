using SteeLog.Presentation;

namespace SteeLog.CompositionRoot
{
    // Único puente para constructores y fachadas pendientes de migrar.
    internal static class ServiciosCompatibles
    {
        internal static IServiciosVista Obtener() => LocalServices.ObtenerCompatible();
    }
}
