using System;
using System.Threading;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;
using SteeLog.UI;
using System.Drawing;

namespace SteeLog.Vistas.Reportes
{
    public partial class ModalFechasInformeObra : Form
    {
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private readonly int idObra;
        public DateTime FechaDesde { get; private set; }
        public DateTime FechaHasta { get; private set; }
        public ModalFechasInformeObra(int idObra)
        {
            this.idObra = idObra;
            InitializeComponent();
            AplicarEstilo();
            dtpHasta.Value = DateTime.Today;
            dtpDesde.Value = DateTime.Today.AddMonths(-1);
            btnAceptar.Enabled = false;
            Shown += CargarPrimeraFecha;
        }
        private void AplicarEstilo()
        {
            ClientSize = new Size(340, 220);
            labelTitulo.AutoSize = false;
            labelTitulo.Font = Tema.FuenteSubtitulo;
            labelTitulo.ForeColor = Tema.TextoSecundario;
            labelTitulo.Text = "Seleccioná el período del informe";
            labelTitulo.Location = new Point(24, 18);
            labelTitulo.Size = new Size(292, 22);

            foreach (var lbl in new[] { lblDesde, lblHasta })
            {
                lbl.AutoSize = false;
                lbl.Font = Tema.FuenteTexto;
                lbl.ForeColor = Tema.TextoSecundario;
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                lbl.Size = new Size(64, Tema.AltoControl);
            }
            lblDesde.Location = new Point(24, 54);
            lblHasta.Location = new Point(24, 104);
            dtpDesde.Location = new Point(92, 54);
            dtpHasta.Location = new Point(92, 104);
            foreach (var dtp in new[] { dtpDesde, dtpHasta })
            {
                dtp.Size = new Size(224, Tema.AltoControl);
                Tema.Fecha(dtp);
            }

            btnAceptar.Text = "Aceptar";
            btnAceptar.Location = new Point(24, 160);
            btnAceptar.Size = new Size(140, Tema.AltoBotonPrincipal);
            Tema.Boton(btnAceptar, Variante.Primario);
            btnCancelar.Text = "Cancelar";
            btnCancelar.Location = new Point(176, 160);
            btnCancelar.Size = new Size(140, Tema.AltoBotonPrincipal);
            Tema.Boton(btnCancelar, Variante.Secundario);

            Tema.ChromeModal(this, "Rango de fechas");
        }

        private async void CargarPrimeraFecha(object sender, EventArgs e)
        {
            var token = vida.Token;
            try
            {
                var primera = await new InformeObraPresenter(LocalServices.Current.Executor, LocalServices.Current.InformeObraPdf).PrimeraFechaAsync(idObra, token);
                if (!token.IsCancellationRequested && !IsDisposed) dtpDesde.Value = primera ?? DateTime.Today.AddMonths(-1);
            }
            catch (OperationCanceledException) { }
            catch { }
            finally { if (!token.IsCancellationRequested && !IsDisposed) btnAceptar.Enabled = true; }
        }
        private void CancelarOperaciones() { vida.Cancel(); }
        protected override void OnFormClosing(FormClosingEventArgs e) { CancelarOperaciones(); base.OnFormClosing(e); }
        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (dtpDesde.Value > dtpHasta.Value) { ToastNotification.ShowWarning("La fecha 'Desde' no puede ser mayor que 'Hasta'."); return; }
            FechaDesde = dtpDesde.Value.Date; FechaHasta = dtpHasta.Value.Date; DialogResult = DialogResult.OK; Close();
        }
        private void btnCancelar_Click(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); }
    }
}
