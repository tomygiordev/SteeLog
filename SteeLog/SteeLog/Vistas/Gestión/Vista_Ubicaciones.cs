using System;
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
    public partial class Vista_Ubicaciones : Form
    {
        private CatalogosPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private List<UbicacionCatalogo> _items = new List<UbicacionCatalogo>();
        private int idSeleccionado = -1, _versionCarga;
        private bool _listo, _ocupado, _seleccionando, _guardando;
        private int _idAntesDelClic = int.MinValue;
        internal Action<string> AvisarAdvertencia = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> AvisarError = mensaje => ToastNotification.ShowError(mensaje);
        internal Func<string, MessageBoxIcon, bool> Confirmar = (mensaje, icono) => MessageBox.Show(mensaje, "Confirmar", MessageBoxButtons.YesNo, icono) == DialogResult.Yes;

        public Vista_Ubicaciones()
        {
            InitializeComponent();
            txtUbicacion.MaxLength = Ubicacion.MaxNombre;
            dgvUbicaciones.CellMouseDown += (s, e) => { if (e.RowIndex >= 0) _idAntesDelClic = idSeleccionado; };
            dgvUbicaciones.SelectionChanged += dgvUbicaciones_SelectionChanged;
            VisibleChanged += (s, e) => { if (Visible && _listo) { SincronizarResaltado(); ActualizarEstado(); } };
            ActualizarEstado();
        }
        internal Vista_Ubicaciones(CatalogosPresenter presenter) : this() { _presenter = presenter; }

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
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Error al cargar ubicaciones: " + ex.Message); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { CancelarCierre(); base.OnFormClosed(e); }
        private void CancelarCierre() { if (!_cierre.IsCancellationRequested) _cierre.Cancel(); }
        private bool Vigente => !_cierre.IsCancellationRequested && !IsDisposed;
        private void ActualizarEstado()
        {
            bool habilitado = _listo && !_ocupado && !_guardando;
            guna2Button1.Enabled = habilitado;
            eliminarUbicacionBoton.Enabled = habilitado && idSeleccionado >= 0;
            txtUbicacion.Enabled = habilitado;
            dgvUbicaciones.Enabled = habilitado;

        }
        private async Task CargarAsync()
        {
            int version = ++_versionCarga;
            _listo = false;
            ActualizarEstado();
            var items = await _presenter.UbicacionesAsync(_cierre.Token);
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
        private void PintarGrilla(List<UbicacionCatalogo> datos)
        {
            dgvUbicaciones.DataSource = null;
            dgvUbicaciones.DataSource = datos;
            dgvUbicaciones.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUbicaciones.RowHeadersVisible = false;
            dgvUbicaciones.AllowUserToResizeRows = false;
            dgvUbicaciones.ColumnHeadersHeight = 35;
            dgvUbicaciones.MultiSelect = false;
            dgvUbicaciones.ReadOnly = true;
            dgvUbicaciones.AllowUserToResizeColumns = false;
            dgvUbicaciones.EnableHeadersVisualStyles = false;
            dgvUbicaciones.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvUbicaciones.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvUbicaciones.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUbicaciones.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvUbicaciones.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvUbicaciones.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            if (dgvUbicaciones.Columns["Id"] != null) dgvUbicaciones.Columns["Id"].Visible = false;
            if (dgvUbicaciones.Columns["Nombre"] != null) { dgvUbicaciones.Columns["Nombre"].HeaderText = "Nombre de ubicación"; dgvUbicaciones.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }

        }
        private void LimpiarSeleccion()
        {
            _seleccionando = true;
            try { idSeleccionado = -1; txtUbicacion.Clear();  guna2Button1.Text = "Guardar"; dgvUbicaciones.ClearSelection(); }
            finally { _seleccionando = false; }

            ActualizarEstado();
        }
        private void SeleccionarItem(UbicacionCatalogo item)
        {
            _seleccionando = true;
            try { idSeleccionado = item.Id; txtUbicacion.Text = item.Nombre;  guna2Button1.Text = "Actualizar"; }
            finally { _seleccionando = false; }
            ActualizarEstado();
        }
        /// <summary>Deja resaltado en la grilla exactamente lo que está seleccionado lógicamente (o nada).</summary>
        private void SincronizarResaltado()
        {
            bool previo = _seleccionando; _seleccionando = true;
            try
            {
                dgvUbicaciones.ClearSelection();
                try { dgvUbicaciones.CurrentCell = null; } catch (InvalidOperationException) { }
                if (idSeleccionado >= 0)
                    foreach (DataGridViewRow fila in dgvUbicaciones.Rows)
                        if ((fila.DataBoundItem as UbicacionCatalogo)?.Id == idSeleccionado) { fila.Selected = true; break; }
            }
            finally { _seleccionando = previo; }
        }
        private void dgvUbicaciones_SelectionChanged(object sender, EventArgs e)
        {
            if (_seleccionando || !_listo || _ocupado || dgvUbicaciones.SelectedRows.Count == 0) return;
            var item = dgvUbicaciones.SelectedRows[0].DataBoundItem as UbicacionCatalogo;
            if (item != null && item.Id != idSeleccionado) SeleccionarItem(item);
        }
        private void dgvUbicaciones_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_listo || _ocupado || e.RowIndex < 0) return;
            var item = dgvUbicaciones.Rows[e.RowIndex].DataBoundItem as UbicacionCatalogo;
            if (item == null) return;
            int previo = _idAntesDelClic == int.MinValue ? idSeleccionado : _idAntesDelClic;
            _idAntesDelClic = int.MinValue;
            if (previo == item.Id) { LimpiarSeleccion(); return; }
            if (idSeleccionado != item.Id) SeleccionarItem(item);
        }
        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || _guardando) return;
            string nombre = txtUbicacion.Text.Trim();
            var errorNombre = Ubicacion.ValidarNombre(nombre);
            if (errorNombre != null) { AvisarAdvertencia(errorNombre); return; }
            int? id = idSeleccionado < 0 ? (int?)null : idSeleccionado;
            _guardando = true; ActualizarEstado();
            bool confirmado;
            try { confirmado = Confirmar(id.HasValue ? "¿Actualizar esta ubicación?" : "¿Crear esta ubicación?", MessageBoxIcon.Question); }
            catch { _guardando = false; ActualizarEstado(); throw; }
            if (!confirmado) { _guardando = false; ActualizarEstado(); txtUbicacion.Focus(); return; }
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.GuardarUbicacionAsync(id, nombre, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == (id ?? -1) && txtUbicacion.Text.Trim() == nombre) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Guardado, pero no se pudo actualizar el listado: " + ex.Message); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) AvisarError(CatalogosPresenter.MensajeGuardado(ex)); }
            finally { _ocupado = false; _guardando = false; if (Vigente) ActualizarEstado(); }
        }
        private async void eliminarUbicacionBoton_Click_1(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || idSeleccionado < 0) return;
            int id = idSeleccionado;
            if (!Confirmar("¿Estás seguro de eliminar esta ubicación?", MessageBoxIcon.Warning)) return;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.EliminarUbicacionAsync(id, _cierre.Token);
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
        private void txtUbicacion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { guna2Button1.PerformClick(); e.SuppressKeyPress = true; }
        }
        private void txtUbicacion_TextChanged(object sender, EventArgs e) { }
        private void Vista_Ubicaciones_Load(object sender, EventArgs e) { }
    }
}
