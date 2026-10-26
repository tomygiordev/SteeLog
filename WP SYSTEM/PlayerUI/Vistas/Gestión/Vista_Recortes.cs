using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Recortes : Form
    {
        private RecorteDatos _recorteDatos = new RecorteDatos();
        private ComunDatos _comunDatos = new ComunDatos();
        private List<Recorte> _recortes = new List<Recorte>();

        public Vista_Recortes()
        {
            InitializeComponent();
            CargarEstadosRecorte();
            CargarCategoriasRecortables();
            CargarRecortes();
            
            cmbCategorias.SelectedIndexChanged += CmbCategorias_SelectedIndexChanged;
            cmbMateriales.SelectedIndexChanged += CmbMateriales_SelectedIndexChanged;
            txtAncho.KeyPress += TxtDimension_KeyPress;
            txtAltura.KeyPress += TxtDimension_KeyPress;
            txtCantidad.KeyPress += TxtCantidad_KeyPress;
            txtBusquedaRecortes.TextChanged += TxtBusquedaRecortes_TextChanged;
        }

        private void CargarEstadosRecorte()
        {
            cmbEstado.Items.Clear();
            cmbEstado.Items.AddRange(new object[] { "Buen estado", "Regular", "Mal estado" });
            cmbEstado.SelectedIndex = -1;
        }

        private void TxtCantidad_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }
        }

        private void CargarCategoriasRecortables()
        {
            DataTable dt = _comunDatos.ObtenerListaGenerica("categorias");
            var categorasRecortables = dt.AsEnumerable()
                .Where(row => row.Field<bool>("recortable") == true)
                .Select(row => new { 
                    id = row.Table.Columns.Contains("id_categoria") ? row.Field<int>("id_categoria") : row.Field<int>("id"), 
                    nombre = row.Field<string>("nombre") 
                })
                .ToList();

            cmbCategorias.DataSource = categorasRecortables;
            cmbCategorias.DisplayMember = "nombre";
            cmbCategorias.ValueMember = "id";
            cmbCategorias.SelectedIndex = -1;
        }

        private void CmbCategorias_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCategorias.SelectedIndex != -1)
            {
                int idCat = (int)cmbCategorias.SelectedValue;
                CargarMaterialesPorCategoria(idCat);
            }
            else
            {
                cmbMateriales.DataSource = null;
            }
        }

        private void CargarMaterialesPorCategoria(int idCategoria)
        {
            DataTable dt = _comunDatos.ObtenerListaGenerica("materiales");
            var materiales = dt.AsEnumerable()
                .Where(row => row.Field<int>("id_categoria") == idCategoria)
                .Select(row => new { id = row.Field<int>("id"), nombre = row.Field<string>("nombre") })
                .ToList();

            cmbMateriales.DataSource = materiales;
            cmbMateriales.DisplayMember = "nombre";
            cmbMateriales.ValueMember = "id";
            cmbMateriales.SelectedIndex = -1;
        }

        private void CmbMateriales_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void CargarRecortes()
        {
            _recortes = _recorteDatos.ObtenerRecortes();
            AplicarFiltroBusqueda();
        }

        private void AplicarFiltroBusqueda()
        {
            string busquedaNormalizada = NormalizarTextoBusqueda(txtBusquedaRecortes.Text);

            var recortesFiltrados = string.IsNullOrWhiteSpace(busquedaNormalizada)
                ? _recortes
                : _recortes
                    .Where(r => CoincideBusquedaFlexible(r.nombre_material, busquedaNormalizada))
                    .ToList();

            dgvRecortes.DataSource = null;
            dgvRecortes.DataSource = recortesFiltrados;
            ConfigurarGrid();
        }

        private static bool CoincideBusquedaFlexible(string texto, string busquedaNormalizada)
        {
            if (string.IsNullOrWhiteSpace(busquedaNormalizada))
            {
                return true;
            }

            string textoNormalizado = NormalizarTextoBusqueda(texto);
            return textoNormalizado.Contains(busquedaNormalizada);
        }

        private static string NormalizarTextoBusqueda(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            string textoDescompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(textoDescompuesto.Length);

            foreach (char caracter in textoDescompuesto)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
                if (categoria == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(caracter))
                {
                    builder.Append(caracter);
                }
            }

            return builder.ToString();
        }

        private void ConfigurarGrid()
        {
            dgvRecortes.EnableHeadersVisualStyles = false;
            dgvRecortes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRecortes.MultiSelect = false;
            dgvRecortes.ReadOnly = true;
            dgvRecortes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvRecortes.ColumnHeadersHeight = 35;
            dgvRecortes.RowHeadersVisible = false;
            dgvRecortes.AllowUserToResizeColumns = false;
            dgvRecortes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgvRecortes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvRecortes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRecortes.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvRecortes.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvRecortes.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgvRecortes.BackgroundColor = Color.FromArgb(20, 30, 25);
            dgvRecortes.DefaultCellStyle.BackColor = Color.FromArgb(28, 40, 32);
            dgvRecortes.DefaultCellStyle.ForeColor = Color.White;
            dgvRecortes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 90, 60);
            dgvRecortes.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvRecortes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 50, 40);
            dgvRecortes.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            dgvRecortes.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 90, 60);

            if (dgvRecortes.Columns["id"] != null) dgvRecortes.Columns["id"].Visible = false;
            if (dgvRecortes.Columns["id_material"] != null) dgvRecortes.Columns["id_material"].Visible = false;
            if (dgvRecortes.Columns["fecha_creacion"] != null) dgvRecortes.Columns["fecha_creacion"].Visible = false;
            
            if (dgvRecortes.Columns["nombre_material"] != null)
            {
                dgvRecortes.Columns["nombre_material"].HeaderText = "Material";
                dgvRecortes.Columns["nombre_material"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["nombre_material"].FillWeight = 30;
            }
            if (dgvRecortes.Columns["nombre_categoria"] != null)
            {
                dgvRecortes.Columns["nombre_categoria"].HeaderText = "Categoría";
                dgvRecortes.Columns["nombre_categoria"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["nombre_categoria"].FillWeight = 20;
            }
            if (dgvRecortes.Columns["ancho"] != null)
            {
                dgvRecortes.Columns["ancho"].HeaderText = "Ancho (m)";
                dgvRecortes.Columns["ancho"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["ancho"].FillWeight = 10;
            }
            if (dgvRecortes.Columns["altura"] != null)
            {
                dgvRecortes.Columns["altura"].HeaderText = "Altura (m)";
                dgvRecortes.Columns["altura"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["altura"].FillWeight = 10;
            }
            if (dgvRecortes.Columns["medida"] != null)
            {
                dgvRecortes.Columns["medida"].HeaderText = "Medida (m)";
                dgvRecortes.Columns["medida"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["medida"].FillWeight = 12;
            }
            if (dgvRecortes.Columns["cantidad"] != null)
            {
                dgvRecortes.Columns["cantidad"].HeaderText = "Stock Recorte";
                dgvRecortes.Columns["cantidad"].DefaultCellStyle.Format = "G29";
                dgvRecortes.Columns["cantidad"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["cantidad"].FillWeight = 12;
            }
            if (dgvRecortes.Columns["estado"] != null)
            {
                dgvRecortes.Columns["estado"].HeaderText = "Estado";
                dgvRecortes.Columns["estado"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["estado"].FillWeight = 16;
            }
        }

        private void TxtDimension_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (cmbMateriales.SelectedIndex == -1)
            {
                ToastNotification.ShowWarning("Seleccione un material.");
                return;
            }

            string inputAncho = txtAncho.Text.Replace(',', '.');
            string inputAltura = txtAltura.Text.Replace(',', '.');

            bool hasAncho = decimal.TryParse(inputAncho, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal ancho) && ancho > 0;
            bool hasAltura = decimal.TryParse(inputAltura, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal altura) && altura > 0;

            if (!hasAncho && !hasAltura)
            {
                ToastNotification.ShowWarning("Ingrese al menos un ancho o una altura mayor a 0.");
                return;
            }

            string inputCantidad = txtCantidad.Text.Replace(',', '.');
            bool hasCantidad = decimal.TryParse(inputCantidad, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal cantidad) && cantidad > 0;

            if (!hasCantidad)
            {
                ToastNotification.ShowWarning("Ingrese una cantidad válida mayor a 0.");
                return;
            }

            decimal medida = 0;
            if (hasAncho && !hasAltura)
            {
                medida = ancho;
                ancho = 0;
            }
            else if (!hasAncho && hasAltura)
            {
                medida = altura;
                altura = 0;
            }

            if (MessageBox.Show("¿Guardar este recorte?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                int idMat = (int)cmbMateriales.SelectedValue;
                Recorte nuevo = new Recorte
                {
                    id_material = idMat,
                    ancho = ancho,
                    altura = altura,
                    medida = medida,
                    cantidad = cantidad,
                    estado = cmbEstado.SelectedItem?.ToString()
                };
                _recorteDatos.GuardarRecorte(nuevo);
                ToastNotification.ShowSuccess("Recorte guardado correctamente.");
                txtAncho.Clear();
                txtAltura.Clear();
                txtCantidad.Text = "1";
                cmbEstado.SelectedIndex = -1;
                CargarRecortes();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvRecortes.SelectedRows.Count == 0) return;

            int id = (int)dgvRecortes.SelectedRows[0].Cells["id"].Value;
            if (MessageBox.Show("¿Seguro que desea eliminar este recorte?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    _recorteDatos.EliminarRecorte(id);
                    CargarRecortes();
                }
                catch (Exception)
                {
                    ToastNotification.ShowError("No se puede eliminar el recorte (posiblemente esté en uso).");
                }
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void txtCantidad_TextChanged(object sender, EventArgs e)
        {

        }

        private void TxtBusquedaRecortes_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltroBusqueda();
        }
    }
}
