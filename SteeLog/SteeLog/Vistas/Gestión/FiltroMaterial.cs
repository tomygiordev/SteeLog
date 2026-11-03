using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Inventario;
using SteeLog.Modelos;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class FiltroMaterial : Form
    {
        public event EventHandler<FiltroMaterialParams> onBuscar;

        private readonly MaterialesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private HashSet<int> _categoriasPrevias = new HashSet<int>();
        private HashSet<int> _proveedoresPrevios = new HashSet<int>();

        public decimal? StockDesde { get; private set; }
        public decimal? StockHasta { get; private set; }
        public List<int> CategoriasSeleccionadas { get; private set; } = new List<int>();
        public List<int> ProveedoresSeleccionados { get; private set; } = new List<int>();
        public bool SoloStockBajo { get; private set; }
        public bool SeLimpioFiltro { get; private set; } = false;

        private CheckedListBox clbCategorias => flCategoria;
        private CheckedListBox clbProveedores => clbProveedoresFiltro;

        private sealed class ItemLista
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre ?? string.Empty;
        }

        public FiltroMaterial() : this(null)
        {
        }

        internal FiltroMaterial(MaterialesPresenter presenter)
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(20, 30, 25);
            try
            {
                this.Region = System.Drawing.Region.FromHrgn(Guna2ControlHelper.CreateRoundRectRgn(0, 0, this.Width, this.Height, 15, 15));
            }
            catch { }

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _presenter = presenter ?? new MaterialesPresenter(LocalServices.Current.Executor);
            FormClosed += (_, __) => _cierre.Cancel();
            Disposed += (_, __) => _cierre.Dispose();
            Shown += FiltroMaterial_Shown;
        }

        private async void FiltroMaterial_Shown(object sender, EventArgs e)
        {
            await CargarListasAsync();
        }

        private async Task CargarListasAsync()
        {
            try
            {
                var categorias = await _presenter.CategoriasAsync(_cierre.Token);
                var proveedores = await _presenter.CatalogoProveedoresAsync(_cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;

                clbCategorias.Items.Clear();
                foreach (var cat in categorias)
                {
                    int idx = clbCategorias.Items.Add(new ItemLista { Id = cat.Id, Nombre = cat.Nombre });
                    if (_categoriasPrevias.Contains(cat.Id))
                        clbCategorias.SetItemChecked(idx, true);
                }

                clbProveedores.Items.Clear();
                foreach (var prov in proveedores)
                {
                    int idx = clbProveedores.Items.Add(new ItemLista { Id = prov.Id, Nombre = prov.Nombre });
                    if (_proveedoresPrevios.Contains(prov.Id))
                        clbProveedores.SetItemChecked(idx, true);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Error al cargar listas para filtrar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CargarEstadoPrevio(decimal? desde, decimal? hasta, List<int> categoriasprevias, List<int> proveedoresPrevios, bool stockBajo)
        {
            if (desde.HasValue)
            {
                desdeCantidadFiltro.Value = Math.Max(desdeCantidadFiltro.Minimum, Math.Min(desdeCantidadFiltro.Maximum, desde.Value));
                StockDesde = desde.Value;
            }
            if (hasta.HasValue)
            {
                hastaCantidadFiltro.Value = Math.Max(hastaCantidadFiltro.Minimum, Math.Min(hastaCantidadFiltro.Maximum, hasta.Value));
                StockHasta = hasta.Value;
            }
            sionoBajo.Checked = stockBajo;
            SoloStockBajo = stockBajo;

            _categoriasPrevias = categoriasprevias != null ? new HashSet<int>(categoriasprevias) : new HashSet<int>();
            _proveedoresPrevios = proveedoresPrevios != null ? new HashSet<int>(proveedoresPrevios) : new HashSet<int>();

            for (int i = 0; i < clbCategorias.Items.Count; i++)
            {
                if (clbCategorias.Items[i] is ItemLista item)
                    clbCategorias.SetItemChecked(i, _categoriasPrevias.Contains(item.Id));
            }

            for (int i = 0; i < clbProveedores.Items.Count; i++)
            {
                if (clbProveedores.Items[i] is ItemLista item)
                    clbProveedores.SetItemChecked(i, _proveedoresPrevios.Contains(item.Id));
            }
        }

        private void AplicarFiltros_Click(object sender, EventArgs e)
        {
            if (desdeCantidadFiltro.Value > 0 && hastaCantidadFiltro.Value > 0 && desdeCantidadFiltro.Value > hastaCantidadFiltro.Value)
            {
                MessageBox.Show("El stock 'Desde' no puede ser mayor que el stock 'Hasta'.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StockDesde = desdeCantidadFiltro.Value > 0 ? (decimal?)desdeCantidadFiltro.Value : null;
            StockHasta = hastaCantidadFiltro.Value > 0 ? (decimal?)hastaCantidadFiltro.Value : null;
            SoloStockBajo = sionoBajo.Checked;

            CategoriasSeleccionadas.Clear();
            foreach (var item in clbCategorias.CheckedItems)
            {
                if (item is ItemLista il)
                    CategoriasSeleccionadas.Add(il.Id);
            }

            ProveedoresSeleccionados.Clear();
            foreach (var item in clbProveedores.CheckedItems)
            {
                if (item is ItemLista il)
                    ProveedoresSeleccionados.Add(il.Id);
            }

            SeLimpioFiltro = false;

            var p = new FiltroMaterialParams
            {
                DesdeCantidad = StockDesde ?? 0,
                HastaCantidad = StockHasta ?? 0,
                SoloStockBajo = SoloStockBajo,
                IdsCategoria = new List<int>(CategoriasSeleccionadas),
                IdsProveedor = new List<int>(ProveedoresSeleccionados)
            };

            onBuscar?.Invoke(this, p);
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void LimpiarFiltros_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbCategorias.Items.Count; i++) clbCategorias.SetItemChecked(i, false);
            for (int i = 0; i < clbProveedores.Items.Count; i++) clbProveedores.SetItemChecked(i, false);
            desdeCantidadFiltro.Value = 0;
            hastaCantidadFiltro.Value = 0;
            sionoBajo.Checked = false;

            StockDesde = null;
            StockHasta = null;
            CategoriasSeleccionadas.Clear();
            ProveedoresSeleccionados.Clear();
            SoloStockBajo = false;
            SeLimpioFiltro = true;

            onBuscar?.Invoke(this, new FiltroMaterialParams());
            this.DialogResult = DialogResult.OK;
        }

        private void btnAplicar_Click(object sender, EventArgs e) => AplicarFiltros_Click(sender, e);
        private void btnLimpiar_Click(object sender, EventArgs e) => LimpiarFiltros_Click(sender, e);

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Form mainForm = null;
            if (this.Owner != null)
            {
                Control parent = this.Owner;
                while (parent.Parent != null)
                    parent = parent.Parent;
                mainForm = parent as Form;
            }
            this.Hide();
            if (mainForm != null && !mainForm.IsDisposed)
            {
                var target = mainForm;
                target.BeginInvoke(new Action(() =>
                {
                    if (!target.IsDisposed)
                    {
                        target.Activate();
                    }
                }));
            }
        }

        private void FiltroMaterial_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void FiltroMaterial_Load(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void panelAntiParpadeo1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void sionoBajo_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void flCategoria_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }
    }

    public static class Guna2ControlHelper
    {
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
    }
}
