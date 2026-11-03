using System;

namespace SteeLog.Domain.Inventario
{
    public static class ImpactoMovimiento
    {
        public static decimal EfectoReal(decimal saldoAnterior, decimal saldoPosterior)
        {
            if (saldoAnterior < 0 || saldoPosterior < 0)
                throw new InvalidOperationException("El saldo de inventario no puede ser negativo.");
            return checked(saldoPosterior - saldoAnterior);
        }

        public static decimal ParaReversion(decimal? efectoRegistrado, decimal cantidad, ImpactoStock impacto)
        {
            if (efectoRegistrado.HasValue) return checked(-efectoRegistrado.Value);
            if (impacto == ImpactoStock.SinImpacto) return 0m;
            if (decimal.Round(cantidad, 2) != cantidad)
                throw new InvalidOperationException("No se puede revertir el movimiento histórico: no se registró su impacto real y la cantidad tiene más de dos decimales.");
            return impacto == ImpactoStock.Entrada ? -cantidad : cantidad;
        }
    }
}
