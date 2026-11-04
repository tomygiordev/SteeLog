using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
namespace SteeLog.Core.Application.Reportes
{
    public sealed class ConsultarReporteStock
    {
        private readonly Session actor;
        private readonly IReporteStockConsulta reporte;
        private readonly IMaterialesConsulta materiales;
        public ConsultarReporteStock(Session actor, IReporteStockConsulta reporte, IMaterialesConsulta materiales) { this.actor = actor; this.reporte = reporte; this.materiales = materiales; }
        public async Task<IReadOnlyList<ReporteStockItem>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token = default)
        {
            actor.Require(Role.Galpon);
            filtro = filtro ?? new FiltroMaterialLectura();
            if (await reporte.TieneConfiguracionAsync(token).ConfigureAwait(false)) return await reporte.ListarConfiguradosAsync(filtro, token).ConfigureAwait(false);
            if (filtro.SoloStockBajo) filtro.Priorizados = true;
            return (await materiales.ListarAsync(filtro, token).ConfigureAwait(false)).Select(m => new ReporteStockItem { Material = m }).ToArray();
        }
        public Task<IReadOnlyList<ReporteStockSeccion>> SeccionesAsync(CancellationToken token = default) { actor.Require(Role.Admin); return reporte.SeccionesAsync(token); }
        public Task<IReadOnlyList<MaterialInventarioLectura>> MaterialesAsync(CancellationToken token = default) { actor.Require(Role.Admin); return materiales.ListarAsync(new FiltroMaterialLectura(), token); }
    }
}
