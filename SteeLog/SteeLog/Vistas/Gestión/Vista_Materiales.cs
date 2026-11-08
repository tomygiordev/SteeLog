using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Data;

using System.Drawing;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

using System.Windows.Forms;

using System.Threading;
using SteeLog.Core.Application.Inventario;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;

using SteeLog.Modelos;

using SteeLog.Servicios;





namespace SteeLog.Vistas

{

    public partial class Vista_Materiales : Form

    {

        private readonly MaterialesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private int _versionLista, _versionProveedores;
        private bool _guardando, _cargandoProveedores, _proveedoresDisponibles = true;
        int idMaterialSeleccionado = -1;

        private decimal stockOriginalSeleccionado = 0;

        private List<int> idsProveedoresSeleccionados = new List<int>();

        private FiltroMaterial filtroAbierto = null;

        public Vista_Materiales() : this(new MaterialesPresenter(LocalServices.Current.Executor)) { }

        internal Vista_Materiales(MaterialesPresenter presenter)

        {

            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            InitializeComponent();

            this.Load += Vista_Materiales_Load;

            stAct.Text = "";

            stMin.Text = "";

            eliminarMaterialBoton.Enabled = false;

            FormClosed += (s, e) => _cierre.Cancel();
            Disposed += (s, e) => _cierre.Cancel();

        }

        private async void Vista_Materiales_Load(object sender, EventArgs e)

        {

            try
            {
                var categorias = await _presenter.CategoriasAsync(_cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                cmbCategoria.DataSource = categorias.ToList();
                cmbCategoria.DisplayMember = "Nombre";
                cmbCategoria.ValueMember = "Id";
                CargarMateriales();
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("No se pudieron cargar categorías: " + ex.Message); return; }



            // Fijar el ancho del dropdown para que no ocupe todo el panel

            cmbCategoria.DropDownWidth = 280;

            cmbCategoria.ItemHeight = 26;

            cmbCategoria.MaxDropDownItems = 10;

        }



        private void btnProveedores_Click(object sender, EventArgs e)

        {

            if (_guardando || _cargandoProveedores || !_proveedoresDisponibles) return;

            using (var modal = new ModalSeleccionProveedores(idsProveedoresSeleccionados, _presenter))

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

        private async void CargarMateriales(string nombre = null, bool soloStockBajo = false, FiltroMaterialParams filtro = null)

        {

            int version = ++_versionLista;
            try
            {
                var consulta = filtro == null ? new FiltroMaterialLectura { Nombre=nombre, SoloStockBajo=soloStockBajo } : new FiltroMaterialLectura { Nombre=filtro.Nombre, Desde=filtro.DesdeCantidad > 0 ? (decimal?)filtro.DesdeCantidad : null, Hasta=filtro.HastaCantidad > 0 ? (decimal?)filtro.HastaCantidad : null, Categorias=filtro.IdsCategoria.ToArray(), Proveedores=filtro.IdsProveedor.ToArray(), SoloStockBajo=filtro.SoloStockBajo };
                var materiales = await _presenter.ListarAsync(consulta, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionLista) return;
                var listaMateriales = materiales.Select(m => new Material { id=m.Id, nombre=m.Nombre, id_categoria=m.CategoriaId, nombre_categoria=m.Categoria, precioUnitario=m.Precio, stockActual=m.Stock, stockMinimo=m.Minimo, cantidadUsada=m.CantidadUsada, cantidadMovimientos=m.CantidadMovimientos }).ToList();
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

            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested && version == _versionLista) ToastNotification.ShowError("No se pudo cargar los materiales: " + ex.Message); }

        }





        private void button1_Click(object sender, EventArgs e)

        {

            this.Close();

        }





































        private async void guna2Button1_Click(object sender, EventArgs e)

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



            if (_guardando || _cargandoProveedores || !_proveedoresDisponibles) return;
            _guardando = true;
            dgvMateriales.Enabled = false; guna2Button1.Enabled = false; eliminarMaterialBoton.Enabled = false;
            try
            {
                var datos = new DatosMaterial { Nombre=nombre, CategoriaId=idCategoriaMaterial, StockMinimo=stockMinimo, Proveedores=idsProveedoresSeleccionados.ToArray() };

                if (idMaterialSeleccionado == -1)

                {

                    int nuevoId = await _presenter.CrearAsync(datos, stockActual, _cierre.Token);
                    if (_cierre.IsCancellationRequested || IsDisposed) return;

                    CargarMateriales();

                    LimpiarCampos();

                }

                else

                {

                    decimal stockAnterior = stockOriginalSeleccionado;
                    await _presenter.EditarAsync(idMaterialSeleccionado, datos, stockActual != stockAnterior ? (decimal?)stockActual : null, stockActual != stockAnterior ? (decimal?)stockAnterior : null, _cierre.Token);
                    if (_cierre.IsCancellationRequested || IsDisposed) return;

                    if (stockActual != stockAnterior)
                    {
                        var vistaGeneral = this.TopLevelControl as VistaGeneral;
                        vistaGeneral?.RefrescarHistorialSiAbierto();
                    }

                    CargarMateriales();
                    LimpiarCampos();

                }



            }

            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("No se pudo guardar el material: " + ex.Message); }
            finally { _guardando = false; if (!IsDisposed) { dgvMateriales.Enabled = true; guna2Button1.Enabled = true; eliminarMaterialBoton.Enabled = idMaterialSeleccionado != -1; } }
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



