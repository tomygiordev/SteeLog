using System;

namespace SteeLog.Domain.Inventario
{
    public sealed class Recorte
    {
        public int Id { get; }
        public int MaterialId { get; }
        public decimal Ancho { get; }
        public decimal Altura { get; }
        public decimal Medida { get; }
        public string Estado { get; }
        public decimal Cantidad { get; private set; }

        public Recorte(int id, int materialId, decimal ancho, decimal altura, decimal medida, decimal cantidad, string estado)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (materialId <= 0) throw new ArgumentOutOfRangeException(nameof(materialId));
            if (!((ancho > 0 && altura > 0 && medida == 0) || (ancho == 0 && altura == 0 && medida > 0) || (ancho > 0 && altura == 0 && medida == ancho) || (altura > 0 && ancho == 0 && medida == altura)))
                throw new ArgumentException("Ingrese una medida o bien ancho y altura positivos.");
            ValidarDimension(ancho, nameof(ancho));
            ValidarDimension(altura, nameof(altura));
            ValidarDimension(medida, nameof(medida));
            if (estado != null && estado != "Buen estado" && estado != "Regular" && estado != "Mal estado")
                throw new ArgumentException("El estado del recorte no es válido.", nameof(estado));
            Material.ValidarCantidad(cantidad, nameof(cantidad));
            Id = id; MaterialId = materialId; Ancho = ancho; Altura = altura; Medida = medida;
            Estado = estado; Cantidad = cantidad;
        }

        private static void ValidarDimension(decimal valor, string nombre)
        {
            if (valor < 0 || valor > 99999999.99m || decimal.Round(valor, 2) != valor)
                throw new ArgumentOutOfRangeException(nombre, "La dimensión debe ser no negativa, no superar 99999999.99 y tener como máximo dos decimales.");
        }

        public void Aplicar(decimal diferencia)
        {
            decimal nuevo = SaldoInventario.Aplicar(Cantidad, diferencia);
            Material.ValidarCantidad(nuevo, nameof(diferencia));
            Cantidad = nuevo;
        }

        public bool CoincideCon(Recorte otro)
        {
            return otro != null && MaterialId == otro.MaterialId && Ancho == otro.Ancho && Altura == otro.Altura && Medida == otro.Medida && Estado == otro.Estado;
        }
    }
}
