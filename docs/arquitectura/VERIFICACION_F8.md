# Verificación F8: Reportes, búsqueda y cierre de dependencias

**Estado:** F8 completada y aceptada por root en su alcance de implementación y verificación automatizada. Los ensayos manuales F0/F9 siguen pendientes.

## Continuidad y alcance

F8 se inició desde `419b9c5` y se interrumpió por cuota de Terra. Se reanudó desde `47473a9` con el WIP ya recuperado, preservando los commits de calidad `477c3d6` y `47473a9`. No se reaplicó ni eliminó el stash de respaldo ni se rehicieron fases cerradas. El plan principal permanece intacto.

Los implementadores fueron GPT Terra 6.1 High, con ownership separado de obra, Kardex, stock y búsqueda. No se utilizó agy. Root integró, revisó el diff real, escribió y ejecutó pruebas SQL adicionales, inspeccionó PDF renderizados y realizó el cierre. No se hizo push ni se inició F9.

## Implementación y decisiones

- R: consultas y DTOs de Kardex, obra y stock configurado en Application, implementaciones Npgsql en Infrastructure y presenters en el host. Las consultas abren conexiones propias, cancelables y liberadas; no hay repositorios/UoW retenidos por formularios. Las lecturas de un informe pueden observar instantes distintos: no se promete un snapshot transaccional de varias conexiones.
- R: se conservan filtros, fechas, orden, nombres, secciones y permisos de lectura Galpón/Admin. La edición de la configuración de stock requiere Admin en Application, como el acceso previo a esa función. No se agregó recepción de OC al Kardex ni se cambiaron las reglas de stock o conteos.
- R: el Kardex conserva saldos persistidos y la acumulación descendente de respaldo cuando no existe snapshot. Usa el clasificador de movimientos ya establecido, también para tipos no canónicos; no se reinterpretó un texto parecido a «aporte» como entrada de stock.
- R: el reporte de stock mantiene la selección y orden de secciones/materiales. Sin configuración, conserva la consulta de inventario y la prioridad por uso al filtrar stock bajo. Esa prioridad excluye materiales sin movimientos; no se cambió silenciosamente por una lista de todos los saldos bajos.
- C: guardar la configuración completa de stock es una operación Application con una UoW. La infraestructura toma bloqueos transaccionales de las dos tablas, cambia nombres temporales para permitir intercambios y guarda secciones/asignaciones en conjunto. Fallo o cancelación previa al commit revierte el conjunto; dos guardados no entremezclan sus filas. Se mantiene última escritura confirmada, sin prometer resolución de conflictos de edición por versión.
- R/C: validación de solicitudes, permisos y clasificación de commit incierto en Application. La cancelación al entrar en `CommitAsync`, antes del commit real, no se clasifica como confirmación incierta, respetando la corrección F7. No hay reenvíos automáticos.
- R: valorización pura en `Domain.Reportes.ValorizacionObra`, con puente de DTOs en Application. Resumen, subtotales, importes por fila, fallback de compra eventual y balance de materiales ya no se calculan en el formulario. Se conservan deliberadamente las diferencias legacy entre el universo usado por las tablas y el cálculo de resumen; no se reemplazaron por una fórmula nueva «uniforme».
- C: las lecturas UI de obra y el snapshot PDF comparten las tres consultas de movimientos/herramientas/insumos. Cada recorrido consulta canaletas una sola vez; la vista conserva su lectura con cantidad de fotos. Se eliminó una consulta duplicada introducida durante el WIP.

## PDF y presentación

Kardex y Reporte Stock tienen renderers completos en Infrastructure, incluidos pies de página, alimentados por DTOs e `IRecursosPdf`/`MarcaPdf`. No contienen SQL, sesión global ni referencias al host. Informe de obra conserva el renderer extraído en F6 y consume las reglas puras de valorización; su validación previa de `LogoPng`, corregida en `477c3d6`, permanece intacta. `Document` y `FileStream` se liberan también en los casos de error probados.

