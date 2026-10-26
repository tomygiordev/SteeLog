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
    public partial class Vista_Herramientas : Form
    {
        private int idSeleccionado = -1;
        private HerramientaDatos herramientaDatos = new HerramientaDatos();
        private ObraDatos obraDatos = new ObraDatos();
        private PrintDocument printDocument1;
        private Color tabActiveColor = Color.FromArgb(25, 135, 84);
        private Color tabInactiveColor = Color.FromArgb(47, 84, 57);
        private bool vistaHistorialActiva = false;

        public Vista_Herramientas()
        {
            InitializeComponent();
            btnEliminar.Enabled = false;

            printDocument1 = new PrintDocument();
            printDocument1.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);
        }

        private void Vista_Herramientas_Load(object sender, EventArgs e)
        {
            CargarComboEstado();
            CargarComboObras();
            CargarComboFiltroHerramienta();
            CargarHerramientas();
        }

        // ===================== COMBOS =====================

        private void CargarComboEstado()
        {
            cboEstado.Items.Clear();
            cboEstado.Items.Add("Óptima");
            cboEstado.Items.Add("Regular");
            cboEstado.Items.Add("Mal estado");
            cboEstado.SelectedIndex = 0;
        }

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

        private void CargarComboFiltroHerramienta()
        {
            cboFiltroHerramienta.DataSource = null;

            var herramientas = herramientaDatos.GetHerramientas();
            var lista = new List<Herramienta>();
            lista.Add(new Herramienta { id = 0, nombre = "— Todas —" });
            foreach (var h in herramientas)
                lista.Add(h);

            cboFiltroHerramienta.DataSource = lista;
            cboFiltroHerramienta.DisplayMember = "nombre";
            cboFiltroHerramienta.ValueMember = "id";
            cboFiltroHerramienta.SelectedIndex = 0;
        }

        private void CargarFiltrosHistorial()
        {
            // Cargar estados para el filtro
            cboFiltroEstado.Items.Clear();
            cboFiltroEstado.Items.Add("Todos");
            cboFiltroEstado.Items.Add("Óptima");
            cboFiltroEstado.Items.Add("Regular");
            cboFiltroEstado.Items.Add("Mal estado");
            cboFiltroEstado.SelectedIndex = 0;

            // Cargar obras para el filtro
            cboFiltroObra.DataSource = null;
            var obras = obraDatos.GetObras();
            var listaObras = new List<Obra>();
            listaObras.Add(new Obra { id = 0, nombre = "Todas" });
            listaObras.Add(new Obra { id = -1, nombre = "Galpón" });
            listaObras.AddRange(obras);
            
            cboFiltroObra.DataSource = listaObras;
            cboFiltroObra.DisplayMember = "nombre";
            cboFiltroObra.ValueMember = "id";
            cboFiltroObra.SelectedIndex = 0;
        }

        // ===================== GRILLA HERRAMIENTAS =====================

        private void CargarHerramientas()
        {
            try
            {
                var lista = herramientaDatos.GetHerramientas();
                
                // Ordenar por nombre que contiene ID
                lista = lista.OrderBy(h => {
                    // Extraer número del nombre si existe
                    string nombre = h.nombre ?? "";
                    // Buscar dígitos al inicio del nombre
                    var match = System.Text.RegularExpressions.Regex.Match(nombre, @"^(\d+)");
                    if (match.Success)
                    {
                        if (int.TryParse(match.Value, out int num))
                        {
                            return num;
                        }
                    }
                    // Si no tiene número al inicio, poner al final
                    return int.MaxValue;
                }).ThenBy(h => h.nombre).ToList();

                dgvHerramientas.DataSource = lista;

                dgvHerramientas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvHerramientas.RowHeadersVisible = false;
                dgvHerramientas.AllowUserToResizeRows = false;
                dgvHerramientas.ColumnHeadersHeight = 35;
                dgvHerramientas.MultiSelect = false;
                dgvHerramientas.ReadOnly = true;
                dgvHerramientas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvHerramientas.AllowUserToResizeColumns = false;
                dgvHerramientas.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

                dgvHerramientas.EnableHeadersVisualStyles = false;
                dgvHerramientas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvHerramientas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvHerramientas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvHerramientas.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvHerramientas.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvHerramientas.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvHerramientas.Columns["id"] != null)
                    dgvHerramientas.Columns["id"].Visible = false;
                if (dgvHerramientas.Columns["id_obra"] != null)
                    dgvHerramientas.Columns["id_obra"].Visible = false;

                if (dgvHerramientas.Columns["nombre"] != null)
                {
                    dgvHerramientas.Columns["nombre"].HeaderText = "Nombre";
                    dgvHerramientas.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["nombre"].FillWeight = 30;
                }
                if (dgvHerramientas.Columns["estado"] != null)
                {
                    dgvHerramientas.Columns["estado"].HeaderText = "Estado";
                    dgvHerramientas.Columns["estado"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["estado"].FillWeight = 20;
                }
                if (dgvHerramientas.Columns["nombre_obra"] != null)
                {
                    dgvHerramientas.Columns["nombre_obra"].HeaderText = "Ubicación (Obra)";
                    dgvHerramientas.Columns["nombre_obra"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["nombre_obra"].FillWeight = 25;
                }
                if (dgvHerramientas.Columns["observaciones"] != null)
                {
                    dgvHerramientas.Columns["observaciones"].HeaderText = "Observaciones";
                    dgvHerramientas.Columns["observaciones"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["observaciones"].FillWeight = 25;
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar herramientas: " + ex.Message);
            }
        }

        // ===================== GRILLA HISTORIAL =====================

        private void CargarHistorial()
        {
            try
            {
                var lista = herramientaDatos.GetHistorial();

                // Filtro por herramienta específica (comparación por id, no por nombre)
                if (cboFiltroHerramienta.SelectedIndex > 0 && cboFiltroHerramienta.SelectedItem is Herramienta seleccionada && seleccionada.id > 0)
                {
                    lista = lista.Where(m => m.id_herramienta == seleccionada.id).ToList();
                }

                // Filtro por estado (ahora sí: usa el campo estado devuelto por GetHistorial)
                if (cboFiltroEstado.SelectedIndex > 0)
                {
                    string estadoSeleccionado = cboFiltroEstado.SelectedItem.ToString();
                    lista = lista.Where(m => string.Equals(m.estado, estadoSeleccionado, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filtro por obra (incluye origen o destino)
                if (cboFiltroObra.SelectedIndex > 0 && cboFiltroObra.SelectedItem is Obra obraSeleccionada)
                {
                    string nombreObra = obraSeleccionada.id == -1 ? "Galpón" : obraSeleccionada.nombre;
                    lista = lista.Where(m =>
                        string.Equals(m.obra_origen, nombreObra, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.obra_destino, nombreObra, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filtro por texto de búsqueda (insensible a mayúsculas)
                string textoBusqueda = txtBuscar.Text.Trim();
                if (!string.IsNullOrEmpty(textoBusqueda))
                {
                    lista = lista.Where(m =>
                        (m.nombre_herramienta ?? "").IndexOf(textoBusqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (m.obra_origen ?? "").IndexOf(textoBusqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (m.obra_destino ?? "").IndexOf(textoBusqueda, StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList();
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
                dgvHistorial.ScrollBars = ScrollBars.Vertical;

                dgvHistorial.EnableHeadersVisualStyles = false;
                dgvHistorial.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                dgvHistorial.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvHistorial.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvHistorial.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvHistorial.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (dgvHistorial.Columns["id"] != null)
                    dgvHistorial.Columns["id"].Visible = false;
                if (dgvHistorial.Columns["id_herramienta"] != null)
                    dgvHistorial.Columns["id_herramienta"].Visible = false;

                dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                if (dgvHistorial.Columns["nombre_herramienta"] != null)
                {
                    dgvHistorial.Columns["nombre_herramienta"].HeaderText = "Herramienta";
                    dgvHistorial.Columns["nombre_herramienta"].FillWeight = 28;
                    dgvHistorial.Columns["nombre_herramienta"].DisplayIndex = 0;
                }
                if (dgvHistorial.Columns["estado"] != null)
                {
                    dgvHistorial.Columns["estado"].HeaderText = "Estado";
                    dgvHistorial.Columns["estado"].FillWeight = 14;
                    dgvHistorial.Columns["estado"].DisplayIndex = 1;
                }
                if (dgvHistorial.Columns["obra_origen"] != null)
                {
                    dgvHistorial.Columns["obra_origen"].HeaderText = "Origen";
                    dgvHistorial.Columns["obra_origen"].FillWeight = 20;
                    dgvHistorial.Columns["obra_origen"].DisplayIndex = 2;
                }
                if (dgvHistorial.Columns["obra_destino"] != null)
                {
                    dgvHistorial.Columns["obra_destino"].HeaderText = "Destino";
                    dgvHistorial.Columns["obra_destino"].FillWeight = 20;
                    dgvHistorial.Columns["obra_destino"].DisplayIndex = 3;
                }
                if (dgvHistorial.Columns["fecha"] != null)
                {
                    dgvHistorial.Columns["fecha"].HeaderText = "Fecha";
                    dgvHistorial.Columns["fecha"].FillWeight = 18;
                    dgvHistorial.Columns["fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    dgvHistorial.Columns["fecha"].DisplayIndex = 4;
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar historial: " + ex.Message);
            }
        }

        // ===================== FILTRO HISTORIAL =====================

        private void cboFiltroHerramienta_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva)
                CargarHistorial();
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva)
                CargarHistorial();
        }

        private void cboFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva)
                CargarHistorial();
        }

        private void cboFiltroObra_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva)
                CargarHistorial();
        }

        // ===================== TABS =====================

        private void btnVerHerramientas_Click(object sender, EventArgs e)
        {
            vistaHistorialActiva = false;
            panelGrid.Visible = true;
            panelGridHistorial.Visible = false;
            panelFiltro.Visible = false;
            btnVerHerramientas.FillColor = tabActiveColor;
            btnVerHistorial.FillColor = tabInactiveColor;
            CargarHerramientas();
        }

        private void btnVerHistorial_Click(object sender, EventArgs e)
        {
            vistaHistorialActiva = true;
            panelGrid.Visible = false;
            panelGridHistorial.Visible = true;
            panelFiltro.Visible = true;
            btnVerHerramientas.FillColor = tabInactiveColor;
            btnVerHistorial.FillColor = tabActiveColor;
            CargarComboFiltroHerramienta();
            CargarFiltrosHistorial();
            CargarHistorial();
        }

        // ===================== SELECTION =====================

        private void LimpiarSeleccion()
        {
            idSeleccionado = -1;
            txtNombre.Clear();
            txtObservaciones.Clear();
            cboEstado.SelectedIndex = 0;
            cboObra.SelectedIndex = 0;
            btnGuardar.Text = "Guardar";
            btnEliminar.Enabled = false;
            dgvHerramientas.ClearSelection();
        }

        private void dgvHerramientas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var fila = dgvHerramientas.Rows[e.RowIndex];
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

                string estado = fila.Cells["estado"].Value.ToString();
                for (int i = 0; i < cboEstado.Items.Count; i++)
                {
                    if (cboEstado.Items[i].ToString() == estado)
                    {
                        cboEstado.SelectedIndex = i;
                        break;
                    }
                }

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
                ToastNotification.ShowWarning("Debes escribir un nombre para la herramienta.");
                return;
            }

            string estado = cboEstado.SelectedItem.ToString();
            var obraSeleccionada = (Obra)cboObra.SelectedItem;
            int? idObra = obraSeleccionada.id == 0 ? (int?)null : obraSeleccionada.id;
            string observaciones = txtObservaciones.Text.Trim();

            string _msg = idSeleccionado == -1 ? "¿Crear esta herramienta?" : "¿Actualizar esta herramienta?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                if (idSeleccionado == -1)
                {
                    herramientaDatos.GuardarHerramienta(nombre, estado, idObra, observaciones);
                }
                else
                {
                    herramientaDatos.ActualizarHerramienta(idSeleccionado, nombre, estado, idObra, observaciones);
                }

                LimpiarSeleccion();
                CargarHerramientas();
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
                "¿Estás seguro de eliminar esta herramienta?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    herramientaDatos.EliminarHerramienta(idSeleccionado);
                    LimpiarSeleccion();
                    CargarHerramientas();
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("No se puede eliminar la herramienta: " + ex.Message);
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
                ImprimirHerramientas(e);
        }

        private void ImprimirHerramientas(PrintPageEventArgs e)
        {
            Font fontHeader = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubHeader = new Font("Arial", 10, FontStyle.Bold);
            Font fontBody = new Font("Arial", 9, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penLine = Pens.Black;

            float x = 40;
            float y = 40;
            float offset = 28;

            e.Graphics.DrawString("INFORME DE HERRAMIENTAS", fontHeader, brushText, x, y);
            y += offset;
            e.Graphics.DrawString("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brushText, x, y);
            y += offset + 10;

            float[] colWidths = new float[] { 180, 100, 150, 180 };
            string[] headers = new string[] { "Nombre", "Estado", "Ubicación", "Observaciones" };

            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.DarkGreen, currentX, y, colWidths[i], 25);
                e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 25);
                e.Graphics.DrawString(headers[i], fontSubHeader, Brushes.White, currentX + 4, y + 4);
                currentX += colWidths[i];
            }
            y += 25;

            foreach (DataGridViewRow row in dgvHerramientas.Rows)
            {
                if (row.IsNewRow) continue;
                if (y > e.MarginBounds.Bottom) break;

                currentX = x;
                string[] cells = new string[]
                {
                    row.Cells["nombre"].Value?.ToString() ?? "",
                    row.Cells["estado"].Value?.ToString() ?? "",
                    row.Cells["nombre_obra"].Value?.ToString() ?? "",
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
            e.Graphics.DrawString($"Total herramientas: {dgvHerramientas.Rows.Count}", fontSubHeader, brushText, x, y);
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

            e.Graphics.DrawString("HISTORIAL DE MOVIMIENTOS - HERRAMIENTAS", fontHeader, brushText, x, y);
            y += offset;

            string filtro = cboFiltroHerramienta.SelectedIndex > 0
                ? ((Herramienta)cboFiltroHerramienta.SelectedItem).nombre
                : "Todas";
            e.Graphics.DrawString("Filtro: " + filtro, fontBody, brushText, x, y);
            y += 18;
            e.Graphics.DrawString("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brushText, x, y);
            y += offset + 10;

            float[] colWidths = new float[] { 170, 150, 150, 140 };
            string[] headers = new string[] { "Herramienta", "Origen", "Destino", "Fecha" };

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
                    row.Cells["nombre_herramienta"].Value?.ToString() ?? "",
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
