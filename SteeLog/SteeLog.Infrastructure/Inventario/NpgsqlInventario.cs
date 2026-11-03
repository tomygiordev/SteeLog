using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Inventario;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Inventario
{
    public sealed class NpgsqlInventario : IInventarioEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlInventario(NpgsqlUnitOfWork uow) { this.uow = uow; }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction);
        public async Task<IDictionary<int, decimal>> BloquearAsync(IReadOnlyList<int> materiales, CancellationToken cancellationToken = default)
        {
            var saldos = new Dictionary<int, decimal>();
            foreach (var id in materiales.Distinct().OrderBy(x => x)) {
                using (var cmd = Cmd("SELECT COALESCE(stockactual,0) FROM public.materiales WHERE id=@id FOR UPDATE")) {
                    cmd.Parameters.AddWithValue("id", id);
                    var valor = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (valor == null || valor == DBNull.Value) throw new InvalidOperationException("No se encontró el material seleccionado.");
                    saldos.Add(id, Convert.ToDecimal(valor));
                }
            }
            return saldos;
        }
        public async Task GuardarSaldoAsync(int materialId, decimal saldo, CancellationToken cancellationToken = default)
        {
            using (var cmd = Cmd("UPDATE public.materiales SET stockactual=@saldo WHERE id=@id")) {
                cmd.Parameters.AddWithValue("saldo", saldo); cmd.Parameters.AddWithValue("id", materialId);
                if (await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1) throw new InvalidOperationException("No se encontró el material seleccionado.");
            }
        }
    }
}
