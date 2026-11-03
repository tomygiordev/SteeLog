using System;

namespace SteeLog.Domain.Inventario
{
    public sealed class Material
    {
        public int Id { get; }
        public string Nombre { get; private set; }
        public int CategoriaId { get; private set; }
        public decimal StockActual { get; private set; }
        public decimal StockMinimo { get; private set; }

        public Material(int id, string nombre, int categoriaId, decimal stockActual, decimal stockMinimo)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            Id = id;
            CambiarDatos(nombre, categoriaId, stockMinimo);
            ValidarCantidad(stockActual, nameof(stockActual));
            StockActual = stockActual;
        }

        public void CambiarDatos(string nombre, int categoriaId, decimal stockMinimo)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 150)
                throw new ArgumentException("El nombre es obligatorio y admite hasta 150 caracteres.", nameof(nombre));
            if (categoriaId <= 0) throw new ArgumentOutOfRangeException(nameof(categoriaId));
            ValidarCantidad(stockMinimo, nameof(stockMinimo));
            Nombre = nombre.Trim();
            CategoriaId = categoriaId;
            StockMinimo = stockMinimo;
        }

        public decimal AjustarStock(decimal nuevoStock, decimal esperado)
        {
            ValidarCantidad(nuevoStock, nameof(nuevoStock));
            ValidarCantidad(esperado, nameof(esperado));
            if (StockActual != esperado) throw new InvalidOperationException("El stock cambió desde la selección. Recargue y vuelva a seleccionar el material.");
            decimal diferencia = nuevoStock - StockActual;
            StockActual = nuevoStock;
            return diferencia;
        }

        internal static void ValidarCantidad(decimal cantidad, string nombre)
        {
            if (cantidad < 0 || cantidad > 99999999.99m || decimal.Round(cantidad, 2) != cantidad)
                throw new ArgumentOutOfRangeException(nombre, "La cantidad debe ser no negativa y tener como máximo dos decimales.");
        }
    }
}
