using System;
using System.Collections.Generic;
using SteeLog.Modelos;
using SteeLog.Core.Application.Borradores;

namespace SteeLog
{
    [System.Obsolete("Fase DI: recibir IServiciosVista por constructor")]
    internal static class MovimientoDraftService
    {
        private static SteeLog.Servicios.BorradorMovimientoVista Servicio => SteeLog.CompositionRoot.ServiciosCompatibles.Obtener().Borrador;
        public static void Guardar(MovimientoDraft draft)
        {
            try { Servicio.Guardar(draft); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error al guardar borrador: " + ex.Message); }
        }
        public static MovimientoDraft Leer()
        {
            try { return Servicio.Leer(); }
            catch { Eliminar(); return null; }
        }
        public static void Eliminar()
        {
            try { Servicio.Eliminar(); }
            catch { }
        }
        public static string VersionActual()
        {
            try { return Servicio.VersionActual(); }
            catch { return null; }
        }
        public static void EliminarSiCoincide(string version) => Servicio.EliminarSiCoincide(version);
        public static MovimientoDraft CrearDesde(
            DateTime fecha, string nroRemito, int idTipoMov, string nombreTipoMov,
            int idOrigen, string tipoOrigen, int idDestino, string tipoDestino,
            int idObra, decimal costoFlete, int? idFlete, decimal precioTotal, string observaciones,
            List<MovimientoMaterial> materiales, string resultadoInciertoRemito = null)
            => Servicio.CrearDesde(fecha, nroRemito, idTipoMov, nombreTipoMov, idOrigen, tipoOrigen, idDestino, tipoDestino,
                idObra, costoFlete, idFlete, precioTotal, observaciones, materiales, resultadoInciertoRemito);
    }
}
