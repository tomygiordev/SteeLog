using SteeLog.UI;
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
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SteeLog.Vistas.Movimientos
{
    public partial class Vista_Movimientos
    {
        private void btnRecorte_Click(object sender, EventArgs e)
        {
            if (_materialSeleccionado == null) return;

            int idMat = _materialSeleccionado.id;
            string nomMat = _materialSeleccionado.nombre;

            using (var modal = new ModalRecortes(servicios, idMat, nomMat, ValidarRecorteParaCarrito))
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    // Extraer selección del modal
                    var recorte = modal.RecorteSeleccionado;
                    decimal cantidad = modal.CantidadSeleccionada;

                    _idRecorteSeleccionado = recorte.id;
                    _anchoRecorteSeleccionado = recorte.ancho;
                    _alturaRecorteSeleccionado = recorte.altura;
                    _medidaRecorteSeleccionado = recorte.medida;

                    // Verificar si ya existe para acumular
                    MovimientoMaterial existente = null;
                    if (recorte.id > 0)
                    {
                        existente = materialesLista.FirstOrDefault(m =>
                            m.id_material == idMat &&
                            m.EsRecorte == true &&
                            m.IdRecorte == recorte.id);
                    }
                    else
                    {
                        existente = materialesLista.FirstOrDefault(m =>
                            m.id_material == idMat &&
                            m.EsRecorte == true &&
                            (!m.IdRecorte.HasValue || m.IdRecorte == 0) &&
                            m.AnchoRecorte == recorte.ancho &&
                            m.AlturaRecorte == recorte.altura &&
                            m.MedidaRecorte == recorte.medida);
                    }

                    if (existente != null)
                    {
                        existente.cantidad += cantidad;
                        ActualizarCantidadEnUI(existente);
                    }
                    else
                    {
                        string detalleDim = recorte.medida > 0
                            ? $" (R: {recorte.medida.ToString("0.##", CultureInfo.InvariantCulture)}m)"
                            : $" (R: {recorte.ancho.ToString("0.##", CultureInfo.InvariantCulture)}x{recorte.altura.ToString("0.##", CultureInfo.InvariantCulture)})";

                        MovimientoMaterial nuevoItem = new MovimientoMaterial
                        {
                            id_material = idMat,
                            NombreMaterial = nomMat + detalleDim,
                            cantidad = cantidad,
                            EsRecorte = true,
                            IdRecorte = recorte.id > 0 ? (int?)recorte.id : null,
                            AnchoRecorte = recorte.ancho,
                            AlturaRecorte = recorte.altura,
                            MedidaRecorte = recorte.medida > 0 ? recorte.medida : recorte.ancho
                        };

                        materialesLista.Add(nuevoItem);
                        AgregarItemLista(nuevoItem);
                    }

                    // Limpiar selección de material y cantidad
                    _materialSeleccionado = null;
                    isProgrammaticChange = true;
                    txtBuscadorMaterial.Text = "";
                    isProgrammaticChange = false;
                    cantMatMov.Text = "";
                    _recorteSeleccionado = null;
                    _idRecorteSeleccionado = 0;
                    _anchoRecorteSeleccionado = 0;
                    _alturaRecorteSeleccionado = 0;
                    _medidaRecorteSeleccionado = 0;
                    btnRecorte.Enabled = false;
                }
            }
        }

        private void btnInsumo_Click(object sender, EventArgs e)
        {
            using (var modal = new ModalInsumos(servicios))
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    var insumo = modal.InsumoSeleccionado;

                    // Buscar si ya existe para acumular
                    var existente = materialesLista.FirstOrDefault(m => m.EsInsumo && m.IdInsumo == insumo.id);
                    if (existente != null)
                    {
                        existente.cantidad += 1;
                        ActualizarCantidadEnUI(existente);
                    }
                    else
                    {
                        MovimientoMaterial nuevoItem = new MovimientoMaterial
                        {
                            id_material = 0,
                            NombreMaterial = insumo.nombre,
                            cantidad = 1,
                            EsInsumo = true,
                            IdInsumo = insumo.id
                        };

                        materialesLista.Add(nuevoItem);
                        AgregarItemLista(nuevoItem);
                    }
                }
            }
        }

        private void ActualizarCantidadEnUI(MovimientoMaterial item)
        {
            foreach (Control ctrl in flpMateriales.Controls)
            {
                if (ctrl is Panel p && p.Tag == item)
                {
                    TableLayoutPanel tbl = p.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                    if (tbl != null)
                    {
                        Label lblCant = tbl.GetControlFromPosition(2, 0) as Label;
                        if (lblCant != null) lblCant.Text = item.cantidad.ToString("G29", CultureInfo.InvariantCulture);
                    }
                    break;
                }
            }
        }

        private decimal StockConReversion(decimal stock, int id, Dictionary<int, decimal> snapshot)
        {
            return _idMovimientoAEditar > 0 && snapshot.TryGetValue(id, out decimal devuelto)
                ? stock + devuelto : stock;
        }

        private decimal StockDisponibleMaterial(int idMaterial)
        {
            return StockConReversion(_materialSeleccionado?.stockActual ?? 0, idMaterial, stockRevertidoMateriales);
        }

        private decimal StockDisponibleRecorte(int idRecorte)
        {
            decimal stock = _recorteSeleccionado?.cantidad ?? 0;
            if (_recorteSeleccionado == null && _idMovimientoAEditar > 0)
                stock = recortesDisponibles.FirstOrDefault(r => r.id == idRecorte)?.cantidad ?? 0;
            return StockConReversion(stock, idRecorte, stockRevertidoRecortes);
        }

        private bool ConsumeStockActual()
        {
            string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
            return ClasificadorMovimiento.Clasificar(cmbMovimientos.Text, tipoDestino) == ImpactoStock.Salida;
        }

        private string MensajeStockMaterialInsuficiente(decimal disponible)
        {
            return servicios.Sesion.DebeOcultarCantidadesStockMateriales
                ? "Stock insuficiente para agregar ese material."
                : $"Stock insuficiente. Disponible: {disponible}";
        }

        private string MensajeStockRecorteInsuficiente(decimal disponible)
        {
            return servicios.Sesion.DebeOcultarCantidadesStockMateriales
                ? "Stock de recorte insuficiente."
                : $"Stock de recorte insuficiente. Disponible: {disponible.ToString("G29")}";
        }

        /// <summary>
        /// Stock que queda disponible de un material descontando lo que ya hay de él en el carrito
        /// (sin contar <paramref name="excluir"/>, el ítem que se está reemplazando).
        /// En edición se suma la reversión del movimiento original, no la del carrito.
        /// </summary>
        private decimal DisponibleRestanteMaterial(int idMaterial, decimal stock, MovimientoMaterial excluir = null)
        {
            decimal enCarrito = materialesLista
                .Where(m => !ReferenceEquals(m, excluir) && !m.EsRecorte && !m.EsInsumo && !m.EsCompraEventual && m.id_material == idMaterial)
                .Sum(m => m.cantidad);
            return StockConReversion(stock, idMaterial, stockRevertidoMateriales) - enCarrito;
        }

        private decimal DisponibleRestanteRecorte(Recorte recorte, decimal stock, MovimientoMaterial excluir = null)
        {
            decimal enCarrito = materialesLista
                .Where(m => !ReferenceEquals(m, excluir) && !m.EsInsumo && m.EsRecorte && m.IdRecorte == recorte.id)
                .Sum(m => m.cantidad);
            return StockConReversion(stock, recorte.id, stockRevertidoRecortes) - enCarrito;
        }

        /// <summary>
        /// Valida, para movimientos que consumen stock, que sumar <paramref name="cantidad"/> del recorte
        /// al carrito no supere lo disponible. Devuelve el aviso a mostrar o null si es válido.
        /// Un recorte nuevo (id 0) no se valida: nace con lo que se carga y el motor lo consume.
        /// </summary>
        private string ValidarRecorteParaCarrito(Recorte recorte, decimal cantidad)
        {
            if (recorte == null || recorte.id <= 0 || !ConsumeStockActual()) return null;
            decimal disponible = DisponibleRestanteRecorte(recorte, recorte.cantidad);
            if (cantidad > disponible) return MensajeStockRecorteInsuficiente(Math.Max(0m, disponible));
            return null;
        }

        /// <summary>
        /// Detalle de los ítems del carrito que superan el stock conocido (solo movimientos que consumen stock).
        /// Los ítems cuyo stock no se conoce, los eventuales y los insumos no se validan.
        /// En edición el disponible incluye la reversión original.
        /// </summary>
        private List<string> ItemsQueExcedenStock()
        {
            var excedidos = new List<string>();
            if (!ConsumeStockActual()) return excedidos;
            bool ocultar = servicios.Sesion.DebeOcultarCantidadesStockMateriales;

            foreach (var grupo in materialesLista
                .Where(m => !m.EsRecorte && !m.EsInsumo && !m.EsCompraEventual && m.id_material.HasValue)
                .GroupBy(m => m.id_material.Value))
            {
                var material = materialesFull.FirstOrDefault(x => x.id == grupo.Key);
                if (material == null) continue;
                decimal cargado = grupo.Sum(m => m.cantidad);
                decimal disponible = StockConReversion(material.stockActual, grupo.Key, stockRevertidoMateriales);
                if (cargado > disponible)
                    excedidos.Add(ocultar ? grupo.First().NombreMaterial : $"{grupo.First().NombreMaterial} (cargado {cargado.ToString("G29")}, disponible {disponible.ToString("G29")})");
            }

            // Los recortes nuevos (sin id) no se validan: nacen con lo que se carga.
            foreach (var grupo in materialesLista
                .Where(m => m.EsRecorte && !m.EsInsumo && !m.EsCompraEventual && m.IdRecorte.HasValue && m.IdRecorte.Value > 0)
                .GroupBy(m => m.IdRecorte.Value))
            {
                var primero = grupo.First();
                var recorte = recortesDisponibles.FirstOrDefault(x => x.id == grupo.Key);
                if (recorte == null) continue;
                decimal cargado = grupo.Sum(m => m.cantidad);
                decimal disponible = StockConReversion(recorte.cantidad, grupo.Key, stockRevertidoRecortes);
                if (cargado > disponible)
                    excedidos.Add(ocultar ? primero.NombreMaterial : $"{primero.NombreMaterial} (cargado {cargado.ToString("G29")}, disponible {disponible.ToString("G29")})");
            }
            return excedidos;
        }

        /// <summary>Avisa (sin tocar el carrito) si algún ítem supera el stock. Devuelve true si hay excesos.</summary>
        private bool AdvertirCarritoExcedeStock(string prefijo)
        {
            var excedidos = ItemsQueExcedenStock();
            if (excedidos.Count == 0) return false;
            string detalle = string.Join("; ", excedidos.Take(3));
            if (excedidos.Count > 3) detalle += $" y {excedidos.Count - 3} más";
            AdvertirGuardado($"{prefijo} Stock insuficiente en: {detalle}. Corregí las cantidades del carrito.");
            return true;
        }

        private void chkCompraEventual_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCompraEventual.Checked)
            {
                // Visual feedback for Eventual Purchase
                txtBuscadorMaterial.BorderColor = Tema.Advertencia;
                txtBuscadorMaterial.PlaceholderText = "Escribir nombre de material eventual...";
                txtBuscadorMaterial.FillColor = Color.FromArgb(48, 44, 34);
                txtBuscadorMaterial.ForeColor = Color.FromArgb(240, 205, 130);

                // Disable search behavior (if implemented via autocomplete or similar)
                if (!isProgrammaticChange)
                {
                    _materialSeleccionado = null;
                    _recorteSeleccionado = null;
                    _idRecorteSeleccionado = 0;
                    _anchoRecorteSeleccionado = 0;
                    _alturaRecorteSeleccionado = 0;
                    _medidaRecorteSeleccionado = 0;
                }
                btnRecorte.Enabled = false;
            }
            else
            {
                // Restore standard look
                txtBuscadorMaterial.BorderColor = Tema.Borde;
                txtBuscadorMaterial.PlaceholderText = "Buscar material...";
                txtBuscadorMaterial.FillColor = Tema.FondoInput;
                txtBuscadorMaterial.ForeColor = Tema.Texto;
            }

            if (!isProgrammaticChange && chkCompraEventual.Checked && _idMovimientoAEditar == 0 && panelSeleccionado != null)
                DeseleccionarItem();

            ActualizarVisibilidadPrecios();
        }

        private void btnAgregarMaterial_Click(object sender, EventArgs e)
        {
            decimal cantidadVal = 0;
            // Acepta tanto "1.5" como "1,5" normalizando a InvariantCulture
            string cantTexto = (cantMatMov.Text ?? "").Trim().Replace(',', '.');
            decimal.TryParse(cantTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadVal);

            bool esCompraEventual = chkCompraEventual.Checked;
            string materialTexto = txtBuscadorMaterial.Text.Trim();

            if (cantidadVal <= 0)
            {
                ToastNotification.ShowWarning("Ingrese una cantidad válida.");
                return;
            }

            bool tieneRecorteSel = _recorteSeleccionado != null || _idRecorteSeleccionado > 0 || _medidaRecorteSeleccionado > 0 || (_anchoRecorteSeleccionado > 0 && _alturaRecorteSeleccionado > 0);

            // Evitar pérdida silenciosa: la columna cantidad es entera en la base para materiales normales.
            // Si el usuario ingresa un decimal (ej: 2,75) se perdería la parte fraccional.
            // Los recortes y compras eventuales aceptan decimales (ej: 0.5, 1.25).
            if (!tieneRecorteSel && !esCompraEventual && cantidadVal != Math.Floor(cantidadVal))
            {
                var resp = Dialogo.Mostrar(this,
                    $"La cantidad '{cantidadVal.ToString(CultureInfo.InvariantCulture)}' tiene decimales y se guardará redondeada a {Math.Round(cantidadVal, MidpointRounding.AwayFromZero)}.\n\n¿Continuar?",
                    "Cantidad con decimales",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (resp != DialogResult.Yes) return;
                cantidadVal = Math.Round(cantidadVal, MidpointRounding.AwayFromZero);
            }

            if (esCompraEventual && string.IsNullOrEmpty(materialTexto))
            {
                ToastNotification.ShowWarning("Ingrese el nombre del material eventual.");
                return;
            }
            if (!esCompraEventual && _materialSeleccionado == null)
            {
                ToastNotification.ShowWarning("Seleccione material de la lista desde el buscador.");
                return;
            }

            int? idMaterial = _materialSeleccionado?.id;
            Material materialInfo = _materialSeleccionado;
            decimal cantidad = cantidadVal;

            MovimientoMaterial itemAnterior = null;
            if (panelSeleccionado != null && panelSeleccionado.Tag is MovimientoMaterial)
            {
                itemAnterior = (MovimientoMaterial)panelSeleccionado.Tag;
                var confirmEdit = Dialogo.Mostrar(this,
                    "¿Estás seguro que querés editar este material?",
                    "Confirmar edición",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (confirmEdit != DialogResult.Yes) return;
            }

            int? idRec = _recorteSeleccionado?.id ?? (_idRecorteSeleccionado > 0 ? (int?)_idRecorteSeleccionado : null);
            decimal? anchoRec = _recorteSeleccionado != null ? (decimal?)_recorteSeleccionado.ancho : (_anchoRecorteSeleccionado > 0 ? (decimal?)_anchoRecorteSeleccionado : null);
            decimal? alturaRec = _recorteSeleccionado != null ? (decimal?)_recorteSeleccionado.altura : (_alturaRecorteSeleccionado > 0 ? (decimal?)_alturaRecorteSeleccionado : null);
            decimal? medidaRec = _recorteSeleccionado != null ? (_recorteSeleccionado.medida > 0 ? _recorteSeleccionado.medida : _recorteSeleccionado.ancho) : (_medidaRecorteSeleccionado > 0 ? (decimal?)_medidaRecorteSeleccionado : (_anchoRecorteSeleccionado > 0 ? (decimal?)_anchoRecorteSeleccionado : null));

            if (!tieneRecorteSel && materialInfo != null)
            {
                // En edición el disponible incluye solo la reversión original
                decimal stockDisponible = idMaterial.HasValue ? StockDisponibleMaterial(idMaterial.Value) : 0;
                decimal yaEnLista = materialesLista.Where(m => m.id_material == idMaterial && !m.EsRecorte).Sum(m => m.cantidad);

                // Si estamos modificando el mismo ítem, restamos su cantidad anterior para no duplicarla en la suma
                if (itemAnterior != null && itemAnterior.id_material == idMaterial && !itemAnterior.EsRecorte)
                {
                    yaEnLista -= itemAnterior.cantidad;
                }

                decimal totalSolicitado = yaEnLista + cantidad;

                string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
                bool consumeStock = ClasificadorMovimiento.Clasificar(cmbMovimientos.Text, tipoDestino) == ImpactoStock.Salida;

                if (consumeStock && totalSolicitado > stockDisponible)
                {
                    ToastNotification.ShowWarning(MensajeStockMaterialInsuficiente(stockDisponible));
                    return;
                }
            }
            else if (tieneRecorteSel)
            {
                string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
                bool consumeStock = ClasificadorMovimiento.Clasificar(cmbMovimientos.Text, tipoDestino) == ImpactoStock.Salida;

                if (idRec.HasValue && idRec.Value > 0)
                {
                    decimal stockDisponibleRec = StockDisponibleRecorte(idRec.Value);
                    decimal yaEnLista = materialesLista.Where(m => m.EsRecorte && m.IdRecorte == idRec.Value).Sum(m => m.cantidad);

                    if (itemAnterior != null && itemAnterior.EsRecorte && itemAnterior.IdRecorte == idRec.Value)
                    {
                        yaEnLista -= itemAnterior.cantidad;
                    }

                    decimal totalSolicitado = yaEnLista + cantidad;
                    if (consumeStock && totalSolicitado > stockDisponibleRec)
                    {
                        ToastNotification.ShowWarning(MensajeStockRecorteInsuficiente(stockDisponibleRec));
                        return;
                    }
                }
            }

            idMaterial = esCompraEventual ? (int?)null : _materialSeleccionado?.id;
            string nombreMaterial = materialInfo?.nombre ?? materialTexto;

            decimal precioUnitario = 0;
            if (esCompraEventual && txtPrecioMaterial != null)
            {
                NumeroDecimal.TryParse(txtPrecioMaterial.Text.Trim(), out precioUnitario);
            }

            string detalleDim = "";
            if (tieneRecorteSel)
            {
                detalleDim = (medidaRec.HasValue && medidaRec.Value > 0)
                    ? $" (R: {medidaRec.Value.ToString("0.##", CultureInfo.InvariantCulture)}m)"
                    : $" (R: {anchoRec?.ToString("0.##", CultureInfo.InvariantCulture)}x{alturaRec?.ToString("0.##", CultureInfo.InvariantCulture)})";
            }

            MovimientoMaterial nuevoItem = new MovimientoMaterial
            {
                id_material = idMaterial,
                NombreMaterial = nombreMaterial + detalleDim,
                NombreMaterialEventual = esCompraEventual ? materialTexto : null,
                cantidad = cantidad,
                EsRecorte = tieneRecorteSel,
                IdRecorte = idRec,
                AnchoRecorte = anchoRec,
                AlturaRecorte = alturaRec,
                MedidaRecorte = medidaRec,
                EsCompraEventual = esCompraEventual,
                precio_unitario = precioUnitario
            };

            if (itemAnterior != null)
            {
                // Estamos editando un material existente en la lista
                int idx = materialesLista.IndexOf(itemAnterior);
                if (idx >= 0) materialesLista[idx] = nuevoItem;

                panelSeleccionado.Tag = nuevoItem;
                TableLayoutPanel tbl = panelSeleccionado.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                if (tbl != null)
                {
                    Label lblMat = tbl.GetControlFromPosition(0, 0) as Label;
                    if (lblMat != null)
                    {
                        string textoPrecio = "";
                        if (nuevoItem.EsCompraEventual && nuevoItem.precio_unitario > 0 && servicios.Sesion.Role == UserRole.Admin)
                        {
                            textoPrecio = $" (${nuevoItem.precio_unitario:N2})";
                        }
                        lblMat.Text = nuevoItem.NombreMaterial + textoPrecio;
                    }

                    Label lblTip = tbl.GetControlFromPosition(1, 0) as Label;
                    if (lblTip != null)
                    {
                        if (nuevoItem.EsRecorte) { lblTip.Text = "RECORTE"; lblTip.ForeColor = Tema.Advertencia; }
                        else if (nuevoItem.EsInsumo) { lblTip.Text = "INSUMO"; lblTip.ForeColor = Tema.Info; }
                        else if (nuevoItem.EsCompraEventual) { lblTip.Text = "EVENTUAL"; lblTip.ForeColor = Tema.Advertencia; }
                        else { lblTip.Text = ""; }
                    }

                    Label lblCant = tbl.GetControlFromPosition(2, 0) as Label;
                    if (lblCant != null) lblCant.Text = nuevoItem.cantidad.ToString("G29", CultureInfo.InvariantCulture);
                }

                DeseleccionarItem();
            }
            else
            {
                // Es uno nuevo - Verificar si ya existe en la lista para acumular
                var existente = materialesLista.FirstOrDefault(m =>
                    m.id_material == nuevoItem.id_material &&
                    m.NombreMaterialEventual == nuevoItem.NombreMaterialEventual &&
                    m.EsRecorte == nuevoItem.EsRecorte &&
                    (nuevoItem.EsRecorte && (!nuevoItem.IdRecorte.HasValue || nuevoItem.IdRecorte == 0)
                        ? (!m.IdRecorte.HasValue || m.IdRecorte == 0) && m.AnchoRecorte == nuevoItem.AnchoRecorte && m.AlturaRecorte == nuevoItem.AlturaRecorte && m.MedidaRecorte == nuevoItem.MedidaRecorte
                        : m.IdRecorte == nuevoItem.IdRecorte) &&
                    m.EsInsumo == nuevoItem.EsInsumo &&
                    m.EsCompraEventual == nuevoItem.EsCompraEventual &&
                    m.precio_unitario == nuevoItem.precio_unitario);

                if (existente != null)
                {
                    existente.cantidad += nuevoItem.cantidad;
                    ActualizarCantidadEnUI(existente);
                }
                else
                {
                    materialesLista.Add(nuevoItem);
                    AgregarItemLista(nuevoItem);
                }

                DeseleccionarItem();
                GuardarBorrador();
            }
        }

        private void DeseleccionarItem()
        {
            if (panelSeleccionado != null)
                panelSeleccionado.BackColor = Color.Transparent;
            panelSeleccionado = null;

            isProgrammaticChange = true;
            _materialSeleccionado = null;
            _recorteSeleccionado = null;
            _idRecorteSeleccionado = 0;
            _anchoRecorteSeleccionado = 0;
            _alturaRecorteSeleccionado = 0;
            _medidaRecorteSeleccionado = 0;
            txtBuscadorMaterial.Text = "";
            cantMatMov.Text = "";
            chkCompraEventual.Checked = cmbMovimientos.Text.ToUpper().Contains("COMPRA EVENTUAL");
            btnRecorte.Enabled = false;
            guna2Button1.Text = "Agregar material";
            isProgrammaticChange = false;

            ActualizarVisibilidadPrecios();

            // Regresar foco al buscador de materiales para agilizar el flujo de entrada
            _ultimoAvisoStockMax = null;
            txtBuscadorMaterial.Focus();
            // El GotFocus programático abre el aviso de mínimo sobre la lista: se oculta.
            OcultarResultados();
        }

        private void AgregarItemLista(MovimientoMaterial item)
        {
            // ⚠️ Los hijos de FlowLayoutPanel NO deben usar Dock: el FLP controla su posición.
            // Usamos Width explícito y un TableLayoutPanel interno para el layout de columnas.
            Panel pnlItem = new Panel();
            pnlItem.Margin = new Padding(0, 0, 0, 4);
            // Tomar el ancho actual del FLP; si aún es 0 (carga inicial), usamos 600 como fallback
            pnlItem.Width = flpMateriales.ClientSize.Width > 5
                ? flpMateriales.ClientSize.Width - 5
                : 600;
            pnlItem.Height = Tema.AltoFila + 4;
            pnlItem.Tag = item;
            pnlItem.Cursor = Cursors.Hand;
            pnlItem.BackColor = Color.Transparent;

            // Tabla interna: [nombre | etiqueta tipo | cantidad]
            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.ColumnCount = 3;
            tbl.RowCount = 1;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // nombre
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // etiqueta RECORTE/INSUMO
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));  // cantidad
            tbl.Padding = new Padding(8, 0, 8, 0);
            tbl.BackColor = Color.Transparent;

            Label lblMaterial = new Label();
            string textoPrecio = "";
            if (item.EsCompraEventual && item.precio_unitario > 0 && servicios.Sesion.Role == UserRole.Admin)
            {
                textoPrecio = $" (${item.precio_unitario:N2})";
            }
            lblMaterial.Text = item.NombreMaterial + textoPrecio;
            lblMaterial.Font = Tema.FuenteTexto;
            lblMaterial.ForeColor = Tema.Texto;
            lblMaterial.Dock = DockStyle.Fill;
            lblMaterial.TextAlign = ContentAlignment.MiddleLeft;
            lblMaterial.AutoEllipsis = true;
            tbl.Controls.Add(lblMaterial, 0, 0);

            Label lblTipo = new Label();
            lblTipo.Dock = DockStyle.Fill;
            lblTipo.TextAlign = ContentAlignment.MiddleCenter;
            lblTipo.Font = Tema.Fuente(9.5f, true);
            lblTipo.Padding = new Padding(4, 0, 4, 0);
            if (item.EsRecorte)       { lblTipo.Text = "RECORTE"; lblTipo.ForeColor = Tema.Advertencia; }
            else if (item.EsInsumo)   { lblTipo.Text = "INSUMO";  lblTipo.ForeColor = Tema.Info; }
            else if (item.EsCompraEventual) { lblTipo.Text = "EVENTUAL"; lblTipo.ForeColor = Tema.Advertencia; }
            tbl.Controls.Add(lblTipo, 1, 0);

            Label lblCantidad = new Label();
            lblCantidad.Text = item.cantidad.ToString("G29", CultureInfo.InvariantCulture);
            lblCantidad.Font = Tema.FuenteBoton;
            lblCantidad.ForeColor = Tema.AcentoHover;
            lblCantidad.Dock = DockStyle.Fill;
            lblCantidad.TextAlign = ContentAlignment.MiddleRight;
            tbl.Controls.Add(lblCantidad, 2, 0);

            pnlItem.Controls.Add(tbl);

            pnlItem.Click += (s, ev) => Item_Click(pnlItem, ev);
            tbl.Click      += (s, ev) => Item_Click(pnlItem, ev);
            lblMaterial.Click += (s, ev) => Item_Click(pnlItem, ev);
            lblTipo.Click     += (s, ev) => Item_Click(pnlItem, ev);
            lblCantidad.Click += (s, ev) => Item_Click(pnlItem, ev);

            flpMateriales.Controls.Add(pnlItem);
        }

        private void Item_Click(object sender, EventArgs e)
        {
            Panel clickedPanel = (Panel)sender;

            // Si clicamos el mismo que ya está seleccionado, lo deseleccionamos (cancelamos edición)
            if (panelSeleccionado == clickedPanel)
            {
                DeseleccionarItem();
                return;
            }

            // Deseleccionar el previo
            if (panelSeleccionado != null)
                panelSeleccionado.BackColor = Color.Transparent;

            panelSeleccionado = clickedPanel;
            panelSeleccionado.BackColor = Tema.Seleccion;
            guna2Button1.Text = "Editar material";

            // Cargar el material y cantidad en los campos del selector
            if (panelSeleccionado.Tag is MovimientoMaterial itemSel)
            {
                isProgrammaticChange = true;
                chkCompraEventual.Checked = itemSel.EsCompraEventual;

                // Buscar el material en la lista completa para tener el objeto completo
                var mat = itemSel.id_material.HasValue ? materialesFull.FirstOrDefault(m => m.id == itemSel.id_material) : null;

                _materialSeleccionado = mat;

                if (itemSel.EsCompraEventual)
                {
                    txtBuscadorMaterial.Text = itemSel.NombreMaterialEventual ?? itemSel.NombreMaterial;
                    btnRecorte.Enabled = false;
                    _recorteSeleccionado = null;
                    _idRecorteSeleccionado = 0;
                    _anchoRecorteSeleccionado = 0;
                    _alturaRecorteSeleccionado = 0;
                    _medidaRecorteSeleccionado = 0;
                }
                else if (mat != null)
                {
                    if (itemSel.EsRecorte)
                    {
                        // Para recortes mostramos el nombre con medida
                        txtBuscadorMaterial.Text = itemSel.NombreMaterial;
                        _idRecorteSeleccionado = itemSel.IdRecorte ?? 0;
                        _anchoRecorteSeleccionado = itemSel.AnchoRecorte ?? 0;
                        _alturaRecorteSeleccionado = itemSel.AlturaRecorte ?? 0;
                        _medidaRecorteSeleccionado = itemSel.MedidaRecorte ?? 0;
                    }
                    else
                    {
                        txtBuscadorMaterial.Text = mat.nombre;
                        // Refrescar categoría desde DB en tiempo real para reflejar cambios inmediatos
                        var cat = _categorias.FirstOrDefault(c => c.id == mat.id_categoria);
                        btnRecorte.Enabled = (cat != null && cat.recortable);
                        _idRecorteSeleccionado = 0;
                        _anchoRecorteSeleccionado = 0;
                        _alturaRecorteSeleccionado = 0;
                        _medidaRecorteSeleccionado = 0;
                    }

                }
                else
                {
                    // Caso material eventual antiguo sin boolean
                    txtBuscadorMaterial.Text = itemSel.NombreMaterial;
                    btnRecorte.Enabled = false;
                    _recorteSeleccionado = null;
                    _idRecorteSeleccionado = 0;
                    _anchoRecorteSeleccionado = 0;
                    _alturaRecorteSeleccionado = 0;
                    _medidaRecorteSeleccionado = 0;
                }

                cantMatMov.Text = itemSel.cantidad.ToString("G29", CultureInfo.InvariantCulture);

                if (itemSel.EsCompraEventual && txtPrecioMaterial != null)
                {
                    txtPrecioMaterial.Text = itemSel.precio_unitario > 0 ? itemSel.precio_unitario.ToString("0.##").Replace('.', ',') : "";
                }

                isProgrammaticChange = false;

                // Enfocar el campo de cantidad para edición inmediata
                cantMatMov.Focus();
                cantMatMov.SelectAll();
            }
        }

        private void EliminarMaterial_Click(object sender, EventArgs e)
        {
            if (panelSeleccionado != null)
            {
                MovimientoMaterial item = (MovimientoMaterial)panelSeleccionado.Tag;
                materialesLista.Remove(item);
                flpMateriales.Controls.Remove(panelSeleccionado);
                panelSeleccionado.Dispose();

                DeseleccionarItem();
                GuardarBorrador();
            }
            else
            {
                ToastNotification.ShowWarning("Seleccione un ítem para eliminar.");
            }
        }

        private decimal CalcularPrecioTotalMateriales()
        {
            decimal total = 0;
            foreach (var detalle in materialesLista.Where(m => !m.EsInsumo))
            {
                decimal precioUnitario = detalle.EsCompraEventual
                    ? detalle.precio_unitario
                    : materialesFull.FirstOrDefault(m => detalle.id_material.HasValue && m.id == detalle.id_material.Value)?.precioUnitario ?? 0;

                total += detalle.cantidad * precioUnitario;
            }

            return total;
        }

    }
}
