using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class OrdenCompraDatos
    {
        public List<OrdenCompra> ObtenerOrdenes(string estado = null, bool soloAbiertasGalpon = false, bool soloHistorial = false, bool excluirCompletas = false)
        {
            List<OrdenCompra> lista = new List<OrdenCompra>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                string query = @"
                    SELECT
                        oc.id_orden_compra,
                        oc.fecha,
                        oc.nro_orden,
                        COALESCE(oc.nro_remito, '') AS nro_remito,
                        COALESCE(oc.id_proveedor, 0) AS id_proveedor,
                        COALESCE(p.nombre, '-') AS proveedor,
                        COALESCE(oc.id_destino, 0) AS id_destino,
                        COALESCE(u.nombre, '-') AS destino,
                        COALESCE(oc.costoflete, 0) AS costoflete,
                        COALESCE(oc.observaciones, '') AS observaciones,
                        oc.estado,
                        COALESCE(oc.usuario_creacion, '') AS usuario_creacion,
                        COALESCE(oc.usuario_revision, '') AS usuario_revision,
                        oc.fecha_revision,
                        COALESCE(oc.observacion_revision, '') AS observacion_revision,
                        COUNT(d.id_detalle)::integer AS cantidad_items,
                        COALESCE(SUM(d.cantidad_pedida), 0) AS total_pedida,
                        COALESCE(SUM(d.cantidad_recibida), 0) AS total_recibida
                    FROM public.ordenes_compra oc
                    LEFT JOIN public.proveedores p ON p.id_proveedores = oc.id_proveedor
                    LEFT JOIN public.ubicacion u ON u.id_ubicacion = oc.id_destino
                    LEFT JOIN public.ordenes_compra_detalle d ON d.id_orden_compra = oc.id_orden_compra
                    WHERE 1 = 1";

                if (!string.IsNullOrWhiteSpace(estado))
                    query += " AND oc.estado = @estado";

                if (soloAbiertasGalpon)
                    query += " AND oc.estado IN (@pendiente, @parcial)";

                if (soloHistorial)
                    query += " AND oc.estado IN (@historial_parcial, @historial_completa)";

                if (excluirCompletas)
                    query += " AND oc.estado <> @excluir_completa";

                query += @"
                    GROUP BY
                        oc.id_orden_compra,
                        oc.fecha,
                        oc.nro_orden,
                        oc.nro_remito,
                        oc.id_proveedor,
                        p.nombre,
                        oc.id_destino,
                        u.nombre,
                        oc.costoflete,
                        oc.observaciones,
                        oc.estado,
                        oc.usuario_creacion,
                        oc.usuario_revision,
                        oc.fecha_revision,
                        oc.observacion_revision
                    ORDER BY oc.fecha DESC, oc.id_orden_compra DESC;";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (!string.IsNullOrWhiteSpace(estado))
                        cmd.Parameters.AddWithValue("@estado", estado);

                    if (soloAbiertasGalpon)
                    {
                        cmd.Parameters.AddWithValue("@pendiente", OrdenCompraEstados.Pendiente);
                        cmd.Parameters.AddWithValue("@parcial", OrdenCompraEstados.RecibidaParcial);
                    }

                    if (soloHistorial)
                    {
                        cmd.Parameters.AddWithValue("@historial_parcial", OrdenCompraEstados.RecibidaParcial);
                        cmd.Parameters.AddWithValue("@historial_completa", OrdenCompraEstados.RecibidaCompleta);
                    }

                    if (excluirCompletas)
                        cmd.Parameters.AddWithValue("@excluir_completa", OrdenCompraEstados.RecibidaCompleta);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(MapearOrdenResumen(reader));
                        }
                    }
                }
            }

            return lista;
        }

        public OrdenCompra ObtenerOrdenPorId(int idOrden)
        {
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                string queryCabecera = @"
                    SELECT
                        oc.id_orden_compra,
                        oc.fecha,
                        oc.nro_orden,
                        COALESCE(oc.nro_remito, '') AS nro_remito,
                        COALESCE(oc.id_proveedor, 0) AS id_proveedor,
                        COALESCE(p.nombre, '-') AS proveedor,
                        COALESCE(oc.id_destino, 0) AS id_destino,
                        COALESCE(u.nombre, '-') AS destino,
                        COALESCE(oc.costoflete, 0) AS costoflete,
                        COALESCE(oc.observaciones, '') AS observaciones,
                        oc.estado,
                        COALESCE(oc.usuario_creacion, '') AS usuario_creacion,
                        COALESCE(oc.usuario_revision, '') AS usuario_revision,
                        oc.fecha_revision,
                        COALESCE(oc.observacion_revision, '') AS observacion_revision
                    FROM public.ordenes_compra oc
                    LEFT JOIN public.proveedores p ON p.id_proveedores = oc.id_proveedor
                    LEFT JOIN public.ubicacion u ON u.id_ubicacion = oc.id_destino
                    WHERE oc.id_orden_compra = @id;";

                OrdenCompra orden = null;
                using (var cmd = new NpgsqlCommand(queryCabecera, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", idOrden);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            orden = MapearOrdenCabecera(reader);
                    }
                }

                if (orden == null)
                    return null;

                string queryDetalles = @"
                    SELECT
                        d.id_detalle,
                        d.id_orden_compra,
                        d.id_material,
                        m.nombre AS material_nombre,
                        d.cantidad_pedida,
                        d.cantidad_recibida,
                        d.cantidad_faltante,
                        COALESCE(d.observacion_item, '') AS observacion_item,
                        COALESCE(d.stock_al_crear, 0) AS stock_al_crear,
                        d.stock_despues_recepcion
                    FROM public.ordenes_compra_detalle d
                    INNER JOIN public.materiales m ON m.id = d.id_material
                    WHERE d.id_orden_compra = @id
                    ORDER BY m.nombre;";

                using (var cmd = new NpgsqlCommand(queryDetalles, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", idOrden);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detalle = MapearDetalle(reader);
                            detalle.cantidad_recibida_anterior = detalle.cantidad_recibida;
                            orden.Detalles.Add(detalle);
                        }
                    }
                }

                orden.cantidad_items = orden.Detalles.Count;
                orden.total_cantidad_pedida = orden.Detalles.Sum(x => x.cantidad_pedida);
                orden.total_cantidad_recibida = orden.Detalles.Sum(x => x.cantidad_recibida);
                return orden;
            }
        }

        public int GuardarOrden(OrdenCompra orden)
        {
            if (orden == null)
                throw new ArgumentNullException("orden");
            if (string.IsNullOrWhiteSpace(orden.nro_orden))
                throw new Exception("El número de orden es obligatorio.");
            if (orden.id_proveedor <= 0)
                throw new Exception("Debe seleccionar un proveedor.");
            if (orden.Detalles == null || orden.Detalles.Count == 0)
                throw new Exception("Debe cargar al menos un material.");

            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        ValidarNroOrdenUnico(orden.nro_orden, orden.id_orden_compra, conexion, tx);
                        int idDestinoGalpon = ObtenerIdDestinoGalpon(conexion, tx);

                        int idOrden;
                        if (orden.id_orden_compra > 0)
                        {
                            string estadoActual = ObtenerEstadoActual(orden.id_orden_compra, conexion, tx);
                            if (!OrdenCompraEstados.EsEditable(estadoActual))
                                throw new Exception("Solo se pueden editar órdenes en estado Pendiente.");

                            string updateOrden = @"
                                UPDATE public.ordenes_compra
                                SET fecha = @fecha,
                                    nro_orden = @nro_orden,
                                    nro_remito = @nro_remito,
                                    id_proveedor = @id_proveedor,
                                    id_destino = @id_destino,
                                    tipo_destino = 'UBICACION',
                                    costoflete = @costoflete,
                                    observaciones = @observaciones,
                                    updated_at = NOW()
                                WHERE id_orden_compra = @id_orden;";

                            using (var cmd = new NpgsqlCommand(updateOrden, conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@fecha", orden.fecha.Date);
                                cmd.Parameters.AddWithValue("@nro_orden", orden.nro_orden.Trim());
                                cmd.Parameters.AddWithValue("@nro_remito", string.IsNullOrWhiteSpace(orden.nro_remito) ? (object)DBNull.Value : orden.nro_remito.Trim());
                                cmd.Parameters.AddWithValue("@id_proveedor", orden.id_proveedor);
                                cmd.Parameters.AddWithValue("@id_destino", idDestinoGalpon);
                                cmd.Parameters.AddWithValue("@costoflete", orden.costoflete);
                                cmd.Parameters.AddWithValue("@observaciones", string.IsNullOrWhiteSpace(orden.observaciones) ? (object)DBNull.Value : orden.observaciones.Trim());
                                cmd.Parameters.AddWithValue("@id_orden", orden.id_orden_compra);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new NpgsqlCommand("DELETE FROM public.ordenes_compra_detalle WHERE id_orden_compra = @id;", conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@id", orden.id_orden_compra);
                                cmd.ExecuteNonQuery();
                            }

                            idOrden = orden.id_orden_compra;
                        }
                        else
                        {
                            string insertOrden = @"
                                INSERT INTO public.ordenes_compra
                                (
                                    fecha,
                                    nro_orden,
                                    nro_remito,
                                    id_proveedor,
                                    id_destino,
                                    tipo_destino,
                                    costoflete,
                                    observaciones,
                                    estado,
                                    usuario_creacion
                                )
                                VALUES
                                (
                                    @fecha,
                                    @nro_orden,
                                    @nro_remito,
                                    @id_proveedor,
                                    @id_destino,
                                    'UBICACION',
                                    @costoflete,
                                    @observaciones,
                                    @estado,
                                    @usuario_creacion
                                )
                                RETURNING id_orden_compra;";

                            using (var cmd = new NpgsqlCommand(insertOrden, conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@fecha", orden.fecha.Date);
                                cmd.Parameters.AddWithValue("@nro_orden", orden.nro_orden.Trim());
                                cmd.Parameters.AddWithValue("@nro_remito", string.IsNullOrWhiteSpace(orden.nro_remito) ? (object)DBNull.Value : orden.nro_remito.Trim());
                                cmd.Parameters.AddWithValue("@id_proveedor", orden.id_proveedor);
                                cmd.Parameters.AddWithValue("@id_destino", idDestinoGalpon);
                                cmd.Parameters.AddWithValue("@costoflete", orden.costoflete);
                                cmd.Parameters.AddWithValue("@observaciones", string.IsNullOrWhiteSpace(orden.observaciones) ? (object)DBNull.Value : orden.observaciones.Trim());
                                cmd.Parameters.AddWithValue("@estado", OrdenCompraEstados.Pendiente);
                                cmd.Parameters.AddWithValue("@usuario_creacion", string.IsNullOrWhiteSpace(orden.usuario_creacion) ? (object)DBNull.Value : orden.usuario_creacion.Trim());
                                idOrden = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }

                        foreach (var detalle in NormalizarDetalles(orden.Detalles))
                        {
                            decimal stockAlCrear = ObtenerStockMaterial(detalle.id_material, conexion, tx);
                            string insertDetalle = @"
                                INSERT INTO public.ordenes_compra_detalle
                                (
                                    id_orden_compra,
                                    id_material,
                                    cantidad_pedida,
                                    cantidad_recibida,
                                    cantidad_faltante,
                                    observacion_item,
                                    stock_al_crear
                                )
                                VALUES
                                (
                                    @id_orden_compra,
                                    @id_material,
                                    @cantidad_pedida,
                                    0,
                                    @cantidad_pedida,
                                    @observacion_item,
                                    @stock_al_crear
                                );";

                            using (var cmd = new NpgsqlCommand(insertDetalle, conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@id_orden_compra", idOrden);
                                cmd.Parameters.AddWithValue("@id_material", detalle.id_material);
                                cmd.Parameters.AddWithValue("@cantidad_pedida", detalle.cantidad_pedida);
                                cmd.Parameters.AddWithValue("@observacion_item", string.IsNullOrWhiteSpace(detalle.observacion_item) ? (object)DBNull.Value : detalle.observacion_item.Trim());
                                cmd.Parameters.AddWithValue("@stock_al_crear", stockAlCrear);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        return idOrden;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void EliminarOrden(int idOrden)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        string estadoActual = ObtenerEstadoActual(idOrden, conexion, tx);
                        if (!OrdenCompraEstados.EsEditable(estadoActual))
                            throw new Exception("Solo se pueden eliminar órdenes en estado Pendiente.");

                        using (var cmd = new NpgsqlCommand("DELETE FROM public.ordenes_compra_detalle WHERE id_orden_compra = @id;", conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id", idOrden);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new NpgsqlCommand("DELETE FROM public.ordenes_compra WHERE id_orden_compra = @id;", conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id", idOrden);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public int ActualizarCabeceraOrden(OrdenCompra orden)
        {
            if (orden == null)
                throw new ArgumentNullException("orden");
            if (orden.id_orden_compra <= 0)
                throw new Exception("La orden indicada no existe.");
            if (string.IsNullOrWhiteSpace(orden.nro_orden))
                throw new Exception("El número de orden es obligatorio.");
            if (orden.id_proveedor <= 0)
                throw new Exception("Debe seleccionar un proveedor.");

            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        string estadoActual = ObtenerEstadoActual(orden.id_orden_compra, conexion, tx, true);
                        if (!OrdenCompraEstados.PermiteEditarCabecera(estadoActual))
                            throw new Exception("La orden seleccionada no permite edición.");

                        ValidarNroOrdenUnico(orden.nro_orden, orden.id_orden_compra, conexion, tx);
                        int idDestinoGalpon = ObtenerIdDestinoGalpon(conexion, tx);

                        string updateOrden = @"
                            UPDATE public.ordenes_compra
                            SET fecha = @fecha,
                                nro_orden = @nro_orden,
                                nro_remito = @nro_remito,
                                id_proveedor = @id_proveedor,
                                id_destino = @id_destino,
                                tipo_destino = 'UBICACION',
                                costoflete = @costoflete,
                                observaciones = @observaciones,
                                updated_at = NOW()
                            WHERE id_orden_compra = @id_orden;";

                        using (var cmd = new NpgsqlCommand(updateOrden, conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@fecha", orden.fecha.Date);
                            cmd.Parameters.AddWithValue("@nro_orden", orden.nro_orden.Trim());
                            cmd.Parameters.AddWithValue("@nro_remito", string.IsNullOrWhiteSpace(orden.nro_remito) ? (object)DBNull.Value : orden.nro_remito.Trim());
                            cmd.Parameters.AddWithValue("@id_proveedor", orden.id_proveedor);
                            cmd.Parameters.AddWithValue("@id_destino", idDestinoGalpon);
                            cmd.Parameters.AddWithValue("@costoflete", orden.costoflete);
                            cmd.Parameters.AddWithValue("@observaciones", string.IsNullOrWhiteSpace(orden.observaciones) ? (object)DBNull.Value : orden.observaciones.Trim());
                            cmd.Parameters.AddWithValue("@id_orden", orden.id_orden_compra);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return orden.id_orden_compra;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void CambiarMaterialDetalle(int idOrden, int idDetalle, int idMaterialNuevo)
        {
            if (idOrden <= 0)
                throw new Exception("La orden indicada no existe.");
            if (idDetalle <= 0)
                throw new Exception("El detalle indicado no existe.");
            if (idMaterialNuevo <= 0)
                throw new Exception("Debe seleccionar un material válido.");

            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        string queryDetalle = @"
                            SELECT
                                d.id_orden_compra,
                                d.id_material,
                                d.cantidad_recibida,
                                d.stock_despues_recepcion,
                                oc.estado
                            FROM public.ordenes_compra_detalle d
                            INNER JOIN public.ordenes_compra oc ON oc.id_orden_compra = d.id_orden_compra
                            WHERE d.id_detalle = @id_detalle
                              AND d.id_orden_compra = @id_orden
                            FOR UPDATE OF d, oc;";

                        int idMaterialAnterior;
                        decimal cantidadRecibida;
                        bool teniaSnapshotRecepcion;
                        string estadoOrden;

                        using (var cmd = new NpgsqlCommand(queryDetalle, conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id_detalle", idDetalle);
                            cmd.Parameters.AddWithValue("@id_orden", idOrden);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new Exception("No se encontró el material dentro de la orden.");

                                idMaterialAnterior = reader.GetInt32(1);
                                cantidadRecibida = Convert.ToDecimal(reader.GetValue(2));
                                teniaSnapshotRecepcion = !reader.IsDBNull(3);
                                estadoOrden = reader.GetString(4);
                            }
                        }

                        if (!OrdenCompraEstados.PermiteEditarCabecera(estadoOrden))
                            throw new Exception("La orden seleccionada no permite edición.");

                        if (idMaterialAnterior == idMaterialNuevo)
                            return;

                        string queryDuplicado = @"
                            SELECT COUNT(1)
                            FROM public.ordenes_compra_detalle
                            WHERE id_orden_compra = @id_orden
                              AND id_material = @id_material_nuevo
                              AND id_detalle <> @id_detalle;";

                        using (var cmd = new NpgsqlCommand(queryDuplicado, conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id_orden", idOrden);
                            cmd.Parameters.AddWithValue("@id_material_nuevo", idMaterialNuevo);
                            cmd.Parameters.AddWithValue("@id_detalle", idDetalle);

                            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                                throw new Exception("La orden ya tiene cargado ese material. No se puede duplicar una línea.");
                        }

                        decimal stockNuevoAntes = ObtenerStockMaterial(idMaterialNuevo, conexion, tx);
                        decimal? stockDespuesNuevo = teniaSnapshotRecepcion ? (decimal?)stockNuevoAntes : null;

                        if (cantidadRecibida > 0)
                        {
                            using (var cmd = new NpgsqlCommand("UPDATE public.materiales SET stockactual = stockactual - @cantidad WHERE id = @id_material;", conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@cantidad", cantidadRecibida);
                                cmd.Parameters.AddWithValue("@id_material", idMaterialAnterior);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new NpgsqlCommand("UPDATE public.materiales SET stockactual = stockactual + @cantidad WHERE id = @id_material RETURNING stockactual;", conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@cantidad", cantidadRecibida);
                                cmd.Parameters.AddWithValue("@id_material", idMaterialNuevo);
                                stockDespuesNuevo = Convert.ToDecimal(cmd.ExecuteScalar());
                            }
                        }

                        string updateDetalle = @"
                            UPDATE public.ordenes_compra_detalle
                            SET id_material = @id_material_nuevo,
                                stock_al_crear = @stock_al_crear,
                                stock_despues_recepcion = @stock_despues_recepcion
                            WHERE id_detalle = @id_detalle;";

                        using (var cmd = new NpgsqlCommand(updateDetalle, conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id_material_nuevo", idMaterialNuevo);
                            cmd.Parameters.AddWithValue("@stock_al_crear", stockNuevoAntes);
                            cmd.Parameters.AddWithValue("@stock_despues_recepcion", stockDespuesNuevo.HasValue ? (object)stockDespuesNuevo.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@id_detalle", idDetalle);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new NpgsqlCommand("UPDATE public.ordenes_compra SET updated_at = NOW() WHERE id_orden_compra = @id_orden;", conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id_orden", idOrden);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public string ConfirmarRecepcion(int idOrden, List<OrdenCompraDetalle> detallesActualizados, string usuarioRevision, string observacionRevision)
        {
            if (detallesActualizados == null || detallesActualizados.Count == 0)
                throw new Exception("No hay materiales para revisar.");

            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        string estadoActual = ObtenerEstadoActual(idOrden, conexion, tx, true);
                        if (!OrdenCompraEstados.PermiteRecepcion(estadoActual))
                            throw new Exception("La orden seleccionada ya no permite revisión.");

                        var detallesDb = new Dictionary<int, OrdenCompraDetalle>();
                        string queryDetalles = @"
                            SELECT
                                d.id_detalle,
                                d.id_orden_compra,
                                d.id_material,
                                m.nombre AS material_nombre,
                                d.cantidad_pedida,
                                d.cantidad_recibida,
                                d.cantidad_faltante,
                                COALESCE(d.observacion_item, '') AS observacion_item,
                                COALESCE(d.stock_al_crear, 0) AS stock_al_crear,
                                d.stock_despues_recepcion
                            FROM public.ordenes_compra_detalle d
                            INNER JOIN public.materiales m ON m.id = d.id_material
                            WHERE d.id_orden_compra = @id
                            ORDER BY d.id_detalle
                            FOR UPDATE OF d;";

                        using (var cmd = new NpgsqlCommand(queryDetalles, conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@id", idOrden);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var detalle = MapearDetalle(reader);
                                    detallesDb[detalle.id_detalle] = detalle;
                                }
                            }
                        }

                        if (detallesDb.Count != detallesActualizados.Count)
                            throw new Exception("La revisión no coincide con el detalle actual de la orden.");

                        bool completa = true;
                        foreach (var detalle in detallesActualizados)
                        {
                            OrdenCompraDetalle actualDb;
                            if (!detallesDb.TryGetValue(detalle.id_detalle, out actualDb))
                                throw new Exception("Se detectó un material inválido en la revisión.");

                            if (detalle.cantidad_recibida < actualDb.cantidad_recibida)
                                throw new Exception("No se puede reducir una cantidad ya recibida.");

                            if (detalle.cantidad_recibida < 0)
                                throw new Exception("La cantidad recibida no puede ser negativa.");

                            if (detalle.cantidad_recibida > actualDb.cantidad_pedida)
                                throw new Exception("La cantidad recibida no puede superar a la pedida.");

                            decimal faltante = actualDb.cantidad_pedida - detalle.cantidad_recibida;
                            decimal deltaRecibido = detalle.cantidad_recibida - actualDb.cantidad_recibida;
                            decimal stockActualizado = actualDb.stock_despues_recepcion ?? actualDb.stock_al_crear;

                            if (deltaRecibido > 0)
                            {
                                string updateStock = @"
                                    UPDATE public.materiales
                                    SET stockactual = stockactual + @delta
                                    WHERE id = @id_material
                                    RETURNING stockactual;";

                                using (var cmd = new NpgsqlCommand(updateStock, conexion))
                                {
                                    cmd.Transaction = tx;
                                    cmd.Parameters.AddWithValue("@delta", deltaRecibido);
                                    cmd.Parameters.AddWithValue("@id_material", actualDb.id_material);
                                    object result = cmd.ExecuteScalar();
                                    stockActualizado = result == null || result == DBNull.Value ? stockActualizado : Convert.ToDecimal(result);
                                }
                            }

                            string updateDetalle = @"
                                UPDATE public.ordenes_compra_detalle
                                SET cantidad_recibida = @cantidad_recibida,
                                    cantidad_faltante = @cantidad_faltante,
                                    observacion_item = @observacion_item,
                                    stock_despues_recepcion = @stock_despues_recepcion
                                WHERE id_detalle = @id_detalle;";

                            using (var cmd = new NpgsqlCommand(updateDetalle, conexion))
                            {
                                cmd.Transaction = tx;
                                cmd.Parameters.AddWithValue("@cantidad_recibida", detalle.cantidad_recibida);
                                cmd.Parameters.AddWithValue("@cantidad_faltante", faltante);
                                cmd.Parameters.AddWithValue("@observacion_item", string.IsNullOrWhiteSpace(detalle.observacion_item) ? (object)DBNull.Value : detalle.observacion_item.Trim());
                                cmd.Parameters.AddWithValue("@stock_despues_recepcion", stockActualizado);
                                cmd.Parameters.AddWithValue("@id_detalle", detalle.id_detalle);
                                cmd.ExecuteNonQuery();
                            }

                            if (faltante > 0)
                                completa = false;
                        }

                        string nuevoEstado = completa ? OrdenCompraEstados.RecibidaCompleta : OrdenCompraEstados.RecibidaParcial;
                        string updateOrden = @"
                            UPDATE public.ordenes_compra
                            SET estado = @estado,
                                usuario_revision = @usuario_revision,
                                fecha_revision = NOW(),
                                observacion_revision = @observacion_revision,
                                updated_at = NOW()
                            WHERE id_orden_compra = @id_orden;";

                        using (var cmd = new NpgsqlCommand(updateOrden, conexion))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                            cmd.Parameters.AddWithValue("@usuario_revision", string.IsNullOrWhiteSpace(usuarioRevision) ? (object)DBNull.Value : usuarioRevision.Trim());
                            cmd.Parameters.AddWithValue("@observacion_revision", string.IsNullOrWhiteSpace(observacionRevision) ? (object)DBNull.Value : observacionRevision.Trim());
                            cmd.Parameters.AddWithValue("@id_orden", idOrden);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return nuevoEstado;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public int ObtenerIdDestinoGalpon()
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                return ObtenerIdDestinoGalpon(conexion, null);
            }
        }

        public string ObtenerNombreDestinoGalpon()
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                string query = @"
                    SELECT nombre
                    FROM public.ubicacion
                    ORDER BY
                        CASE
                            WHEN UPPER(nombre) LIKE '%GALPON%' THEN 0
                            WHEN UPPER(nombre) LIKE '%DEPÓSITO%' OR UPPER(nombre) LIKE '%DEPOSITO%' THEN 1
                            ELSE 2
                        END,
                        nombre
                    LIMIT 1;";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        throw new Exception("No se encontró una ubicación para Galpón.");

                    return result.ToString();
                }
            }
        }

        private OrdenCompra MapearOrdenResumen(IDataRecord reader)
        {
            return new OrdenCompra
            {
                id_orden_compra = reader.GetInt32(0),
                fecha = reader.GetDateTime(1),
                nro_orden = reader.GetString(2),
                nro_remito = reader.GetString(3),
                id_proveedor = reader.GetInt32(4),
                proveedor_nombre = reader.GetString(5),
                id_destino = reader.GetInt32(6),
                destino_nombre = reader.GetString(7),
                costoflete = reader.GetDecimal(8),
                observaciones = reader.GetString(9),
                estado = reader.GetString(10),
                usuario_creacion = reader.GetString(11),
                usuario_revision = reader.GetString(12),
                fecha_revision = reader.IsDBNull(13) ? (DateTime?)null : reader.GetDateTime(13),
                observacion_revision = reader.GetString(14),
                cantidad_items = reader.GetInt32(15),
                total_cantidad_pedida = Convert.ToDecimal(reader.GetValue(16)),
                total_cantidad_recibida = Convert.ToDecimal(reader.GetValue(17))
            };
        }

        private OrdenCompra MapearOrdenCabecera(IDataRecord reader)
        {
            return new OrdenCompra
            {
                id_orden_compra = reader.GetInt32(0),
                fecha = reader.GetDateTime(1),
                nro_orden = reader.GetString(2),
                nro_remito = reader.GetString(3),
                id_proveedor = reader.GetInt32(4),
                proveedor_nombre = reader.GetString(5),
                id_destino = reader.GetInt32(6),
                destino_nombre = reader.GetString(7),
                costoflete = reader.GetDecimal(8),
                observaciones = reader.GetString(9),
                estado = reader.GetString(10),
                usuario_creacion = reader.GetString(11),
                usuario_revision = reader.GetString(12),
                fecha_revision = reader.IsDBNull(13) ? (DateTime?)null : reader.GetDateTime(13),
                observacion_revision = reader.GetString(14)
            };
        }

        private OrdenCompraDetalle MapearDetalle(IDataRecord reader)
        {
            return new OrdenCompraDetalle
            {
                id_detalle = reader.GetInt32(0),
                id_orden_compra = reader.GetInt32(1),
                id_material = reader.GetInt32(2),
                material_nombre = reader.GetString(3),
                cantidad_pedida = Convert.ToDecimal(reader.GetValue(4)),
                cantidad_recibida = Convert.ToDecimal(reader.GetValue(5)),
                cantidad_faltante = Convert.ToDecimal(reader.GetValue(6)),
                observacion_item = reader.GetString(7),
                stock_al_crear = reader.GetDecimal(8),
                stock_despues_recepcion = reader.IsDBNull(9) ? (decimal?)null : reader.GetDecimal(9)
            };
        }

        private string ObtenerEstadoActual(int idOrden, NpgsqlConnection conexion, NpgsqlTransaction tx, bool bloquearFila = false)
        {
            string query = "SELECT estado FROM public.ordenes_compra WHERE id_orden_compra = @id";
            if (bloquearFila)
                query += " FOR UPDATE";

            using (var cmd = new NpgsqlCommand(query + ";", conexion))
            {
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", idOrden);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new Exception("La orden indicada no existe.");

                return result.ToString();
            }
        }

        private void ValidarNroOrdenUnico(string nroOrden, int idOrdenActual, NpgsqlConnection conexion, NpgsqlTransaction tx)
        {
            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM public.ordenes_compra WHERE nro_orden = @nro_orden AND id_orden_compra <> @id;", conexion))
            {
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@nro_orden", nroOrden.Trim());
                cmd.Parameters.AddWithValue("@id", idOrdenActual);
                long count = Convert.ToInt64(cmd.ExecuteScalar());
                if (count > 0)
                    throw new Exception("El número de orden ya existe. Debe ser único.");
            }
        }

        private int ObtenerIdDestinoGalpon(NpgsqlConnection conexion, NpgsqlTransaction tx)
        {
            string query = @"
                SELECT id_ubicacion
                FROM public.ubicacion
                ORDER BY
                    CASE
                        WHEN UPPER(nombre) LIKE '%GALPON%' THEN 0
                        WHEN UPPER(nombre) LIKE '%DEPÓSITO%' OR UPPER(nombre) LIKE '%DEPOSITO%' THEN 1
                        ELSE 2
                    END,
                    nombre
                LIMIT 1;";

            using (var cmd = new NpgsqlCommand(query, conexion))
            {
                cmd.Transaction = tx;
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new Exception("No se encontró una ubicación para Galpón.");

                return Convert.ToInt32(result);
            }
        }

        private decimal ObtenerStockMaterial(int idMaterial, NpgsqlConnection conexion, NpgsqlTransaction tx)
        {
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(stockactual, 0) FROM public.materiales WHERE id = @id;", conexion))
            {
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", idMaterial);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    throw new Exception("No se encontró el material seleccionado.");

                return Convert.ToDecimal(result);
            }
        }

        private List<OrdenCompraDetalle> NormalizarDetalles(List<OrdenCompraDetalle> detalles)
        {
            var normalizados = detalles
                .Where(x => x != null)
                .Select(x => new OrdenCompraDetalle
                {
                    id_material = x.id_material,
                    material_nombre = x.material_nombre,
                    cantidad_pedida = x.cantidad_pedida,
                    observacion_item = x.observacion_item
                })
                .ToList();

            if (normalizados.Any(x => x.id_material <= 0))
                throw new Exception("Todos los materiales de la orden deben ser válidos.");

            if (normalizados.Any(x => x.cantidad_pedida <= 0))
                throw new Exception("Todas las cantidades pedidas deben ser mayores a cero.");

            if (normalizados.GroupBy(x => x.id_material).Any(x => x.Count() > 1))
                throw new Exception("No se puede repetir un mismo material dentro de la orden.");

            return normalizados;
        }

        public List<ReporteOrdenPendienteItem> ObtenerReporteOrdenesPendientes()
        {
            List<ReporteOrdenPendienteItem> lista = new List<ReporteOrdenPendienteItem>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed)
                    conexion.Open();

                string query = @"
                    SELECT 
                        oc.nro_orden,
                        COALESCE(p.nombre, '-') AS proveedor,
                        m.nombre AS material,
                        d.cantidad_pedida,
                        d.cantidad_recibida,
                        d.cantidad_faltante,
                        oc.estado
                    FROM public.ordenes_compra oc
                    INNER JOIN public.ordenes_compra_detalle d ON oc.id_orden_compra = d.id_orden_compra
                    INNER JOIN public.materiales m ON d.id_material = m.id
                    LEFT JOIN public.proveedores p ON oc.id_proveedor = p.id_proveedores
                    WHERE oc.estado IN ('Pendiente', 'Recibida Parcial') AND d.cantidad_faltante > 0
                    ORDER BY oc.id_orden_compra, m.nombre;";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ReporteOrdenPendienteItem
                            {
                                nro_orden = reader.GetString(0),
                                proveedor = reader.GetString(1),
                                material = reader.GetString(2),
                                cantidad_pedida = Convert.ToDecimal(reader.GetValue(3)),
                                cantidad_recibida = Convert.ToDecimal(reader.GetValue(4)),
                                cantidad_faltante = Convert.ToDecimal(reader.GetValue(5)),
                                estado = reader.GetString(6)
                            });
                        }
                    }
                }
            }

            return lista;
        }
    }
}
