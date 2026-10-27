using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas
{
    public partial class FiltroMaterial : Form
    {
        public event EventHandler<FiltroMaterialParams> onBuscar;
        public FiltroMaterial()
        {
            InitializeComponent();
            CategoriaDatos datos = new CategoriaDatos();
            flCategoria.DataSource = datos.GetCategorias();
            flCategoria.DisplayMember = "nombre";

            CargarProveedores();
            this.BackColor = Color.FromArgb(20, 30, 25);

            // Borde redondeado para el formulario
            this.Region = System.Drawing.Region.FromHrgn(Guna2ControlHelper.CreateRoundRectRgn(0, 0, this.Width, this.Height, 15, 15));
        }

        private void CargarProveedores()
        {
            ComunDatos comunDatos = new ComunDatos();
            var proveedores = comunDatos.ObtenerListaGenerica("proveedores");
            clbProveedoresFiltro.Items.Clear();
            foreach (DataRow row in proveedores.Rows)
            {
                int id = Convert.ToInt32(row["id_proveedores"]);
                string nombre = row["nombre"].ToString();
                clbProveedoresFiltro.Items.Add(new ProveedorItem(id, nombre));
            }
        }
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            // Find the top-level main form before hiding
            Form mainForm = null;
            if (this.Owner != null)
            {
                Control parent = this.Owner;
                while (parent.Parent != null)
                    parent = parent.Parent;
                mainForm = parent as Form;
            }
            this.Hide();
            // Defer activation to avoid cyclic activate/deactivate flicker
            if (mainForm != null && !mainForm.IsDisposed)
            {
                var target = mainForm;
                target.BeginInvoke(new Action(() =>
                {
                    if (!target.IsDisposed)
                    {
                        target.Activate();
                    }
                }));
            }
        }

        private void FiltroMaterial_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void flCategoria_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void AplicarFiltros_Click(object sender, EventArgs e)
        {
            var p = new FiltroMaterialParams();
            if (desdeCantidadFiltro.Value > 0) p.DesdeCantidad = desdeCantidadFiltro.Value;
            if (hastaCantidadFiltro.Value > 0) p.HastaCantidad = hastaCantidadFiltro.Value;
            p.SoloStockBajo = sionoBajo.Checked;

            foreach (WP_Inventario.Modelos.Categoria item in flCategoria.CheckedItems)
                p.IdsCategoria.Add(item.id);

            foreach (ProveedorItem item in clbProveedoresFiltro.CheckedItems)
                p.IdsProveedor.Add(item.Id);

            onBuscar?.Invoke(this, p);
            this.Hide();
        }

        private void FiltroMaterial_Load(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void panelAntiParpadeo1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void sionoBajo_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void LimpiarFiltros_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < flCategoria.Items.Count; i++) flCategoria.SetItemChecked(i, false);
            for (int i = 0; i < clbProveedoresFiltro.Items.Count; i++) clbProveedoresFiltro.SetItemChecked(i, false);
            desdeCantidadFiltro.Value = 0;
            hastaCantidadFiltro.Value = 0;
            sionoBajo.Checked = false;
            onBuscar?.Invoke(this, new FiltroMaterialParams());
        }
    }

    public static class Guna2ControlHelper
    {
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
    }
}
