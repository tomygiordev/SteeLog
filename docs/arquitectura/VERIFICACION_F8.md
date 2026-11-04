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