Se conservaron marca y estilos. La inspección visual detectó fechas partidas y falta de encabezados repetidos en Kardex: se ajustó únicamente el ancho/padding de fecha y se repiten sección/columnas en cada continuación. No es un rediseño. Las muestras finales muestran las fechas completas y el resumen con/sin saldo según el perfil. Las muestras de obra conservan total 828 (100 + 723 + 5), secciones y cabeceras repetidas.

Los constructores de vistas no consultan ni activan servicios. Cargas y exportaciones usan cancelación y versiones de contexto; se descartan respuestas obsoletas y se evita abrir el visor después de cerrar/cambiar el contexto. Se bloquean dobles exportaciones, guardados de configuración y eliminaciones desde Kardex. Esta última reutiliza el caso de uso F4, captura el ID antes de confirmar y no recarga ni avisa sobre un contexto nuevo. Los campos/precios ocultos siguen siendo una política visual legacy, no una promesa de confidencialidad de todos los DTOs.

Búsqueda conserva ranking y caché, carga DTOs mediante Application fuera del hilo UI y comparte una carga de caché en curso sin retener conexiones. El aviso de stock bajo y su callback se descartan al cerrar/disponer. El cierre es idempotente, incluso con `Close` y varios `Dispose`.

## Retiro legacy y arquitectura

Se retiraron los ocho adaptadores restantes de `Datos`, el renderer host de Kardex y sus modelos sin consumidores. `SortableBindingList` se movió físicamente a Presentation, conservando su namespace y utilidad. El wrapper de Informe de obra solo delega al presenter; ya no consulta ni calcula negocio. `Branding` conserva recursos e imágenes WinForms, pero no crea imágenes iText.

Las referencias directas Npgsql/iTextSharp del proyecto WinForms se retiraron; las dependencias de Infrastructure siguen disponibles mediante Costura. El test de arquitectura exige **cero dependencias prohibidas y cero excepciones**: el inventario pasó de **116 a 0 pares**. No se agregó ninguna excepción. Las pruebas verifican además la carga de los recursos embebidos Npgsql e iTextSharp.

Se migraron las aserciones útiles de los harness legacy: configuración prueba la fábrica real por la composición, incluidos errores contextualizados/sanitizados y liberación tras conexiones fallidas; branding genera los renderers reales con datos y verifica texto, imágenes, iconos y formularios; búsqueda mantiene sus seis comprobaciones. No se sustituyeron por pruebas de carpetas o archivos vacíos.

## Verificación propia de root

Los runners SQL usan clústeres PostgreSQL 17 propios y efímeros, tmpfs, puerto loopback dinámico y DB `steelog_test_<GUID>`. Comprueban identidad, sondas SQL válida exit 0 e inválida exit 1 mediante API Docker, DLL versionadas, copias TEMP sin bin/obj compartidos y TRX con total=executed=passed>0. F8 no agrega esquema, auto-DDL ni migraciones. Las migraciones F4 de sus runners existentes solo se aplican a su clúster de prueba.

| Suite | Resultado | Evidencia TEMP |
|---|---:|---|
| F8 reportes | 11/11, exit 0 | `steelog-root-f8-final-11-reportes.log`; `steelog-reportes-d91db14ec0194714881b94481fe6af9b/results/reportes.trx` |
| F7 catálogos/herramientas | 12/12, exit 0 | `steelog-root-f8-frozen-catalogos.log`; `steelog-catalogos-4cb06936eb034dbfbcc22d0f1c91c9bd/results/catalogos.trx` |
| F5 conteos | 15/15, exit 0 | `steelog-root-f8-frozen-conteos.log`; `steelog-conteos-77d3e63644a94001bc00af6e3a4eed03/results/conteos.trx` |
| F4 inventario | 40/40, exit 0 | `steelog-root-f8-frozen-inventario.log`; `steelog-inventario-239a0770891347c3a215be230392bfc1/results/inventario.trx` |
| F3 órdenes | 6/6, exit 0 | `steelog-root-f8-frozen-ordenes.log`; `steelog-ordenes-67774d3f381f4e8fbb1bd229eec5c335/results/ordenes.trx` |
| F6 canaletas | 14/14, exit 0 | `steelog-root-f8-frozen-canaletas.log`; `steelog-canaletas-44664900783a4939829d78ca0fbf8aac/results/canaletas.trx` |

