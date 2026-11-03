# Verificación F5 — Conteos

**Estado:** F5 aceptada por root en su alcance de implementación y verificación automatizada, sobre la base `9c43d1e`. Se preservaron las correcciones externas de F3/F4. F5 no requiere una migración nueva ni ajusta stock. No se aplicaron migraciones ni se tocaron bases, roles o volúmenes existentes. La ejecución F4 aún requiere la adopción manual de su migración donde corresponda, como documenta `VERIFICACION_F4.md`.

## Operaciones y consistencia

- R: agregado `Conteo` con estados, líneas y reglas de acumulación, corrección exacta, eliminación de detalle, cierre y cancelación. Datos de consulta separados y PDF alimentado por DTO, sin SQL ni sesión en el renderer. Vista con presenter y scope/UoW por operación; consultas con conexiones independientes. No se alteraron Designer, recursos ni controles visuales.
- C: todos los escritores de detalle y las transiciones bloquean **primero la misma cabecera**, comprueban estado y permisos dentro de la transacción y recién después escriben. El cierre bloquea materiales por ID ascendente, exige detalles y congela stock y diferencia exacta; no modifica el stock ni crea movimientos. La cancelación antes del commit revierte; no hay reintentos automáticos.
- `numeric(18,2)` se valida sin redondeo silencioso, también después de acumular y calcular diferencia. Si la cantidad o diferencia no cabe, se rechaza y revierte el conjunto. El snapshot inicial de nombre/categoría se conserva al acumular o corregir.
- El actor proviene de `Session`. Creador: carga, corrección, quitar y finalizar; creador o Admin: cancelar; sólo Admin: eliminar conteos finalizados/cancelados. Se conserva la visualización histórica y sólo se vinculan los propietarios antiguos `admin` y `galpon` con las cuentas canónicas exactas `admin@tesis.local` y `galpon@tesis.local`, respectivamente, sin asignar localparts de otros dominios. Los nuevos conteos guardan email completo; la UI muestra el nombre anterior para esos dos usuarios.
- La ocultación de stock y diferencias para Galpón conserva la presentación y el PDF legacy; **no** constituye filtrado de campos en DTO ni aislamiento de datos.

## Presentación y PDF

- Vista de carga y detalle asíncronos, constructor sin acceso a DB/servicios, consulta inicial y detalles con modelo no habilitado hasta completar, prevención de doble envío, cancelación al cerrar y disponer, y versiones para descartar respuestas y mutaciones tardías de otro contexto. La impresión captura selección y rol, renderiza fuera de UI y no abre el visor si se cerró o cambió el contexto. Un render sincrónico que ya empezó puede terminar el archivo; no se revierte ni reintenta.
- C visual documentada: la tabla de Galpón/no finalizado declaraba cuatro columnas pero emitía cinco encabezados/celdas; se corrigió a cinco. También se corrigió texto blanco ilegible de observaciones y se mantuvieron juntas cabecera y cuerpo de observaciones para evitar título huérfano de página. No es un rediseño.
- Se retiraron `ConteoInventarioDatos`, los tres modelos legacy y renderer host de conteo sin consumidores; el inventario de dependencias temporales disminuyó de 238 a 205 pares, sin nuevas excepciones. `BrandingSmoke` usa el renderer real y conserva los demás casos.

## Evidencia antes del congelamiento

- `scripts/verificar.ps1 -LegacyLocal`: exit 0, **117/117 Debug y 117/117 Release**, 0 omitidas y cuatro familias legacy PASS. Log `%TEMP%/steelog-f5-verificar.log`. Esta corrida precedió al último seam de apertura de PDF y su prueba STA; root debe repetirla sobre el snapshot final.
- MSBuild de Visual Studio Debug del host y tests: PASS. Tras el seam final, suite focal `ConteoVistaTests`: **4/4**, 0 omitidas. Usa ventanas reales STA y dobles controlados para consulta inicial pendiente, resultados A→B, doble envío, cambio de contexto, cierre/Dispose y render bloqueado sin apertura posterior del visor.
- Suite SQL propia efímera: **15/15**, 0 omitidas; incluye ambos órdenes de carrera carga/cierre, cancelación/corrección, detalle/cierre, eliminación/carga, rollback tras SQL, cancelación, serialización de stock, precisión, identidad histórica y consultas. Evidencia `%TEMP%/steelog-conteos-fc4c5018d0d74a26bc33aeb2fd000986`; el agente SQL informa DB/contenedor eliminados. Root debe repetir sobre snapshot final y verificar regresión F3/F4.
- `BrandingSmoke` Debug PASS con Conteo, OC, remito, Kardex, informe y stock; `%TEMP%/steelog-branding-smoke-454e135b0afe41579e17c9f4d9d00744`. Root inspeccionó muestras PDF Admin finalizado/abierto y Galpón finalizado; pruebas de renderer aisladas **6/6** con contraste/observaciones/secciones, recurso inválido y descriptor liberado. Su evidencia final se registra abajo.
- Intento directo anterior `dotnet test` sin `STEELOG_HOST_ASSEMBLY`: 105/115; los 10 errores fueron por ambiente no configurado, no se consideran PASS ni se atribuyen al código. El runner oficial configuró esa variable y pasó luego. `dotnet build/test` directo del host puede fallar `GenerateResource` x86; usar MSBuild Visual Studio como hace el runner.

