using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Common;

namespace SteeLog.Core.Application.Reportes
{
    public interface IReporteStockConfiguracion
    {
        Task GuardarAsync(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, CancellationToken token = default);
    }

    public sealed class GuardarReporteStockConfiguracion
    {
        private readonly Session actor;
        private readonly IReporteStockConfiguracion configuracion;
        private readonly IUnitOfWork uow;
        public GuardarReporteStockConfiguracion(Session actor, IUnitOfWork uow, IReporteStockConfiguracion configuracion)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.uow = uow ?? throw new ArgumentNullException(nameof(uow));
            this.configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        }
        public async Task EjecutarAsync(IReadOnlyList<ReporteStockSeccion> secciones, IReadOnlyList<int> eliminadas, CancellationToken token = default)
        {
            actor.Require(Role.Admin);
            if (secciones == null || eliminadas == null) throw new ArgumentNullException();
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var materiales = new HashSet<int>();
            var ids = new HashSet<int>();
            foreach (var seccion in secciones)
            {
                if (seccion == null || string.IsNullOrWhiteSpace(seccion.Nombre)) throw new InvalidOperationException("Todas las secciones deben tener nombre.");
                if (!nombres.Add(seccion.Nombre.Trim())) throw new InvalidOperationException("No puede haber dos secciones con el mismo nombre.");
                if (seccion.Id < 0 || (seccion.Id > 0 && !ids.Add(seccion.Id))) throw new InvalidOperationException("Identificador de sección inválido o duplicado.");
                if (seccion.Materiales == null || seccion.Materiales.Any(id => id <= 0 || !materiales.Add(id))) throw new InvalidOperationException("Un material no puede asignarse más de una vez.");
            }
            if (eliminadas.Any(id => id <= 0 || ids.Contains(id)) || eliminadas.Distinct().Count() != eliminadas.Count)
                throw new InvalidOperationException("Identificadores de secciones eliminadas inválidos.");
            var snapshot = secciones.Select(s => new ReporteStockSeccion { Id = s.Id, Nombre = s.Nombre.Trim(), Materiales = s.Materiales.ToList() }).ToArray();
            var idsEliminados = eliminadas.ToArray();
            token.ThrowIfCancellationRequested();
            await uow.BeginAsync(token).ConfigureAwait(false);
            bool inicioCommit = false;
            try
            {
                await configuracion.GuardarAsync(snapshot, idsEliminados, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                inicioCommit = true;
                await uow.CommitAsync(token).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                try { await uow.RollbackAsync().ConfigureAwait(false); } catch { }
                if (inicioCommit && !(error is OperationCanceledException)) throw new ResultadoEscrituraInciertoException(error);
                throw;
            }
        }
    }
}
