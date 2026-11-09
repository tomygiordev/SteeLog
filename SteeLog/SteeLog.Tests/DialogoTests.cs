using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class DialogoTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static void EnSta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { accion(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(60)));
            Assert.Null(error);
        }

        private static Form Crear(MessageBoxButtons botones, MessageBoxDefaultButton def, bool pedirTexto)
        {
            var dialogo = typeof(SteeLog.Vistas.Movimientos.Vista_Movimientos).Assembly.GetType("SteeLog.UI.Dialogo", true);
            var tipo = dialogo.GetNestedType("FormDialogo", BindingFlags.NonPublic);
            var tipoDialogo = dialogo.GetNestedType("TipoDialogo", BindingFlags.NonPublic);
            var pregunta = Enum.Parse(tipoDialogo, "Pregunta");
            return (Form)Activator.CreateInstance(tipo, Flags, null,
                new object[] { "Mensaje", "Titulo", botones, pregunta, def, false, null, pedirTexto, "inicial" }, null);
        }

        // El proyecto de tests no referencia Guna: se identifican los controles por nombre de tipo.
        private static Control[] Botones(Form f) =>
            f.Controls.Cast<Control>().Where(c => c.GetType().Name == "Guna2Button").OrderBy(b => b.TabIndex).ToArray();

        private static Control Entrada(Form f) =>
            f.Controls.Cast<Control>().Single(c => c.GetType().Name == "Guna2TextBox");

        private static void Clic(Control boton) => boton.GetType().GetMethod("PerformClick", Flags).Invoke(boton, null);

        // Muestra el diálogo modal, ejecuta la acción ya mostrado y devuelve su resultado.
        // Un temporizador de seguridad lo cierra con Abort para que el test nunca quede bloqueado.
        private static DialogResult Mostrar(Form f, Action<Form> accion)
        {
            using (f)
            using (var seguridad = new System.Windows.Forms.Timer { Interval = 5000 })
            {
                f.ShowInTaskbar = false;
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new System.Drawing.Point(-20000, -20000);
                seguridad.Tick += (s, e) => { seguridad.Stop(); f.DialogResult = DialogResult.Abort; };
                f.Shown += (s, e) => f.BeginInvoke((Action)(() => accion(f)));
                seguridad.Start();
                return f.ShowDialog();
            }
        }

        private static bool Tecla(Form f, Keys k)
        {
            var msg = new Message();
            var m = typeof(Form).GetMethod("ProcessCmdKey", Flags);
            var args = new object[] { msg, k };
            return (bool)m.Invoke(f, args);
        }

        [Theory]
        [InlineData(MessageBoxButtons.YesNo, new[] { DialogResult.Yes, DialogResult.No })]
        [InlineData(MessageBoxButtons.OKCancel, new[] { DialogResult.OK, DialogResult.Cancel })]
        [InlineData(MessageBoxButtons.OK, new[] { DialogResult.OK })]
        public void ClicEnCadaBotonAsignaSuResultadoAlFormulario(MessageBoxButtons botones, DialogResult[] esperados)
        {
            EnSta(() =>
            {
                for (int i = 0; i < esperados.Length; i++)
                {
                    int idx = i;
                    var r = Mostrar(Crear(botones, MessageBoxDefaultButton.Button1, false), f => Clic(Botones(f)[idx]));
                    Assert.Equal(esperados[i], r);
                }
            });
        }

        [Fact]
        public void EnterConFocoEnUnBotonEjecutaEseBoton()
        {
            EnSta(() =>
            {
                // El botón por defecto es Sí, pero el foco está en No: Enter debe devolver No.
                var r = Mostrar(Crear(MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button1, false), f =>
                {
                    f.ActiveControl = Botones(f)[1];
                    Assert.True(Tecla(f, Keys.Enter));
                });
                Assert.Equal(DialogResult.No, r);
            });
        }

        [Fact]
        public void EnterConFocoFueraDeUnBotonEjecutaElBotonPorDefecto()
        {
            EnSta(() =>
            {
                var r = Mostrar(Crear(MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2, true), f =>
                {
                    f.ActiveControl = Entrada(f);
                    Assert.True(Tecla(f, Keys.Enter));
                });
                Assert.Equal(DialogResult.No, r);
            });
        }

        [Fact]
        public void EnterEnElDialogoDeTextoAceptaYConservaElTexto()
        {
            EnSta(() =>
            {
                string texto = null;
                var r = Mostrar(Crear(MessageBoxButtons.OKCancel, MessageBoxDefaultButton.Button1, true), f =>
                {
                    var entrada = Entrada(f);
                    entrada.Text = "escrito";
                    f.ActiveControl = entrada;
                    Assert.True(Tecla(f, Keys.Enter));
                    texto = (string)f.GetType().GetProperty("TextoIngresado").GetValue(f);
                });
                Assert.Equal(DialogResult.OK, r);
                Assert.Equal("escrito", texto);
            });
        }

        [Fact]
        public void EscDevuelveElResultadoDeEscape()
        {
            EnSta(() =>
            {
                Assert.Equal(DialogResult.No, Mostrar(Crear(MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button1, false), f => Tecla(f, Keys.Escape)));
                Assert.Equal(DialogResult.Cancel, Mostrar(Crear(MessageBoxButtons.OKCancel, MessageBoxDefaultButton.Button1, true), f => Tecla(f, Keys.Escape)));
            });
        }
    }
}