## Pendientes de cierre y límites

Designer interactivo, recorrido visual completo con datos reales y ensayo offline de instalación siguen pendientes de F0/F9; pruebas STA y PDF no los sustituyen. No se ejecutó `Program.Main`, updater real, publicación, servicios remotos ni modificación de configuración privada o sesión DPAPI real. No se habilitó OC → Kardex ni ajuste automático de stock por conteo.

## Auditoría final de root

Las fuentes y el diff se contrastaron con el plan y con la nueva base `9c43d1e`. Los subagentes Terra 6.1 High implementaron el núcleo, las consultas, la integración de vista y las pruebas SQL; root revisó, corrigió el manejo de recursos y la presentación del PDF y ejecutó las siguientes verificaciones sobre el snapshot final:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal`: **exit 0, 118/118 Debug y 118/118 Release**, más Configuration, search, Branding y LoginUsability. Log `%TEMP%/steelog-root-f5-final-local-0785f599de144c7b9dc73640a8d41f6b.log`. Incluye cuatro STA reales con TCS y las seis pruebas del renderer. No hubo omisiones.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/tests/conteos-integracion.ps1`: **exit 0, 15/15 SQL, cero omitidas**. Log `%TEMP%/steelog-root-f5-final-conteos-09b4e639308f4a719dea615cc3dd50be.log`; TRX `%TEMP%/steelog-conteos-5b83a0e5edc646bdbddd000081ceeb02/results/conteos.trx`.
- Regresión F4 `scripts/tests/inventario-integracion.ps1`: **exit 0, 40/40 SQL, cero omitidas**. Log `%TEMP%/steelog-root-f5-final-inventario-f8069d9884e94a3dbdd4e1833ae9eb55.log`; TRX `%TEMP%/steelog-inventario-709a52844c954378ba1050841f9ce450/results/inventario.trx`.
- Regresión F3 `scripts/tests/ordenes-integracion.ps1`: **exit 0, 6/6 SQL, cero omitidas**. Log `%TEMP%/steelog-root-f5-final-ordenes-94a173e69ef04627830038d9ddcd7b2a.log`; TRX `%TEMP%/steelog-ordenes-524a19c5bda244b984e5e597f1d83dec/results/ordenes.trx`.
- Los tres runners comprobaron transporte SQL con exit 0/1 y eliminaron sus bases y contenedores propios. Se contrastó la ausencia de contenedores `steelog-ca-test-*` remanentes; no se utilizó el clúster del usuario.
- PDF: suite aislada de root **6/6**, log/TRX y muestras en `%TEMP%/steelog-root-conteo-pdf-a0ddbb24010643b7970d1a5851239c5e`. Se renderizaron las muestras con Poppler y se inspeccionaron ambas páginas del Admin finalizado y la variante Galpón: marca, tablas, columnas, cifras y observaciones legibles, sin recortes visibles. Se corrigieron contraste y título huérfano observados en la primera iteración. Los fallos iniciales del test por alias `Path` y por leer el MediaBox sin rotación se corrigieron; no eran regresiones del generador. Las advertencias de fuentes Symbol/ArialUnicode no produjeron defectos visibles en las muestras.
- Arquitectura: inventario legacy **238→205 pares**, sin nuevas excepciones. La vista/presenter de Conteos no conoce Datos, Npgsql ni iTextSharp; el renderer recibe DTO y recursos, sin SQL ni estado global de sesión.
- Staging limitado a F5; UTF-8, ausencia de mojibake y `git diff --cached --check` revisados. Después de las pruebas sólo se retiraron líneas vacías finales y se completó esta evidencia, sin cambios funcionales. `README.md`, `docker/iniciar.ps1` y ambos planes de arquitectura/calidad permanecieron sin diferencias respecto de `9c43d1e`.

La actualización canónica de Engram queda pendiente del registro autoritativo del runtime por el host; un resumen provisional no sustituye esa atribución. El historial Git y este informe conservan los resultados y límites verificables de F5. La siguiente fase del plan es F6, Canaletas y archivos locales; no se declara implementada por este cierre.
