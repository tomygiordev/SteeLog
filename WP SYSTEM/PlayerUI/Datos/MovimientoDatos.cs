using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class MovimientoDatos
    {
        private enum ImpactoStock
        {
            SinImpacto,
            Entrada,
            Salida
        }

        private static bool EsRegistroInterno(string tipo)
        {
            return tipo.Contains("REGISTRO INTERNO");
        }

        private static ImpactoStock ClasificarImpactoStock(string tipoMovimiento, string tipoDestino = null, bool registroInternoYaAplicado = false)
        {
            string tipo = (tipoMovimiento ?? "").ToUpperInvariant();

            if (EsRegistroInterno(tipo))
                return registroInternoYaAplicado ? ImpactoStock.SinImpacto : ImpactoStock.Entrada;

            if (tipo.Contains("APORTE DEL CLIENTE") ||
                tipo.Contains("APORTE DE CLIENTE") ||
                tipo.Contains("COMPRA EVENTUAL"))
                return ImpactoStock.SinImpacto;

            if (tipo.Contains("ENTRADA") || tipo.Contains("INGRESO") ||
                tipo.Contains("CARGA INICIAL") || tipo.Contains("COMPRA") ||
                tipo.Contains("DEVOLUCION") || tipo.Contains("DEVOLUCIÓN") ||
                tipo.Contains("AJUSTE POSITIVO"))
                return ImpactoStock.Entrada;

            if (tipo.Contains("SALIDA") || tipo.Contains("EGRESO") ||
                tipo.Contains("CONSUMO") || tipo.Contains("VENTA") ||
                tipo.Contains("ENVÍO") || tipo.Contains("ENVIO") ||
                tipo.Contains("USO INTERNO") || tipo.Contains("AJUSTE NEGATIVO"))
                return ImpactoStock.Salida;

            if (string.Equals(tipoDestino, "OBRA", StringComparison.OrdinalIgnoreCase))
                return ImpactoStock.Salida;

            return ImpactoStock.SinImpacto;
        }

        public static bool TipoMovimientoConsumeStock(string tipoMovimiento)
        {
            return ClasificarImpactoStock(tipoMovimiento) == ImpactoStock.Salida;
        }

        public static bool TipoMovimientoConsumeStock(string tipoMovimiento, string tipoDestino)
        {
            return ClasificarImpactoStock(tipoMovimiento, tipoDestino) == ImpactoStock.Salida;
        }

        private static object ObtenerValorRemito(string nroRemito)
        {
            return string.IsNullOrWhiteSpace(nroRemito) ? (object)DBNull.Value : nroRemito.Trim();
        }

        private static string ObtenerNombreTipoMovimiento(NpgsqlConnection conexion, NpgsqlTransaction transaction, int idTipoMovimiento)
        {
            const string queryTipo = "SELECT nombre FROM public.tipo_movimiento WHERE id_tipomovimiento = @id_tipo";
            using (NpgsqlCommand cmdTipo = new NpgsqlCommand(queryTipo, conexion))
            {
                cmdTipo.Transaction = transaction;
                cmdTipo.Parameters.AddWithValue("@id_tipo", idTipoMovimiento);
                object result = cmdTipo.ExecuteScalar();
                return result?.ToString().ToUpperInvariant() ?? "";
            }
        }

        private static void ValidarRemitoDuplicado(NpgsqlConnection conexion, NpgsqlTransaction transaction, string nroRemito, int? idMovimientoAExcluir = null)
        {
            string nroRemitoNormalizado = (nroRemito ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nroRemitoNormalizado))
                return;

            string query = @"
                SELECT COUNT(*)
                FROM public.movimientos
                WHERE nroremito = @nroremito
                  AND (@idExcluir::integer IS NULL OR id_movimiento <> @idExcluir::integer);";

            using (var cmd = new NpgsqlCommand(query, conexion))
            {
                cmd.Transaction = transaction;
                cmd.Parameters.AddWithValue("@nroremito", nroRemitoNormalizado);
                cmd.Parameters.AddWithValue("@idExcluir", (object)idMovimientoAExcluir ?? DBNull.Value);

                int cantidad = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                if (cantidad > 0)
                    throw new Exception($"Ya existe un movimiento con el remito '{nroRemitoNormalizado}'.");
            }
        }

        private static void AplicarDetallesMovimiento(
            NpgsqlConnection conexion,
            NpgsqlTransaction transaction,
            int idMovimiento,
            Movimiento movimiento,
            string nombreTipoMovimiento,
            List<MovimientoMaterial> detalles)
        {
            string queryDetalle = @"
                INSERT INTO public.movimientomaterial (
                    id_movimiento, id_material, cantidad, stock_actual, esrecorte, id_recorte, medida_recorte, material_nombre_eventual, es_compra_eventual, precio_unitario
                ) VALUES (
                    @id_movimiento, @id_material::integer, @cantidad, @stock_actual, @esrecorte, @id_recorte::integer, @medida_recorte::numeric, @material_eventual::varchar, @es_compra_eventual, @precio_unitario
                );";

            foreach (var detalle in detalles)
            {
                if (!detalle.id_material.HasValue && !detalle.EsInsumo && !detalle.EsCompraEventual)
                {
                    throw new Exception($"Error de integridad: El material '{detalle.NombreMaterial}' no tiene ID y no está marcado como Compra Eventual ni Insumo.");
                }

                decimal stockActualDB = 0;
                if (detalle.id_material.HasValue && detalle.id_material > 0)
                {
                    string queryStock = "SELECT stockactual FROM public.materiales WHERE id = @id_material FOR UPDATE";
                    using (NpgsqlCommand cmdStock = new NpgsqlCommand(queryStock, conexion))
                    {
                        cmdStock.Transaction = transaction;
                        cmdStock.Parameters.AddWithValue("@id_material", detalle.id_material.Value);
                        object result = cmdStock.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stockActualDB = Convert.ToDecimal(result);
                        }
                    }
                }

                ImpactoStock impactoStock = ClasificarImpactoStock(nombreTipoMovimiento, movimiento.tipo_destino, registroInternoYaAplicado: true);
                bool sinImpactoStock = impactoStock == ImpactoStock.SinImpacto;

                decimal nuevoStock = stockActualDB;
                if (impactoStock == ImpactoStock.Entrada)
                {
                    nuevoStock += detalle.cantidad;
                }
                else if (impactoStock == ImpactoStock.Salida)
                {
                    nuevoStock -= detalle.cantidad;
                }

                if (!detalle.EsRecorte && !sinImpactoStock && !detalle.EsCompraEventual && detalle.id_material.HasValue && detalle.id_material > 0)
                {
                    if (impactoStock == ImpactoStock.Salida && nuevoStock < 0)
                    {
                        throw new Exception($"Stock insuficiente para el material '{detalle.NombreMaterial}'. Stock actual: {stockActualDB}, cantidad de salida solicitada: {detalle.cantidad}. La operación fue cancelada y no se guardaron cambios en la base de datos.");
                    }

                    string queryUpdateStock = "UPDATE public.materiales SET stockactual = @nuevoStock WHERE id = @idMaterial";
                    using (NpgsqlCommand cmdUpdateStock = new NpgsqlCommand(queryUpdateStock, conexion))
                    {
                        cmdUpdateStock.Transaction = transaction;
                        cmdUpdateStock.Parameters.AddWithValue("@nuevoStock", nuevoStock);
                        cmdUpdateStock.Parameters.AddWithValue("@idMaterial", detalle.id_material.Value);
                        cmdUpdateStock.ExecuteNonQuery();
                    }
                }
                else if (detalle.IdRecorte.HasValue)
                {
                    string queryUpdateRecorte = null;
                    if (impactoStock == ImpactoStock.Entrada)
                    {
                        queryUpdateRecorte = "UPDATE public.recortes SET cantidad = cantidad + @cantidad WHERE id = @idRecorte";
                    }
                    else if (impactoStock == ImpactoStock.Salida)
                    {
                        queryUpdateRecorte = "UPDATE public.recortes SET cantidad = cantidad - @cantidad WHERE id = @idRecorte";
                    }

                    if (queryUpdateRecorte != null)
                    {
                        using (NpgsqlCommand cmdUpdateRecorte = new NpgsqlCommand(queryUpdateRecorte, conexion))
                        {
                            cmdUpdateRecorte.Transaction = transaction;
                            cmdUpdateRecorte.Parameters.AddWithValue("@cantidad", (decimal)detalle.cantidad);
                            cmdUpdateRecorte.Parameters.AddWithValue("@idRecorte", detalle.IdRecorte.Value);
                            cmdUpdateRecorte.ExecuteNonQuery();
                        }
                    }
                }

                using (NpgsqlCommand cmdDetalle = new NpgsqlCommand(queryDetalle, conexion))
                {
                    cmdDetalle.Transaction = transaction;
                    cmdDetalle.Parameters.AddWithValue("@id_movimiento", idMovimiento);
                    cmdDetalle.Parameters.AddWithValue("@id_material", (object)detalle.id_material ?? DBNull.Value);
                    cmdDetalle.Parameters.AddWithValue("@cantidad", detalle.cantidad);
                    cmdDetalle.Parameters.AddWithValue("@stock_actual", nuevoStock);
                    cmdDetalle.Parameters.AddWithValue("@esrecorte", detalle.EsRecorte);
                    cmdDetalle.Parameters.AddWithValue("@id_recorte", (object)detalle.IdRecorte ?? DBNull.Value);
                    cmdDetalle.Parameters.AddWithValue("@medida_recorte", (object)detalle.MedidaRecorte ?? DBNull.Value);
                    cmdDetalle.Parameters.AddWithValue("@material_eventual", (object)detalle.NombreMaterialEventual ?? DBNull.Value);
                    cmdDetalle.Parameters.AddWithValue("@es_compra_eventual", detalle.EsCompraEventual);
                    cmdDetalle.Parameters.AddWithValue("@precio_unitario", detalle.precio_unitario);
                    cmdDetalle.ExecuteNonQuery();
                }
            }
        }

        private static void RevertirImpactoMovimiento(
            NpgsqlConnection conexion,
            NpgsqlTransaction transaction,
            int idMovimiento,
            string tipoMovimiento,
            string tipoDestino)
        {
            ImpactoStock impactoStock = ClasificarImpactoStock(tipoMovimiento, tipoDestino);

            string queryItems = @"SELECT id_material, cantidad, esrecorte, id_recorte
                                  FROM public.movimientomaterial
                                  WHERE id_movimiento = @idMovimiento";

            var itemsMat = new List<(int idMat, decimal qty)>();
            var itemsRec = new List<(int idRec, decimal qty)>();

            using (var cmdItems = new NpgsqlCommand(queryItems, conexion))
            {
                cmdItems.Transaction = transaction;
                cmdItems.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                using (var rdr = cmdItems.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        bool esRecorte = !rdr.IsDBNull(2) && rdr.GetBoolean(2);
                        decimal qty = rdr.IsDBNull(1) ? 0 : rdr.GetDecimal(1);
                        if (esRecorte && !rdr.IsDBNull(3))
                            itemsRec.Add((rdr.GetInt32(3), qty));
                        else if (!esRecorte && !rdr.IsDBNull(0))
                            itemsMat.Add((rdr.GetInt32(0), qty));
                    }
                }
            }

            foreach (var item in itemsMat)
            {
                string q = null;
                if (impactoStock == ImpactoStock.Entrada)
                    q = "UPDATE public.materiales SET stockactual = stockactual - @qty WHERE id = @idMat";
                else if (impactoStock == ImpactoStock.Salida)
                    q = "UPDATE public.materiales SET stockactual = stockactual + @qty WHERE id = @idMat";

                if (q == null) continue;

                using (var cmd = new NpgsqlCommand(q, conexion))
                {
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@qty", item.qty);
                    cmd.Parameters.AddWithValue("@idMat", item.idMat);
                    cmd.ExecuteNonQuery();
                }
            }

            foreach (var rec in itemsRec)
            {
                string q = null;
                if (impactoStock == ImpactoStock.Entrada)
                    q = "UPDATE public.recortes SET cantidad = cantidad - @qty WHERE id = @idRec";
                else if (impactoStock == ImpactoStock.Salida)
                    q = "UPDATE public.recortes SET cantidad = cantidad + @qty WHERE id = @idRec";

                if (q == null) continue;

                using (var cmd = new NpgsqlCommand(q, conexion))
                {
                    cmd.Transaction = transaction;
                    cmd.Parameters.AddWithValue("@qty", rec.qty);
                    cmd.Parameters.AddWithValue("@idRec", rec.idRec);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int GuardarMovimiento(Movimiento movimiento, List<MovimientoMaterial> detalles)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (NpgsqlConnection conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                {
                    conexion.Open();
                }

                using (var transaction = conexion.BeginTransaction())
                {
                    try
                    {
                        ValidarRemitoDuplicado(conexion, transaction, movimiento.nroremito);

                        string queryMovimiento = @"
                            INSERT INTO public.movimientos (
                                fecha, nroremito, id_origen, tipo_origen, id_destino, 
                                tipo_destino, id_tipomovimiento, id_obra, costoflete, 
                                observaciones, precio_total, id_flete
                            ) VALUES (
                                @fecha, @nroremito::varchar, @id_origen, @tipo_origen::varchar, @id_destino, 
                                @tipo_destino::varchar, @id_tipomovimiento, @id_obra::integer, @costoflete, 
                                @observaciones::varchar, @precio_total, @id_flete::integer
                            ) RETURNING id_movimiento;";

                        int idMovimientoGenerado;

                        using (NpgsqlCommand cmd = new NpgsqlCommand(queryMovimiento, conexion))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddWithValue("@fecha", movimiento.fecha);
                            cmd.Parameters.AddWithValue("@nroremito", ObtenerValorRemito(movimiento.nroremito));
                            cmd.Parameters.AddWithValue("@id_origen", movimiento.id_origen);
                            cmd.Parameters.AddWithValue("@tipo_origen", movimiento.tipo_origen ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@id_destino", movimiento.id_destino);
                            cmd.Parameters.AddWithValue("@tipo_destino", movimiento.tipo_destino ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@id_tipomovimiento", movimiento.id_tipomovimiento);
                            cmd.Parameters.AddWithValue("@id_obra", movimiento.id_obra > 0 ? (object)movimiento.id_obra : DBNull.Value);
                            cmd.Parameters.AddWithValue("@costoflete", movimiento.costoflete);
                            cmd.Parameters.AddWithValue("@observaciones", movimiento.observaciones ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@precio_total", movimiento.precio_total);
                            cmd.Parameters.AddWithValue("@id_flete", (object)movimiento.id_flete ?? DBNull.Value);

                            idMovimientoGenerado = (int)cmd.ExecuteScalar();
                        }

                        string nombreTipoMovimiento = ObtenerNombreTipoMovimiento(conexion, transaction, movimiento.id_tipomovimiento);
                        AplicarDetallesMovimiento(conexion, transaction, idMovimientoGenerado, movimiento, nombreTipoMovimiento, detalles);

                        transaction.Commit();
                        return idMovimientoGenerado;
                    }
                    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "movimientos_nroremito_key")
                    {
                        transaction.Rollback();
                        string remito = (movimiento.nroremito ?? "").Trim();
                        throw new Exception($"Ya existe un movimiento con el remito '{remito}'.");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public int ActualizarMovimiento(int idMovimiento, Movimiento movimiento, List<MovimientoMaterial> detalles)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (NpgsqlConnection conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                {
                    conexion.Open();
                }

                using (var transaction = conexion.BeginTransaction())
                {
                    try
                    {
                        string tipoDestinoAnterior = null;
                        int idTipoAnterior = 0;

                        const string queryActual = @"
                            SELECT tipo_destino, id_tipomovimiento
                            FROM public.movimientos
                            WHERE id_movimiento = @idMovimiento;";

                        using (var cmdActual = new NpgsqlCommand(queryActual, conexion))
                        {
                            cmdActual.Transaction = transaction;
                            cmdActual.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                            using (var reader = cmdActual.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new Exception("El movimiento que intentás actualizar no existe.");

                                tipoDestinoAnterior = reader.IsDBNull(0) ? null : reader.GetString(0);
                                idTipoAnterior = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            }
                        }

                        ValidarRemitoDuplicado(conexion, transaction, movimiento.nroremito, idMovimiento);

                        string nombreTipoAnterior = ObtenerNombreTipoMovimiento(conexion, transaction, idTipoAnterior);
                        RevertirImpactoMovimiento(conexion, transaction, idMovimiento, nombreTipoAnterior, tipoDestinoAnterior);

                        using (var cmdDeleteDetalles = new NpgsqlCommand("DELETE FROM public.movimientomaterial WHERE id_movimiento = @idMovimiento", conexion))
                        {
                            cmdDeleteDetalles.Transaction = transaction;
                            cmdDeleteDetalles.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                            cmdDeleteDetalles.ExecuteNonQuery();
                        }

                        const string queryUpdateMovimiento = @"
                            UPDATE public.movimientos
                            SET fecha = @fecha,
                                nroremito = @nroremito::varchar,
                                id_origen = @id_origen,
                                tipo_origen = @tipo_origen::varchar,
                                id_destino = @id_destino,
                                tipo_destino = @tipo_destino::varchar,
                                id_tipomovimiento = @id_tipomovimiento,
                                id_obra = @id_obra::integer,
                                costoflete = @costoflete,
                                observaciones = @observaciones::varchar,
                                precio_total = @precio_total,
                                id_flete = @id_flete::integer
                            WHERE id_movimiento = @idMovimiento;";

                        using (var cmdUpdateMovimiento = new NpgsqlCommand(queryUpdateMovimiento, conexion))
                        {
                            cmdUpdateMovimiento.Transaction = transaction;
                            cmdUpdateMovimiento.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@fecha", movimiento.fecha);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@nroremito", ObtenerValorRemito(movimiento.nroremito));
                            cmdUpdateMovimiento.Parameters.AddWithValue("@id_origen", movimiento.id_origen);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@tipo_origen", movimiento.tipo_origen ?? (object)DBNull.Value);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@id_destino", movimiento.id_destino);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@tipo_destino", movimiento.tipo_destino ?? (object)DBNull.Value);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@id_tipomovimiento", movimiento.id_tipomovimiento);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@id_obra", movimiento.id_obra > 0 ? (object)movimiento.id_obra : DBNull.Value);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@costoflete", movimiento.costoflete);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@observaciones", movimiento.observaciones ?? (object)DBNull.Value);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@precio_total", movimiento.precio_total);
                            cmdUpdateMovimiento.Parameters.AddWithValue("@id_flete", (object)movimiento.id_flete ?? DBNull.Value);
                            cmdUpdateMovimiento.ExecuteNonQuery();
                        }

                        string nombreTipoNuevo = ObtenerNombreTipoMovimiento(conexion, transaction, movimiento.id_tipomovimiento);
                        AplicarDetallesMovimiento(conexion, transaction, idMovimiento, movimiento, nombreTipoNuevo, detalles);

                        transaction.Commit();
                        return idMovimiento;
                    }
                    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "movimientos_nroremito_key")
                    {
                        transaction.Rollback();
                        string remito = (movimiento.nroremito ?? "").Trim();
                        throw new Exception($"Ya existe un movimiento con el remito '{remito}'.");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public decimal ActualizarPrecioUnitarioCompraEventual(int idMovimientoMaterial, decimal precioUnitario)
        {
            if (precioUnitario < 0)
                throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario no puede ser negativo.");

            ConexionDB conexionDB = new ConexionDB();
            using (NpgsqlConnection conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                using (NpgsqlTransaction transaction = conexion.BeginTransaction())
                {
                    const string actualizarDetalle = @"
                        UPDATE public.movimientomaterial
                        SET precio_unitario = @precio_unitario
                        WHERE id_movimientomaterial = @id_detalle
                          AND es_compra_eventual = true
                        RETURNING id_movimiento;";

                    int idMovimiento;
                    using (NpgsqlCommand cmd = new NpgsqlCommand(actualizarDetalle, conexion, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id_detalle", idMovimientoMaterial);
                        cmd.Parameters.AddWithValue("@precio_unitario", precioUnitario);
                        object resultado = cmd.ExecuteScalar();
                        if (resultado == null)
                            throw new InvalidOperationException("Solo se puede modificar el precio de un detalle eventual.");

                        idMovimiento = Convert.ToInt32(resultado);
                    }

                    const string recalcularTotal = @"
                        UPDATE public.movimientos
                        SET precio_total = (
                            SELECT COALESCE(SUM(
                                mm.cantidad *
                                CASE
                                    WHEN mm.es_compra_eventual = true THEN COALESCE(mm.precio_unitario, 0)
                                    ELSE COALESCE(mat.preciounitario, 0)
                                END
                            ), 0)
                            FROM public.movimientomaterial mm
                            LEFT JOIN public.materiales mat ON mm.id_material = mat.id
                            WHERE mm.id_movimiento = @id_movimiento
                        )
                        WHERE id_movimiento = @id_movimiento
                        RETURNING precio_total;";

                    decimal precioTotal;
                    using (NpgsqlCommand cmd = new NpgsqlCommand(recalcularTotal, conexion, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id_movimiento", idMovimiento);
                        precioTotal = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    transaction.Commit();
                    return precioTotal;
                }
            }
        }

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

                            ImpactoStock impactoStock = ClasificarImpactoStock(item.tipo_movimiento, tipoDestino);

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
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                using (var transaction = conexion.BeginTransaction())
                {
                    try
                    {
                        string tipoDestino = null;
                        string queryTipoDestino = "SELECT tipo_destino FROM public.movimientos WHERE id_movimiento = @idMovimiento";
                        using (var cmdTipoDestino = new NpgsqlCommand(queryTipoDestino, conexion))
                        {
                            cmdTipoDestino.Transaction = transaction;
                            cmdTipoDestino.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                            object result = cmdTipoDestino.ExecuteScalar();
                            if (result != null && result != DBNull.Value) tipoDestino = result.ToString();
                        }

                        RevertirImpactoMovimiento(conexion, transaction, idMovimiento, tipoMovimiento, tipoDestino);

                        // 2. Eliminar detalle y movimiento
                        string deleteDetalleObj = "DELETE FROM movimientomaterial WHERE id_movimiento = @idMovimiento";
                        using (var cmdDelDet = new NpgsqlCommand(deleteDetalleObj, conexion))
                        {
                            cmdDelDet.Transaction = transaction;
                            cmdDelDet.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                            cmdDelDet.ExecuteNonQuery();
                        }

                        string deleteMovObj = "DELETE FROM movimientos WHERE id_movimiento = @idMovimiento";
                        using (var cmdDelMov = new NpgsqlCommand(deleteMovObj, conexion))
                        {
                            cmdDelMov.Transaction = transaction;
                            cmdDelMov.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                            cmdDelMov.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<HistorialMovimiento> ObtenerHistorial(DateTime? fechaInicio = null, DateTime? fechaFin = null, string tipoMovimientoFiltro = null)
        {
            List<HistorialMovimiento> historial = new List<HistorialMovimiento>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT
                        m.id_movimiento::integer,
                        mm.id_material::integer,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        COALESCE(mat.nombre, mm.material_nombre_eventual) as material_nombre,
                        mm.cantidad,
                        CASE 
                            WHEN m.tipo_origen = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_origen)
                            WHEN m.tipo_origen = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_origen)
                            WHEN m.tipo_origen = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_origen)
                            ELSE '-'
                        END as origen_nombre,
                        CASE 
                            WHEN m.tipo_destino = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_destino)
                            WHEN m.tipo_destino = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_destino)
                            WHEN m.tipo_destino = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_destino)
                            ELSE '-'
                        END as destino_nombre,
                        m.observaciones,
                        COALESCE(mm.esrecorte, false) as esrecorte,
                        mm.medida_recorte,
                        false as esinsumo,
                        COALESCE(mm.es_compra_eventual, false) as es_compra_eventual,
                        CASE
                            WHEN COALESCE(mm.es_compra_eventual, false) THEN COALESCE(mm.precio_unitario, 0)
                            ELSE COALESCE(mat.preciounitario, 0)
                        END as precio_unitario,
                        COALESCE(m.precio_total, 0) as precio_total,
                        mm.id_movimientomaterial::integer
                    FROM public.movimientos m
                    INNER JOIN public.movimientomaterial mm ON m.id_movimiento = mm.id_movimiento
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    LEFT JOIN public.materiales mat ON mm.id_material = mat.id
                    WHERE 1=1
                ";

                if (fechaInicio.HasValue)
                    query += " AND m.fecha >= @fechaInicio";
                if (fechaFin.HasValue)
                    query += " AND m.fecha <= @fechaFin";
                if (!string.IsNullOrEmpty(tipoMovimientoFiltro) && tipoMovimientoFiltro != "TODOS")
                    query += " AND tm.nombre = @tipoMov";

                // UNION con Insumos
                query += @"
                    UNION ALL
                    SELECT
                        m.id_movimiento::integer,
                        i.id_insumo::integer,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        i.nombre as material_nombre,
                        1::integer as cantidad,
                        im.obra_origen as origen_nombre,
                        im.obra_destino as destino_nombre,
                        m.observaciones,
                        false as esrecorte,
                        NULL as medida_recorte,
                        true as esinsumo,
                        false as es_compra_eventual,
                        0::numeric as precio_unitario,
                        0::numeric as precio_total,
                        NULL::integer as id_movimientomaterial
                    FROM public.movimientos m
                    INNER JOIN public.insumo_movimientos im ON m.id_movimiento = im.id_movimiento
                    INNER JOIN public.insumos i ON im.id_insumo = i.id_insumo
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    WHERE 1=1
                ";

                if (fechaInicio.HasValue)
                    query += " AND m.fecha >= @fechaInicio";
                if (fechaFin.HasValue)
                    query += " AND m.fecha <= @fechaFin";
                if (!string.IsNullOrEmpty(tipoMovimientoFiltro) && tipoMovimientoFiltro != "TODOS")
                    query += " AND tm.nombre = @tipoMov";

                query += " ORDER BY 3 DESC, 1 DESC"; // Ordenar por fecha (3) y luego id_movimiento (1)

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (fechaInicio.HasValue) cmd.Parameters.AddWithValue("@fechaInicio", fechaInicio.Value);
                    if (fechaFin.HasValue) cmd.Parameters.AddWithValue("@fechaFin", fechaFin.Value);
                    if (!string.IsNullOrEmpty(tipoMovimientoFiltro) && tipoMovimientoFiltro != "TODOS")
                        cmd.Parameters.AddWithValue("@tipoMov", tipoMovimientoFiltro);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new HistorialMovimiento();
                            item.id_movimiento = reader.GetInt32(0);
                            item.id_material = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                            item.fecha = reader.GetDateTime(2);
                            item.nroremito = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            item.tipo_movimiento = reader.IsDBNull(4) ? "-" : reader.GetString(4);
                            item.material = reader.GetString(5);
                            item.cantidad = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6);
                            item.origen = reader.IsDBNull(7) ? "-" : reader.GetString(7);
                            item.destino = reader.IsDBNull(8) ? "-" : reader.GetString(8);
                            item.observaciones = reader.IsDBNull(9) ? "" : reader.GetString(9);
                            item.esrecorte = reader.GetBoolean(10);
                            item.medida_recorte = reader.IsDBNull(11) ? (decimal?)null : reader.GetDecimal(11);
                            item.esinsumo = reader.GetBoolean(12);
                            item.es_compra_eventual = reader.GetBoolean(13);
                            item.precio_unitario = reader.IsDBNull(14) ? 0 : reader.GetDecimal(14);
                            item.precio_total = reader.IsDBNull(15) ? 0 : reader.GetDecimal(15);
                            item.id_movimientomaterial = reader.IsDBNull(16) ? (int?)null : reader.GetInt32(16);
                            historial.Add(item);
                        }
                    }
                }
            }
            return historial;
        }

        public Movimiento ObtenerMovimientoPorId(int id_movimiento)
        {
            Movimiento mov = null;
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                string query = "SELECT * FROM public.movimientos WHERE id_movimiento = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id_movimiento);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mov = new Movimiento
                            {
                                id_movimiento = reader.IsDBNull(reader.GetOrdinal("id_movimiento")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_movimiento")),
                                fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? DateTime.Today : reader.GetDateTime(reader.GetOrdinal("fecha")),
                                nroremito = reader.IsDBNull(reader.GetOrdinal("nroremito")) ? "" : reader.GetString(reader.GetOrdinal("nroremito")),
                                id_origen = reader.IsDBNull(reader.GetOrdinal("id_origen")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_origen")),
                                tipo_origen = reader.IsDBNull(reader.GetOrdinal("tipo_origen")) ? "" : reader.GetString(reader.GetOrdinal("tipo_origen")),
                                id_destino = reader.IsDBNull(reader.GetOrdinal("id_destino")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_destino")),
                                tipo_destino = reader.IsDBNull(reader.GetOrdinal("tipo_destino")) ? "" : reader.GetString(reader.GetOrdinal("tipo_destino")),
                                id_tipomovimiento = reader.IsDBNull(reader.GetOrdinal("id_tipomovimiento")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_tipomovimiento")),
                                id_obra = reader.IsDBNull(reader.GetOrdinal("id_obra")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_obra")),
                                costoflete = reader.IsDBNull(reader.GetOrdinal("costoflete")) ? 0 : reader.GetDecimal(reader.GetOrdinal("costoflete")),
                                observaciones = reader.IsDBNull(reader.GetOrdinal("observaciones")) ? "" : reader.GetString(reader.GetOrdinal("observaciones")),
                                precio_total = reader.IsDBNull(reader.GetOrdinal("precio_total")) ? 0 : reader.GetDecimal(reader.GetOrdinal("precio_total")),
                                id_flete = reader.IsDBNull(reader.GetOrdinal("id_flete")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("id_flete"))
                            };
                        }
                    }
                }
            }
            return mov;
        }

        public List<MovimientoMaterial> ObtenerDetallesPorMovimiento(int id_movimiento)
        {
            List<MovimientoMaterial> lista = new List<MovimientoMaterial>();
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                string query = @"SELECT mm.*, COALESCE(m.nombre, mm.material_nombre_eventual) as nombre_resuelto 
                                 FROM public.movimientomaterial mm 
                                 LEFT JOIN public.materiales m ON mm.id_material = m.id 
                                 WHERE mm.id_movimiento = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id_movimiento);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new MovimientoMaterial
                            {
                                id_material = reader.IsDBNull(reader.GetOrdinal("id_material")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("id_material")),
                                cantidad = reader.IsDBNull(reader.GetOrdinal("cantidad")) ? 0 : reader.GetDecimal(reader.GetOrdinal("cantidad")),
                                EsRecorte = reader.IsDBNull(reader.GetOrdinal("esrecorte")) ? false : reader.GetBoolean(reader.GetOrdinal("esrecorte")),
                                NombreMaterial = reader.GetString(reader.GetOrdinal("nombre_resuelto")),
                                NombreMaterialEventual = reader.IsDBNull(reader.GetOrdinal("material_nombre_eventual")) ? null : reader.GetString(reader.GetOrdinal("material_nombre_eventual")),
                                IdRecorte = reader.IsDBNull(reader.GetOrdinal("id_recorte")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("id_recorte")),
                                MedidaRecorte = reader.IsDBNull(reader.GetOrdinal("medida_recorte")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("medida_recorte")),
                                EsCompraEventual = reader.IsDBNull(reader.GetOrdinal("es_compra_eventual")) ? false : reader.GetBoolean(reader.GetOrdinal("es_compra_eventual")),
                                precio_unitario = reader.IsDBNull(reader.GetOrdinal("precio_unitario")) ? 0 : reader.GetDecimal(reader.GetOrdinal("precio_unitario"))
                            };
                            
                            lista.Add(item);
                        }
                    }
                }
            }
            return lista;
        }

        public string GenerarProximoRemito()
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                string query = "SELECT COALESCE(MAX(nroremito::bigint), 0) + 1 FROM public.movimientos WHERE nroremito ~ '^[0-9]+$'";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    object result = cmd.ExecuteScalar();
                    long next = result != null && result != DBNull.Value ? Convert.ToInt64(result) : 1;
                    return next.ToString();
                }
            }
        }

        /// <summary>
        /// Busca el id_movimiento de Registro Interno (tipo 11) del día de hoy, si existe.
        /// Devuelve -1 si no hay ninguno.
        /// </summary>
        public int BuscarRemitoInternoDiario(DateTime fecha)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                string query = @"
                    SELECT id_movimiento FROM public.movimientos
                    WHERE id_tipomovimiento = 11
                      AND DATE(fecha) = @fecha
                    ORDER BY id_movimiento DESC
                    LIMIT 1";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
                }
            }
        }

        /// <summary>
        /// Si el material ya tiene una fila en este movimiento, acumula el delta (signed) en cantidad
        /// y actualiza stock_actual al valor más reciente.
        /// Si no existe, inserta una fila nueva.
        /// delta positivo = aumento de stock; delta negativo = baja de stock.
        /// </summary>
        public void ActualizarOAgregarDetalle(int idMovimiento, int idMaterial, int signedDelta, decimal stockActual)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                // Verificar si ya existe una fila para este material en este movimiento
                string queryCheck = @"
                    SELECT id_movimientomaterial, cantidad 
                    FROM public.movimientomaterial 
                    WHERE id_movimiento = @idMovimiento AND id_material = @idMaterial
                    LIMIT 1";

                int? idExistente = null;
                decimal cantidadExistente = 0;

                using (var cmdCheck = new NpgsqlCommand(queryCheck, conexion))
                {
                    cmdCheck.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                    cmdCheck.Parameters.AddWithValue("@idMaterial", idMaterial);
                    using (var reader = cmdCheck.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idExistente = reader.GetInt32(0);
                            cantidadExistente = reader.GetDecimal(1);
                        }
                    }
                }

                if (idExistente.HasValue)
                {
                    // Ya existe → acumular delta en cantidad y actualizar stock_actual
                    decimal nuevaCantidad = cantidadExistente + signedDelta;
                    string queryUpdate = @"
                        UPDATE public.movimientomaterial 
                        SET cantidad = @cantidad, stock_actual = @stockActual
                        WHERE id_movimientomaterial = @id";
                    using (var cmdUpdate = new NpgsqlCommand(queryUpdate, conexion))
                    {
                        cmdUpdate.Parameters.AddWithValue("@cantidad", nuevaCantidad);
                        cmdUpdate.Parameters.AddWithValue("@stockActual", stockActual);
                        cmdUpdate.Parameters.AddWithValue("@id", idExistente.Value);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
                else
                {
                    // No existe → insertar nuevo
                    string queryInsert = @"
                        INSERT INTO public.movimientomaterial
                            (id_movimiento, id_material, cantidad, stock_actual, esrecorte, id_recorte, medida_recorte, es_compra_eventual)
                        VALUES
                            (@idMovimiento, @idMaterial, @cantidad, @stockActual, false, NULL, NULL, false)";
                    using (var cmdInsert = new NpgsqlCommand(queryInsert, conexion))
                    {
                        cmdInsert.Parameters.AddWithValue("@idMovimiento", idMovimiento);
                        cmdInsert.Parameters.AddWithValue("@idMaterial", idMaterial);
                        cmdInsert.Parameters.AddWithValue("@cantidad", signedDelta);
                        cmdInsert.Parameters.AddWithValue("@stockActual", stockActual);
                        cmdInsert.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
