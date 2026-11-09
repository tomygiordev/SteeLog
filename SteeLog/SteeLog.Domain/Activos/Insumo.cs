using System;
namespace SteeLog.Domain.Activos
{
    public sealed class Insumo
    {
        public int Id { get; }
        public string Nombre { get; }
        public int? ObraId { get; }
        public bool Consumido { get; }
        public string Observaciones { get; }
        public Insumo(int id, string nombre, int? obraId, bool consumido, string observaciones)
        {
            if (id < 0 || string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 255 || (obraId.HasValue && obraId.Value <= 0)) throw new ArgumentException("Datos del insumo inválidos.");
            Id=id; Nombre=nombre.Trim(); ObraId=obraId; Consumido=consumido; Observaciones=observaciones;
        }
    }
}
