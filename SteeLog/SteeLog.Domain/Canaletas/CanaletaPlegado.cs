using System;
namespace SteeLog.Domain.Canaletas
{
    public sealed class CanaletaPlegado
    {
        public int Id { get; }
        public int ObraId { get; private set; }
        public string Descripcion { get; private set; }
        public decimal Precio { get; private set; }
        public bool Pagado { get; private set; }
        public CanaletaPlegado(int id, int obraId, string descripcion, decimal precio, bool pagado)
        { if (id < 0) throw new ArgumentException("Identificador inválido."); Id = id; Modificar(obraId, descripcion, precio, pagado); }
        public void Modificar(int obraId, string descripcion, decimal precio, bool pagado)
        {
            if (obraId <= 0) throw new ArgumentException("Seleccione una obra.");
            if (string.IsNullOrWhiteSpace(descripcion)) throw new ArgumentException("La descripción es obligatoria.");
            if (precio < 0 || precio > 999999999999999999.99m || decimal.Round(precio, 2) != precio) throw new ArgumentException("El precio debe tener hasta 18 dígitos enteros y dos decimales, y no puede ser negativo.");
            ObraId = obraId; Descripcion = descripcion.Trim(); Precio = precio; Pagado = pagado;
        }
        public int SiguienteOrden(int maximo) { if (maximo < 0 || maximo == int.MaxValue) throw new InvalidOperationException("Orden de fotos inválido."); return maximo + 1; }
    }
}
