using System;
using System.Collections.Generic;
using System.Linq;

namespace SteeLog.Domain.Reportes
{
    public sealed class MovimientoValorizacion
    {
        public int Id { get; set; }
        public string Remito { get; set; }
        public string Tipo { get; set; }
        public string Material { get; set; }
        public bool EsRecorte { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioTotal { get; set; }
        public decimal CostoFlete { get; set; }
        public bool EsCompraEventual { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime Fecha { get; set; }
    }

    public sealed class ResumenValorizacion
    {
        public int CantRemitos { get; set; }
        public int CantMovimientos { get; set; }
        public int TotalMateriales { get; set; }
        public decimal TotalFletes { get; set; }
        public decimal TotalPendiente { get; set; }
    }

    public sealed class BalanceMaterialObra
    {
        public decimal Enviado { get; set; }
        public decimal Devuelto { get; set; }
        public decimal Neto => Enviado - Devuelto;
        public decimal PrecioUnitario { get; set; }
        public decimal ValorNeto { get; set; }
    }

    public static class ValorizacionObra
    {
        public static decimal ValorDetalle(decimal cantidad, decimal precioUnitario) => cantidad * precioUnitario;

        public static IReadOnlyList<decimal> ValoresFilas(IEnumerable<MovimientoValorizacion> filas, IEnumerable<MovimientoValorizacion> universo)
        {
            var individuales = new HashSet<int>((universo ?? Enumerable.Empty<MovimientoValorizacion>())
                .Where(m => m.EsCompraEventual && m.PrecioUnitario > 0).Select(m => m.Id));
            var mostradas = new HashSet<int>();
            return (filas ?? Enumerable.Empty<MovimientoValorizacion>()).Select(m =>
            {
                if ((m.Tipo ?? "").IndexOf("COMPRA EVENTUAL", StringComparison.OrdinalIgnoreCase) >= 0
                    && m.PrecioTotal > 0 && !individuales.Contains(m.Id))
                    return mostradas.Add(m.Id) ? m.PrecioTotal : 0m;
                return ValorDetalle(m.Cantidad, m.PrecioUnitario);
            }).ToList();
        }

        public static BalanceMaterialObra BalanceMaterial(IEnumerable<MovimientoValorizacion> movimientos, string nombreObra)
        {
            var lista = (movimientos ?? Enumerable.Empty<MovimientoValorizacion>()).ToList();
            var enviados = lista.Where(m => m.Destino != null && m.Destino.Equals(nombreObra, StringComparison.OrdinalIgnoreCase));
            var devueltos = lista.Where(m => m.Origen != null && m.Origen.Equals(nombreObra, StringComparison.OrdinalIgnoreCase));
            return new BalanceMaterialObra {
                Enviado = enviados.Sum(m => m.Cantidad), Devuelto = devueltos.Sum(m => m.Cantidad),
                PrecioUnitario = lista.OrderByDescending(m => m.Fecha).Select(m => m.PrecioUnitario).FirstOrDefault(),
                ValorNeto = enviados.Sum(m => ValorDetalle(m.Cantidad, m.PrecioUnitario)) - devueltos.Sum(m => ValorDetalle(m.Cantidad, m.PrecioUnitario))
            };
        }

        public static decimal CalcularValor(IEnumerable<MovimientoValorizacion> movimientos)
        {
            return (movimientos ?? Enumerable.Empty<MovimientoValorizacion>())
                .GroupBy(m => m.Id).Sum(grupo =>
                {
                    var tipo = (grupo.First().Tipo ?? "").ToUpperInvariant();
                    var precioTotal = grupo.Max(m => m.PrecioTotal);
                    var detalles = grupo.Sum(m => ValorDetalle(m.Cantidad, m.PrecioUnitario));
                    return tipo.Contains("COMPRA EVENTUAL") && detalles == 0 && precioTotal > 0
                        ? precioTotal : detalles;
                });
        }

        public static ResumenValorizacion CalcularResumen(IEnumerable<MovimientoValorizacion> movimientos, bool incluirValorizacion, decimal canaletasPendientes = 0)
        {
            var lista = (movimientos ?? Enumerable.Empty<MovimientoValorizacion>()).ToList();
            var resumen = new ResumenValorizacion
            {
                CantRemitos = lista.Where(m => !string.IsNullOrWhiteSpace(m.Remito)).Select(m => m.Remito.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CantMovimientos = lista.Select(m => m.Id).Distinct().Count(),
                TotalMateriales = lista.Where(m => !m.EsRecorte && !string.IsNullOrWhiteSpace(m.Material)).Select(m => m.Material.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            };
            if (!incluirValorizacion) return resumen;
            foreach (var grupo in lista.GroupBy(m => m.Id))
            {
                var tipo = (grupo.First().Tipo ?? "").ToUpperInvariant();
                var valor = CalcularValor(grupo);
                if (tipo.Contains("ENVÍO") || tipo.Contains("ENVIO") || tipo.Contains("COMPRA EVENTUAL") || tipo.Contains("APORTE")) resumen.TotalPendiente += valor;
                else if (tipo.Contains("DEVOLUCIÓN") || tipo.Contains("DEVOLUCION")) resumen.TotalPendiente -= valor;
            }
            resumen.TotalFletes = lista.Where(m => m.CostoFlete > 0).GroupBy(m => m.Id).Select(g => g.First().CostoFlete).Sum();
            resumen.TotalPendiente += resumen.TotalFletes;
            resumen.TotalPendiente += canaletasPendientes;
            return resumen;
        }
    }
}
