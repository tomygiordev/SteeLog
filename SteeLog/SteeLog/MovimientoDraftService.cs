using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using SteeLog.Modelos;
using SteeLog.Core.Application.Borradores;

namespace SteeLog
{
    /// <summary>
    /// Servicio estático que gestiona la persistencia del borrador de movimiento en disco.
    /// No usa hilos propios: siempre se llama desde el hilo UI via un Timer.
    /// Todos los errores son silenciosos para no interferir con el flujo del usuario.
    /// </summary>
    internal static class MovimientoDraftService
    {
        private static BorradorMovimiento Servicio => SteeLog.CompositionRoot.LocalServices.Current.BorradorMovimiento;

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

        public static void EliminarSiCoincide(string version)
        {
            Servicio.EliminarSiCoincide(version);
        }

        public static MovimientoDraft CrearDesde(
            DateTime fecha, string nroRemito, int idTipoMov, string nombreTipoMov,
            int idOrigen, string tipoOrigen, int idDestino, string tipoDestino,
            int idObra, decimal costoFlete, int? idFlete, decimal precioTotal, string observaciones,
            List<MovimientoMaterial> materiales)
        {
            var draft = new MovimientoDraft
            {
                Fecha = fecha,
                NroRemito = nroRemito,
                IdTipoMovimiento = idTipoMov,
                NombreTipoMovimiento = nombreTipoMov,
                IdOrigen = idOrigen,
                TipoOrigen = tipoOrigen,
                IdDestino = idDestino,
                TipoDestino = tipoDestino,
                IdObra = idObra,
                CostoFlete = costoFlete,
                IdFlete = idFlete,
                PrecioTotal = precioTotal,
                Observaciones = observaciones
            };

            if (materiales != null)
            {
                foreach (var m in materiales)
                {
                    draft.Materiales.Add(new MovimientoMaterialDraft
                    {
                        IdMaterial = m.id_material,
                        NombreMaterial = m.NombreMaterial,
                        NombreMaterialEventual = m.NombreMaterialEventual,
                        Cantidad = m.cantidad,
                        EsRecorte = m.EsRecorte,
                        IdRecorte = m.IdRecorte,
                        AnchoRecorte = m.AnchoRecorte,
                        AlturaRecorte = m.AlturaRecorte,
                        MedidaRecorte = m.MedidaRecorte,
                        EsInsumo = m.EsInsumo,
                        IdInsumo = m.IdInsumo,
                        EsCompraEventual = m.EsCompraEventual,
                        PrecioUnitario = m.precio_unitario
                    });
                }
            }
            return draft;
        }
    }
}
