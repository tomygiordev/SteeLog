using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Inventario;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Inventario
{
    public sealed class NpgsqlMateriales : IMaterialesEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlMateriales(NpgsqlUnitOfWork uow) { this.uow = uow; }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction);
        public async Task<int> CrearAsync(Material m, CancellationToken token = default)
        {
            using (var cmd = Cmd("INSERT INTO public.materiales(nombre,id_categoria,preciounitario,stockactual,stockminimo) VALUES(@nombre,@categoria,0,@stock,@minimo) RETURNING id"))
            {
                cmd.Parameters.AddWithValue("nombre", m.Nombre); cmd.Parameters.AddWithValue("categoria", m.CategoriaId);
                cmd.Parameters.AddWithValue("stock", m.StockActual); cmd.Parameters.AddWithValue("minimo", m.StockMinimo);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false));
            }
        }
        public async Task ActualizarDatosAsync(int id, DatosMaterial datos, CancellationToken token = default)
        {
            using (var cmd = Cmd("UPDATE public.materiales SET nombre=@nombre,id_categoria=@categoria,stockminimo=@minimo WHERE id=@id"))
            {
                cmd.Parameters.AddWithValue("nombre", datos.Nombre); cmd.Parameters.AddWithValue("categoria", datos.CategoriaId);
                cmd.Parameters.AddWithValue("minimo", datos.StockMinimo); cmd.Parameters.AddWithValue("id", id);
                if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("No se encontró el material.");
            }
        }
        public async Task GuardarProveedoresAsync(int id, IReadOnlyList<int> proveedores, CancellationToken token = default)
        {
            using (var cmd = Cmd("DELETE FROM public.materiales_proveedores WHERE id_material=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
            foreach (var proveedor in proveedores)
                using (var cmd = Cmd("INSERT INTO public.materiales_proveedores(id_material,id_proveedor) VALUES(@id,@proveedor)"))
                { cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("proveedor", proveedor); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
        public async Task<int?> BloquearAjusteDiarioAsync(DateTime fecha, CancellationToken token = default)
        {
            using (var cmd = Cmd("SELECT pg_advisory_xact_lock(141414,@dia)"))
            { cmd.Parameters.AddWithValue("dia", fecha.Year * 10000 + fecha.Month * 100 + fecha.Day); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
            using (var cmd = Cmd("SELECT id_movimiento FROM public.movimientos WHERE id_tipomovimiento=11 AND fecha>=@fecha AND fecha<@hasta ORDER BY id_movimiento DESC LIMIT 1 FOR UPDATE"))
            {
                cmd.Parameters.AddWithValue("fecha", fecha); cmd.Parameters.AddWithValue("hasta", fecha.AddDays(1));
                var resultado = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
                return resultado == null || resultado == DBNull.Value ? (int?)null : Convert.ToInt32(resultado);
            }
        }
        public async Task RegistrarAjusteAsync(int? cabeceraId, DateTime fecha, int materialId, decimal delta, decimal saldo, CancellationToken token = default)
        {
            if (!cabeceraId.HasValue)
            {
                using (var cmd = Cmd(@"INSERT INTO public.movimientos(fecha,nroremito,id_origen,tipo_origen,id_destino,tipo_destino,id_tipomovimiento,id_obra,costoflete,observaciones,precio_total,id_flete) VALUES(@fecha,@remito,1,'UBICACION',1,'UBICACION',11,NULL,0,'Actualización interna de stock',0,NULL) RETURNING id_movimiento"))
                { cmd.Parameters.AddWithValue("fecha", fecha); cmd.Parameters.AddWithValue("remito", "141414-" + fecha.ToString("yyyyMMdd")); cabeceraId = Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)); }
            }
            int? linea;
            using (var cmd = Cmd("SELECT id_movimientomaterial FROM public.movimientomaterial WHERE id_movimiento=@mov AND id_material=@material AND NOT COALESCE(esrecorte,false) ORDER BY id_movimientomaterial LIMIT 1 FOR UPDATE"))
            { cmd.Parameters.AddWithValue("mov", cabeceraId.Value); cmd.Parameters.AddWithValue("material", materialId); var valor = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false); linea = valor == null ? (int?)null : Convert.ToInt32(valor); }
            using (var cmd = Cmd(linea.HasValue
                ? "UPDATE public.movimientomaterial SET cantidad=cantidad+@delta, stock_actual=@saldo, impacto_stock_real=impacto_stock_real+@delta WHERE id_movimientomaterial=@linea"
                : "INSERT INTO public.movimientomaterial(id_movimiento,id_material,cantidad,stock_actual,esrecorte,es_compra_eventual,impacto_stock_real) VALUES(@mov,@material,@delta,@saldo,false,false,@delta)"))
            {
                cmd.Parameters.AddWithValue("delta", delta); cmd.Parameters.AddWithValue("saldo", saldo);
                if (linea.HasValue) cmd.Parameters.AddWithValue("linea", linea.Value);
                else { cmd.Parameters.AddWithValue("mov", cabeceraId.Value); cmd.Parameters.AddWithValue("material", materialId); }
                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }
        public async Task EliminarAsync(int id, CancellationToken token = default)
        {
            using (var cmd = Cmd("SELECT id FROM public.recortes WHERE id_material=@id ORDER BY id FOR UPDATE"))
            { cmd.Parameters.AddWithValue("id", id); using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while (await reader.ReadAsync(token).ConfigureAwait(false)) { } }
            using (var cmd = Cmd("SELECT EXISTS(SELECT 1 FROM public.recortes r JOIN public.movimientomaterial mm ON mm.id_recorte=r.id WHERE r.id_material=@id)"))
            { cmd.Parameters.AddWithValue("id", id); if ((bool)await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)) throw new InvalidOperationException("El material tiene recortes referenciados por movimientos."); }
            using (var cmd = Cmd("DELETE FROM public.materiales WHERE id=@id"))
            { cmd.Parameters.AddWithValue("id", id); if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("No se encontró el material."); }
        }
    }
}
