using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using SteeLog.Modelos;
using SteeLog;

namespace SteeLog.Vistas.Movimientos
{
    public partial class ModalRecortes : Form
    {
        private int _idMaterial;
        private string _nombreMaterial;
        private readonly RecortesPresenter _presenter = new RecortesPresenter(LocalServices.Current.Executor);
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private int _versionCarga;
        private bool _guardando;
        public Recorte RecorteSeleccionado { get; private set; }
        public decimal CantidadSeleccionada { get; private set; } = 1;

        public ModalRecortes(int idMaterial, string nombreMaterial)
        {
            InitializeComponent();
            _idMaterial = idMaterial;
            _nombreMaterial = nombreMaterial;
            lblMaterial.Text = $"Material: {_nombreMaterial}";

            Shown += (s, e) => CargarRecortes();
            FormClosed += (s, e) => { _cierre.Cancel(); };
            Disposed += (s, e) => _cierre.Cancel();
            txtAnchoBuscado.TextChanged += TxtFiltro_TextChanged;
            txtAlturaBuscada.TextChanged += TxtFiltro_TextChanged;
            txtAnchoBuscado.KeyPress += TxtDimension_KeyPress;
            txtAlturaBuscada.KeyPress += TxtDimension_KeyPress;
            txtCantidad.KeyPress += TxtDimension_KeyPress;
            txtCantidad.Text = "1";
            dgvRecortesDisponibles.SelectionChanged += DgvRecortesDisponibles_SelectionChanged;
        }

        private async void CargarRecortes(decimal? ancho = null, decimal? altura = null)
        {
            int version = ++_versionCarga;
            try
            {
                var lista = await _presenter.ListarAsync(_idMaterial, ancho, altura, null, _cierre.Token);
                if (_cierre.IsCancellationRequested || version != _versionCarga || IsDisposed) return;
                dgvRecortesDisponibles.DataSource = lista.Select(r => new Recorte { id = r.Id, id_material = r.MaterialId, nombre_material = r.Material, nombre_categoria = r.Categoria, ancho = r.Ancho, altura = r.Altura, medida = r.Medida, cantidad = r.Cantidad, estado = r.Estado, fecha_creacion = r.FechaCreacion }).ToList();
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested && version == _versionCarga) ToastNotification.ShowError("Error al cargar recortes: " + ex.Message); return; }

            if (dgvRecortesDisponibles.Columns["id"] != null) dgvRecortesDisponibles.Columns["id"].Visible = false;
            if (dgvRecortesDisponibles.Columns["id_material"] != null) dgvRecortesDisponibles.Columns["id_material"].Visible = false;
            if (dgvRecortesDisponibles.Columns["nombre_material"] != null) dgvRecortesDisponibles.Columns["nombre_material"].Visible = false;
            if (dgvRecortesDisponibles.Columns["nombre_categoria"] != null) dgvRecortesDisponibles.Columns["nombre_categoria"].Visible = false;
            if (dgvRecortesDisponibles.Columns["fecha_creacion"] != null) dgvRecortesDisponibles.Columns["fecha_creacion"].Visible = false;

