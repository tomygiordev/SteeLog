using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NpgsqlTypes;
using SteeLog.Core.Application.Inventario;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Inventario
{
    public sealed class NpgsqlRecortesConsulta : IRecortesConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlRecortesConsulta(PostgresConnectionFactory factory) { this.factory = factory; }
        public Task<IReadOnlyList<OpcionRecorte>> CategoriasAsync(CancellationToken token = default)
            => OpcionesAsync("SELECT id_categoria,nombre FROM public.categorias WHERE recortable=true ORDER BY nombre", null, token);
        public Task<IReadOnlyList<OpcionRecorte>> MaterialesAsync(int categoriaId, CancellationToken token = default)
            => OpcionesAsync("SELECT id,nombre FROM public.materiales WHERE id_categoria=@categoria ORDER BY nombre", categoriaId, token);
        private async Task<IReadOnlyList<OpcionRecorte>> OpcionesAsync(string sql, int? categoriaId, CancellationToken token)
        {
            var lista = new List<OpcionRecorte>();
            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = sql;
                    if (categoriaId.HasValue) cmd.Parameters.AddWithValue("categoria", categoriaId.Value);
                    using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                        while (await reader.ReadAsync(token).ConfigureAwait(false)) lista.Add(new OpcionRecorte { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
                }
            }
            return lista;
        }
        public async Task<IReadOnlyList<RecorteLectura>> ListarAsync(int? materialId, decimal? ancho, decimal? altura, decimal? medida, CancellationToken token = default)
        {
            var lista = new List<RecorteLectura>();
            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = @"SELECT r.id,r.id_material,m.nombre,c.nombre,r.ancho,r.altura,r.medida,r.cantidad,r.estado,r.fecha_creacion FROM public.recortes r JOIN public.materiales m ON m.id=r.id_material JOIN public.categorias c ON c.id_categoria=m.id_categoria WHERE r.cantidad>0 AND (@material IS NULL OR r.id_material=@material) AND (@ancho IS NULL OR r.ancho=@ancho) AND (@altura IS NULL OR r.altura=@altura) AND (@medida IS NULL OR r.medida=@medida) ORDER BY r.fecha_creacion DESC";
                    cmd.Parameters.Add("material", NpgsqlDbType.Integer).Value = (object)materialId ?? DBNull.Value;
                    cmd.Parameters.Add("ancho", NpgsqlDbType.Numeric).Value = (object)ancho ?? DBNull.Value;
                    cmd.Parameters.Add("altura", NpgsqlDbType.Numeric).Value = (object)altura ?? DBNull.Value;
                    cmd.Parameters.Add("medida", NpgsqlDbType.Numeric).Value = (object)medida ?? DBNull.Value;
                    using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                        while (await r.ReadAsync(token).ConfigureAwait(false))
                            lista.Add(new RecorteLectura { Id=r.GetInt32(0), MaterialId=r.GetInt32(1), Material=r.IsDBNull(2)?"":r.GetString(2), Categoria=r.IsDBNull(3)?"":r.GetString(3), Ancho=r.IsDBNull(4)?0:r.GetDecimal(4), Altura=r.IsDBNull(5)?0:r.GetDecimal(5), Medida=r.GetDecimal(6), Cantidad=r.GetDecimal(7), Estado=r.IsDBNull(8)?null:r.GetString(8), FechaCreacion=r.IsDBNull(9)?DateTime.MinValue:r.GetDateTime(9) });
                }
            }
            return lista;
        }
    }
}
