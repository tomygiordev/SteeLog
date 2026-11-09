using SteeLog.Presentation;
using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using SteeLog.Servicios;
using SteeLog.UI;

namespace SteeLog.Vistas.Movimientos.Ordenes
{
    public class Vista_OrdenesModulo : Form, IRecargaCatalogos
    {
        private readonly IServiciosVista servicios;
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

        public Vista_OrdenesModulo(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));

            InitializeComponent();
            Load += Vista_OrdenesModulo_Load;
        }

        private void Vista_OrdenesModulo_Load(object sender, EventArgs e)
        {
            if (DesignMode) return;
            btnTabNueva.Visible = servicios.Sesion.Role == UserRole.Admin;
            InicializarVistas();
            MostrarListado();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = Tema.Fondo;

            // Banda de titulo estandar de la app (la vista ya no repite "Ordenes de Compra" debajo).
            Tema.TituloVista(this, "Órdenes");

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 2;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Margin = new Padding(0);
            root.Padding = new Padding(0);
            Controls.Add(root);
            root.BringToFront();

            var tabs = new FlowLayoutPanel();
            tabs.Dock = DockStyle.Fill;
            tabs.FlowDirection = FlowDirection.LeftToRight;
            tabs.WrapContents = false;
            tabs.Padding = new Padding(28, 10, 28, 0);
            tabs.BackColor = Tema.Fondo;

            btnTabListado = UiVista.BotonTab("Listado", 168);
            btnTabListado.Click += (s, e) => MostrarListado();

            btnTabNueva = UiVista.BotonTab("Nueva orden", 168);
            btnTabNueva.Visible = false;
            btnTabNueva.Click += (s, e) => MostrarAlta();

            btnTabRecepcion = UiVista.BotonTab("Recepción", 168);
            btnTabRecepcion.Click += (s, e) => MostrarRecepcion();

            btnTabHistorial = UiVista.BotonTab("Historial", 168);
            btnTabHistorial.Click += (s, e) => MostrarHistorial();

            tabs.Controls.Add(btnTabListado);
            tabs.Controls.Add(btnTabNueva);
            tabs.Controls.Add(btnTabRecepcion);
            tabs.Controls.Add(btnTabHistorial);

            panelHost = new Panel();
            panelHost.Dock = DockStyle.Fill;
            panelHost.BackColor = OrdenesUi.FondoForm;

            root.Controls.Add(tabs, 0, 0);
            root.Controls.Add(panelHost, 0, 1);
            ResumeLayout(false);
        }

        private void InicializarVistas()
        {
            _vistaListado = new Vista_OrdenesListado(servicios,
                soloRecepcion: false,
                mostrarNuevaOrden: false,
                onNuevaOrden: MostrarAlta,
                onEditarOrden: MostrarAlta,
                onRevisarOrden: MostrarRevision);

            _vistaRecepcion = new Vista_OrdenesListado(servicios,
                soloRecepcion: true,
                mostrarNuevaOrden: false,
                onEditarOrden: MostrarAlta,
                onRevisarOrden: MostrarRevisionDesdeRecepcion);

            _vistaHistorial = new Vista_OrdenesListado(servicios,
                soloHistorial: true,
                mostrarNuevaOrden: false,
                onEditarOrden: MostrarAlta,
                onRevisarOrden: MostrarRevisionDesdeHistorial);

            _vistaAlta = new Vista_OrdenesAlta(servicios);
            _vistaAlta.OnOrdenGuardada = idOrden =>
            {
                RefrescarListas();
                MostrarListado();
            };

            _vistaRevision = new Vista_OrdenesRevision(servicios);
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
            if (servicios.Sesion.Role != UserRole.Admin)
                return;

            ActivarTab(btnTabNueva);
            _vistaAlta.PrepararNuevaOrden();
            MostrarControl(_vistaAlta);
        }

        private void MostrarAlta(int idOrden)
        {
            if (servicios.Sesion.Role != UserRole.Admin)
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
            foreach (var tab in new[] { btnTabListado, btnTabNueva, btnTabRecepcion, btnTabHistorial })
                Tema.Boton(tab, tab == activo ? Variante.TabActiva : Variante.Tab);
        }

        private void RefrescarListas()
        {
            _vistaListado.Refrescar();
            _vistaRecepcion.Refrescar();
            _vistaHistorial.Refrescar();
        }

        public void RecargarCatalogos() { _vistaAlta?.RecargarCatalogos(); }

        public void Refrescar()
        {
            if (_vistaListado == null) return;
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
