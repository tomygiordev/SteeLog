using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Inventario;

namespace SteeLog.Core.Application.Movimientos
{
    internal sealed class MotorMovimiento
    {
        private readonly IUnitOfWork uow;
        private readonly IMovimientosEscritura repo;
        private readonly IInventarioEscritura inventario;
        public MotorMovimiento(IUnitOfWork uow, IMovimientosEscritura repo, IInventarioEscritura inventario)
        { this.uow = uow; this.repo = repo; this.inventario = inventario; }

        public async Task<int> EjecutarAsync(int? id, MovimientoSolicitud solicitud, bool eliminar, CancellationToken token)
        {
            if (id.HasValue && id.Value <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (!eliminar) solicitud = ValidarYCopiar(solicitud);
            token.ThrowIfCancellationRequested();
            await uow.BeginAsync(token).ConfigureAwait(false);
            try
            {
                var previo = id.HasValue ? await repo.BloquearAsync(id.Value, token).ConfigureAwait(false) : null;
                if (id.HasValue && previo == null) throw new InvalidOperationException("No se encontró el movimiento.");
                if (previo != null) SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(previo.Cabecera.TipoMovimientoId, previo.TipoNombre);
                var anteriores = previo?.Lineas ?? new List<LineaMovimiento>();
                var nuevas = solicitud?.Lineas ?? new List<LineaMovimiento>();
                var insumosPrevios = previo?.Insumos ?? new List<TrasladoInsumo>();
                var insumosNuevos = solicitud?.Insumos ?? new List<int>();

                string nombreTipo = null;
                ImpactoStock impactoNuevo = ImpactoStock.SinImpacto;
                decimal factorNuevo = 0m;
                if (!eliminar && solicitud != null)
                {
                    nombreTipo = await repo.NombreTipoAsync(solicitud.Cabecera.TipoMovimientoId, token).ConfigureAwait(false);
                    SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(solicitud.Cabecera.TipoMovimientoId, nombreTipo);
                    impactoNuevo = ClasificadorMovimiento.Clasificar(nombreTipo, solicitud.Cabecera.TipoDestino, !id.HasValue);
                    factorNuevo = impactoNuevo == ImpactoStock.Entrada ? 1m : impactoNuevo == ImpactoStock.Salida ? -1m : 0m;
                }

                var recorteIdsPrevios = anteriores.Where(x => x.RecorteId.HasValue).Select(x => x.RecorteId.Value).Distinct().OrderBy(x => x).ToArray();
                var duenosPrevios = await repo.ObtenerMaterialesRecortesAsync(recorteIdsPrevios, token).ConfigureAwait(false);
                var materialesIds = anteriores.Concat(nuevas).Where(x => x.MaterialId.HasValue && !x.CompraEventual)
                    .Select(x => x.MaterialId.Value).Concat(duenosPrevios.Values).Distinct().OrderBy(x => x).ToArray();
                var insumoIds = insumosPrevios.Select(x => x.InsumoId).Concat(insumosNuevos).Distinct().OrderBy(x => x).ToArray();
                var materiales = await inventario.BloquearAsync(materialesIds, token).ConfigureAwait(false);

                foreach (var linea in nuevas.Where(x => x.EsRecorte && (!x.RecorteId.HasValue || x.RecorteId.Value <= 0)))
                {
                    if (!linea.MaterialId.HasValue || !materiales.ContainsKey(linea.MaterialId.Value))
                        throw new InvalidOperationException("El material indicado no existe.");
                    if (factorNuevo <= 0)
                        throw new InvalidOperationException("Stock insuficiente.");
                    var spec = new SteeLog.Domain.Inventario.Recorte(0, linea.MaterialId.Value, linea.AnchoRecorte ?? 0m, linea.AlturaRecorte ?? 0m, linea.MedidaRecorte ?? 0m, 0m, null);
                    linea.RecorteId = await repo.ResolverRecorteAsync(spec.MaterialId, spec.Ancho, spec.Altura, spec.Medida, token).ConfigureAwait(false);
                }

                var recorteIds = anteriores.Concat(nuevas).Where(x => x.RecorteId.HasValue).Select(x => x.RecorteId.Value).Distinct().OrderBy(x => x).ToArray();
                var duenos = await repo.ObtenerMaterialesRecortesAsync(recorteIds, token).ConfigureAwait(false);
                var recortes = await repo.BloquearRecortesAsync(recorteIds, token).ConfigureAwait(false);
                var duenosBloqueados = await repo.ObtenerMaterialesRecortesAsync(recorteIds, token).ConfigureAwait(false);
                foreach (var recorte in recorteIds)
                    if (duenos[recorte] != duenosBloqueados[recorte]) throw new InvalidOperationException("El material del recorte cambió durante la operación; vuelva a intentar.");
                foreach (var linea in nuevas.Where(x => x.RecorteId.HasValue))
                    if (linea.MaterialId != duenosBloqueados[linea.RecorteId.Value]) throw new InvalidOperationException("El recorte no pertenece al material indicado.");
                var insumos = await repo.BloquearInsumosAsync(insumoIds, token).ConfigureAwait(false);
                if (previo != null)
                {
                    foreach (var traslado in insumosPrevios)
                    {
                        var actual = insumos[traslado.InsumoId];
                        if (!traslado.SnapshotCompleto || actual.UltimoHistorialId != traslado.HistorialId ||
                            actual.ObraId != traslado.ObraDestinoId || actual.Consumido != traslado.ConsumidoDestino)
                            throw new InvalidOperationException("No se puede revertir el insumo: su historial es ambiguo o cambió después del movimiento.");
                    }
                    var impacto = ClasificadorMovimiento.Clasificar(previo.TipoNombre, previo.Cabecera.TipoDestino);
                    foreach (var linea in anteriores.AsEnumerable().Reverse())
                        await CambiarSaldo(linea, ImpactoMovimiento.ParaReversion(linea.ImpactoReal, linea.Cantidad, impacto), materiales, recortes, token).ConfigureAwait(false);
                    foreach (var traslado in insumosPrevios)
                    {
                        await repo.RevertirTrasladoAsync(traslado, token).ConfigureAwait(false);
                        insumos[traslado.InsumoId].ObraId = traslado.ObraOrigenId;
                        insumos[traslado.InsumoId].Consumido = traslado.ConsumidoOrigen;
                    }
                    await repo.EliminarLineasYTrasladosAsync(id.Value, token).ConfigureAwait(false);
                }
                if (eliminar)
                {
                    await repo.EliminarCabeceraAsync(id.Value, token).ConfigureAwait(false);
                    await uow.CommitAsync(token).ConfigureAwait(false);
                    return id.Value;
                }
                int movimientoId;
                if (id.HasValue) { movimientoId = id.Value; await repo.ActualizarCabeceraAsync(id.Value, solicitud.Cabecera, token).ConfigureAwait(false); }
                else movimientoId = await repo.CrearCabeceraAsync(solicitud.Cabecera, token).ConfigureAwait(false);
                foreach (var linea in nuevas)
                {
                    decimal delta = impactoNuevo == ImpactoStock.Entrada ? linea.Cantidad : impactoNuevo == ImpactoStock.Salida ? -linea.Cantidad : 0m;
                    decimal saldo = await CambiarSaldo(linea, delta, materiales, recortes, token).ConfigureAwait(false);
                    await repo.AgregarLineaAsync(movimientoId, linea, saldo, token).ConfigureAwait(false);
                }
                int? destino = string.Equals(solicitud.Cabecera.TipoDestino, "OBRA", StringComparison.OrdinalIgnoreCase) ? solicitud.Cabecera.DestinoId : (int?)null;
                foreach (var insumoId in insumosNuevos)
                {
                    var actual = insumos[insumoId];
                    if (actual.Consumido) throw new InvalidOperationException("No se puede trasladar un insumo consumido.");
                    var traslado = new TrasladoInsumo { InsumoId = insumoId, ObraOrigenId = actual.ObraId, ObraDestinoId = destino,
                        ConsumidoOrigen = actual.Consumido, ConsumidoDestino = actual.Consumido, Origen = solicitud.OrigenNombre, Destino = solicitud.DestinoNombre, SnapshotCompleto = true };
                    await repo.RegistrarTrasladoAsync(movimientoId, traslado, token).ConfigureAwait(false);
                }
                token.ThrowIfCancellationRequested();
                await uow.CommitAsync(token).ConfigureAwait(false);
                return movimientoId;
            }
            catch { await uow.RollbackAsync().ConfigureAwait(false); throw; }
        }
        private async Task<decimal> CambiarSaldo(LineaMovimiento linea, decimal delta, IDictionary<int, decimal> materiales, IDictionary<int, decimal> recortes, CancellationToken token)
        {
            if (linea.CompraEventual || (!linea.MaterialId.HasValue && !linea.RecorteId.HasValue)) { linea.ImpactoReal = 0m; return 0m; }
            var esRecorte = linea.RecorteId.HasValue;
            int id = esRecorte ? linea.RecorteId.Value : linea.MaterialId.Value;
            var saldos = esRecorte ? recortes : materiales;
            decimal anterior = saldos[id];
            decimal esperado = decimal.Round(SaldoInventario.Aplicar(anterior, delta), 2, MidpointRounding.AwayFromZero);
            decimal posterior;
            if (esRecorte) posterior = await repo.GuardarRecorteAsync(id, esperado, token).ConfigureAwait(false);
            else { await inventario.GuardarSaldoAsync(id, esperado, token).ConfigureAwait(false); posterior = esperado; }
            saldos[id] = posterior;
            linea.ImpactoReal = ImpactoMovimiento.EfectoReal(anterior, posterior);
            return posterior;
        }
        private static MovimientoSolicitud ValidarYCopiar(MovimientoSolicitud s)
        {
            if (s?.Cabecera == null || s.Lineas == null || s.Insumos == null) throw new ArgumentException("Movimiento incompleto.");
            var c = s.Cabecera;
            var lineasDominio = new List<SteeLog.Domain.Movimientos.LineaMovimiento>();
            foreach (var x in s.Lineas)
            {
                if (x == null)
                {
                    lineasDominio.Add(null);
                    continue;
                }
                if (x.EsRecorte && (!x.RecorteId.HasValue || x.RecorteId.Value <= 0))
                {
                    if (!x.MaterialId.HasValue || x.MaterialId.Value <= 0 || x.EsInsumo || x.EsCompraEventual)
                        throw new ArgumentException("El recorte es inválido.");
                    var recorteValido = new SteeLog.Domain.Inventario.Recorte(0, x.MaterialId.Value, x.AnchoRecorte ?? 0m, x.AlturaRecorte ?? 0m, x.MedidaRecorte ?? 0m, 0m, null);
                    lineasDominio.Add(new SteeLog.Domain.Movimientos.LineaMovimiento(x.MaterialId, null, recorteValido.Medida > 0 ? (decimal?)recorteValido.Medida : null, x.Cantidad, false, null, x.PrecioUnitario));
                }
                else
                {
                    lineasDominio.Add(new SteeLog.Domain.Movimientos.LineaMovimiento(x.MaterialId, x.RecorteId, x.MedidaRecorte, x.Cantidad, x.CompraEventual, x.MaterialEventual, x.PrecioUnitario));
                }
            }
            var m = new SteeLog.Domain.Movimientos.Movimiento(c.Fecha, c.OrigenId, c.TipoOrigen, c.DestinoId, c.TipoDestino, c.TipoMovimientoId, c.Remito, c.ObraId, c.CostoFlete, c.FleteId, c.Observaciones, c.PrecioTotal, lineasDominio, s.Insumos, s.OrigenNombre, s.DestinoNombre);
            return new MovimientoSolicitud
            {
                Cabecera = new CabeceraMovimiento { Fecha = m.Fecha, Remito = m.Remito, OrigenId = m.OrigenId, TipoOrigen = m.TipoOrigen, DestinoId = m.DestinoId, TipoDestino = m.TipoDestino, TipoMovimientoId = m.TipoId, ObraId = m.ObraId, CostoFlete = m.CostoFlete, FleteId = m.FleteId, Observaciones = m.Observaciones, PrecioTotal = m.PrecioTotal },
                Lineas = s.Lineas.Where(x => x != null).Select(x => new LineaMovimiento
                {
                    MaterialId = x.MaterialId,
                    RecorteId = x.RecorteId > 0 ? x.RecorteId : null,
                    AnchoRecorte = x.AnchoRecorte,
                    AlturaRecorte = x.AlturaRecorte,
                    MedidaRecorte = x.MedidaRecorte,
                    Cantidad = x.Cantidad,
                    MaterialEventual = x.MaterialEventual,
                    EsRecorte = x.EsRecorte || (x.RecorteId.HasValue && x.RecorteId.Value > 0),
                    EsInsumo = x.EsInsumo,
                    InsumoId = x.InsumoId,
                    EsCompraEventual = x.EsCompraEventual,
                    PrecioUnitario = x.PrecioUnitario
                }).ToList(),
                Insumos = m.Insumos.ToList(),
                OrigenNombre = m.OrigenNombre,
                DestinoNombre = m.DestinoNombre
            };
        }

    }
    public sealed class RegistrarMovimiento
    {
        private readonly Session actor; private readonly MotorMovimiento motor;
        public RegistrarMovimiento(Session actor, IUnitOfWork uow, IMovimientosEscritura repo, IInventarioEscritura inventario)
        { this.actor = actor; motor = new MotorMovimiento(uow, repo, inventario); }
        public Task<int> EjecutarAsync(MovimientoSolicitud solicitud, CancellationToken token = default)
        { actor.Require(Role.Galpon); return motor.EjecutarAsync(null, solicitud, false, token); }
    }
    public sealed class EditarMovimiento
    {
        private readonly Session actor; private readonly MotorMovimiento motor;
        public EditarMovimiento(Session actor, IUnitOfWork uow, IMovimientosEscritura repo, IInventarioEscritura inventario)
        { this.actor = actor; motor = new MotorMovimiento(uow, repo, inventario); }
        public Task<int> EjecutarAsync(int id, MovimientoSolicitud solicitud, CancellationToken token = default)
        { actor.Require(Role.Admin); return motor.EjecutarAsync(id, solicitud, false, token); }
    }
    public sealed class EliminarMovimiento
    {
        private readonly Session actor; private readonly MotorMovimiento motor;
        public EliminarMovimiento(Session actor, IUnitOfWork uow, IMovimientosEscritura repo, IInventarioEscritura inventario)
        { this.actor = actor; motor = new MotorMovimiento(uow, repo, inventario); }
        public Task<int> EjecutarAsync(int id, CancellationToken token = default)
        { actor.Require(Role.Galpon); return motor.EjecutarAsync(id, null, true, token); }
    }
}
