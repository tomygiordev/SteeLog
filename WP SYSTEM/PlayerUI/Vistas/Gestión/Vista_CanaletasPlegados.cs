using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas
{
    public partial class Vista_CanaletasPlegados : Form
    {
        private readonly CanaletaPlegadoDatos _datos = new CanaletaPlegadoDatos();
        private readonly ObraDatos _obraDatos = new ObraDatos();
        private readonly CanaletaPlegadoFotoDatos _fotoDatos = new CanaletaPlegadoFotoDatos();
        private List<CanaletaPlegado> _items = new List<CanaletaPlegado>();
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
            CargarObras();
            CargarFiltroPagado();
            CargarItems();
            LimpiarFotosUI();
            UbicarPanelFotosJuntoACarga();
            AjustarLayoutResponsivo();
            Resize += Vista_CanaletasPlegados_Resize;
        }

        // ===================== CARGAS INICIALES =====================

        private void CargarObras()
        {
            var obras = _obraDatos.GetObras();
            var listaCombo = new List<Obra>();
            listaCombo.Add(new Obra { id = 0, nombre = "— Seleccione —" });
            listaCombo.AddRange(obras);

            cmbObra.DataSource = new List<Obra>(listaCombo);
            cmbObra.DisplayMember = "nombre";
            cmbObra.ValueMember = "id";
            cmbObra.SelectedIndex = 0;

            var listaFiltro = new List<Obra>();
            listaFiltro.Add(new Obra { id = 0, nombre = "— Todas —" });
            listaFiltro.AddRange(obras);

            cmbFiltroObra.DataSource = new List<Obra>(listaFiltro);
            cmbFiltroObra.DisplayMember = "nombre";
            cmbFiltroObra.ValueMember = "id";
            cmbFiltroObra.SelectedIndex = 0;
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

        private void CargarItems()
        {
            try
            {
                _items = _datos.ObtenerCanaletasPlegados();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar canaletas/plegados: " + ex.Message);
            }
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

        private void dgvCanaletas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
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

            CargarFotosExistente(id);
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
        }

        private void cmbFiltroPagado_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        // ===================== CRUD =====================

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!(cmbObra.SelectedItem is Obra obraSeleccionada) || obraSeleccionada.id <= 0)
            {
                ToastNotification.ShowWarning("Seleccione una obra válida.");
                return;
            }

            string descripcion = txtDescripcion.Text.Trim();
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                ToastNotification.ShowWarning("Ingrese una descripción.");
                return;
            }

            string precioTexto = txtPrecio.Text.Replace(',', '.').Trim();
            if (!decimal.TryParse(precioTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precio) || precio < 0)
            {
                ToastNotification.ShowWarning("Ingrese un precio válido (mayor o igual a 0).");
                return;
            }

            string _msg = _idSeleccionado < 0 ? "¿Registrar este trabajo?" : "¿Actualizar este trabajo?";
            if (MessageBox.Show(_msg, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                CambiarEstadoGuardado(true);

                var item = new CanaletaPlegado
                {
                    id = _idSeleccionado,
                    id_obra = obraSeleccionada.id,
                    descripcion = descripcion,
                    precio = precio,
                    pagado = chkPagado.Checked
                };

                if (_idSeleccionado < 0)
                {
                    int nuevoId = _datos.GuardarCanaletaPlegado(item);
                    var fallidas = await SubirFotosPendientesAsync(nuevoId);
                    if (fallidas.Count == 0)
                    {
                        ToastNotification.ShowSuccess("Trabajo registrado correctamente.");
                        LimpiarSeleccion();
                    }
                    else
                    {
                        MarcarComoEdicion(nuevoId);
                        ToastNotification.ShowWarning(
                            "Trabajo registrado. No se pudo subir " + fallidas.Count + " foto(s). Las fotos pendientes quedan en pantalla para reintentar con Actualizar.");
                    }
                }
                else
                {
                    _datos.ActualizarCanaletaPlegado(item);
                    var fallidas = await SubirFotosPendientesAsync(_idSeleccionado);
                    if (fallidas.Count == 0)
                    {
                        ToastNotification.ShowSuccess("Trabajo actualizado correctamente.");
                        LimpiarSeleccion();
                    }
                    else
                    {
                        ToastNotification.ShowWarning(
                            "Trabajo actualizado. No se pudo subir " + fallidas.Count + " foto(s). Las fotos pendientes quedan en pantalla para reintentar.");
                    }
                }

                CargarItems();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
            finally
            {
                CambiarEstadoGuardado(false);
            }
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_idSeleccionado < 0) return;

            if (MessageBox.Show("¿Eliminar este registro?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int idAEliminar = _idSeleccionado;
            try
            {
                CambiarEstadoGuardado(true);
                await EliminarFotosDeCanaletaAsync(idAEliminar);
                await Task.Run(() => _datos.EliminarCanaletaPlegado(idAEliminar));
                ToastNotification.ShowSuccess("Registro eliminado.");
                LimpiarSeleccion();
                CargarItems();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al eliminar: " + ex.Message);
            }
            finally
            {
                CambiarEstadoGuardado(false);
            }
        }

        private void LimpiarSeleccion()
        {
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
            btnGuardar.Enabled = !guardando;
            btnEliminar.Enabled = !guardando && _idSeleccionado >= 0;
            btnAgregarFoto.Enabled = !guardando;
            btnQuitarFoto.Enabled = !guardando && lstFotos.SelectedIndex >= 0;
            Cursor = guardando ? Cursors.WaitCursor : Cursors.Default;
        }

        // ===================== FOTOS =====================

        private void LimpiarFotosUI()
        {
            lstFotos.Items.Clear();
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

        private void CargarFotosExistente(int canaletaPlegadoId)
        {
            lstFotos.Items.Clear();
            picPreview.Image = null;
            picPreview.Tag = null;

            try
            {
                var fotos = _fotoDatos.ObtenerPorCanaleta(canaletaPlegadoId);
                foreach (var f in fotos)
                {
                    var item = new FotoListItem
                    {
                        FotoId = f.id,
                        NombreMostrar = f.nombre_archivo,
                        BytesLocal = null,
                        ObjectPath = f.object_path,
                        ContentType = f.content_type,
                        TamanioBytes = f.tamanio_bytes
                    };
                    lstFotos.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar fotos: " + ex.Message);
            }

            ActualizarContadorFotos();
            btnQuitarFoto.Enabled = false;

            if (lstFotos.Items.Count > 0)
                lstFotos.SelectedIndex = 0;
        }

        private async void btnAgregarFoto_Click(object sender, EventArgs e)
        {
            if (!SupabaseStorageService.TokenDisponible())
            {
                ToastNotification.ShowWarning("No hay sesión activa. Reinicie sesión para gestionar fotos.");
                return;
            }

            using (var ofd = new OpenFileDialog
            {
                Title = "Seleccionar fotos",
                Filter = "Imágenes|*.*",
                Multiselect = true
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                int agregadas = 0;
                int rechazadas = 0;
                foreach (string archivo in ofd.FileNames)
                {
                    if (!File.Exists(archivo))
                    {
                        rechazadas++;
                        continue;
                    }

                    byte[] bytes;
                    try
                    {
                        bytes = File.ReadAllBytes(archivo);
                    }
                    catch (Exception ex)
                    {
                        ToastNotification.ShowError("No se pudo leer '" + Path.GetFileName(archivo) + "': " + ex.Message);
                        rechazadas++;
                        continue;
                    }

                    if (!EsImagenValida(bytes))
                    {
                        ToastNotification.ShowWarning("'" + Path.GetFileName(archivo) + "' no se pudo leer como imagen.");
                        rechazadas++;
                        continue;
                    }

                    if (SupabaseStorageService.SuperaTamanoMaximo(bytes.Length))
                    {
                        ToastNotification.ShowWarning("'" + Path.GetFileName(archivo) + "' supera el tamaño máximo (8 MB).");
                        rechazadas++;
                        continue;
                    }

                    string nombreSanitizado = SupabaseStorageService.SanitizarNombreArchivo(Path.GetFileName(archivo));
                    string contentType = SupabaseStorageService.ObtenerContentType(archivo);

                    var item = new FotoListItem
                    {
                        FotoId = null,
                        NombreMostrar = nombreSanitizado,
                        BytesLocal = bytes,
                        ContentType = contentType,
                        TamanioBytes = bytes.Length,
                        ObjectPath = null
                    };
                    lstFotos.Items.Add(item);
                    agregadas++;
                }

                if (agregadas > 0)
                {
                    ActualizarContadorFotos();
                    if (lstFotos.Items.Count > 0)
                        lstFotos.SelectedIndex = lstFotos.Items.Count - 1;

                    if (_idSeleccionado >= 0)
                        await SubirFotosAgregadasARegistroActualAsync(agregadas);
                }

                if (rechazadas > 0 && agregadas == 0)
                {
                    ToastNotification.ShowWarning("No se agregó ninguna foto (" + rechazadas + " rechazada(s)).");
                }
                else if (rechazadas > 0)
                {
                    ToastNotification.ShowWarning(agregadas + " agregada(s), " + rechazadas + " rechazada(s).");
                }
            }
        }

        private async Task SubirFotosAgregadasARegistroActualAsync(int agregadas)
        {
            try
            {
                CambiarEstadoGuardado(true);
                var fallidas = await SubirFotosPendientesAsync(_idSeleccionado);

                if (fallidas.Count == 0)
                {
                    ToastNotification.ShowSuccess(agregadas == 1
                        ? "Foto guardada correctamente."
                        : agregadas + " fotos guardadas correctamente.");
                    CargarFotosExistente(_idSeleccionado);
                    CargarItems();
                    return;
                }

                RefrescarListaFotos();
                ToastNotification.ShowWarning(
                    "No se pudo subir " + fallidas.Count + " foto(s). Quedan marcadas como pendientes para reintentar con Actualizar.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar fotos: " + ex.Message);
            }
            finally
            {
                CambiarEstadoGuardado(false);
            }
        }

        private bool EsImagenValida(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return false;

            try
            {
                using (var ms = new MemoryStream(bytes))
                using (Image.FromStream(ms))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async void btnQuitarFoto_Click(object sender, EventArgs e)
        {
            if (lstFotos.SelectedIndex < 0)
                return;

            var item = lstFotos.SelectedItem as FotoListItem;
            if (item == null)
                return;

            if (MessageBox.Show("¿Quitar esta foto?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (item.EsPendiente)
            {
                lstFotos.Items.Remove(item);
                picPreview.Image = null;
                picPreview.Tag = null;
                ActualizarContadorFotos();
                btnQuitarFoto.Enabled = lstFotos.SelectedIndex >= 0;
                return;
            }

            try
            {
                CambiarEstadoGuardado(true);

                if (item.FotoId.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(item.ObjectPath))
                    {
                        try
                        {
                            await SupabaseStorageService.EliminarAsync(item.ObjectPath);
                        }
                        catch (Exception exStorage)
                        {
                            ToastNotification.ShowWarning("No se pudo borrar el archivo en Storage. Se quitará el registro igual. " + exStorage.Message);
                        }
                    }
                    await Task.Run(() => _fotoDatos.Eliminar(item.FotoId.Value));
                }

                lstFotos.Items.Remove(item);
                picPreview.Image = null;
                picPreview.Tag = null;
                ActualizarContadorFotos();
                btnQuitarFoto.Enabled = lstFotos.SelectedIndex >= 0;
                ToastNotification.ShowSuccess("Foto eliminada.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al eliminar foto: " + ex.Message);
            }
            finally
            {
                CambiarEstadoGuardado(false);
            }
        }

        private async void lstFotos_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = lstFotos.SelectedItem as FotoListItem;
            btnQuitarFoto.Enabled = item != null;

            if (item == null)
            {
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
                picPreview.Image = null;
                return;
            }

            await MostrarPreviewRemotoAsync(item);
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
                picPreview.Image?.Dispose();
                picPreview.Image = null;
            }
        }

        private async Task MostrarPreviewRemotoAsync(FotoListItem item)
        {
            try
            {
                picPreview.Image = null;
                string objectPath = item?.ObjectPath;
                byte[] bytes = await SupabaseStorageService.DescargarAsync(objectPath);
                if (bytes != null && bytes.Length > 0)
                {
                    using (var ms = new MemoryStream(bytes))
                    using (var imagen = Image.FromStream(ms))
                    {
                        picPreview.Image?.Dispose();
                        picPreview.Image = new Bitmap(imagen);
                    }
                }
            }
            catch (Exception ex)
            {
                if (SupabaseStorageService.EsErrorObjetoNoEncontrado(ex))
                {
                    QuitarFotoHuerfana(item);
                    ToastNotification.ShowWarning("La foto ya no existe en Storage. Se quitó el registro de la lista.");
                    return;
                }

                ToastNotification.ShowWarning("No se pudo previsualizar la foto: " + ex.Message);
            }
        }

        private void QuitarFotoHuerfana(FotoListItem item)
        {
            if (item == null)
                return;

            if (item.FotoId.HasValue)
                _fotoDatos.Eliminar(item.FotoId.Value);

            if (lstFotos.Items.Contains(item))
                lstFotos.Items.Remove(item);

            picPreview.Image?.Dispose();
            picPreview.Image = null;
            picPreview.Tag = null;
            ActualizarContadorFotos();
            btnQuitarFoto.Enabled = lstFotos.SelectedIndex >= 0;
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

        private async Task<List<FotoListItem>> SubirFotosPendientesAsync(int canaletaPlegadoId)
        {
            var pendientes = ObtenerPendientes();
            var fallidas = new List<FotoListItem>();

            if (pendientes.Count == 0)
                return fallidas;

            if (!SupabaseStorageService.TokenDisponible())
            {
                ToastNotification.ShowWarning("No hay sesión activa para subir fotos.");
                return pendientes;
            }

            int ordenBase = _fotoDatos.ObtenerSiguienteOrden(canaletaPlegadoId);
            int orden = ordenBase;

            foreach (var fli in pendientes)
            {
                string objectPath = null;
                bool archivoSubido = false;
                try
                {
                    objectPath = SupabaseStorageService.ConstruirObjectPath(canaletaPlegadoId, fli.NombreMostrar);
                    await SupabaseStorageService.SubirAsync(objectPath, fli.BytesLocal, fli.ContentType);
                    archivoSubido = true;

                    var foto = new CanaletaPlegadoFoto
                    {
                        canaleta_plegado_id = canaletaPlegadoId,
                        bucket_id = SupabaseStorageService.BucketCanaletasFotos,
                        object_path = objectPath,
                        nombre_archivo = fli.NombreMostrar,
                        content_type = fli.ContentType,
                        tamanio_bytes = fli.TamanioBytes,
                        orden = orden
                    };
                    int fotoId = _fotoDatos.Guardar(foto);
                    orden++;

                    fli.FotoId = fotoId;
                    fli.ObjectPath = objectPath;
                    fli.BytesLocal = null;
                    fli.TamanioBytes = foto.tamanio_bytes;
                }
                catch (Exception ex)
                {
                    if (archivoSubido && !string.IsNullOrWhiteSpace(objectPath))
                    {
                        try
                        {
                            await SupabaseStorageService.EliminarAsync(objectPath);
                        }
                        catch
                        {
                        }
                    }

                    fli.ObjectPath = null;
                    fli.FotoId = null;
                    fli.BytesLocal = fli.BytesLocal ?? new byte[0];
                    fallidas.Add(fli);
                    ToastNotification.ShowWarning("No se pudo subir '" + fli.NombreMostrar + "': " + ex.Message);
                }
            }

            return fallidas;
        }

        private void RefrescarListaFotos()
        {
            int indice = lstFotos.SelectedIndex;
            lstFotos.BeginUpdate();
            try
            {
                for (int i = 0; i < lstFotos.Items.Count; i++)
                    lstFotos.Items[i] = lstFotos.Items[i];

                if (indice >= 0 && indice < lstFotos.Items.Count)
                    lstFotos.SelectedIndex = indice;
            }
            finally
            {
                lstFotos.EndUpdate();
            }

            ActualizarContadorFotos();
            btnQuitarFoto.Enabled = lstFotos.SelectedIndex >= 0;
        }

        private async Task EliminarFotosDeCanaletaAsync(int canaletaPlegadoId)
        {
            try
            {
                var fotos = await Task.Run(() => _fotoDatos.ObtenerPorCanaleta(canaletaPlegadoId));
                foreach (var f in fotos)
                {
                    if (!string.IsNullOrWhiteSpace(f.object_path))
                    {
                        try
                        {
                            await SupabaseStorageService.EliminarAsync(f.object_path);
                        }
                        catch
                        {
                        }
                    }
                }
                await Task.Run(() => _fotoDatos.EliminarPorCanaleta(canaletaPlegadoId));
            }
            catch
            {
            }
        }
    }
}
