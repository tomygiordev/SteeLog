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
    public partial class Vista_Proveedores : Form
    {
        private int idSeleccionado = -1;
        public Vista_Proveedores()
        {
            InitializeComponent();
            CargarProveedor();
            eliminarProveedorBoton.Enabled = false;
        }
        private void CargarProveedor()
        {
            try
            {
                ProveedorDatos datos = new ProveedorDatos();
                dgvProveedores.DataSource = datos.GetProveedor();

                dgvProveedores.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvProveedores.RowHeadersVisible = false;
                dgvProveedores.AllowUserToResizeRows = false;
                dgvProveedores.ColumnHeadersHeight = 35;
                dgvProveedores.MultiSelect = false;
                dgvProveedores.ReadOnly = true;
                dgvProveedores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvProveedores.AllowUserToResizeColumns = false;

                dgvProveedores.EnableHeadersVisualStyles = false;
                dgvProveedores.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvProveedores.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvProveedores.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvProveedores.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvProveedores.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvProveedores.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvProveedores.Columns["id"] != null)
                {
                    dgvProveedores.Columns["id"].Visible = false;
                }

                if (dgvProveedores.Columns["nombre"] != null)
                {
                    dgvProveedores.Columns["nombre"].HeaderText = "Nombre de proveedor";
                    dgvProveedores.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            catch (Exception)
            {
                ToastNotification.ShowError("Error al cargar proveedores.");
            }
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panelAntiParpadeo3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            string proveedor = txtProveedor.Text;
            if (string.IsNullOrEmpty(proveedor))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre.");
                return;
            }
            string _msg = idSeleccionado < 0 ? "¿Crear este proveedor?" : "¿Actualizar este proveedor?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if (idSeleccionado < 0)
                {
                    ProveedorDatos datos = new ProveedorDatos();
                    datos.GuardarProveedor(proveedor);
                    txtProveedor.Clear();
                }
                else {
                    ComunDatos datos = new ComunDatos();
                    var valores = new Dictionary<string, object>();
                    valores.Add("nombre",proveedor);
                    datos.ActualizarEntidadGenerica("proveedores", "id_proveedores", idSeleccionado, valores);
                }
                LimpiarSeleccion();
                CargarProveedor();
                
            }catch(Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }

        }

        private void dgvProveedores_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var filas = dgvProveedores.Rows[e.RowIndex];
            if (filas.Cells["id"].Value == null) return;
            int clicID = Convert.ToInt32(filas.Cells["id"].Value);
            if (idSeleccionado == clicID)
            {
                LimpiarSeleccion();
            }
            else
            {
                idSeleccionado = clicID;
                string proveedor = filas.Cells["nombre"].Value.ToString();
                txtProveedor.Text = proveedor;
                guna2Button1.Text = "Actualizar";
                eliminarProveedorBoton.Enabled = true;
            }
        }
        private void LimpiarSeleccion()
        {
            idSeleccionado = -1;
            eliminarProveedorBoton.Enabled = false;
            guna2Button1.Text = "Guardar";
            txtProveedor.Clear();
            dgvProveedores.ClearSelection();
        }

        private void eliminarProveedorBoton_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == -1) return;
            ComunDatos datos = new ComunDatos();
            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de eliminar este proveedor?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    datos.EliminarEntidadGenerica("proveedores", "id_proveedores", idSeleccionado);

                    LimpiarSeleccion();
                    CargarProveedor();
                }
                catch (Exception)
                {
                    ToastNotification.ShowError("No se puede eliminar (está en uso).");
                }
            }
        }

        private void txtProveedor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                guna2Button1.PerformClick();
                e.SuppressKeyPress = true;
            }
        }
    }
}
