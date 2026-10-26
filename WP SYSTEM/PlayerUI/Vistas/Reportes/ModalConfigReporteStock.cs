using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;

namespace WP_Inventario.Vistas.Reportes
{
    public class ModalConfigReporteStock : Form
    {
        private readonly ReporteStockDatos datos = new ReporteStockDatos();
        private readonly MaterialDatos materialDatos = new MaterialDatos();

        private readonly ListBox lstSecciones = new ListBox();
        private readonly TextBox txtNombreSeccion = new TextBox();
        private readonly TextBox txtBuscarMaterial = new TextBox();
        private readonly ListBox lstResultadosMateriales = new ListBox();
        private readonly ListBox lstMaterialesSeccion = new ListBox();

        private readonly List<SeccionEditable> secciones = new List<SeccionEditable>();
        private readonly List<int> seccionesEliminadas = new List<int>();
        private List<Material> materialesDisponibles = new List<Material>();

        private bool actualizandoUI;

        public ModalConfigReporteStock()
        {
            Text = "Configurar secciones del reporte de stock";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1100, 720);
            MinimumSize = new Size(980, 620);
            BackColor = Color.FromArgb(29, 38, 35);
            ForeColor = Color.White;
            Font = new Font("Bahnschrift Light", 10F);

            CrearInterfaz();
            CargarDatos();
        }

        private void CrearInterfaz()
        {
            var panelPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(29, 38, 35),
                Padding = new Padding(14)
            };
            panelPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            panelPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            panelPrincipal.Controls.Add(CrearPanelSecciones(), 0, 0);
            panelPrincipal.Controls.Add(CrearPanelMateriales(), 1, 0);

