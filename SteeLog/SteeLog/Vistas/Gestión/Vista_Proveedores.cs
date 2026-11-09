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
    public partial class Vista_Proveedores : Form
    {
        private CatalogosPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private List<ProveedorCatalogo> _items = new List<ProveedorCatalogo>();
        private int idSeleccionado = -1, _versionCarga;
        private bool _listo, _ocupado, _seleccionando, _guardando;
        internal Action<string> AvisarAdvertencia = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> AvisarError = mensaje => ToastNotification.ShowError(mensaje);
        internal Func<string, MessageBoxIcon, bool> Confirmar = (mensaje, icono) => MessageBox.Show(mensaje, "Confirmar", MessageBoxButtons.YesNo, icono) == DialogResult.Yes;

        public Vista_Proveedores()
        {
            InitializeComponent();
            txtProveedor.MaxLength = Proveedor.MaxNombre;
            ActualizarEstado();
        }
        internal Vista_Proveedores(CatalogosPresenter presenter) : this() { _presenter = presenter; }

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
            dgvProveedores.DataSource = null;
            dgvProveedores.DataSource = datos;
            dgvProveedores.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProveedores.RowHeadersVisible = false;
            dgvProveedores.AllowUserToResizeRows = false;
            dgvProveedores.ColumnHeadersHeight = 35;
            dgvProveedores.MultiSelect = false;
            dgvProveedores.ReadOnly = true;
            dgvProveedores.AllowUserToResizeColumns = false;
            dgvProveedores.EnableHeadersVisualStyles = false;
            dgvProveedores.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvProveedores.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvProveedores.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvProveedores.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvProveedores.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvProveedores.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            if (dgvProveedores.Columns["Id"] != null) dgvProveedores.Columns["Id"].Visible = false;
            if (dgvProveedores.Columns["Nombre"] != null) { dgvProveedores.Columns["Nombre"].HeaderText = "Nombre de proveedor"; dgvProveedores.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }

        }
        private void LimpiarSeleccion()
        {
            _seleccionando = true;
            try { idSeleccionado = -1; txtProveedor.Clear();  guna2Button1.Text = "Guardar"; dgvProveedores.ClearSelection(); }
            finally { _seleccionando = false; }

            ActualizarEstado();
        }
        private void dgvProveedores_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_listo || _ocupado || e.RowIndex < 0) return;
            var item = dgvProveedores.Rows[e.RowIndex].DataBoundItem as ProveedorCatalogo;
            if (item == null) return;
            if (idSeleccionado == item.Id) { LimpiarSeleccion(); return; }
            _seleccionando = true;
            try { idSeleccionado = item.Id; txtProveedor.Text = item.Nombre;  guna2Button1.Text = "Actualizar"; }
            finally { _seleccionando = false; }
            ActualizarEstado();
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
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo eliminar: " + ex.Message); }
            finally { _ocupado = false; if (Vigente) ActualizarEstado(); }
        }
        private void txtProveedor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { guna2Button1.PerformClick(); e.SuppressKeyPress = true; }
        }
    }
}
