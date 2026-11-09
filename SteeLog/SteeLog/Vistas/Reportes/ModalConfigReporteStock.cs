using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;
using SteeLog.Core.Application.Reportes;
using System.Threading;
using SteeLog.Modelos;
using SteeLog.UI;

namespace SteeLog.Vistas.Reportes
{
    public class ModalConfigReporteStock : Form
    {
        private readonly ReporteStockPresenter presenter;
        private Func<object> identidadActual = () => LocalServices.Current.Session.Current;
        private Action<string> avisarError = mensaje => ToastNotification.ShowError(mensaje);
        private Action<string> avisarExito = mensaje => ToastNotification.ShowSuccess(mensaje);
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private bool cargado;
        private object identidadDatos;
        private bool guardando;
        private CancellationTokenSource carga;
        private int version;
        private void CancelarOperaciones()
        {
            if (!vida.IsCancellationRequested)
            {
                try { vida.Cancel(); } catch (ObjectDisposedException) { }
            }
            try { carga?.Cancel(); } catch (ObjectDisposedException) { }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelarOperaciones();
                try { vida.Dispose(); } catch (ObjectDisposedException) { }
                try { carga?.Dispose(); } catch (ObjectDisposedException) { }
            }
            base.Dispose(disposing);
        }

        private readonly ListBox lstSecciones = new ListBox();
        private readonly TextBox txtNombreSeccion = new TextBox();
        private readonly TextBox txtBuscarMaterial = new TextBox();
        private readonly ListBox lstResultadosMateriales = new ListBox();
        private readonly ListBox lstMaterialesSeccion = new ListBox();

        private readonly List<SeccionEditable> secciones = new List<SeccionEditable>();
        private readonly List<int> seccionesEliminadas = new List<int>();
        private List<Material> materialesDisponibles = new List<Material>();

        private bool actualizandoUI;
        private Button btnNuevaSeccion;
        private Button btnEliminarSeccion;
        private Button btnSubirSeccion;
        private Button btnBajarSeccion;
        private Button btnAgregar;
        private Button btnQuitar;
        private Button btnSubirMaterial;
        private Button btnBajarMaterial;
        private Button btnGuardar;

        private void ActualizarEstadoEdicion()
        {
            bool habilitado = cargado && !guardando;
            if (btnNuevaSeccion != null) btnNuevaSeccion.Enabled = habilitado;
            if (btnEliminarSeccion != null) btnEliminarSeccion.Enabled = habilitado;
            if (btnSubirSeccion != null) btnSubirSeccion.Enabled = habilitado;
            if (btnBajarSeccion != null) btnBajarSeccion.Enabled = habilitado;
            if (txtNombreSeccion != null) txtNombreSeccion.Enabled = habilitado;
            if (lstSecciones != null) lstSecciones.Enabled = habilitado;
            if (txtBuscarMaterial != null) txtBuscarMaterial.Enabled = habilitado;
            if (btnAgregar != null) btnAgregar.Enabled = habilitado;
            if (lstResultadosMateriales != null) lstResultadosMateriales.Enabled = habilitado;
            if (btnQuitar != null) btnQuitar.Enabled = habilitado;
            if (btnSubirMaterial != null) btnSubirMaterial.Enabled = habilitado;
            if (btnBajarMaterial != null) btnBajarMaterial.Enabled = habilitado;
            if (lstMaterialesSeccion != null) lstMaterialesSeccion.Enabled = habilitado;
            if (btnGuardar != null) btnGuardar.Enabled = habilitado;
        }

        public ModalConfigReporteStock(ReporteStockPresenter presenter)
        {
            this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            Text = "Configurar secciones del reporte de stock";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 660);
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            Font = Tema.FuenteTexto;

