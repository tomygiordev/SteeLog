using System;
using System.Collections.Generic;
using SteeLog.UI;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SteeLog.Presentation;
using SteeLog.Modelos;

namespace SteeLog.Vistas
{
    public partial class Vista_Recortes : Form
    {
        private readonly IServiciosVista servicios;
        private readonly RecortesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private int _versionRecortes, _versionMateriales;
        private bool _guardando;
        private List<Recorte> _recortes = new List<Recorte>();

        public Vista_Recortes(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _presenter = new RecortesPresenter(servicios.Executor);
            InitializeComponent();
            GestionUi.Abm(this, panelAntiParpadeo1, label1, new[] { panelAntiParpadeo2, panelAntiParpadeo3 }, btnGuardar, btnEliminar);
            GestionUi.Subtitulo(panelAntiParpadeo4, label7);
            guna2Panel1.Dock = DockStyle.Fill;
            CargarEstadosRecorte();
            Shown += (s, e) => { CargarCategoriasRecortables(); CargarRecortes(); };
            FormClosed += (s, e) => _cierre.Cancel();
            Disposed += (s, e) => _cierre.Cancel();
            
            cmbCategorias.SelectedIndexChanged += CmbCategorias_SelectedIndexChanged;
            cmbMateriales.SelectedIndexChanged += CmbMateriales_SelectedIndexChanged;
            txtAncho.KeyPress += TxtDimension_KeyPress;
            txtAltura.KeyPress += TxtDimension_KeyPress;
            txtCantidad.KeyPress += TxtCantidad_KeyPress;
            txtBusquedaRecortes.TextChanged += TxtBusquedaRecortes_TextChanged;
            dgvRecortes.SelectionChanged += (s, e) => ActualizarBotonEliminar();
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

        private async void CargarCategoriasRecortables()
        {
            try
            {
                var categorias = await _presenter.CategoriasAsync(_cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                cmbCategorias.DataSource = categorias.ToList();
                cmbCategorias.DisplayMember = "Nombre"; cmbCategorias.ValueMember = "Id";
                cmbCategorias.SelectedIndex = -1;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("Error al cargar categorías: " + ex.Message); }
        }

        private void CmbCategorias_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCategorias.SelectedIndex != -1)
            {
                if (cmbCategorias.SelectedValue is int idCat) CargarMaterialesPorCategoria(idCat);
            }
            else
            {
                _versionMateriales++;
                cmbMateriales.DataSource = null;
            }
        }

        private async void CargarMaterialesPorCategoria(int idCategoria)
        {
            int version = ++_versionMateriales;
            try
            {
                var materiales = await _presenter.MaterialesAsync(idCategoria, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionMateriales) return;
                cmbMateriales.DataSource = materiales.ToList();
                cmbMateriales.DisplayMember = "Nombre"; cmbMateriales.ValueMember = "Id";
                cmbMateriales.SelectedIndex = -1;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested && version == _versionMateriales) ToastNotification.ShowError("Error al cargar materiales: " + ex.Message); }
        }

        private void CmbMateriales_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private async void CargarRecortes()
        {
            int version = ++_versionRecortes;
            try
            {
                var lista = await _presenter.ListarAsync(null, null, null, null, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionRecortes) return;
                _recortes = lista.Select(r => new Recorte { id=r.Id, id_material=r.MaterialId, nombre_material=r.Material, nombre_categoria=r.Categoria, ancho=r.Ancho, altura=r.Altura, medida=r.Medida, cantidad=r.Cantidad, estado=r.Estado, fecha_creacion=r.FechaCreacion }).ToList();
                AplicarFiltroBusqueda();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested && version == _versionRecortes) ToastNotification.ShowError("Error al cargar recortes: " + ex.Message); }
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
            dgvRecortes.ClearSelection();
            ActualizarBotonEliminar();
        }

        private void ActualizarBotonEliminar()
        {
            btnEliminar.Enabled = !_guardando && dgvRecortes.SelectedRows.Count > 0;
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
            dgvRecortes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRecortes.MultiSelect = false;
            dgvRecortes.ReadOnly = true;
            dgvRecortes.RowHeadersVisible = false;
            dgvRecortes.AllowUserToResizeColumns = false;
            dgvRecortes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

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
                dgvRecortes.Columns["ancho"].DefaultCellStyle.Format = "N2";
            }
            if (dgvRecortes.Columns["altura"] != null)
            {
                dgvRecortes.Columns["altura"].HeaderText = "Altura (m)";
                dgvRecortes.Columns["altura"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["altura"].FillWeight = 10;
                dgvRecortes.Columns["altura"].DefaultCellStyle.Format = "N2";
            }
            if (dgvRecortes.Columns["medida"] != null)
            {
                dgvRecortes.Columns["medida"].HeaderText = "Medida (m)";
                dgvRecortes.Columns["medida"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortes.Columns["medida"].FillWeight = 12;
                dgvRecortes.Columns["medida"].DefaultCellStyle.Format = "N2";
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
            GestionUi.AjustarColumnas(dgvRecortes);
        }

        private void TxtDimension_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
            {
                e.Handled = true;
            }
        }

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if (cmbMateriales.SelectedIndex == -1)
            {
                ToastNotification.ShowWarning("Seleccione un material.");
                return;
            }

            string inputAncho = txtAncho.Text;
            string inputAltura = txtAltura.Text;

            bool hasAncho = NumeroDecimal.TryParse(inputAncho, out decimal ancho) && ancho > 0;
            bool hasAltura = NumeroDecimal.TryParse(inputAltura, out decimal altura) && altura > 0;

            if (!hasAncho && !hasAltura)
            {
                ToastNotification.ShowWarning("Ingrese al menos un ancho o una altura mayor a 0.");
                return;
            }

            string inputCantidad = txtCantidad.Text;
            bool hasCantidad = NumeroDecimal.TryParse(inputCantidad, out decimal cantidad) && cantidad > 0;

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

            if (!GestionUi.Confirmar("¿Guardar este recorte?", "Confirmar", MessageBoxIcon.Question)) return;

            if (_guardando) return;
            _guardando = true;
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
                await _presenter.AgregarAsync(nuevo.id_material, nuevo.ancho, nuevo.altura, nuevo.medida, nuevo.cantidad, nuevo.estado, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                ToastNotification.ShowSuccess("Recorte guardado correctamente.");
                txtAncho.Clear();
                txtAltura.Clear();
                txtCantidad.Text = "1";
                cmbEstado.SelectedIndex = -1;
                CargarRecortes();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("Error al guardar: " + ex.Message); }
            finally { _guardando = false; }
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_guardando || dgvRecortes.SelectedRows.Count == 0) return;

            var recorte = (Recorte)dgvRecortes.SelectedRows[0].DataBoundItem;
            int id = recorte.id;
            if (GestionUi.Confirmar("¿Seguro que desea eliminar este recorte?", "Confirmar", MessageBoxIcon.Warning))
            {
                if (_guardando) return;
                _guardando = true; ActualizarBotonEliminar();
                try
                {
                    await _presenter.EliminarAsync(id, recorte.id_material, _cierre.Token);
                    if (!_cierre.IsCancellationRequested && !IsDisposed) CargarRecortes();
                }
                catch (OperationCanceledException) { }
                catch (Exception) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("No se puede eliminar el recorte (posiblemente esté en uso)."); }
                finally { _guardando = false; ActualizarBotonEliminar(); }
            }
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