La suite SQL F8 prueba filtros y cantidades/saldos Kardex, respaldo sin snapshot y tipos no canónicos, composición/fechas/roles/valorización de obra, fechas/pertenencia de herramientas e insumos, orden y baja de secciones, fallback sin configuración, permisos, duplicados, FK real, rollback tras SQL, cancelación y bloqueo/concurrencia observados en PostgreSQL. Sus pruebas de escritura usan datos reales de la DB efímera, no un inventario simulado.

Verificación local final: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal`, **exit 0, 224/224 Debug y 224/224 Release**, sin omitidos, y las cuatro familias legacy Configuration, search, Branding y LoginUsability aprobadas. Log `%TEMP%/steelog-root-f8-delivery-local.log`. Se volvió a ejecutar después de la última corrección de eliminación de Kardex y de las guardas de errores frente a cambios de identidad. El build de VS MSBuild y la ejecución de tests son pasos distintos; no se usó un build como sustituto de los tests. Las advertencias de campos usados por reflexión y otras advertencias previas no se presentan como una compilación sin warnings.

PDF propios de root: **9/9** en `steelog-root-f8-pdf-a8bff080086a4d12b93b683a650bf71c`, con TRX y logo real. Tras corregir Kardex: **3/3** adicionales en `steelog-root-f8-kardex-final-5af248b1a6c2462699a3287159b7f7c6`. Root renderizó con Poppler e inspeccionó las **24 páginas finales**: obra Admin 5/Galpón 3, stock 4/4 y Kardex 4/4, mediante páginas y hojas de contacto. No se aceptó únicamente extracción de texto. Los tests de Kardex comprueban también fechas completas y encabezados por página.

Los SQL se ejecutaron sobre el backend congelado; los últimos ajustes posteriores fueron exclusivamente lifecycle de UI y presentación de Kardex, cubiertos por la suite local final. Se comprobó que no quedaran contenedores `steelog-ca-test-*`, el allowlist como subconjunto estricto (116 → 0), UTF-8 estricto y ausencia de cambios del plan/esquema. Antes del commit se revisó el diff preparado y `git diff --cached --check`; solo se stagearon los archivos F8.

### Fallos intermedios que no se presentan como PASS

- `steelog-root-f8-final-local.log`: 219/219 Debug y Release, pero fallo de Branding por `Cancel` sobre CTS ya dispuesto en un segundo `Dispose` de VistaGeneral. Se corrigió y se agregó una regresión STA real; ese intento global no se consideró aprobado.
- `steelog-root-f8-final-reportes.log`: 9/10 por expectativa incompleta del fixture de stock bajo, que no tenía uso. Se contrastó el legacy, se agregó un movimiento real al fixture y se comprobó también la exclusión previa sin uso. No se cambió producción para hacer pasar la prueba.
- `steelog-root-f8-accepted-reportes.log`: 10/11 por clasificar erróneamente «APORTE» no canónico como entrada en el fixture. Se preservó el clasificador establecido y se caracterizaron entrada canónica, fallback por destino y salida; no se alteró la semántica existente.

## Límites y continuidad

No se ejecutaron Main, updater real, publicación ni servicios remotos; no se leyó configuración privada ni DPAPI, ni se modificaron bases, roles, volúmenes o fotos del usuario. Los contenedores de prueba fueron eliminados tras comprobar su identidad.

Las pruebas STA, SQL y PDF no sustituyen Designer interactivo, impresión física, recorrido manual de todas las pantallas, instalación limpia ni ensayo offline integral. Esos pendientes F0/F9 continúan abiertos. Antislop se aplicó `during` como filtro de conservación, sin Impeccable ni entregables artificiales.

El hook no confirmó identidad de runtime para Engram. Root no realizó escrituras ni actualizó la memoria canónica. Los resúmenes que subagentes informaron haber guardado no se consideran autoritativos ni sustituyen Git y esta evidencia. El próximo paso es F9, no reabrir F1–F8 ni aplicar automáticamente la migración F4 a instalaciones existentes.


## Auditoría posterior y correcciones de calidad (2026-11-04)

Base auditada: `7ac661a`, con árbol limpio. Se revisaron los contratos, consultas, renderers, presenters, ciclo de vida UI, pruebas y retiro de dependencias de F8. No se reabrieron fases anteriores ni se modificó el plan. La auditoría propia volvió a ejecutar 224/224 Debug y Release, cuatro familias legacy y 11/11 SQL F8. Esos PASS no descartaban los siguientes defectos, reproducidos con pruebas adversariales:

1. **P2 — aislamiento de PDF de prueba:** los nombres fijos directamente bajo TEMP podían sobreescribir y borrar archivos preexistentes. Un testigo propio desapareció al ejecutar el caso original. Ahora cada caso usa un subdirectorio GUID propio, también cuando se solicita evidencia; la limpieza queda limitada a su directorio. Root rechazó un primer test nuevo que volvía a tocar el nombre global: la regresión incorporada usa un directorio propio y la comprobación adversarial de root redirige TEMP/TMP de su proceso hijo a una carpeta exclusiva.
2. **P2 — edición durante carga de configuración:** el botón «Nueva sección» y el editor seguían activos antes de recibir el modelo, y la respuesta borraba los cambios. Root reprodujo una sección antes de responder y cero después. Se bloquean controles y handlers mientras falta el modelo o hay guardado; la recarga posterior a un guardado tampoco habilita edición anticipadamente. Un guardado confirmado se informa por separado del eventual error de recarga.
3. **P2 — avisos obsoletos:** los caminos de error de carga, guardado y exportación de stock no tenían las mismas guardas de identidad que los caminos exitosos. Root cambió A→B con una consulta pendiente y observó un aviso de A en B. Se verifican identidad, vigencia de solicitud, cancelación y cierre antes de avisar o reactivar el editor.
4. **P2 — totales Kardex por signo crudo:** un ENVÍO real con cantidad positiva 2 y saldo persistido 8 se mostraba como entrada +2 y salida 0. Este cálculo ya existía antes de F8: es una corrección funcional autorizada, no una regresión atribuida a la extracción. El resumen ahora aplica el clasificador establecido al tipo y al destino original, sin usar el nombre traducido de la obra. No se modifican cantidad original, saldo persistido, orden descendente, respaldo sin snapshot ni reglas de inventario. `SinImpacto` queda excluido; cantidades históricas negativas conservan dirección × cantidad, sin `Abs` silencioso. El resumen no se cachea sobre una lista mutable.

La implementación se delegó por CLI autenticado a `gemini-3.8-flash-high`, con ownership separado. La entrega PDF terminó; stock y Kardex alcanzaron el timeout de 30 minutos. La cuota consultada seguía disponible (46% semanal / 86% de cinco horas): no se informó agotamiento ni se sustituyó el modelo. Root revisó y completó los cambios parciales y volvió a delegar solamente las regresiones UI. Los reportes parciales de los agentes no se aceptaron como evidencia final.

### Verificación de las correcciones

- SQL propio de root: `scripts/tests/reportes-integracion.ps1`, **12/12**, sin omisiones, exit 0. Log `%TEMP%/steelog-f8-correcciones-root-sql.log`; TRX `steelog-reportes-43daf25284f84d32bddd7fcf6e15e18d/results/reportes.trx`. Incluye PostgreSQL→consulta→snapshot→renderer real para ENVÍO +2, resumen entradas 0/salidas -2 y saldo 8. Su PNG mínimo prueba el cálculo, no la marca; los PDF de presentación usan el logo real del host.
- PDF propio de root: **10/10** con TEMP/TMP exclusivos y los cuatro archivos testigo intactos, más **4/4** exportaciones de evidencia. TRX en `steelog-f8-correcciones-pdf-38f4cdba4954423f8536758f2bfe5c8d`. Se renderizaron e inspeccionaron **16 páginas**, Kardex y stock Admin/Galpón, con logo real, fechas completas, encabezados repetidos y columnas conservadas. El Kardex de muestra muestra entradas +110 y salidas -55, con cantidades originales positivas. Poppler emitió avisos de fuentes Symbol/ArialUnicode; la inspección visual no encontró cortes ni pérdida de contenido.
- Sonda STA independiente de root en `steelog-f8-correcciones-sta-6521ea74e12f4a5eb06d18988a2f5ca4/audit-sta.log`: botón/editor deshabilitados durante carga, cero secciones agregadas prematuramente y **cero avisos de identidad anterior**.
- Un primer intento local de root tuvo 246/249: tres fallos de PDF porque se habían agregado expectativas de totales antes de adaptar efectivamente el fixture multilínea. Se corrigió el fixture a entradas y envíos canónicos positivos; ese intento no se presenta como aprobado.

No se cambiaron esquema ni scripts de migración, ni se tocó almacenamiento o DB del usuario. El allowlist permanece en cero. Designer interactivo, recorrido manual integral, instalación y ensayo offline siguen pendientes de F0/F9. Engram continúa sin identidad de runtime confirmada: no se realizaron escrituras atribuidas ni se fabricó un cierre de memoria.

#### Resultado final del seguimiento

- `scripts/verificar.ps1`: **258/258 Debug y 258/258 Release**, exit 0, sin omitidos, sobre código y pruebas congelados. Log `%TEMP%/steelog-f8-correcciones-root-congelado-local.log`. Las cuatro familias legacy fueron aprobadas en la ejecución propia inmediatamente anterior `steelog-f8-correcciones-root-final-local.log` (256/256 por configuración); los únicos cambios posteriores fueron dos casos de prueba adicionales y ajustes de sus aserciones, sin cambios de producción.
- Regresiones STA nuevas: carga inicial pendiente/fallida/válida, handlers bloqueados, recarga posterior a guardado tanto exitosa como fallida, error de carga/guardado/render de identidad anterior, A→B fuera de orden y Dispose durante carga/guardado. Root corrigió fixtures delegados antes de aceptarlos: `Materiales` requiere `List<int>`, la identidad debe ser estable entre lecturas y Dispose no promete cambiar por sí solo el valor previo de `Enabled`. No se cambió producción para satisfacer esas expectativas incorrectas.
- SQL repetido tras normalizar el diff del test: **12/12**, exit 0, log `steelog-f8-correcciones-root-final-sql.log`, TRX `steelog-reportes-382520eb6ea74b5bad94b8e97e6f003d/results/reportes.trx`. Contenedor y DB propios eliminados, sin contenedores de prueba remanentes.
- PDF adicional propio: **1/1**, `envio.trx` en la carpeta de evidencia anterior. Se renderizó e inspeccionó una página con logo real y ENVÍO +2: detalle 2, entradas +0, salidas -2 y saldo 8,00. Total de inspección visual de este seguimiento: **17 páginas**. No se volvió a presentar el signo crudo como impacto de stock.
- Se mantuvieron cero dependencias prohibidas/cero excepciones; no se modificó el plan, Designer, recursos Guna ni configuración privada. Las suites SQL F3–F7 no se repitieron en este seguimiento: sus resultados previos constan arriba y no se atribuyen a esta ejecución.

Se revisaron diff y contratos reales, UTF-8 estricto y `git diff --cached --check` antes del commit. El seguimiento no inicia F9 ni certifica las validaciones manuales pendientes.
