using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Movimientos
{
    public sealed class NpgsqlFletes : IFletesEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlFletes(NpgsqlUnitOfWork uow) { this.uow=uow; }
        public async Task<int> CrearAsync(string nombre,CancellationToken token=default)
        { using(var c=new NpgsqlCommand("INSERT INTO public.fletes(nombre) VALUES(@nombre) RETURNING id_flete",uow.Connection,uow.Transaction)) { c.Parameters.AddWithValue("nombre",nombre); return Convert.ToInt32(await c.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
        public async Task DesactivarAsync(int id,CancellationToken token=default)
        { using(var c=new NpgsqlCommand("UPDATE public.fletes SET activo=false WHERE id_flete=@id",uow.Connection,uow.Transaction)) { c.Parameters.AddWithValue("id",id); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró el flete."); } }
    }
    public sealed class NpgsqlPrecioEventual : IPrecioEventualEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlPrecioEventual(NpgsqlUnitOfWork uow) { this.uow=uow; }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql,uow.Connection,uow.Transaction);
        public async Task<decimal> ActualizarAsync(int id,decimal precio,CancellationToken token=default)
        {
            int movimiento;
            using(var c=Cmd("SELECT id_movimiento FROM public.movimientomaterial WHERE id_movimientomaterial=@id AND es_compra_eventual=true")) { c.Parameters.AddWithValue("id",id); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null) throw new InvalidOperationException("Solo se puede modificar el precio de un detalle eventual."); movimiento=Convert.ToInt32(x); }
            using(var c=Cmd("SELECT id_tipomovimiento FROM public.movimientos WHERE id_movimiento=@id FOR UPDATE")) { c.Parameters.AddWithValue("id",movimiento); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null) throw new InvalidOperationException("El movimiento ya no existe."); SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(Convert.ToInt32(x),null); }
            using(var c=Cmd("UPDATE public.movimientomaterial SET precio_unitario=@precio WHERE id_movimientomaterial=@id AND id_movimiento=@movimiento AND es_compra_eventual=true")) { c.Parameters.AddWithValue("id",id); c.Parameters.AddWithValue("movimiento",movimiento); c.Parameters.AddWithValue("precio",precio); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("El detalle ya no existe."); }
            using(var c=Cmd(@"UPDATE public.movimientos SET precio_total=(SELECT COALESCE(SUM(mm.cantidad*CASE WHEN mm.es_compra_eventual THEN COALESCE(mm.precio_unitario,0) ELSE COALESCE(mat.preciounitario,0) END),0) FROM public.movimientomaterial mm LEFT JOIN public.materiales mat ON mat.id=mm.id_material WHERE mm.id_movimiento=@id) WHERE id_movimiento=@id RETURNING precio_total")) { c.Parameters.AddWithValue("id",movimiento); return Convert.ToDecimal(await c.ExecuteScalarAsync(token).ConfigureAwait(false)); }
        }
    }
}
