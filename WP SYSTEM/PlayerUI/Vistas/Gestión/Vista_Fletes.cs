using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Fletes : Form
    {
        private int idSeleccionado=-1;
        public Vista_Fletes()
        {
            InitializeComponent();
            eliminarFleteBoton.Enabled = false;
            CargarFlete();
        }
        private void CargarFlete()
        {
            try
            {
                FleteDatos datos = new FleteDatos();
                dgvFletes.DataSource = datos.GetFletes();

                dgvFletes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvFletes.RowHeadersVisible = false;
                dgvFletes.AllowUserToResizeRows = false;
                dgvFletes.ColumnHeadersHeight = 35;
                dgvFletes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvFletes.MultiSelect = false;
                dgvFletes.ReadOnly = true;
                dgvFletes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvFletes.AllowUserToResizeColumns = false;
                dgvFletes.AllowUserToResizeRows = false;

                dgvFletes.EnableHeadersVisualStyles = false;
                dgvFletes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvFletes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvFletes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvFletes.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvFletes.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvFletes.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvFletes.Columns["id"] != null)
                {
                    dgvFletes.Columns["id"].Visible = false;
                }

                if (dgvFletes.Columns["nombre"] != null)
                {
                    dgvFletes.Columns["nombre"].HeaderText = "Nombre de Flete";
                    dgvFletes.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }

                if (dgvFletes.Columns["activo"] != null)
                {
                    dgvFletes.Columns["activo"].HeaderText = "Activo";
                    dgvFletes.Columns["activo"].Width = 80;
                }
            }
            catch (Exception)
            {
                ToastNotification.ShowError("Error al cargar fletes.");
            }
        }
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            string flete = txtFlete.Text;

            if (string.IsNullOrEmpty(flete))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre.");
                return;
            }
            string _msg = idSeleccionado < 0 ? "¿Crear este flete?" : "¿Actualizar este flete?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if(idSeleccionado < 0)
                {
                    FleteDatos datos = new FleteDatos();
                    int nuevoId = datos.GuardarFlete(flete);
                    
                    // Si el usuario desmarcó "Activo" antes de guardar, actualizamos el estado
                    if (!chkActivo.Checked)
                    {
                        var valores = new Dictionary<string, object>();
                        ComunDatos cDatos = new ComunDatos();
                        valores.Add("activo", false);
                        cDatos.ActualizarEntidadGenerica("fletes", "id_flete", nuevoId, valores);
                    }
                    txtFlete.Clear();
                }
                else
                {
                    var valores = new Dictionary<string, object>();
                    ComunDatos datos = new ComunDatos();
                    valores.Add("nombre", flete);
                    valores.Add("activo", chkActivo.Checked);
                    datos.ActualizarEntidadGenerica("fletes", "id_flete", idSeleccionado, valores);
                }
                CargarFlete();
                LimpiarSeleccion();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void dgvFletes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var filas = dgvFletes.Rows[e.RowIndex];
            if (filas.Cells["id"].Value == null) return;
            int clicID = Convert.ToInt32(filas.Cells["id"].Value);
            if (clicID == idSeleccionado)
            {
                LimpiarSeleccion();
            }
            else
            {
                idSeleccionado=clicID;
                txtFlete.Text = Convert.ToString(filas.Cells["nombre"].Value);
                chkActivo.Checked = filas.Cells["activo"].Value != null && filas.Cells["activo"].Value != DBNull.Value 
                    ? Convert.ToBoolean(filas.Cells["activo"].Value) : true;
                guna2Button1.Text = "Actualizar";
                eliminarFleteBoton.Enabled = true;
            }
        }
        private void LimpiarSeleccion()
        {
            idSeleccionado = -1;
            txtFlete.Clear();
            chkActivo.Checked = true;
            guna2Button1.Text = "Guardar";
            eliminarFleteBoton.Enabled = false;
            dgvFletes.ClearSelection();
        }

        private void eliminarFleteBoton_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == -1) return;
            ComunDatos datos = new ComunDatos();
            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de eliminar este flete?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    var valores = new Dictionary<string, object>();
                    valores.Add("activo", false);
                    datos.ActualizarEntidadGenerica("fletes", "id_flete", idSeleccionado, valores);

                    LimpiarSeleccion();
                    CargarFlete();
                    ToastNotification.ShowSuccess("Flete desactivado (borrado lógico).");
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al desactivar flete: " + ex.Message);
                }
            }
        }

        private void txtFlete_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                guna2Button1.PerformClick();
                e.SuppressKeyPress = true;
            }
        }
    }
}
