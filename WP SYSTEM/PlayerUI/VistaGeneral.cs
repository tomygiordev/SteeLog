using Guna.UI2.WinForms; 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices; 
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Servicios;
using WP_Inventario.Vistas;
using WP_Inventario.Vistas.Movimientos;
using WP_Inventario.Vistas.Movimientos.Ordenes;
using WP_Inventario.Vistas.Reportes;
using WP_Inventario.Vistas.Inventario;

namespace WP_Inventario
{
    public partial class VistaGeneral : Form
    {
        public VistaGeneral()
        {
            InitializeComponent();
            this.Text = "SteeLog - Sistema de Gestión de Inventario";
            hideSubMenu();

            var btnClose = CrearBoton("Cerrar", Properties.Resources.ic_close, (s, e) => this.Close());

            var btnMax = CrearBoton("Maximizar/Restaurar", Properties.Resources.ic_max, (s, e) =>
            {
                if (this.WindowState == FormWindowState.Normal)
                    this.WindowState = FormWindowState.Maximized;
                else
                    this.WindowState = FormWindowState.Normal;
            });

            var btnMin = CrearBoton("Minimizar", Properties.Resources.ic_min, (s, e) =>
            {
                this.WindowState = FormWindowState.Minimized;
            });

            panelMaximizar.Controls.Add(btnMin);
            panelMaximizar.Controls.Add(btnMax);
            panelMaximizar.Controls.Add(btnClose);    
            this.BackColor = Color.FromArgb(11, 7, 17);
            var designSize = this.ClientSize;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Size = designSize;
            this.Padding = new Padding(1);
            this.DoubleBuffered = true; // mejora el parpadeo al redimensionar
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // Ajuste de fuentes para la barra lateral (Padres > Hijos)
            // La fuente base ya fue incrementada en el Designer (aprox 11.75F), que afectará a los hijos.
            // Aquí incrementamos explícitamente los padres a 14F.
            float parentFontSize = 14f;
            try
            {
                btnInventario.Font = new Font(this.Font.FontFamily, parentFontSize);
                btnGestion.Font = new Font(this.Font.FontFamily, parentFontSize);
                btnMovimientos.Font = new Font(this.Font.FontFamily, parentFontSize);
                btnReportes.Font = new Font(this.Font.FontFamily, parentFontSize);
                btnSalir.Font = new Font(this.Font.FontFamily, parentFontSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting fonts: " + ex.Message);
            }

            // Inicializar búsqueda global en la barra superior
            var busqueda = new BusquedaGlobal(this, panelMaximizar, (form) => openChildForm(form));

            // Botón cerrar sesión
            var btnLogout = new Button
            {
                Text = "  Cerrar sesión",
                Dock = DockStyle.Bottom,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(100, 120, 110),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                Height = 38,
                Font = new Font(this.Font.FontFamily, 9.5f)
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 24, 22);
            btnLogout.Click += (s, e) =>
            {
                Servicios.CredentialStore.Clear();
                Servicios.SessionManager.Clear();
                Application.Restart();
            };
            panelSideMenu.Controls.Add(btnLogout);

            // Botón de actualizaciones (encima del botón Salir)
            var btnUpdate = new Button
            {
                Text = "  Buscar actualizaciones",
                Dock = DockStyle.Bottom,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(120, 160, 140),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                Height = 38,
                Font = new Font(this.Font.FontFamily, 11f)
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 29, 34);
            btnUpdate.Click += BtnUpdate_Click;
            panelSideMenu.Controls.Add(btnUpdate);

            ConfigurarSubmenuInventario();
            ConfigurarSubmenuOrdenes();
            ConfigurarSubmenuGestion();
            AplicarRol();

            this.Shown += VistaGeneral_Shown;
        }

        private async void VistaGeneral_Shown(object sender, EventArgs e)
        {
            // Pequeño retardo para que la interfaz se muestre primero y el toast se asome despues suavemente
            await Task.Delay(1000);

            // 1. Chequear actualizaciones automáticamente al iniciar
            try
            {
                var update = await UpdaterService.CheckForUpdateAsync();
                if (update != null)
                {
                    var modal = new ModalActualizacion(update);
                    modal.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al chequear actualizaciones al inicio: " + ex.Message);
            }
            
            // 2. Chequear stock bajo
            try
            {
                var materiales = new WP_Inventario.Datos.MaterialDatos().obtenerMateriales();
                int countBajoStock = materiales.Count(m => m.stockMinimo > 0 && m.stockActual <= m.stockMinimo);
                
                if (countBajoStock > 0)
                {
                    ToastNotification.ShowWarning(
                        $"¡Atención! Hay {countBajoStock} materiales con stock bajo. CLICK AQUÍ para verlos.",
                        () => 
                        {
                            using (var modal = new ModalStockBajo())
                            {
                                modal.ShowDialog(this);
                            }
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al chequear stock inicial: " + ex.Message);
            }
        }
        
        private Guna.UI2.WinForms.Guna2Button CrearBoton(string tooltip, Image icon, EventHandler onClick)
        {
            var btn = new Guna.UI2.WinForms.Guna2Button();

            btn.Dock = DockStyle.Right;
            btn.Width = 40;
            btn.FillColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.Image = icon;
            btn.ImageSize = new Size(16, 16);
            btn.BorderRadius = 6;
            btn.Cursor = Cursors.Hand;

            // Efectos hover / pressed
            btn.HoverState.FillColor = Color.FromArgb(50, 255, 255, 255);
            btn.PressedColor = Color.FromArgb(80, 200, 200, 200);

            btn.Click += onClick;

            // Tooltip
            var tip = new ToolTip();
            tip.SetToolTip(btn, tooltip);

            return btn;
        }

        private void hideSubMenu()
        {
            panelGestion.Visible = false;
            panelMovimientos.Visible = false;
            panelReportes.Visible = false;
            panelInventario.Visible = false;
        }

        private void showSubMenu(Panel subMenu)
        {
            if (subMenu.Visible == false)
            {
                hideSubMenu();
                if (subMenu == panelMovimientos)
                    AjustarAlturaPanelMovimientos();
                if (subMenu == panelInventario)
                    AjustarAlturaPanelInventario();
                subMenu.Visible = true;
            }
            else
                subMenu.Visible = false;
        }

        private void btnMedia_Click(object sender, EventArgs e)
        {
            showSubMenu(panelGestion);
        }

        #region MediaSubMenu
        private void button2_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Materiales());
            //..
            //your codes
            //..
            hideSubMenu();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Categorias());
            //..
            //your codes
            //..
            hideSubMenu();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //..
            //your codes
            //..
            hideSubMenu();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Proveedores());
            //..
            //your codes
            //..
            hideSubMenu();
        }
        private void btnRecortes_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Recortes());
            hideSubMenu();
        }
        private void btnFletes_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Fletes());
            hideSubMenu();
        }
        #endregion

        

        #region PlayListManagemetSubMenu
        private void button8_Click(object sender, EventArgs e)
        {
            //..
            //your codes
            //..
            hideSubMenu();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //..
            //your codes
            //..
            hideSubMenu();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //..
            //your codes
            //..
            hideSubMenu();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //..
            //your codes
            //..
            hideSubMenu();
        }
        #endregion

        private void btnTools_Click(object sender, EventArgs e)
        {
            showSubMenu(panelReportes);
        }
        #region ToolsSubMenu
        private void button13_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_ReporteStock());
            hideSubMenu();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Kardex());
            hideSubMenu();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_InformeObra());
            hideSubMenu();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //..
            //your codes
            //..
            hideSubMenu();
        }
        #endregion

        private void btnEqualizer_Click(object sender, EventArgs e)
        {
            showSubMenu(panelInventario);
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Inventario());
            hideSubMenu();
        }

        private void btnInventarioGeneral_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Inventario());
            hideSubMenu();
        }

        private void btnInventarioConteo_Click(object sender, EventArgs e)
        {
            OpenConteo();
            hideSubMenu();
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private Form activeForm = null;
        private Dictionary<Type, Form> childFormsCache = new Dictionary<Type, Form>();

        public void OpenEditMovimiento(int idMovimiento)
        {
            if (childFormsCache.ContainsKey(typeof(Vista_Movimientos)) && !childFormsCache[typeof(Vista_Movimientos)].IsDisposed)
            {
                var form = (Vista_Movimientos)childFormsCache[typeof(Vista_Movimientos)];
                form.SetEditMode(idMovimiento);
                openChildForm(form);
            }
            else
            {
                var form = new Vista_Movimientos();
                form.SetEditMode(idMovimiento);
                openChildForm(form);
            }
        }

        public void RefrescarHistorialSiAbierto()
        {
            if (childFormsCache.ContainsKey(typeof(Vista_HistorialMovimientos))
                && !childFormsCache[typeof(Vista_HistorialMovimientos)].IsDisposed)
            {
                ((Vista_HistorialMovimientos)childFormsCache[typeof(Vista_HistorialMovimientos)]).Refrescar();
            }
        }

        public void OpenHistorialMovimientos()
        {
            if (childFormsCache.ContainsKey(typeof(Vista_HistorialMovimientos))
                && !childFormsCache[typeof(Vista_HistorialMovimientos)].IsDisposed)
            {
                var historial = (Vista_HistorialMovimientos)childFormsCache[typeof(Vista_HistorialMovimientos)];
                historial.Refrescar();
                openChildForm(historial);
            }
            else
            {
                openChildForm(new Vista_HistorialMovimientos());
            }
        }

        public void OpenConteo()
        {
            if (childFormsCache.ContainsKey(typeof(Vista_Conteo))
                && !childFormsCache[typeof(Vista_Conteo)].IsDisposed)
            {
                var form = (Vista_Conteo)childFormsCache[typeof(Vista_Conteo)];
                form.Refrescar();
                openChildForm(form);
            }
            else
            {
                openChildForm(new Vista_Conteo());
            }
        }

        public void RefrescarOrdenesSiAbierto()
        {
            if (childFormsCache.ContainsKey(typeof(Vista_OrdenesModulo))
                && !childFormsCache[typeof(Vista_OrdenesModulo)].IsDisposed)
            {
                var modulo = (Vista_OrdenesModulo)childFormsCache[typeof(Vista_OrdenesModulo)];
                modulo.Refrescar();
                openChildForm(modulo);
            }
        }

        public void OpenOrdenForm(int idOrden)
        {
            Vista_OrdenesAlta form;
            if (childFormsCache.ContainsKey(typeof(Vista_OrdenesAlta)) && !childFormsCache[typeof(Vista_OrdenesAlta)].IsDisposed)
            {
                form = (Vista_OrdenesAlta)childFormsCache[typeof(Vista_OrdenesAlta)];
            }
            else
            {
                form = new Vista_OrdenesAlta();
            }

            if (idOrden > 0)
                form.SetEditMode(idOrden);
            else
                form.PrepararNuevaOrden();

            openChildForm(form);
        }

        public void OpenRevisionOrden(int idOrden)
        {
            Vista_OrdenesRevision form;
            if (childFormsCache.ContainsKey(typeof(Vista_OrdenesRevision)) && !childFormsCache[typeof(Vista_OrdenesRevision)].IsDisposed)
            {
                form = (Vista_OrdenesRevision)childFormsCache[typeof(Vista_OrdenesRevision)];
            }
            else
            {
                form = new Vista_OrdenesRevision();
            }

            form.SetOrder(idOrden);
            openChildForm(form);
        }

        public void openChildForm(Form childForm)
        {
            try
            {
                Type formType = childForm.GetType();

                if (activeForm != null)
                {
                    if (activeForm.GetType() == formType)
                    {
                        // Si ya es el mismo form, intentamos refrescarlo si tiene un método Refrescar
                        if (activeForm is Vista_HistorialMovimientos hist) hist.Refrescar();
                        // (Podrías agregar otros forms aquí si fuera necesario)

                        if (activeForm != childForm) childForm.Dispose();
                        return;
                    }
                    activeForm.Hide(); // Solo ocultar, no cerrar ni destruir
                }

                // Verificar si ya existe en la caché
                if (childFormsCache.ContainsKey(formType) && !childFormsCache[formType].IsDisposed)
                {
                    // Si existe, destruimos el que nos acaban de pasar por parámetro para ahorrar memoria RAM
                    if (childFormsCache[formType] != childForm)
                        childForm.Dispose();

                    activeForm = childFormsCache[formType];

                    // Refrescar datos automáticamente al recuperar del caché
                    if (activeForm is IRefrescable refrescable)
                        refrescable.Refrescar();
                }
                else
                {
                    // Si no existe, lo agregamos a la caché y lo configuramos visualmente
                    childFormsCache[formType] = childForm;
                    activeForm = childForm;
                    childForm.TopLevel = false;
                    childForm.FormBorderStyle = FormBorderStyle.None;
                    childForm.Dock = DockStyle.Fill;
                    panelChildForm.Controls.Add(childForm);
                    panelChildForm.Tag = childForm;
                }

                activeForm.BringToFront();
                activeForm.Show();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al abrir el formulario: " + ex.Message);
            }
        }
        // Drag Form con soporte para AeroSnap
        [System.Runtime.InteropServices.DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        

        private void panelChildForm_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panelMaximizar_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panelMaximizar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_NCCALCSIZE = 0x0083;
            const int WM_NCHITTEST = 0x0084;

            const int HTCLIENT = 1;
            const int HTCAPTION = 2;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            // Permite AeroSnap/Maximize en borderless
            if (m.Msg == WM_NCCALCSIZE && m.WParam != IntPtr.Zero)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Redimensionar por bordes + arrastre 
            if (m.Msg == WM_NCHITTEST)
            {
                Point p = PointToClient(Cursor.Position);
                int grip = 50; 

                bool left = p.X <= grip;
                bool right = p.X >= ClientSize.Width - grip;
                bool top = p.Y <= grip;
                bool bottom = p.Y >= ClientSize.Height - grip;

                // esquinas
                if (left && top) { m.Result = (IntPtr)HTTOPLEFT; return; }
                if (right && top) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                if (left && bottom) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                if (right && bottom) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }

                // lados
                if (left) { m.Result = (IntPtr)HTLEFT; return; }
                if (right) { m.Result = (IntPtr)HTRIGHT; return; }
                if (top) { m.Result = (IntPtr)HTTOP; return; }
                if (bottom) { m.Result = (IntPtr)HTBOTTOM; return; }

               
                if (panelMaximizar.Bounds.Contains(p))
                {
                    var local = new Point(p.X - panelMaximizar.Left, p.Y - panelMaximizar.Top);
                    Control child = panelMaximizar.GetChildAtPoint(local);
                    if (child == null || !(child is Guna.UI2.WinForms.Guna2Button))
                    {
                        m.Result = (IntPtr)HTCAPTION;
                        return;
                    }
                }

                m.Result = (IntPtr)HTCLIENT;
                return;
            }

            base.WndProc(ref m);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);
        const int MONITOR_DEFAULTTONEAREST = 2;


        private void btnCargaInicialObras_Click(object sender, EventArgs e)
        {

        }

        private void btnMovimientos_Click(object sender, EventArgs e)
        {
            showSubMenu(panelMovimientos);
        }

        private void btnEntrada_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Movimientos());
            hideSubMenu();
        }

        private void btnHistorial_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_HistorialMovimientos());
            hideSubMenu();
        }

        private void Fletes_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Fletes());
            hideSubMenu();
        }

        private void Obras_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Obras());
            hideSubMenu();
        }

        private void Ubicaciones_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Ubicaciones());
            hideSubMenu();
        }

        private void btnHerramientas_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Herramientas());
            hideSubMenu();
        }

        private void btnInsumos_Click(object sender, EventArgs e)
        {
            openChildForm(new Vista_Insumos());
            hideSubMenu();
        }

        private void AplicarRol()
        {
            if (SessionManager.Role == Servicios.UserRole.Galpon)
            {
                btnMateriales.Visible = false;
                btnCategorias.Visible = false;
                btnProveedores.Visible = false;
                Obras.Visible = false;
                Ubicaciones.Visible = false;

                // Ajustar altura cuando el panel se hace visible
                panelGestion.VisibleChanged += (s, e) =>
                {
                    if (panelGestion.Visible)
                    {
                        int altura = 0;
                        foreach (Control c in panelGestion.Controls)
                            if (c.Visible) altura += c.Height;
                        panelGestion.Height = altura;
                    }
                };
            }
        }

        private void ConfigurarSubmenuOrdenes()
        {
            if (panelMovimientos.Controls.OfType<Button>().Any(b => b.Name == "btnOrdenes"))
            {
                AjustarAlturaPanelMovimientos();
                return;
            }

            var btnNuevaOrden = new Button
            {
                Name = "btnOrdenes",
                Text = "Órdenes",
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Silver,
                Height = 40,
                Padding = new Padding(35, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = true
            };
            btnNuevaOrden.FlatAppearance.BorderSize = 0;
            btnNuevaOrden.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 20);
            btnNuevaOrden.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 34, 35);
            btnNuevaOrden.Click += (s, e) =>
            {
                openChildForm(new Vista_OrdenesModulo());
                hideSubMenu();
            };
            panelMovimientos.Controls.Add(btnNuevaOrden);

            panelMovimientos.Controls.SetChildIndex(btnEntrada, 2);
            panelMovimientos.Controls.SetChildIndex(btnHistorial, 1);
            panelMovimientos.Controls.SetChildIndex(btnNuevaOrden, 0);

            panelMovimientos.VisibleChanged += (s, e) =>
            {
                if (panelMovimientos.Visible)
                    AjustarAlturaPanelMovimientos();
            };

            AjustarAlturaPanelMovimientos();
        }

        private void ConfigurarSubmenuGestion()
        {
            if (panelGestion.Controls.OfType<Button>().Any(b => b.Name == "btnCanaletasPlegados"))
                return;

            var btnCanaletasPlegados = new Button
            {
                Name = "btnCanaletasPlegados",
                Text = "Canaletas / Plegados",
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Silver,
                Height = 40,
                Padding = new Padding(35, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = true
            };
            btnCanaletasPlegados.FlatAppearance.BorderSize = 0;
            btnCanaletasPlegados.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 20);
            btnCanaletasPlegados.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 34, 35);
            btnCanaletasPlegados.Click += (s, e) =>
            {
                openChildForm(new Vista_CanaletasPlegados());
                hideSubMenu();
            };
            panelGestion.Controls.Add(btnCanaletasPlegados);

            panelGestion.Controls.SetChildIndex(btnCanaletasPlegados, 0);

            panelGestion.VisibleChanged += (s, e) =>
            {
                if (panelGestion.Visible)
                {
                    int altura = 0;
                    foreach (Control c in panelGestion.Controls)
                        if (c.Visible) altura += c.Height;
                    panelGestion.Height = altura;
                }
            };
        }

        private void ConfigurarSubmenuInventario()
        {
            panelInventario.Controls.SetChildIndex(btnInventarioConteo, 0);
            panelInventario.Controls.SetChildIndex(btnInventarioGeneral, 1);

            panelInventario.VisibleChanged += (s, e) =>
            {
                if (panelInventario.Visible)
                    AjustarAlturaPanelInventario();
            };

            AjustarAlturaPanelInventario();
        }

        private void AjustarAlturaPanelMovimientos()
        {
            int altura = 0;
            foreach (Control control in panelMovimientos.Controls)
            {
                if (control.Visible)
                    altura += control.Height;
            }

            panelMovimientos.Height = altura;
        }

        private void AjustarAlturaPanelInventario()
        {
            int altura = 0;
            foreach (Control control in panelInventario.Controls)
            {
                if (control.Visible)
                    altura += control.Height;
            }
            panelInventario.Height = altura;
        }

        private async void BtnUpdate_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            btn.Enabled = false;
            btn.Text = "  Verificando...";

            try
            {
                var update = await UpdaterService.CheckForUpdateAsync();
                if (update == null)
                {
                    ToastNotification.ShowSuccess("Ya tenés la última versión (" + AppVersion.Current + ").");
                }
                else
                {
                    var modal = new ModalActualizacion(update);
                    modal.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo verificar actualizaciones: " + ex.Message);
            }
            finally
            {
                btn.Enabled = true;
                btn.Text = "  Buscar actualizaciones";
            }
        }
    }
}
