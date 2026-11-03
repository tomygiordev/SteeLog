using System;

namespace SteeLog.Domain.Inventario
{
    public enum ImpactoStock { SinImpacto, Entrada, Salida }

    public static class ClasificadorMovimiento
    {
        public static ImpactoStock Clasificar(string tipo, string destino = null, bool registroInternoYaAplicado = false)
        {
            var nombre = (tipo ?? string.Empty).ToUpperInvariant();
            if (nombre.Contains("REGISTRO INTERNO"))
                return registroInternoYaAplicado ? ImpactoStock.SinImpacto : ImpactoStock.Entrada;
            if (nombre.Contains("APORTE DEL CLIENTE") || nombre.Contains("APORTE DE CLIENTE") || nombre.Contains("COMPRA EVENTUAL"))
                return ImpactoStock.SinImpacto;
            if (nombre.Contains("ENTRADA") || nombre.Contains("INGRESO") || nombre.Contains("CARGA INICIAL") ||
                nombre.Contains("COMPRA") || nombre.Contains("DEVOLUCION") || nombre.Contains("DEVOLUCIÓN") || nombre.Contains("AJUSTE POSITIVO"))
                return ImpactoStock.Entrada;
            if (nombre.Contains("SALIDA") || nombre.Contains("EGRESO") || nombre.Contains("CONSUMO") ||
                nombre.Contains("VENTA") || nombre.Contains("ENVÍO") || nombre.Contains("ENVIO") ||
                nombre.Contains("USO INTERNO") || nombre.Contains("AJUSTE NEGATIVO"))
                return ImpactoStock.Salida;
            return string.Equals(destino, "OBRA", StringComparison.OrdinalIgnoreCase) ? ImpactoStock.Salida : ImpactoStock.SinImpacto;
        }
    }
}
