using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Servicios;
using SteeLog.Core.Application.Actualizaciones;
using System.Threading;
using SteeLog.UI;

namespace SteeLog.Vistas
{
    public class ModalActualizacion : Form
    {
        private Label lblTitulo;
        private Label lblVersion;
        private Label lblNotas;
        private Guna.UI2.WinForms.Guna2TextBox txtNotas;
        private Guna.UI2.WinForms.Guna2ProgressBar progressBar;
        private Label lblProgreso;
        private Guna.UI2.WinForms.Guna2Button btnActualizar;
        private Guna.UI2.WinForms.Guna2Button btnCancelar;

        private readonly UpdateInfo _update;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly SteeLog.Presentation.IServiciosVista servicios;

        public ModalActualizacion(SteeLog.Presentation.IServiciosVista servicios, UpdateInfo update)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _update = update;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Actualización DEMO";
            this.ClientSize = new Size(480, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Tema.Fondo;
            this.ForeColor = Tema.Texto;

            lblTitulo = new Label
            {
                Text = "Actualización demostrativa",
                Font = Tema.Fuente(13f, true),
                ForeColor = Tema.Acento,
                Location = new Point(24, 20),
                Size = new Size(432, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblVersion = new Label
            {
                Text = $"Versión actual: {AppVersion.Current}   →   Nueva: {_update.Version}",
                Font = Tema.FuenteTexto,
                ForeColor = Tema.Texto,
                Location = new Point(24, 52),
                Size = new Size(432, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblNotas = new Label
            {
                Text = "Novedades",
                Font = Tema.FuenteSubtitulo,
                ForeColor = Tema.TextoSecundario,
                Location = new Point(24, 84),
                Size = new Size(200, 20)
            };

            txtNotas = new Guna.UI2.WinForms.Guna2TextBox
            {
                Text = string.IsNullOrWhiteSpace(_update.ReleaseNotes) ? "Sin notas." : _update.ReleaseNotes,
                Location = new Point(24, 108),
                Size = new Size(432, 84),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            Tema.Input(txtNotas);
            txtNotas.Cursor = Cursors.Default;
            txtNotas.TabStop = false;
            txtNotas.GotFocus += (s, e) => txtNotas.SelectionLength = 0;

            progressBar = new Guna.UI2.WinForms.Guna2ProgressBar
            {
                Location = new Point(24, 204),
                Size = new Size(432, 8),
                Value = 0,
                FillColor = Tema.FondoInput,
                ProgressColor = Tema.Acento,
                ProgressColor2 = Tema.AcentoHover,
                BorderRadius = 4,
                Visible = false
            };

            lblProgreso = new Label
            {
                Text = "",
                Font = Tema.Fuente(10f),
                ForeColor = Tema.TextoSecundario,
                Location = new Point(24, 218),
                Size = new Size(432, 20),
                Visible = false
            };

            btnActualizar = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Preparar paquete demo",
                Location = new Point(24, 246),
                Size = new Size(220, Tema.AltoBotonPrincipal)
            };
            Tema.Boton(btnActualizar, Variante.Primario);
            btnActualizar.Click += BtnActualizar_Click;

            btnCancelar = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Cancelar",
                Location = new Point(256, 246),
                Size = new Size(200, Tema.AltoBotonPrincipal)
            };
            Tema.Boton(btnCancelar, Variante.Secundario);
            btnCancelar.Click += (s, e) => { cancellation.Cancel(); this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblTitulo, lblVersion, lblNotas, txtNotas,
                progressBar, lblProgreso, btnActualizar, btnCancelar
            });

            Tema.ChromeModal(this, "Actualización DEMO");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            cancellation.Cancel();
            base.OnFormClosing(e);
        }

        private async void BtnActualizar_Click(object sender, EventArgs e)
        {
            btnActualizar.Enabled = false;
            btnCancelar.Enabled = true;
            progressBar.Visible = true;
            lblProgreso.Visible = true;
            lblProgreso.Text = "Preparando paquete...";

            var progress = new Progress<int>(p =>
            {
                if (IsDisposed || Disposing) return;
                progressBar.Value = p;
                lblProgreso.Text = $"Preparando paquete... {p}%";
            });

            try
            {
                var path = await servicios.Actualizaciones.PrepareAsync(_update, progress, cancellation.Token);
                if (IsDisposed) return;
                lblProgreso.Text = "Paquete demo preparado; no se instaló ninguna actualización.";
                btnCancelar.Text = "Cerrar";
                Dialogo.Mostrar(this, "Paquete demo preparado en:\n" + path + "\nNo se modificó ni reinició la aplicación.", "Demostración", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                btnActualizar.Enabled = true;
                btnCancelar.Enabled = true;
                progressBar.Visible = false;
                lblProgreso.Visible = false;
                Dialogo.Mostrar(this, "Error al preparar paquete demo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
