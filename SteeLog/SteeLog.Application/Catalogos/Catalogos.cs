using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Domain.Catalogos;
namespace SteeLog.Core.Application.Catalogos
{
    public sealed class CatalogoDuplicadoException : Exception
    {
        public CatalogoDuplicadoException(string entidadConArticulo, Exception causa = null)
            : base("Ya existe " + entidadConArticulo + " con ese nombre.", causa) { }
    }
    public sealed class CatalogoEnUsoException : Exception
    {
        public CatalogoEnUsoException(string entidadConArticulo, string referencia = null, Exception causa = null)
            : base("No se puede eliminar " + entidadConArticulo + " porque está en uso" + (string.IsNullOrWhiteSpace(referencia) ? "" : " (" + referencia + ")") + ".", causa) { }
    }
    public sealed class CategoriaCatalogo { public int Id { get; set; } public string Nombre { get; set; } public bool Recortable { get; set; } }
    public sealed class ObraCatalogo { public int Id { get; set; } public string Nombre { get; set; } public bool Activa { get; set; } }
    public sealed class ProveedorCatalogo { public int Id { get; set; } public string Nombre { get; set; } }
    public sealed class FleteCatalogo { public int Id { get; set; } public string Nombre { get; set; } public bool Activo { get; set; } }
    public sealed class UbicacionCatalogo { public int Id { get; set; } public string Nombre { get; set; } }
    public interface ICatalogosConsulta
    {
        Task<IReadOnlyList<CategoriaCatalogo>> CategoriasAsync(CancellationToken token = default);
        Task<IReadOnlyList<ObraCatalogo>> ObrasAsync(CancellationToken token = default);
        Task<IReadOnlyList<ProveedorCatalogo>> ProveedoresAsync(CancellationToken token = default);
        Task<IReadOnlyList<FleteCatalogo>> FletesAsync(CancellationToken token = default);
        Task<IReadOnlyList<UbicacionCatalogo>> UbicacionesAsync(CancellationToken token = default);
    }
    public sealed class ConsultarCatalogos
    {
        private readonly Session actor; private readonly ICatalogosConsulta consulta;
        public ConsultarCatalogos(Session actor, ICatalogosConsulta consulta) { this.actor=actor ?? throw new ArgumentNullException(nameof(actor)); this.consulta=consulta ?? throw new ArgumentNullException(nameof(consulta)); }
        public Task<IReadOnlyList<CategoriaCatalogo>> CategoriasAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.CategoriasAsync(token); }
        public Task<IReadOnlyList<ObraCatalogo>> ObrasAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.ObrasAsync(token); }
        public Task<IReadOnlyList<ProveedorCatalogo>> ProveedoresAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.ProveedoresAsync(token); }
        public Task<IReadOnlyList<FleteCatalogo>> FletesAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.FletesAsync(token); }
        public Task<IReadOnlyList<UbicacionCatalogo>> UbicacionesAsync(CancellationToken token = default) { actor.Require(Role.Galpon); return consulta.UbicacionesAsync(token); }
    }
    public interface ICatalogosEscritura
    {
        Task<int> CrearCategoriaAsync(Categoria valor, CancellationToken token = default);
        Task EliminarCategoriaAsync(int id, CancellationToken token = default);
        Task EditarCategoriaAsync(Categoria valor, CancellationToken token = default);
        Task<int> CrearObraAsync(Obra valor, CancellationToken token = default);
        Task EliminarObraAsync(int id, CancellationToken token = default);
        Task EditarObraAsync(Obra valor, CancellationToken token = default);
        Task<int> CrearProveedorAsync(Proveedor valor, CancellationToken token = default);
        Task EliminarProveedorAsync(int id, CancellationToken token = default);
        Task EditarProveedorAsync(Proveedor valor, CancellationToken token = default);
        Task EditarFleteAsync(Flete valor, CancellationToken token = default);
        Task<int> CrearUbicacionAsync(Ubicacion valor, CancellationToken token = default);
        Task EliminarUbicacionAsync(int id, CancellationToken token = default);
        Task EditarUbicacionAsync(Ubicacion valor, CancellationToken token = default);
    }
    public sealed class GestionarCatalogos
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly ICatalogosEscritura repo; private readonly IFletesEscritura fletes;
        public GestionarCatalogos(Session actor, IUnitOfWork uow, ICatalogosEscritura repo, IFletesEscritura fletes)
        { this.actor=actor ?? throw new ArgumentNullException(nameof(actor)); this.uow=uow ?? throw new ArgumentNullException(nameof(uow)); this.repo=repo ?? throw new ArgumentNullException(nameof(repo)); this.fletes=fletes ?? throw new ArgumentNullException(nameof(fletes)); }
        private Task<int> Transaccion(Func<Task<int>> accion, CancellationToken token)
        {
            return Common.Transaccion.EjecutarAsync(uow, _ => accion(), token);
        }
        private void AutorizarId(int id) { actor.Require(Role.Galpon); if(id <= 0) throw new ArgumentOutOfRangeException(nameof(id)); }
        public Task<int> CrearCategoriaAsync(string nombre, bool recortable, CancellationToken token = default)
        { actor.Require(Role.Galpon); var valor=new Categoria(0, nombre, recortable); return Transaccion(async () => { var id=await repo.CrearCategoriaAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EditarCategoriaAsync(int id, string nombre, bool recortable, CancellationToken token = default)
        { AutorizarId(id); var valor=new Categoria(id, nombre, recortable); return Transaccion(async () => { await repo.EditarCategoriaAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EliminarCategoriaAsync(int id, CancellationToken token = default) { AutorizarId(id); return Transaccion(async () => { await repo.EliminarCategoriaAsync(id, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> CrearObraAsync(string nombre, bool activa, CancellationToken token = default)
        { actor.Require(Role.Galpon); var valor=new Obra(0, nombre, activa); return Transaccion(async () => { var id=await repo.CrearObraAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EditarObraAsync(int id, string nombre, bool activa, CancellationToken token = default)
        { AutorizarId(id); var valor=new Obra(id, nombre, activa); return Transaccion(async () => { await repo.EditarObraAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EliminarObraAsync(int id, CancellationToken token = default) { AutorizarId(id); return Transaccion(async () => { await repo.EliminarObraAsync(id, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> CrearProveedorAsync(string nombre, CancellationToken token = default)
        { actor.Require(Role.Galpon); var valor=new Proveedor(0, nombre); return Transaccion(async () => { var id=await repo.CrearProveedorAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EditarProveedorAsync(int id, string nombre, CancellationToken token = default)
        { AutorizarId(id); var valor=new Proveedor(id, nombre); return Transaccion(async () => { await repo.EditarProveedorAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EliminarProveedorAsync(int id, CancellationToken token = default) { AutorizarId(id); return Transaccion(async () => { await repo.EliminarProveedorAsync(id, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> CrearFleteAsync(string nombre, bool activo, CancellationToken token = default)
        { actor.Require(Role.Galpon); var valor=new Flete(0, nombre, activo); return Transaccion(async () => { var id=await fletes.CrearAsync(valor.Nombre, token).ConfigureAwait(false); if (!valor.Activo) await fletes.DesactivarAsync(id, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EditarFleteAsync(int id, string nombre, bool activo, CancellationToken token = default)
        { AutorizarId(id); var valor=new Flete(id, nombre, activo); return Transaccion(async () => { await repo.EditarFleteAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> DesactivarFleteAsync(int id, CancellationToken token = default) { AutorizarId(id); return Transaccion(async () => { await fletes.DesactivarAsync(id, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> CrearUbicacionAsync(string nombre, CancellationToken token = default)
        { actor.Require(Role.Galpon); var valor=new Ubicacion(0, nombre); return Transaccion(async () => { var id=await repo.CrearUbicacionAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EditarUbicacionAsync(int id, string nombre, CancellationToken token = default)
        { AutorizarId(id); var valor=new Ubicacion(id, nombre); return Transaccion(async () => { await repo.EditarUbicacionAsync(valor, token).ConfigureAwait(false); return id; }, token); }
        public Task<int> EliminarUbicacionAsync(int id, CancellationToken token = default) { AutorizarId(id); return Transaccion(async () => { await repo.EliminarUbicacionAsync(id, token).ConfigureAwait(false); return id; }, token); }
    }
}
