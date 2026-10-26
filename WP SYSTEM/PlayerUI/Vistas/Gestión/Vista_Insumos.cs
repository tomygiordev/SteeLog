using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Insumos : Form
    {
        private int idSeleccionado = -1;
        private InsumoDatos insumoDatos = new InsumoDatos();
        private ObraDatos obraDatos = new ObraDatos();
        private PrintDocument printDocument1;
        private Color tabActiveColor = Color.FromArgb(25, 135, 84);
        private Color tabInactiveColor = Color.FromArgb(47, 84, 57);
        private bool vistaHistorialActiva = false;

        public Vista_Insumos()
        {
            InitializeComponent();
            btnEliminar.Enabled = false;

            printDocument1 = new PrintDocument();
            printDocument1.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);
        }

        private void Vista_Insumos_Load(object sender, EventArgs e)
        {
            CargarComboObras();
            CargarComboFiltroInsumo();
            CargarInsumos();
        }

        // ===================== COMBOS =====================

        private void CargarComboObras()
        {
            cboObra.Items.Clear();
            cboObra.DataSource = null;

            var obras = obraDatos.GetObras();
            var listaObras = new List<Obra>();
            listaObras.Add(new Obra { id = 0, nombre = "Galpón" });
            listaObras.AddRange(obras);

            cboObra.DataSource = listaObras;
            cboObra.DisplayMember = "nombre";
            cboObra.ValueMember = "id";
            cboObra.SelectedIndex = 0;
        }

        private void CargarComboFiltroInsumo()
        {
            cboFiltroInsumo.DataSource = null;

            var insumos = insumoDatos.GetInsumos();
            var lista = new List<Insumo>();
            lista.Add(new Insumo { id = 0, nombre = "— Todos —" });
            foreach (var i in insumos)
                lista.Add(i);

            cboFiltroInsumo.DataSource = lista;
            cboFiltroInsumo.DisplayMember = "nombre";
            cboFiltroInsumo.ValueMember = "id";
            cboFiltroInsumo.SelectedIndex = 0;
        }

        // ===================== GRILLA INSUMOS =====================

        private void CargarInsumos()
        {
            try
            {
                var lista = insumoDatos.GetInsumos();
                dgvInsumos.DataSource = lista;

                dgvInsumos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvInsumos.RowHeadersVisible = false;
                dgvInsumos.AllowUserToResizeRows = false;
                dgvInsumos.ColumnHeadersHeight = 35;
                dgvInsumos.MultiSelect = false;
                dgvInsumos.ReadOnly = true;
                dgvInsumos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvInsumos.AllowUserToResizeColumns = false;
                dgvInsumos.ScrollBars = ScrollBars.None;

                dgvInsumos.EnableHeadersVisualStyles = false;
                dgvInsumos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvInsumos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvInsumos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvInsumos.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvInsumos.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvInsumos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvInsumos.Columns["id"] != null)
                    dgvInsumos.Columns["id"].Visible = false;
                if (dgvInsumos.Columns["id_obra"] != null)
                    dgvInsumos.Columns["id_obra"].Visible = false;

                if (dgvInsumos.Columns["nombre"] != null)
                {
                    dgvInsumos.Columns["nombre"].HeaderText = "Nombre";
                    dgvInsumos.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvInsumos.Columns["nombre"].FillWeight = 30;
                }
                if (dgvInsumos.Columns["nombre_obra"] != null)
                {
                    dgvInsumos.Columns["nombre_obra"].HeaderText = "Ubicación (Obra)";
                    dgvInsumos.Columns["nombre_obra"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvInsumos.Columns["nombre_obra"].FillWeight = 25;
                }
                if (dgvInsumos.Columns["consumido"] != null)
                {
                    dgvInsumos.Columns["consumido"].HeaderText = "Consumido";
                    dgvInsumos.Columns["consumido"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvInsumos.Columns["consumido"].FillWeight = 15;
                }
                if (dgvInsumos.Columns["observaciones"] != null)
                {
                    dgvInsumos.Columns["observaciones"].HeaderText = "Observaciones";
                    dgvInsumos.Columns["observaciones"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvInsumos.Columns["observaciones"].FillWeight = 30;
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar insumos: " + ex.Message);
            }
        }

        // ===================== GRILLA HISTORIAL =====================

        private void CargarHistorial()
        {
            try
            {
                var lista = insumoDatos.GetHistorial();

                if (cboFiltroInsumo.SelectedIndex > 0)
                {
                    var seleccionado = (Insumo)cboFiltroInsumo.SelectedItem;
                    lista = lista.Where(m => m.nombre_insumo == seleccionado.nombre).ToList();
                }

                dgvHistorial.DataSource = lista;

                dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvHistorial.RowHeadersVisible = false;
                dgvHistorial.AllowUserToResizeRows = false;
                dgvHistorial.ColumnHeadersHeight = 35;
                dgvHistorial.MultiSelect = false;
                dgvHistorial.ReadOnly = true;
                dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvHistorial.AllowUserToResizeColumns = false;
                dgvHistorial.ScrollBars = ScrollBars.None;

                dgvHistorial.EnableHeadersVisualStyles = false;
                dgvHistorial.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvHistorial.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvHistorial.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvHistorial.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvHistorial.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvHistorial.Columns["id"] != null)
                    dgvHistorial.Columns["id"].Visible = false;

                if (dgvHistorial.Columns["nombre_insumo"] != null)
                {
                    dgvHistorial.Columns["nombre_insumo"].HeaderText = "Insumo";
                    dgvHistorial.Columns["nombre_insumo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHistorial.Columns["nombre_insumo"].FillWeight = 30;
                }
                if (dgvHistorial.Columns["obra_origen"] != null)
                {
                    dgvHistorial.Columns["obra_origen"].HeaderText = "Origen";
                    dgvHistorial.Columns["obra_origen"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHistorial.Columns["obra_origen"].FillWeight = 25;
                }
                if (dgvHistorial.Columns["obra_destino"] != null)
                {
                    dgvHistorial.Columns["obra_destino"].HeaderText = "Destino";
                    dgvHistorial.Columns["obra_destino"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHistorial.Columns["obra_destino"].FillWeight = 25;
                }
                if (dgvHistorial.Columns["fecha"] != null)
                {
                    dgvHistorial.Columns["fecha"].HeaderText = "Fecha";
                    dgvHistorial.Columns["fecha"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHistorial.Columns["fecha"].FillWeight = 20;
                    dgvHistorial.Columns["fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar historial: " + ex.Message);
            }
        }

        // ===================== FILTRO HISTORIAL =====================

        private void cboFiltroInsumo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva)
                CargarHistorial();
        }

        // ===================== TABS =====================

        private void btnVerInsumos_Click(object sender, EventArgs e)
        {
            vistaHistorialActiva = false;
            panelGrid.Visible = true;
            panelGridHistorial.Visible = false;
            panelFiltro.Visible = false;
            btnVerInsumos.FillColor = tabActiveColor;
            btnVerHistorial.FillColor = tabInactiveColor;
            CargarInsumos();
        }

        private void btnVerHistorial_Click(object sender, EventArgs e)
        {
            vistaHistorialActiva = true;
            panelGrid.Visible = false;
            panelGridHistorial.Visible = true;
            panelFiltro.Visible = true;
            btnVerInsumos.FillColor = tabInactiveColor;
            btnVerHistorial.FillColor = tabActiveColor;
            CargarComboFiltroInsumo();
            CargarHistorial();
        }

        // ===================== SELECTION =====================

        private void LimpiarSeleccion()
        {
            idSeleccionado = -1;
            txtNombre.Clear();
            txtObservaciones.Clear();
            cboObra.SelectedIndex = 0;
            chkConsumido.Checked = false;
            btnGuardar.Text = "Guardar";
            btnEliminar.Enabled = false;
            dgvInsumos.ClearSelection();
        }

        private void dgvInsumos_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var fila = dgvInsumos.Rows[e.RowIndex];
            if (fila.Cells["id"].Value == null) return;

            int clicID = Convert.ToInt32(fila.Cells["id"].Value);

            if (idSeleccionado == clicID)
            {
                LimpiarSeleccion();
            }
            else
            {
                idSeleccionado = clicID;
                txtNombre.Text = fila.Cells["nombre"].Value.ToString();
                txtObservaciones.Text = fila.Cells["observaciones"].Value?.ToString() ?? "";

                bool consumido = fila.Cells["consumido"].Value != null && (bool)fila.Cells["consumido"].Value;
                chkConsumido.Checked = consumido;

                var idObraVal = fila.Cells["id_obra"].Value;
                if (idObraVal == null || idObraVal == DBNull.Value)
                {
                    cboObra.SelectedIndex = 0;
                }
                else
                {
                    int idObra = Convert.ToInt32(idObraVal);
                    for (int i = 0; i < cboObra.Items.Count; i++)
                    {
                        var obra = (Obra)cboObra.Items[i];
                        if (obra.id == idObra)
                        {
                            cboObra.SelectedIndex = i;
                            break;
                        }
                    }
                }

                btnGuardar.Text = "Actualizar";
                btnEliminar.Enabled = true;
            }
        }

        // ===================== CRUD =====================

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre para el insumo.");
                return;
            }

            var obraSeleccionada = (Obra)cboObra.SelectedItem;
            int? idObra = obraSeleccionada.id == 0 ? (int?)null : obraSeleccionada.id;
            bool consumido = chkConsumido.Checked;
            string observaciones = txtObservaciones.Text.Trim();

            string _msg = idSeleccionado == -1 ? "¿Crear este insumo?" : "¿Actualizar este insumo?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if (idSeleccionado == -1)
                {
                    insumoDatos.GuardarInsumo(nombre, idObra, observaciones);
                }
                else
                {
                    insumoDatos.ActualizarInsumo(idSeleccionado, nombre, idObra, consumido, observaciones);
                }

                LimpiarSeleccion();
                CargarInsumos();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == -1) return;

            DialogResult resultado = MessageBox.Show(
                "¿Estás seguro de eliminar este insumo?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    insumoDatos.EliminarInsumo(idSeleccionado);
                    LimpiarSeleccion();
                    CargarInsumos();
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("No se puede eliminar el insumo: " + ex.Message);
                }
            }
        }

        // ===================== PDF =====================

        private void btnPDF_Click(object sender, EventArgs e)
        {
            try
            {
                string printerName = "Microsoft Print to PDF";
                bool printerFound = false;
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        printerFound = true;
                        break;
                    }
                }

                if (printerFound)
                {
                    printDocument1.PrinterSettings.PrinterName = printerName;
                    printDocument1.PrintController = new StandardPrintController();
                    printDocument1.Print();
                }
                else
                {
                    PrintDialog pd = new PrintDialog();
                    pd.Document = printDocument1;
                    if (pd.ShowDialog() == DialogResult.OK)
                    {
                        printDocument1.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al generar PDF: " + ex.Message);
            }
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (vistaHistorialActiva)
                ImprimirHistorial(e);
            else
                ImprimirInsumos(e);
        }

        private void ImprimirInsumos(PrintPageEventArgs e)
        {
            Font fontHeader = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubHeader = new Font("Arial", 10, FontStyle.Bold);
            Font fontBody = new Font("Arial", 9, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penLine = Pens.Black;

            float x = 40;
            float y = 40;
            float offset = 28;

            e.Graphics.DrawString("INFORME DE INSUMOS", fontHeader, brushText, x, y);
            y += offset;
            e.Graphics.DrawString("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brushText, x, y);
            y += offset + 10;

            float[] colWidths = new float[] { 170, 140, 80, 220 };
            string[] headers = new string[] { "Nombre", "Ubicación", "Consumido", "Observaciones" };

            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.DarkGreen, currentX, y, colWidths[i], 25);
                e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 25);
                e.Graphics.DrawString(headers[i], fontSubHeader, Brushes.White, currentX + 4, y + 4);
                currentX += colWidths[i];
            }
            y += 25;

            foreach (DataGridViewRow row in dgvInsumos.Rows)
            {
                if (row.IsNewRow) continue;
                if (y > e.MarginBounds.Bottom) break;

                currentX = x;
                bool cons = row.Cells["consumido"].Value != null && (bool)row.Cells["consumido"].Value;
                string[] cells = new string[]
                {
                    row.Cells["nombre"].Value?.ToString() ?? "",
                    row.Cells["nombre_obra"].Value?.ToString() ?? "",
                    cons ? "Sí" : "No",
                    row.Cells["observaciones"].Value?.ToString() ?? ""
                };

                for (int i = 0; i < cells.Length; i++)
                {
                    e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 22);
                    e.Graphics.DrawString(cells[i], fontBody, brushText, new RectangleF(currentX + 3, y + 3, colWidths[i] - 4, 22));
                    currentX += colWidths[i];
                }
                y += 22;
            }

            y += 15;
            e.Graphics.DrawString($"Total insumos: {dgvInsumos.Rows.Count}", fontSubHeader, brushText, x, y);
        }

        private void ImprimirHistorial(PrintPageEventArgs e)
        {
            Font fontHeader = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubHeader = new Font("Arial", 10, FontStyle.Bold);
            Font fontBody = new Font("Arial", 9, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penLine = Pens.Black;

            float x = 40;
            float y = 40;
            float offset = 28;

            e.Graphics.DrawString("HISTORIAL DE MOVIMIENTOS - INSUMOS", fontHeader, brushText, x, y);
            y += offset;

            string filtro = cboFiltroInsumo.SelectedIndex > 0
                ? ((Insumo)cboFiltroInsumo.SelectedItem).nombre
                : "Todos";
            e.Graphics.DrawString("Filtro: " + filtro, fontBody, brushText, x, y);
            y += 18;
            e.Graphics.DrawString("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brushText, x, y);
            y += offset + 10;

            float[] colWidths = new float[] { 170, 150, 150, 140 };
            string[] headers = new string[] { "Insumo", "Origen", "Destino", "Fecha" };

            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.DarkGreen, currentX, y, colWidths[i], 25);
                e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 25);
                e.Graphics.DrawString(headers[i], fontSubHeader, Brushes.White, currentX + 4, y + 4);
                currentX += colWidths[i];
            }
            y += 25;

            foreach (DataGridViewRow row in dgvHistorial.Rows)
            {
                if (row.IsNewRow) continue;
                if (y > e.MarginBounds.Bottom) break;

                currentX = x;
                string fechaStr = "";
                if (row.Cells["fecha"].Value != null && DateTime.TryParse(row.Cells["fecha"].Value.ToString(), out DateTime dt))
                    fechaStr = dt.ToString("dd/MM/yyyy HH:mm");

                string[] cells = new string[]
                {
                    row.Cells["nombre_insumo"].Value?.ToString() ?? "",
                    row.Cells["obra_origen"].Value?.ToString() ?? "",
                    row.Cells["obra_destino"].Value?.ToString() ?? "",
                    fechaStr
                };

                for (int i = 0; i < cells.Length; i++)
                {
                    e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 22);
                    e.Graphics.DrawString(cells[i], fontBody, brushText, new RectangleF(currentX + 3, y + 3, colWidths[i] - 4, 22));
                    currentX += colWidths[i];
                }
                y += 22;
            }

            y += 15;
            e.Graphics.DrawString($"Total movimientos: {dgvHistorial.Rows.Count}", fontSubHeader, brushText, x, y);
        }
    }
}
