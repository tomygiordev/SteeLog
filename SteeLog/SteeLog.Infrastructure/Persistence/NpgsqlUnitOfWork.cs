using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Common;

namespace SteeLog.Infrastructure.Persistence
{
    public sealed class NpgsqlUnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly PostgresConnectionFactory factory;
        private NpgsqlConnection connection;
        private NpgsqlTransaction transaction;
        public NpgsqlUnitOfWork(PostgresConnectionFactory factory) { this.factory = factory; }
        internal NpgsqlConnection Connection => connection ?? throw new InvalidOperationException("No hay transacción activa.");
        internal NpgsqlTransaction Transaction => transaction ?? throw new InvalidOperationException("No hay transacción activa.");
        public async Task BeginAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (connection != null) throw new InvalidOperationException("Ya existe una transacción en este scope.");
            connection = factory.Create();
            try { await connection.OpenAsync(cancellationToken).ConfigureAwait(false); cancellationToken.ThrowIfCancellationRequested(); transaction = connection.BeginTransaction(); }
            catch { connection.Dispose(); connection = null; throw; }
        }
        internal Task<bool> TieneImpactoTipoAsync(CancellationToken token) => factory.TieneImpactoTipoAsync(Connection, Transaction, token);

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Transaction.Commit(); transaction.Dispose(); transaction = null;
            connection.Dispose(); connection = null;
            return Task.CompletedTask;
        }
        public Task RollbackAsync()
        {
            try { transaction?.Rollback(); }
            finally { transaction?.Dispose(); transaction = null; connection?.Dispose(); connection = null; }
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            try { transaction?.Rollback(); }
            finally { transaction?.Dispose(); transaction = null; connection?.Dispose(); connection = null; }
        }
    }
}
