using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Presentation;
using SteeLog.UI;

namespace SteeLog.Vistas
{
    /// <summary>
    /// Bitácora de actividad (solo Admin): quién hizo qué y cuándo. Filtros, grilla con colores suaves por tipo
    /// y panel de detalle con el antes/después de cada cambio de datos. Interfaz armada por código.
    /// </summary>
    public class Vista_Bitacora : Form, IRefrescable
    {
        private const string Todos = "Todos";

        private readonly IServiciosVista servicios;
        private readonly BitacoraPresenter presenter;
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private CancellationTokenSource busqueda;
        private int version;
        private List<BitacoraRegistro> registros = new List<BitacoraRegistro>();

        private PanelAntiParpadeo banda, filtros, detalle;
        private Label titulo, estado, lblDescripcion;
        private Guna2DateTimePicker dtpDesde, dtpHasta;
        private Guna2ComboBox cmbUsuario, cmbEntidad, cmbAccion;
        private Guna2TextBox txtTexto;
        private Guna2Button btnBuscar, btnExportar;
        private Guna2DataGridView grilla, grillaDetalle;
        private TextBox txtDescripcion;

        public Vista_Bitacora(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            presenter = new BitacoraPresenter(servicios.Executor);
            Text = "Bitácora";
            Name = "Vista_Bitacora";
            Tema.SinEscala(this);
            BackColor = Tema.Fondo;
            ConstruirUi();
            GestionUi.Aplicar(this);
            Tema.Boton(btnBuscar, Variante.Primario);
            Tema.Boton(btnExportar, Variante.Secundario);
            Load += (s, e) => { CargarOpcionesAsync(); BuscarAsync(); };
            Disposed += (s, e) => CancelarOperaciones();
        }

        /// <summary>Se llama cuando la vista vuelve del caché de VistaGeneral.</summary>
        public void Refrescar()
        {
            CargarOpcionesAsync();
            BuscarAsync();
        }

        private void CancelarOperaciones()
        {
            try
            {
                if (!vida.IsCancellationRequested) vida.Cancel();
                if (busqueda != null) busqueda.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        // ------------------------------------------------------------------ UI
        private void ConstruirUi()
        {
            // Banda de título
            banda = new PanelAntiParpadeo { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 1 };
            banda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            banda.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titulo = new Label { Text = "Bitácora", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            banda.Controls.Add(titulo, 0, 0);
            GestionUi.Banda(banda, titulo);

            // Filtros: fila de etiquetas + fila de controles
            filtros = new PanelAntiParpadeo { Dock = DockStyle.Top, ColumnCount = 8, RowCount = 2 };
            float[] anchos = { 150, 150, 0, 0, 0, 0, 120, 150 };
            float[] pesos = { 0, 0, 20, 20, 18, 30, 0, 0 };
            for (int i = 0; i < 8; i++)
                filtros.ColumnStyles.Add(anchos[i] > 0 ? new ColumnStyle(SizeType.Absolute, anchos[i]) : new ColumnStyle(SizeType.Percent, pesos[i]));
            filtros.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            filtros.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.AltoControl));
            filtros.BackColor = Tema.Fondo;
            filtros.Padding = new Padding(GestionUi.Pad, 12, GestionUi.Pad, 10);
            filtros.Height = 12 + 22 + Tema.AltoControl + 10;

            dtpDesde = new Guna2DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today.AddDays(-7) };
            dtpHasta = new Guna2DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today };
            Tema.Fecha(dtpDesde);
            Tema.Fecha(dtpHasta);
            cmbUsuario = NuevoCombo();
            cmbEntidad = NuevoCombo();
            cmbAccion = NuevoCombo();
            txtTexto = new Guna2TextBox { PlaceholderText = "Buscar en descripción, ID o datos..." };
            btnBuscar = new Guna2Button { Text = "Buscar" };
            btnExportar = new Guna2Button { Text = "Exportar Excel" };

            string[] etiquetas = { "Desde", "Hasta", "Usuario", "Entidad", "Acción", "Texto" };
            Control[] campos = { dtpDesde, dtpHasta, cmbUsuario, cmbEntidad, cmbAccion, txtTexto };
            for (int i = 0; i < etiquetas.Length; i++)
            {
                var l = new Label
                {
                    Text = etiquetas[i], AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft,
                    Font = Tema.Fuente(9.5f), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent,
                    Margin = new Padding(0, 0, 8, 2)
                };
                filtros.Controls.Add(l, i, 0);
                campos[i].Dock = DockStyle.Fill;
                campos[i].Margin = new Padding(0, 0, 8, 0);
                filtros.Controls.Add(campos[i], i, 1);
            }
            btnBuscar.Dock = DockStyle.Fill; btnBuscar.Margin = new Padding(0, 0, 8, 0);
            btnExportar.Dock = DockStyle.Fill; btnExportar.Margin = new Padding(0);
            filtros.Controls.Add(btnBuscar, 6, 1);
            filtros.Controls.Add(btnExportar, 7, 1);

