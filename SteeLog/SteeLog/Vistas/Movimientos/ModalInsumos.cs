using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SteeLog.Modelos;
using SteeLog;
using SteeLog.Presentation;
using System.Threading;
using System.Drawing;
using SteeLog.UI;

namespace SteeLog.Vistas.Movimientos
{
    public partial class ModalInsumos : Form, IInsumosVista
    {
        private readonly IServiciosVista servicios;
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private List<Insumo> _todosInsumos = new List<Insumo>();
        private InsumosPresenter presenter;
        private readonly CargasInsumos cargas = new CargasInsumos();
        private bool cargando;
        private InsumosPresenter Presenter => presenter ?? (presenter = servicios.CrearInsumosPresenter(this));
        public void SetOcupada(bool valor) { if (!IsDisposed) btnConfirmar.Enabled = !valor && !cargando; }
        public void MostrarError(Exception error) { if (!IsDisposed) ToastNotification.ShowError(error.Message); }
        public Insumo InsumoSeleccionado { get; private set; }

        public ModalInsumos(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));

            InitializeComponent();
            AplicarTema();
            Load += async (s, e) => await CargarInsumos();
            FormClosed += (s, e) => { cargas.Cerrar(); cierre.Cancel(); };
            Disposed += (s, e) => { cargas.Cerrar(); cierre.Cancel(); };
            btnConfirmar.Enabled = false;
            txtBuscar.TextChanged += TxtBuscar_TextChanged;
        }

        private void AplicarTema()
        {
            lblTitulo.Visible = false;
            lblBuscar.Font = Tema.FuenteTexto;
            lblBuscar.ForeColor = Tema.TextoSecundario;
            lblBuscar.AutoSize = false;
            lblBuscar.TextAlign = ContentAlignment.MiddleLeft;
            lblBuscar.Location = new Point(16, 16);
            lblBuscar.Size = new Size(70, Tema.AltoControl);
            Tema.Input(txtBuscar);
            txtBuscar.Location = new Point(92, 16);
            txtBuscar.Size = new Size(422, Tema.AltoControl);
            Tema.Grilla(dgvInsumos);
            dgvInsumos.Location = new Point(16, 64);
            dgvInsumos.Size = new Size(498, 290);
            Tema.Boton(btnConfirmar, Variante.Primario);
            Tema.Boton(btnCancelar, Variante.Secundario);
            btnConfirmar.Size = new Size(130, Tema.AltoControl);
            btnCancelar.Size = new Size(130, Tema.AltoControl);
            panelBotones.Location = new Point(16, 366);
            panelBotones.Size = new Size(498, 42);
            ClientSize = new Size(530, 424);
            CancelButton = btnCancelar;
            Tema.ChromeModal(this, "Seleccionar insumo");
        }

        private async System.Threading.Tasks.Task CargarInsumos()
        {
            int actual = cargas.Iniciar();
            cargando = true;
            btnConfirmar.Enabled = false;
            dgvInsumos.DataSource = null;
            try
            {
                var lista = await Presenter.InsumosAsync(cierre.Token);
                if (IsDisposed || cierre.IsCancellationRequested || !cargas.EsActual(actual)) return;
                _todosInsumos = lista.Where(x => !x.Consumido).Select(x => new Insumo { id=x.Id, nombre=x.Nombre, id_obra=x.ObraId, nombre_obra=x.Obra, consumido=x.Consumido, observaciones=x.Observaciones }).ToList();
                TxtBuscar_TextChanged(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !cierre.IsCancellationRequested && cargas.EsActual(actual)) MostrarError(ex); }
            finally
            {
                if (!IsDisposed && !cierre.IsCancellationRequested && cargas.EsActual(actual))
                { cargando = false; btnConfirmar.Enabled = _todosInsumos.Count > 0; }
            }
        }

        private void MostrarInsumos(List<Insumo> lista)
        {
            dgvInsumos.DataSource = null;
            dgvInsumos.DataSource = lista;

            if (dgvInsumos.Columns["id"] != null) dgvInsumos.Columns["id"].Visible = false;
            if (dgvInsumos.Columns["id_obra"] != null) dgvInsumos.Columns["id_obra"].Visible = false;
            if (dgvInsumos.Columns["consumido"] != null) dgvInsumos.Columns["consumido"].Visible = false;

            if (dgvInsumos.Columns["nombre"] != null)
            {
                dgvInsumos.Columns["nombre"].HeaderText = "Nombre";
                dgvInsumos.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvInsumos.Columns["nombre"].FillWeight = 45;
            }
            if (dgvInsumos.Columns["nombre_obra"] != null)
            {
                dgvInsumos.Columns["nombre_obra"].HeaderText = "Ubicación";
                dgvInsumos.Columns["nombre_obra"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvInsumos.Columns["nombre_obra"].FillWeight = 30;
            }
            if (dgvInsumos.Columns["observaciones"] != null)
            {
                dgvInsumos.Columns["observaciones"].HeaderText = "Obs.";
                dgvInsumos.Columns["observaciones"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvInsumos.Columns["observaciones"].FillWeight = 25;
            }

            dgvInsumos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInsumos.RowHeadersVisible = false;
            dgvInsumos.ReadOnly = true;
            dgvInsumos.MultiSelect = false;
            dgvInsumos.AllowUserToResizeRows = false;
        }

        private void TxtBuscar_TextChanged(object sender, EventArgs e)
        {
            string filtro = txtBuscar.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(filtro))
            {
                MostrarInsumos(_todosInsumos);
            }
            else
            {
                var filtrados = _todosInsumos.Where(i => i.nombre.ToLower().Contains(filtro)).ToList();
                MostrarInsumos(filtrados);
            }
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (cargando || cierre.IsCancellationRequested || dgvInsumos.SelectedRows.Count == 0 || dgvInsumos.CurrentRow == null)
            {
                ToastNotification.ShowWarning("Seleccione un insumo.");
                return;
            }

            var fila = dgvInsumos.CurrentRow;
            InsumoSeleccionado = new Insumo
            {
                id = Convert.ToInt32(fila.Cells["id"].Value),
                nombre = fila.Cells["nombre"].Value.ToString(),
                nombre_obra = fila.Cells["nombre_obra"].Value?.ToString() ?? "Galpón",
                id_obra = fila.Cells["id_obra"].Value == null || fila.Cells["id_obra"].Value == DBNull.Value
                    ? (int?)null : Convert.ToInt32(fila.Cells["id_obra"].Value),
                observaciones = fila.Cells["observaciones"].Value?.ToString() ?? ""
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