        private async void dgvMateriales_CellClick(object sender, DataGridViewCellEventArgs e)

        {

            if (e.RowIndex < 0) return;



                var fila = dgvMateriales.Rows[e.RowIndex];

                var clicID= (int)fila.Cells["id"].Value;

            if (idMaterialSeleccionado == clicID)

            {

                guna2Button1.Text = "Guardar Material";

                idMaterialSeleccionado = -1;
                _versionProveedores++; _cargandoProveedores = false; _proveedoresDisponibles = true;

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

                int version = ++_versionProveedores;
                _cargandoProveedores = true; _proveedoresDisponibles = false;
                try
                {
                    var proveedores = await _presenter.ProveedoresAsync(clicID, _cierre.Token);
                    if (_cierre.IsCancellationRequested || IsDisposed || version != _versionProveedores || idMaterialSeleccionado != clicID) return;
                    idsProveedoresSeleccionados = proveedores.ToList();
                    _proveedoresDisponibles = true;
                    ActualizarBotonProveedores();
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { if (!_cierre.IsCancellationRequested && version == _versionProveedores) ToastNotification.ShowError("No se pudieron cargar proveedores: " + ex.Message); return; }
                finally { if (version == _versionProveedores) _cargandoProveedores = false; }

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

            _versionProveedores++; _cargandoProveedores = false; _proveedoresDisponibles = true;
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



        private async void eliminarMaterialBoton_Click(object sender, EventArgs e)

        {

            DialogResult resultado = MessageBox.Show(

    "¿Estás seguro de que deseas eliminar este material?",

    "Confirmar Eliminación",

    MessageBoxButtons.YesNo,

    MessageBoxIcon.Warning

);





            if (resultado == DialogResult.Yes)

            {

                if (_guardando || _cargandoProveedores || !_proveedoresDisponibles) return;
                _guardando = true;
                dgvMateriales.Enabled = false; guna2Button1.Enabled = false; eliminarMaterialBoton.Enabled = false;
                try
                {
                    await _presenter.EliminarAsync(idMaterialSeleccionado, _cierre.Token);
                    if (_cierre.IsCancellationRequested || IsDisposed) return;

                    CargarMateriales();

                    LimpiarCampos();

                }

                catch (OperationCanceledException) { }
                catch (Exception) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("No se puede eliminar el material (está siendo usado en movimientos)."); }
                finally { _guardando = false; if (!IsDisposed) { dgvMateriales.Enabled = true; guna2Button1.Enabled = true; eliminarMaterialBoton.Enabled = idMaterialSeleccionado != -1; } }

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

                this.filtroAbierto = new FiltroMaterial(_presenter);

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
