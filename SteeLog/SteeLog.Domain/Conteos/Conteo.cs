using System;
using System.Collections.Generic;
using System.Linq;

namespace SteeLog.Domain.Conteos
{
    public enum EstadoConteo { Abierto, Finalizado, Cancelado }
    public static class CantidadConteo
    {
        public const decimal Maximo = 9999999999999999.99m;
        public static decimal Validar(decimal valor, bool permitirNegativo = false)
        {
            if (valor != decimal.Round(valor, 2) || valor > Maximo || valor < (permitirNegativo ? -Maximo : 0))
                throw new ArgumentOutOfRangeException(nameof(valor), "La cantidad debe tener hasta dos decimales y caber en numeric(18,2).");
            return valor;
        }
        public static decimal Diferencia(decimal contada, decimal stock) => Validar(Validar(contada) - Validar(stock, true), true);
    }
    public sealed class DetalleConteo
    {
        public int Id { get; }
        public int MaterialId { get; }
        public decimal Cantidad { get; private set; }
        public DetalleConteo(int id, int materialId, decimal cantidad)
        {
            if (id < 0 || materialId <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            Id = id; MaterialId = materialId; Cantidad = CantidadConteo.Validar(cantidad);
        }
        internal void Reemplazar(decimal cantidad) { Cantidad = CantidadConteo.Validar(cantidad); }
    }
    public sealed class Conteo
    {
        private readonly List<DetalleConteo> detalles;
        public int Id { get; }
        public string Propietario { get; }
        public EstadoConteo Estado { get; private set; }
        public IReadOnlyList<DetalleConteo> Detalles => detalles.AsReadOnly();
        public Conteo(int id, string propietario, EstadoConteo estado, IEnumerable<DetalleConteo> detalles)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (!Enum.IsDefined(typeof(EstadoConteo), estado)) throw new ArgumentOutOfRangeException(nameof(estado));
            Id = id; Propietario = propietario; Estado = estado;
            this.detalles = (detalles ?? throw new ArgumentNullException(nameof(detalles))).ToList();
            if (this.detalles.Any(x => x == null) || this.detalles.GroupBy(x => x.MaterialId).Any(x => x.Count() > 1)) throw new ArgumentException("Detalle inválido.");
        }
        public void ExigirAbierto() { if (Estado != EstadoConteo.Abierto) throw new InvalidOperationException("El conteo ya no está abierto."); }
        public decimal Acumular(int materialId, decimal cantidad)
        {
            ExigirAbierto(); if (materialId <= 0) throw new ArgumentOutOfRangeException(nameof(materialId));
            var actual = detalles.SingleOrDefault(x => x.MaterialId == materialId);
            var total = CantidadConteo.Validar((actual?.Cantidad ?? 0) + CantidadConteo.Validar(cantidad));
            if (actual == null) detalles.Add(new DetalleConteo(0, materialId, total)); else actual.Reemplazar(total);
            return total;
        }
        public void Corregir(int materialId, decimal cantidad)
        {
            ExigirAbierto(); var detalle = detalles.SingleOrDefault(x => x.MaterialId == materialId) ?? throw new InvalidOperationException("No existe el detalle.");
            detalle.Reemplazar(cantidad);
        }
        public void Quitar(int detalleId)
        {
            ExigirAbierto(); if (detalleId <= 0) throw new ArgumentOutOfRangeException(nameof(detalleId)); var detalle = detalles.SingleOrDefault(x => x.Id == detalleId) ?? throw new InvalidOperationException("No existe el detalle."); detalles.Remove(detalle);
        }
        public IReadOnlyDictionary<int, decimal> Finalizar(IDictionary<int, decimal> stock)
        {
            ExigirAbierto(); if (detalles.Count == 0) throw new InvalidOperationException("No se puede finalizar un conteo sin materiales cargados.");
            if (stock == null || detalles.Any(x => !stock.ContainsKey(x.MaterialId))) throw new InvalidOperationException("Falta stock para comparar.");
            var diferencias = detalles.ToDictionary(x => x.MaterialId, x => CantidadConteo.Diferencia(x.Cantidad, stock[x.MaterialId]));
            Estado = EstadoConteo.Finalizado; return diferencias;
        }
        public void Cancelar() { ExigirAbierto(); Estado = EstadoConteo.Cancelado; }
        public void ExigirEliminable() { if (Estado == EstadoConteo.Abierto) throw new InvalidOperationException("Primero cancelá el conteo abierto."); }
    }
}
