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
    // Regla de la vista: en un movimiento que consume stock, lo cargado en el carrito de un material
    // o de un recorte nunca puede superar su stock disponible. No toca el motor ni la persistencia.
    public class MovimientosStockCarritoTests
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

        [Fact]
        public void SumarElMismoMaterialEnConsumoQuedaTopeadoPorLoDisponibleMenosLoYaCargado()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    var avisosTope = new List<string>();
                    Fijar(c.Vista, "AvisarStockMaximo", new Action<string>(m => avisosTope.Add(m)));
                    AgregarMaterial(c, 3);
                    Assert.Equal(3, c.Carrito.Single().cantidad);
                    Assert.Empty(avisosTope);
                    // Tipear 3 más (3 + 3 > 5): el tope es lo que queda, 2, no el stock total.
                    AgregarMaterial(c, 3);
                    Assert.Single(avisosTope);
                    Assert.Contains("2", avisosTope[0]);
                    Assert.Equal(5, c.Carrito.Single().cantidad);
                }
            });
        }

        [Fact]
        public void RecorteYaEnCarritoNoPuedeSumarMasDeLoDisponibleEnConsumo()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    var recorte = new Recorte { id = 7, id_material = 1, ancho = 2, altura = 1, medida = 0, cantidad = 5 };
                    Assert.Null(ValidarRecorte(c, recorte, 3));
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil (R: 2x1)", EsRecorte = true, IdRecorte = 7, AnchoRecorte = 2, AlturaRecorte = 1, MedidaRecorte = 0, cantidad = 3 });
                    Assert.NotNull(ValidarRecorte(c, recorte, 3));   // 3 + 3 > 5
                    Assert.Null(ValidarRecorte(c, recorte, 2));      // 3 + 2 == 5
                    // Otro recorte del mismo material no se ve afectado.
                    Assert.Null(ValidarRecorte(c, new Recorte { id = 8, id_material = 1, ancho = 1, altura = 1, cantidad = 5 }, 5));
                }
            });
        }

        [Fact]
        public void RecorteNuevoSeAceptaEnConsumoSinTopeYElExistenteSigueValidado()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    // Un recorte nuevo nace con lo que se carga: no hay tope.
                    var nuevo = new Recorte { id = 0, id_material = 1, ancho = 2, altura = 3, medida = 0, cantidad = 0 };
                    Assert.Null(ValidarRecorte(c, nuevo, 19));
                    c.Tipo.SelectedIndex = 1; // Compra: entra stock
                    Assert.Null(ValidarRecorte(c, nuevo, 19));
                    c.Tipo.SelectedIndex = 0;
                    // Uno que ya existe (mismas medidas) se valida contra su stock real.
                    Assert.NotNull(ValidarRecorte(c, new Recorte { id = 9, id_material = 1, ancho = 2, altura = 3, cantidad = 5 }, 19));
                }
            });
        }

        [Fact]
        public void RecorteNuevoEnElCarritoDeUnConsumoNoExcedeStockNiBloqueaElGuardado()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil (R: 2x3)", EsRecorte = true, IdRecorte = null, AnchoRecorte = 2, AlturaRecorte = 3, MedidaRecorte = 0, cantidad = 19 });
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil (R: 2x3)", EsRecorte = true, IdRecorte = 0, AnchoRecorte = 2, AlturaRecorte = 3, MedidaRecorte = 0, cantidad = 4 });
                    c.Guardar();
                    Assert.Empty(c.Avisos);
                    Assert.Equal(1, c.Confirmaciones);
                }
            });
        }

        [Fact]
        public void AgregarRecorteNuevoAlCarritoEnConsumoNoTienePorQueTopearseNiRechazarse()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    var avisosTope = new List<string>();
                    Fijar(c.Vista, "AvisarStockMaximo", new Action<string>(m => avisosTope.Add(m)));
                    Fijar(c.Vista, "_materialSeleccionado", Campo<List<Material>>(c.Vista, "materialesFull").First());
                    Fijar(c.Vista, "_anchoRecorteSeleccionado", 2m); Fijar(c.Vista, "_alturaRecorteSeleccionado", 3m);
                    Campo<Control>(c.Vista, "cantMatMov").Text = "19";
                    c.Vista.GetType().GetMethod("btnAgregarMaterial_Click", Flags).Invoke(c.Vista, new object[] { c.Vista, EventArgs.Empty });
                    var item = c.Carrito.Single();
                    Assert.True(item.EsRecorte);
                    Assert.Equal(19, item.cantidad);
                    Assert.Empty(avisosTope);
                    Assert.Equal(7m, (decimal)c.Vista.GetType().GetMethod("LimitarCantidadAStock", Flags).Invoke(c.Vista, new object[] { 7m }));
                    Assert.Empty(avisosTope);
                }
            });
        }

        [Fact]
        public void AvisoDeRecorteOcultaCantidadesSegunElRolYLasMuestraParaAdmin()
        {
            Sta(() => {
                var oculto = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra");
                using (oculto.Vista)
                {
                    var msg = ValidarRecorte(oculto, new Recorte { id = 7, id_material = 1, ancho = 2, altura = 1, cantidad = 5 }, 9);
                    Assert.Equal("Stock de recorte insuficiente.", msg);
                }
                var servicios = new ServiciosVistaDePrueba(); servicios.Sesion.SetSession("admin@tesis.local");
                var visible = Preparar(servicios, "Envio a Obra", "Envio a Obra");
                using (visible.Vista)
                {
                    var msg = ValidarRecorte(visible, new Recorte { id = 7, id_material = 1, ancho = 2, altura = 1, cantidad = 5 }, 9);
                    Assert.Equal("Stock de recorte insuficiente. Disponible: 5", msg);
                }
            });
        }

        private static ModalRecortes CrearModal(Func<Recorte, decimal, string> validar)
        {
            var ctor = typeof(ModalRecortes).GetConstructors().Single();
            var args = ctor.GetParameters().Length == 4
                ? new object[] { new ServiciosVistaDePrueba(), 1, "Perfil", validar }
                : new object[] { new ServiciosVistaDePrueba(), 1, "Perfil" };
            var modal = (ModalRecortes)ctor.Invoke(args);
            var h = modal.Handle;
            typeof(ModalRecortes).GetField("AdvertirCarrito", Flags).SetValue(modal, new Action<string>(m => { }));
            return modal;
        }

        private static void CargarGrilla(ModalRecortes modal, params Recorte[] recortes)
        {
            var grilla = (DataGridView)typeof(ModalRecortes).GetField("dgvRecortesDisponibles", Flags).GetValue(modal);
            grilla.DataSource = recortes.ToList();
            grilla.Rows[0].Selected = true;
            grilla.CurrentCell = grilla.Rows[0].Cells[0];
        }

        [Fact]
        public void ModalUsarRecorteConsultaAlCarritoYNoCierraSiExcede()
        {
            Sta(() => {
                var consultas = new List<decimal>();
                using (var modal = CrearModal((r, cant) => { consultas.Add(cant); return cant > 2 ? "Stock de recorte insuficiente." : null; }))
                {
                    CargarGrilla(modal, new Recorte { id = 7, id_material = 1, ancho = 2, altura = 1, cantidad = 5 });
                    var txt = (Control)typeof(ModalRecortes).GetField("txtCantidad", Flags).GetValue(modal);
                    var usar = typeof(ModalRecortes).GetMethod("btnSeleccionarRecorte_Click", Flags);
                    txt.Text = "3";
                    usar.Invoke(modal, new object[] { modal, EventArgs.Empty });
                    Assert.NotEqual(DialogResult.OK, modal.DialogResult);
                    Assert.Null(modal.RecorteSeleccionado);
                    txt.Text = "2";
                    usar.Invoke(modal, new object[] { modal, EventArgs.Empty });
                    Assert.Equal(DialogResult.OK, modal.DialogResult);
                    Assert.Equal(2m, modal.CantidadSeleccionada);
                    Assert.Equal(new decimal[] { 3, 2 }, consultas.ToArray());
                }
            });
        }

        [Fact]
        public void ModalCrearYUsarTambienValidaElRecorteNuevo()
        {
            Sta(() => {
                Recorte consultado = null;
                using (var modal = CrearModal((r, cant) => { consultado = r; return "Stock de recorte insuficiente."; }))
                {
                    var t = typeof(ModalRecortes);
                    ((Control)t.GetField("txtAnchoBuscado", Flags).GetValue(modal)).Text = "2";
                    ((Control)t.GetField("txtAlturaBuscada", Flags).GetValue(modal)).Text = "3";
                    ((Control)t.GetField("txtCantidad", Flags).GetValue(modal)).Text = "19";
                    t.GetMethod("btnCrearNuevo_Click", Flags).Invoke(modal, new object[] { modal, EventArgs.Empty });
                    Assert.NotEqual(DialogResult.OK, modal.DialogResult);
                    Assert.Null(modal.RecorteSeleccionado);
                    Assert.Equal(0, consultado.id);
                    Assert.Equal(0m, consultado.cantidad);
                }
            });
        }

        [Fact]
        public void ModalCrearYUsarConMedidasDeUnRecorteExistenteLoValidaComoExistente()
        {
            Sta(() => {
                Recorte consultado = null;
                using (var modal = CrearModal((r, cant) => { consultado = r; return null; }))
                {
                    var t = typeof(ModalRecortes);
                    CargarGrilla(modal, new Recorte { id = 9, id_material = 1, ancho = 2, altura = 3, medida = 0, cantidad = 5 });
                    ((Control)t.GetField("txtAnchoBuscado", Flags).GetValue(modal)).Text = "2";
                    ((Control)t.GetField("txtAlturaBuscada", Flags).GetValue(modal)).Text = "3";
                    ((Control)t.GetField("txtCantidad", Flags).GetValue(modal)).Text = "4";
                    t.GetMethod("btnCrearNuevo_Click", Flags).Invoke(modal, new object[] { modal, EventArgs.Empty });
                    Assert.Equal(9, consultado.id);
                    Assert.Equal(5m, consultado.cantidad);
                    Assert.Equal(9, modal.RecorteSeleccionado.id);
                }
            });
        }

        [Fact]
        public void CargarSinTipoYLuegoElegirConsumoAvisaSinTocarElCarritoYElGuardadoNoLlegaALaConfirmacion()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), null, "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    Fijar(c.Vista, "_materialSeleccionado", Campo<List<Material>>(c.Vista, "materialesFull").First());
                    Campo<Control>(c.Vista, "cantMatMov").Text = "3";
                    c.Vista.GetType().GetMethod("btnAgregarMaterial_Click", Flags).Invoke(c.Vista, new object[] { c.Vista, EventArgs.Empty });
                    Assert.Equal(3, c.Carrito.Single().cantidad); // sin tipo todavía no se valida
                    c.Carrito.Single().cantidad = 8;
                    Assert.Empty(c.Avisos);

                    c.Tipo.SelectedIndex = 0; // Envio a Obra: consume stock
                    Assert.Single(c.Avisos);
                    Assert.Contains("Stock insuficiente", c.Avisos[0]);
                    Assert.Equal(8, c.Carrito.Single().cantidad); // el carrito queda intacto

                    c.Avisos.Clear();
                    c.Guardar();
                    Assert.Equal(0, c.Confirmaciones);
                    Assert.Equal(0, c.Ejecutor.Llamadas);
                    Assert.Single(c.Avisos);
                    Assert.Equal(8, c.Carrito.Single().cantidad);
                }
            });
        }

        [Fact]
        public void AvisoDelCarritoExcedidoOcultaCantidadesParaRolesRestringidos()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Compra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil", cantidad = 8 });
                    c.Tipo.SelectedIndex = 0;
                    Assert.Single(c.Avisos);
                    Assert.DoesNotContain("8", c.Avisos[0]);
                    Assert.DoesNotContain("5", c.Avisos[0]);
                    Assert.Contains("Perfil", c.Avisos[0]);
                }
                var servicios = new ServiciosVistaDePrueba(); servicios.Sesion.SetSession("admin@tesis.local");
                var a = Preparar(servicios, "Compra", "Envio a Obra", "Compra");
                using (a.Vista)
                {
                    CargarStock(a, 5);
                    a.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil", cantidad = 8 });
                    a.Tipo.SelectedIndex = 0;
                    Assert.Contains("cargado 8, disponible 5", a.Avisos.Single());
                }
            });
        }

        [Fact]
        public void MovimientoDeEntradaConCantidadesMayoresAlStockSigueLlegandoALaConfirmacion()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Compra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    AgregarMaterial(c, 3);
                    AgregarMaterial(c, 9);
                    Assert.Equal(12, c.Carrito.Single().cantidad);
                    Assert.Empty(c.Avisos);
                    c.Guardar();
                    Assert.Empty(c.Avisos);
                    Assert.Equal(1, c.Confirmaciones);
                }
            });
        }

        [Fact]
        public void ConsumoValidoSigueLlegandoALaConfirmacionYEventualesNoSeValidan()
        {
            Sta(() => {
                var c = Preparar(new ServiciosVistaDePrueba(), "Envio a Obra", "Envio a Obra", "Compra");
                using (c.Vista)
                {
                    CargarStock(c, 5);
                    c.Carrito.Add(new MovimientoMaterial { id_material = 1, NombreMaterial = "Perfil", cantidad = 5 });
                    c.Carrito.Add(new MovimientoMaterial { id_material = null, NombreMaterial = "Eventual", NombreMaterialEventual = "Eventual", EsCompraEventual = true, cantidad = 500 });
                    c.Guardar();
                    Assert.Empty(c.Avisos);
                    Assert.Equal(1, c.Confirmaciones);
                }
            });
        }
    }
}
