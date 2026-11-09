using System;
using SteeLog.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Domain.Catalogos;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class Vista_Categorias : Form
    {
        private CatalogosPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private List<CategoriaCatalogo> _items = new List<CategoriaCatalogo>();
        private int idSeleccionado = -1, _versionCarga;
        private bool _listo, _ocupado, _seleccionando, _guardando;
        private int _idAntesDelClic = int.MinValue;
        internal Action<string> AvisarAdvertencia = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> AvisarError = mensaje => ToastNotification.ShowError(mensaje);
        internal Func<string, MessageBoxIcon, bool> Confirmar = (mensaje, icono) => GestionUi.Confirmar(mensaje, "Confirmar", icono);

        public Vista_Categorias()
        {
            InitializeComponent();
            GestionUi.AbmSimple(this, panelAntiParpadeo1, label1, panelAntiParpadeo2, panelAntiParpadeo4, label3, guna2Panel1, guna2Button1, eliminarCategoriaBoton);
            txtCategoria.MaxLength = Categoria.MaxNombre;
            dgvCategorias.CellMouseDown += (s, e) => { if (e.RowIndex >= 0) _idAntesDelClic = idSeleccionado; };
            dgvCategorias.SelectionChanged += dgvCategorias_SelectionChanged;
            VisibleChanged += (s, e) => { if (Visible && _listo) { SincronizarResaltado(); ActualizarEstado(); } };
            ActualizarEstado();
        }
        internal Vista_Categorias(CatalogosPresenter presenter) : this() { _presenter = presenter; }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return;
            try
            {
                if (_presenter == null) _presenter = new CatalogosPresenter(LocalServices.Current.Executor);
                await CargarAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Error al cargar categorías: " + ex.Message); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { CancelarCierre(); base.OnFormClosed(e); }
        private void CancelarCierre() { if (!_cierre.IsCancellationRequested) _cierre.Cancel(); }
        private bool Vigente => !_cierre.IsCancellationRequested && !IsDisposed;
        private void ActualizarEstado()
        {
            bool habilitado = _listo && !_ocupado && !_guardando;
            guna2Button1.Enabled = habilitado;
            eliminarCategoriaBoton.Enabled = habilitado && idSeleccionado >= 0;
            txtCategoria.Enabled = habilitado;
            dgvCategorias.Enabled = habilitado;
            chkRecortable.Enabled = habilitado;
        }
        private async Task CargarAsync()
        {
            int version = ++_versionCarga;
            _listo = false;
            ActualizarEstado();
            var items = await _presenter.CategoriasAsync(_cierre.Token);
            if (!Vigente || version != _versionCarga) return;
            _items = items.ToList();
            _listo = true;
            MostrarItems();
            ActualizarEstado();
        }
        private void MostrarItems()
        {
            var datos = _items;
            bool previoSeleccionando = _seleccionando; _seleccionando = true;
            try { PintarGrilla(datos); SincronizarResaltado(); }
            finally { _seleccionando = previoSeleccionando; }
        }
        private void PintarGrilla(List<CategoriaCatalogo> datos)
        {
            dgvCategorias.DataSource = null;
            dgvCategorias.DataSource = datos;
            dgvCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCategorias.RowHeadersVisible = false;
            dgvCategorias.AllowUserToResizeRows = false;
            dgvCategorias.MultiSelect = false;
            dgvCategorias.ReadOnly = true;
            dgvCategorias.AllowUserToResizeColumns = false;
            if (dgvCategorias.Columns["Id"] != null) dgvCategorias.Columns["Id"].Visible = false;
            if (dgvCategorias.Columns["Nombre"] != null) { dgvCategorias.Columns["Nombre"].HeaderText = "Nombre de categoría"; dgvCategorias.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            if (dgvCategorias.Columns["Recortable"] != null) { dgvCategorias.Columns["Recortable"].HeaderText = "Recortable"; dgvCategorias.Columns["Recortable"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; dgvCategorias.Columns["Recortable"].FillWeight = 25; }
            GestionUi.AjustarColumnas(dgvCategorias);
        }
        private void LimpiarSeleccion()
        {
            _seleccionando = true;
            try { idSeleccionado = -1; txtCategoria.Clear(); chkRecortable.Checked = false; guna2Button1.Text = "Guardar"; dgvCategorias.ClearSelection(); }
            finally { _seleccionando = false; }

            ActualizarEstado();
        }
        private void SeleccionarItem(CategoriaCatalogo item)
        {
            _seleccionando = true;
            try { idSeleccionado = item.Id; txtCategoria.Text = item.Nombre; chkRecortable.Checked = item.Recortable; guna2Button1.Text = "Actualizar"; }
            finally { _seleccionando = false; }
            ActualizarEstado();
        }
        /// <summary>Deja resaltado en la grilla exactamente lo que está seleccionado lógicamente (o nada).</summary>
        private void SincronizarResaltado()
        {
            bool previo = _seleccionando; _seleccionando = true;
            try
            {
                dgvCategorias.ClearSelection();
                try { dgvCategorias.CurrentCell = null; } catch (InvalidOperationException) { }
                if (idSeleccionado >= 0)
                    foreach (DataGridViewRow fila in dgvCategorias.Rows)
                        if ((fila.DataBoundItem as CategoriaCatalogo)?.Id == idSeleccionado) { fila.Selected = true; break; }
            }
            finally { _seleccionando = previo; }
        }
        private void dgvCategorias_SelectionChanged(object sender, EventArgs e)
        {
            if (_seleccionando || !_listo || _ocupado || dgvCategorias.SelectedRows.Count == 0) return;
            var item = dgvCategorias.SelectedRows[0].DataBoundItem as CategoriaCatalogo;
            if (item != null && item.Id != idSeleccionado) SeleccionarItem(item);
        }
        private void dgvCategorias_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_listo || _ocupado || e.RowIndex < 0) return;
            var item = dgvCategorias.Rows[e.RowIndex].DataBoundItem as CategoriaCatalogo;
            if (item == null) return;
            int previo = _idAntesDelClic == int.MinValue ? idSeleccionado : _idAntesDelClic;
            _idAntesDelClic = int.MinValue;
            if (previo == item.Id) { LimpiarSeleccion(); return; }
            if (idSeleccionado != item.Id) SeleccionarItem(item);
        }
        private async void guna2Button1_Click_1(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || _guardando) return;
            string nombre = txtCategoria.Text.Trim();
            var errorNombre = Categoria.ValidarNombre(nombre);
            if (errorNombre != null) { AvisarAdvertencia(errorNombre); return; }
            int? id = idSeleccionado < 0 ? (int?)null : idSeleccionado;
            _guardando = true; ActualizarEstado();
            bool confirmado;
            try { confirmado = Confirmar(id.HasValue ? "¿Actualizar esta categoría?" : "¿Crear esta categoría?", MessageBoxIcon.Question); }
            catch { _guardando = false; ActualizarEstado(); throw; }
            if (!confirmado) { _guardando = false; ActualizarEstado(); txtCategoria.Focus(); return; }
            bool estado = chkRecortable.Checked;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.GuardarCategoriaAsync(id, nombre, estado, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == (id ?? -1) && txtCategoria.Text.Trim() == nombre && chkRecortable.Checked == estado) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Guardado, pero no se pudo actualizar el listado: " + ex.Message); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) AvisarError(CatalogosPresenter.MensajeGuardado(ex)); }
            finally { _ocupado = false; _guardando = false; if (Vigente) ActualizarEstado(); }
        }
        private async void eliminarCategoriaBoton_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || idSeleccionado < 0) return;
            int id = idSeleccionado;
            if (!Confirmar("¿Estás seguro de eliminar esta categoría?", MessageBoxIcon.Warning)) return;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.EliminarCategoriaAsync(id, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == id) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Eliminado, pero no se pudo actualizar el listado: " + ex.Message); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) AvisarError(CatalogosPresenter.MensajeEliminacion(ex)); }
            finally { _ocupado = false; if (Vigente) ActualizarEstado(); }
        }
        private void txtCategoria_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { guna2Button1.PerformClick(); e.SuppressKeyPress = true; }
        }
        private void txtCategoria_TextChanged(object sender, EventArgs e) { }
        private void Vista_Categorias_Load_1(object sender, EventArgs e) { }
    }
}
