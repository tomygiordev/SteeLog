using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Servicios;
using SteeLog.Core.Application.Actualizaciones;
using System.Threading;

namespace SteeLog.Vistas
{
    public class ModalActualizacion : Form
    {
        private Label lblTitulo;
        private Label lblVersion;
        private Label lblNotas;
        private TextBox txtNotas;
        private Guna.UI2.WinForms.Guna2ProgressBar progressBar;
        private Label lblProgreso;
        private Guna.UI2.WinForms.Guna2Button btnActualizar;
        private Guna.UI2.WinForms.Guna2Button btnCancelar;

        private readonly UpdateInfo _update;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        public ModalActualizacion(UpdateInfo update)
        {
            _update = update;
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Actualización DEMO";
            this.Size = new Size(460, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(29, 38, 35);
            this.ForeColor = Color.White;

            lblTitulo = new Label
            {
                Text = "Actualización demostrativa",
                Font = new Font("Bahnschrift SemiBold", 14F),
                ForeColor = Color.FromArgb(39, 174, 96),
                Location = new Point(20, 20),
                Size = new Size(420, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblVersion = new Label
            {
                Text = $"Versión actual: {AppVersion.Current}   →   Nueva: {_update.Version}",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(180, 210, 190),
                Location = new Point(20, 55),
                Size = new Size(420, 20)
            };

            lblNotas = new Label
            {
                Text = "Novedades:",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(155, 185, 165),
                Location = new Point(20, 82),
                Size = new Size(100, 18)
            };

            txtNotas = new TextBox
            {
                Text = string.IsNullOrWhiteSpace(_update.ReleaseNotes) ? "Sin notas." : _update.ReleaseNotes,
                Location = new Point(20, 103),
                Size = new Size(415, 80),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(22, 30, 26),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };

            progressBar = new Guna.UI2.WinForms.Guna2ProgressBar
            {
                Location = new Point(20, 198),
                Size = new Size(415, 14),
                Value = 0,
                FillColor = Color.FromArgb(39, 174, 96),
                ProgressColor = Color.FromArgb(39, 174, 96),
                Visible = false
            };

            lblProgreso = new Label
            {
                Text = "",
                Font = new Font("Bahnschrift Light", 9F),
                ForeColor = Color.FromArgb(155, 185, 165),
                Location = new Point(20, 215),
                Size = new Size(415, 18),
                Visible = false
            };

            btnActualizar = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Preparar paquete demo",
                FillColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light", 11F),
                Location = new Point(20, 240),
                Size = new Size(200, 36)
            };
            btnActualizar.Click += BtnActualizar_Click;

            btnCancelar = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Cancelar",
                FillColor = Color.FromArgb(55, 70, 62),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Bahnschrift Light", 11F),
                Location = new Point(235, 240),
                Size = new Size(200, 36)
            };
            btnCancelar.Click += (s, e) => { cancellation.Cancel(); this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblTitulo, lblVersion, lblNotas, txtNotas,
                progressBar, lblProgreso, btnActualizar, btnCancelar
            });
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
                var path = await UpdaterService.PrepareAsync(_update, progress, cancellation.Token);
                if (IsDisposed) return;
                lblProgreso.Text = "Paquete demo preparado; no se instaló ninguna actualización.";
                btnCancelar.Text = "Cerrar";
                MessageBox.Show("Paquete demo preparado en:\n" + path + "\nNo se modificó ni reinició la aplicación.", "Demostración", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                btnActualizar.Enabled = true;
                btnCancelar.Enabled = true;
                progressBar.Visible = false;
                lblProgreso.Visible = false;
                MessageBox.Show("Error al preparar paquete demo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
