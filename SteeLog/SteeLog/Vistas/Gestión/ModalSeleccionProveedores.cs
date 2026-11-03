using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class ModalSeleccionProveedores : Form
    {
        private readonly MaterialesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private readonly HashSet<int> _idsIniciales;

        public List<int> IDsSeleccionados { get; private set; } = new List<int>();
        public List<int> ProveedoresSeleccionadosIds => IDsSeleccionados;

        private sealed class ItemProveedor
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre ?? string.Empty;
        }

        public ModalSeleccionProveedores(List<int> idsIniciales) : this(idsIniciales, null)
        {
        }

        internal ModalSeleccionProveedores(List<int> idsIniciales, MaterialesPresenter presenter)
        {
            InitializeComponent();
            _idsIniciales = idsIniciales != null ? new HashSet<int>(idsIniciales) : new HashSet<int>();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _presenter = presenter ?? new MaterialesPresenter(LocalServices.Current.Executor);
            FormClosed += (_, __) => _cierre.Cancel();
            Disposed += (_, __) => _cierre.Dispose();
            Shown += ModalSeleccionProveedores_Shown;
        }

        private async void ModalSeleccionProveedores_Shown(object sender, EventArgs e)
        {
            await CargarProveedoresAsync();
        }

        private async Task CargarProveedoresAsync()
        {
            try
            {
                var listaTodos = await _presenter.CatalogoProveedoresAsync(_cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;

                clbProveedores.Items.Clear();
                foreach (var prov in listaTodos)
                {
                    int index = clbProveedores.Items.Add(new ItemProveedor { Id = prov.Id, Nombre = prov.Nombre });
                    if (_idsIniciales.Contains(prov.Id))
                        clbProveedores.SetItemChecked(index, true);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Error al cargar la lista de proveedores: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            IDsSeleccionados.Clear();
            foreach (var item in clbProveedores.CheckedItems)
            {
                if (item is ItemProveedor ip)
                    IDsSeleccionados.Add(ip.Id);
                else if (item is ProveedorItem pi)
                    IDsSeleccionados.Add(pi.Id);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnConfirmar_Click(object sender, EventArgs e) => btnAceptar_Click(sender, e);

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
