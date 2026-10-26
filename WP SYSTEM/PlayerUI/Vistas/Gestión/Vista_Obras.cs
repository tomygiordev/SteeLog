using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Obras : Form
    {
        private int idSeleccionado = -1;
        private List<Obra> _todasLasObras = new List<Obra>();
        private bool _isSelecting = false;

        public Vista_Obras()
        {
            InitializeComponent();
            CargarObra();
            eliminarObraBoton.Enabled = false;
        }

        public void CargarObra()
        {
            try
            {
                ObraDatos datos = new ObraDatos();
                _todasLasObras = datos.GetObras();
                FiltrarObras();
            }
            catch (Exception)
            {
                ToastNotification.ShowError("Error al cargar obras.");
            }
        }

        private void FiltrarObras()
        {
            string busqueda = txtObra.Text.Trim();
            
            if (string.IsNullOrEmpty(busqueda) || _isSelecting)
            {
                dgvObras.DataSource = null;
                dgvObras.DataSource = _todasLasObras;
            }
            else
            {
                // Filtro que busca coincidencias según lo tipeado (insensible a mayúsculas para mejorar la búsqueda)
                var filtradas = _todasLasObras.FindAll(o => o.nombre.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0);
                dgvObras.DataSource = null;
                dgvObras.DataSource = filtradas;
            }

            ConfigurarEstiloGrid();
        }

        private void ConfigurarEstiloGrid()
        {
            try
            {
                dgvObras.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvObras.RowHeadersVisible = false;
                dgvObras.AllowUserToResizeRows = false;
                dgvObras.ColumnHeadersHeight = 35;
                dgvObras.MultiSelect = false;
                dgvObras.ReadOnly = true;
                dgvObras.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvObras.AllowUserToResizeColumns = false;
                dgvObras.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

                dgvObras.EnableHeadersVisualStyles = false;
                dgvObras.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvObras.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvObras.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvObras.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvObras.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvObras.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvObras.Columns["id"] != null)
                {
                    dgvObras.Columns["id"].Visible = false;
                }

                if (dgvObras.Columns["nombre"] != null)
                {
                    dgvObras.Columns["nombre"].HeaderText = "Nombre de Obra";
                    dgvObras.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvObras.Columns["nombre"].FillWeight = 80;
                }

                if (dgvObras.Columns["activa"] != null)
                {
                    dgvObras.Columns["activa"].HeaderText = "Estado";
                    dgvObras.Columns["activa"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvObras.Columns["activa"].FillWeight = 20;
                    dgvObras.Columns["activa"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
            catch (Exception)
            {
                ToastNotification.ShowError("Error en estilo de grilla.");
            }
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            string obra = txtObra.Text;
            if (string.IsNullOrEmpty(obra))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre.");
                return;
            }
            string _msg = idSeleccionado < 0 ? "¿Crear esta obra?" : "¿Actualizar esta obra?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if (idSeleccionado < 0)
                {
                    ObraDatos datos = new ObraDatos();
                    datos.GuardarObra(obra, chkActiva.Checked);
                    txtObra.Clear();
                }
                else
                {
                    ComunDatos datos = new ComunDatos();
                    var valores = new Dictionary<string, object>();
                    valores.Add("nombre", obra);
                    valores.Add("activa", chkActiva.Checked);
                    datos.ActualizarEntidadGenerica("obras", "id", idSeleccionado, valores);
                }
                LimpiarSeleccion();
                CargarObra();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void dgvObras_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var filas = dgvObras.Rows[e.RowIndex];

            if (filas.Cells["id"].Value == null) return;

            int clicID = Convert.ToInt32(filas.Cells["id"].Value);

            if (idSeleccionado == clicID)
            {
                LimpiarSeleccion();
            }
            else
            {
                try
                {
                    _isSelecting = true;
                    idSeleccionado = clicID;
                    string obraNombre = filas.Cells["nombre"].Value.ToString();
                    txtObra.Text = obraNombre;
                    if (filas.Cells["activa"] != null && filas.Cells["activa"].Value != null)
                    {
                        chkActiva.Checked = (bool)filas.Cells["activa"].Value;
                    }
                    guna2Button1.Text = "Actualizar";
                    eliminarObraBoton.Enabled = true;
                }
                finally
                {
                    _isSelecting = false;
                }
            }
        }

        private void LimpiarSeleccion()
        {
            try
            {
                _isSelecting = true;
                idSeleccionado = -1;
                eliminarObraBoton.Enabled = false;
                guna2Button1.Text = "Guardar";
                txtObra.Clear();
                chkActiva.Checked = true;
                dgvObras.ClearSelection();
            }
            finally
            {
                _isSelecting = false;
                FiltrarObras();
            }
        }

        private void eliminarObraBoton_Click(object sender, EventArgs e)
        {
           
        }

        private void panelAntiParpadeo1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void eliminarObraBoton_Click_1(object sender, EventArgs e)
        {
            if (idSeleccionado == -1) return;
            ComunDatos datos = new ComunDatos();
            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de eliminar esta obra?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    datos.EliminarEntidadGenerica("obras", "id", idSeleccionado);
                    LimpiarSeleccion();
                    CargarObra();
                }
                catch (Exception ex)
                {
                    string msg = "No se puede eliminar la obra.\n\n";
                    if (ex.Message.Contains("23503") || ex.Message.ToLower().Contains("fk") || ex.Message.ToLower().Contains("constraint"))
                    {
                        msg += "Está vinculada a otros datos (movimientos, insumos, etc.). Sugerimos desmarcar 'Activa' para archivarla.\n\n";
                    }
                    msg += "Error técnico: " + ex.Message;
                    
                    ToastNotification.ShowError("No se puede eliminar la obra (está en uso).");
                }
            }
        }
        
        private void txtObra_TextChanged(object sender, EventArgs e)
        {
            if (!_isSelecting) // 4. Update the txtObra_TextChanged event to call FilterObras.
            {
                FiltrarObras();
            }
        }

        private void txtObra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                guna2Button1.PerformClick();
                e.SuppressKeyPress = true;
            }
        }
    }
}