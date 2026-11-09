using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;
using SteeLog.UI;

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
            AplicarEstilo();
            _idsIniciales = idsIniciales != null ? new HashSet<int>(idsIniciales) : new HashSet<int>();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _presenter = presenter ?? new MaterialesPresenter(LocalServices.Current.Executor);
            FormClosed += (_, __) => _cierre.Cancel();
            Disposed += (_, __) => _cierre.Dispose();
            Shown += ModalSeleccionProveedores_Shown;
        }

        /// <summary>Chrome de modal propio (adiós a la barra nativa con mojibake) y controles del tema.</summary>
        private void AplicarEstilo()
        {
            Tema.SinEscala(this);
            const int margen = 24;
            label1.Text = "Elegí los proveedores del material";
            label1.Font = Tema.Fuente(10.5f); label1.ForeColor = Tema.TextoSecundario;
            label1.Location = new Point(margen, 16);
            clbProveedores.Location = new Point(margen, 46);
            clbProveedores.Size = new Size(ClientSize.Width - 2 * margen, 250);
            GestionUi.ListaChecks(clbProveedores);
            btnAceptar.Size = new Size(140, Tema.AltoBotonPrincipal);
            btnCancelar.Size = new Size(140, Tema.AltoBotonPrincipal);
            btnAceptar.Location = new Point(ClientSize.Width - margen - 140, 312);
            btnCancelar.Location = new Point(btnAceptar.Left - 8 - 140, 312);
            Tema.Boton(btnAceptar, Variante.Primario);
            Tema.Boton(btnCancelar, Variante.Secundario);
            btnAceptar.Text = "Aceptar"; btnCancelar.Text = "Cancelar";
            ClientSize = new Size(ClientSize.Width, 312 + Tema.AltoBotonPrincipal + 20);
            AcceptButton = btnAceptar; CancelButton = btnCancelar;
            Tema.ChromeModal(this, "Selección de proveedores");
            Padding = new Padding(1);
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
                    Dialogo.Error("Error al cargar la lista de proveedores: " + ex.Message, "Error", this);
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
