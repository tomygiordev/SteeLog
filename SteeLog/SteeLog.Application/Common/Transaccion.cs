using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteeLog.Core.Application.Common
{
    public static class Transaccion
    {
        public static async Task<T> EjecutarAsync<T>(IUnitOfWork uow, Func<CancellationToken, Task<T>> trabajo, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await uow.BeginAsync(token).ConfigureAwait(false);
            bool inicioCommit = false;
            try
            {
                var resultado = await trabajo(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                inicioCommit = true;
                await uow.CommitAsync(token).ConfigureAwait(false);
                return resultado;
            }
            catch (Exception error)
            {
                try { await uow.RollbackAsync().ConfigureAwait(false); }
                catch { /* Conservar el fallo original, incluso si falla la limpieza. */ }
                if (inicioCommit) throw new ResultadoEscrituraInciertoException(error);
                throw;
            }
        }

        public static Task EjecutarAsync(IUnitOfWork uow, Func<CancellationToken, Task> trabajo, CancellationToken token = default)
        {
            return EjecutarAsync(uow, async t =>
            {
                await trabajo(t).ConfigureAwait(false);
                return true;
            }, token);
        }
    }
}
