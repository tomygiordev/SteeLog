using System;
using System.Threading;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;

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
            dtpHasta.Value = DateTime.Today;
            dtpDesde.Value = DateTime.Today.AddMonths(-1);
            btnAceptar.Enabled = false;
            Shown += CargarPrimeraFecha;
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
