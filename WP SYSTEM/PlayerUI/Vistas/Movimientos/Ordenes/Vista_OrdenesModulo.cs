using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas.Movimientos.Ordenes
{
    public class Vista_OrdenesModulo : Form
    {
        private readonly Color _tabActiveColor = Color.FromArgb(25, 135, 84);
        private readonly Color _tabInactiveColor = Color.FromArgb(47, 84, 57);

        private Guna2Button btnTabListado;
        private Guna2Button btnTabNueva;
        private Guna2Button btnTabRecepcion;
        private Guna2Button btnTabHistorial;
        private Panel panelHost;

        private Vista_OrdenesListado _vistaListado;
        private Vista_OrdenesListado _vistaRecepcion;
        private Vista_OrdenesListado _vistaHistorial;
        private Vista_OrdenesAlta _vistaAlta;
        private Vista_OrdenesRevision _vistaRevision;
        private Control _activeControl;
        private bool _revisionDesdeHistorial;

        public Vista_OrdenesModulo()
        {
            InitializeComponent();
            InicializarVistas();
            MostrarListado();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = OrdenesUi.FondoForm;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            var header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(18, 18, 18, 6);
            var lblTitulo = new Label
            {
                Text = "Órdenes",
                Font = new Font("Bahnschrift SemiBold", 17F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            };
            header.Controls.Add(lblTitulo);

            var tabs = new Panel();
            tabs.Dock = DockStyle.Fill;
            tabs.Padding = new Padding(18, 0, 18, 8);

            btnTabListado = CrearTab("Listado", 18);
            btnTabListado.Click += (s, e) => MostrarListado();

            btnTabNueva = CrearTab("Nueva orden", 216);
            btnTabNueva.Visible = SessionManager.Role == UserRole.Admin;
            btnTabNueva.Click += (s, e) => MostrarAlta();

            btnTabRecepcion = CrearTab("Recepción", 414);
            btnTabRecepcion.Click += (s, e) => MostrarRecepcion();

            btnTabHistorial = CrearTab("Historial", 612);
            btnTabHistorial.Click += (s, e) => MostrarHistorial();

            tabs.Controls.Add(btnTabListado);
            tabs.Controls.Add(btnTabNueva);
            tabs.Controls.Add(btnTabRecepcion);
            tabs.Controls.Add(btnTabHistorial);

            panelHost = new Panel();
            panelHost.Dock = DockStyle.Fill;
            panelHost.BackColor = OrdenesUi.FondoForm;

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(tabs, 0, 1);
            root.Controls.Add(panelHost, 0, 2);
            ResumeLayout(false);
        }

        private Guna2Button CrearTab(string texto, int x)
        {
            var btn = OrdenesUi.CrearBoton(texto);
            btn.Location = new Point(x, 10);
            btn.Size = new Size(180, 36);
            btn.FillColor = _tabInactiveColor;
            btn.HoverState.FillColor = _tabActiveColor;
            return btn;
        }

        private void InicializarVistas()
        {
            _vistaListado = new Vista_OrdenesListado(
                soloRecepcion: false,
                mostrarNuevaOrden: false,
                onNuevaOrden: MostrarAlta,
                onEditarOrden: MostrarAlta,
                onRevisarOrden: MostrarRevision);

            _vistaRecepcion = new Vista_OrdenesListado(
                soloRecepcion: true,
                mostrarNuevaOrden: false,
                onEditarOrden: MostrarAlta,
                onRevisarOrden: MostrarRevisionDesdeRecepcion);

            _vistaHistorial = new Vista_OrdenesListado(
                soloHistorial: true,
                mostrarNuevaOrden: false,
                onEditarOrden: MostrarAlta,
                onRevisarOrden: MostrarRevisionDesdeHistorial);

            _vistaAlta = new Vista_OrdenesAlta();
            _vistaAlta.OnOrdenGuardada = idOrden =>
            {
                RefrescarListas();
                MostrarListado();
            };

            _vistaRevision = new Vista_OrdenesRevision();
            _vistaRevision.OnRecepcionConfirmada = idOrden =>
            {
                RefrescarListas();
                if (_revisionDesdeHistorial)
                    MostrarHistorial();
                else
                    MostrarRecepcion();
            };
            _vistaRevision.OnCancelar = () =>
            {
                if (_revisionDesdeHistorial)
                    MostrarHistorial();
                else
                    MostrarRecepcion();
            };

            PrepararControl(_vistaListado);
            PrepararControl(_vistaRecepcion);
            PrepararControl(_vistaHistorial);
            PrepararControl(_vistaAlta);
            PrepararControl(_vistaRevision);
        }

        private void PrepararControl(Form form)
        {
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Visible = false;
            panelHost.Controls.Add(form);
        }

        private void MostrarListado()
        {
            _revisionDesdeHistorial = false;
            ActivarTab(btnTabListado);
            _vistaListado.Refrescar();
            MostrarControl(_vistaListado);
        }

        private void MostrarAlta()
        {
            if (SessionManager.Role != UserRole.Admin)
                return;

            ActivarTab(btnTabNueva);
            _vistaAlta.PrepararNuevaOrden();
            MostrarControl(_vistaAlta);
        }

        private void MostrarAlta(int idOrden)
        {
            if (SessionManager.Role != UserRole.Admin)
                return;

            ActivarTab(btnTabNueva);
            if (idOrden > 0)
                _vistaAlta.SetEditMode(idOrden);
            else
                _vistaAlta.PrepararNuevaOrden();
            MostrarControl(_vistaAlta);
        }

        private void MostrarRecepcion()
        {
            _revisionDesdeHistorial = false;
            ActivarTab(btnTabRecepcion);
            _vistaRecepcion.Refrescar();
            MostrarControl(_vistaRecepcion);
        }

        private void MostrarHistorial()
        {
            _revisionDesdeHistorial = true;
            ActivarTab(btnTabHistorial);
            _vistaHistorial.Refrescar();
            MostrarControl(_vistaHistorial);
        }

        private void MostrarRevisionDesdeRecepcion(int idOrden)
        {
            _revisionDesdeHistorial = false;
            MostrarRevision(idOrden);
        }

        private void MostrarRevisionDesdeHistorial(int idOrden)
        {
            _revisionDesdeHistorial = true;
            MostrarRevision(idOrden);
        }

        private void MostrarRevision(int idOrden)
        {
            ActivarTab(_revisionDesdeHistorial ? btnTabHistorial : btnTabRecepcion);
            _vistaRevision.SetOrder(idOrden);
            MostrarControl(_vistaRevision);
        }

        private void MostrarControl(Form form)
        {
            if (_activeControl != null)
                _activeControl.Hide();

            _activeControl = form;
            form.Show();
            form.BringToFront();
        }

        private void ActivarTab(Guna2Button activo)
        {
            btnTabListado.FillColor = _tabInactiveColor;
            btnTabRecepcion.FillColor = _tabInactiveColor;
            btnTabHistorial.FillColor = _tabInactiveColor;
            if (btnTabNueva.Visible)
                btnTabNueva.FillColor = _tabInactiveColor;

            activo.FillColor = _tabActiveColor;
        }

        private void RefrescarListas()
        {
            _vistaListado.Refrescar();
            _vistaRecepcion.Refrescar();
            _vistaHistorial.Refrescar();
        }

        public void Refrescar()
        {
            RefrescarListas();

            if (_activeControl == _vistaRevision)
            {
                if (_vistaRevision.CurrentOrderId > 0)
                    _vistaRevision.SetOrder(_vistaRevision.CurrentOrderId);
                return;
            }

            if (_activeControl == _vistaRecepcion)
            {
                _vistaRecepcion.Refrescar();
                return;
            }

            if (_activeControl == _vistaHistorial)
            {
                _vistaHistorial.Refrescar();
                return;
            }

            _vistaListado.Refrescar();
        }
    }
}
