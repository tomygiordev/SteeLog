using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Reportes
{
    public sealed class NpgsqlReporteStockConfiguracion : IReporteStockConfiguracion
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlReporteStockConfiguracion(NpgsqlUnitOfWork uow) { this.uow = uow ?? throw new ArgumentNullException(nameof(uow)); }
        public async Task GuardarAsync(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, CancellationToken token = default)
        {
                    // Lock before editing and keep every mutation in the same transaction.
                    using (var cmd = new NpgsqlCommand("LOCK TABLE reporte_stock_secciones, reporte_stock_materiales IN SHARE ROW EXCLUSIVE MODE", uow.Connection, uow.Transaction))
                        await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                    foreach (var id in eliminadas)
                    {
                        using (var cmd = new NpgsqlCommand("DELETE FROM reporte_stock_secciones WHERE id=@id", uow.Connection, uow.Transaction))
                        {
                            cmd.Parameters.AddWithValue("id", id);
                            if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("La sección eliminada ya no existe.");
                        }
                    }
                    foreach (var seccion in secciones)
                    {
                        if (seccion.Id == 0) continue;
                        using (var cmd = new NpgsqlCommand("UPDATE reporte_stock_secciones SET nombre=@nombre WHERE id=@id", uow.Connection, uow.Transaction))
                        {
                            cmd.Parameters.AddWithValue("id", seccion.Id);
                            cmd.Parameters.AddWithValue("nombre", "__steelog_tmp_" + Guid.NewGuid().ToString("N"));
                            if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("La sección ya no existe.");
                        }
                        using (var cmd = new NpgsqlCommand("DELETE FROM reporte_stock_materiales WHERE id_seccion=@id", uow.Connection, uow.Transaction))
                        { cmd.Parameters.AddWithValue("id", seccion.Id); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
                    }
                    for (int i = 0; i < secciones.Count; i++)
                    {
                        var seccion = secciones[i];
                        int id;
                        using (var cmd = new NpgsqlCommand(seccion.Id == 0
                            ? "INSERT INTO reporte_stock_secciones(nombre,orden) VALUES(@nombre,@orden) RETURNING id"
                            : "UPDATE reporte_stock_secciones SET nombre=@nombre,orden=@orden WHERE id=@id RETURNING id", uow.Connection, uow.Transaction))
                        {
                            cmd.Parameters.AddWithValue("nombre", seccion.Nombre.Trim());
                            cmd.Parameters.AddWithValue("orden", i + 1);
                            if (seccion.Id != 0) cmd.Parameters.AddWithValue("id", seccion.Id);
                            id = Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false));
                        }
                        for (int j = 0; j < seccion.Materiales.Count; j++)
                        {
                            using (var cmd = new NpgsqlCommand("INSERT INTO reporte_stock_materiales(id_seccion,id_material,orden) VALUES(@seccion,@material,@orden)", uow.Connection, uow.Transaction))
                            {
                                cmd.Parameters.AddWithValue("seccion", id);
                                cmd.Parameters.AddWithValue("material", seccion.Materiales[j]);
                                cmd.Parameters.AddWithValue("orden", j + 1);
                                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                            }
                        }
                    }
        }
    }
}
