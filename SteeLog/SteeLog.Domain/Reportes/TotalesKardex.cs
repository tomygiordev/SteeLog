using System;
using System.Collections.Generic;
using System.Linq;
using SteeLog.Domain.Inventario;

namespace SteeLog.Domain.Reportes
{
    public sealed class ResumenKardex
    {
        public decimal TotalEntradas { get; set; }
        public decimal TotalSalidas { get; set; }
        public decimal StockFinal { get; set; }

        public ResumenKardex() { }

        public ResumenKardex(decimal totalEntradas, decimal totalSalidas, decimal stockFinal = 0m)
        {
            TotalEntradas = totalEntradas;
            TotalSalidas = totalSalidas;
            StockFinal = stockFinal;
        }
    }

    public sealed class LineaKardex
    {
        public string TipoMovimiento { get; set; }
        public string TipoDestino { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Saldo { get; set; }
        public ImpactoStock? Impacto { get; set; }
    }

    public static class TotalesKardex
    {
        public static ImpactoStock Clasificar(string tipo, string tipoDestino = null)
            => ClasificadorMovimiento.Clasificar(tipo, tipoDestino);

        public static decimal Delta(ImpactoStock impacto, decimal cantidad)
        {
            if (impacto == ImpactoStock.Entrada) return cantidad;
            if (impacto == ImpactoStock.Salida) return -cantidad;
            return 0m;
        }

        public static ResumenKardex Calcular(IEnumerable<LineaKardex> lineas)
        {
            decimal totalEntradas = 0m;
            decimal totalSalidas = 0m;
            decimal stockFinal = 0m;
            bool primero = true;

            if (lineas != null)
            {
                foreach (var l in lineas)
                {
                    if (l == null) continue;
                    if (primero)
                    {
                        stockFinal = l.Saldo;
                        primero = false;
                    }

                    var impacto = l.Impacto ?? Clasificar(l.TipoMovimiento, l.TipoDestino);
                    if (impacto == ImpactoStock.Entrada)
                    {
                        // Dirección (+1) * {cantidad original}: preserva cantidad histórica negativa sin normalizar con Abs
                        totalEntradas += l.Cantidad;
                    }
                    else if (impacto == ImpactoStock.Salida)
                    {
                        // Dirección (-1) * {cantidad original}: ENVÍO +2 resta 2 (salida -2); salida negativa -2 suma 2 (+2)
                        totalSalidas -= l.Cantidad;
                    }
                    // SinImpacto (compra eventual, aporte del cliente, etc.) no debe contarse como entrada/salida de stock
                }
            }

            return new ResumenKardex(totalEntradas, totalSalidas, stockFinal);
        }
    }
}
