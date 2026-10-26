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
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;
using Npgsql;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Materiales : Form
    {
        int idMaterialSeleccionado = -1;
        private decimal stockOriginalSeleccionado = 0;
        private List<int> idsProveedoresSeleccionados = new List<int>();
        private FiltroMaterial filtroAbierto = null;
        public Vista_Materiales()
        {
            InitializeComponent();
            this.Load += Vista_Materiales_Load;
            stAct.Text = "";
            stMin.Text = "";
            eliminarMaterialBoton.Enabled = false;
            CargarMateriales();
        }
        private void Vista_Materiales_Load(object sender, EventArgs e)
        {
            MaterialDatos datos1 = new MaterialDatos();
            CategoriaDatos datos = new CategoriaDatos();
            cmbCategoria.DataSource = datos.GetCategorias();
            cmbCategoria.DisplayMember = "nombre";
            cmbCategoria.ValueMember = "id";

            // Fijar el ancho del dropdown para que no ocupe todo el panel
            cmbCategoria.DropDownWidth = 280;
            cmbCategoria.ItemHeight = 26;
            cmbCategoria.MaxDropDownItems = 10;
        }

        private void btnProveedores_Click(object sender, EventArgs e)
        {
            using (var modal = new ModalSeleccionProveedores(idsProveedoresSeleccionados))
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    idsProveedoresSeleccionados = modal.IDsSeleccionados;
                    btnProveedores.Text = $"Seleccionar Proveedores ({idsProveedoresSeleccionados.Count})";
                }
            }
        }

        private void ActualizarBotonProveedores()
        {
            btnProveedores.Text = $"Seleccionar Proveedores ({idsProveedoresSeleccionados.Count})";
        }
        private void CargarMateriales(string nombre = null, bool soloStockBajo = false, FiltroMaterialParams filtro = null)
        {
            try
            {
                MaterialDatos datos = new MaterialDatos();
                var listaMateriales = filtro != null
                    ? datos.obtenerMateriales(filtro)
                    : datos.obtenerMateriales(nombre, soloStockBajo);
                dgvMateriales.DataSource = new SortableBindingList<Material>(listaMateriales);

                dgvMateriales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvMateriales.MultiSelect = false; 
                dgvMateriales.ReadOnly = true;
                dgvMateriales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvMateriales.AllowUserToResizeColumns = false;
                dgvMateriales.AllowUserToResizeRows = false;


                dgvMateriales.EnableHeadersVisualStyles = false;
                dgvMateriales.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgvMateriales.ColumnHeadersHeight = 35;

                dgvMateriales.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                dgvMateriales.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgvMateriales.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                dgvMateriales.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                dgvMateriales.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvMateriales.ColumnHeadersDefaultCellStyle.Font = new Font("Bahnschrift Light", 11F);
                dgvMateriales.DefaultCellStyle.Font = new Font("Bahnschrift Light", 12F);
                dgvMateriales.DefaultCellStyle.SelectionBackColor = Color.FromArgb(47, 84, 57);
                dgvMateriales.DefaultCellStyle.SelectionForeColor = Color.White;

                var columnasPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "nombre", "nombre_categoria", "stockActual", "stockMinimo"
                };
                foreach (DataGridViewColumn col in dgvMateriales.Columns)
                {
                    if (!columnasPermitidas.Contains(col.Name))
                        col.Visible = false;
                }

                if (dgvMateriales.Columns["id"] != null) dgvMateriales.Columns["id"].Visible = false;

                if (dgvMateriales.Columns["nombre"] != null)
                {
                    dgvMateriales.Columns["nombre"].HeaderText = "Nombre del Material";
                    dgvMateriales.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvMateriales.Columns["nombre"].FillWeight = 30;
                    dgvMateriales.Columns["nombre"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                if (dgvMateriales.Columns["id_categoria"] != null)
                {
                    dgvMateriales.Columns["id_categoria"].Visible = false;
                    /*dgvMateriales.Columns["id_categoria"].HeaderText = "Categoría";
                    dgvMateriales.Columns["id_categoria"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvMateriales.Columns["id_categoria"].FillWeight = 20;
                    dgvMateriales.Columns["id_categoria"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    */
                }
                if (dgvMateriales.Columns["nombre_categoria"] != null)
                {
                    dgvMateriales.Columns["nombre_categoria"].Visible = true;
                    dgvMateriales.Columns["nombre_categoria"].HeaderText = "Categoría";
                    dgvMateriales.Columns["nombre_categoria"].DataPropertyName = "nombre_categoria";
                    dgvMateriales.Columns["nombre_categoria"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvMateriales.Columns["nombre_categoria"].FillWeight = 10;
                }
                if (dgvMateriales.Columns["unidad"] != null)
                {
                    /* dgvMateriales.Columns["unidad"].HeaderText = "Unidad";
                     dgvMateriales.Columns["unidad"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                     dgvMateriales.Columns["unidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;*/
                    dgvMateriales.Columns["unidad"].Visible = false;
                }

                if (dgvMateriales.Columns["valorTotal"] != null)
                {
                    dgvMateriales.Columns["valorTotal"].Visible = false;
                }

                if (dgvMateriales.Columns["stockActual"] != null)
                {
                    dgvMateriales.Columns["stockActual"].HeaderText = SessionManager.DebeOcultarCantidadesStockMateriales ? "Disponibilidad" : "Stock";
                    dgvMateriales.Columns["stockActual"].DefaultCellStyle.Format = SessionManager.DebeOcultarCantidadesStockMateriales ? string.Empty : "N0";
                    dgvMateriales.Columns["stockActual"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dgvMateriales.Columns["stockActual"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                if (dgvMateriales.Columns["stockMinimo"] != null)
                {
                    dgvMateriales.Columns["stockMinimo"].Visible = !SessionManager.DebeOcultarCantidadesStockMateriales;
                    dgvMateriales.Columns["stockMinimo"].HeaderText = "Mínimo";
                    dgvMateriales.Columns["stockMinimo"].DefaultCellStyle.Format = "N0";
                    dgvMateriales.Columns["stockMinimo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dgvMateriales.Columns["stockMinimo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }

                if (dgvMateriales.Columns["cantidadUsada"] != null)
                {
                    dgvMateriales.Columns["cantidadUsada"].Visible = false;
                }

                if (dgvMateriales.Columns["cantidadMovimientos"] != null)
                {
                    dgvMateriales.Columns["cantidadMovimientos"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cargar los materiales: " + ex.Message);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }


















        private void guna2Button1_Click(object sender, EventArgs e)
        {
            string nombre = nombreMaterial.Text;
            int idCategoriaMaterial = Convert.ToInt32(cmbCategoria.SelectedValue);
            decimal stockActual=stAct.Value;
            decimal stockMinimo=stMin.Value;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ToastNotification.ShowWarning("Completa los campos obligatorios.");
                return;
            }

            if (idCategoriaMaterial <= 0)
            {
                ToastNotification.ShowWarning("Selecciona una categoría válida.");
                return;
            }
            string _msg = idMaterialSeleccionado == -1 ? "¿Crear este material?" : "¿Actualizar este material?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                MaterialDatos datos = new MaterialDatos();
                if (idMaterialSeleccionado == -1)
                {
                    int nuevoId = datos.guardarMaterial(nombre, stockActual, stockMinimo, idCategoriaMaterial);
                    if (nuevoId > 0 && idsProveedoresSeleccionados != null && idsProveedoresSeleccionados.Count > 0)
                    {
                        datos.GuardarProveedoresMaterial(nuevoId, idsProveedoresSeleccionados);
                    }
                    CargarMateriales();
                    LimpiarCampos();
                }
                else
                {
                    decimal stockAnterior = stockOriginalSeleccionado;
                    datos.editarMaterial(nombre, stockActual, stockMinimo, idCategoriaMaterial, idMaterialSeleccionado);
                    datos.GuardarProveedoresMaterial(idMaterialSeleccionado, idsProveedoresSeleccionados);

                    // Registrar movimiento solo si el stock cambió
                    if (stockActual != stockAnterior)
                    {
                        try
                        {
                            MovimientoDatos movDatos = new MovimientoDatos();
                            // Delta signed: positivo si subió el stock, negativo si bajó
                            decimal signedDelta = stockActual - stockAnterior;

                            // Buscar si ya hay un remito interno de hoy
                            int idRemitoHoy = movDatos.BuscarRemitoInternoDiario(DateTime.Today);

                            if (idRemitoHoy > 0)
                            {
                                // Ya existe remito del día → acumular en la fila del material (o insertar si es nuevo material)
                                movDatos.ActualizarOAgregarDetalle(idRemitoHoy, idMaterialSeleccionado, signedDelta, stockActual);
                            }
                            else
                            {
                                // No existe → crear remito nuevo con su detalle
                                Movimiento mov = new Movimiento();
                                mov.fecha = DateTime.Now;
                                mov.nroremito = "141414";
                                mov.id_tipomovimiento = 11; // Registro Interno
                                mov.id_origen = 1; // Galpón
                                mov.tipo_origen = "UBICACION";
                                mov.id_destino = 1; // Galpón
                                mov.tipo_destino = "UBICACION";
                                mov.id_obra = 0;
                                mov.observaciones = "Actualización interna de stock";

                                List<MovimientoMaterial> detalles = new List<MovimientoMaterial>();
                                detalles.Add(new MovimientoMaterial
                                {
                                    id_material = idMaterialSeleccionado,
                                    cantidad = signedDelta,
                                    StockActual = stockActual,
                                    EsRecorte = false
                                });

                                movDatos.GuardarMovimiento(mov, detalles);
                            }
                        }
                        catch (Exception ex)
                        {
                            ToastNotification.ShowError("No se pudo registrar el cambio en el historial: " + ex.Message);
                        }

                        // Refrescar el historial si está abierto
                        var vistaGeneral = this.TopLevelControl as VistaGeneral;
                        vistaGeneral?.RefrescarHistorialSiAbierto();
                    }
                    
                    CargarMateriales();
                    LimpiarCampos();
                }
              
            }
            catch (Exception)
            {
                ToastNotification.ShowError("El material ya existe o los datos son inválidos.");
            }
        }



        private void dgvMateriales_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvMateriales.Rows.Count)
            {
                var row = dgvMateriales.Rows[e.RowIndex];
                if (row.DataBoundItem is Material item)
                {
                    if (SessionManager.DebeOcultarCantidadesStockMateriales
                        && dgvMateriales.Columns[e.ColumnIndex].Name == "stockActual")
                    {
                        e.Value = SessionManager.ObtenerEstadoStockMaterial(item.stockActual);
                        e.FormattingApplied = true;
                    }

                    if (item.stockMinimo > 0 && item.stockActual <= item.stockMinimo)
                    {
                        e.CellStyle.ForeColor = Color.FromArgb(255, 70, 70);
                        e.CellStyle.SelectionForeColor = Color.FromArgb(255, 120, 120);
                        e.CellStyle.Font = new System.Drawing.Font(e.CellStyle.Font, FontStyle.Bold);
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.SelectionForeColor = Color.White;
                    }
                }
            }
        }

        private void dgvMateriales_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

                var fila = dgvMateriales.Rows[e.RowIndex];
                var clicID= (int)fila.Cells["id"].Value;
            if (idMaterialSeleccionado == clicID)
            {
                guna2Button1.Text = "Guardar Material";
                idMaterialSeleccionado = -1;
                dgvMateriales.ClearSelection();
                nombreMaterial.Clear();
                stAct.Text = "";
                stMin.Text = "";
                cmbCategoria.Text = "";
                idsProveedoresSeleccionados.Clear();
                ActualizarBotonProveedores();
                eliminarMaterialBoton.Enabled = false;
            }
            else
            {
                idMaterialSeleccionado = clicID;
                nombreMaterial.Text = fila.Cells["nombre"].Value.ToString();
                stAct.Text = fila.Cells["stockActual"].Value?.ToString() ?? "0";
                stockOriginalSeleccionado = Convert.ToDecimal(fila.Cells["stockActual"].Value ?? 0);
                stMin.Text = fila.Cells["stockMinimo"].Value?.ToString() ?? "0";
                cmbCategoria.SelectedValue = fila.Cells["id_categoria"].Value;
                // Cargar proveedores del material seleccionado
                MaterialDatos matDatos = new MaterialDatos();
                idsProveedoresSeleccionados = matDatos.ObtenerProveedoresMaterial(clicID);
                ActualizarBotonProveedores();
                guna2Button1.Text = "Actualizar Material";
                eliminarMaterialBoton.Enabled = true;
            }
        }
        private void dgvMateriales_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ignorado para evitar limpieza accidental si el usuario hace doble clic rápido
        }

        private void LimpiarCampos()
        {
            guna2Button1.Text = "Guardar Material";
            idMaterialSeleccionado = -1;
            idsProveedoresSeleccionados.Clear();
            ActualizarBotonProveedores();
            dgvMateriales.ClearSelection();
            nombreMaterial.Clear();
            stAct.Value = 0;
            stMin.Value = 0;
            stockOriginalSeleccionado = 0;
            cmbCategoria.SelectedIndex = -1;
            eliminarMaterialBoton.Enabled = false;
        }












        private void guna2TextBox1_TextChanged_1(object sender, EventArgs e)
        {
            string nombre = txtFiltroMaterial.Text.Trim();
            CargarMateriales(string.IsNullOrEmpty(nombre) ? null : nombre);
        }

        private void eliminarMaterialBoton_Click(object sender, EventArgs e)
        {
            DialogResult resultado = MessageBox.Show(
    "¿Estás seguro de que deseas eliminar este material?",
    "Confirmar Eliminación",                             
    MessageBoxButtons.YesNo,                             
    MessageBoxIcon.Warning                               
);

            
            if (resultado == DialogResult.Yes)
            {
                try
                {
                    MaterialDatos datos = new MaterialDatos();
                    datos.eliminarMaterial(idMaterialSeleccionado);
                    CargarMateriales();
                    LimpiarCampos();
                }
                catch (Exception)
                {
                    ToastNotification.ShowError("No se puede eliminar el material (está siendo usado en movimientos).");
                }
            }
        }




        private void abrirFiltros(FiltroMaterial filtroForm)
        {
            filtroForm.StartPosition = FormStartPosition.Manual;

            // Obtenemos el área de trabajo del monitor actual para asegurar responsividad
            Rectangle areaTrabajo = Screen.FromControl(this).WorkingArea;
            Point botonScreen = filtrarMaterial.PointToScreen(Point.Empty);

            // Intentamos posicionarlo a la izquierda del botón, centrado verticalmente respecto al mismo
            int x = botonScreen.X - filtroForm.Width - 10;
            int y = botonScreen.Y - (filtroForm.Height / 2) + (filtrarMaterial.Height / 2);

            // Si se sale por la izquierda de la pantalla, lo movemos a la derecha del botón
            if (x < areaTrabajo.Left)
            {
                x = botonScreen.X + filtrarMaterial.Width + 10;
            }

            // Validaciones verticales para que no se salga por arriba ni por abajo
            if (y < areaTrabajo.Top)
            {
                y = areaTrabajo.Top + 10;
            }
            else if (y + filtroForm.Height > areaTrabajo.Bottom)
            {
                y = areaTrabajo.Bottom - filtroForm.Height - 10;
            }

            // Validación final horizontal por si el monitor es muy estrecho
            if (x + filtroForm.Width > areaTrabajo.Right)
            {
                x = areaTrabajo.Right - filtroForm.Width - 10;
            }

            filtroForm.Location = new Point(x, y);
            this.filtroAbierto = filtroForm;
            filtroForm.Show(this);
        }
        private void RecibirFiltro(object sender, FiltroMaterialParams filtro)
        {
            CargarMateriales(filtro: filtro);
        }
        private void filtrarMaterial_Click(object sender, EventArgs e)
        {
            if (this.filtroAbierto == null || this.filtroAbierto.IsDisposed)
            {
                this.filtroAbierto = new FiltroMaterial();
                this.filtroAbierto.onBuscar += RecibirFiltro;
            }
            abrirFiltros(this.filtroAbierto);
        }

        }


    }

    public class ProveedorItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        public ProveedorItem(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public override string ToString()
        {
            return Nombre;
        }
    }
