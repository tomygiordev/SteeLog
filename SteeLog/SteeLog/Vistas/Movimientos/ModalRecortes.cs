using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using SteeLog.Presentation;
using SteeLog.Modelos;
using SteeLog;
using System.Drawing;
using SteeLog.UI;

namespace SteeLog.Vistas.Movimientos
{
    public partial class ModalRecortes : Form
    {
        private readonly IServiciosVista servicios;
        private int _idMaterial;
        private string _nombreMaterial;
        private readonly RecortesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private readonly Func<Recorte, decimal, string> _validarCarrito;
        internal Action<string> AdvertirCarrito = mensaje => ToastNotification.ShowWarning(mensaje);
        private int _versionCarga;
        private List<Recorte> _listaOriginal = new List<Recorte>();
        public Recorte RecorteSeleccionado { get; private set; }
        public decimal CantidadSeleccionada { get; private set; } = 1;

        public ModalRecortes(IServiciosVista servicios, int idMaterial, string nombreMaterial, Func<Recorte, decimal, string> validarCarrito = null)
        {
            _validarCarrito = validarCarrito;
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _presenter = new RecortesPresenter(servicios.Executor);

            InitializeComponent();
            AplicarTema();
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

        private void AplicarTema()
        {
            foreach (var lbl in new Label[] { lblFiltro, lblAncho, lblAltura, lblX, lblInfo, label2 })
            {
                lbl.Font = Tema.FuenteTexto;
                lbl.ForeColor = Tema.TextoSecundario;
            }
            lblMaterial.Font = Tema.Fuente(12f, true);
            lblMaterial.ForeColor = Tema.Texto;
            lblFiltro.Font = Tema.FuenteSubtitulo;
            foreach (var t in new Guna.UI2.WinForms.Guna2TextBox[] { txtAnchoBuscado, txtAlturaBuscada, txtCantidad })
            {
                Tema.Input(t);
                t.Height = Tema.AltoControl;
            }
            Tema.Grilla(dgvRecortesDisponibles);
            Tema.Boton(btnCrearNuevo, Variante.Primario);
            btnCrearNuevo.Text = "Crear y usar";
            btnCrearNuevo.Size = new Size(190, Tema.AltoControl);
            Tema.Boton(btnSumarTodo, Variante.Secundario);
            btnSumarTodo.Text = "Cargar stock completo";
            Tema.Boton(btnCancelar, Variante.Secundario);
            btnCancelar.Text = "Cancelar";
            btnCancelar.Size = new Size(270, Tema.AltoBotonPrincipal);
            Tema.Boton(btnSeleccionarRecorte, Variante.Primario);
            btnSeleccionarRecorte.Text = "Usar recorte";
            btnSeleccionarRecorte.Size = new Size(270, Tema.AltoBotonPrincipal);
            btnCancelar.Location = new Point(20, 516);
            btnSeleccionarRecorte.Location = new Point(310, 516);
            pnlCantidad.BackColor = Tema.Superficie;
            CancelButton = btnCancelar;
            Tema.ChromeModal(this, "Selección de recorte");
        }

        private async void CargarRecortes(decimal? ancho = null, decimal? altura = null)
        {
            int version = ++_versionCarga;
            try
            {
                var lista = await _presenter.ListarAsync(_idMaterial, ancho, altura, null, _cierre.Token);
                if (_cierre.IsCancellationRequested || version != _versionCarga || IsDisposed) return;
                var items = lista.Select(r => new Recorte { id = r.Id, id_material = r.MaterialId, nombre_material = r.Material, nombre_categoria = r.Categoria, ancho = r.Ancho, altura = r.Altura, medida = r.Medida, cantidad = r.Cantidad, estado = r.Estado, fecha_creacion = r.FechaCreacion }).ToList();
                if (!ancho.HasValue && !altura.HasValue)
                {
                    _listaOriginal = items;
                }
                dgvRecortesDisponibles.DataSource = items;
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
            if (dgvRecortesDisponibles.Columns["medida"] != null)
            {
                dgvRecortesDisponibles.Columns["medida"].HeaderText = "Medida (m)";
                dgvRecortesDisponibles.Columns["medida"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvRecortesDisponibles.Columns["medida"].FillWeight = 20;
            }
            if (dgvRecortesDisponibles.Columns["cantidad"] != null)
            {
                dgvRecortesDisponibles.Columns["cantidad"].FillWeight = 30;
                dgvRecortesDisponibles.Columns["cantidad"].HeaderText = "Stock disponible";
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

            string inputAncho = txtAnchoBuscado.Text;
            if (hasAncho && NumeroDecimal.TryParse(inputAncho, out decimal anchoVal))
                ancho = anchoVal;

            string inputAltura = txtAlturaBuscada.Text;
            if (hasAltura && NumeroDecimal.TryParse(inputAltura, out decimal alturaVal))
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

                string motivo = _validarCarrito?.Invoke(recorte, cant);
                if (motivo != null)
                {
                    AdvertirCarrito(motivo);
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

        private void btnCrearNuevo_Click(object sender, EventArgs e)
        {
            string inputAncho = txtAnchoBuscado.Text;
            string inputAltura = txtAlturaBuscada.Text;

            bool hasAncho = NumeroDecimal.TryParse(inputAncho, out decimal ancho) && ancho > 0;
            bool hasAltura = NumeroDecimal.TryParse(inputAltura, out decimal altura) && altura > 0;

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

            try
            {
                var validado = new SteeLog.Domain.Inventario.Recorte(0, _idMaterial, ancho, altura, medida, 0m, null);
                var listaParaBuscar = (_listaOriginal != null && _listaOriginal.Count > 0)
                    ? _listaOriginal
                    : (dgvRecortesDisponibles.DataSource as List<Recorte>);

                var existente = listaParaBuscar?.FirstOrDefault(r =>
                    r.ancho == validado.Ancho &&
                    r.altura == validado.Altura &&
                    r.medida == validado.Medida);

                string inputCantCrear = txtCantidad.Text.Replace(',', '.');
                decimal.TryParse(inputCantCrear, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadCrear);
                decimal cantidadElegida = cantidadCrear > 0 ? cantidadCrear : 1m;

                var elegido = existente ?? new Recorte
                {
                    id = 0,
                    id_material = _idMaterial,
                    nombre_material = _nombreMaterial,
                    ancho = validado.Ancho,
                    altura = validado.Altura,
                    medida = validado.Medida,
                    cantidad = 0,
                    estado = null
                };

                string motivo = _validarCarrito?.Invoke(elegido, cantidadElegida);
                if (motivo != null)
                {
                    AdvertirCarrito(motivo);
                    return;
                }

                CantidadSeleccionada = cantidadElegida;
                RecorteSeleccionado = elegido;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (!IsDisposed) ToastNotification.ShowWarning("Error en las dimensiones del recorte: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
