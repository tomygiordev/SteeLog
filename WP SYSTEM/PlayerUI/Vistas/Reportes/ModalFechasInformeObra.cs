using System;
using System.Windows.Forms;
using WP_Inventario.Datos;

namespace WP_Inventario.Vistas.Reportes
{
    public partial class ModalFechasInformeObra : Form
    {
        public DateTime FechaDesde { get; private set; }
        public DateTime FechaHasta { get; private set; }

        public ModalFechasInformeObra(int idObra)
        {
            InitializeComponent();

            dtpHasta.Value = DateTime.Today;

            try
            {
                var datos = new InformeObraDatos();
                DateTime? primeraFecha = datos.ObtenerPrimeraFechaRemitos(idObra);
                dtpDesde.Value = primeraFecha ?? DateTime.Today.AddMonths(-1);
            }
            catch
            {
                dtpDesde.Value = DateTime.Today.AddMonths(-1);
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (dtpDesde.Value > dtpHasta.Value)
            {
                ToastNotification.ShowWarning("La fecha 'Desde' no puede ser mayor que 'Hasta'.");
                return;
            }

            FechaDesde = dtpDesde.Value.Date;
            FechaHasta = dtpHasta.Value.Date;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
