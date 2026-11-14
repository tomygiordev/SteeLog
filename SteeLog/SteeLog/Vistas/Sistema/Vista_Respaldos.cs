using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using SteeLog.Core.Application.Respaldos;
using SteeLog.Presentation;
using SteeLog.UI;

namespace SteeLog.Vistas
{
    /// <summary>
    /// Copias de seguridad de la base (solo Admin): estado general, listado de copias con su detalle, creación, importación,
    /// verificación, restauración y configuración del respaldo automático. Interfaz armada por código.
    /// </summary>
    public class Vista_Respaldos : Form, IRefrescable
    {
        internal const string PalabraRestaurar = "RESTAURAR";
        private static readonly CultureInfo Ar = new CultureInfo("es-AR");
        private static readonly Color Rojo = Color.FromArgb(226, 96, 86);

        private readonly IServiciosVista servicios;
        private readonly RespaldosPresenter presenter;
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private CancellationTokenSource operacion;
        private int versionCarga;
        private bool ocupado;
        private bool configSucia;
        private bool llenandoConfig;
        private EstadoRespaldos estadoActual;
        private readonly Dictionary<string, bool> verificados = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private PanelAntiParpadeo banda, tarjetas, ficha, botonesFicha, cuerpoConfig;
        private Panel contenedor, panelGrilla, vacio, aviso, progresoPanel, configPanel, fichaScroll;
        private Label titulo, subtitulo, lblAviso, lblEtapa, lblPorcentaje, lblVacioTitulo, lblVacioTexto, lblFichaTitulo, lblFichaVacia;
        private TarjetaEstado tarjUltima, tarjCopias, tarjProxima, tarjEstado;
        private Guna2Button btnCrear, btnImportar, btnAbrirCarpeta, btnCancelar, btnCerrarAviso, btnVacioCrear;
        private Guna2Button btnVerificar, btnRestaurar, btnEliminar, btnConfigToggle, btnExaminar, btnGuardarConfig;
        private Guna2DataGridView grilla;
        private Guna2TextBox txtCarpeta;
        private Guna2ToggleSwitch tglAutomatico;
        private Guna2NumericUpDown numHoras, numConservar;
        private BarraProgreso barra;
        private TableLayoutPanel fichaTabla;
        private ContextMenuStrip menu;
        private ToolStripMenuItem mnuVerificar, mnuRestaurar, mnuEliminar, mnuCarpeta;
        private readonly ToolTip tip = new ToolTip();
        private bool configAbierta;

        // ---- Puntos de enganche (los usa la propia vista y las pruebas; no hay estado global) ----
        /// <summary>Pide un texto (mensaje, título) y devuelve null si se cancela.</summary>
        public Func<string, string, string> PedirTexto;
        /// <summary>Pide una confirmación (mensaje, título, destructiva).</summary>
        public Func<string, string, bool> Confirmar;
        /// <summary>Informa al usuario (título, mensaje, esError).</summary>
        public Action<string, string, bool> Informar;
        /// <summary>Cierra la sesión y vuelve al login (los datos cambiaron).</summary>
        public Action CerrarSesion;
        /// <summary>Elige un archivo .dump a importar (devuelve null si se cancela).</summary>
        public Func<string> ElegirArchivo;

