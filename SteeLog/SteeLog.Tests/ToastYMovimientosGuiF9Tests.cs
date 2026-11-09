using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SteeLog;
using SteeLog.Modelos;
using SteeLog.Vistas.Movimientos;
using Xunit;

namespace SteeLog.Tests
{
    public class ToastYMovimientosGuiF9Tests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Campo<T>(object t, string n) => (T)t.GetType().GetField(n, Flags).GetValue(t);
        private static void Fijar(object t, string n, object v) => t.GetType().GetField(n, Flags).SetValue(t, v);
        private static void Sta(Action cuerpo)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { cuerpo(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(20))); Assert.Null(error);
        }

        [Fact]
        public void ToastEligeLaPantallaDeLaVentanaConDosPantallasSimuladas()
        {
            var primaria = new Rectangle(0, 0, 1920, 1040);
            var secundaria = new Rectangle(1920, 0, 1280, 984);
            var areas = new List<Rectangle> { primaria, secundaria };
            var m = typeof(ToastNotification).GetMethod("SeleccionarAreaTrabajo", BindingFlags.Static | BindingFlags.NonPublic);
            Func<Rectangle?, Rectangle> elegir = b => (Rectangle)m.Invoke(null, new object[] { b, areas, primaria });

            Assert.Equal(secundaria, elegir(new Rectangle(2000, 100, 800, 600)));
            Assert.Equal(secundaria, elegir(new Rectangle(1800, 100, 800, 600))); // mayor parte en la secundaria
            Assert.Equal(primaria, elegir(new Rectangle(100, 100, 800, 600)));
            Assert.Equal(primaria, elegir(null));                                  // sin ventana: principal
            Assert.Equal(primaria, elegir(new Rectangle(9000, 9000, 10, 10)));     // fuera de todo: principal
        }

        private static Point Pos(Rectangle area, Size t, int off, bool centrado) => (Point)typeof(ToastNotification).GetMethod("CalcularPosicionFinal", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { area, t, off, centrado });
        private static void AssertDentro(Rectangle area, Point p, Size t) => Assert.True(area.Contains(new Rectangle(p, t)), $"{p} {t} fuera de {area}");

        [Fact]
        public void ToastCentradoEnAreaConXNegativaQuedaDentroYCentrado()
        {
            var area = new Rectangle(-1600, 0, 1600, 860); var t = new Size(500, 180);
            var p = Pos(area, t, 0, true);
            AssertDentro(area, p, t);
            Assert.Equal(-1600 + (1600 - 500) / 2, p.X);
            Assert.Equal((860 - 180) / 2, p.Y);
        }

        [Fact]
        public void ToastBottomRightEnAreaConXNegativaQuedaDentro()
        {
            var area = new Rectangle(-1600, 0, 1600, 860); var t = new Size(380, 60);
            var p = Pos(area, t, 0, false);
            AssertDentro(area, p, t);
            Assert.Equal(0 - 380 - 30, p.X);
            Assert.Equal(860 - 60 - 30, p.Y);
        }

        [Fact]
        public void ToastCentradoEnMonitorSecundarioADerechaQuedaCentradoAhi()
        {
            var area = new Rectangle(1920, 0, 1280, 984); var t = new Size(500, 180);
            var p = Pos(area, t, 0, true);
            AssertDentro(area, p, t);
            Assert.Equal(1920 + (1280 - 500) / 2, p.X);
            Assert.Equal((984 - 180) / 2, p.Y);
        }

        [Fact]
        public void ApiladoEnormeQuedaAcotadoDentroDelArea()
        {
            foreach (var area in new[] { new Rectangle(-1600, 0, 1600, 860), new Rectangle(1920, 0, 1280, 984) })
            {
                var t = new Size(380, 60);
                var p = Pos(area, t, 100000, false);
                AssertDentro(area, p, t);
                Assert.Equal(area.Top, p.Y);
            }
        }

        [Fact]
        public void SeleccionDeVentanaIgnoraFormsHijosEmbebidosYNoVisibles()
        {
            Sta(() =>
            {
                using (var hijo = new Form { TopLevel = false, Visible = true })
                using (var noMostrado = new Form())
                using (var toast = new ToastNotification("x", ToastType.Info))
                {
                    var m = typeof(ToastNotification).GetMethod("EsVentanaValidaParaArea", BindingFlags.Static | BindingFlags.NonPublic);
                    Func<Form, bool> valido = f => (bool)m.Invoke(null, new object[] { f });
                    Assert.False(valido(hijo));       // TopLevel=false
                    Assert.False(valido(noMostrado)); // TopLevel pero no visible
                    Assert.False(valido(toast));
                    Assert.False(valido(null));
                    var desechado = new Form(); desechado.Dispose();
                    Assert.False(valido(desechado));
                }
            });
        }

        [Fact]
        public void PanelDeResultadosQuedaOcultoTrasAgregarLineaYUnaLetraSigueAvisandoElMinimo()
        {
            Sta(() =>
            {
                using (var vista = new Vista_Movimientos(new ServiciosVistaDePrueba()))
                {
                    var h = vista.Handle;
                    var buscar = vista.GetType().GetMethod("EjecutarBusqueda", Flags);
                    var panel = Campo<Panel>(vista, "_panelResultados");
                    buscar.Invoke(vista, new object[] { "a" });
                    Assert.Single(panel.Controls); // aviso de mínimo de 2 letras
                    vista.GetType().GetMethod("DeseleccionarItem", Flags).Invoke(vista, null);
                    Assert.Empty(panel.Controls);
                    Assert.False(panel.Visible);
                    buscar.Invoke(vista, new object[] { "a" });
                    Assert.Single(panel.Controls); // la búsqueda normal no cambia
                }
            });
        }

        [Fact]
        public void RuedaRespetaStockMaxYAvisaUnaSolaVezPorTope()
        {
            Sta(() =>
            {
                using (var vista = new Vista_Movimientos(new ServiciosVistaDePrueba()))
                {
                    var h = vista.Handle;
                    var avisos = new List<string>();
                    Fijar(vista, "AvisarStockMaximo", new Action<string>(m => avisos.Add(m)));
                    var tipos = new DataTable(); tipos.Columns.Add("id_tipomovimiento", typeof(int)); tipos.Columns.Add("nombre", typeof(string)); tipos.Rows.Add(4, "Salida");
                    var tipo = Campo<ComboBox>(vista, "cmbMovimientos"); tipo.DataSource = tipos; tipo.DisplayMember = "nombre"; tipo.ValueMember = "id_tipomovimiento"; tipo.SelectedIndex = 0;
                    var destino = Campo<ComboBox>(vista, "cmbDestino"); destino.DataSource = null; destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal = 2, Tipo = "UBICACION", TextoDisplay = "Depósito" }); destino.SelectedIndex = 0;
                    Fijar(vista, "_materialSeleccionado", new Material { id = 1, nombre = "M", stockActual = 5 });
                    var cant = Campo<Control>(vista, "cantMatMov");
                    var rueda = vista.GetType().GetMethod("cantMatMov_MouseWheel", Flags);
                    Func<int, object[]> evento = d => new object[] { cant, new MouseEventArgs(MouseButtons.None, 0, 0, 0, d) };

                    cant.Text = "4";
                    rueda.Invoke(vista, evento(120)); Assert.Equal("5", cant.Text); Assert.Empty(avisos);
                    rueda.Invoke(vista, evento(120)); Assert.Equal("5", cant.Text); Assert.Single(avisos);
                    rueda.Invoke(vista, evento(120)); Assert.Equal("5", cant.Text); Assert.Single(avisos); // mismo tope: sin repetir
                    rueda.Invoke(vista, evento(-120)); Assert.Equal("4", cant.Text);

                    // TextChanged: teclear valores excedidos repetidamente avisa una vez
                    cant.Text = "9"; Assert.Equal("5", cant.Text); Assert.Equal(2, avisos.Count);
                    cant.Text = "55"; Assert.Equal("5", cant.Text); Assert.Equal(2, avisos.Count);
                }
            });
        }
    }
}
