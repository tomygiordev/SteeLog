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
    public partial class Vista_Obras : Form
    {
        private CatalogosPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private List<ObraCatalogo> _items = new List<ObraCatalogo>();
        private int idSeleccionado = -1, _versionCarga;
        private bool _listo, _ocupado, _seleccionando, _guardando;
        private int _idAntesDelClic = int.MinValue;
        internal Action<string> AvisarAdvertencia = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> AvisarError = mensaje => ToastNotification.ShowError(mensaje);
        internal Func<string, MessageBoxIcon, bool> Confirmar = (mensaje, icono) => GestionUi.Confirmar(mensaje, "Confirmar", icono);

        public Vista_Obras()
        {
            InitializeComponent();
            GestionUi.AbmSimple(this, panelAntiParpadeo1, label1, panelAntiParpadeo2, panelAntiParpadeo4, label3, guna2Panel1, guna2Button1, eliminarObraBoton);
            txtObra.MaxLength = Obra.MaxNombre;
            dgvObras.CellMouseDown += (s, e) => { if (e.RowIndex >= 0) _idAntesDelClic = idSeleccionado; };
            dgvObras.SelectionChanged += dgvObras_SelectionChanged;
            VisibleChanged += (s, e) => { if (Visible && _listo) { SincronizarResaltado(); ActualizarEstado(); } };
            ActualizarEstado();
        }
        internal Vista_Obras(CatalogosPresenter presenter) : this() { _presenter = presenter; }

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
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Error al cargar obras: " + ex.Message); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { CancelarCierre(); base.OnFormClosed(e); }
        private void CancelarCierre() { if (!_cierre.IsCancellationRequested) _cierre.Cancel(); }
        private bool Vigente => !_cierre.IsCancellationRequested && !IsDisposed;
        private void ActualizarEstado()
        {
            bool habilitado = _listo && !_ocupado && !_guardando;
            guna2Button1.Enabled = habilitado;
            eliminarObraBoton.Enabled = habilitado && idSeleccionado >= 0;
            txtObra.Enabled = habilitado;
            dgvObras.Enabled = habilitado;
            chkActiva.Enabled = habilitado;
        }
        private async Task CargarAsync()
        {
            int version = ++_versionCarga;
            _listo = false;
            ActualizarEstado();
            var items = await _presenter.ObrasAsync(_cierre.Token);
            if (!Vigente || version != _versionCarga) return;
            _items = items.ToList();
            _listo = true;
            MostrarItems();
            ActualizarEstado();
        }
        private void MostrarItems()
        {
            var datos = _items;
            if (!_seleccionando && idSeleccionado < 0 && !string.IsNullOrWhiteSpace(txtObra.Text)) datos = _items.Where(x => x.Nombre.IndexOf(txtObra.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            bool previoSeleccionando = _seleccionando; _seleccionando = true;
            try { PintarGrilla(datos); SincronizarResaltado(); }
            finally { _seleccionando = previoSeleccionando; }
        }
        private void PintarGrilla(List<ObraCatalogo> datos)
        {
            dgvObras.DataSource = null;
            dgvObras.DataSource = datos;
            dgvObras.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvObras.RowHeadersVisible = false;
            dgvObras.AllowUserToResizeRows = false;
            dgvObras.MultiSelect = false;
            dgvObras.ReadOnly = true;
            dgvObras.AllowUserToResizeColumns = false;
            if (dgvObras.Columns["Id"] != null) dgvObras.Columns["Id"].Visible = false;
            if (dgvObras.Columns["Nombre"] != null) { dgvObras.Columns["Nombre"].HeaderText = "Nombre de obra"; dgvObras.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            if (dgvObras.Columns["Activa"] != null) { dgvObras.Columns["Activa"].HeaderText = "Estado"; dgvObras.Columns["Activa"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; dgvObras.Columns["Activa"].FillWeight = 25; }
            GestionUi.AjustarColumnas(dgvObras);
        }
        private void LimpiarSeleccion()
        {
            _seleccionando = true;
            try { idSeleccionado = -1; txtObra.Clear(); chkActiva.Checked = true; guna2Button1.Text = "Guardar"; dgvObras.ClearSelection(); }
            finally { _seleccionando = false; }
            MostrarItems();
            ActualizarEstado();
        }
        private void SeleccionarItem(ObraCatalogo item)
        {
            _seleccionando = true;
            try { idSeleccionado = item.Id; txtObra.Text = item.Nombre; chkActiva.Checked = item.Activa; guna2Button1.Text = "Actualizar"; }
            finally { _seleccionando = false; }
            ActualizarEstado();
        }
        /// <summary>Deja resaltado en la grilla exactamente lo que está seleccionado lógicamente (o nada).</summary>
        private void SincronizarResaltado()
        {
            bool previo = _seleccionando; _seleccionando = true;
            try
            {
                dgvObras.ClearSelection();
                try { dgvObras.CurrentCell = null; } catch (InvalidOperationException) { }
                if (idSeleccionado >= 0)
                    foreach (DataGridViewRow fila in dgvObras.Rows)
                        if ((fila.DataBoundItem as ObraCatalogo)?.Id == idSeleccionado) { fila.Selected = true; break; }
            }
            finally { _seleccionando = previo; }
        }
        private void dgvObras_SelectionChanged(object sender, EventArgs e)
        {
            if (_seleccionando || !_listo || _ocupado || dgvObras.SelectedRows.Count == 0) return;
            var item = dgvObras.SelectedRows[0].DataBoundItem as ObraCatalogo;
            if (item != null && item.Id != idSeleccionado) SeleccionarItem(item);
        }
        private void dgvObras_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_listo || _ocupado || e.RowIndex < 0) return;
            var item = dgvObras.Rows[e.RowIndex].DataBoundItem as ObraCatalogo;
            if (item == null) return;
            int previo = _idAntesDelClic == int.MinValue ? idSeleccionado : _idAntesDelClic;
            _idAntesDelClic = int.MinValue;
            if (previo == item.Id) { LimpiarSeleccion(); return; }
            if (idSeleccionado != item.Id) SeleccionarItem(item);
        }
        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || _guardando) return;
            string nombre = txtObra.Text.Trim();
            var errorNombre = Obra.ValidarNombre(nombre);
            if (errorNombre != null) { AvisarAdvertencia(errorNombre); return; }
            int? id = idSeleccionado < 0 ? (int?)null : idSeleccionado;
            _guardando = true; ActualizarEstado();
            bool confirmado;
            try { confirmado = Confirmar(id.HasValue ? "¿Actualizar esta obra?" : "¿Crear esta obra?", MessageBoxIcon.Question); }
            catch { _guardando = false; ActualizarEstado(); throw; }
            if (!confirmado) { _guardando = false; ActualizarEstado(); txtObra.Focus(); return; }
            bool estado = chkActiva.Checked;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.GuardarObraAsync(id, nombre, estado, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == (id ?? -1) && txtObra.Text.Trim() == nombre && chkActiva.Checked == estado) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Guardado, pero no se pudo actualizar el listado: " + ex.Message); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) AvisarError(CatalogosPresenter.MensajeGuardado(ex)); }
            finally { _ocupado = false; _guardando = false; if (Vigente) ActualizarEstado(); }
        }
        private async void eliminarObraBoton_Click_1(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || idSeleccionado < 0) return;
            int id = idSeleccionado;
            if (!Confirmar("¿Estás seguro de eliminar esta obra?", MessageBoxIcon.Warning)) return;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.EliminarObraAsync(id, _cierre.Token);
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
        private void txtObra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { guna2Button1.PerformClick(); e.SuppressKeyPress = true; }
        }
        private void txtObra_TextChanged(object sender, EventArgs e) { if (!_seleccionando) MostrarItems(); }
        private void panelAntiParpadeo1_Paint(object sender, PaintEventArgs e) { }
    }
}