        public Vista_Respaldos(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            presenter = servicios.CrearRespaldosPresenter();
            Text = "Copias de seguridad";
            Name = "Vista_Respaldos";
            Tema.SinEscala(this);
            BackColor = Tema.Fondo;

            PedirTexto = (m, t) => Dialogo.Pedir(m, t, string.Empty, this);
            Confirmar = (m, t) => Dialogo.Confirmar(m, t, true, this);
            Informar = (t, m, error) =>
            {
                if (error) Dialogo.ErrorYaRegistrado(m, t, this);
                else Dialogo.Mostrar(this, m, t, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            CerrarSesion = () =>
            {
                servicios.Credenciales.Clear();
                servicios.Sesion.Clear();
                Application.Restart();
            };
            ElegirArchivo = () =>
            {
                using (var abrir = new OpenFileDialog())
                {
                    abrir.Title = "Importar copia de seguridad";
                    abrir.Filter = "Copias de seguridad (*.dump)|*.dump";
                    abrir.CheckFileExists = true;
                    return abrir.ShowDialog(this) == DialogResult.OK ? abrir.FileName : null;
                }
            };

            ConstruirUi();
            GestionUi.Aplicar(this);
            EstilizarBotones();
            Load += (s, e) => CargarAsync();
            Disposed += (s, e) => CancelarOperaciones();
        }

        /// <summary>Se llama cuando la vista vuelve del caché de VistaGeneral.</summary>
        public void Refrescar()
        {
            if (!ocupado) CargarAsync();
        }

        private void CancelarOperaciones()
        {
            try
            {
                if (!vida.IsCancellationRequested) vida.Cancel();
                var op = operacion;
                if (op != null) op.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        // ================================================================== Formato
        /// <summary>"hace 3 h", "ayer", "hace 2 semanas": lenguaje natural para la antigüedad de una copia.</summary>
        public static string TextoHace(DateTime fecha, DateTime ahora)
        {
            var d = ahora - fecha;
            if (d < TimeSpan.Zero) d = TimeSpan.Zero;
            if (d.TotalMinutes < 1) return "hace instantes";
            if (d.TotalMinutes < 60) return "hace " + (int)d.TotalMinutes + " min";
            if (d.TotalHours < 24)
            {
                int h = (int)d.TotalHours;
                return "hace " + h + (h == 1 ? " hora" : " horas");
            }
            int dias = (int)d.TotalDays;
            if (dias == 1) return "ayer";
            if (dias < 14) return "hace " + dias + " días";
            if (dias < 60) return "hace " + (dias / 7) + " semanas";
            if (dias < 730) return "hace " + (dias / 30) + " meses";
            return "hace " + (dias / 365) + " años";
        }

        /// <summary>Tamaño legible con formato es-AR (KB / MB / GB).</summary>
        public static string TextoTamano(long bytes)
        {
            if (bytes < 0) bytes = 0;
            if (bytes < 1024) return bytes.ToString("N0", Ar) + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString(kb < 10 ? "N1" : "N0", Ar) + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString(mb < 10 ? "N2" : "N1", Ar) + " MB";
            double gb = mb / 1024.0;
            return gb.ToString(gb < 10 ? "N2" : "N1", Ar) + " GB";
        }

        /// <summary>La restauración solo avanza si se escribió exactamente la palabra pedida.</summary>
        public static bool ConfirmacionValida(string texto)
        {
            return texto != null && string.Equals(texto.Trim(), PalabraRestaurar, StringComparison.Ordinal);
        }

        /// <summary>Verde menos de 24 h, ámbar menos de 7 días, rojo más (o sin copias).</summary>
        internal static Color ColorSalud(DateTime? ultima, DateTime ahora)
        {
            if (!ultima.HasValue) return Rojo;
            var d = ahora - ultima.Value;
            if (d < TimeSpan.FromHours(24)) return Tema.Acento;
            if (d < TimeSpan.FromDays(7)) return Tema.Advertencia;
            return Rojo;
        }

        private static string NombreTipo(TipoRespaldo t)
        {
            switch (t)
            {
                case TipoRespaldo.Manual: return "Manual";
                case TipoRespaldo.Automatico: return "Automática";
                case TipoRespaldo.PreRestauracion: return "Pre-restauración";
                default: return "Importada";
            }
        }

        private static Color ColorTipo(TipoRespaldo t)
        {
            switch (t)
            {
                case TipoRespaldo.Manual: return Tema.Info;
                case TipoRespaldo.Automatico: return Tema.Acento;
                case TipoRespaldo.PreRestauracion: return Tema.Advertencia;
                default: return Tema.TextoSecundario;
            }
        }

        private static string Fecha(DateTime f) { return f.ToString("dd/MM/yyyy HH:mm", Ar); }

        // ================================================================== UI
        private void ConstruirUi()
        {
            // ---- Banda: título + acciones
            banda = new PanelAntiParpadeo { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 1 };
            banda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            banda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
            banda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            banda.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            banda.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titulo = new Label { Text = "Copias de seguridad", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            btnCrear = new Guna2Button { Name = "btnCrearRespaldo", Text = "Crear copia ahora" };
            btnImportar = new Guna2Button { Name = "btnImportarRespaldo", Text = "Importar…" };
            btnAbrirCarpeta = new Guna2Button { Name = "btnAbrirCarpeta", Text = "Abrir carpeta" };
            banda.Controls.Add(titulo, 0, 0);
            int col = 1;
            foreach (var b in new[] { btnCrear, btnImportar, btnAbrirCarpeta })
            {
                b.Dock = DockStyle.None; b.Anchor = AnchorStyles.None; b.Size = new Size(b == btnCrear ? 156 : b == btnImportar ? 116 : 138, Tema.AltoControl);
                b.Margin = new Padding(6, 0, 6, 0);
                banda.Controls.Add(b, col++, 0);
            }
            GestionUi.Banda(banda, titulo);
            banda.Padding = new Padding(28, 0, 22, 0);

            // ---- Subtítulo
            subtitulo = new Label
            {
                Name = "lblSubtituloRespaldos", Dock = DockStyle.Top, Height = 34, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(GestionUi.Pad, 2, GestionUi.Pad, 0), Font = Tema.Fuente(10f), ForeColor = Tema.TextoSecundario, BackColor = Tema.Fondo,
                Text = "Resguardá toda la base de datos en un archivo y recuperala cuando haga falta. Las copias automáticas se crean al iniciar sesión."
            };

            // ---- Tarjetas de estado
            tarjetas = new PanelAntiParpadeo { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 1, BackColor = Tema.Fondo };
            for (int i = 0; i < 4; i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tarjetas.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tarjetas.Padding = new Padding(GestionUi.Pad - 6, 4, GestionUi.Pad - 6, 12);
            tarjetas.Height = TarjetaEstado.Alto + 16;
            tarjUltima = new TarjetaEstado("Última copia") { Name = "tarjUltima" };
            tarjCopias = new TarjetaEstado("Copias guardadas") { Name = "tarjCopias" };
            tarjProxima = new TarjetaEstado("Próxima automática") { Name = "tarjProxima" };
            tarjEstado = new TarjetaEstado("Estado") { Name = "tarjEstado" };
            var lista = new[] { tarjUltima, tarjCopias, tarjProxima, tarjEstado };
            for (int i = 0; i < lista.Length; i++)
            {
                lista[i].Dock = DockStyle.Fill;
                lista[i].Margin = new Padding(6, 0, 6, 0);
                tarjetas.Controls.Add(lista[i], i, 0);
            }

            // ---- Aviso (resultado de la última acción)
            aviso = new Panel { Name = "panelAviso", Dock = DockStyle.Top, Height = 44, BackColor = Tema.Fondo, Visible = false, Padding = new Padding(GestionUi.Pad, 0, GestionUi.Pad, 8) };
            var avisoCaja = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Superficie, Padding = new Padding(14, 0, 4, 0) };
            lblAviso = new Label { Name = "lblAviso", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = Tema.Fuente(10f), ForeColor = Tema.Texto, BackColor = Color.Transparent, AutoEllipsis = true };
            btnCerrarAviso = new Guna2Button { Text = "Cerrar", Dock = DockStyle.Right, Width = 78 };
            avisoCaja.Controls.Add(lblAviso);
            avisoCaja.Controls.Add(btnCerrarAviso);
            aviso.Controls.Add(avisoCaja);
            avisoCaja.Paint += (s, e) =>
            {
                var c = avisoCaja.Tag is Color ? (Color)avisoCaja.Tag : Tema.Acento;
                using (var br = new SolidBrush(c)) e.Graphics.FillRectangle(br, 0, 0, 4, avisoCaja.Height);
            };
            aviso.Tag = avisoCaja;
            btnCerrarAviso.Click += (s, e) => aviso.Visible = false;

            // ---- Progreso
            progresoPanel = new Panel { Name = "panelProgreso", Dock = DockStyle.Top, Height = 76, BackColor = Tema.Fondo, Visible = false, Padding = new Padding(GestionUi.Pad, 0, GestionUi.Pad, 10) };
            var progCaja = new PanelAntiParpadeo { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, BackColor = Tema.Superficie, Padding = new Padding(16, 10, 12, 10) };
            progCaja.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            progCaja.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            progCaja.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            progCaja.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            progCaja.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            lblEtapa = new Label { Name = "lblEtapa", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = Tema.Fuente(10.5f, true), ForeColor = Tema.Texto, BackColor = Color.Transparent, AutoEllipsis = true };
            lblPorcentaje = new Label { Name = "lblPorcentaje", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleRight, Font = Tema.Fuente(10.5f, true), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent };
            barra = new BarraProgreso { Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 6) };
            btnCancelar = new Guna2Button { Name = "btnCancelarOperacion", Text = "Cancelar", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
            progCaja.Controls.Add(lblEtapa, 0, 0);
            progCaja.SetColumnSpan(lblEtapa, 2);
            progCaja.Controls.Add(btnCancelar, 2, 0);
            progCaja.SetRowSpan(btnCancelar, 2);
            progCaja.Controls.Add(barra, 0, 1);
            progCaja.Controls.Add(lblPorcentaje, 1, 1);
            progresoPanel.Controls.Add(progCaja);
            btnCancelar.Click += (s, e) => { var op = operacion; if (op != null) { try { op.Cancel(); } catch (ObjectDisposedException) { } } btnCancelar.Enabled = false; };

            // ---- Configuración (plegable, abajo)
            btnConfigToggle = new Guna2Button { Name = "btnConfiguracion", Text = "Configuración  ▸", Dock = DockStyle.Top, Height = 40 };
            cuerpoConfig = new PanelAntiParpadeo { Name = "cuerpoConfig", Dock = DockStyle.Top, ColumnCount = 7, RowCount = 2, BackColor = Tema.Superficie, Visible = false };
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            cuerpoConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
            cuerpoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            cuerpoConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            cuerpoConfig.Padding = new Padding(GestionUi.Pad, 10, GestionUi.Pad, 10);
            cuerpoConfig.Height = 20 + 96;

            txtCarpeta = new Guna2TextBox { Name = "txtCarpetaRespaldos", PlaceholderText = "Carpeta donde se guardan las copias" };
            btnExaminar = new Guna2Button { Name = "btnExaminar", Text = "Examinar…" };
            tglAutomatico = new Guna2ToggleSwitch { Name = "tglAutomatico", Size = new Size(44, 22) };
            numHoras = new Guna2NumericUpDown { Name = "numHoras", Minimum = 1, Maximum = ConfiguracionRespaldo.HorasMaximo, Value = ConfiguracionRespaldo.HorasPorDefecto };
            numConservar = new Guna2NumericUpDown { Name = "numConservar", Minimum = 1, Maximum = ConfiguracionRespaldo.ConservarMaximo, Value = ConfiguracionRespaldo.ConservarPorDefecto };
            btnGuardarConfig = new Guna2Button { Name = "btnGuardarConfig", Text = "Guardar" };

            cuerpoConfig.Controls.Add(EtiquetaConfig("Carpeta"), 0, 0);
            txtCarpeta.Dock = DockStyle.Fill; txtCarpeta.Margin = new Padding(0, 4, 8, 4);
            cuerpoConfig.Controls.Add(txtCarpeta, 1, 0);
            cuerpoConfig.SetColumnSpan(txtCarpeta, 5);
            btnExaminar.Dock = DockStyle.Fill; btnExaminar.Margin = new Padding(0, 4, 0, 4);
            cuerpoConfig.Controls.Add(btnExaminar, 6, 0);

            var filaAuto = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Margin = Padding.Empty, Padding = new Padding(0, 10, 0, 0) };
            tglAutomatico.Margin = new Padding(0, 1, 10, 0);
            var lblAuto = new Label { Text = "Copia automática", AutoSize = true, Font = Tema.Fuente(10.5f), ForeColor = Tema.Texto, BackColor = Color.Transparent, Margin = new Padding(0, 2, 0, 0) };
            filaAuto.Controls.Add(tglAutomatico);
            filaAuto.Controls.Add(lblAuto);
            cuerpoConfig.Controls.Add(filaAuto, 0, 1);
            cuerpoConfig.SetColumnSpan(filaAuto, 2);
            cuerpoConfig.Controls.Add(EtiquetaConfig("Cada (horas)"), 2, 1);
            numHoras.Dock = DockStyle.Fill; numHoras.Margin = new Padding(0, 4, 16, 4);
            cuerpoConfig.Controls.Add(numHoras, 3, 1);
            var lblCons = EtiquetaConfig("Conservar (autom.)"); lblCons.Margin = new Padding(0);
            cuerpoConfig.Controls.Add(lblCons, 4, 1);
            numConservar.Dock = DockStyle.Fill; numConservar.Margin = new Padding(0, 4, 8, 4);
            cuerpoConfig.Controls.Add(numConservar, 5, 1);
            btnGuardarConfig.Dock = DockStyle.Fill; btnGuardarConfig.Margin = new Padding(0, 4, 0, 4);
            cuerpoConfig.Controls.Add(btnGuardarConfig, 6, 1);

            configPanel = new Panel { Name = "panelConfig", Dock = DockStyle.Bottom, BackColor = Tema.Fondo, Padding = new Padding(GestionUi.Pad, 0, GestionUi.Pad, 10) };
            var configCaja = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Superficie };
            configCaja.Controls.Add(cuerpoConfig);
            configCaja.Controls.Add(btnConfigToggle);
            configPanel.Controls.Add(configCaja);
            configPanel.Height = 40 + 10;
            btnConfigToggle.Click += (s, e) => AlternarConfig();
            btnExaminar.Click += (s, e) => ExaminarCarpeta();
            btnGuardarConfig.Click += (s, e) => GuardarConfigAsync();
            EventHandler sucia = (s, e) => { if (!llenandoConfig) configSucia = true; };
            txtCarpeta.TextChanged += sucia;
            tglAutomatico.CheckedChanged += sucia;
            numHoras.ValueChanged += sucia;
            numConservar.ValueChanged += sucia;
            tglAutomatico.CheckedChanged += (s, e) => { numHoras.Enabled = tglAutomatico.Checked && !ocupado; numConservar.Enabled = tglAutomatico.Checked && !ocupado; };

            // ---- Cuerpo: grilla + detalle
            contenedor = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo, Padding = new Padding(GestionUi.Pad, 0, GestionUi.Pad, 10) };
            ficha = new PanelAntiParpadeo { Name = "panelFicha", Dock = DockStyle.Right, Width = 312, ColumnCount = 1, RowCount = 3, BackColor = Tema.Superficie };
            ficha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ficha.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.AltoHeader));
            ficha.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ficha.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            lblFichaTitulo = new Label { Text = "Detalle de la copia", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = Tema.FuenteHeaderGrilla, ForeColor = Color.White, BackColor = Tema.Banda, Padding = new Padding(16, 0, 8, 0), Margin = Padding.Empty };
            ficha.Controls.Add(lblFichaTitulo, 0, 0);

            fichaScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Tema.Superficie, Margin = Padding.Empty, Padding = new Padding(14, 6, 6, 2) };
            fichaTabla = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, BackColor = Tema.Superficie, Margin = Padding.Empty };
            fichaTabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            fichaTabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            lblFichaVacia = new Label
            {
                Name = "lblFichaVacia", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = Tema.Fuente(10.5f),
                ForeColor = Tema.TextoApagado, BackColor = Tema.Superficie, Text = "Seleccioná una copia de la lista\npara ver su detalle y operar sobre ella."
            };
            fichaScroll.Controls.Add(fichaTabla);
            Tema.ScrollbarsOscuros(fichaScroll);
            ficha.Controls.Add(fichaScroll, 0, 1);

