using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Modelos;
using SteeLog.Presentation;
using SteeLog.Vistas.Movimientos;
using Xunit;

namespace SteeLog.Tests
{
    public class MovimientosEdicionStockTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Campo<T>(object t, string n) => (T)t.GetType().GetField(n, Flags).GetValue(t);
        private static void Fijar(object t, string n, object v) => t.GetType().GetField(n, Flags).SetValue(t, v);
        private static void Sta(Action cuerpo)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { cuerpo(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(30))); Assert.Null(error);
        }

        private sealed class EjecutorContador : IUseCaseExecutor
        {
            public int Llamadas;
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> action) where TUseCase : class
            { Llamadas++; throw new InvalidOperationException("No debía llegar al presenter."); }
        }

        private sealed class Contexto
        {
            public Vista_Movimientos Vista; public EjecutorContador Ejecutor = new EjecutorContador();
            public List<string> Avisos = new List<string>(); public int Confirmaciones;
            public List<MovimientoMaterial> Carrito => Campo<List<MovimientoMaterial>>(Vista, "materialesLista");
            public ComboBox Tipo => Campo<ComboBox>(Vista, "cmbMovimientos");
            public void Guardar() => Vista.GetType().GetMethod("GuardarMovimiento_Click", Flags).Invoke(Vista, new object[] { Vista, EventArgs.Empty });
        }

        private static Contexto Preparar(ServiciosVistaDePrueba servicios, string tipoInicial, params string[] tipos)
        {
            var c = new Contexto { Vista = new Vista_Movimientos(servicios) };
            var vista = c.Vista; var handle = vista.Handle;
            Fijar(vista, "presenter", new MovimientosPresenter(c.Ejecutor, vista));
            Fijar(vista, "catalogosListos", true); Fijar(vista, "modeloListo", true);
            Fijar(vista, "ConfirmarGuardado", new Func<DialogResult>(() => { c.Confirmaciones++; return DialogResult.No; }));
            Fijar(vista, "VersionBorrador", new Func<string>(() => null));
            Fijar(vista, "GuardarBorradorActual", new Action<SteeLog.Core.Application.Borradores.MovimientoDraft>(d => { }));
            Fijar(vista, "AdvertirGuardado", new Action<string>(m => c.Avisos.Add(m)));
            var dt = new DataTable(); dt.Columns.Add("id_tipomovimiento", typeof(int)); dt.Columns.Add("nombre", typeof(string));
            for (int i = 0; i < tipos.Length; i++) dt.Rows.Add(i + 1, tipos[i]);
            var tipo = c.Tipo; tipo.DataSource = dt; tipo.DisplayMember = "nombre"; tipo.ValueMember = "id_tipomovimiento";
            tipo.SelectedIndex = tipoInicial == null ? -1 : Array.IndexOf(tipos, tipoInicial);
            var origen = Campo<ComboBox>(vista, "cmbOrigen"); origen.DataSource = null; origen.Items.Clear();
            origen.Items.Add(new Vista_Movimientos.ItemCombo { IdReal = 1, Tipo = "UBICACION", TextoDisplay = "Galpón" }); origen.SelectedIndex = 0;
            var destino = Campo<ComboBox>(vista, "cmbDestino"); destino.DataSource = null; destino.Items.Clear();
            destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal = 2, Tipo = "UBICACION", TextoDisplay = "Depósito" }); destino.SelectedIndex = 0;
            Campo<Control>(vista, "nroRemitoTxt").Text = "123";
            c.Avisos.Clear();
            return c;
        }

        private static void CargarStock(Contexto c, decimal stockMaterial)
        {
            Fijar(c.Vista, "materialesFull", new List<Material> { new Material { id = 1, nombre = "Perfil", stockActual = stockMaterial } });
        }

        private static void AgregarMaterial(Contexto c, decimal cantidad)
        {
            Fijar(c.Vista, "_materialSeleccionado", Campo<List<Material>>(c.Vista, "materialesFull").First());
            Campo<Control>(c.Vista, "cantMatMov").Text = cantidad.ToString(System.Globalization.CultureInfo.InvariantCulture);
            c.Vista.GetType().GetMethod("btnAgregarMaterial_Click", Flags).Invoke(c.Vista, new object[] { c.Vista, EventArgs.Empty });
        }

        private static string ValidarRecorte(Contexto c, Recorte r, decimal cantidad)
            => (string)c.Vista.GetType().GetMethod("ValidarRecorteParaCarrito", Flags).Invoke(c.Vista, new object[] { r, cantidad });

        private static void Capturar(Contexto c, params SteeLog.Core.Application.Movimientos.LineaEdicion[] lineas)
        {
            Fijar(c.Vista, "_idMovimientoAEditar", 42);
            var edicion = new SteeLog.Core.Application.Movimientos.MovimientoEdicion
            {
                Cabecera = new SteeLog.Core.Application.Movimientos.CabeceraMovimiento { TipoDestino = "UBICACION" },
                Lineas = lineas.ToList()
            };
            c.Vista.GetType().GetMethod("CapturarStockRevertido", Flags).Invoke(c.Vista, new object[] { edicion, "Envio a Obra" });
        }

        private static SteeLog.Core.Application.Movimientos.LineaEdicion Original(bool recorte, decimal? impacto = -5m)
            => new SteeLog.Core.Application.Movimientos.LineaEdicion
            {
                Linea = new SteeLog.Core.Application.Movimientos.LineaMovimiento
                { MaterialId = 1, RecorteId = recorte ? (int?)7 : null, Cantidad = 5, ImpactoReal = impacto }
            };

        [Fact]
        public void MaterialOriginalCincoConStockDosPermiteSietePeroNoOcho()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra");
                using (c.Vista)
                {
                    CargarStock(c, 2);
                    Capturar(c, Original(false));
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil", cantidad = 5 });
                    var avisos = new List<string>();
                    Fijar(c.Vista, "AvisarStockMaximo", new Action<string>(avisos.Add));
                    AgregarMaterial(c, 2);
                    Assert.Equal(7m, c.Carrito.Single().cantidad);
                    Assert.Empty(avisos);
                    Fijar(c.Vista, "_materialSeleccionado", Campo<List<Material>>(c.Vista, "materialesFull").Single());
                    Campo<Control>(c.Vista, "cantMatMov").Text = "1";
                    Assert.Single(avisos);
                    Assert.Equal("0", Campo<Control>(c.Vista, "cantMatMov").Text);
                    Assert.Equal(7m, c.Carrito.Single().cantidad);
                    Assert.Equal(7m, (decimal)c.Vista.GetType().GetMethod("StockDisponibleMaterial", Flags).Invoke(c.Vista, new object[] { 1 }));
                    c.Guardar();
                    Assert.Empty(c.Avisos);
                    Assert.Equal(1, c.Confirmaciones);
                }
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GuardadoExcedidoEnEdicionNoLlegaAConfirmacionNiPresenter(bool recorte)
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra");
                using (c.Vista)
                {
                    CargarStock(c, 2);
                    Fijar(c.Vista, "recortesDisponibles", new List<Recorte> { new Recorte { id = 7, id_material = 1, cantidad = 2 } });
                    Capturar(c, Original(recorte));
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil", EsRecorte = recorte, IdRecorte = recorte ? (int?)7 : null, cantidad = 8 });
                    c.Guardar();
                    Assert.Equal(0, c.Confirmaciones);
                    Assert.Equal(0, c.Ejecutor.Llamadas);
                    Assert.Single(c.Avisos);
                    Assert.Contains("Stock insuficiente", c.Avisos.Single());
                    Assert.DoesNotContain("8", c.Avisos.Single());
                    Assert.DoesNotContain("7", c.Avisos.Single());
                    Assert.Equal(8m, c.Carrito.Single().cantidad);
                }
            });
        }

        [Fact]
        public void RecorteOriginalCincoConStockDosPermiteSietePeroNoOcho()
        {
            Sta(() => {
                var servicios = new ServiciosVistaDePrueba(); servicios.Sesion.SetSession("admin@tesis.local");
                var c = Preparar(servicios, "Envio a Obra", "Envio a Obra");
                using (c.Vista)
                {
                    Capturar(c, Original(true));
                    var r = new Recorte { id = 7, id_material = 1, cantidad = 2 };
                    Assert.Null(ValidarRecorte(c, r, 7));
                    Assert.Equal("Stock de recorte insuficiente. Disponible: 7", ValidarRecorte(c, r, 8));
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, EsRecorte = true, IdRecorte = 7, cantidad = 5 });
                    Assert.Null(ValidarRecorte(c, r, 2));
                    Assert.NotNull(ValidarRecorte(c, r, 3));
                    c.Carrito.Single().cantidad = 7;
                    Assert.NotNull(ValidarRecorte(c, r, 1));
                    Fijar(c.Vista, "_recorteSeleccionado", r);
                    Assert.Equal(7m, (decimal)c.Vista.GetType().GetMethod("StockDisponibleRecorte", Flags).Invoke(c.Vista, new object[] { 7 }));
                    Assert.Single(c.Carrito);
                    Assert.Equal(7m, c.Carrito.Single().cantidad);
                }
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReemplazarItemOriginalDescuentaSoloLosOtrosItems(bool recorte)
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra");
                using (c.Vista)
                {
                    CargarStock(c, 2);
                    Capturar(c, Original(recorte));
                    var r = new Recorte { id = 7, id_material = 1, cantidad = 2 };
                    Fijar(c.Vista, "recortesDisponibles", new List<Recorte> { r });
                    Fijar(c.Vista, "_materialSeleccionado", Campo<List<Material>>(c.Vista, "materialesFull").Single());
                    if (recorte) Fijar(c.Vista, "_idRecorteSeleccionado", 7);
                    var item = new MovimientoMaterial { id_material = 1, EsRecorte = recorte, IdRecorte = recorte ? (int?)7 : null, cantidad = 5 };
                    c.Carrito.Add(item);
                    using (var panel = new Panel { Tag = item })
                    {
                        Fijar(c.Vista, "panelSeleccionado", panel);
                        var avisos = new List<string>();
                        Fijar(c.Vista, "AvisarStockMaximo", new Action<string>(avisos.Add));
                        Campo<Control>(c.Vista, "cantMatMov").Text = "7";
                        Assert.Equal("7", Campo<Control>(c.Vista, "cantMatMov").Text);
                        Assert.Empty(avisos);
                        Campo<Control>(c.Vista, "cantMatMov").Text = "8";
                        Assert.Single(avisos);
                        Assert.Equal("7", Campo<Control>(c.Vista, "cantMatMov").Text);
                        Assert.Equal(5m, c.Carrito.Single().cantidad);
                    }
                    Fijar(c.Vista, "panelSeleccionado", null);
                }
            });
        }

        [Theory]
        [InlineData(null, 5)]
        [InlineData(-3, 3)]
        [InlineData(0, 0)]
        [InlineData(4, -4)]
        public void SnapshotUsaImpactoRealOFallbackYExcluyeEventualesEInsumos(int? efecto, int devuelto)
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra");
                using (c.Vista)
                {
                    var material = Original(false, efecto);
                    var eventual = Original(false); eventual.Linea.CompraEventual = true;
                    var insumo = Original(false); insumo.InsumoId = 8;
                    Capturar(c, material, Original(true, efecto), eventual, insumo);
                    Assert.Equal((decimal)devuelto, Campo<Dictionary<int, decimal>>(c.Vista, "stockRevertidoMateriales")[1]);
                    Assert.Equal((decimal)devuelto, Campo<Dictionary<int, decimal>>(c.Vista, "stockRevertidoRecortes")[7]);
                    material.Linea.Cantidad = 500;
                    Assert.Equal((decimal)devuelto, Campo<Dictionary<int, decimal>>(c.Vista, "stockRevertidoMateriales")[1]);
                    c.Vista.GetType().GetMethod("LimpiarFormulario", Flags).Invoke(c.Vista, null);
                    Assert.Empty(Campo<Dictionary<int, decimal>>(c.Vista, "stockRevertidoMateriales"));
                    Assert.Empty(Campo<Dictionary<int, decimal>>(c.Vista, "stockRevertidoRecortes"));
                }
            });
        }
    }
}
