using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SteeLog.Modelos;
using SteeLog.Vistas.Movimientos;
using Xunit;

namespace SteeLog.Tests
{
    public class ModalRecortesCantidadesOcultasTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Campo<T>(ModalRecortes modal, string nombre)
            => (T)typeof(ModalRecortes).GetField(nombre, Flags).GetValue(modal);

        private static void Invocar(ModalRecortes modal, string nombre)
            => typeof(ModalRecortes).GetMethod(nombre, Flags).Invoke(modal, new object[] { modal, EventArgs.Empty });

        private static void Sta(Action cuerpo)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { cuerpo(); } catch (Exception ex) { error = ex; } });
            hilo.SetApartmentState(ApartmentState.STA);
            hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(30)));
            Assert.Null(error);
        }

        private static ModalRecortes CrearModal(bool admin, List<string> avisos)
        {
            var servicios = new ServiciosVistaDePrueba();
            servicios.Sesion.SetSession(admin ? "admin@tesis.local" : "galpon@tesis.local");
            var modal = new ModalRecortes(servicios, 1, "Perfil");
            var handle = modal.Handle;
            typeof(ModalRecortes).GetField("AdvertirCarrito", Flags)
                .SetValue(modal, new Action<string>(avisos.Add));
            return modal;
        }

        private static DataGridView CargarGrilla(ModalRecortes modal, decimal stock)
        {
            var grilla = Campo<DataGridView>(modal, "dgvRecortesDisponibles");
            grilla.DataSource = new List<Recorte> { new Recorte { id = 7, id_material = 1, ancho = 2, cantidad = stock } };
            grilla.CurrentCell = grilla.Rows[0].Cells[0];
            grilla.Rows[0].Selected = true;
            return grilla;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExcesoRespetaVisibilidadDeCantidadesDelRol(bool admin)
        {
            Sta(() =>
            {
                var avisos = new List<string>();
                using (var modal = CrearModal(admin, avisos))
                {
                    CargarGrilla(modal, 5m);
                    var cantidad = Campo<Control>(modal, "txtCantidad");
                    cantidad.Text = "9";
                    Invocar(modal, "btnSeleccionarRecorte_Click");
                    Assert.Equal(admin
                        ? "La cantidad (9) supera el stock disponible (5)."
                        : "La cantidad supera el stock disponible del recorte.", avisos.Single());
                    if (!admin) Assert.DoesNotContain(avisos.Single(), char.IsDigit);
                    Assert.Equal(admin ? "5" : "9", cantidad.Text);
                    Assert.Null(modal.RecorteSeleccionado);
                    Assert.NotEqual(DialogResult.OK, modal.DialogResult);

                    var sumarTodo = Campo<Control>(modal, "btnSumarTodo");
                    // El modal no se muestra (Shown carga datos); separar el botón evita heredar Visible=false del formulario.
                    sumarTodo.Parent.Controls.Remove(sumarTodo);
                    using (sumarTodo) Assert.Equal(admin, sumarTodo.Visible);
                }
            });
        }

        [Theory]
        [InlineData(false, 5, "Hay stock")]
        [InlineData(false, 0, "Sin stock")]
        [InlineData(true, 5, "5")]
        public void GrillaFormateaStockSinCambiarElDato(bool admin, int stock, string esperado)
        {
            Sta(() =>
            {
                using (var modal = CrearModal(admin, new List<string>()))
                {
                    var grilla = CargarGrilla(modal, stock);
                    Assert.Equal(admin ? "Stock disponible" : "Disponibilidad", grilla.Columns["cantidad"].HeaderText);
                    var celda = grilla.Rows[0].Cells["cantidad"];
                    Assert.Equal((decimal)stock, celda.Value);
                    Assert.Equal(typeof(decimal), grilla.Columns["cantidad"].ValueType);
                    Assert.Equal(esperado, celda.FormattedValue);
                    if (!admin) Assert.DoesNotContain((string)celda.FormattedValue, char.IsDigit);
                }
            });
        }

        [Theory]
        [InlineData(false, "1.5")]
        [InlineData(false, "1,5")]
        [InlineData(false, "1.500")]
        [InlineData(true, "1.5")]
        [InlineData(true, "1,5")]
        [InlineData(true, "1.500")]
        public void UsarYCrearInterpretanAmbosSeparadoresComoDecimal(bool admin, string entrada)
        {
            Sta(() =>
            {
                using (var modal = CrearModal(admin, new List<string>()))
                {
                    CargarGrilla(modal, 5m);
                    Campo<Control>(modal, "txtCantidad").Text = entrada;
                    Invocar(modal, "btnSeleccionarRecorte_Click");
                    Assert.Equal(DialogResult.OK, modal.DialogResult);
                    Assert.Equal(1.5m, modal.CantidadSeleccionada);
                }
                using (var modal = CrearModal(admin, new List<string>()))
                {
                    var ancho = Campo<Control>(modal, "txtAnchoBuscado");
                    var filtrar = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), modal,
                        typeof(ModalRecortes).GetMethod("TxtFiltro_TextChanged", Flags));
                    ancho.TextChanged -= filtrar;
                    ancho.Text = "2";
                    Campo<Control>(modal, "txtCantidad").Text = entrada;
                    Invocar(modal, "btnCrearNuevo_Click");
                    Assert.Equal(DialogResult.OK, modal.DialogResult);
                    Assert.Equal(1.5m, modal.CantidadSeleccionada);
                    Assert.Equal(0, modal.RecorteSeleccionado.id);
                }
            });
        }
    }
}
