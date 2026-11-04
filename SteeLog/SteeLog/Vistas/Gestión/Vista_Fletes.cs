using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class Vista_Fletes : Form
    {
        private CatalogosPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private List<FleteCatalogo> _items = new List<FleteCatalogo>();
        private int idSeleccionado = -1, _versionCarga;
        private bool _listo, _ocupado, _seleccionando;
        internal Func<string, MessageBoxIcon, bool> Confirmar = (mensaje, icono) => MessageBox.Show(mensaje, "Confirmar", MessageBoxButtons.YesNo, icono) == DialogResult.Yes;

        public Vista_Fletes()
        {
            InitializeComponent();
            ActualizarEstado();
        }
        internal Vista_Fletes(CatalogosPresenter presenter) : this() { _presenter = presenter; }

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
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Error al cargar fletes: " + ex.Message); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { CancelarCierre(); base.OnFormClosed(e); }
        private void CancelarCierre() { if (!_cierre.IsCancellationRequested) _cierre.Cancel(); }
        private bool Vigente => !_cierre.IsCancellationRequested && !IsDisposed;
        private void ActualizarEstado()
        {
            bool habilitado = _listo && !_ocupado;
            guna2Button1.Enabled = habilitado;
            eliminarFleteBoton.Enabled = habilitado && idSeleccionado >= 0;
            txtFlete.Enabled = habilitado;
            dgvFletes.Enabled = habilitado;
            chkActivo.Enabled = habilitado;
        }
        private async Task CargarAsync()
        {
            int version = ++_versionCarga;
            _listo = false;
            ActualizarEstado();
            var items = await _presenter.FletesAsync(_cierre.Token);
            if (!Vigente || version != _versionCarga) return;
            _items = items.ToList();
            _listo = true;
            MostrarItems();
            ActualizarEstado();
        }
        private void MostrarItems()
        {
            var datos = _items;
            dgvFletes.DataSource = null;
            dgvFletes.DataSource = datos;
            dgvFletes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFletes.RowHeadersVisible = false;
            dgvFletes.AllowUserToResizeRows = false;
            dgvFletes.ColumnHeadersHeight = 35;
            dgvFletes.MultiSelect = false;
            dgvFletes.ReadOnly = true;
            dgvFletes.AllowUserToResizeColumns = false;
            dgvFletes.EnableHeadersVisualStyles = false;
            dgvFletes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvFletes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvFletes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvFletes.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvFletes.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvFletes.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            if (dgvFletes.Columns["Id"] != null) dgvFletes.Columns["Id"].Visible = false;
            if (dgvFletes.Columns["Nombre"] != null) { dgvFletes.Columns["Nombre"].HeaderText = "Nombre de flete"; dgvFletes.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            if (dgvFletes.Columns["Activo"] != null) { dgvFletes.Columns["Activo"].HeaderText = "Activo"; dgvFletes.Columns["Activo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; dgvFletes.Columns["Activo"].FillWeight = 25; }
        }
        private void LimpiarSeleccion()
        {
            _seleccionando = true;
            try { idSeleccionado = -1; txtFlete.Clear(); chkActivo.Checked = true; guna2Button1.Text = "Guardar"; dgvFletes.ClearSelection(); }
            finally { _seleccionando = false; }

            ActualizarEstado();
        }
        private void dgvFletes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!_listo || _ocupado || e.RowIndex < 0) return;
            var item = dgvFletes.Rows[e.RowIndex].DataBoundItem as FleteCatalogo;
            if (item == null) return;
            if (idSeleccionado == item.Id) { LimpiarSeleccion(); return; }
            _seleccionando = true;
            try { idSeleccionado = item.Id; txtFlete.Text = item.Nombre; chkActivo.Checked = item.Activo; guna2Button1.Text = "Actualizar"; }
            finally { _seleccionando = false; }
            ActualizarEstado();
        }
        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado) return;
            string nombre = txtFlete.Text.Trim();
            if (string.IsNullOrEmpty(nombre)) { ToastNotification.ShowWarning("Debes escribir un nombre."); return; }
            int? id = idSeleccionado < 0 ? (int?)null : idSeleccionado;
            if (!Confirmar(id.HasValue ? "¿Actualizar este flete?" : "¿Crear este flete?", MessageBoxIcon.Question)) return;
            bool estado = chkActivo.Checked;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.GuardarFleteAsync(id, nombre, estado, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == (id ?? -1) && txtFlete.Text.Trim() == nombre && chkActivo.Checked == estado) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Guardado, pero no se pudo actualizar el listado: " + ex.Message); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Error al guardar: " + ex.Message); }
            finally { _ocupado = false; if (Vigente) ActualizarEstado(); }
        }
        private async void eliminarFleteBoton_Click(object sender, EventArgs e)
        {
            if (!_listo || _ocupado || idSeleccionado < 0) return;
            int id = idSeleccionado;
            if (!Confirmar("¿Estás seguro de desactivar este flete?", MessageBoxIcon.Warning)) return;
            _ocupado = true; ActualizarEstado();
            try
            {
                await _presenter.EliminarFleteAsync(id, _cierre.Token);
                if (!Vigente) return;
                if (idSeleccionado == id) LimpiarSeleccion();
                try { await CargarAsync(); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (Vigente) ToastNotification.ShowError("Cambio confirmado, pero no se pudo actualizar el listado: " + ex.Message); }
                if (Vigente) ToastNotification.ShowSuccess("Flete desactivado (borrado lógico).");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo desactivar: " + ex.Message); }
            finally { _ocupado = false; if (Vigente) ActualizarEstado(); }
        }
        private void txtFlete_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { guna2Button1.PerformClick(); e.SuppressKeyPress = true; }
        }
    }
}
