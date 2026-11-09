using System;
using System.Windows.Forms;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using System.Threading;
using System.Drawing;
using SteeLog.UI;

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
            AplicarTema();
            FormClosed += (s,e) => cierre.Cancel();
            Disposed += (s,e) => cierre.Cancel();
        }

        private void AplicarTema()
        {
            // El titulo vive en la barra del modal (Tema.ChromeModal): se reacomoda el contenido hacia arriba.
            lblTitulo.Visible = false;
            lblNombre.Font = Tema.FuenteTexto;
            lblNombre.ForeColor = Tema.TextoSecundario;
            lblNombre.Location = new Point(24, 20);
            Tema.Input(txtNombre);
            txtNombre.Location = new Point(24, 46);
            txtNombre.Size = new Size(352, Tema.AltoControl);
            Tema.Boton(btnConfirmar, Variante.Primario);
            Tema.Boton(btnCancelar, Variante.Secundario);
            btnConfirmar.Size = new Size(130, Tema.AltoControl);
            btnCancelar.Size = new Size(130, Tema.AltoControl);
            btnConfirmar.Location = new Point(246, 104);
            btnCancelar.Location = new Point(106, 104);
            ClientSize = new Size(400, 156);
            AcceptButton = btnConfirmar;
            CancelButton = btnCancelar;
            Tema.ChromeModal(this, "Nuevo flete");
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
