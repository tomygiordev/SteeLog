using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using System.Windows.Forms;
using SteeLog.Servicios;

namespace SteeLog.Vistas
{
    public partial class Vista_CanaletasPlegados : Form
    {
        private CanaletasPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private int _versionSeleccion, _versionPreview;
        private bool _inicializando = true, _cargandoFotos, _guardando, _errorFotos, _conciliacionIniciada;
        internal Action<string> AdvertirConciliacion = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> AdvertirAdjuntos = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Action<string> InformarOperacion = mensaje => ToastNotification.ShowSuccess(mensaje);
        private List<CanaletaPlegado> _items = new List<CanaletaPlegado>();
        private sealed class Obra { public int id { get; set; } public string nombre { get; set; } }
        private sealed class CanaletaPlegado
        {
            public int id { get; set; } public int id_obra { get; set; }
            public string nombre_obra { get; set; } public string descripcion { get; set; }
            public decimal precio { get; set; } public bool pagado { get; set; } public DateTime fecha_creacion { get; set; }
        }
        private int _idSeleccionado = -1;
        private const int AltoPanelFotosCarga = 126;
        private const int AltoPanelFotosCargaCompacto = 150;
        private const int AnchoMinimoLayoutFotos = 620;
        private const int AnchoMinimoPreviewFotos = 900;

        private class FotoListItem
        {
            public int? FotoId { get; set; }
            public string NombreMostrar { get; set; }
            public byte[] BytesLocal { get; set; }
            public string ObjectPath { get; set; }
            public string ContentType { get; set; }
            public long TamanioBytes { get; set; }
            public bool EsPendiente => !FotoId.HasValue;

            public override string ToString()
            {
                string prefijo = EsPendiente ? "[pendiente] " : "";
                return prefijo + (NombreMostrar ?? "foto");
            }
        }

        private class FiltroPagado
        {
            public string Texto { get; set; }
            public bool? Valor { get; set; }
            public override string ToString() => Texto;
        }

        public Vista_CanaletasPlegados()
        {
            InitializeComponent();
            txtPrecio.KeyPress += TxtPrecio_KeyPress;
            txtFiltroPrecio.KeyPress += TxtPrecio_KeyPress;
            lblListado.Visible = false;
            CargarFiltroPagado();
            LimpiarFotosUI();
            UbicarPanelFotosJuntoACarga();
            AjustarLayoutResponsivo();
            Resize += Vista_CanaletasPlegados_Resize;
        }

        // ===================== CARGAS INICIALES =====================

