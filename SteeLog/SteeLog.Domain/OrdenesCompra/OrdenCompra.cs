using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SteeLog.Domain.OrdenesCompra
{
    public enum EstadoOrden { Pendiente, RecibidaParcial, RecibidaCompleta }

    public static class CantidadOrden
    {
        public static int Entera(decimal valor, bool permitirCero)
        {
            if (valor != decimal.Truncate(valor) || valor > int.MaxValue || valor < (permitirCero ? 0 : 1))
                throw new ArgumentOutOfRangeException(nameof(valor), "La cantidad debe ser entera y estar dentro del rango permitido.");
            return decimal.ToInt32(valor);
        }
    }

    public sealed class LineaOrden
    {
        public int Id { get; }
        public int MaterialId { get; private set; }
        public int Pedida { get; }
        public int Recibida { get; private set; }
        public int Faltante => Pedida - Recibida;
        public bool TieneSnapshotRecepcion { get; }

        public LineaOrden(int id, int materialId, decimal pedida, decimal recibida, bool tieneSnapshotRecepcion = false)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (materialId <= 0) throw new ArgumentOutOfRangeException(nameof(materialId));
            TieneSnapshotRecepcion = tieneSnapshotRecepcion;
            Id = id;
            MaterialId = materialId;
            Pedida = CantidadOrden.Entera(pedida, false);
            Recibida = CantidadOrden.Entera(recibida, true);
            if (Recibida > Pedida) throw new ArgumentException("La cantidad recibida no puede superar a la pedida.");
        }

        internal int Delta(decimal total)
        {
            int recibido = CantidadOrden.Entera(total, true);
            if (recibido < Recibida || recibido > Pedida)
                throw new InvalidOperationException("La recepción debe ser acumulativa y no superar la cantidad pedida.");
            return recibido - Recibida;
        }
        internal void AplicarRecepcion(int delta) { Recibida += delta; }
        internal void CambiarMaterial(int id) { MaterialId = id; }
    }

    public sealed class OrdenCompra
    {
        private readonly List<LineaOrden> lineas;
        public int Id { get; }
        public Cabecera Cabecera { get; private set; }
        public EstadoOrden Estado { get; private set; }
        public IReadOnlyList<LineaOrden> Lineas { get; }

        public OrdenCompra(int id, Cabecera cabecera, EstadoOrden estado, IEnumerable<LineaOrden> lineas)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            Id = id; Cabecera = cabecera ?? throw new ArgumentNullException(nameof(cabecera));
            if (!Enum.IsDefined(typeof(EstadoOrden), estado)) throw new ArgumentOutOfRangeException(nameof(estado));
            if (lineas == null) throw new ArgumentNullException(nameof(lineas));
            this.lineas = lineas.ToList();
            if (this.lineas.Count == 0 || this.lineas.Any(x => x == null)) throw new ArgumentException("Debe cargar al menos un material.");
            if (this.lineas.GroupBy(x => x.MaterialId).Any(x => x.Count() > 1)) throw new ArgumentException("No se puede repetir un mismo material dentro de la orden.");
            if (this.lineas.Where(x => x.Id > 0).GroupBy(x => x.Id).Any(x => x.Count() > 1)) throw new ArgumentException("Los identificadores de línea deben ser únicos.");
            if (estado == EstadoOrden.Pendiente && this.lineas.Any(x => x.Recibida != 0)) throw new ArgumentException("Una orden pendiente no puede tener recepción acumulada.");
            if (estado == EstadoOrden.RecibidaCompleta && this.lineas.Any(x => x.Faltante != 0)) throw new ArgumentException("Una orden completa no puede tener faltantes.");
            if (estado == EstadoOrden.RecibidaParcial && this.lineas.All(x => x.Faltante == 0)) throw new ArgumentException("Una orden parcial debe tener faltantes.");
            Estado = estado;
            Lineas = new ReadOnlyCollection<LineaOrden>(this.lineas);
        }

        public void EditarCabecera(Cabecera cabecera) { Cabecera = cabecera ?? throw new ArgumentNullException(nameof(cabecera)); }

        public void ExigirPendiente()
        {
            if (Estado != EstadoOrden.Pendiente) throw new InvalidOperationException("Solo se permiten cambios de líneas o eliminación en órdenes pendientes.");
        }

        public IReadOnlyDictionary<int, int> Recibir(IReadOnlyDictionary<int, decimal> acumulados)
        {
            if (Estado == EstadoOrden.RecibidaCompleta) throw new InvalidOperationException("La orden ya está completa.");
            if (acumulados == null || acumulados.Count != lineas.Count || lineas.Any(x => x.Id <= 0 || !acumulados.ContainsKey(x.Id)))
                throw new ArgumentException("La revisión no coincide con el detalle actual de la orden.");
            var deltas = lineas.ToDictionary(x => x.Id, x => x.Delta(acumulados[x.Id]));
            foreach (var linea in lineas) linea.AplicarRecepcion(deltas[linea.Id]);
            Estado = lineas.All(x => x.Faltante == 0) ? EstadoOrden.RecibidaCompleta : EstadoOrden.RecibidaParcial;
            return new ReadOnlyDictionary<int, int>(deltas);
        }

        public int CambiarMaterial(int lineaId, int materialNuevo)
        {
            if (materialNuevo <= 0) throw new ArgumentOutOfRangeException(nameof(materialNuevo));
            var linea = lineas.SingleOrDefault(x => x.Id == lineaId) ?? throw new InvalidOperationException("La línea indicada no existe.");
            if (lineas.Any(x => x != linea && x.MaterialId == materialNuevo)) throw new InvalidOperationException("No se puede repetir un mismo material dentro de la orden.");
            linea.CambiarMaterial(materialNuevo);
            return linea.Recibida;
        }
    }
}
