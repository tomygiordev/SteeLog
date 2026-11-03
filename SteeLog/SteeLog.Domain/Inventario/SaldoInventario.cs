using System;

namespace SteeLog.Domain.Inventario
{
    public static class SaldoInventario
    {
        public static decimal Aplicar(decimal saldo, decimal delta)
        {
            decimal nuevo = checked(saldo + delta);
            if (nuevo < 0) throw new InvalidOperationException("Stock insuficiente para transferir la cantidad recibida.");
            return nuevo;
        }
    }
}
