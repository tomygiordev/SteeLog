using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Modelos;
using SteeLog.Presentation;
using SteeLog.Vistas.Movimientos;
using SteeLog.Vistas.Movimientos.Ordenes;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CatalogosCacheYRecepcionTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private sealed class EjecutorCatalogos : IUseCaseExecutor
        {
            public int Llamadas; public CatalogosMovimiento Catalogos = new CatalogosMovimiento();
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> action) where TUseCase : class
            {
                Llamadas++;
                if (typeof(TResult) == typeof(CatalogosMovimiento)) return Task.FromResult((TResult)(object)Catalogos);
                if (typeof(TResult) == typeof(IReadOnlyList<MaterialInventarioLectura>)) return Task.FromResult((TResult)(object)new List<MaterialInventarioLectura>());
                if (typeof(TResult) == typeof(IReadOnlyList<CategoriaMaterialLectura>)) return Task.FromResult((TResult)(object)new List<CategoriaMaterialLectura>());
                if (typeof(TResult) == typeof(IReadOnlyList<RecorteLectura>)) return Task.FromResult((TResult)(object)new List<RecorteLectura>());
                throw new InvalidOperationException("Caso inesperado en test.");
            }
        }
        private static void EnSta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { accion(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(30))); Assert.Null(error);
        }
        private static T Campo<T>(object o, string n) => (T)o.GetType().GetField(n, Flags).GetValue(o);
        private static void Fijar(object o, string n, object v) => o.GetType().GetField(n, Flags).SetValue(o, v);
        private static void Esperar(Func<bool> c)
        {
            var reloj = Stopwatch.StartNew();
            while (!c() && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
        }
        private static OpcionMovimiento Obra(int id, string nombre) => new OpcionMovimiento { Id = id, Nombre = nombre, Tipo = "OBRA", Activo = true };
        private static Vista_Movimientos VistaListaParaRecarga(EjecutorCatalogos ejecutor, List<string> toasts)
        {
            var vista = new Vista_Movimientos(new ServiciosVistaDePrueba()); var h = vista.Handle;
            Fijar(vista, "presenter", new MovimientosPresenter(ejecutor, vista));
            Fijar(vista, "catalogosListos", true); Fijar(vista, "modeloListo", true);
            Fijar(vista, "NotificarGuardado", new Action<string>(toasts.Add));
            Fijar(vista, "AdvertirGuardado", new Action<string>(toasts.Add));
            Fijar(vista, "AdvertirErrorCarga", new Action<string>(toasts.Add));
            Fijar(vista, "AdvertirResultadoIncierto", new Action<string>(toasts.Add));
            return vista;
        }
        [Fact]
        public void MovimientosRecargaCatalogosAlReMostrarseYConservaCarritoYRemito()
        {
            EnSta(() => {
                var ejecutor = new EjecutorCatalogos(); var toasts = new List<string>();
                ejecutor.Catalogos.Destinos.Add(Obra(1, "Obra A"));
                using (var vista = VistaListaParaRecarga(ejecutor, toasts))
                {
                    var cmbObras = Campo<ComboBox>(vista, "cmbObras");
                    vista.RecargarCatalogos(); Esperar(() => cmbObras.Items.Count == 1 && Campo<bool>(vista, "catalogosListos"));
                    var carrito = Campo<List<MovimientoMaterial>>(vista, "materialesLista");
                    carrito.Add(new MovimientoMaterial { id_material = 5, cantidad = 2, NombreMaterial = "X" });
                    Campo<Control>(vista, "nroRemitoTxt").Text = "R-77";
                    cmbObras.SelectedValue = 1;
                    ejecutor.Catalogos = new CatalogosMovimiento();
                    ejecutor.Catalogos.Destinos.Add(Obra(1, "Obra A")); ejecutor.Catalogos.Destinos.Add(Obra(2, "Obra Nueva"));
                    bool incierto = Campo<bool>(vista, "resultadoIncierto");
                    vista.RecargarCatalogos(); Esperar(() => cmbObras.Items.Count == 2 && Campo<bool>(vista, "catalogosListos"));
                    Assert.Equal(2, cmbObras.Items.Count); Assert.Equal(1, (int)cmbObras.SelectedValue);
                    Assert.Single(carrito); Assert.Equal("R-77", Campo<Control>(vista, "nroRemitoTxt").Text);
                    Assert.Equal(incierto, Campo<bool>(vista, "resultadoIncierto")); Assert.False(Campo<bool>(vista, "envioPendiente"));
                    Assert.Empty(toasts);
                }
            });
        }
        [Fact]
        public void MovimientosNoRecargaSiEstaOcupadaOEnviando()
        {
            EnSta(() => {
                var ejecutor = new EjecutorCatalogos(); var toasts = new List<string>();
                using (var vista = VistaListaParaRecarga(ejecutor, toasts))
                {
                    Application.DoEvents();
                    int antes = ejecutor.Llamadas;
                    Fijar(vista, "ocupada", true); vista.RecargarCatalogos();
                    Fijar(vista, "ocupada", false); Fijar(vista, "envioPendiente", true); vista.RecargarCatalogos();
                    Fijar(vista, "envioPendiente", false);
                    Application.DoEvents(); Thread.Sleep(50); Application.DoEvents();
                    Assert.Equal(antes, ejecutor.Llamadas);
                }
            });
        }
        [Fact]
        public void LimpiarDesmarcaEventualYOcultaPrecio()
        {
            EnSta(() => {
                var servicios = new ServiciosVistaDePrueba(); servicios.Sesion.SetSession("admin@tesis.local");
                try
                {
                    using (var vista = new Vista_Movimientos(servicios))
                    {
                        var h = vista.Handle;
                        var chk = Campo<Control>(vista, "chkCompraEventual"); var checkedProp = chk.GetType().GetProperty("Checked"); checkedProp.SetValue(chk, true);
                        var precio = Campo<Control>(vista, "txtPrecioMaterial"); precio.Text = "123";
                        var cols = Campo<TableLayoutPanel>(vista, "panelAntiParpadeo4"); Assert.Equal(70F, cols.ColumnStyles[6].Width);
                        vista.GetType().GetMethod("LimpiarFormulario", Flags).Invoke(vista, null);
                        Assert.False((bool)checkedProp.GetValue(chk)); Assert.Equal(0F, cols.ColumnStyles[6].Width); Assert.Equal("", precio.Text);
                    }
                }
                finally { servicios.Sesion.Clear(); }
            });
        }
        [Fact]
        public void RecepcionConValorExcedidoRevierteCeldaYResumen()
        {
            EnSta(() => {
                using (var revision = new Vista_OrdenesRevision(new ServiciosVistaDePrueba()))
                {
                    var h = revision.Handle; var avisos = new List<string>();
                    Fijar(revision, "AdvertirEdicion", new Action<string>(avisos.Add));
                    var detalles = Campo<BindingList<OrdenCompraDetalle>>(revision, "_detalles");
                    var d = new OrdenCompraDetalle { id_detalle = 1, material_nombre = "M", cantidad_pedida = 12, cantidad_recibida = 4, cantidad_faltante = 8 };
                    detalles.Add(d);
                    Campo<Dictionary<OrdenCompraDetalle, decimal>>(revision, "_recibidaValida")[d] = 4;
                    var grilla = Campo<DataGridView>(revision, "dgvDetalles"); Fijar(revision, "_indiceActual", 0);
                    grilla.DataSource = null; grilla.DataSource = detalles;
                    int col = grilla.Columns["colRecibida"].Index;
                    var finEdicion = revision.GetType().GetMethod("DgvDetalles_CellEndEdit", Flags);
                    // el DataBinding ya escribió el valor rechazado en el objeto antes de CellEndEdit
                    d.cantidad_recibida = 99; grilla.Rows[0].Cells[col].Value = 99m;
                    finEdicion.Invoke(revision, new object[] { grilla, new DataGridViewCellEventArgs(col, 0) });
                    Assert.Equal(4m, d.cantidad_recibida); Assert.Equal(8m, d.cantidad_faltante);
                    Assert.Equal(4m, Convert.ToDecimal(grilla.Rows[0].Cells[col].Value));
                    Assert.Equal(8m, Convert.ToDecimal(grilla.Rows[0].Cells["colFaltante"].Value));
                    var rotulo = Campo<Label>(revision, "lblPendienteActual");
                    Assert.Contains("Recibida: 4", rotulo.Text); Assert.Contains("Faltante: 8", rotulo.Text);
                    Assert.Single(avisos);
                    // edición válida: el rótulo de cabecera se actualiza
                    d.cantidad_recibida = 10; grilla.Rows[0].Cells[col].Value = 10m;
                    finEdicion.Invoke(revision, new object[] { grilla, new DataGridViewCellEventArgs(col, 0) });
                    Assert.Equal(2m, d.cantidad_faltante);
                    Assert.Contains("Recibida: 10", rotulo.Text); Assert.Contains("Faltante: 2", rotulo.Text);
                }
            });
        }
        [Fact]
        public void ListadoNoPendienteNoMuestraBotonEliminarVacio()
        {
            EnSta(() => {
                using (var listado = new Vista_OrdenesListado(new ServiciosVistaDePrueba()))
                {
                    var h = listado.Handle;
                    var grilla = Campo<DataGridView>(listado, "dgvOrdenes");
                    grilla.DataSource = new List<OrdenCompra> {
                        new OrdenCompra { id_orden_compra = 1, estado = SteeLog.Domain.OrdenesCompra.EstadosOrden.Pendiente },
                        new OrdenCompra { id_orden_compra = 2, estado = SteeLog.Domain.OrdenesCompra.EstadosOrden.RecibidaCompleta } };
                    listado.GetType().GetMethod("AplicarAccionesGrilla", Flags).Invoke(listado, null);
                    Assert.IsType<DataGridViewButtonCell>(grilla.Rows[0].Cells["colEliminar"]);
                    Assert.Equal("Eliminar", grilla.Rows[0].Cells["colEliminar"].Value);
                    Assert.IsNotType<DataGridViewButtonCell>(grilla.Rows[1].Cells["colEliminar"]);
                    Assert.Equal("", grilla.Rows[1].Cells["colEliminar"].Value);
                    ((OrdenCompra)grilla.Rows[1].DataBoundItem).estado = SteeLog.Domain.OrdenesCompra.EstadosOrden.Pendiente;
                    listado.GetType().GetMethod("AplicarAccionesGrilla", Flags).Invoke(listado, null);
                    Assert.IsType<DataGridViewButtonCell>(grilla.Rows[1].Cells["colEliminar"]);
                    Assert.Equal("Eliminar", grilla.Rows[1].Cells["colEliminar"].Value);
                }
            });
        }
        [Fact]
        public void DialogoCambiarMaterialTieneLosBotonesDentroDelCliente()
        {
            EnSta(() => {
                var tipo = typeof(Vista_OrdenesAlta).GetNestedType("ModalCambioMaterialOrden", BindingFlags.NonPublic);
                using (var modal = (Form)Activator.CreateInstance(tipo, new object[] { new ServiciosVistaDePrueba(), new List<Material>(), 0 }))
                {
                    var h = modal.Handle;
                    var a = (Control)tipo.GetField("btnAceptar", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(modal);
                    var c = (Control)tipo.GetField("btnCancelar", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(modal);
                    foreach (var b in new[] { a, c })
                    {
                        Assert.True(b.Right + 18 <= modal.ClientSize.Width, b.Text + " sobresale: " + b.Right + " > " + modal.ClientSize.Width);
                        Assert.True(b.Bottom <= modal.ClientSize.Height);
                    }
                    Assert.True(c.Right < a.Left);
                }
            });
        }
    }
}
