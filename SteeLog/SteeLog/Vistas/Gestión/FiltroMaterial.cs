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
using SteeLog.UI;

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
            AplicarEstilo();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _presenter = presenter ?? new MaterialesPresenter(LocalServices.Current.Executor);
            FormClosed += (_, __) => _cierre.Cancel();
            Disposed += (_, __) => _cierre.Dispose();
            Shown += FiltroMaterial_Shown;
        }

        /// <summary>Popup coherente con el resto de modales: barra propia, borde 1px, controles del tema.</summary>
        private void AplicarEstilo()
        {
            Tema.SinEscala(this);
            Tema.ChromeModal(this, "Filtrar materiales");
            Padding = new Padding(1);
            panelAntiParpadeo1.BringToFront(); // el panel Fill debe acoplarse último, tras la barra de título
            panelAntiParpadeo1.ColumnStyles[1].Width = 150;
            BackColor = Tema.Borde;
            panelAntiParpadeo1.BackColor = Tema.Superficie;
            panelAntiParpadeo1.Padding = new Padding(16, 4, 16, 14);
            // La X nativa del panel sobra: la barra propia ya la trae.
            guna2ControlBox1.Visible = false;
            panelAntiParpadeo1.RowStyles[0].Height = 0;
            GestionUi.Aplicar(panelAntiParpadeo1);
            foreach (var titulo in new[] { label3, label7, label9 })
            {
                titulo.Font = Tema.FuenteSubtitulo; titulo.ForeColor = Tema.TextoSecundario; titulo.Padding = new Padding(0, 0, 0, 4);
            }
            label3.Text = "Rango de stock";
            foreach (var etiqueta in new[] { label5, label6 })
            {
                etiqueta.Font = Tema.Fuente(10.5f); etiqueta.ForeColor = Tema.TextoSecundario; etiqueta.Padding = Padding.Empty;
            }
            label8.Font = Tema.Fuente(10.5f); label8.ForeColor = Tema.Texto; label8.Padding = Padding.Empty; label8.Text = "Solo stock bajo";
            Tema.Toggle(sionoBajo);
            GestionUi.ListaChecks(flCategoria);
            GestionUi.ListaChecks(clbProveedoresFiltro);
            flCategoria.Dock = DockStyle.Fill; clbProveedoresFiltro.Dock = DockStyle.Fill;
            flCategoria.Margin = new Padding(0, 2, 0, 2); clbProveedoresFiltro.Margin = new Padding(0, 2, 0, 2);
            Tema.Boton(AplicarFiltros, Variante.Primario);
            Tema.Boton(LimpiarFiltros, Variante.Secundario);
            AplicarFiltros.Text = "Aplicar filtros"; LimpiarFiltros.Text = "Borrar filtros";
            AplicarFiltros.Height = Tema.AltoBotonPrincipal; LimpiarFiltros.Height = Tema.AltoBotonPrincipal;
            AplicarFiltros.Margin = new Padding(0, 4, 0, 4); LimpiarFiltros.Margin = new Padding(0, 4, 0, 4);
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
                    Dialogo.Error("Error al cargar listas para filtrar: " + ex.Message, "Error", this);
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
                Dialogo.Advertencia("El stock 'Desde' no puede ser mayor que el stock 'Hasta'.", "Atención", this);
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
    }
}
