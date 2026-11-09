using System;
using SteeLog.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Domain.Catalogos;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class Vista_Proveedores : Form
    {
        private readonly IServiciosVista servicios;
        private CatalogosPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private List<ProveedorCatalogo> _items = new List<ProveedorCatalogo>();
        private int idSeleccionado = -1, _versionCarga;
        private bool _listo, _ocupado, _seleccionando, _guardando;
        private int _idAntesDelClic = int.MinValue;
        internal Action<string> AvisarAdvertencia = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> AvisarError = mensaje => ToastNotification.ShowError(mensaje);
        internal Func<string, MessageBoxIcon, bool> Confirmar = (mensaje, icono) => GestionUi.Confirmar(mensaje, "Confirmar", icono);

        public Vista_Proveedores(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            InitializeComponent();
            GestionUi.AbmSimple(this, panelAntiParpadeo1, label1, panelAntiParpadeo2, panelAntiParpadeo4, label3, guna2Panel1, guna2Button1, eliminarProveedorBoton);
            txtProveedor.MaxLength = Proveedor.MaxNombre;
            dgvProveedores.CellMouseDown += (s, e) => { if (e.RowIndex >= 0) _idAntesDelClic = idSeleccionado; };
            dgvProveedores.SelectionChanged += dgvProveedores_SelectionChanged;
            VisibleChanged += (s, e) => { if (Visible && _listo) { SincronizarResaltado(); ActualizarEstado(); } };
            ActualizarEstado();
        }
        internal Vista_Proveedores(IServiciosVista servicios, CatalogosPresenter presenter) : this(servicios) { _presenter = presenter; }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return;
            try
            {
                if (_presenter == null) _presenter = new CatalogosPresenter(servicios.Executor);
                await CargarAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Error al cargar proveedores: " + ex.Message); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { CancelarCierre(); base.OnFormClosed(e); }
        private void CancelarCierre() { if (!_cierre.IsCancellationRequested) _cierre.Cancel(); }
        private bool Vigente => !_cierre.IsCancellationRequested && !IsDisposed;
        private void ActualizarEstado()
        {
            bool habilitado = _listo && !_ocupado && !_guardando;
            guna2Button1.Enabled = habilitado;
            eliminarProveedorBoton.Enabled = habilitado && idSeleccionado >= 0;
            txtProveedor.Enabled = habilitado;
            dgvProveedores.Enabled = habilitado;

        }
        private async Task CargarAsync()
        {
            int version = ++_versionCarga;
            _listo = false;
            ActualizarEstado();
            var items = await _presenter.ProveedoresAsync(_cierre.Token);
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
        private void PintarGrilla(List<ProveedorCatalogo> datos)
        {
            dgvProveedores.DataSource = null;
            dgvProveedores.DataSource = datos;
            dgvProveedores.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProveedores.RowHeadersVisible = false;
            dgvProveedores.AllowUserToResizeRows = false;
            dgvProveedores.MultiSelect = false;
            dgvProveedores.ReadOnly = true;
            dgvProveedores.AllowUserToResizeColumns = false;
            if (dgvProveedores.Columns["Id"] != null) dgvProveedores.Columns["Id"].Visible = false;
            if (dgvProveedores.Columns["Nombre"] != null) { dgvProveedores.Columns["Nombre"].HeaderText = "Nombre de proveedor"; dgvProveedores.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }

            GestionUi.AjustarColumnas(dgvProveedores);
        }
        private void LimpiarSeleccion()
        {
            _seleccionando = true;
            try { idSeleccionado = -1; txtProveedor.Clear();  guna2Button1.Text = "Guardar"; dgvProveedores.ClearSelection(); }
            finally { _seleccionando = false; }

            ActualizarEstado();
        }
        private void SeleccionarItem(ProveedorCatalogo item)
        {
            _seleccionando = true;
            try { idSeleccionado = item.Id; txtProveedor.Text = item.Nombre;  guna2Button1.Text = "Actualizar"; }
            finally { _seleccionando = false; }
            ActualizarEstado();
        }
        /// <summary>Deja resaltado en la grilla exactamente lo que está seleccionado lógicamente (o nada).</summary>
        private void SincronizarResaltado()
        {
            bool previo = _seleccionando; _seleccionando = true;
            try
            {
                dgvProveedores.ClearSelection();
                try { dgvProveedores.CurrentCell = null; } catch (InvalidOperationException) { }
                if (idSeleccionado >= 0)
                    foreach (DataGridViewRow fila in dgvProveedores.Rows)
                        if ((fila.DataBoundItem as ProveedorCatalogo)?.Id == idSeleccionado) { fila.Selected = true; break; }
            }
            finally { _seleccionando = previo; }
        }
        private void dgvProveedores_SelectionChanged(object sender, EventArgs e)
        {
            if (_seleccionando || !_listo || _ocupado || dgvProveedores.SelectedRows.Count == 0) return;
            var item = dgvProveedores.SelectedRows[0].DataBoundItem as ProveedorCatalogo;
            if (item != null && item.Id != idSeleccionado) SeleccionarItem(item);
        }
        private void dgvProveedores_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_listo || _ocupado || e.RowIndex < 0) return;
            var item = dgvProveedores.Rows[e.RowIndex].DataBoundItem as ProveedorCatalogo;
            if (item == null) return;
            int previo = _idAntesDelClic == int.MinValue ? idSeleccionado : _idAntesDelClic;
            _idAntesDelClic = int.MinValue;
            if (previo == item.Id) { LimpiarSeleccion(); return; }
            if (idSeleccionado != item.Id) SeleccionarItem(item);
        }
        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || _guardando) return;
            string nombre = txtProveedor.Text.Trim();
            var errorNombre = Proveedor.ValidarNombre(nombre);
            if (errorNombre != null) { AvisarAdvertencia(errorNombre); return; }
            int? id = idSeleccionado < 0 ? (int?)null : idSeleccionado;
            _guardando = true; ActualizarEstado();
            bool confirmado;
            try { confirmado = Confirmar(id.HasValue ? "¿Actualizar este proveedor?" : "¿Crear este proveedor?", MessageBoxIcon.Question); }
            catch { _guardando = false; ActualizarEstado(); throw; }
            if (!confirmado) { _guardando = false; ActualizarEstado(); txtProveedor.Focus(); return; }
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.GuardarProveedorAsync(id, nombre, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == (id ?? -1) && txtProveedor.Text.Trim() == nombre) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Guardado, pero no se pudo actualizar el listado: " + ex.Message); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) AvisarError(CatalogosPresenter.MensajeGuardado(ex)); }
            finally { _ocupado = false; _guardando = false; if (Vigente) ActualizarEstado(); }
        }
        private async void eliminarProveedorBoton_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || idSeleccionado < 0) return;
            int id = idSeleccionado;
            if (!Confirmar("¿Estás seguro de eliminar este proveedor?", MessageBoxIcon.Warning)) return;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.EliminarProveedorAsync(id, _cierre.Token);
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
        private void txtProveedor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { guna2Button1.PerformClick(); e.SuppressKeyPress = true; }
        }
    }
}
