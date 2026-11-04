using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Inventario;

namespace SteeLog.Core.Application.Reportes
{
    public sealed class ReporteStockItem
    {
        public MaterialInventarioLectura Material { get; set; }
        public string Seccion { get; set; }
        public int OrdenSeccion { get; set; }
        public int OrdenMaterial { get; set; }
    }

    public sealed class ReporteStockSeccion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public List<int> Materiales { get; set; } = new List<int>();
    }

    public interface IReporteStockConsulta
    {
        Task<bool> TieneConfiguracionAsync(CancellationToken token = default);
        Task<IReadOnlyList<ReporteStockItem>> ListarConfiguradosAsync(FiltroMaterialLectura filtro, CancellationToken token = default);
        Task<IReadOnlyList<ReporteStockSeccion>> SeccionesAsync(CancellationToken token = default);
    }
}
