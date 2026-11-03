using System;
using System.Windows.Forms;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using System.Threading;

namespace SteeLog.Vistas.Movimientos
{
    public partial class ModalAgregarFlete : Form, IMovimientosVista
    {
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private MovimientosPresenter presenter;
        private MovimientosPresenter Presenter => presenter ?? (presenter = LocalServices.Current.CrearMovimientosPresenter(this));
        public void SetOcupada(bool valor) { if (!IsDisposed) btnConfirmar.Enabled = !valor; }
        public void MostrarError(Exception error) { if (!IsDisposed) ToastNotification.ShowError(error.Message); }
        public int IdFleteCreado { get; private set; }
        public string NombreFleteCreado { get; private set; }

        public ModalAgregarFlete()
        {
            InitializeComponent();
            FormClosed += (s,e) => cierre.Cancel();
            Disposed += (s,e) => cierre.Cancel();
        }

        private async void btnConfirmar_Click(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                ToastNotification.ShowWarning("El nombre del flete no puede estar vacío.");
                return;
            }

            try
            {
                int? nuevoId = await Presenter.CrearFleteAsync(nombre,cierre.Token);
                if (!nuevoId.HasValue || IsDisposed || cierre.IsCancellationRequested) return;
                IdFleteCreado = nuevoId.Value;
                NombreFleteCreado = nombre;
                
                ToastNotification.ShowSuccess("Flete creado correctamente.");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar flete: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
