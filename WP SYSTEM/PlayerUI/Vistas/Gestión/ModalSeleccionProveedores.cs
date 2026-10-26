using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario;

namespace WP_Inventario.Vistas
{
    public partial class ModalSeleccionProveedores : Form
    {
        public List<int> IDsSeleccionados { get; private set; } = new List<int>();

        public ModalSeleccionProveedores(List<int> idsIniciales)
        {
            InitializeComponent();
            CargarProveedores(idsIniciales);
        }

        private void CargarProveedores(List<int> idsIniciales)
        {
            try
            {
                ComunDatos comunDatos = new ComunDatos();
                var proveedores = comunDatos.ObtenerListaGenerica("proveedores");

                clbProveedores.Items.Clear();
                foreach (DataRow row in proveedores.Rows)
                {
                    int id = Convert.ToInt32(row["id_proveedores"]);
                    string nombre = row["nombre"].ToString();
                    bool seleccionado = idsIniciales.Contains(id);
                    clbProveedores.Items.Add(new ProveedorItem(id, nombre), seleccionado);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar proveedores: " + ex.Message);
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            IDsSeleccionados.Clear();
            foreach (var item in clbProveedores.CheckedItems)
            {
                if (item is ProveedorItem pi)
                    IDsSeleccionados.Add(pi.Id);
            }
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
