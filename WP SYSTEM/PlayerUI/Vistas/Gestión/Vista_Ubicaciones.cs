using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WP_Inventario.Datos;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Ubicaciones : Form
    {
        private int idSeleccionado = -1;

        public Vista_Ubicaciones()
        {
            InitializeComponent();
            CargarUbicacion();
            eliminarUbicacionBoton.Enabled = false;
        }

        public void CargarUbicacion()
        {
            try
            {
                ComunDatos datos = new ComunDatos();
                dgvUbicaciones.DataSource = datos.ObtenerListaGenerica("Ubicacion");

                dgvUbicaciones.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvUbicaciones.RowHeadersVisible = false;
                dgvUbicaciones.AllowUserToResizeRows = false;
                dgvUbicaciones.ColumnHeadersHeight = 35;
                dgvUbicaciones.MultiSelect = false;
                dgvUbicaciones.ReadOnly = true;
                dgvUbicaciones.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvUbicaciones.AllowUserToResizeColumns = false;

                dgvUbicaciones.EnableHeadersVisualStyles = false;
                dgvUbicaciones.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvUbicaciones.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvUbicaciones.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvUbicaciones.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvUbicaciones.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvUbicaciones.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvUbicaciones.Columns["id_ubicacion"] != null)
                {
                    dgvUbicaciones.Columns["id_ubicacion"].Visible = false;
                }

                if (dgvUbicaciones.Columns["nombre"] != null)
                {
                    dgvUbicaciones.Columns["nombre"].HeaderText = "Nombre de Ubicación";
                    dgvUbicaciones.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            catch (Exception)
            {
                ToastNotification.ShowError("Error al cargar ubicaciones.");
            }
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            string ubicacion = txtUbicacion.Text;
            if (string.IsNullOrEmpty(ubicacion))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre.");
                return;
            }
            string _msg = idSeleccionado < 0 ? "¿Crear esta ubicación?" : "¿Actualizar esta ubicación?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if (idSeleccionado < 0)
                {
                    ComunDatos datos = new ComunDatos();
                    var ubi = new Dictionary<string,object>();
                    ubi.Add("nombre",ubicacion);
                    datos.GuardarEntidadGenerica("Ubicacion",ubi);
                    txtUbicacion.Clear();
                }
                else
                {
                    ComunDatos datos = new ComunDatos();
                    var valores = new Dictionary<string, object>();
                    valores.Add("nombre", ubicacion);
                    datos.ActualizarEntidadGenerica("ubicacion", "id_ubicacion", idSeleccionado, valores);
                }
                LimpiarSeleccion();
                CargarUbicacion();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void dgvUbicaciones_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var filas = dgvUbicaciones.Rows[e.RowIndex];

            if (filas.Cells["id_ubicacion"].Value == null) return;

            int clicID = Convert.ToInt32(filas.Cells["id_ubicacion"].Value);

            if (idSeleccionado == clicID)
            {
                LimpiarSeleccion();
            }
            else
            {
                idSeleccionado = clicID;
                string ubicacionNombre = filas.Cells["nombre"].Value.ToString();
                txtUbicacion.Text = ubicacionNombre;
                guna2Button1.Text = "Actualizar";
                eliminarUbicacionBoton.Enabled = true;
            }
        }

        private void LimpiarSeleccion()
        {
            idSeleccionado = -1;
            eliminarUbicacionBoton.Enabled = false;
            guna2Button1.Text = "Guardar";
            txtUbicacion.Clear();
            dgvUbicaciones.ClearSelection();
        }

        private void eliminarUbicacionBoton_Click(object sender, EventArgs e)
        {

        }

        private void panelAntiParpadeo1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void eliminarUbicacionBoton_Click_1(object sender, EventArgs e)
        {
            if (idSeleccionado == -1) return;
            ComunDatos datos = new ComunDatos();
            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de eliminar esta ubicación?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    datos.EliminarEntidadGenerica("ubicacion", "id_ubicacion", idSeleccionado);
                    LimpiarSeleccion();
                    CargarUbicacion();
                }
                catch (Exception)
                {
                    ToastNotification.ShowError("No se puede eliminar (está en uso).");
                }
            }
        }

        private void txtUbicacion_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtUbicacion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                guna2Button1.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        private void Vista_Ubicaciones_Load(object sender, EventArgs e)
        {

        }
        private void dgvUbicaciones_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}