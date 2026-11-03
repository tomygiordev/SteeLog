using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace SteeLog.Core.Application.Inventario
{
    public interface IInventarioEscritura
    {
        Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> materiales, CancellationToken cancellationToken = default);
        Task GuardarSaldoAsync(int materialId, decimal saldo, CancellationToken cancellationToken = default);
    }
}
