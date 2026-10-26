using System;
using System.Windows.Forms;
using WP_Inventario.Datos;

namespace WP_Inventario.Vistas.Movimientos
{
    public partial class ModalAgregarFlete : Form
    {
        public int IdFleteCreado { get; private set; }
        public string NombreFleteCreado { get; private set; }

        public ModalAgregarFlete()
        {
            InitializeComponent();
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                ToastNotification.ShowWarning("El nombre del flete no puede estar vacío.");
                return;
            }

            try
            {
                FleteDatos datos = new FleteDatos();
                int nuevoId = datos.GuardarFlete(nombre);
                
                IdFleteCreado = nuevoId;
                NombreFleteCreado = nombre;
                
                ToastNotification.ShowSuccess("Flete creado correctamente.");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar flete: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