            botonesFicha = new PanelAntiParpadeo { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Tema.Superficie, Padding = new Padding(14, 8, 14, 12), Margin = Padding.Empty };
            foreach (float peso in new[] { 29F, 42F, 29F }) botonesFicha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, peso));
            botonesFicha.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            btnVerificar = new Guna2Button { Name = "btnVerificar", Text = "Verificar" };
            btnRestaurar = new Guna2Button { Name = "btnRestaurar", Text = "Restaurar…" };
            btnEliminar = new Guna2Button { Name = "btnEliminarRespaldo", Text = "Eliminar" };
            var bs = new[] { btnVerificar, btnRestaurar, btnEliminar };
            for (int i = 0; i < 3; i++) { bs[i].Dock = DockStyle.Fill; bs[i].Margin = new Padding(i == 0 ? 0 : 3, 0, i == 2 ? 0 : 3, 0); botonesFicha.Controls.Add(bs[i], i, 0); }
            ficha.Controls.Add(botonesFicha, 0, 2);
            btnVerificar.Click += (s, e) => VerificarSeleccionadaAsync();
            btnRestaurar.Click += (s, e) => { var r = Seleccionada(); if (r != null) SolicitarRestauracionAsync(r.Id); };
            btnEliminar.Click += (s, e) => EliminarSeleccionadaAsync();

            panelGrilla = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo, Padding = new Padding(0, 0, 14, 0) };
            grilla = new Guna2DataGridView
            {
                Name = "dgvRespaldos", AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, ReadOnly = true,
                MultiSelect = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, BackgroundColor = Tema.Fondo, Dock = DockStyle.Fill
            };
            Tema.Grilla(grilla);
            AgregarColumna("fecha", "Fecha", 132, DataGridViewContentAlignment.MiddleLeft);
            AgregarColumna("tipo", "Tipo", 146, DataGridViewContentAlignment.MiddleLeft);
            AgregarColumna("tamano", "Tamaño", 84, DataGridViewContentAlignment.MiddleRight);
            AgregarColumna("usuario", "Usuario", 84, DataGridViewContentAlignment.MiddleLeft);
            AgregarColumna("nota", "Nota", 80, DataGridViewContentAlignment.MiddleLeft);
            AgregarColumna("integridad", "Integridad", 122, DataGridViewContentAlignment.MiddleLeft);
            grilla.Columns["nota"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grilla.Columns["nota"].MinimumWidth = 70;
            grilla.SelectionChanged += (s, e) => MostrarFicha();
            grilla.CellPainting += PintarCeldaGrilla;
            grilla.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) { grilla.ClearSelection(); grilla.Rows[e.RowIndex].Selected = true; }
            };
            grilla.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) VerificarSeleccionadaAsync(); };
            panelGrilla.Controls.Add(grilla);

            vacio = new Panel { Name = "panelVacio", Dock = DockStyle.Fill, BackColor = Tema.Fondo, Visible = false };
            lblVacioTitulo = new Label { Name = "lblVacioTitulo", AutoSize = false, TextAlign = ContentAlignment.BottomCenter, Font = Tema.Fuente(14f, true), ForeColor = Tema.Texto, BackColor = Color.Transparent, Text = "Todavía no hay copias de seguridad" };
            lblVacioTexto = new Label { Name = "lblVacioTexto", AutoSize = false, TextAlign = ContentAlignment.TopCenter, Font = Tema.Fuente(10.5f), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent, Text = "Creá la primera ahora o importá un archivo .dump existente.\nLas copias automáticas aparecerán acá a medida que se generen." };
            btnVacioCrear = new Guna2Button { Name = "btnVacioCrear", Text = "Crear la primera copia", Size = new Size(210, Tema.AltoBotonPrincipal) };
            vacio.Controls.Add(lblVacioTitulo); vacio.Controls.Add(lblVacioTexto); vacio.Controls.Add(btnVacioCrear);
            vacio.Resize += (s, e) => CentrarVacio();
            btnVacioCrear.Click += (s, e) => CrearAsync();
            panelGrilla.Controls.Add(vacio);
            vacio.BringToFront();

            contenedor.Controls.Add(panelGrilla);
            contenedor.Controls.Add(ficha);
            ficha.BringToFront();
            panelGrilla.BringToFront();

            // ---- Menú contextual
            menu = new ContextMenuStrip { ShowImageMargin = false, BackColor = Tema.FondoInput, ForeColor = Tema.Texto, Font = Tema.Fuente(10.5f), Renderer = new RenderizadorMenu() };
            mnuVerificar = new ToolStripMenuItem("Verificar integridad");
            mnuRestaurar = new ToolStripMenuItem("Restaurar…");
            mnuEliminar = new ToolStripMenuItem("Eliminar…");
            mnuCarpeta = new ToolStripMenuItem("Abrir carpeta de copias");
            menu.Items.AddRange(new ToolStripItem[] { mnuVerificar, mnuRestaurar, new ToolStripSeparator(), mnuEliminar, new ToolStripSeparator(), mnuCarpeta });
            mnuVerificar.Click += (s, e) => VerificarSeleccionadaAsync();
            mnuRestaurar.Click += (s, e) => { var r = Seleccionada(); if (r != null) SolicitarRestauracionAsync(r.Id); };
            mnuEliminar.Click += (s, e) => EliminarSeleccionadaAsync();
            mnuCarpeta.Click += (s, e) => AbrirCarpeta();
            grilla.ContextMenuStrip = menu;
            menu.Opening += (s, e) => { if (Seleccionada() == null) e.Cancel = true; };

            btnCrear.Click += (s, e) => CrearAsync();
            btnImportar.Click += (s, e) => ImportarAsync();
            btnAbrirCarpeta.Click += (s, e) => AbrirCarpeta();

            // Orden: el control Fill primero, luego los acoplados (el último agregado queda arriba del todo).
            Controls.Add(contenedor);
            Controls.Add(configPanel);
            Controls.Add(progresoPanel);
            Controls.Add(aviso);
            Controls.Add(tarjetas);
            Controls.Add(subtitulo);
            Controls.Add(banda);

            Resize += (s, e) => CentrarVacio();
        }

        private static Label EtiquetaConfig(string texto)
        {
            return new Label { Text = texto, Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = Tema.Fuente(10.5f), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent, Margin = new Padding(0, 0, 8, 0) };
        }

        private void AgregarColumna(string nombre, string encabezado, int ancho, DataGridViewContentAlignment alineacion)
        {
            int i = grilla.Columns.Add(nombre, encabezado);
            var c = grilla.Columns[i];
            c.Width = ancho;
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            c.DefaultCellStyle.Alignment = alineacion;
            if (alineacion == DataGridViewContentAlignment.MiddleRight) c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void EstilizarBotones()
        {
            Tema.Boton(btnCrear, Variante.Primario);
            Tema.Boton(btnImportar, Variante.Secundario);
            Tema.Boton(btnAbrirCarpeta, Variante.Secundario);
            Tema.Boton(btnCancelar, Variante.Secundario);
            Tema.Boton(btnCerrarAviso, Variante.Fantasma);
            Tema.Boton(btnVacioCrear, Variante.Primario);
            Tema.Boton(btnVerificar, Variante.Secundario);
            Tema.Boton(btnRestaurar, Variante.Primario);
            Tema.Boton(btnEliminar, Variante.PeligroSuave);
            foreach (var b in new[] { btnVerificar, btnRestaurar, btnEliminar }) b.Font = Tema.Fuente(9.5f, true);
            Tema.Boton(btnExaminar, Variante.Secundario);
            Tema.Boton(btnGuardarConfig, Variante.Primario);
            Tema.Boton(btnConfigToggle, Variante.Fantasma);
            btnConfigToggle.TextAlign = HorizontalAlignment.Left;
            btnConfigToggle.TextOffset = new Point(10, 0);
            btnConfigToggle.BorderThickness = 0;
            btnConfigToggle.FillColor = Tema.Superficie;
            btnConfigToggle.HoverState.FillColor = Tema.Banda;
            btnConfigToggle.HoverState.BorderColor = Color.Transparent;
            btnConfigToggle.ForeColor = Tema.Texto;
            btnConfigToggle.HoverState.ForeColor = Color.White;
            btnConfigToggle.BorderRadius = 0;
            Tema.Toggle(tglAutomatico);
            Tema.Input(txtCarpeta);
            Tema.Numerico(numHoras);
            Tema.Numerico(numConservar);
            btnCerrarAviso.BorderThickness = 0;
            btnCerrarAviso.FillColor = Color.Transparent;
            btnCerrarAviso.Font = Tema.Fuente(9.5f, true);
        }

        private void CentrarVacio()
        {
            if (vacio == null || lblVacioTitulo == null) return;
            int w = Math.Max(200, vacio.ClientSize.Width);
            int centroY = Math.Max(110, vacio.ClientSize.Height / 2 - 10);
            lblVacioTitulo.SetBounds(0, centroY - 92, w, 32);
            lblVacioTexto.SetBounds(0, centroY - 54, w, 48);
            btnVacioCrear.Location = new Point((w - btnVacioCrear.Width) / 2, centroY + 6);
        }

        private void AlternarConfig()
        {
            configAbierta = !configAbierta;
            cuerpoConfig.Visible = configAbierta;
            btnConfigToggle.Text = configAbierta ? "Configuración  ▾" : "Configuración  ▸";
            configPanel.Height = configAbierta ? 40 + cuerpoConfig.Height + 10 : 40 + 10;
        }

        // ================================================================== Pintado de la grilla
        private void PintarCeldaGrilla(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var info = grilla.Rows[e.RowIndex].Tag as RespaldoInfo;
            if (info == null) return;
            string col = grilla.Columns[e.ColumnIndex].Name;
            if (col == "tipo")
            {
                UiVista.PintarPildora(e, NombreTipo(info.Tipo), ColorTipo(info.Tipo));
            }
            else if (col == "integridad")
            {
                e.PaintBackground(e.ClipBounds, e.State.HasFlag(DataGridViewElementStates.Selected));
                string texto; Color color;
                bool ok;
                if (verificados.TryGetValue(info.Id, out ok)) { texto = ok ? "✓  Verificada" : "✗  Con errores"; color = ok ? Tema.Acento : Rojo; }
                else { texto = "—  Sin verificar"; color = Tema.TextoApagado; }
                var r = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, texto, Tema.Fuente(10f, true), r, color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                e.Handled = true;
            }
        }

        // ================================================================== Carga
        private async void CargarAsync()
        {
            if (IsDisposed || vida.IsCancellationRequested) return;
            int actual = ++versionCarga;
            try
            {
                var estado = await presenter.ObtenerEstadoAsync(vida.Token);
                if (IsDisposed || actual != versionCarga) return;
                if (estado == null) throw new InvalidOperationException("No se obtuvo el estado de las copias de seguridad.");
                AplicarEstado(estado);
            }
            catch (OperationCanceledException) { }
            catch (UnauthorizedAccessException)
            {
                if (IsDisposed || actual != versionCarga) return;
                Sinestado("Las copias de seguridad solo están disponibles para administradores.");
            }
            catch (Exception ex)
            {
                if (IsDisposed || actual != versionCarga) return;
                Sinestado("No se pudo consultar el estado de las copias: " + ex.Message);
            }
        }

        /// <summary>Muestra un error de carga en la propia vista, sin romper nada.</summary>
        private void Sinestado(string mensaje)
        {
            estadoActual = null;
            tarjUltima.Establecer("—", "Sin datos", Tema.TextoApagado, Tema.Borde, null);
            tarjCopias.Establecer("—", "Sin datos", Tema.TextoApagado, Tema.Borde, null);
            tarjProxima.Establecer("—", "Sin datos", Tema.TextoApagado, Tema.Borde, null);
            tarjEstado.Establecer("Sin datos", mensaje, Rojo, Rojo, mensaje);
            LlenarGrilla(new List<RespaldoInfo>());
            MostrarAviso(TipoAviso.Error, mensaje);
            ActualizarHabilitados();
        }

        private void AplicarEstado(EstadoRespaldos e)
        {
            estadoActual = e;
            var ahora = DateTime.Now;
            var copias = (e.Respaldos ?? new List<RespaldoInfo>()).ToList();

            // 1) Última copia
            var ultimo = e.Ultimo ?? copias.FirstOrDefault();
            if (ultimo == null)
                tarjUltima.Establecer("Sin copias", "Todavía no se creó ninguna copia de seguridad.", Rojo, Rojo, null);
            else
            {
                var color = ColorSalud(ultimo.Fecha, ahora);
                tarjUltima.Establecer(TextoHace(ultimo.Fecha, ahora), Fecha(ultimo.Fecha) + " · " + NombreTipo(ultimo.Tipo), color, color, null);
            }

            // 2) Copias guardadas
            string libre = e.EspacioLibreBytes.HasValue ? " · " + TextoTamano(e.EspacioLibreBytes.Value) + " libres" : string.Empty;
            tarjCopias.Establecer(copias.Count.ToString("N0", Ar), TextoTamano(e.EspacioUsadoBytes) + " usados" + libre, Tema.Texto, Tema.Info, null);

            // 3) Próxima automática
            var cfg = e.Configuracion;
            if (cfg == null || !cfg.AutomaticoActivo || !e.ProximoAutomatico.HasValue)
                tarjProxima.Establecer("Desactivada", "Activala en Configuración para que se haga sola.", Tema.TextoApagado, Tema.TextoApagado, null);
            else if (e.ProximoAutomatico.Value <= ahora)
                tarjProxima.Establecer("Pendiente", "Se creará al próximo inicio de sesión.", Tema.Advertencia, Tema.Advertencia, null);
            else
                tarjProxima.Establecer(Fecha(e.ProximoAutomatico.Value), "Cada " + cfg.HorasEntreAutomaticos + " h · conserva " + cfg.Conservar, Tema.Texto, Tema.Acento, null);

            // 4) Estado
            string versiones = "PostgreSQL " + (string.IsNullOrWhiteSpace(e.VersionServidor) ? "?" : e.VersionServidor)
                + " · herramientas " + (string.IsNullOrWhiteSpace(e.VersionHerramientas) ? "?" : e.VersionHerramientas);
            if (!e.HerramientasDisponibles)
                tarjEstado.Establecer("Sin herramientas", string.IsNullOrWhiteSpace(e.MensajeHerramientas) ? "No se encontraron pg_dump y pg_restore." : e.MensajeHerramientas, Rojo, Rojo, e.MensajeHerramientas);
            else if (!e.BaseConectada)
                tarjEstado.Establecer("Base sin conexión", string.IsNullOrWhiteSpace(e.MensajeBase) ? "No se pudo conectar con la base de datos." : e.MensajeBase, Rojo, Rojo, e.MensajeBase);
            else
                tarjEstado.Establecer("Operativo", "Base conectada · " + versiones, Tema.Acento, Tema.Acento, versiones);

            // Configuración (no se pisa lo que el usuario está editando)
            if (cfg != null && !configSucia) LlenarConfig(cfg);

            LlenarGrilla(copias);
            ActualizarHabilitados();
        }

        private void LlenarConfig(ConfiguracionRespaldo cfg)
        {
            llenandoConfig = true;
            try
            {
                txtCarpeta.Text = cfg.Carpeta ?? string.Empty;
                tglAutomatico.Checked = cfg.AutomaticoActivo;
                numHoras.Value = Math.Max(numHoras.Minimum, Math.Min(numHoras.Maximum, cfg.HorasEntreAutomaticos));
                numConservar.Value = Math.Max(numConservar.Minimum, Math.Min(numConservar.Maximum, cfg.Conservar));
                numHoras.Enabled = tglAutomatico.Checked && !ocupado;
                numConservar.Enabled = tglAutomatico.Checked && !ocupado;
            }
            finally { llenandoConfig = false; }
        }

        private void LlenarGrilla(IList<RespaldoInfo> copias)
        {
            string idPrevio = Seleccionada() != null ? Seleccionada().Id : null;
            grilla.SuspendLayout();
            grilla.Rows.Clear();
            foreach (var r in copias)
            {
                int i = grilla.Rows.Add(Fecha(r.Fecha), NombreTipo(r.Tipo), TextoTamano(r.TamanoBytes), r.Usuario ?? string.Empty, r.Nota ?? string.Empty, string.Empty);
                grilla.Rows[i].Tag = r;
            }
            grilla.ResumeLayout();
            grilla.ClearSelection();
            bool hay = copias.Count > 0;
            grilla.Visible = hay;
            ficha.Visible = hay;
            vacio.Visible = !hay;
            CentrarVacio();
            if (hay)
            {
                int idx = idPrevio == null ? -1 : copias.ToList().FindIndex(c => string.Equals(c.Id, idPrevio, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) { grilla.Rows[idx].Selected = true; grilla.CurrentCell = grilla.Rows[idx].Cells[0]; }
            }
            MostrarFicha();
        }

        private RespaldoInfo Seleccionada()
        {
            if (grilla == null || grilla.SelectedRows.Count == 0) return null;
            return grilla.SelectedRows[0].Tag as RespaldoInfo;
        }

        // ================================================================== Detalle
        private void FilaFicha(string clave, string valor, Color? color = null, string ayuda = null, float tamano = 10f, bool elipsis = false)
        {
            var k = new Label { Text = clave, AutoSize = true, Font = Tema.Fuente(9.5f), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent, Margin = new Padding(0, 1, 4, 1), MaximumSize = new Size(84, 0) };
            var v = new Label { Text = valor, AutoSize = true, Font = Tema.Fuente(tamano), ForeColor = color ?? Tema.Texto, BackColor = Color.Transparent, Margin = new Padding(0, 1, 0, 1), MaximumSize = new Size(184, 0), UseCompatibleTextRendering = false };
            if (elipsis) { v.AutoSize = false; v.AutoEllipsis = true; v.Size = new Size(184, 20); }
            if (ayuda != null) tip.SetToolTip(v, ayuda);
            int fila = fichaTabla.RowCount++;
            fichaTabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fichaTabla.Controls.Add(k, 0, fila);
            fichaTabla.Controls.Add(v, 1, fila);
        }

        private static Panel CeldaResumen(string valor, string titulo)
        {
            var p = new Panel { Size = new Size(126, 38), Margin = new Padding(0, 0, 4, 0), BackColor = Color.Transparent };
            p.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Fill, AutoSize = false, Font = Tema.Fuente(8.5f), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent });
            p.Controls.Add(new Label { Text = valor, Dock = DockStyle.Top, Height = 21, AutoSize = false, Font = Tema.Fuente(12f, true), ForeColor = Tema.Texto, BackColor = Color.Transparent });
            return p;
        }

        private void MostrarFicha()
        {
            var r = Seleccionada();
            fichaTabla.SuspendLayout();
            foreach (Control c in fichaTabla.Controls.Cast<Control>().ToList()) { fichaTabla.Controls.Remove(c); c.Dispose(); }
            fichaTabla.RowStyles.Clear();
            fichaTabla.RowCount = 0;
            if (r == null)
            {
                fichaScroll.Controls.Remove(fichaTabla);
                if (!fichaScroll.Controls.Contains(lblFichaVacia)) fichaScroll.Controls.Add(lblFichaVacia);
                lblFichaVacia.BringToFront();
            }
            else
            {
                fichaScroll.Controls.Remove(lblFichaVacia);
                if (!fichaScroll.Controls.Contains(fichaTabla)) fichaScroll.Controls.Add(fichaTabla);
                FilaFicha("Archivo", r.NombreArchivo ?? r.Id, null, r.NombreArchivo ?? r.Id, 8.5f, true);
                FilaFicha("Creada", Fecha(r.Fecha) + (string.IsNullOrWhiteSpace(r.Usuario) ? string.Empty : " · " + r.Usuario));
                FilaFicha("Tamaño", TextoTamano(r.TamanoBytes));
                string sha = r.Sha256;
                FilaFicha("SHA-256", string.IsNullOrEmpty(sha) ? "—" : (sha.Length > 20 ? sha.Substring(0, 12) + "…" + sha.Substring(sha.Length - 8) : sha), null, sha);
                FilaFicha("Versiones", "App " + (string.IsNullOrWhiteSpace(r.VersionApp) ? "—" : r.VersionApp) + " · PG " + (string.IsNullOrWhiteSpace(r.VersionServidor) ? "—" : r.VersionServidor));
                if (!string.IsNullOrWhiteSpace(r.Nota)) FilaFicha("Nota", r.Nota);
                bool ok;
                if (verificados.TryGetValue(r.Id, out ok)) FilaFicha("Integridad", ok ? "✓ Verificada" : "✗ Falló la verificación", ok ? Tema.Acento : Rojo);
                else FilaFicha("Integridad", "— Sin verificar", Tema.TextoApagado);

                var sep = new Label { Text = "CONTENIDO AL HACER LA COPIA", AutoSize = true, Font = Tema.Fuente(8.5f, true), ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent, Margin = new Padding(0, 8, 0, 3) };
                int f = fichaTabla.RowCount++;
                fichaTabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                fichaTabla.Controls.Add(sep, 0, f);
                fichaTabla.SetColumnSpan(sep, 2);
                if (!r.TieneFicha || r.ResumenFilas == null || r.ResumenFilas.Count == 0)
                    FilaFicha("Resumen", r.TieneFicha ? "No disponible" : "Sin ficha: el archivo se agregó a mano, no hay datos del contenido.", Tema.TextoApagado);
                else
                {
                    var rejilla = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent, Margin = Padding.Empty };
                    rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
                    rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
                    string[,] nombres = { { "materiales", "Materiales" }, { "movimientos", "Movimientos" }, { "obras", "Obras" }, { "herramientas", "Herramientas" } };
                    for (int i = 0; i < nombres.GetLength(0); i++)
                    {
                        long n;
                        rejilla.Controls.Add(CeldaResumen(r.ResumenFilas.TryGetValue(nombres[i, 0], out n) ? n.ToString("N0", Ar) : "—", nombres[i, 1]), i % 2, i / 2);
                    }
                    int fr = fichaTabla.RowCount++;
                    fichaTabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    fichaTabla.Controls.Add(rejilla, 0, fr);
                    fichaTabla.SetColumnSpan(rejilla, 2);
                }
            }
            fichaTabla.ResumeLayout(true);
            ActualizarHabilitados();
        }

        private void ActualizarHabilitados()
        {
            bool haySel = Seleccionada() != null;
            bool herramientas = estadoActual != null && estadoActual.HerramientasDisponibles;
            bool base_ = estadoActual != null && estadoActual.BaseConectada;
            btnCrear.Enabled = !ocupado && estadoActual != null && herramientas && base_;
            btnVacioCrear.Enabled = btnCrear.Enabled;
            btnImportar.Enabled = !ocupado && estadoActual != null && herramientas;
            btnAbrirCarpeta.Enabled = !ocupado && estadoActual != null;
            btnVerificar.Enabled = !ocupado && haySel && herramientas;
            btnRestaurar.Enabled = !ocupado && haySel && herramientas && base_;
            btnEliminar.Enabled = !ocupado && haySel;
            mnuVerificar.Enabled = btnVerificar.Enabled;
            mnuRestaurar.Enabled = btnRestaurar.Enabled;
            mnuEliminar.Enabled = btnEliminar.Enabled;
            mnuCarpeta.Enabled = btnAbrirCarpeta.Enabled;
            bool cfg = !ocupado && estadoActual != null;
            txtCarpeta.Enabled = cfg; btnExaminar.Enabled = cfg; tglAutomatico.Enabled = cfg; btnGuardarConfig.Enabled = cfg;
            numHoras.Enabled = cfg && tglAutomatico.Checked;
            numConservar.Enabled = cfg && tglAutomatico.Checked;
            btnConfigToggle.Enabled = true;
        }

        // ================================================================== Avisos y progreso
        private enum TipoAviso { Exito, Advertencia, Error }

        private void MostrarAviso(TipoAviso tipo, string mensaje)
        {
            if (IsDisposed) return;
            var color = tipo == TipoAviso.Exito ? Tema.Acento : tipo == TipoAviso.Advertencia ? Tema.Advertencia : Rojo;
            string prefijo = tipo == TipoAviso.Exito ? "✓  " : tipo == TipoAviso.Advertencia ? "!  " : "✗  ";
            lblAviso.Text = prefijo + (mensaje ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            tip.SetToolTip(lblAviso, mensaje);
            lblAviso.ForeColor = Tema.Texto;
            var caja = (Panel)aviso.Tag;
            caja.Tag = color;
            caja.Invalidate();
            aviso.Visible = true;
        }

        private void IniciarProgreso(string etapa)
        {
            ocupado = true;
            aviso.Visible = false;
            lblEtapa.Text = etapa;
            lblPorcentaje.Text = string.Empty;
            barra.Indeterminada = true;
            btnCancelar.Enabled = true;
            progresoPanel.Visible = true;
            ActualizarHabilitados();
        }

        private void ActualizarProgreso(ProgresoRespaldo p)
        {
            if (p == null || IsDisposed) return;
            if (!string.IsNullOrWhiteSpace(p.Etapa)) lblEtapa.Text = p.Etapa;
            if (p.Porcentaje.HasValue)
            {
                int v = Math.Max(0, Math.Min(100, p.Porcentaje.Value));
                barra.Indeterminada = false; barra.Valor = v;
                lblPorcentaje.Text = v + " %";
            }
            else { barra.Indeterminada = true; lblPorcentaje.Text = string.Empty; }
        }

        private void TerminarProgreso()
        {
            ocupado = false;
            if (IsDisposed) return;
            progresoPanel.Visible = false;
            barra.Indeterminada = false;
            ActualizarHabilitados();
        }

        /// <summary>Ejecuta una operación larga mostrando el progreso. Devuelve null si falló o se canceló (ya informado en pantalla).</summary>
        private async Task<T> EjecutarAsync<T>(string etapaInicial, Func<IProgress<ProgresoRespaldo>, CancellationToken, Task<T>> operacionAsync) where T : class
        {
            if (ocupado || IsDisposed) return null;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(vida.Token);
            operacion = cts;
            IniciarProgreso(etapaInicial);
            try
            {
                var progreso = new Progress<ProgresoRespaldo>(ActualizarProgreso);
                return await operacionAsync(progreso, cts.Token);
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed && !vida.IsCancellationRequested) MostrarAviso(TipoAviso.Advertencia, "Operación cancelada.");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                if (!IsDisposed) MostrarAviso(TipoAviso.Error, "Las copias de seguridad solo están disponibles para administradores.");
                return null;
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MostrarAviso(TipoAviso.Error, "No se pudo completar la operación: " + ex.Message);
                return null;
            }
            finally
            {
                if (ReferenceEquals(operacion, cts)) operacion = null;
                cts.Dispose();
                TerminarProgreso();
            }
        }

        // ================================================================== Acciones
        /// <summary>Crea una copia manual (pide una nota opcional). Público para las pruebas.</summary>
        public async void CrearAsync()
        {
            if (ocupado) return;
            string nota = PedirTexto("Podés agregar una nota para reconocer esta copia más adelante (opcional).", "Crear copia de seguridad");
            if (nota == null) return;
            var r = await EjecutarAsync("Preparando la copia…", (prog, tok) => presenter.CrearAsync(nota, prog, tok));
            if (r == null || IsDisposed) return;
            if (r.Ok) MostrarAviso(TipoAviso.Exito, string.IsNullOrWhiteSpace(r.Detalle) ? "Copia de seguridad creada." : r.Detalle);
            else MostrarAviso(TipoAviso.Error, r.Detalle ?? "No se pudo crear la copia de seguridad.");
            CargarAsync();
        }

        private async void ImportarAsync()
        {
            if (ocupado) return;
            string ruta = ElegirArchivo();
            if (string.IsNullOrEmpty(ruta)) return;
            var r = await EjecutarAsync("Importando la copia…", (prog, tok) => presenter.ImportarAsync(ruta, tok));
            if (r == null || IsDisposed) return;
            if (r.Ok) MostrarAviso(TipoAviso.Exito, string.IsNullOrWhiteSpace(r.Detalle) ? "Copia importada." : r.Detalle);
            else MostrarAviso(TipoAviso.Error, r.Detalle ?? "No se pudo importar el archivo.");
            CargarAsync();
        }

        private async void VerificarSeleccionadaAsync()
        {
            var info = Seleccionada();
            if (info == null || ocupado) return;
            var r = await EjecutarAsync("Verificando la integridad de " + info.NombreArchivo + "…", (prog, tok) => presenter.VerificarAsync(info.Id, tok));
            if (r == null || IsDisposed) return;
            verificados[info.Id] = r.Ok;
            if (r.Ok) MostrarAviso(TipoAviso.Exito, "La copia está íntegra" + (r.Objetos > 0 ? " (" + r.Objetos.ToString("N0", Ar) + " objetos)" : string.Empty) + ".");
            else MostrarAviso(TipoAviso.Error, "La verificación falló: " + (r.Detalle ?? "el archivo no se pudo leer."));
            grilla.Invalidate();
            MostrarFicha();
        }

        private async void EliminarSeleccionadaAsync()
        {
            var info = Seleccionada();
            if (info == null || ocupado) return;
            if (!Confirmar("¿Eliminar la copia del " + Fecha(info.Fecha) + " (" + info.NombreArchivo + ")?\n\nEl archivo se borra de la carpeta de copias y no se puede recuperar.", "Eliminar copia de seguridad")) return;
            var r = await EjecutarAsync("Eliminando la copia…", (prog, tok) => presenter.EliminarAsync(info.Id, tok));
            if (r == null || IsDisposed) return;
            if (r.Ok) { verificados.Remove(info.Id); MostrarAviso(TipoAviso.Exito, "Copia eliminada."); }
            else MostrarAviso(TipoAviso.Error, r.Detalle ?? "No se pudo eliminar la copia.");
            CargarAsync();
        }

        /// <summary>Texto de la confirmación fuerte previa a restaurar.</summary>
        internal static string MensajeRestauracion(RespaldoInfo r)
        {
            return "Vas a restaurar la copia del " + Fecha(r.Fecha) + " (" + r.NombreArchivo + ").\n\n"
                + "• Antes se crea automáticamente una copia del estado actual, por si hay que volver atrás.\n"
                + "• Se reemplazan TODOS los datos de la base por los de esa copia: lo cargado después de esa fecha se pierde.\n"
                + "• Los demás usuarios deben cerrar la aplicación antes de continuar.\n"
                + "• Al terminar se cierra tu sesión para cargar los datos restaurados.\n\n"
                + "Para confirmar, escribí " + PalabraRestaurar + ":";
        }

        /// <summary>Pide la confirmación fuerte y, si se escribe RESTAURAR, restaura la copia y cierra la sesión.</summary>
        public async Task SolicitarRestauracionAsync(string id)
        {
            if (ocupado || IsDisposed) return;
            var info = estadoActual == null || estadoActual.Respaldos == null ? null
                : estadoActual.Respaldos.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
            if (info == null) { MostrarAviso(TipoAviso.Error, "La copia ya no existe en la carpeta de copias."); return; }

            string escrito = PedirTexto(MensajeRestauracion(info), "Restaurar copia de seguridad");
            if (escrito == null) return;
            if (!ConfirmacionValida(escrito))
            {
                MostrarAviso(TipoAviso.Advertencia, "No se escribió " + PalabraRestaurar + ": no se restauró nada y los datos siguen como estaban.");
                return;
            }

            var r = await EjecutarAsync("Restaurando la copia…", (prog, tok) => presenter.RestaurarAsync(info.Id, prog, tok));
            if (IsDisposed) return;
            if (r == null)
            {
                MostrarAviso(TipoAviso.Advertencia, "La restauración no se completó. La base quedó como estaba.");
                CargarAsync();
                return;
            }
            if (r.Ok)
            {
                string previa = r.RespaldoPrevio != null ? "\n\nAntes se guardó una copia del estado anterior: " + r.RespaldoPrevio.NombreArchivo + "." : string.Empty;
                Informar("Restauración completada", "La base se restauró desde la copia del " + Fecha(info.Fecha) + "." + previa
                    + "\n\nComo los datos cambiaron, se va a cerrar la sesión para volver a cargarlos.", false);
                CerrarSesion();
            }
            else
            {
                string detalle = string.IsNullOrWhiteSpace(r.Detalle) ? "No se pudo restaurar la copia." : r.Detalle;
                string aclaracion = detalle.IndexOf("quedó como estaba", StringComparison.OrdinalIgnoreCase) >= 0 ? string.Empty : "\n\nLa base de datos quedó como estaba: no se modificó nada.";
                Informar("No se pudo restaurar", detalle + aclaracion, true);
                MostrarAviso(TipoAviso.Error, "La restauración falló. La base quedó como estaba.");
                CargarAsync();
            }
        }

        private void AbrirCarpeta()
        {
            string carpeta = estadoActual != null && estadoActual.Configuracion != null ? estadoActual.Configuracion.Carpeta : null;
            try
            {
                if (string.IsNullOrWhiteSpace(carpeta)) { MostrarAviso(TipoAviso.Advertencia, "Todavía no hay una carpeta de copias configurada."); return; }
                if (!Directory.Exists(carpeta)) { MostrarAviso(TipoAviso.Advertencia, "La carpeta de copias todavía no existe: se crea al guardar la primera copia."); return; }
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + carpeta + "\"") { UseShellExecute = true });
            }
            catch (Exception ex) { MostrarAviso(TipoAviso.Error, "No se pudo abrir la carpeta: " + ex.Message); }
        }

        private void ExaminarCarpeta()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = "Elegí la carpeta donde se guardan las copias de seguridad";
                if (!string.IsNullOrWhiteSpace(txtCarpeta.Text) && Directory.Exists(txtCarpeta.Text)) d.SelectedPath = txtCarpeta.Text;
                if (d.ShowDialog(this) == DialogResult.OK) txtCarpeta.Text = d.SelectedPath;
            }
        }

        private async void GuardarConfigAsync()
        {
            if (ocupado || estadoActual == null) return;
            var cfg = (estadoActual.Configuracion ?? new ConfiguracionRespaldo()).Clonar();
            cfg.Carpeta = (txtCarpeta.Text ?? string.Empty).Trim();
            cfg.AutomaticoActivo = tglAutomatico.Checked;
            cfg.HorasEntreAutomaticos = (int)numHoras.Value;
            cfg.Conservar = (int)numConservar.Value;
            var r = await EjecutarAsync("Guardando la configuración…", (prog, tok) => presenter.GuardarConfiguracionAsync(cfg, tok));
            if (r == null || IsDisposed) return;
            if (r.Ok) { configSucia = false; MostrarAviso(TipoAviso.Exito, "Configuración guardada."); }
            else MostrarAviso(TipoAviso.Error, r.Detalle ?? "No se pudo guardar la configuración.");
            CargarAsync();
        }

        // ================================================================== Controles propios
        /// <summary>Tarjeta de estado: etiqueta, valor grande coloreado y detalle de hasta tres líneas, con barra de acento a la izquierda.</summary>
        private sealed class TarjetaEstado : Control
        {
            public const int Alto = 116;
            private readonly string etiqueta;
            private string valor = "—", detalle = string.Empty;
            private Color colorValor = Tema.Texto, colorAcento = Tema.Borde;
            private readonly ToolTip ayuda = new ToolTip();

            public TarjetaEstado(string etiqueta)
            {
                this.etiqueta = etiqueta;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Tema.Fondo;
                Height = Alto;
            }

            public void Establecer(string valor, string detalle, Color colorValor, Color colorAcento, string tooltip)
            {
                this.valor = valor ?? string.Empty; this.detalle = detalle ?? string.Empty; this.colorValor = colorValor; this.colorAcento = colorAcento;
                ayuda.SetToolTip(this, string.IsNullOrWhiteSpace(tooltip) ? null : tooltip);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent != null ? Parent.BackColor : Tema.Fondo);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
                using (var p = Tema.RectRedondeado(r, 6))
                {
                    using (var br = new SolidBrush(Tema.Superficie)) g.FillPath(br, p);
                    using (var pen = new Pen(Tema.Borde, 1f)) g.DrawPath(pen, p);
                }
                using (var br = new SolidBrush(colorAcento)) g.FillRectangle(br, 14, 16, 3, Height - 32);

                const TextFormatFlags una = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                int x = 30, w = Math.Max(20, Width - x - 14);
                TextRenderer.DrawText(g, etiqueta.ToUpper(Ar), Tema.Fuente(9f, true), new Rectangle(x, 14, w, 16), Tema.TextoSecundario, una);
                TextRenderer.DrawText(g, valor, Tema.Fuente(17f, true), new Rectangle(x, 32, w, 30), colorValor, una);
                var fd = Tema.Fuente(9.5f);
                int lh = TextRenderer.MeasureText(g, "Ag", fd, new Size(400, 40), TextFormatFlags.NoPadding).Height;
                int lineas = Math.Max(1, (Height - 62 - 6) / lh);
                TextRenderer.DrawText(g, detalle, fd, new Rectangle(x, 62, w, lineas * lh), Tema.TextoSecundario,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) ayuda.Dispose();
                base.Dispose(disposing);
            }
        }

        /// <summary>Barra de progreso del tema; sin porcentaje conocido muestra un tramo que va y viene.</summary>
        private sealed class BarraProgreso : Control
        {
            private int valor;
            private bool indeterminada = true;
            private float fase;
            private readonly System.Windows.Forms.Timer reloj = new System.Windows.Forms.Timer { Interval = 30 };

            public BarraProgreso()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Height = 10;
                reloj.Tick += (s, e) => { fase = (fase + 0.018f) % 1f; if (indeterminada && Visible) Invalidate(); };
            }

            public int Valor { get { return valor; } set { valor = value; Invalidate(); } }
            public bool Indeterminada
            {
                get { return indeterminada; }
                set { indeterminada = value; if (value) reloj.Start(); else reloj.Stop(); Invalidate(); }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent != null ? Parent.BackColor : Tema.Superficie);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int h = 8, y = (Height - h) / 2;
                var pista = new RectangleF(0, y, Width - 1, h);
                using (var p = Tema.RectRedondeado(pista, h / 2f)) using (var br = new SolidBrush(Tema.FondoInput)) g.FillPath(br, p);
                RectangleF relleno;
                if (indeterminada)
                {
                    float ancho = Width * 0.28f;
                    float t = fase < 0.5f ? fase * 2 : (1 - fase) * 2;
                    relleno = new RectangleF((Width - ancho) * t, y, ancho, h);
                }
                else relleno = new RectangleF(0, y, Math.Max(h, (Width - 1) * valor / 100f), h);
                using (var p = Tema.RectRedondeado(relleno, h / 2f)) using (var br = new SolidBrush(Tema.Acento)) g.FillPath(br, p);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) reloj.Dispose();
                base.Dispose(disposing);
            }
        }

        /// <summary>Menú contextual con los colores del tema.</summary>
        private sealed class RenderizadorMenu : ToolStripProfessionalRenderer
        {
            public RenderizadorMenu() : base(new TablaColoresMenu()) { RoundedEdges = false; }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? (e.Item.Selected ? Color.White : Tema.Texto) : Tema.DeshabilitadoTexto;
                base.OnRenderItemText(e);
            }
        }

        private sealed class TablaColoresMenu : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Tema.FondoInput; } }
            public override Color MenuBorder { get { return Tema.Borde; } }
            public override Color MenuItemBorder { get { return Tema.Seleccion; } }
            public override Color MenuItemSelected { get { return Tema.Seleccion; } }
            public override Color ImageMarginGradientBegin { get { return Tema.FondoInput; } }
            public override Color ImageMarginGradientMiddle { get { return Tema.FondoInput; } }
            public override Color ImageMarginGradientEnd { get { return Tema.FondoInput; } }
            public override Color SeparatorDark { get { return Tema.Borde; } }
            public override Color SeparatorLight { get { return Tema.Borde; } }
        }
    }
}
