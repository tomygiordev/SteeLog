using System;
using System.Threading.Tasks;

namespace SteeLog.Presentation
{
    public interface IUseCaseExecutor
    {
        Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class;
    }
}
