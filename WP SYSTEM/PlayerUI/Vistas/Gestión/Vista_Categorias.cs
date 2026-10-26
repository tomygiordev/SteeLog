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
using WP_Inventario;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Categorias : Form
    {
        private int idSeleccionado = -1;
        private ComunDatos comunDatos = new ComunDatos();
        public Vista_Categorias()
        {
            InitializeComponent();
            this.Load += Vista_Categorias_Load;
            eliminarCategoriaBoton.Enabled = false;
        }
        private void Vista_Categorias_Load(object sender, EventArgs e)
        {
            CargarCategorias();
            
        }

        private void dgvCategorias_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void CargarCategorias()
        {
            try
            {
                dgvCategorias.DataSource = comunDatos.ObtenerListaGenerica("categorias", "nombre");

                dgvCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvCategorias.RowHeadersVisible = false;
                dgvCategorias.AllowUserToResizeRows = false;
                dgvCategorias.ColumnHeadersHeight = 35;
                dgvCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvCategorias.MultiSelect = false;
                dgvCategorias.ReadOnly = true;
                dgvCategorias.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvCategorias.AllowUserToResizeColumns = false;
                dgvCategorias.AllowUserToResizeRows = false;
                dgvCategorias.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

                dgvCategorias.EnableHeadersVisualStyles = false;
                dgvCategorias.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvCategorias.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvCategorias.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvCategorias.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvCategorias.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvCategorias.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvCategorias.Columns["id_categoria"] != null)
                {
                    dgvCategorias.Columns["id_categoria"].Visible = false;
                }

                if (dgvCategorias.Columns["nombre"] != null)
                {
                    dgvCategorias.Columns["nombre"].HeaderText = "Nombre de Categoría";
                    dgvCategorias.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvCategorias.Columns["nombre"].FillWeight = 75;
                }

                if (dgvCategorias.Columns["recortable"] != null)
                {
                    dgvCategorias.Columns["recortable"].HeaderText = "Recortable";
                    dgvCategorias.Columns["recortable"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvCategorias.Columns["recortable"].FillWeight = 25;
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar categorías: " + ex.Message);
            }
        }
        private void LimpiarSeleccion()
        {
            idSeleccionado = -1;
            txtCategoria.Clear();
            chkRecortable.Checked = false;
            guna2Button1.Text = "Guardar";
            eliminarCategoriaBoton.Enabled = false;
            dgvCategorias.ClearSelection();
        }
        private void dgvCategorias_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var fila = dgvCategorias.Rows[e.RowIndex];

            if (fila.Cells["id_categoria"].Value == null) return;

            int clicID = Convert.ToInt32(fila.Cells["id_categoria"].Value);

            if (idSeleccionado == clicID)
            {
                LimpiarSeleccion();
            }
            else
            {
                idSeleccionado = clicID;
                txtCategoria.Text = fila.Cells["nombre"].Value.ToString();
                if (fila.Cells["recortable"] != null && fila.Cells["recortable"].Value != null)
                    chkRecortable.Checked = Convert.ToBoolean(fila.Cells["recortable"].Value);
                else
                    chkRecortable.Checked = false;
                guna2Button1.Text = "Actualizar";
                eliminarCategoriaBoton.Enabled = true;
            }
        }

        private void panelAntiParpadeo3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dgvCategorias_CellContentClick_2(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void txtCategoria_TextChanged(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click_1(object sender, EventArgs e)
        {
            string nombreCategoria = txtCategoria.Text.Trim();

            if (string.IsNullOrEmpty(nombreCategoria))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre.");
                return;
            }

            string _msg = idSeleccionado == -1 ? "¿Crear esta categoría?" : "¿Actualizar esta categoría?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if (idSeleccionado == -1)
                {
                    CategoriaDatos datos = new CategoriaDatos();
                    datos.GuardarCategoria(nombreCategoria, chkRecortable.Checked);
                }
                else
                {
                    var valores = new Dictionary<string, object>();
                    valores.Add("nombre", nombreCategoria);
                    valores.Add("recortable", chkRecortable.Checked);

                    comunDatos.ActualizarEntidadGenerica("categorias", "id_categoria", idSeleccionado, valores);
                }

                LimpiarSeleccion();
                CargarCategorias();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void Vista_Categorias_Load_1(object sender, EventArgs e)
        {

        }

        private void dgvCategorias_CellContentClick_3(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void eliminarCategoriaBoton_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == -1) return;

            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de eliminar esta categoría?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    comunDatos.EliminarEntidadGenerica("categorias", "id_categoria", idSeleccionado);

                    LimpiarSeleccion();
                    CargarCategorias();
                }
                catch (Exception)
                {
                    ToastNotification.ShowError("No se puede eliminar (posiblemente esté en uso)");
                }
            }
        }

        private void txtCategoria_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                guna2Button1.PerformClick();
                e.SuppressKeyPress = true;
            }
        }
    }
}