            btnBuscar.Click += (s, e) => BuscarAsync();
            btnExportar.Click += (s, e) => Exportar();
            txtTexto.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarAsync(); }
            };

            // Estado (conteo / avisos)
            estado = new Label
            {
                Name = "lblEstadoBitacora", Dock = DockStyle.Top, Height = 30, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(GestionUi.Pad, 0, GestionUi.Pad, 0), Font = Tema.Fuente(10f),
                ForeColor = Tema.TextoSecundario, BackColor = Tema.Fondo
            };

            // Detalle (abajo): Campo | Antes | Después a la izquierda, datos y descripción completa a la derecha
            detalle = new PanelAntiParpadeo { Dock = DockStyle.Bottom, Height = 270, ColumnCount = 2, RowCount = 1, BackColor = Tema.Superficie };
            detalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            detalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            detalle.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detalle.Padding = new Padding(0, 1, 0, 0);

            grillaDetalle = NuevaGrilla();
            grillaDetalle.Name = "dgvDetalleBitacora";
            grillaDetalle.Dock = DockStyle.Fill;
            grillaDetalle.Margin = Padding.Empty;
            grillaDetalle.Columns.Add("campo", "Campo");
            grillaDetalle.Columns.Add("antes", "Antes");
            grillaDetalle.Columns.Add("despues", "Después");
            grillaDetalle.Columns["campo"].Width = 170;
            grillaDetalle.Columns["antes"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grillaDetalle.Columns["despues"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            detalle.Controls.Add(grillaDetalle, 0, 0);

            var derecha = new PanelAntiParpadeo { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Tema.Fondo, Margin = Padding.Empty };
            derecha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.AltoHeader));
            derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblDescripcion = new Label
            {
                Text = "Descripción completa", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Font = Tema.FuenteHeaderGrilla, ForeColor = Color.White, BackColor = Tema.Banda, Padding = new Padding(12, 0, 8, 0)
            };
            txtDescripcion = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None, BackColor = Tema.Fondo, ForeColor = Tema.Texto, Font = Tema.Fuente(10f),
                Margin = Padding.Empty
            };
            derecha.Controls.Add(lblDescripcion, 0, 0);
            derecha.Controls.Add(txtDescripcion, 0, 1);
            detalle.Controls.Add(derecha, 1, 0);

            // Grilla principal
            grilla = NuevaGrilla();
            grilla.Name = "dgvBitacora";
            grilla.Dock = DockStyle.Fill;
            AgregarColumna("fecha", "Fecha", 150);
            AgregarColumna("usuario", "Usuario", 170);
            AgregarColumna("accion", "Acción", 160);
            AgregarColumna("entidad", "Entidad", 150);
            AgregarColumna("descripcion", "Resumen", 300);
            grilla.Columns["descripcion"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grilla.Columns["descripcion"].MinimumWidth = 300;
            grilla.SelectionChanged += (s, e) => MostrarDetalle();

            // Orden: el control Fill primero, luego los acoplados (el último agregado queda arriba del todo).
            Controls.Add(grilla);
            Controls.Add(detalle);
            Controls.Add(estado);
            Controls.Add(filtros);
            Controls.Add(banda);
        }

        private static Guna2ComboBox NuevoCombo()
        {
            var c = new Guna2ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            UiVista.AsignarDatos(c, new List<string> { Todos }, string.Empty, string.Empty);
            return c;
        }

        private static Guna2DataGridView NuevaGrilla()
        {
            var g = new Guna2DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Tema.Fondo
            };
            Tema.Grilla(g);
            return g;
        }

        private void AgregarColumna(string nombre, string encabezado, int ancho)
        {
            int i = grilla.Columns.Add(nombre, encabezado);
            grilla.Columns[i].Width = ancho;
            grilla.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        // ------------------------------------------------------------ Carga
        private async void CargarOpcionesAsync()
        {
            try
            {
                var opciones = await presenter.OpcionesAsync(vida.Token);
                if (IsDisposed || vida.IsCancellationRequested || opciones == null || !opciones.Instalada) return;
                LlenarCombo(cmbUsuario, opciones.Usuarios);
                LlenarCombo(cmbEntidad, opciones.Entidades);
                LlenarCombo(cmbAccion, opciones.Acciones);
            }
            catch (Exception)
            {
                // Los combos son accesorios: si fallan, la búsqueda ya informa el error.
            }
        }

        private static void LlenarCombo(Guna2ComboBox combo, IEnumerable<string> valores)
        {
            string actual = combo.SelectedItem as string;
            var lista = new List<string> { Todos };
            if (valores != null) lista.AddRange(valores.Where(v => !string.IsNullOrWhiteSpace(v)));
            UiVista.AsignarDatos(combo, lista, string.Empty, string.Empty);
            int idx = actual != null ? lista.IndexOf(actual) : 0;
            combo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private static string ValorCombo(Guna2ComboBox combo)
        {
            string v = combo.SelectedItem as string;
            return string.IsNullOrEmpty(v) || v == Todos ? null : v;
        }

        private async void BuscarAsync()
        {
            if (IsDisposed || vida.IsCancellationRequested) return;
            int actual = ++version;
            if (busqueda != null) busqueda.Cancel();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(vida.Token);
            busqueda = cts;

            var filtro = new BitacoraFiltro
            {
                Desde = dtpDesde.Value.Date,
                Hasta = dtpHasta.Value.Date,
                Usuario = ValorCombo(cmbUsuario),
                Entidad = ValorCombo(cmbEntidad),
                Accion = ValorCombo(cmbAccion),
                Texto = string.IsNullOrWhiteSpace(txtTexto.Text) ? null : txtTexto.Text.Trim()
            };

            btnBuscar.Enabled = false;
            estado.ForeColor = Tema.TextoSecundario;
            estado.Text = "Buscando...";
            try
            {
                var resultado = await presenter.BuscarAsync(filtro, cts.Token);
                if (IsDisposed || actual != version) return;

                registros = resultado != null && resultado.Registros != null ? resultado.Registros : new List<BitacoraRegistro>();
                LlenarGrilla();
                if (resultado != null && !resultado.Instalada)
                {
                    estado.ForeColor = Tema.Advertencia;
                    estado.Text = resultado.Mensaje;
                }
                else if (resultado != null && resultado.Truncado)
                {
                    estado.ForeColor = Tema.Advertencia;
                    estado.Text = "Se muestran los primeros " + registros.Count.ToString(CultureInfo.CurrentCulture) + " registros: hay más que coinciden. Refiná los filtros.";
                }
                else
                {
                    estado.ForeColor = Tema.TextoSecundario;
                    estado.Text = registros.Count + (registros.Count == 1 ? " registro" : " registros");
                }
            }
            catch (OperationCanceledException) { }
            catch (UnauthorizedAccessException)
            {
                if (IsDisposed || actual != version) return;
                registros = new List<BitacoraRegistro>();
                LlenarGrilla();
                estado.ForeColor = Tema.Advertencia;
                estado.Text = "La bitácora solo está disponible para administradores.";
            }
            catch (Exception ex)
            {
                if (IsDisposed || actual != version) return;
                estado.ForeColor = Color.FromArgb(226, 96, 86);
                estado.Text = "No se pudo consultar la bitácora: " + ex.Message;
            }
            finally
            {
                if (!IsDisposed && actual == version) btnBuscar.Enabled = true;
                if (busqueda == cts) busqueda = null;
                cts.Dispose();
            }
        }

        private void LlenarGrilla()
        {
            grilla.SuspendLayout();
            grilla.Rows.Clear();
            foreach (var r in registros)
            {
                int i = grilla.Rows.Add(
                    r.Fecha.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    r.Usuario, r.Accion, r.Entidad, r.Resumen);
                var fila = grilla.Rows[i];
                fila.Tag = r;
                Color? fondo = ColorFila(r.Accion);
                if (fondo.HasValue) fila.DefaultCellStyle.BackColor = fondo.Value;
            }
            grilla.ResumeLayout();
            if (grilla.Rows.Count > 0) grilla.ClearSelection();
            MostrarDetalle();
        }

        /// <summary>Tinte suave por tipo de acción: alta, modificación, baja, copias de seguridad y errores.</summary>
        internal static Color? ColorFila(string accion)
        {
            switch ((accion ?? string.Empty).ToUpperInvariant())
            {
                case "INSERT": return Color.FromArgb(30, 62, 42);                // verde suave
                case "UPDATE": return Color.FromArgb(31, 48, 66);                // azul suave
                case "DELETE": return Color.FromArgb(82, 58, 30);                // naranja suave
                case "ERROR":
                case "RESPALDO_ERROR":
                case "ERROR_NO_CONTROLADO": return Color.FromArgb(78, 38, 38);   // rojo suave
                case "RESPALDO":
                case "RESTAURACION": return Tema.Mezclar(Tema.Fondo, Tema.TextoSecundario, 0.18); // gris verdoso discreto
                default: return null;
            }
        }

        // --------------------------------------------------------- Detalle
        private BitacoraRegistro RegistroSeleccionado()
        {
            if (grilla.SelectedRows.Count == 0) return null;
            return grilla.SelectedRows[0].Tag as BitacoraRegistro;
        }

        private void MostrarDetalle()
        {
            grillaDetalle.Rows.Clear();
            var r = RegistroSeleccionado();
            if (r == null) { txtDescripcion.Text = string.Empty; return; }

            var lineas = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.Rol)) lineas.Add("Rol: " + r.Rol);
            if (!string.IsNullOrWhiteSpace(r.Equipo)) lineas.Add("Equipo: " + r.Equipo);
            if (!string.IsNullOrWhiteSpace(r.VersionApp)) lineas.Add("Versión: " + r.VersionApp);
            if (!string.IsNullOrWhiteSpace(r.Origen)) lineas.Add("Origen: " + r.Origen);
            if (!string.IsNullOrWhiteSpace(r.EntidadId)) lineas.Add("ID: " + r.EntidadId);
            string desc = !string.IsNullOrWhiteSpace(r.Descripcion) ? r.Descripcion : r.Resumen;
            var texto = string.Join("\n", lineas) + (lineas.Count > 0 ? "\n\n" : string.Empty) + (desc ?? string.Empty);
            txtDescripcion.Text = texto.Replace("\r\n", "\n").Replace("\n", "\r\n");

            foreach (var c in ResumenBitacora.Cambios(r))
                grillaDetalle.Rows.Add(c.Campo, c.Antes ?? string.Empty, c.Despues ?? string.Empty);
            grillaDetalle.ClearSelection();
        }

        // ---------------------------------------------------------- Excel
        private void Exportar()
        {
            if (registros.Count == 0)
            {
                ToastNotification.ShowWarning("No hay datos para exportar.");
                return;
            }

            using (var guardar = new SaveFileDialog())
            {
                guardar.Filter = "Excel Files (*.xlsx)|*.xlsx";
                guardar.FileName = "Bitacora_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                try
                {
                    string carpeta = servicios.ReportsDirectory;
                    if (!string.IsNullOrEmpty(carpeta) && Directory.Exists(carpeta)) guardar.InitialDirectory = carpeta;
                }
                catch (Exception) { }
                if (guardar.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    using (var excel = new OfficeOpenXml.ExcelPackage())
                    {
                        var ws = excel.Workbook.Worksheets.Add("Bitácora");
                        string[] cab = { "Fecha", "Usuario", "Rol", "Equipo", "Versión", "Origen", "Acción", "Entidad", "ID", "Resumen", "Datos antes", "Datos después" };
                        for (int i = 0; i < cab.Length; i++)
                        {
                            var c = ws.Cells[1, i + 1];
                            c.Value = cab[i];
                            c.Style.Font.Bold = true;
                            c.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            c.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(35, 63, 43));
                            c.Style.Font.Color.SetColor(Color.White);
                        }
                        int fila = 2;
                        foreach (var r in registros)
                        {
                            ws.Cells[fila, 1].Value = r.Fecha.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            ws.Cells[fila, 2].Value = r.Usuario;
                            ws.Cells[fila, 3].Value = r.Rol;
                            ws.Cells[fila, 4].Value = r.Equipo;
                            ws.Cells[fila, 5].Value = r.VersionApp;
                            ws.Cells[fila, 6].Value = r.Origen;
                            ws.Cells[fila, 7].Value = r.Accion;
                            ws.Cells[fila, 8].Value = r.Entidad;
                            ws.Cells[fila, 9].Value = r.EntidadId;
                            ws.Cells[fila, 10].Value = r.Resumen;
                            ws.Cells[fila, 11].Value = r.DatosAntes;
                            ws.Cells[fila, 12].Value = r.DatosDespues;
                            fila++;
                        }
                        // Autoajuste solo en las columnas cortas; las de texto largo quedan con ancho fijo.
                        for (int c = 1; c <= 9; c++) ws.Column(c).AutoFit();
                        ws.Column(10).Width = 70;
                        ws.Column(11).Width = 45;
                        ws.Column(12).Width = 45;
                        excel.SaveAs(new FileInfo(guardar.FileName));
                    }
                    ToastNotification.ShowSuccess("Exportación exitosa.");
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al exportar: " + ex.Message);
                }
            }
        }
    }
}
