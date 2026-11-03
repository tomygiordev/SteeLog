using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Inventario;

namespace SteeLog.Presentation
{
    public sealed class RecortesPresenter
    {
        private readonly IUseCaseExecutor executor;
        public RecortesPresenter(IUseCaseExecutor executor) { this.executor = executor ?? throw new ArgumentNullException(nameof(executor)); }
        public Task<IReadOnlyList<OpcionRecorte>> CategoriasAsync(CancellationToken token)
            => executor.ExecuteAsync<ConsultarRecortes, IReadOnlyList<OpcionRecorte>>(x => x.CategoriasAsync(token));
        public Task<IReadOnlyList<OpcionRecorte>> MaterialesAsync(int categoria, CancellationToken token)
            => executor.ExecuteAsync<ConsultarRecortes, IReadOnlyList<OpcionRecorte>>(x => x.MaterialesAsync(categoria, token));
        public Task<IReadOnlyList<RecorteLectura>> ListarAsync(int? material, decimal? ancho, decimal? altura, decimal? medida, CancellationToken token)
            => executor.ExecuteAsync<ConsultarRecortes, IReadOnlyList<RecorteLectura>>(x => x.ListarAsync(material, ancho, altura, medida, token));
        public Task<int> AgregarAsync(int material, decimal ancho, decimal altura, decimal medida, decimal cantidad, string estado, CancellationToken token)
            => executor.ExecuteAsync<AgregarRecorte, int>(x => x.EjecutarAsync(material, ancho, altura, medida, cantidad, estado, token));
        public Task<int> EliminarAsync(int id, int material, CancellationToken token)
            => executor.ExecuteAsync<EliminarRecorte, int>(async x => { await x.EjecutarAsync(id, material, token); return 0; });
    }
}
