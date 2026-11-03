using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Movimientos
{
    public sealed class NpgsqlCatalogosMovimientoConsulta : ICatalogosMovimientoConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlCatalogosMovimientoConsulta(PostgresConnectionFactory factory) { this.factory=factory; }
        public async Task<CatalogosMovimiento> ObtenerAsync(CancellationToken token=default)
        {
            var resultado=new CatalogosMovimiento();
            using(var cn=factory.Create())
            {
                await cn.OpenAsync(token).ConfigureAwait(false);
                using(var cmd=new NpgsqlCommand(@"SELECT id_tipomovimiento,nombre FROM public.tipo_movimiento ORDER BY nombre;
                    SELECT id,nombre,'OBRA' FROM public.obras WHERE activa=true UNION ALL SELECT id_ubicacion,nombre,'UBICACION' FROM public.ubicacion UNION ALL SELECT id_proveedores,nombre,'PROVEEDOR' FROM public.proveedores;
                    SELECT id_flete,nombre,COALESCE(activo,true) FROM public.fletes ORDER BY nombre",cn))
                using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    while(await r.ReadAsync(token).ConfigureAwait(false)) resultado.Tipos.Add(new OpcionMovimiento { Id=r.GetInt32(0),Nombre=r.IsDBNull(1)?"":r.GetString(1) });
                    await r.NextResultAsync(token).ConfigureAwait(false);
                    while(await r.ReadAsync(token).ConfigureAwait(false)) resultado.Destinos.Add(new OpcionMovimiento { Id=r.GetInt32(0),Nombre=r.IsDBNull(1)?"":r.GetString(1),Tipo=r.GetString(2) });
                    await r.NextResultAsync(token).ConfigureAwait(false);
                    while(await r.ReadAsync(token).ConfigureAwait(false)) resultado.Fletes.Add(new OpcionMovimiento { Id=r.GetInt32(0),Nombre=r.IsDBNull(1)?"":r.GetString(1),Activo=r.GetBoolean(2) });
                }
            }
            return resultado;
        }
    }
}
