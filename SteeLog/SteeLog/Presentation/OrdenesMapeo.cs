using System.Linq;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Modelos;
namespace SteeLog.Presentation
{
    public static class OrdenesMapeo
    {
        public static OrdenCompra Orden(OrdenLectura o)
        {
            if (o == null) return null;
            return new OrdenCompra { id_orden_compra=o.Id, fecha=o.Fecha, nro_orden=o.Numero, nro_remito=o.Remito,
                id_proveedor=o.ProveedorId, proveedor_nombre=o.Proveedor, id_destino=o.DestinoId, destino_nombre=o.Destino,
                costoflete=o.Flete, observaciones=o.Observaciones, estado=o.Estado, usuario_creacion=o.UsuarioCreacion,
                usuario_revision=o.UsuarioRevision, fecha_revision=o.FechaRevision, observacion_revision=o.ObservacionRevision,
                cantidad_items=o.CantidadItems, total_cantidad_pedida=o.TotalPedido, total_cantidad_recibida=o.TotalRecibido,
                Detalles=o.Lineas.Select(x => new OrdenCompraDetalle { id_detalle=x.Id,id_orden_compra=x.OrdenId,id_material=x.MaterialId,material_nombre=x.Material,cantidad_pedida=x.Pedida,cantidad_recibida=x.Recibida,cantidad_recibida_anterior=x.Recibida,cantidad_faltante=x.Faltante,observacion_item=x.Observacion,stock_al_crear=x.StockAlCrear,stock_despues_recepcion=x.StockDespues }).ToList() };
        }
        public static CabeceraOrden Cabecera(OrdenCompra o) => new CabeceraOrden { Fecha=o.fecha,Numero=o.nro_orden,Remito=o.nro_remito,ProveedorId=o.id_proveedor,Flete=o.costoflete,Observaciones=o.observaciones };
        public static LineaNueva Linea(OrdenCompraDetalle d) => new LineaNueva { MaterialId=d.id_material,Pedida=d.cantidad_pedida,Observacion=d.observacion_item };
        public static Proveedor Proveedor(ProveedorLectura p) => new Proveedor { id=p.Id,nombre=p.Nombre };
        public static Material Material(MaterialLectura m) => new Material { id=m.Id,nombre=m.Nombre,stockActual=m.Stock,stockMinimo=m.Minimo,nombre_categoria=m.Categoria };
    }
}