            Controls.Add(panelPrincipal);
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
                Height = 38,
                ForeColor = Color.FromArgb(170, 195, 180),
                Text = "Cada sección se muestra como cabecera en el reporte. Podés ordenarlas con subir y bajar.",
                Padding = new Padding(0, 4, 0, 6)
            };

            lstSecciones.Dock = DockStyle.Fill;
            lstSecciones.BorderStyle = BorderStyle.None;
            lstSecciones.BackColor = Color.FromArgb(35, 50, 40);
            lstSecciones.ForeColor = Color.White;
            lstSecciones.Font = new Font("Bahnschrift SemiBold", 10.5F);
            lstSecciones.IntegralHeight = false;
            lstSecciones.SelectedIndexChanged += LstSecciones_SelectedIndexChanged;

            var pnlEditor = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 220,
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Color.FromArgb(24, 32, 29),
                Padding = new Padding(10)
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
                ForeColor = Color.White,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlEditor.Controls.Add(lblNombre, 0, 0);
            pnlEditor.SetColumnSpan(lblNombre, 2);

            txtNombreSeccion.Dock = DockStyle.Fill;
            txtNombreSeccion.BorderStyle = BorderStyle.FixedSingle;
            txtNombreSeccion.BackColor = Color.FromArgb(35, 50, 40);
            txtNombreSeccion.ForeColor = Color.White;
            txtNombreSeccion.TextChanged += TxtNombreSeccion_TextChanged;
            pnlEditor.Controls.Add(txtNombreSeccion, 0, 1);
            pnlEditor.SetColumnSpan(txtNombreSeccion, 2);

            pnlEditor.Controls.Add(CrearBoton("Nueva sección", (s, e) => NuevaSeccion()), 0, 2);
            pnlEditor.Controls.Add(CrearBoton("Eliminar sección", (s, e) => EliminarSeccionActual(), Color.FromArgb(160, 55, 55)), 1, 2);
            pnlEditor.Controls.Add(CrearBoton("Subir sección", (s, e) => MoverSeccion(-1)), 0, 3);
            pnlEditor.Controls.Add(CrearBoton("Bajar sección", (s, e) => MoverSeccion(1)), 1, 3);

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
                Height = 38,
                ForeColor = Color.FromArgb(170, 195, 180),
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
                ForeColor = Color.White,
                TextAlign = ContentAlignment.BottomLeft
            };
            panelContenido.Controls.Add(lblBuscar, 0, 0);

            var lblSeleccionados = new Label
            {
                Text = "Materiales agregados",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.BottomLeft
            };
            panelContenido.Controls.Add(lblSeleccionados, 2, 0);

            txtBuscarMaterial.Dock = DockStyle.Fill;
            txtBuscarMaterial.BorderStyle = BorderStyle.FixedSingle;
            txtBuscarMaterial.BackColor = Color.FromArgb(35, 50, 40);
            txtBuscarMaterial.ForeColor = Color.White;
            txtBuscarMaterial.TextChanged += TxtBuscarMaterial_TextChanged;
            txtBuscarMaterial.KeyDown += TxtBuscarMaterial_KeyDown;
            panelContenido.Controls.Add(txtBuscarMaterial, 0, 1);

            var btnAgregar = CrearBoton("Agregar →", (s, e) => AgregarMaterialSeleccionado());
            btnAgregar.Dock = DockStyle.Fill;
            panelContenido.Controls.Add(btnAgregar, 1, 1);

            lstResultadosMateriales.Dock = DockStyle.Fill;
            lstResultadosMateriales.BorderStyle = BorderStyle.None;
            lstResultadosMateriales.BackColor = Color.FromArgb(35, 50, 40);
            lstResultadosMateriales.ForeColor = Color.White;
            lstResultadosMateriales.IntegralHeight = false;
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
            panelBotonesMateriales.Controls.Add(CrearBoton("Quitar", (s, e) => QuitarMaterialSeleccionado(), Color.FromArgb(160, 55, 55)), 0, 0);
            panelBotonesMateriales.Controls.Add(CrearBoton("Subir", (s, e) => MoverMaterial(-1)), 0, 1);
            panelBotonesMateriales.Controls.Add(CrearBoton("Bajar", (s, e) => MoverMaterial(1)), 0, 2);
            panelContenido.Controls.Add(panelBotonesMateriales, 1, 2);

            lstMaterialesSeccion.Dock = DockStyle.Fill;
            lstMaterialesSeccion.BorderStyle = BorderStyle.None;
            lstMaterialesSeccion.BackColor = Color.FromArgb(35, 50, 40);
            lstMaterialesSeccion.ForeColor = Color.White;
            lstMaterialesSeccion.IntegralHeight = false;
            panelContenido.Controls.Add(lstMaterialesSeccion, 2, 2);

            var btnGuardar = CrearBoton("Guardar cambios", (s, e) => GuardarTodo(), Color.FromArgb(39, 174, 96));
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
                Font = new Font("Bahnschrift SemiBold", 14F),
                ForeColor = Color.White
            };
        }

        private Button CrearBoton(string texto, EventHandler click, Color? color = null)
        {
            var boton = new Button
            {
                Text = texto,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = color ?? Color.FromArgb(47, 84, 57),
                ForeColor = Color.White
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.Click += click;
            return boton;
        }

        private void CargarDatos(int? seleccionarSeccionId = null)
        {
            try
            {
                actualizandoUI = true;
                materialesDisponibles = materialDatos.obtenerMateriales()
                    .OrderBy(m => m.nombre_categoria)
                    .ThenBy(m => m.nombre)
                    .ToList();

                secciones.Clear();
                seccionesEliminadas.Clear();

                foreach (var seccion in datos.ObtenerSecciones())
                {
                    var asignados = datos.ObtenerMaterialesSeccion(seccion.id);
                    var materiales = asignados
                        .OrderBy(m => m.orden)
                        .Select(m => CrearMaterialSeleccionado(m.idMaterial))
                        .Where(m => m != null)
                        .ToList();

                    secciones.Add(new SeccionEditable
                    {
                        id = seccion.id,
                        nombre = seccion.nombre,
                        Materiales = materiales
                    });
                }

                RefrescarSecciones(seleccionarSeccionId);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cargar la configuración de secciones del reporte de stock. " + ex.Message);
            }
            finally
            {
                actualizandoUI = false;
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

            if (MessageBox.Show("¿Eliminar la sección y sus materiales asignados?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
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

        private void GuardarTodo()
        {
            try
            {
                ValidarConfiguracion();

                foreach (var idSeccion in seccionesEliminadas.Distinct().ToList())
                {
                    datos.EliminarSeccion(idSeccion);
                }

                for (int i = 0; i < secciones.Count; i++)
                {
                    var seccion = secciones[i];
                    seccion.nombre = (seccion.nombre ?? string.Empty).Trim();
                    seccion.id = datos.GuardarSeccion(seccion.id, seccion.nombre, i + 1);
                }

                foreach (var seccion in secciones)
                {
                    var materiales = seccion.Materiales
                        .Select((material, indice) => new ReporteStockMaterialConfigurado
                        {
                            idMaterial = material.idMaterial,
                            orden = indice + 1
                        })
                        .ToList();

                    datos.GuardarMaterialesSeccion(seccion.id, materiales);
                }

                seccionesEliminadas.Clear();
                int? seccionId = SeccionActual != null && SeccionActual.id != 0 ? SeccionActual.id : (int?)null;
                CargarDatos(seccionId);
                ToastNotification.ShowSuccess("Configuración de secciones guardada.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo guardar la configuración del reporte: " + ex.Message);
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
            if (actualizandoUI)
            {
                return;
            }

            CargarSeccionEnPantalla(SeccionActual);
        }

        private void TxtNombreSeccion_TextChanged(object sender, EventArgs e)
        {
            if (actualizandoUI)
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