        internal Vista_CanaletasPlegados(CanaletasPresenter presenter) : this() { _presenter = presenter; }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CambiarEstadoGuardado(false);
            try
            {
                if (_presenter == null) _presenter = new CanaletasPresenter(LocalServices.Current.Executor);
                if (!_conciliacionIniciada) { _conciliacionIniciada = true; _ = ConciliarAlAbrirAsync(); }
                var obras = await _presenter.ObrasAsync(_cierre.Token);
                var items = await _presenter.ListarAsync(null, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                var lista = obras.Select(x => new Obra { id = x.Id, nombre = x.Nombre }).ToList();
                cmbObra.DisplayMember = "nombre"; cmbObra.ValueMember = "id";
                cmbObra.DataSource = new[] { new Obra { id = 0, nombre = "— Seleccione —" } }.Concat(lista).ToList();
                cmbFiltroObra.DisplayMember = "nombre"; cmbFiltroObra.ValueMember = "id";
                cmbFiltroObra.DataSource = new[] { new Obra { id = 0, nombre = "— Todas —" } }.Concat(lista).ToList();
                AsignarItems(items);
                _inicializando = false;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested) ToastNotification.ShowError("Error al cargar canaletas/plegados: " + ex.Message); }
            finally { if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); }
        }
        private async Task ConciliarAlAbrirAsync()
        {
            try { await _presenter.ConciliarAsync(_cierre.Token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested) AdvertirConciliacion("No se pudo completar la limpieza pendiente de fotos: " + ex.Message); }
        }
        protected override void OnFormClosed(FormClosedEventArgs e) { CancelarCierre(); base.OnFormClosed(e); }
        private void CancelarCierre() { if (!_cierre.IsCancellationRequested) _cierre.Cancel(); }
        private void AsignarItems(System.Collections.Generic.IReadOnlyList<SteeLog.Core.Application.Canaletas.CanaletaLectura> items)
        {
            _items = items.Select(x => new CanaletaPlegado { id=x.Id, id_obra=x.ObraId, nombre_obra=x.NombreObra, descripcion=x.Descripcion, precio=x.Precio, pagado=x.Pagado, fecha_creacion=x.FechaCreacion }).ToList();
            AplicarFiltros();
        }
        private async Task CargarItemsAsync()
        {
            var items = await _presenter.ListarAsync(null, _cierre.Token);
            if (!_cierre.IsCancellationRequested && !IsDisposed) AsignarItems(items);
        }

        private Task RefrescarTrasOperacionAsync()
        {
            return CanaletasPresenter.RefrescarTrasOperacionAsync(CargarItemsAsync,
                () => !IsDisposed && !_cierre.IsCancellationRequested,
                error => AdvertirAdjuntos("Operación confirmada, pero no se pudo actualizar la lista: " + error.Message));
        }

        private void CargarFiltroPagado()
        {
            var opciones = new List<FiltroPagado>
            {
                new FiltroPagado { Texto = "— Todas —", Valor = null },
                new FiltroPagado { Texto = "Pagado", Valor = true },
                new FiltroPagado { Texto = "No pagado", Valor = false }
            };
            cmbFiltroPagado.DisplayMember = nameof(FiltroPagado.Texto);
            cmbFiltroPagado.DataSource = opciones;
            cmbFiltroPagado.SelectedIndex = 0;
        }

        // ===================== FILTROS =====================

        private void AplicarFiltros()
        {
            string busqueda = NormalizarTextoBusqueda(txtBusqueda.Text);
            string precioFiltroTexto = txtFiltroPrecio.Text.Replace(',', '.').Trim();
            decimal.TryParse(precioFiltroTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precioFiltro);

            int idObraFiltro = 0;
            if (cmbFiltroObra.SelectedItem is Obra obraFiltro)
                idObraFiltro = obraFiltro.id;

            bool? filtroPagado = null;
            if (cmbFiltroPagado.SelectedItem is FiltroPagado fp && fp.Valor.HasValue)
                filtroPagado = fp.Valor.Value;

            var filtrados = _items.AsEnumerable();

            if (idObraFiltro > 0)
                filtrados = filtrados.Where(i => i.id_obra == idObraFiltro);

            if (precioFiltro > 0)
                filtrados = filtrados.Where(i => i.precio == precioFiltro);

            if (filtroPagado.HasValue)
                filtrados = filtrados.Where(i => i.pagado == filtroPagado.Value);

            if (!string.IsNullOrWhiteSpace(busqueda))
                filtrados = filtrados.Where(i => CoincideBusquedaFlexible(i.descripcion, busqueda)
                                             || CoincideBusquedaFlexible(i.nombre_obra, busqueda));

            var lista = filtrados.ToList();

            dgvCanaletas.DataSource = null;
            dgvCanaletas.DataSource = lista;
            ConfigurarGrid();
        }

        private static bool CoincideBusquedaFlexible(string texto, string busquedaNormalizada)
        {
            if (string.IsNullOrWhiteSpace(busquedaNormalizada))
                return true;

            string textoNormalizado = NormalizarTextoBusqueda(texto);
            return textoNormalizado.Contains(busquedaNormalizada);
        }

        private static string NormalizarTextoBusqueda(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string textoDescompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(textoDescompuesto.Length);

            foreach (char caracter in textoDescompuesto)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter) || caracter == ' ')
                    builder.Append(caracter);
            }

            return builder.ToString();
        }

        private void ConfigurarGrid()
        {
            dgvCanaletas.EnableHeadersVisualStyles = false;
            dgvCanaletas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCanaletas.MultiSelect = false;
            dgvCanaletas.ReadOnly = true;
            dgvCanaletas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvCanaletas.ColumnHeadersHeight = 32;
            dgvCanaletas.RowHeadersVisible = false;
            dgvCanaletas.AllowUserToResizeColumns = false;
            dgvCanaletas.AllowUserToResizeRows = false;
            dgvCanaletas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgvCanaletas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvCanaletas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCanaletas.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvCanaletas.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvCanaletas.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgvCanaletas.BackgroundColor = Color.FromArgb(20, 30, 25);
            dgvCanaletas.DefaultCellStyle.BackColor = Color.FromArgb(28, 40, 32);
            dgvCanaletas.DefaultCellStyle.ForeColor = Color.White;
            dgvCanaletas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 90, 60);
            dgvCanaletas.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvCanaletas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 50, 40);
            dgvCanaletas.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            dgvCanaletas.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 90, 60);

            if (dgvCanaletas.Columns["id"] != null) dgvCanaletas.Columns["id"].Visible = false;
            if (dgvCanaletas.Columns["id_obra"] != null) dgvCanaletas.Columns["id_obra"].Visible = false;
            if (dgvCanaletas.Columns["fecha_creacion"] != null) dgvCanaletas.Columns["fecha_creacion"].Visible = false;

            if (dgvCanaletas.Columns["nombre_obra"] != null)
            {
                dgvCanaletas.Columns["nombre_obra"].HeaderText = "Obra";
                dgvCanaletas.Columns["nombre_obra"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvCanaletas.Columns["nombre_obra"].FillWeight = 22;
            }
            if (dgvCanaletas.Columns["descripcion"] != null)
            {
                dgvCanaletas.Columns["descripcion"].HeaderText = "Descripción";
                dgvCanaletas.Columns["descripcion"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvCanaletas.Columns["descripcion"].FillWeight = 48;
            }
            if (dgvCanaletas.Columns["precio"] != null)
            {
                dgvCanaletas.Columns["precio"].HeaderText = "Precio";
                dgvCanaletas.Columns["precio"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvCanaletas.Columns["precio"].FillWeight = 15;
                dgvCanaletas.Columns["precio"].DefaultCellStyle.Format = "C2";
                dgvCanaletas.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (dgvCanaletas.Columns["pagado"] != null)
            {
                dgvCanaletas.Columns["pagado"].HeaderText = "Pag.";
                dgvCanaletas.Columns["pagado"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvCanaletas.Columns["pagado"].Width = 58;
                dgvCanaletas.Columns["pagado"].MinimumWidth = 58;
                dgvCanaletas.Columns["pagado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void Vista_CanaletasPlegados_Resize(object sender, EventArgs e)
        {
            AjustarLayoutResponsivo();
        }

        private void AjustarLayoutResponsivo()
        {
            bool usarLayoutCompacto = ClientSize.Width < AnchoMinimoLayoutFotos;
            bool mostrarPreview = !usarLayoutCompacto && ClientSize.Width >= AnchoMinimoPreviewFotos;

            panelFotos.SuspendLayout();
            panelFotosBotones.SuspendLayout();

            panelFotos.Dock = DockStyle.Top;
            panelFotos.Height = usarLayoutCompacto ? AltoPanelFotosCargaCompacto : AltoPanelFotosCarga;
            panelFotos.Padding = new Padding(10);

            lblFotosTitulo.Dock = DockStyle.None;
            lblContadorFotos.Dock = DockStyle.None;
            panelFotosBotones.Dock = DockStyle.None;
            lstFotos.Dock = DockStyle.None;
            picPreview.Dock = DockStyle.None;

            btnAgregarFoto.Dock = DockStyle.Left;
            btnAgregarFoto.Width = 145;
            btnQuitarFoto.Dock = DockStyle.Fill;

            int margen = 10;
            int anchoDisponible = Math.Max(0, panelFotos.ClientSize.Width);
            int altoDisponible = Math.Max(0, panelFotos.ClientSize.Height);
            int altoContenido = Math.Max(60, altoDisponible - (margen * 2));

            if (usarLayoutCompacto)
            {
                int anchoContenido = Math.Max(160, anchoDisponible - (margen * 2));
                lblFotosTitulo.SetBounds(margen, margen, anchoContenido, 22);
                lblContadorFotos.SetBounds(margen, margen + 24, anchoContenido, 20);
                panelFotosBotones.SetBounds(margen, margen + 52, anchoContenido, 34);
                lstFotos.SetBounds(margen, margen + 92, anchoContenido, Math.Max(36, altoDisponible - (margen + 92) - margen));
                picPreview.Visible = false;
                panelFotosBotones.ResumeLayout();
                panelFotos.ResumeLayout();
                return;
            }

            int anchoInfo = 118;
            int anchoBotones = 300;
            int anchoPreview = mostrarPreview ? Math.Min(260, Math.Max(190, anchoDisponible / 4)) : 0;

            int xInfo = margen;
            int xCarga = xInfo + anchoInfo + 8;
            int xPreview = mostrarPreview ? anchoDisponible - margen - anchoPreview : anchoDisponible - margen;
            int anchoCarga = Math.Max(220, xPreview - xCarga - 8);

            lblFotosTitulo.SetBounds(xInfo, margen, anchoInfo, 24);
            lblContadorFotos.SetBounds(xInfo, margen + 30, anchoInfo, 24);
            panelFotosBotones.SetBounds(xCarga, margen, Math.Min(anchoBotones, anchoCarga), 36);
            lstFotos.SetBounds(xCarga, margen + 44, anchoCarga, Math.Max(42, altoContenido - 44));

            picPreview.Visible = mostrarPreview;
            if (mostrarPreview)
                picPreview.SetBounds(xPreview, margen, anchoPreview, altoContenido);

            panelFotosBotones.ResumeLayout();
            panelFotos.ResumeLayout();
        }

        private void UbicarPanelFotosJuntoACarga()
        {
            Controls.SetChildIndex(guna2Panel1, 0);
            Controls.SetChildIndex(panelAntiParpadeo3, 1);
            Controls.SetChildIndex(panelFotos, 2);
            Controls.SetChildIndex(panelAntiParpadeo2, 3);
            Controls.SetChildIndex(panelAntiParpadeo1, 4);
        }

        // ===================== EVENTOS =====================

        private void TxtPrecio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',')
                e.Handled = true;
        }

        private async void dgvCanaletas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_inicializando || _guardando || e.RowIndex < 0) return;
            var fila = dgvCanaletas.Rows[e.RowIndex];
            if (fila.Cells["id"].Value == null) return;

            int id = Convert.ToInt32(fila.Cells["id"].Value);
            if (id == _idSeleccionado)
            {
                LimpiarSeleccion();
                return;
            }

            _idSeleccionado = id;
            txtDescripcion.Text = fila.Cells["descripcion"].Value?.ToString() ?? "";
            txtPrecio.Text = fila.Cells["precio"].Value != null
                ? Convert.ToDecimal(fila.Cells["precio"].Value).ToString("0.00", CultureInfo.InvariantCulture)
                : "";
            chkPagado.Checked = fila.Cells["pagado"].Value != null && Convert.ToBoolean(fila.Cells["pagado"].Value);

            int idObra = Convert.ToInt32(fila.Cells["id_obra"].Value);
            for (int i = 0; i < cmbObra.Items.Count; i++)
            {
                if (cmbObra.Items[i] is Obra obra && obra.id == idObra)
                {
                    cmbObra.SelectedIndex = i;
                    break;
                }
            }

            btnGuardar.Text = "Actualizar";
            btnGuardar.FillColor = Color.FromArgb(33, 150, 243);
            btnEliminar.Enabled = true;

            await CargarFotosExistenteAsync(id);
        }

        private void cmbFiltroObra_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void txtBusqueda_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void txtFiltroPrecio_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBusqueda.Clear();
            txtFiltroPrecio.Clear();
            if (cmbFiltroObra.Items.Count > 0)
                cmbFiltroObra.SelectedIndex = 0;
            if (cmbFiltroPagado.Items.Count > 0)
                cmbFiltroPagado.SelectedIndex = 0;
            AplicarFiltros();
            if (_errorFotos && !_guardando && !_inicializando && !_cargandoFotos)
            {
                LimpiarSeleccion();
            }
        }

        private void cmbFiltroPagado_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        // ===================== CRUD =====================

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if (_inicializando || _cargandoFotos || _guardando || _errorFotos) return;
            if (!(cmbObra.SelectedItem is Obra obra) || obra.id <= 0) { ToastNotification.ShowWarning("Seleccione una obra válida."); return; }
            string descripcion = txtDescripcion.Text.Trim();
            if (string.IsNullOrWhiteSpace(descripcion)) { ToastNotification.ShowWarning("Ingrese una descripción."); return; }
            if (!decimal.TryParse(txtPrecio.Text.Replace(',', '.').Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precio) || precio < 0) { ToastNotification.ShowWarning("Ingrese un precio válido (mayor o igual a 0)."); return; }
            if (MessageBox.Show(_idSeleccionado < 0 ? "¿Registrar este trabajo?" : "¿Actualizar este trabajo?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            await GuardarCoreAsync(obra.id, descripcion, precio, chkPagado.Checked);
        }

        private async Task GuardarCoreAsync(int obraId, string descripcion, decimal precio, bool pagado)
        {
            if (_inicializando || _cargandoFotos || _guardando || _errorFotos) return;
            int version = _versionSeleccion; int idAnterior = _idSeleccionado;
            var pendientes = ObtenerPendientes();
            _guardando = true; CambiarEstadoGuardado(true);
            int id = -1;
            try
            {
                id = await _presenter.GuardarAsync(idAnterior < 0 ? (int?)null : idAnterior, obraId, descripcion, precio, pagado, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                if (!_cierre.IsCancellationRequested && !IsDisposed && version == _versionSeleccion && idAnterior < 0) MarcarComoEdicion(id);
                int fallidas = 0; bool limpiezaPendiente = false;
                foreach (var foto in pendientes)
                {
                    try { foto.FotoId = await _presenter.AdjuntarAsync(id, foto.NombreMostrar, foto.BytesLocal, _cierre.Token); }
                    catch (OperationCanceledException) { throw; }
                    catch (SteeLog.Core.Application.Canaletas.CanaletaResultadoInciertoException) { throw; }
                    catch (SteeLog.Core.Application.Canaletas.CanaletaLimpiezaPendienteException ex) { if (!ex.ResultadoId.HasValue) throw; foto.FotoId = ex.ResultadoId; limpiezaPendiente = true; }
                    catch { fallidas++; }
                }
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                if (fallidas == 0)
                {
                    if (!limpiezaPendiente) InformarOperacion("Trabajo guardado correctamente.");
                    LimpiarSeleccion();
                }
                else
                {
                    MarcarComoEdicion(id);
                    await RefrescarFotosConPendientesAsync(id, version);
                    if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                    AdvertirAdjuntos("Trabajo guardado. " + fallidas + " foto(s) pendientes para reintentar.");
                }
                if (limpiezaPendiente) AdvertirAdjuntos("Fotos guardadas; queda limpieza de archivos pendiente.");
                await RefrescarTrasOperacionAsync();
            }
            catch (OperationCanceledException) { }
            catch (SteeLog.Core.Application.Canaletas.CanaletaResultadoInciertoException ex)
            {
                if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion)
                {
                    if (idAnterior < 0 && id > 0)
                    {
                        try { await CargarItemsAsync(); } catch { }
                    }
                    _errorFotos = true;
                    AdvertirAdjuntos("Resultado incierto; verifique antes de reintentar. " + ex.Message);
                }
            }
            catch (SteeLog.Core.Application.Canaletas.CanaletaLimpiezaPendienteException ex)
            {
                if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion)
                {
                    if (idAnterior < 0 && id > 0)
                    {
                        try { await CargarItemsAsync(); } catch { }
                    }
                    _errorFotos = true;
                    AdvertirAdjuntos("Operación confirmada; queda limpieza pendiente. " + ex.Message);
                }
            }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) ToastNotification.ShowError("Error al guardar: " + ex.Message); }
            finally { _guardando = false; if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); }
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_inicializando || _cargandoFotos || _guardando || _errorFotos || _idSeleccionado < 0) return;
            if (MessageBox.Show("¿Eliminar este registro?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await EliminarCoreAsync();
        }

        private async Task EliminarCoreAsync()
        {
            if (_inicializando || _cargandoFotos || _guardando || _errorFotos || _idSeleccionado < 0) return;
            int id = _idSeleccionado, version = _versionSeleccion;
            _guardando = true; CambiarEstadoGuardado(true);
            try
            {
                await _presenter.EliminarAsync(id, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                InformarOperacion("Registro eliminado."); LimpiarSeleccion(); await RefrescarTrasOperacionAsync();
            }
            catch (OperationCanceledException) { }
            catch (SteeLog.Core.Application.Canaletas.CanaletaResultadoInciertoException ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { _errorFotos = true; ToastNotification.ShowWarning("Resultado incierto; verifique antes de reintentar. " + ex.Message); } }
            catch (SteeLog.Core.Application.Canaletas.CanaletaLimpiezaPendienteException ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { LimpiarSeleccion(); ToastNotification.ShowWarning("Eliminación confirmada; queda limpieza pendiente. " + ex.Message); try { await CargarItemsAsync(); } catch (OperationCanceledException) { } catch (Exception error) { if (!IsDisposed && !_cierre.IsCancellationRequested) ToastNotification.ShowError("No se pudo actualizar la lista: " + error.Message); } } }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) ToastNotification.ShowError("Error al eliminar: " + ex.Message); }
            finally { _guardando = false; if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); }
        }

        private void LimpiarSeleccion()
        {
            ++_versionSeleccion; ++_versionPreview; _errorFotos = false; _cargandoFotos = false;
            _idSeleccionado = -1;
            txtDescripcion.Clear();
            txtPrecio.Clear();
            chkPagado.Checked = false;
            if (cmbObra.Items.Count > 0)
                cmbObra.SelectedIndex = 0;
            btnGuardar.Text = "Guardar";
            btnGuardar.FillColor = Color.FromArgb(39, 174, 96);
            btnEliminar.Enabled = false;
            dgvCanaletas.ClearSelection();
            LimpiarFotosUI();
            CambiarEstadoGuardado(false);
        }

        private void MarcarComoEdicion(int id)
        {
            _idSeleccionado = id;
            btnGuardar.Text = "Actualizar";
            btnGuardar.FillColor = Color.FromArgb(33, 150, 243);
            btnEliminar.Enabled = true;
        }

        private void CambiarEstadoGuardado(bool guardando)
        {
            bool enCurso = guardando || _guardando || _inicializando || _cargandoFotos;
            bool bloqueado = enCurso || _errorFotos;
            btnGuardar.Enabled = !bloqueado;
            btnEliminar.Enabled = !bloqueado && _idSeleccionado >= 0;
            btnAgregarFoto.Enabled = !bloqueado;
            dgvCanaletas.Enabled = !_guardando && !_inicializando;
            btnQuitarFoto.Enabled = !bloqueado && lstFotos.SelectedIndex >= 0;
            Cursor = enCurso ? Cursors.WaitCursor : Cursors.Default;
        }

        // ===================== FOTOS =====================

        private void LimpiarFotosUI()
        {
            lstFotos.Items.Clear();
            picPreview.Image?.Dispose();
            picPreview.Image = null;
            picPreview.Tag = null;
            ActualizarContadorFotos();
            btnQuitarFoto.Enabled = false;
        }

        private void ActualizarContadorFotos()
        {
            int cantidad = lstFotos.Items.Count;
            lblContadorFotos.Text = cantidad == 1 ? "1 foto" : cantidad + " fotos";
        }

        private async Task CargarFotosExistenteAsync(int id)
        {
            int version = ++_versionSeleccion; ++_versionPreview;
            _cargandoFotos = true; _errorFotos = false; LimpiarFotosUI(); CambiarEstadoGuardado(false);
            try
            {
                var fotos = await _presenter.FotosAsync(id, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                foreach (var f in fotos) lstFotos.Items.Add(new FotoListItem { FotoId=f.Id, NombreMostrar=f.NombreArchivo, ObjectPath=f.ObjectPath, ContentType=f.ContentType, TamanioBytes=f.TamanioBytes });
                ActualizarContadorFotos(); if (lstFotos.Items.Count > 0) lstFotos.SelectedIndex = 0;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { _errorFotos = true; ToastNotification.ShowError("Error al cargar fotos: " + ex.Message); } }
            finally { if (version == _versionSeleccion) { _cargandoFotos = false; if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); } }
        }

        private async void btnAgregarFoto_Click(object sender, EventArgs e)
        {
            if (_inicializando || _cargandoFotos || _guardando || _errorFotos) return;
            using (var dialogo = new OpenFileDialog { Title = "Seleccionar fotos", Filter = SteeLog.Core.Application.Canaletas.OperacionesCanaleta.FiltroDialogoFotos, Multiselect = true })
            {
                if (dialogo.ShowDialog() != DialogResult.OK) return;
                int version = _versionSeleccion, id = _idSeleccionado, agregadas = 0, rechazadas = 0;
                _guardando = true; CambiarEstadoGuardado(true);
                try
                {
                    foreach (string archivo in dialogo.FileNames)
                    {
                        try
                        {
                            var bytes = await LeerArchivoFotoAsync(archivo, _cierre.Token);
                            var tipo = SteeLog.Core.Application.Canaletas.OperacionesCanaleta.ValidarTipoFoto(bytes);
                            if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                            lstFotos.Items.Add(new FotoListItem { NombreMostrar=Path.GetFileName(archivo), BytesLocal=bytes, ContentType=tipo, TamanioBytes=bytes.Length });
                            agregadas++;
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return; rechazadas++; ToastNotification.ShowWarning("No se pudo agregar '" + Path.GetFileName(archivo) + "': " + ex.Message); }
                    }
                    if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                    ActualizarContadorFotos(); if (agregadas > 0) lstFotos.SelectedIndex = lstFotos.Items.Count - 1;
                    if (rechazadas > 0) ToastNotification.ShowWarning(agregadas + " agregada(s), " + rechazadas + " rechazada(s).");
                    if (agregadas > 0 && id >= 0) await AdjuntarPendientesAsync(id, version);
                }
                catch (OperationCanceledException) { }
                finally { _guardando = false; if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); }
            }
        }

        private static async Task<byte[]> LeerArchivoFotoAsync(string ruta, CancellationToken token)
        {
            const int maximo = 8 * 1024 * 1024;
            using (var archivo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            {
                if (archivo.Length > maximo) throw new InvalidDataException("La foto supera el tamaño máximo (8 MB).");
                using (var salida = new MemoryStream())
                {
                    var bloque = new byte[81920]; int leidos;
                    while ((leidos = await archivo.ReadAsync(bloque, 0, bloque.Length, token)) != 0)
                    {
                        if (salida.Length + leidos > maximo) throw new InvalidDataException("La foto supera el tamaño máximo (8 MB).");
                        salida.Write(bloque, 0, leidos);
                    }
                    return salida.ToArray();
                }
            }
        }

        private async Task RefrescarFotosConPendientesAsync(int id, int version)
        {
            var pendientes = ObtenerPendientes();
            var fotos = await _presenter.FotosAsync(id, _cierre.Token);
            if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
            ++_versionPreview; LimpiarFotosUI();
            foreach (var f in fotos) lstFotos.Items.Add(new FotoListItem { FotoId=f.Id, NombreMostrar=f.NombreArchivo, ObjectPath=f.ObjectPath, ContentType=f.ContentType, TamanioBytes=f.TamanioBytes });
            foreach (var pendiente in pendientes) lstFotos.Items.Add(pendiente);
            ActualizarContadorFotos(); if (lstFotos.Items.Count > 0) lstFotos.SelectedIndex = 0;
        }

        private async Task AdjuntarPendientesAsync(int id, int version)
        {
            var pendientes = ObtenerPendientes();
            _guardando = true; CambiarEstadoGuardado(true);
            try
            {
                int fallidas = 0; bool limpiezaPendiente = false;
                foreach (var foto in pendientes)
                {
                    try { foto.FotoId = await _presenter.AdjuntarAsync(id, foto.NombreMostrar, foto.BytesLocal, _cierre.Token); }
                    catch (OperationCanceledException) { throw; }
                    catch (SteeLog.Core.Application.Canaletas.CanaletaResultadoInciertoException) { throw; }
                    catch (SteeLog.Core.Application.Canaletas.CanaletaLimpiezaPendienteException ex) { if (!ex.ResultadoId.HasValue) throw; foto.FotoId = ex.ResultadoId; limpiezaPendiente = true; }
                    catch { fallidas++; }
                }
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                if (fallidas > 0)
                {
                    await RefrescarFotosConPendientesAsync(id, version);
                    if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                    AdvertirAdjuntos(fallidas + " foto(s) pendientes para reintentar.");
                }
                else
                {
                    if (!limpiezaPendiente) ToastNotification.ShowSuccess("Fotos guardadas correctamente.");
                    await CargarFotosExistenteAsync(id);
                }
                if (limpiezaPendiente && !_cierre.IsCancellationRequested && !IsDisposed) AdvertirAdjuntos("Fotos guardadas; queda limpieza de archivos pendiente.");
            }
            catch (OperationCanceledException) { }
            catch (SteeLog.Core.Application.Canaletas.CanaletaResultadoInciertoException ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { _errorFotos = true; ToastNotification.ShowWarning("Resultado incierto; verifique antes de reintentar. " + ex.Message); } }
            catch (SteeLog.Core.Application.Canaletas.CanaletaLimpiezaPendienteException ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { _errorFotos = true; ToastNotification.ShowWarning("Operación confirmada; queda limpieza pendiente. " + ex.Message); } }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested) ToastNotification.ShowError("Error al guardar fotos: " + ex.Message); }
            finally { _guardando = false; if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); }
        }

        private async void btnQuitarFoto_Click(object sender, EventArgs e)
        {
            if (_inicializando || _cargandoFotos || _guardando || _errorFotos || lstFotos.SelectedIndex < 0)
                return;

            var item = lstFotos.SelectedItem as FotoListItem;
            if (item == null)
                return;

            if (MessageBox.Show("¿Quitar esta foto?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (item.EsPendiente)
            {
                lstFotos.Items.Remove(item);
                picPreview.Image?.Dispose();
                picPreview.Image = null;
                picPreview.Tag = null;
                ActualizarContadorFotos();
                btnQuitarFoto.Enabled = lstFotos.SelectedIndex >= 0;
                return;
            }

            int version = _versionSeleccion;
            _guardando = true; CambiarEstadoGuardado(true);
            try
            {
                await _presenter.EliminarFotoAsync(item.FotoId.Value, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionSeleccion) return;
                lstFotos.Items.Remove(item); ActualizarContadorFotos(); ToastNotification.ShowSuccess("Foto eliminada.");
            }
            catch (OperationCanceledException) { }
            catch (SteeLog.Core.Application.Canaletas.CanaletaResultadoInciertoException ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { _errorFotos = true; ToastNotification.ShowWarning("Resultado incierto; verifique antes de reintentar. " + ex.Message); } }
            catch (SteeLog.Core.Application.Canaletas.CanaletaLimpiezaPendienteException ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) { lstFotos.Items.Remove(item); ActualizarContadorFotos(); ToastNotification.ShowWarning("Eliminación confirmada; queda limpieza pendiente. " + ex.Message); } }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionSeleccion) ToastNotification.ShowError("Error al eliminar foto: " + ex.Message); }
            finally { _guardando = false; if (!IsDisposed && !_cierre.IsCancellationRequested) CambiarEstadoGuardado(false); }
        }

        private async void lstFotos_SelectedIndexChanged(object sender, EventArgs e)
        {
            int version = ++_versionPreview;
            var item = lstFotos.SelectedItem as FotoListItem;
            btnQuitarFoto.Enabled = item != null && !_guardando && !_cargandoFotos && !_inicializando;

            if (item == null)
            {
                picPreview.Image?.Dispose();
                picPreview.Image = null;
                picPreview.Tag = null;
                return;
            }

            if (item.BytesLocal != null)
            {
                MostrarPreviewDesdeBytes(item.BytesLocal);
                return;
            }

            if (string.IsNullOrWhiteSpace(item.ObjectPath))
            {
                picPreview.Image?.Dispose();
                picPreview.Image = null;
                return;
            }

            await MostrarPreviewAsync(item, version);
        }

        private void LimpiarImagenPreview()
        {
            picPreview.Image?.Dispose();
            picPreview.Image = null;
            picPreview.Tag = null;
        }

        private void MostrarPreviewDesdeBytes(byte[] bytes)
        {
            try
            {
                using (var ms = new MemoryStream(bytes))
                using (var imagen = Image.FromStream(ms))
                {
                    picPreview.Image?.Dispose();
                    picPreview.Image = new Bitmap(imagen);
                }
            }
            catch
            {
                LimpiarImagenPreview();
                AdvertirAdjuntos("No se puede previsualizar esta imagen en este equipo; la foto se conserva.");
            }
        }

        private async Task MostrarPreviewAsync(FotoListItem item, int version)
        {
            try
            {
                byte[] bytes = await _presenter.LeerFotoAsync(item.ObjectPath, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed || version != _versionPreview || lstFotos.SelectedItem != item) return;
                if (bytes == null || bytes.Length == 0)
                {
                    LimpiarImagenPreview();
                    AdvertirAdjuntos("El archivo de la foto no se encuentra en el almacenamiento local.");
                    return;
                }
                MostrarPreviewDesdeBytes(bytes);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !_cierre.IsCancellationRequested && version == _versionPreview) AdvertirAdjuntos("No se pudo previsualizar la foto: " + ex.Message); }
        }

        private List<FotoListItem> ObtenerPendientes()
        {
            var pendientes = new List<FotoListItem>();
            foreach (var it in lstFotos.Items)
            {
                var fli = it as FotoListItem;
                if (fli != null && fli.EsPendiente)
                    pendientes.Add(fli);
            }
            return pendientes;
        }


    }
}
