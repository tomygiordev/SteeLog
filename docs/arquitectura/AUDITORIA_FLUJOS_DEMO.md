# Auditoría de flujos para demostración — 7/11/2026

Objetivo vigente: que el software sea presentable y los flujos funcionen sin fallar durante una demostración. Esta pasada estática y automatizada reduce riesgos concretos, pero no certifica ese objetivo completo.

## Método y restricciones

Implementadores Terra 6.1 high; root revisó diff, devolvió correcciones incompletas y ejecutó integración. Sin automatización de desktop, Main ni foreground, por indicación del usuario. Regresiones de vistas reales STA sin mostrarlas y callbacks de notificación sustituidos; sin esquema ni bases existentes modificados, push o Impeccable. Plan académico original intacto.

## Correcciones aceptadas

| Flujo | Defecto y corrección | Commit |
|---|---|---|
| Actualizaciones | Continuación tardía podía publicar/abrir modal con propietario dispuesto; single-flight, guardas de cierre y disposal del modal. | db4320e |
| Movimientos/borrador | Carrito seguía reenviable mientras se recargaba stock o generaba PDF; se retira al confirmar. Guarda propia de envío bloquea clic/autosave entre liberación del presenter y continuación. Cierre confirmado retira solo borrador de la versión enviada. GUID opcional distingue guardados con mismo reloj y conserva lectura legacy; borrado fallido se informa sin presentar la escritura como fallida. | f7efb17 |
| Materiales | Rango vacío 0/0 del modal se interpretaba como stock exactamente cero; se traduce sentinel a null. | 016046e |
| Conteos | Crear confirmado y leer fallido permitía otra creación; error de carga acumulativa permitía reenvío sin verificar. Se bloquea hasta F5 y se distingue confirmación de fallo posterior; navegación/refresco bloqueados durante escritura. | e24abe8 |
| Órdenes | Callback de navegación fallido tras commit se mostraba como fallo de escritura. Alta limpia antes de callback y recepción no se puede reconfirmar accidentalmente; aviso distingue navegación. UNIQUE ya impide duplicar mismo número. | 8421127 |
| Insumos | Homónimos mezclaban historiales y eran indistinguibles; SQL/DTO/filtro usan ID y etiquetas repetidas incluyen #ID. Fallo no cancelación durante commit se clasifica incierto y bloquea escrituras hasta recarga exitosa. | 7189350 |
| Canaletas | Limpiar selección cambiaba versión y silenciaba fallo de recarga postcommit; ahora se advierte sin repetir escritura. Root exigió pruebas de vista, no solo helper. | 113fd08 |
| Reporte stock | Aplicar filtros avanzados ignoraba controles visibles y rango vacío se volvía límite cero. Se combinan filtros y se normalizan límites. | aa158f0 |
| Informe obra | Nombre válido como A/B producía ruta inválida y timestamp por segundo podía colisionar. Ruta temporal GUID sin nombre de usuario. | 2953496 |
| Kardex | Acciones y error PDF podían usar identidad actual sobre datos de sesión anterior. Se vinculan a identidad de filas y solicitud vigente. | 9684530 |

## Evidencia de root

- Primer lote: 291/291 Debug y Release más cuatro familias legacy, exit 0. TEMP `steelog-auditoria-flujos-lote1.log`.
- Intento intermedio: 302/304 Debug, con dos fallos reales de espera de inicio de renderer en fixtures de Conteo y ReporteStock. No se contó como PASS. Se reemplazó Thread.Join bloqueante por espera async TCS en fixtures vulnerables y se garantizó liberación del renderer en finally; sin reintentos, aumento de timeouts ni deshabilitar paralelismo global. La contención del pool es una hipótesis, no causa probada por ese log.
- Suite final: 305/305 Debug y Release y cuatro familias legacy, exit 0. TEMP `steelog-auditoria-flujos-final.log`. La última edición de fixture Kardex coincidió con la primera compilación Debug; por eso se recompiló y repitió **todo Debug con fuentes congeladas**, 305/305, sin omitidos: `steelog-auditoria-flujos-debug-congelado.log`. Release ya incluía esas fuentes.
- SQL demo: 100/100, cero omitidos, siete TRX contrastados por root: órdenes 6, inventario 41, conteos 15, canaletas 14, catálogos 4, herramientas 8, reportes 12. Incluye nuevo caso de insumos homónimos. TEMP `steelog-auditoria-flujos-sql.log`; resultados `steelog-demo-bootstrap-ee6aca9abf7f4c73b634ae4ae4a0e923/results`. Esquema/seed nuevos, persistencia tras reinicio y rechazo de reinstalación. Recursos propios eliminados y root comprobó HTTP404 de contenedor y volumen.

## Límites y próximo trabajo

- Objetivo amplio sigue activo; no se afirma que todos los flujos estén auditados ni que una ejecución interactiva no pueda fallar. F9 integral conserva GUI/Galpón, Designer y recorrido con Internet efectivamente desconectado pendientes.
- La reconciliación de conteos/insumos no es idempotencia general. Cancelación durante commit conserva contratos actuales; requiere revisión específica junto con resultados inciertos de movimientos y órdenes.
- Comparación y borrado de borrador no son atómicos entre procesos; no se certifica uso concurrente de dos aplicaciones. No se garantiza exclusión entre sesiones que crean conteos.
- Revisar más profundamente recuperación ante caída de DB, ciclos completos de materiales/recortes, exportaciones Excel y navegación cruzada. Los tests STA no sustituyen aceptación visual, teclado real, Designer ni legibilidad de PDFs del recorrido completo.