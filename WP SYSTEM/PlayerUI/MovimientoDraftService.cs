using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using WP_Inventario.Modelos;

namespace WP_Inventario
{
    [DataContract]
    public class MovimientoMaterialDraft
    {
        [DataMember] public int? IdMaterial { get; set; }
        [DataMember] public string NombreMaterial { get; set; }
        [DataMember] public string NombreMaterialEventual { get; set; }
        [DataMember] public decimal Cantidad { get; set; }
        [DataMember] public bool EsRecorte { get; set; }
        [DataMember] public int? IdRecorte { get; set; }
        [DataMember] public decimal? MedidaRecorte { get; set; }
        [DataMember] public bool EsInsumo { get; set; }
        [DataMember] public int? IdInsumo { get; set; }
        [DataMember] public bool EsCompraEventual { get; set; }
        [DataMember] public decimal PrecioUnitario { get; set; }
    }

    [DataContract]
    public class MovimientoDraft
    {
        [DataMember] public DateTime FechaGuardado { get; set; }
        [DataMember] public int IdTipoMovimiento { get; set; }
        [DataMember] public string NombreTipoMovimiento { get; set; }
        [DataMember] public string NroRemito { get; set; }
        [DataMember] public int IdOrigen { get; set; }
        [DataMember] public string TipoOrigen { get; set; }
        [DataMember] public int IdDestino { get; set; }
        [DataMember] public string TipoDestino { get; set; }
        [DataMember] public int IdObra { get; set; }
        [DataMember] public decimal CostoFlete { get; set; }
        [DataMember] public int? IdFlete { get; set; }
        [DataMember] public string Observaciones { get; set; }
        [DataMember] public DateTime Fecha { get; set; }
        [DataMember] public decimal PrecioTotal { get; set; }
        [DataMember] public List<MovimientoMaterialDraft> Materiales { get; set; }

        public MovimientoDraft()
        {
            Materiales = new List<MovimientoMaterialDraft>();
        }
    }

    /// <summary>
    /// Servicio estático que gestiona la persistencia del borrador de movimiento en disco.
    /// No usa hilos propios: siempre se llama desde el hilo UI via un Timer.
    /// Todos los errores son silenciosos para no interferir con el flujo del usuario.
    /// </summary>
    internal static class MovimientoDraftService
    {
        private static readonly string _carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SteeLog");

        private static readonly string _ruta = Path.Combine(_carpeta, "movimiento_draft.json");
        private static readonly TimeSpan _maxEdad = TimeSpan.FromHours(24);

        private static readonly DataContractJsonSerializer _serializer =
            new DataContractJsonSerializer(typeof(MovimientoDraft));

        public static void Guardar(MovimientoDraft draft)
        {
            if (draft == null) return;
            try
            {
                if (!Directory.Exists(_carpeta))
                    Directory.CreateDirectory(_carpeta);

                draft.FechaGuardado = DateTime.Now;
                using (var ms = new MemoryStream())
                {
                    _serializer.WriteObject(ms, draft);
                    File.WriteAllBytes(_ruta, ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                // En debug podríamos querer ver esto, pero por ahora lo mantenemos silencioso
                // para el usuario final, aunque podrías loguearlo.
                System.Diagnostics.Debug.WriteLine("Error al guardar borrador: " + ex.Message);
            }
        }

        public static MovimientoDraft Leer()
        {
            if (!File.Exists(_ruta)) return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(_ruta);
                if (bytes.Length == 0) return null;

                MovimientoDraft draft;
                using (var ms = new MemoryStream(bytes))
                {
                    draft = (MovimientoDraft)_serializer.ReadObject(ms);
                }

                if (draft == null || (DateTime.Now - draft.FechaGuardado) > _maxEdad)
                {
                    Eliminar();
                    return null;
                }
                return draft;
            }
            catch
            {
                Eliminar();
                return null;
            }
        }

        public static void Eliminar()
        {
            try { if (File.Exists(_ruta)) File.Delete(_ruta); }
            catch { }
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
