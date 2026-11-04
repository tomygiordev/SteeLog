using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Reportes
{
    public sealed class NpgsqlReporteStockConsulta : IReporteStockConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlReporteStockConsulta(PostgresConnectionFactory factory) { this.factory = factory; }

        public async Task<bool> TieneConfiguracionAsync(CancellationToken token = default)
        {
            using (var connection = factory.Create())
            {
                await connection.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM reporte_stock_secciones s JOIN reporte_stock_materiales rm ON rm.id_seccion=s.id)", connection))
                    return (bool)await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
            }
        }

        public async Task<IReadOnlyList<ReporteStockSeccion>> SeccionesAsync(CancellationToken token = default)
        {
            var result = new List<ReporteStockSeccion>();
            using (var connection = factory.Create())
            {
                await connection.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand(@"SELECT s.id,s.nombre,rm.id_material FROM reporte_stock_secciones s
                    LEFT JOIN reporte_stock_materiales rm ON rm.id_seccion=s.id
                    ORDER BY s.orden,s.nombre,rm.orden", connection))
                using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    ReporteStockSeccion actual = null;
                    while (await reader.ReadAsync(token).ConfigureAwait(false))
                    {
                        if (actual == null || actual.Id != reader.GetInt32(0))
                        {
                            actual = new ReporteStockSeccion { Id = reader.GetInt32(0), Nombre = reader.GetString(1) };
                            result.Add(actual);
                        }
                        if (!reader.IsDBNull(2)) actual.Materiales.Add(reader.GetInt32(2));
                    }
                }
            }
            return result;
        }

        public async Task<IReadOnlyList<ReporteStockItem>> ListarConfiguradosAsync(FiltroMaterialLectura filtro, CancellationToken token = default)
        {
            filtro = filtro ?? new FiltroMaterialLectura();
            var result = new List<ReporteStockItem>();
            using (var connection = factory.Create())
            {
                await connection.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = connection.CreateCommand())
                {
                    var where = new List<string>();
                    if (!string.IsNullOrWhiteSpace(filtro.Nombre)) { where.Add("m.nombre ILIKE @nombre"); cmd.Parameters.AddWithValue("nombre", "%" + filtro.Nombre.Trim() + "%"); }
                    if (filtro.Desde.HasValue) { where.Add("m.stockactual>=@desde"); cmd.Parameters.AddWithValue("desde", filtro.Desde.Value); }
                    if (filtro.Hasta.HasValue) { where.Add("m.stockactual<=@hasta"); cmd.Parameters.AddWithValue("hasta", filtro.Hasta.Value); }
                    if (filtro.Categorias != null && filtro.Categorias.Count > 0) { where.Add("m.id_categoria=ANY(@categorias)"); cmd.Parameters.AddWithValue("categorias", filtro.Categorias.ToArray()); }
                    if (filtro.Proveedores != null && filtro.Proveedores.Count > 0) { where.Add("EXISTS(SELECT 1 FROM materiales_proveedores mp WHERE mp.id_material=m.id AND mp.id_proveedor=ANY(@proveedores))"); cmd.Parameters.AddWithValue("proveedores", filtro.Proveedores.ToArray()); }
                    if (filtro.SoloStockBajo) where.Add("m.stockminimo>0 AND m.stockactual<=m.stockminimo");
                    cmd.CommandText = @"SELECT m.id,m.nombre,m.id_categoria,c.nombre,m.preciounitario,m.stockactual,m.stockminimo,
                        s.nombre,s.orden,rm.orden,uso.cantidad,uso.movimientos
                        FROM reporte_stock_materiales rm JOIN reporte_stock_secciones s ON s.id=rm.id_seccion
                        JOIN materiales m ON m.id=rm.id_material JOIN categorias c ON c.id_categoria=m.id_categoria
                        LEFT JOIN LATERAL (SELECT COALESCE(SUM(ABS(mm.cantidad)),0) cantidad,COUNT(*) movimientos
                            FROM movimientomaterial mm WHERE mm.id_material=m.id) uso ON true" +
                        (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") +
                        " ORDER BY s.orden,s.nombre,rm.orden,m.nombre";
                    using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                        while (await reader.ReadAsync(token).ConfigureAwait(false))
                            result.Add(new ReporteStockItem { Material = new MaterialInventarioLectura {
                                Id=reader.GetInt32(0), Nombre=reader.GetString(1), CategoriaId=reader.GetInt32(2), Categoria=reader.GetString(3),
                                Precio=reader.GetDecimal(4), Stock=reader.GetDecimal(5), Minimo=reader.GetDecimal(6),
                                CantidadUsada=reader.GetDecimal(10), CantidadMovimientos=checked((int)reader.GetInt64(11)) },
                                Seccion=reader.GetString(7), OrdenSeccion=reader.GetInt32(8), OrdenMaterial=reader.GetInt32(9) });
                }
            }
            return result;
        }
    }
}
