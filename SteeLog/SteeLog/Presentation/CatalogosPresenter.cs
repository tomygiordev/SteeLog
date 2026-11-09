using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Catalogos;

namespace SteeLog.Presentation
{
    public sealed class CatalogosPresenter
    {
        /// <summary>Traduce un fallo de guardado de catálogo a un mensaje para el usuario (sin detalles técnicos).</summary>
        public static string MensajeGuardado(Exception error)
        {
            while (error is AggregateException agregado && agregado.InnerExceptions.Count == 1) error = agregado.InnerException;
            if (error is CatalogoDuplicadoException) return error.Message;
            var argumento = error as ArgumentException;
            if (argumento != null && !(error is ArgumentOutOfRangeException))
            {
                var linea = (argumento.Message ?? string.Empty).Split(new[] { (char)13, (char)10 }, StringSplitOptions.RemoveEmptyEntries);
                if (linea.Length > 0) return linea[0];
            }
            return "Error al guardar: " + error.Message;
        }
        private readonly IUseCaseExecutor executor;
        public CatalogosPresenter(IUseCaseExecutor executor) { this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); }
        public Task<IReadOnlyList<CategoriaCatalogo>> CategoriasAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarCatalogos, IReadOnlyList<CategoriaCatalogo>>(x => x.CategoriasAsync(t));
        public Task<int> GuardarCategoriaAsync(int? id, string nombre, bool estado, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => id.HasValue ? x.EditarCategoriaAsync(id.Value, nombre, estado, t) : x.CrearCategoriaAsync(nombre, estado, t));
        public Task<int> EliminarCategoriaAsync(int id, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => x.EliminarCategoriaAsync(id, t));
        public Task<IReadOnlyList<ObraCatalogo>> ObrasAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarCatalogos, IReadOnlyList<ObraCatalogo>>(x => x.ObrasAsync(t));
        public Task<int> GuardarObraAsync(int? id, string nombre, bool estado, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => id.HasValue ? x.EditarObraAsync(id.Value, nombre, estado, t) : x.CrearObraAsync(nombre, estado, t));
        public Task<int> EliminarObraAsync(int id, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => x.EliminarObraAsync(id, t));
        public Task<IReadOnlyList<ProveedorCatalogo>> ProveedoresAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarCatalogos, IReadOnlyList<ProveedorCatalogo>>(x => x.ProveedoresAsync(t));
        public Task<int> GuardarProveedorAsync(int? id, string nombre, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => id.HasValue ? x.EditarProveedorAsync(id.Value, nombre, t) : x.CrearProveedorAsync(nombre, t));
        public Task<int> EliminarProveedorAsync(int id, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => x.EliminarProveedorAsync(id, t));
        public Task<IReadOnlyList<UbicacionCatalogo>> UbicacionesAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarCatalogos, IReadOnlyList<UbicacionCatalogo>>(x => x.UbicacionesAsync(t));
        public Task<int> GuardarUbicacionAsync(int? id, string nombre, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => id.HasValue ? x.EditarUbicacionAsync(id.Value, nombre, t) : x.CrearUbicacionAsync(nombre, t));
        public Task<int> EliminarUbicacionAsync(int id, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => x.EliminarUbicacionAsync(id, t));
        public Task<IReadOnlyList<FleteCatalogo>> FletesAsync(CancellationToken t) => executor.ExecuteAsync<ConsultarCatalogos, IReadOnlyList<FleteCatalogo>>(x => x.FletesAsync(t));
        public Task<int> GuardarFleteAsync(int? id, string nombre, bool estado, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => id.HasValue ? x.EditarFleteAsync(id.Value, nombre, estado, t) : x.CrearFleteAsync(nombre, estado, t));
        public Task<int> EliminarFleteAsync(int id, CancellationToken t) => executor.ExecuteAsync<GestionarCatalogos, int>(x => x.DesactivarFleteAsync(id, t));
    }
}