            CrearInterfaz();
            Load += (s, e) => CargarDatos();
            FormClosed += (s, e) => CancelarOperaciones();
            Disposed += (s, e) => CancelarOperaciones();
        }

        internal ModalConfigReporteStock(ReporteStockPresenter presenter, Func<object> identidadActual, Action<string> avisarError, Action<string> avisarExito) : this(presenter)
        {
            this.identidadActual = identidadActual ?? throw new ArgumentNullException(nameof(identidadActual));
            this.avisarError = avisarError ?? throw new ArgumentNullException(nameof(avisarError));
            this.avisarExito = avisarExito ?? throw new ArgumentNullException(nameof(avisarExito));
        }

        private void CrearInterfaz()
        {
            var panelPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Tema.Fondo,
                Padding = new Padding(24, 20, 24, 20)
            };
            panelPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            panelPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            panelPrincipal.Controls.Add(CrearPanelSecciones(), 0, 0);
            panelPrincipal.Controls.Add(CrearPanelMateriales(), 1, 0);

            Controls.Add(panelPrincipal);
            EstilizarLista(lstSecciones);
            EstilizarLista(lstResultadosMateriales);
            EstilizarLista(lstMaterialesSeccion);
            ActualizarEstadoEdicion();
            Tema.ChromeModal(this, "Configurar secciones del reporte de stock");
            panelPrincipal.BringToFront();
        }

        private Control CrearPanelSecciones()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 12, 0)
            };

            var lblTitulo = CrearTitulo("SECCIONES");
            lblTitulo.Dock = DockStyle.Top;

            var lblAyuda = new Label
            {
                Dock = DockStyle.Top,
                Height = 54,
                ForeColor = Tema.TextoApagado,
                Text = "Cada sección se muestra como cabecera en el reporte. Podés ordenarlas con subir y bajar.",
                Padding = new Padding(0, 4, 0, 6)
            };

            lstSecciones.Dock = DockStyle.Fill;
            lstSecciones.SelectedIndexChanged += LstSecciones_SelectedIndexChanged;

            var pnlEditor = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 220,
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Tema.Superficie,
                Padding = new Padding(12)
            };
            pnlEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblNombre = new Label
            {
                Text = "Nombre de la sección",
                Dock = DockStyle.Fill,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlEditor.Controls.Add(lblNombre, 0, 0);
            pnlEditor.SetColumnSpan(lblNombre, 2);

            txtNombreSeccion.TextChanged += TxtNombreSeccion_TextChanged;
            var marcoNombre = EnvolverInput(txtNombreSeccion);
            pnlEditor.Controls.Add(marcoNombre, 0, 1);
            pnlEditor.SetColumnSpan(marcoNombre, 2);

            btnNuevaSeccion = CrearBoton("Nueva sección", (s, e) => NuevaSeccion());
            pnlEditor.Controls.Add(btnNuevaSeccion, 0, 2);
            btnEliminarSeccion = CrearBoton("Eliminar sección", (s, e) => EliminarSeccionActual(), Variante.Peligro);
            pnlEditor.Controls.Add(btnEliminarSeccion, 1, 2);
            btnSubirSeccion = CrearBoton("Subir sección", (s, e) => MoverSeccion(-1));
            pnlEditor.Controls.Add(btnSubirSeccion, 0, 3);
            btnBajarSeccion = CrearBoton("Bajar sección", (s, e) => MoverSeccion(1));
            pnlEditor.Controls.Add(btnBajarSeccion, 1, 3);

            panel.Controls.Add(lstSecciones);
            panel.Controls.Add(pnlEditor);
            panel.Controls.Add(lblAyuda);
            panel.Controls.Add(lblTitulo);

            return panel;
        }

        private Control CrearPanelMateriales()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var lblTitulo = CrearTitulo("MATERIALES DE LA SECCIÓN");
            lblTitulo.Dock = DockStyle.Top;

            var lblAyuda = new Label
            {
                Dock = DockStyle.Top,
                Height = 54,
                ForeColor = Tema.TextoApagado,
                Text = "Buscá materiales, agregalos como carrito y ordenalos con subir y bajar para definir cómo se muestran.",
                Padding = new Padding(0, 4, 0, 6)
            };

            var panelContenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4
            };
            panelContenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43F));
            panelContenido.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            panelContenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57F));
            panelContenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            panelContenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            panelContenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panelContenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            var lblBuscar = new Label
            {
                Text = "Buscar material",
                Dock = DockStyle.Fill,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.BottomLeft
            };
            panelContenido.Controls.Add(lblBuscar, 0, 0);

            var lblSeleccionados = new Label
            {
                Text = "Materiales agregados",
                Dock = DockStyle.Fill,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.BottomLeft
            };
            panelContenido.Controls.Add(lblSeleccionados, 2, 0);

            txtBuscarMaterial.TextChanged += TxtBuscarMaterial_TextChanged;
            txtBuscarMaterial.KeyDown += TxtBuscarMaterial_KeyDown;
            panelContenido.Controls.Add(EnvolverInput(txtBuscarMaterial), 0, 1);

            btnAgregar = CrearBoton("Agregar →", (s, e) => AgregarMaterialSeleccionado());
            btnAgregar.Dock = DockStyle.Fill;
            panelContenido.Controls.Add(btnAgregar, 1, 1);

            lstResultadosMateriales.Dock = DockStyle.Fill;
            lstResultadosMateriales.DoubleClick += (s, e) => AgregarMaterialSeleccionado();
            panelContenido.Controls.Add(lstResultadosMateriales, 0, 2);

            var panelBotonesMateriales = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0, 8, 0, 8)
            };
            panelBotonesMateriales.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            panelBotonesMateriales.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            panelBotonesMateriales.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            panelBotonesMateriales.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            btnQuitar = CrearBoton("Quitar", (s, e) => QuitarMaterialSeleccionado(), Variante.Peligro);
            panelBotonesMateriales.Controls.Add(btnQuitar, 0, 0);
            btnSubirMaterial = CrearBoton("Subir", (s, e) => MoverMaterial(-1));
            panelBotonesMateriales.Controls.Add(btnSubirMaterial, 0, 1);
            btnBajarMaterial = CrearBoton("Bajar", (s, e) => MoverMaterial(1));
            panelBotonesMateriales.Controls.Add(btnBajarMaterial, 0, 2);
            panelContenido.Controls.Add(panelBotonesMateriales, 1, 2);

            lstMaterialesSeccion.Dock = DockStyle.Fill;
            panelContenido.Controls.Add(lstMaterialesSeccion, 2, 2);

            btnGuardar = CrearBoton("Guardar cambios", (s, e) => GuardarTodo(), Variante.Primario);
            btnGuardar.Dock = DockStyle.Fill;
            panelContenido.Controls.Add(btnGuardar, 2, 3);

            panel.Controls.Add(panelContenido);
            panel.Controls.Add(lblAyuda);
            panel.Controls.Add(lblTitulo);

            return panel;
        }

        private Label CrearTitulo(string texto)
        {
            return new Label
            {
                Text = texto,
                Height = 32,
                Font = Tema.FuenteSubtitulo,
                ForeColor = Tema.TextoSecundario,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button CrearBoton(string texto, EventHandler click, Variante estilo = Variante.Secundario)
        {
            var boton = new BotonTema
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Estilo = estilo,
                Margin = new Padding(3)
            };
            boton.Click += click;
            return boton;
        }

        /// <summary>Botón nativo (se conserva el tipo Button) pintado con los tokens de Tema.</summary>
        private sealed class BotonTema : Button
        {
            private bool hover, presionado;
            public Variante Estilo = Variante.Secundario;

            public BotonTema()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                FlatStyle = FlatStyle.Flat;
                Cursor = Cursors.Hand;
                Font = Tema.FuenteBoton;
                TabStop = true;
            }
            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; presionado = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { presionado = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { presionado = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent != null && Parent.BackColor.A == 255 ? Parent.BackColor : Tema.Fondo);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color fill, texto = Color.White;
                switch (Estilo)
                {
                    case Variante.Primario: fill = presionado ? Tema.AcentoPressed : hover ? Tema.AcentoHover : Tema.Acento; break;
                    case Variante.Peligro: fill = hover ? Tema.PeligroHover : Tema.Peligro; break;
                    default: fill = hover ? Tema.SecundarioHover : Tema.Secundario; texto = Tema.Texto; break;
                }
                if (!Enabled) { fill = Tema.DeshabilitadoFill; texto = Tema.DeshabilitadoTexto; }
                var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
                using (var p = Tema.RectRedondeado(r, Tema.Radio))
                using (var br = new SolidBrush(fill))
                    g.FillPath(br, p);
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, texto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        /// <summary>TextBox nativo dentro de un contenedor con borde 1px y relleno de input.</summary>
        private static Control EnvolverInput(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = Tema.FondoInput;
            txt.ForeColor = Tema.Texto;
            txt.Font = Tema.FuenteTexto;
            txt.Dock = DockStyle.None;
            txt.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            var interior = new Panel { Dock = DockStyle.Fill, BackColor = Tema.FondoInput, Padding = new Padding(10, 0, 10, 0) };
            var marco = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Borde, Padding = new Padding(1), Margin = new Padding(3) };
            interior.Controls.Add(txt);
            marco.Controls.Add(interior);
            interior.Resize += (s, e) => { txt.Width = Math.Max(10, interior.ClientSize.Width - 20); txt.Left = 10; txt.Top = Math.Max(0, (interior.ClientSize.Height - txt.Height) / 2); };
            txt.Enter += (s, e) => marco.BackColor = Tema.BordeFoco;
            txt.Leave += (s, e) => marco.BackColor = Tema.Borde;
            return marco;
        }

        private static void EstilizarLista(ListBox lst)
        {
            lst.BorderStyle = BorderStyle.None;
            lst.BackColor = Tema.FondoInput;
            lst.ForeColor = Tema.Texto;
            lst.Font = Tema.FuenteTexto;
            lst.IntegralHeight = false;
            lst.DrawMode = DrawMode.OwnerDrawFixed;
            lst.ItemHeight = 30;
            lst.DrawItem += (s, e) =>
            {
                var l = (ListBox)s;
                bool sel = (e.State & DrawItemState.Selected) != 0;
                using (var br = new SolidBrush(sel ? Tema.Seleccion : Tema.FondoInput)) e.Graphics.FillRectangle(br, e.Bounds);
                if (e.Index < 0 || e.Index >= l.Items.Count) return;
                var rect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, Convert.ToString(l.Items[e.Index]), l.Font, rect, sel ? Color.White : Tema.Texto,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            };
        }

        private async void CargarDatos(int? seleccionarSeccionId = null)
        {
            if (vida.IsCancellationRequested || IsDisposed) return;
            cargado = false;
            identidadDatos = null;

            ActualizarEstadoEdicion();

            carga?.Cancel();
            var anterior = carga;
            carga = CancellationTokenSource.CreateLinkedTokenSource(vida.Token);
            anterior?.Dispose();
            var token = carga.Token;
            int solicitud = ++version;

            var identidad = identidadActual();
            try
            {
                var lista = await presenter.MaterialesAsync(token);
                if (token.IsCancellationRequested || vida.IsCancellationRequested || IsDisposed || solicitud != version || !ReferenceEquals(identidad, identidadActual())) return;
                var configuracion = await presenter.SeccionesAsync(token);
                if (token.IsCancellationRequested || vida.IsCancellationRequested || IsDisposed || solicitud != version || !ReferenceEquals(identidad, identidadActual())) return;

                actualizandoUI = true;
                materialesDisponibles = lista.Select(m => new Material { id = m.Id, nombre = m.Nombre, nombre_categoria = m.Categoria }).OrderBy(m => m.nombre_categoria).ThenBy(m => m.nombre).ToList();
                secciones.Clear(); seccionesEliminadas.Clear();
                foreach (var seccion in configuracion)
                    secciones.Add(new SeccionEditable { id = seccion.Id, nombre = seccion.Nombre, Materiales = seccion.Materiales.Select(CrearMaterialSeleccionado).Where(m => m != null).ToList() });
                RefrescarSecciones(seleccionarSeccionId);
                identidadDatos = identidad;
                cargado = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested && !vida.IsCancellationRequested && !IsDisposed && solicitud == version && ReferenceEquals(identidad, identidadActual())) avisarError("No se pudo cargar la configuración de secciones del reporte de stock. " + ex.Message); }
            finally
            {
                if (!IsDisposed && !vida.IsCancellationRequested && solicitud == version && ReferenceEquals(identidad, identidadActual()))
                {
                    actualizandoUI = false;
                    ActualizarEstadoEdicion();
                }
            }
        }

        private void RefrescarSecciones(int? seleccionarSeccionId = null)
        {
            var seccionActual = seleccionarSeccionId.HasValue
                ? secciones.FirstOrDefault(s => s.id == seleccionarSeccionId.Value)
                : SeccionActual;

            for (int i = 0; i < secciones.Count; i++)
            {
                secciones[i].DisplayOrder = i + 1;
            }

            actualizandoUI = true;
            lstSecciones.BeginUpdate();
            lstSecciones.Items.Clear();
            foreach (var seccion in secciones)
            {
                lstSecciones.Items.Add(seccion);
            }
            lstSecciones.EndUpdate();

            if (secciones.Count == 0)
            {
                txtNombreSeccion.Clear();
                lstMaterialesSeccion.Items.Clear();
                lstResultadosMateriales.Items.Clear();
            }
            else
            {
                var seccionASeleccionar = seccionActual != null && secciones.Contains(seccionActual)
                    ? seccionActual
                    : secciones[0];

                lstSecciones.SelectedItem = seccionASeleccionar;
                CargarSeccionEnPantalla(seccionASeleccionar);
            }

            actualizandoUI = false;
        }

        private void CargarSeccionEnPantalla(SeccionEditable seccion)
        {
            if (seccion == null)
            {
                txtNombreSeccion.Clear();
                lstMaterialesSeccion.Items.Clear();
                lstResultadosMateriales.Items.Clear();
                return;
            }

            txtNombreSeccion.Text = seccion.nombre ?? string.Empty;
            RefrescarMaterialesSeccion(seccion);
            RefrescarResultadosBusqueda();
        }

        private void RefrescarMaterialesSeccion(SeccionEditable seccion)
        {
            if (seccion == null)
            {
                lstMaterialesSeccion.Items.Clear();
                return;
            }

            for (int i = 0; i < seccion.Materiales.Count; i++)
            {
                seccion.Materiales[i].DisplayOrder = i + 1;
            }

            actualizandoUI = true;
            var seleccionado = lstMaterialesSeccion.SelectedItem as MaterialSeleccionado;
            lstMaterialesSeccion.BeginUpdate();
            lstMaterialesSeccion.Items.Clear();
            foreach (var material in seccion.Materiales)
            {
                lstMaterialesSeccion.Items.Add(material);
            }
            lstMaterialesSeccion.EndUpdate();

            if (seleccionado != null)
            {
                var materialActual = seccion.Materiales.FirstOrDefault(m => m.idMaterial == seleccionado.idMaterial);
                if (materialActual != null)
                {
                    lstMaterialesSeccion.SelectedItem = materialActual;
                }
            }

            actualizandoUI = false;
        }

        private void RefrescarResultadosBusqueda()
        {
            var seccion = SeccionActual;
            if (!cargado || guardando)
            {
                lstResultadosMateriales.Items.Clear();
                return;
            }
            var texto = txtBuscarMaterial.Text.Trim();
            var busquedaNormalizada = NormalizarTexto(texto);

            lstResultadosMateriales.BeginUpdate();
            lstResultadosMateriales.Items.Clear();

            if (seccion != null && !string.IsNullOrWhiteSpace(busquedaNormalizada))
            {
                var resultados = materialesDisponibles
                    .Where(m => CoincideBusquedaFlexible(m.nombre, m.nombre_categoria, busquedaNormalizada))
                    .Where(m => !seccion.Materiales.Any(sm => sm.idMaterial == m.id))
                    .Where(m => !MaterialAsignadoEnOtraSeccion(m.id, seccion))
                    .Take(30)
                    .Select(m => new MaterialBusqueda(m))
                    .ToList();

                foreach (var resultado in resultados)
                {
                    lstResultadosMateriales.Items.Add(resultado);
                }
            }

            lstResultadosMateriales.EndUpdate();
        }

        private void NuevaSeccion()
        {
            if (!cargado || guardando) return;
            var nueva = new SeccionEditable
            {
                nombre = string.Empty
            };

            secciones.Add(nueva);
            RefrescarSecciones();
            lstSecciones.SelectedItem = nueva;
            txtNombreSeccion.Focus();
            txtNombreSeccion.SelectAll();
        }

        private void EliminarSeccionActual()
        {
            var seccion = SeccionActual;
            if (seccion == null)
            {
                return;
            }

            if (!cargado || guardando) return;
            if (!Dialogo.Confirmar("¿Eliminar la sección y sus materiales asignados?", "Eliminar sección", true, this))
            {
                return;
            }

            if (seccion.id != 0)
            {
                seccionesEliminadas.Add(seccion.id);
            }

            secciones.Remove(seccion);
            RefrescarSecciones();
            ToastNotification.ShowSuccess("Sección quitada del armado actual. Guardá cambios para aplicar el borrado.");
        }

        private void MoverSeccion(int direccion)
        {
            var seccion = SeccionActual;
            if (seccion == null)
            {
                return;
            }

            if (!cargado || guardando) return;
            int indiceActual = secciones.IndexOf(seccion);
            int indiceNuevo = indiceActual + direccion;
            if (indiceActual < 0 || indiceNuevo < 0 || indiceNuevo >= secciones.Count)
            {
                return;
            }

            secciones.RemoveAt(indiceActual);
            secciones.Insert(indiceNuevo, seccion);
            RefrescarSecciones(seccion.id == 0 ? (int?)null : seccion.id);
            lstSecciones.SelectedItem = seccion;
        }

        private void AgregarMaterialSeleccionado()
        {
            var seccion = SeccionActual;
            if (!cargado || guardando) return;
            var material = lstResultadosMateriales.SelectedItem as MaterialBusqueda;

            if (seccion == null)
            {
                ToastNotification.ShowWarning("Seleccioná una sección primero.");
                return;
            }

            if (material == null)
            {
                return;
            }

            if (seccion.Materiales.Any(m => m.idMaterial == material.idMaterial))
            {
                return;
            }

            seccion.Materiales.Add(new MaterialSeleccionado
            {
                idMaterial = material.idMaterial,
                nombre = material.nombre,
                categoria = material.categoria
            });

            RefrescarMaterialesSeccion(seccion);
            RefrescarResultadosBusqueda();
            txtBuscarMaterial.Clear();
            txtBuscarMaterial.Focus();
        }

        private void QuitarMaterialSeleccionado()
        {
            var seccion = SeccionActual;
            var material = lstMaterialesSeccion.SelectedItem as MaterialSeleccionado;

            if (seccion == null || material == null)
            {
                return;
            }

            if (!cargado || guardando) return;
            seccion.Materiales.Remove(material);
            RefrescarMaterialesSeccion(seccion);
            RefrescarResultadosBusqueda();
        }

        private void MoverMaterial(int direccion)
        {
            var seccion = SeccionActual;
            var material = lstMaterialesSeccion.SelectedItem as MaterialSeleccionado;

            if (seccion == null || material == null)
            {
                return;
            }

            if (!cargado || guardando) return;
            int indiceActual = seccion.Materiales.IndexOf(material);
            int indiceNuevo = indiceActual + direccion;
            if (indiceActual < 0 || indiceNuevo < 0 || indiceNuevo >= seccion.Materiales.Count)
            {
                return;
            }

            seccion.Materiales.RemoveAt(indiceActual);
            seccion.Materiales.Insert(indiceNuevo, material);
            RefrescarMaterialesSeccion(seccion);
            lstMaterialesSeccion.SelectedItem = material;
        }

        private async void GuardarTodo()
        {
            if (!cargado || guardando || vida.IsCancellationRequested || !ReferenceEquals(identidadDatos, identidadActual())) return;
            var identidad = identidadActual();
            int solicitud = version;
            try
            {
                ValidarConfiguracion();
                var snapshot = secciones.Select(s => new SteeLog.Core.Application.Reportes.ReporteStockSeccion { Id = s.id, Nombre = s.nombre, Materiales = s.Materiales.Select(m => m.idMaterial).ToList() }).ToArray();
                var eliminadas = seccionesEliminadas.ToArray();

                guardando = true;
                Enabled = false;
                ActualizarEstadoEdicion();
                await presenter.GuardarAsync(snapshot, eliminadas, vida.Token);
                if (vida.IsCancellationRequested || IsDisposed || solicitud != version || !ReferenceEquals(identidad, identidadActual())) return;
                avisarExito("Configuración de secciones guardada.");
                guardando = false;
                Enabled = true;
                CargarDatos();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!vida.IsCancellationRequested && !IsDisposed && solicitud == version && ReferenceEquals(identidad, identidadActual())) avisarError("No se pudo guardar la configuración del reporte: " + ex.Message); }
            finally
            {
                guardando = false;
                if (!IsDisposed && !vida.IsCancellationRequested && (solicitud == version || ReferenceEquals(identidadDatos, identidad)) && ReferenceEquals(identidad, identidadActual()))
                {
                    Enabled = true;
                    ActualizarEstadoEdicion();
                }
            }
        }

        private void ValidarConfiguracion()
        {
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var seccion in secciones)
            {
                string nombre = (seccion.nombre ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new InvalidOperationException("Todas las secciones deben tener nombre.");
                }

                if (!nombres.Add(nombre))
                {
                    throw new InvalidOperationException("No puede haber dos secciones con el mismo nombre.");
                }
            }
        }

        private void LstSecciones_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (actualizandoUI || !cargado || guardando)
            {
                return;
            }

            CargarSeccionEnPantalla(SeccionActual);
        }

        private void TxtNombreSeccion_TextChanged(object sender, EventArgs e)
        {
            if (actualizandoUI || !cargado || guardando)
            {
                return;
            }

            var seccion = SeccionActual;
            if (seccion == null)
            {
                return;
            }

            seccion.nombre = txtNombreSeccion.Text;
            lstSecciones.Invalidate();
        }

        private void TxtBuscarMaterial_TextChanged(object sender, EventArgs e)
        {
            RefrescarResultadosBusqueda();
        }

        private void TxtBuscarMaterial_KeyDown(object sender, KeyEventArgs e)
        {
            if (!cargado || guardando) return;
            if (e.KeyCode == Keys.Down && lstResultadosMateriales.Items.Count > 0)
            {
                lstResultadosMateriales.SelectedIndex = 0;
                lstResultadosMateriales.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (lstResultadosMateriales.SelectedIndex == -1 && lstResultadosMateriales.Items.Count > 0)
                {
                    lstResultadosMateriales.SelectedIndex = 0;
                }

                AgregarMaterialSeleccionado();
                e.SuppressKeyPress = true;
            }
        }

        private MaterialSeleccionado CrearMaterialSeleccionado(int idMaterial)
        {
            var material = materialesDisponibles.FirstOrDefault(m => m.id == idMaterial);
            if (material == null)
            {
                return null;
            }

            return new MaterialSeleccionado
            {
                idMaterial = material.id,
                nombre = material.nombre,
                categoria = material.nombre_categoria
            };
        }

        private bool MaterialAsignadoEnOtraSeccion(int idMaterial, SeccionEditable seccionActual)
        {
            return secciones
                .Where(s => s != seccionActual)
                .Any(s => s.Materiales.Any(m => m.idMaterial == idMaterial));
        }

        private static bool CoincideBusquedaFlexible(string nombre, string categoria, string busquedaNormalizada)
        {
            if (string.IsNullOrWhiteSpace(busquedaNormalizada))
            {
                return false;
            }

            string texto = NormalizarTexto((nombre ?? string.Empty) + " " + (categoria ?? string.Empty));
            return texto.Contains(busquedaNormalizada);
        }

        private static string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            string normalizado = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalizado.Length);

            foreach (char caracter in normalizado)
            {
                var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(caracter);
                if (categoria == System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(caracter))
                {
                    builder.Append(caracter);
                }
            }

            return builder.ToString();
        }

        private SeccionEditable SeccionActual
        {
            get { return lstSecciones.SelectedItem as SeccionEditable; }
        }

        private sealed class SeccionEditable
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public int DisplayOrder { get; set; }
            public List<MaterialSeleccionado> Materiales { get; set; } = new List<MaterialSeleccionado>();

            public override string ToString()
            {
                return DisplayOrder + ". " + (string.IsNullOrWhiteSpace(nombre) ? "(sin nombre)" : nombre.Trim());
            }
        }

        private sealed class MaterialSeleccionado
        {
            public int idMaterial { get; set; }
            public string nombre { get; set; }
            public string categoria { get; set; }
            public int DisplayOrder { get; set; }

            public override string ToString()
            {
                return DisplayOrder + ". " + nombre + "  [" + categoria + "]";
            }
        }

        private sealed class MaterialBusqueda
        {
            public MaterialBusqueda(Material material)
            {
                idMaterial = material.id;
                nombre = material.nombre;
                categoria = material.nombre_categoria;
            }

            public int idMaterial { get; private set; }
            public string nombre { get; private set; }
            public string categoria { get; private set; }

            public override string ToString()
            {
                return nombre + "  [" + categoria + "]";
            }
        }
    }
}
