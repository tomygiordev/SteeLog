using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
namespace SteeLog.Core.Application.Inventario
{
    public class InventarioMaterial
    {
        public int id { get; set; }
        public string codigo { get; set; }
        public string nombre { get; set; }
        public string categoria { get; set; }
        public string unidad { get; set; }
        public decimal stock_actual { get; set; }
        public decimal stock_minimo { get; set; }
        public decimal precio_unitario { get; set; }
        public string estado_stock { get; set; }
        public string proveedores { get; set; }
    }

    public class InventarioRecorte
    {
        public int id { get; set; }
        public string material { get; set; }
        public string categoria { get; set; }
        public decimal ancho { get; set; }
        public decimal altura { get; set; }
        public decimal medida { get; set; }
        public decimal cantidad { get; set; }
        public DateTime fecha_creacion { get; set; }
    }

    public class InventarioHerramienta
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string estado { get; set; }
        public string obra { get; set; }
        public string observaciones { get; set; }
    }

    public class InventarioInsumo
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string obra { get; set; }
        public bool consumido { get; set; }
        public string estado { get; set; }
        public string observaciones { get; set; }
    }

    public class ResumenInventario
    {
        public int total_materiales { get; set; }
        public int materiales_sin_stock { get; set; }
        public int materiales_stock_bajo { get; set; }
        public int total_recortes { get; set; }
        public int total_herramientas { get; set; }
        public int herramientas_en_obra { get; set; }
        public int total_insumos { get; set; }
        public int insumos_en_obra { get; set; }
    }

    public interface IInventarioConsulta
    {
        Task<ResumenInventario> ObtenerResumenAsync(CancellationToken token = default);
        Task<List<InventarioMaterial>> ObtenerMaterialesAsync(string busqueda, string categoria, string estadoStock, CancellationToken token = default);
        Task<List<InventarioRecorte>> ObtenerRecortesAsync(string busqueda, string categoria, CancellationToken token = default);
        Task<List<InventarioHerramienta>> ObtenerHerramientasAsync(string busqueda, string estadoFiltro, string obraFiltro, CancellationToken token = default);
        Task<List<InventarioInsumo>> ObtenerInsumosAsync(string busqueda, string estadoFiltro, string obraFiltro, CancellationToken token = default);
        Task<List<string>> ObtenerCategoriasAsync(CancellationToken token = default);
        Task<List<string>> ObtenerObrasActivasAsync(CancellationToken token = default);
        Task<List<string>> ObtenerObrasConHerramientasAsync(CancellationToken token = default);
    }
    public sealed class ConsultarInventario
    {
        private readonly Session actor; private readonly IInventarioConsulta consulta;
        public ConsultarInventario(Session actor, IInventarioConsulta consulta) { this.actor=actor; this.consulta=consulta; }
        public Task<ResumenInventario> ResumenAsync(CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerResumenAsync(token); }
        public Task<List<InventarioMaterial>> MaterialesAsync(string busqueda, string categoria, string estadoStock, CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerMaterialesAsync(busqueda, categoria, estadoStock, token); }
        public Task<List<InventarioRecorte>> RecortesAsync(string busqueda, string categoria, CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerRecortesAsync(busqueda, categoria, token); }
        public Task<List<InventarioHerramienta>> HerramientasAsync(string busqueda, string estadoFiltro, string obraFiltro, CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerHerramientasAsync(busqueda, estadoFiltro, obraFiltro, token); }
        public Task<List<InventarioInsumo>> InsumosAsync(string busqueda, string estadoFiltro, string obraFiltro, CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerInsumosAsync(busqueda, estadoFiltro, obraFiltro, token); }
        public Task<List<string>> CategoriasAsync(CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerCategoriasAsync(token); }
        public Task<List<string>> ObrasActivasAsync(CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerObrasActivasAsync(token); }
        public Task<List<string>> ObrasConHerramientasAsync(CancellationToken token) { actor.Require(Role.Galpon); return consulta.ObtenerObrasConHerramientasAsync(token); }
    }
}
