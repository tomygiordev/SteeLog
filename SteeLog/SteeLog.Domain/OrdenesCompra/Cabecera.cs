using System;

namespace SteeLog.Domain.OrdenesCompra
{
    public sealed class Cabecera
    {
        public DateTime Fecha { get; }
        public string Numero { get; }
        public string Remito { get; }
        public int ProveedorId { get; }
        public decimal Flete { get; }
        public string Observaciones { get; }
        public Cabecera(DateTime fecha, string numero, string remito, int proveedorId, decimal flete, string observaciones)
            : this(fecha, numero, remito, proveedorId, flete, observaciones, false) { }

        public static Cabecera Restaurar(DateTime fecha, string numero, string remito, int proveedorId, decimal flete, string observaciones)
            => new Cabecera(fecha, numero, remito, proveedorId, flete, observaciones, true);

        private Cabecera(DateTime fecha, string numero, string remito, int proveedorId, decimal flete, string observaciones, bool restaurar)
        {
            if (string.IsNullOrWhiteSpace(numero) || numero.Trim().Length > 80) throw new ArgumentException("El número de orden es obligatorio y no puede superar 80 caracteres.", nameof(numero));
            if (proveedorId < 0 || (!restaurar && proveedorId == 0)) throw new ArgumentOutOfRangeException(nameof(proveedorId));
            if (!restaurar && flete < 0) throw new ArgumentOutOfRangeException(nameof(flete));
            Fecha = fecha.Date; Numero = numero.Trim(); Remito = string.IsNullOrWhiteSpace(remito) ? null : remito.Trim();
            ProveedorId = proveedorId; Flete = flete; Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        }
    }
}
