using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SteeLog.Presentation;

namespace SteeLog.CompositionRoot
{
    internal sealed class UseCaseExecutor : IUseCaseExecutor
    {
        private readonly IServiceScopeFactory scopes;
        internal UseCaseExecutor(IServiceScopeFactory scopes) { this.scopes = scopes; }
        public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            using (var scope = scopes.CreateScope())
                return await operation(scope.ServiceProvider.GetRequiredService<TUseCase>());
        }
    }
}