            if (dgvRecortesDisponibles.Columns["ancho"] != null)
            {
                dgvRecortesDisponibles.Columns["ancho"].HeaderText = "Ancho (m)";
                dgvRecortesDisponibles.Columns["ancho"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            if (dgvRecortesDisponibles.Columns["altura"] != null)
            {
                dgvRecortesDisponibles.Columns["altura"].HeaderText = "Altura (m)";
                dgvRecortesDisponibles.Columns["altura"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            if (dgvRecortesDisponibles.Columns["cantidad"] != null)
            {
                dgvRecortesDisponibles.Columns["cantidad"].HeaderText = "Stock Disponible";
                dgvRecortesDisponibles.Columns["cantidad"].DefaultCellStyle.Format = "G29";
                dgvRecortesDisponibles.Columns["cantidad"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            if (dgvRecortesDisponibles.Columns["estado"] != null)
            {
                dgvRecortesDisponibles.Columns["estado"].HeaderText = "Estado";
                dgvRecortesDisponibles.Columns["estado"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void TxtFiltro_TextChanged(object sender, EventArgs e)
        {
            FiltrarRecortes();
        }

        private void FiltrarRecortes()
        {
            bool hasAncho = !string.IsNullOrEmpty(txtAnchoBuscado.Text);
            bool hasAltura = !string.IsNullOrEmpty(txtAlturaBuscada.Text);

            if (!hasAncho && !hasAltura)
            {
                CargarRecortes();
                lblInfo.Text = "Mostrando todos los recortes disponibles.";
                return;
            }

            decimal? ancho = null, altura = null;

            string inputAncho = txtAnchoBuscado.Text.Replace(',', '.');
            if (hasAncho && decimal.TryParse(inputAncho, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal anchoVal))
                ancho = anchoVal;

            string inputAltura = txtAlturaBuscada.Text.Replace(',', '.');
            if (hasAltura && decimal.TryParse(inputAltura, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal alturaVal))
                altura = alturaVal;

            CargarRecortes(ancho, altura);
            lblInfo.Text = (ancho.HasValue || altura.HasValue) ? "Filtrando por dimensiones." : "Mostrando todos los recortes.";
        }

        private void TxtDimension_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                e.Handled = true;

            var txt = sender as Guna.UI2.WinForms.Guna2TextBox;
            if (txt != null && (e.KeyChar == '.' || e.KeyChar == ',') && (txt.Text.Contains(".") || txt.Text.Contains(",")))
                e.Handled = true;
        }



        private void DgvRecortesDisponibles_SelectionChanged(object sender, EventArgs e)
        {
            txtCantidad.Text = "1";
        }

        private void btnSeleccionarRecorte_Click(object sender, EventArgs e)
        {
            if (dgvRecortesDisponibles.SelectedRows.Count > 0)
            {
                var recorte = (Recorte)dgvRecortesDisponibles.SelectedRows[0].DataBoundItem;
                decimal stockDisponible = recorte.cantidad;

                string inputCant = txtCantidad.Text.Replace(',', '.');
                if (!decimal.TryParse(inputCant, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cant) || cant <= 0)
                {
                    ToastNotification.ShowWarning("La cantidad debe ser un número mayor a 0.");
                    return;
                }

                if (cant > stockDisponible)
                {
                    ToastNotification.ShowWarning($"La cantidad ({cant.ToString("G29", CultureInfo.InvariantCulture)}) supera el stock disponible ({stockDisponible.ToString("G29", CultureInfo.InvariantCulture)}).");
                    txtCantidad.Text = stockDisponible.ToString("G29", CultureInfo.InvariantCulture);
                    return;
                }

                RecorteSeleccionado = recorte;
                CantidadSeleccionada = cant;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                ToastNotification.ShowInfo("Por favor, seleccione un recorte de la lista.");
            }
        }

        private void btnSumarTodo_Click(object sender, EventArgs e)
        {
            if (dgvRecortesDisponibles.SelectedRows.Count > 0)
            {
                var recorte = (Recorte)dgvRecortesDisponibles.SelectedRows[0].DataBoundItem;
                txtCantidad.Text = recorte.cantidad.ToString("G29", CultureInfo.InvariantCulture);
            }
        }

        private async void btnCrearNuevo_Click(object sender, EventArgs e)
        {
            string inputAncho = txtAnchoBuscado.Text.Replace(',', '.');
            string inputAltura = txtAlturaBuscada.Text.Replace(',', '.');

            bool hasAncho = decimal.TryParse(inputAncho, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal ancho) && ancho > 0;
            bool hasAltura = decimal.TryParse(inputAltura, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal altura) && altura > 0;

            if (!hasAncho && !hasAltura)
            {
                ToastNotification.ShowWarning("Ingrese al menos un ancho o una altura mayor a 0.");
                return;
            }

            decimal medida = 0;
            if (hasAncho && !hasAltura)
            {
                medida = ancho;
                altura = 0;
            }
            else if (!hasAncho && hasAltura)
            {
                medida = altura;
                ancho = 0;
            }
            else
            {
                medida = 0; // Ambos fueron ingresados
            }

            if (_guardando) return;
            _guardando = true;
            try
            {
                string inputCantCrear = txtCantidad.Text.Replace(',', '.');
                decimal.TryParse(inputCantCrear, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadCrear);
                int id = await _presenter.AgregarAsync(_idMaterial, ancho, altura, medida, 0, null, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                var lista = await _presenter.ListarAsync(_idMaterial, ancho, altura, medida, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                var existente = lista.FirstOrDefault(r => r.Id == id);
                RecorteSeleccionado = existente == null ? new Recorte { id = id, id_material = _idMaterial, nombre_material = _nombreMaterial, ancho = ancho, altura = altura, medida = medida, estado = null } : new Recorte { id = existente.Id, id_material = existente.MaterialId, nombre_material = existente.Material, nombre_categoria = existente.Categoria, ancho = existente.Ancho, altura = existente.Altura, medida = existente.Medida, cantidad = existente.Cantidad, estado = existente.Estado, fecha_creacion = existente.FechaCreacion };
                CantidadSeleccionada = cantidadCrear > 0 ? cantidadCrear : 1m;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("Error al gestionar el recorte: " + ex.Message); }
            finally { _guardando = false; }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
