using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using SteeLog.Modelos;
using SteeLog.Domain.Inventario;

namespace SteeLog.Datos
{
    public class MovimientoDatos
    {
        public List<KardexItem> ObtenerKardex(int? idMaterial = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)

        {

            List<KardexItem> kardex = new List<KardexItem>();

            ConexionDB conexionDB = new ConexionDB();



            using (var conexion = conexionDB.GetConnection())

            {

                if (conexion.State == ConnectionState.Closed) conexion.Open();



                string query = @"

                    SELECT

                        m.fecha,

                        tm.nombre as tipo_movimiento,

                        mm.cantidad,

                        -- Resolve Origin Name

                        CASE

                            WHEN m.tipo_origen = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_origen)

                            WHEN m.tipo_origen = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_origen)

                            WHEN m.tipo_origen = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_origen)

                            ELSE 'Desconocido'

                        END as origen_nombre,

                        -- Resolve Destination Name

                        CASE

                            WHEN m.tipo_destino = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_destino)

                            WHEN m.tipo_destino = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_destino)

                            WHEN m.tipo_destino = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_destino)

                            ELSE 'Desconocido'

                        END as destino_nombre,

                        m.tipo_destino,

                        mm.stock_actual,

                        m.id_movimiento,

                        mm.id_material,

                        COALESCE(mat.nombre, mm.material_nombre_eventual) as nombre_material

                    FROM public.movimientos m

                    INNER JOIN public.movimientomaterial mm ON m.id_movimiento = mm.id_movimiento

                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento

                    LEFT JOIN public.materiales mat ON mm.id_material = mat.id

                    WHERE 1=1

                ";



                if (idMaterial.HasValue)

                {

                    query += " AND mm.id_material = @idMaterial";

                }

                if (fechaInicio.HasValue)

                {

                    query += " AND m.fecha >= @fechaInicio";

                }

                if (fechaFin.HasValue)

                {

                    query += " AND m.fecha <= @fechaFin";

                }



                query += " ORDER BY m.fecha DESC, m.id_movimiento DESC"; // Descending order for history view makes more sense if "All"



                using (var cmd = new NpgsqlCommand(query, conexion))

                {

                    if (idMaterial.HasValue) cmd.Parameters.AddWithValue("@idMaterial", idMaterial.Value);

                    if (fechaInicio.HasValue) cmd.Parameters.AddWithValue("@fechaInicio", fechaInicio.Value);

                    if (fechaFin.HasValue) cmd.Parameters.AddWithValue("@fechaFin", fechaFin.Value);



                    using (var reader = cmd.ExecuteReader())

                    {

                        decimal saldoAcumulado = 0;

                        while (reader.Read())

                        {

                            var item = new KardexItem();

                            item.fecha = reader.GetDateTime(0);

                            item.tipo_movimiento = reader.IsDBNull(1) ? "-" : reader.GetString(1);

                            decimal cantidad = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);

                            item.origen = reader.IsDBNull(3) ? "-" : reader.GetString(3);

                            item.destino = reader.IsDBNull(4) ? "-" : reader.GetString(4);

                            string tipoDestino = reader.IsDBNull(5) ? null : reader.GetString(5);



                            decimal? stockActualRegistrado = reader.IsDBNull(6) ? (decimal?)null : reader.GetDecimal(6);



                            item.id_movimiento = reader.GetInt32(7);

                            item.id_material = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8);

                            item.nombre_material = reader.IsDBNull(9) ? "" : reader.GetString(9);



                            ImpactoStock impactoStock = ClasificadorMovimiento.Clasificar(item.tipo_movimiento, tipoDestino);



                            // Maintain calculation logic for older records or fallback

                            if (impactoStock == ImpactoStock.Entrada)

                            {

                                saldoAcumulado += cantidad;

                            }

                            else if (impactoStock == ImpactoStock.Salida)

                            {

                                saldoAcumulado -= cantidad;

                            }



                            item.cantidad = cantidad;



                            if (stockActualRegistrado.HasValue)

                            {

                                item.saldo = stockActualRegistrado.Value;

                                saldoAcumulado = stockActualRegistrado.Value;

                            }

                            else

                            {

                                item.saldo = saldoAcumulado;

                            }



                            kardex.Add(item);

                        }

                    }

                }

            }

            return kardex;

        }

        public void EliminarMovimiento(int idMovimiento, int? idMaterial, decimal cantidad, string tipoMovimiento)

        {

            System.Threading.Tasks.Task.Run(() => SteeLog.CompositionRoot.LocalServices.Current.Executor.ExecuteAsync<SteeLog.Core.Application.Movimientos.EliminarMovimiento,int>(x => x.EjecutarAsync(idMovimiento))).GetAwaiter().GetResult();

        }
    }
}
