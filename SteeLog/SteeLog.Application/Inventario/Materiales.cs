using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Inventario;

namespace SteeLog.Core.Application.Inventario
{
    public sealed class DatosMaterial
    {
        public string Nombre { get; set; }
        public int CategoriaId { get; set; }
        public decimal StockMinimo { get; set; }
        public IReadOnlyList<int> Proveedores { get; set; }
    }
    public interface IMaterialesEscritura
    {
        Task<int> CrearAsync(Material material, CancellationToken token = default);
        Task ActualizarDatosAsync(int id, DatosMaterial datos, CancellationToken token = default);
        Task GuardarProveedoresAsync(int id, IReadOnlyList<int> proveedores, CancellationToken token = default);
        Task<int?> BloquearAjusteDiarioAsync(DateTime fecha, CancellationToken token = default);
        Task RegistrarAjusteAsync(int? cabeceraId, DateTime fecha, int materialId, decimal delta, decimal saldo, CancellationToken token = default);
        Task EliminarAsync(int id, CancellationToken token = default);
    }
    internal static class MaterialesOperacion
    {
        public static DatosMaterial Validar(DatosMaterial datos, decimal saldo)
        {
            if (datos == null) throw new ArgumentNullException(nameof(datos));
            var m = new Material(0, datos.Nombre, datos.CategoriaId, saldo, datos.StockMinimo);
            var proveedores = datos.Proveedores?.Distinct().ToArray();
            if (proveedores != null && proveedores.Any(x => x <= 0)) throw new ArgumentOutOfRangeException(nameof(datos.Proveedores));
            return new DatosMaterial { Nombre = m.Nombre, CategoriaId = m.CategoriaId, StockMinimo = m.StockMinimo, Proveedores = proveedores };
        }
        public static async Task<T> Transaccion<T>(IUnitOfWork uow, Func<Task<T>> accion, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); await uow.BeginAsync(token).ConfigureAwait(false);
            try { var valor = await accion().ConfigureAwait(false); token.ThrowIfCancellationRequested(); await uow.CommitAsync(token).ConfigureAwait(false); return valor; }
            catch { await uow.RollbackAsync().ConfigureAwait(false); throw; }
        }
    }
    public sealed class CrearMaterial
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IMaterialesEscritura repo;
        public CrearMaterial(Session actor, IUnitOfWork uow, IMaterialesEscritura repo) { this.actor = actor; this.uow = uow; this.repo = repo; }
        public Task<int> EjecutarAsync(DatosMaterial datos, decimal stockInicial, CancellationToken token = default)
        {
            actor.Require(Role.Admin); var validos = MaterialesOperacion.Validar(datos, stockInicial);
            return MaterialesOperacion.Transaccion(uow, async () => {
                var id = await repo.CrearAsync(new Material(0, validos.Nombre, validos.CategoriaId, stockInicial, validos.StockMinimo), token).ConfigureAwait(false);
                if (validos.Proveedores != null) await repo.GuardarProveedoresAsync(id, validos.Proveedores, token).ConfigureAwait(false);
                return id;
            }, token);
        }
    }
    public sealed class EditarMaterial
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IMaterialesEscritura repo; private readonly IInventarioEscritura inventario; private readonly IClock clock;
        public EditarMaterial(Session actor, IUnitOfWork uow, IMaterialesEscritura repo, IInventarioEscritura inventario, IClock clock) { this.actor = actor; this.uow = uow; this.repo = repo; this.inventario = inventario; this.clock = clock; }
        public Task EjecutarAsync(int id, DatosMaterial datos, decimal? stockNuevo, decimal? stockEsperado, CancellationToken token = default)
        {
            actor.Require(Role.Admin);
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (stockNuevo.HasValue != stockEsperado.HasValue) throw new ArgumentException("Para ajustar stock se requiere el saldo esperado.");
            var validos = MaterialesOperacion.Validar(datos, stockNuevo ?? 0);
            if (stockEsperado.HasValue) new Material(id, validos.Nombre, validos.CategoriaId, stockEsperado.Value, validos.StockMinimo);
            DateTime fecha = stockNuevo.HasValue ? clock.FechaNegocio.Date : default(DateTime);
            return MaterialesOperacion.Transaccion(uow, async () => {
                int? cabecera = stockNuevo.HasValue ? await repo.BloquearAjusteDiarioAsync(fecha, token).ConfigureAwait(false) : null;
                var saldos = await inventario.BloquearAsync(new[] { id }, token).ConfigureAwait(false);
                var entidad = new Material(id, validos.Nombre, validos.CategoriaId, saldos[id], validos.StockMinimo);
                if (stockNuevo.HasValue)
                {
                    var delta = entidad.AjustarStock(stockNuevo.Value, stockEsperado.Value);
                    if (delta != 0) {
                        await inventario.GuardarSaldoAsync(id, entidad.StockActual, token).ConfigureAwait(false);
                        await repo.RegistrarAjusteAsync(cabecera, fecha, id, delta, entidad.StockActual, token).ConfigureAwait(false);
                    }
                }
                await repo.ActualizarDatosAsync(id, validos, token).ConfigureAwait(false);
                if (validos.Proveedores != null) await repo.GuardarProveedoresAsync(id, validos.Proveedores, token).ConfigureAwait(false);
                return 0;
            }, token);
        }
    }
    public sealed class EliminarMaterial
    {
        private readonly Session actor; private readonly IUnitOfWork uow; private readonly IMaterialesEscritura repo; private readonly IInventarioEscritura inventario;
        public EliminarMaterial(Session actor, IUnitOfWork uow, IMaterialesEscritura repo, IInventarioEscritura inventario) { this.actor = actor; this.uow = uow; this.repo = repo; this.inventario = inventario; }
        public Task EjecutarAsync(int id, CancellationToken token = default)
        {
            actor.Require(Role.Admin); if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            return MaterialesOperacion.Transaccion(uow, async () => { await inventario.BloquearAsync(new[] { id }, token).ConfigureAwait(false); await repo.EliminarAsync(id, token).ConfigureAwait(false); return 0; }, token);
        }
    }
}
