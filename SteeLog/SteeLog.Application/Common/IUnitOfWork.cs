using System.Threading;
using System.Threading.Tasks;
namespace SteeLog.Core.Application.Common
{
    public interface IUnitOfWork
    {
        Task BeginAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync();
    }
}
