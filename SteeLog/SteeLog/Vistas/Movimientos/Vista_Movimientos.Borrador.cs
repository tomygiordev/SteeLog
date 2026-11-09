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
using SteeLog.CompositionRoot;
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
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Captura el estado actual del formulario y lo persiste como borrador en disco.
        /// Se llama desde el timer cada 30 segundos. No lanza excepciones.
        /// </summary>
        private void GuardarBorrador()
        {
            try
            {
                // Con resultado incierto (de esta vista o pendiente en el proceso) no se autoguarda hasta resolver.
                if (!catalogosListos || !modeloListo || ocupada || envioPendiente || resultadoIncierto || _hayInciertoSinResolver) return;
                // Si no hay nada cargado, no guardar un borrador vacío
                if (cmbMovimientos.SelectedIndex < 0 && materialesLista.Count == 0) return;

                GuardarBorradorActual(ConstruirDraft(null));
            }
            catch { /* silencioso */ }
        }

        private SteeLog.Core.Application.Borradores.MovimientoDraft ConstruirDraft(string marcadorIncierto)
        {
                var origen = cmbOrigen.SelectedItem as ItemCombo;
                var destino = cmbDestino.SelectedItem as ItemCombo;

                decimal ptDraft = CalcularPrecioTotalMateriales();

                var draft = MovimientoDraftService.CrearDesde(
                    fecha: guna2DateTimePicker1.Value,
                    nroRemito: nroRemitoTxt.Text.Trim(),
                    idTipoMov: cmbMovimientos.SelectedIndex >= 0 ? Convert.ToInt32(cmbMovimientos.SelectedValue) : 0,
                    nombreTipoMov: cmbMovimientos.Text,
                    idOrigen: origen?.IdReal ?? 0,
                    tipoOrigen: origen?.Tipo ?? "",
                    idDestino: destino?.IdReal ?? 0,
                    tipoDestino: destino?.Tipo ?? "",
                    idObra: cmbObras.SelectedIndex >= 0 ? Convert.ToInt32(cmbObras.SelectedValue) : 0,
                    costoFlete: decimal.TryParse(costoFleteTxt.Text.Replace(',', '.'),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal cf) ? cf : 0,
                    idFlete: (cmbFlete.SelectedValue != null && Convert.ToInt32(cmbFlete.SelectedValue) > 0) ? Convert.ToInt32(cmbFlete.SelectedValue) : (int?)null,
                    precioTotal: ptDraft,
                    observaciones: txtCategoria.Text,
                    materiales: materialesLista,
                    resultadoInciertoRemito: marcadorIncierto
                );
            return draft;
        }

        /// <summary>
        /// Escritura explícita y acotada (no depende del autoguardado, que queda pausado con un incierto):
        /// persiste el estado actual con el marcador de resultado incierto, o sin él (null) al resolverlo.
        /// </summary>
        private void PersistirBorradorConMarcador(string marcadorIncierto)
        {
            try
            {
                if (_idMovimientoAEditar > 0) return;
                if (cmbMovimientos.SelectedIndex < 0 && materialesLista.Count == 0) return;
                GuardarBorradorActual(ConstruirDraft(marcadorIncierto));
            }
            catch { /* silencioso */ }
        }

        /// <summary>
        /// Verifica si existe un borrador válido al abrir la vista y, si el usuario acepta,
        /// restaura todos los campos y la lista de materiales.
        /// </summary>
        private void IntentarRecuperarBorrador()
        {
            var draft = LeerBorrador();
            if (draft == null || (draft.Materiales.Count == 0 && draft.IdTipoMovimiento == 0)) { _hayInciertoSinResolver = false; return; }

            string hace = "";
            var edad = DateTime.Now - draft.FechaGuardado;
            if (edad.TotalMinutes < 60)
                hace = $"hace {(int)edad.TotalMinutes} min";
            else
                hace = $"hace {(int)edad.TotalHours}h";

            var resultado = ConfirmarRecuperacion != null ? (ConfirmarRecuperacion(draft) ? DialogResult.Yes : DialogResult.No) : Dialogo.Mostrar(
                this,
                $"Se encontró un movimiento sin guardar ({hace}).\n\n" +
                $"Tipo: {draft.NombreTipoMovimiento}\n" +
                $"Materiales: {draft.Materiales.Count}\n\n" +
                "¿Desea recuperarlo?",
                "Borrador encontrado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (resultado != DialogResult.Yes)
            {
                DescartarBorrador();
                _hayInciertoSinResolver = false;
                return;
            }

            try
            {
                isProgrammaticChange = true;

                // Fecha y remito
                guna2DateTimePicker1.Value = draft.Fecha == default ? DateTime.Now : draft.Fecha;
                nroRemitoTxt.Text = draft.NroRemito ?? "";

                // Tipo de movimiento
                if (draft.IdTipoMovimiento > 0)
                    cmbMovimientos.SelectedValue = draft.IdTipoMovimiento;

                // Origen
                if (draft.IdOrigen > 0)
                {
                    foreach (var item in cmbOrigen.Items)
                    {
                        if (item is ItemCombo ic && ic.IdReal == draft.IdOrigen && ic.Tipo == draft.TipoOrigen)
                        {
                            cmbOrigen.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Destino
                if (draft.IdDestino > 0)
                {
                    foreach (var item in cmbDestino.Items)
                    {
                        if (item is ItemCombo ic && ic.IdReal == draft.IdDestino && ic.Tipo == draft.TipoDestino)
                        {
                            cmbDestino.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Obra
                if (draft.IdObra > 0)
                    cmbObras.SelectedValue = draft.IdObra;

                // Flete
                if (draft.IdFlete.HasValue && draft.IdFlete.Value > 0)
                {
                    EstablecerFletes(draft.IdFlete.Value);
                }
                else
                {
                    cmbFlete.SelectedIndex = 0;
                }

                // Costo flete y observaciones
                costoFleteTxt.Text = draft.CostoFlete > 0 ? draft.CostoFlete.ToString("0.##").Replace('.', ',') : "";
                if (txtPrecioGeneral != null)
                {
                    txtPrecioGeneral.Text = draft.PrecioTotal > 0 ? draft.PrecioTotal.ToString("0.##").Replace('.', ',') : "";
                }
                txtCategoria.Text = draft.Observaciones ?? "";

                // Materiales
                foreach (var md in draft.Materiales)
                {
                    var item = new MovimientoMaterial
                    {
                        id_material = md.IdMaterial,
                        NombreMaterial = md.NombreMaterial,
                        NombreMaterialEventual = md.NombreMaterialEventual,
                        cantidad = md.Cantidad,
                        EsRecorte = md.EsRecorte,
                        IdRecorte = md.IdRecorte,
                        AnchoRecorte = md.AnchoRecorte,
                        AlturaRecorte = md.AlturaRecorte,
                        MedidaRecorte = md.MedidaRecorte,
                        EsInsumo = md.EsInsumo,
                        IdInsumo = md.IdInsumo,
                        EsCompraEventual = md.EsCompraEventual,
                        precio_unitario = md.PrecioUnitario
                    };
                    materialesLista.Add(item);
                    AgregarItemLista(item);
                }

                NotificarGuardado("Borrador recuperado correctamente.");
            }
            catch
            {
                AdvertirErrorCarga("No se pudo restaurar el borrador completamente.");
            }
            finally
            {
                isProgrammaticChange = false;
            }
            // Si el borrador trae el marcador persistido (o quedó un incierto sin resolver en este proceso), no se
            // puede reenviar sin la misma confirmación, aunque la restauración haya sido parcial. El marcador
            // persistido sobrevive al reinicio de la aplicación.
            if ((draft.ResultadoInciertoRemito != null || _hayInciertoSinResolver) && !resultadoIncierto)
            {
                resultadoIncierto = true; versionCatalogosAlMarcarIncierto = versionCatalogos;
                remitoIncierto = draft.ResultadoInciertoRemito ?? _remitoInciertoSinResolver; _hayInciertoSinResolver = true; _remitoInciertoSinResolver = remitoIncierto; SetOcupada(ocupada);
                BeginInvoke(new Action(() => { if (resultadoIncierto) ResolverResultadoIncierto(remitoIncierto, true); }));
            }
        }

    }
}
