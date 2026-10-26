using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario;

namespace WP_Inventario.Vistas.Movimientos
{
    public partial class ModalInsumos : Form
    {
        private InsumoDatos _insumoDatos = new InsumoDatos();
        private List<Insumo> _todosInsumos;
        public Insumo InsumoSeleccionado { get; private set; }

        public ModalInsumos()
        {
            InitializeComponent();
            CargarInsumos();
            txtBuscar.TextChanged += TxtBuscar_TextChanged;
        }

        private void CargarInsumos()
        {
            _todosInsumos = _insumoDatos.GetInsumos().Where(i => !i.consumido).ToList();
            MostrarInsumos(_todosInsumos);
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
            dgvInsumos.ColumnHeadersHeight = 35;
            dgvInsumos.EnableHeadersVisualStyles = false;
            dgvInsumos.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(35, 63, 43);
            dgvInsumos.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            dgvInsumos.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(35, 63, 43);
            dgvInsumos.ColumnHeadersDefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
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
            if (dgvInsumos.SelectedRows.Count == 0 || dgvInsumos.CurrentRow == null)
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
