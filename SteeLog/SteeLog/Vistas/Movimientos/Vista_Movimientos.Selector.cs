using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using SteeLog.Presentation;
using SteeLog.Core.Application.Movimientos;
using System.Windows.Forms;
using ImpactoStock = SteeLog.Domain.Inventario.ImpactoStock;
using ClasificadorMovimiento = SteeLog.Domain.Inventario.ClasificadorMovimiento;
using SteeLog.Modelos;
using SteeLog.Reportes;
using SteeLog.Servicios;
using System.IO;
using SteeLog.UI;
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SteeLog.Vistas.Movimientos
{
    public partial class Vista_Movimientos
    {
        private void CrearPanelResultados()
        {
            _panelResultados = new Panel
            {
                Size = new Size(516, 0),
                BackColor = Tema.Superficie,
                Visible = false,
                AutoScroll = true
            };

            this.Controls.Add(_panelResultados);
            _panelResultados.BringToFront();
        }

        private void PosicionarPanelResultados()
        {
            var screenPoint = txtBuscadorMaterial.PointToScreen(Point.Empty);
            var parentPoint = this.PointToClient(screenPoint);

            _panelResultados.Location = new Point(
                parentPoint.X,
                parentPoint.Y + txtBuscadorMaterial.Height + 5
            );
        }

        private void ConfigurarDebounce()
        {
            _timerDebounce = new Timer { Interval = 300 };
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                EjecutarBusqueda(txtBuscadorMaterial.Text.Trim());
            };
        }

        private void TxtBuscadorMaterial_TextChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;

            _timerDebounce.Stop();
            string nombreEsperado = _materialSeleccionado != null ? _materialSeleccionado.nombre : "";
            if (_recorteSeleccionado != null)
            {
                nombreEsperado = $"{_recorteSeleccionado.nombre_material} (Recorte: {_recorteSeleccionado.ancho}x{_recorteSeleccionado.altura})";
            }

            // Si estaba editando pero borro algo, deselecciono el material/recorte
            if (_materialSeleccionado != null && txtBuscadorMaterial.Text != nombreEsperado)
            {
                _materialSeleccionado = null;
                _recorteSeleccionado = null;
                btnRecorte.Enabled = false;
            }

            if (txtBuscadorMaterial.Text.Trim().Length < 2)
            {
                // Mostrar indicación en vez de buscar si hay menos de 2 caracteres
                EjecutarBusqueda("");
                return;
            }
            _timerDebounce.Start();
        }

        private void TxtBuscadorMaterial_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OcultarResultados();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && _panelResultados.Visible && _panelResultados.Controls.Count > 0)
            {
                _panelResultados.Controls[0].Focus();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter && _materialSeleccionado != null)
            {
                cantMatMov.Focus();
                e.SuppressKeyPress = true;
            }
        }

        private class ItemBuscador
        {
            public bool EsMaterial { get; set; }
            public Material Mat { get; set; }
            public Recorte Rec { get; set; }
            public string NombreMostrar { get; set; }
            public string InfoMostrar { get; set; }
            public Color ColorAcento { get; set; }
            public string Icono { get; set; }
        }

        private void EjecutarBusqueda(string query)
        {
            string qLower = query?.ToLower() ?? "";

            if (qLower.Length < 2)
            {
                // Menos de 2 caracteres: pasar un mensaje especial a MostrarResultados
                var vacio = new List<ItemBuscador>();
                MostrarResultados(vacio, "MIN_CHARS");
                return;
            }

            var itemsCandidatos = new List<ItemBuscador>();
            var listaRec = recortesDisponibles;

            // Filtrar materiales (requiere al menos 2 caracteres minimo)
            var mats = materialesFull
                .Where(m => m.nombre != null && m.nombre.ToLower().Contains(qLower))
                .OrderByDescending(m => m.nombre.ToLower().StartsWith(qLower))
                .ThenBy(m => m.nombre)
                .Take(50) // Limitar a 50 resultados para evitar colapso de UI
                .ToList();

            foreach (var m in mats)
            {
                string infoStockMaterial = servicios.Sesion.DebeOcultarCantidadesStockMateriales
                    ? $"Material • {servicios.Sesion.ObtenerEstadoStockMaterial(m.stockActual)}"
                    : $"Material • Stock: {m.stockActual:N0}";

                itemsCandidatos.Add(new ItemBuscador
                {
                    EsMaterial = true,
                    Mat = m,
                    NombreMostrar = m.nombre,
                    InfoMostrar = infoStockMaterial,
                    ColorAcento = Tema.Acento,
                    Icono = "📦"
                });

                if (listaRec != null)
                {
                    var recortesHijos = listaRec.Where(r => r.id_material == m.id && r.cantidad > 0).ToList();
                    foreach (var r in recortesHijos)
                    {
                        itemsCandidatos.Add(new ItemBuscador
                        {
                            EsMaterial = false,
                            Rec = r,
                            NombreMostrar = $"{r.nombre_material} (Recorte: {r.ancho}x{r.altura})",
                            InfoMostrar = $"Recorte • Cantidad disponible: {r.cantidad:N0}",
                            ColorAcento = Tema.Advertencia,
                            Icono = "✂️"
                        });
                    }
                }
            }

            MostrarResultados(itemsCandidatos, query);
        }

        private void MostrarResultados(List<ItemBuscador> resultados, string query)
        {
            _panelResultados.Controls.Clear();

            const int MAX_PANEL_HEIGHT = 260;
            const int ITEM_HEIGHT = 48;

            if (resultados.Count == 0)
            {
                string mensaje = "";
                if (query == "MIN_CHARS")
                    mensaje = "Escribí al menos 2 letras para buscar...";
                else if (string.IsNullOrEmpty(query))
                    mensaje = "No hay materiales disponibles.";
                else
                    mensaje = $"Sin resultados para \"{query}\"";

                var lblNoResults = new Label
                {
                    Text = mensaje,
                    Font = Tema.FuenteTexto,
                    ForeColor = Tema.TextoApagado,
                    Size = new Size(516, 45),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Top
                };
                _panelResultados.Controls.Add(lblNoResults);
                _panelResultados.Size = new Size(516, 50);
                PosicionarPanelResultados();
                _panelResultados.BringToFront();
                _panelResultados.Visible = true;
                return;
            }

            // Sin limite de items - el scroll se encarga
            int totalContentHeight = resultados.Count * ITEM_HEIGHT;
            int panelHeight = Math.Min(totalContentHeight, MAX_PANEL_HEIGHT);

            // Agregar items en orden (Dock Top apila al revés, invertimos)
            for (int i = resultados.Count - 1; i >= 0; i--)
            {
                var item = resultados[i];
                var panel = CrearItemResultado(item);
                _panelResultados.Controls.Add(panel);
            }

            _panelResultados.Size = new Size(516, panelHeight);
            PosicionarPanelResultados();
            _panelResultados.BringToFront();
            _panelResultados.Visible = true;
        }

        private sealed class PanelResultado : Panel
        {
            internal PanelResultado() { SetStyle(ControlStyles.Selectable, true); }
        }

        private Panel CrearItemResultado(ItemBuscador item)
        {
            var panel = new PanelResultado
            {
                Size = new Size(496, 48),
                BackColor = Tema.Superficie,
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 10, 0),
                TabStop = true,
                AccessibleName = item.NombreMostrar
            };
            panel.PreviewKeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Escape)
                    e.IsInputKey = true;
            };
            panel.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) ItemClicked(item);
                else if (e.KeyCode == Keys.Escape) { OcultarResultados(); txtBuscadorMaterial.Focus(); }
                else return;
                e.SuppressKeyPress = true;
            };

            var accentBar = new Panel
            {
                Size = new Size(3, 30),
                BackColor = item.ColorAcento,
                Location = new Point(5, 9)
            };

            var lblIcon = new Label
            {
                Text = item.Icono,
                Font = new Font("Segoe UI Emoji", 13f),
                Size = new Size(35, 48),
                Location = new Point(15, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblName = new Label
            {
                Text = item.NombreMostrar,
                Font = Tema.FuenteTexto,
                ForeColor = Tema.Texto,
                Size = new Size(400, 28),
                Location = new Point(55, 3),
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            var lblType = new Label
            {
                Text = item.InfoMostrar,
                Font = Tema.Fuente(9f),
                ForeColor = item.ColorAcento,
                Size = new Size(400, 18),
                Location = new Point(55, 28),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };

            var allControls = new Control[] { panel, lblIcon, lblName, lblType, accentBar };
            foreach (var ctrl in allControls)
            {
                ctrl.MouseEnter += (s, e) => panel.BackColor = Tema.Seleccion;
                ctrl.MouseLeave += (s, e) => panel.BackColor = Tema.Superficie;
                ctrl.Click += (s, e) => ItemClicked(item);
            }

            panel.Controls.Add(accentBar);
            panel.Controls.Add(lblIcon);
            panel.Controls.Add(lblName);
            panel.Controls.Add(lblType);

            return panel;
        }

        private void ItemClicked(ItemBuscador item)
        {
            OcultarResultados();

            isProgrammaticChange = true;
            if (item.EsMaterial)
            {
                _materialSeleccionado = item.Mat;
                txtBuscadorMaterial.Text = item.Mat.nombre;
                _recorteSeleccionado = null;

                int idCat = item.Mat.id_categoria;
                // Refrescar categoría desde DB en tiempo real para reflejar cambios inmediatos
                var cat = _categorias.FirstOrDefault(c => c.id == idCat);
                btnRecorte.Enabled = (cat != null && cat.recortable);
            }
            else
            {
                _materialSeleccionado = materialesFull.FirstOrDefault(m => m.id == item.Rec.id_material);
                txtBuscadorMaterial.Text = $"{item.Rec.nombre_material} (Recorte: {item.Rec.ancho}x{item.Rec.altura})";
                _recorteSeleccionado = item.Rec;
                btnRecorte.Enabled = false;
            }
            isProgrammaticChange = false;

            cantMatMov.Focus();
        }

        private void OcultarResultados()
        {
            if (_panelResultados != null)
            {
                _panelResultados.Visible = false;
                _panelResultados.Controls.Clear();
            }
        }

    }
}
