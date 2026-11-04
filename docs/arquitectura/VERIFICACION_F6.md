# Verificación F6: Canaletas y archivos locales

**Estado:** F6 completada y aceptada por root en su alcance de implementación y verificación automatizada. Los ensayos manuales de F0/F9 indicados al final siguen pendientes.

## Alcance y continuidad

Se continuó exclusivamente F6 desde `47fc688`, en `plan/clean-architecture-academica`, con el árbol inicialmente limpio. F1–F5 no se reiniciaron. El plan principal y el antecedente de calidad permanecen intactos. No se incorporó ninguna migración ni auto-DDL de F6 y no se aplicaron scripts sobre bases existentes. La adopción explícita de la migración F4 sigue siendo un requisito independiente donde corresponda.

Los implementadores utilizaron GPT Terra 6.1 High y root revisó e integró el diff, ejecutó las verificaciones indicadas abajo y realizó el commit. Hubo una interrupción por cuota; se retomaron los archivos existentes con el mismo modelo cuando volvió a estar disponible. El usuario autorizó agy Flash 3.8 High como alternativa si Terra quedaba indisponible; no fue necesario usar esa alternativa.

## Implementación y decisiones

- R: `CanaletaPlegado` conserva su estado y modifica obra, descripción, precio y pago mediante invariantes. `FotoCanaleta` conserva y valida metadatos, tamaño, MIME y orden. Application autoriza las operaciones para usuarios demo autenticados Galpón/Admin, como el acceso legacy, sin inventar permisos adicionales.
- R/C: consultas y DTO tipados; alta, edición, adjunto, quitar foto y eliminación con scope/UoW propios. El adjunto bloquea la cabecera para asignar el siguiente orden. Las consultas usan conexiones independientes y liberadas; la comprobación posterior al commit no reutiliza la transacción fallida.
- C: precio `numeric(20,2)` validado sin redondeo silencioso. El esquema versionado permite JPEG, PNG y WebP, hasta 8.388.608 bytes. Se rechazan vacíos, exceso de tamaño, formatos ajenos y estructuras incompletas; no se deduce el MIME de la extensión. El validador comprueba estructura, no constituye un codec completo ni garantiza detectar cualquier corrupción interna de una imagen.
- R: se conservan los `object_path`, la categoría histórica `canaletas-fotos` y la raíz `%LOCALAPPDATA%/SteeLog/Storage`, calculada e inyectada desde el host. Las fotos anteriores se leen sin renombrarlas ni moverlas.
- C: las rutas absolutas, `..`, segmentos ambiguos y recorridos por enlaces/reparse points se rechazan. Los leases y las consultas de referencias contemplan aliases Windows de mayúsculas y separadores; la consulta conservadora también contempla puntos/espacios finales en referencias persistidas. Nunca se usa la ruta original seleccionada por el usuario como destino de escritura o eliminación.
- R: vista principal con presenter y ejecutor por operación. Constructor sin consultas ni activación de servicios, inicialización y lectura de fotos asíncronas, doble envío bloqueado, respuestas A→B descartadas y cancelación tanto al cerrar como al disponer. Una escritura tardía no limpia el contexto nuevo. Las fotos de un registro existente conservan el adjunto inmediato; las de un alta nueva se adjuntan al ID confirmado, sin fingir atomicidad conjunta con la cabecera.
- R: se retiraron los dos Datos y los dos modelos legacy de canaletas sin consumidores. El inventario de dependencias temporales disminuye de **205 a 163 pares**, sin agregar excepciones.

## PostgreSQL y archivos: garantías y límites

No hay una transacción ACID conjunta. Cada adjunto genera una clave GUID estable durante esa ejecución, sin reintentos automáticos ni promesa de idempotencia ante un reenvío manual.

1. Antes de publicar una referencia se registra un pendiente durable y se adquiere un lease exclusivo por archivo, compartido también entre instancias mediante el filesystem.
2. Se escribe y fuerza el staging propio; se promueve mediante `Move` sin sobrescribir. Los certificados locales `.owned` acreditan qué archivos creó este adaptador. Son evidencia operativa local, no una barrera contra manipulación deliberada del directorio.
3. PostgreSQL guarda los metadatos en su UoW. El rollback conocido compensa de forma conservadora; si la compensación falla, se conserva el pendiente.
4. Si falla el commit, se consulta por la clave mediante una conexión nueva. Un adjunto confirmado recupera su ID; una ausencia comprobada permite compensar. Si no puede determinarse el resultado, se conservan archivo y pendiente y se informa resultado incierto, sin sugerir un reenvío ciego.
5. Para eliminar se registran primero los pendientes y se confirma DB antes de limpiar archivos. El fallo posterior se distingue como **operación confirmada con limpieza pendiente**; la UI refleja la confirmación y no ofrece repetir ese adjunto como si hubiera fallado.
6. Al abrir el módulo se intenta una conciliación controlada, fuera del hilo UI y sin bloquear la carga si hay un error. Los archivos con lease activo se omiten para una próxima apertura; no se afirma haber vaciado todos los pendientes. Una referencia vigente impide borrar. Una consulta fallida tampoco autoriza la eliminación.

Una foto preexistente sin certificado de propiedad se conserva incluso después de quitar sus metadatos: queda un pendiente que requiere conciliación manual sobre una copia/respaldo y comprobación de referencias. No se infiere propiedad por el nombre `f6_`. Si se pierde el certificado de una foto nueva, se adopta la misma política conservadora. Esto evita borrar originales o archivos de propiedad incierta a cambio de una posible limpieza manual; no se promete ausencia absoluta de huérfanos ante fallos del disco o terminación abrupta.

WebP se admite para validar, guardar y recuperar los bytes. La previsualización utiliza el soporte de imagen disponible en WinForms/GDI; cuando no puede decodificar una foto se advierte y se conserva tanto el archivo como su registro. No se agregó un codec ni se afirma haber comprobado visualmente WebP en todos los equipos.

## Informe de obra y PDF

La lectura de canaletas/fotos del informe utiliza Application y el almacenamiento local, sin Supabase ni tokens. La selección de obra, el visor de fotos y el PDF descartan resultados de contextos anteriores y no abren un visor después del cierre/cambio. Las lecturas legacy restantes se capturan fuera del hilo UI y no se asignan antes de validar la versión de carga.

Se extrajo el renderer **completo** de Informe de obra a Infrastructure, alimentado por `InformeObraSnapshot` e `IRecursosPdf`. Esta extracción acotada se adelantó de F8 porque pasar un `Document` por Application o llamar directamente a Infrastructure desde la vista habría roto el grafo. El wrapper host conserva temporalmente las consultas y el mapeo legacy de movimientos/herramientas/insumos; su migración y los cálculos generales de valorización siguen perteneciendo a F8. No se reescribieron esas reglas ni se agregó un reporte nuevo.

`FileStream` y `Document` se liberan incluso ante una excepción de recursos. Se conservaron marca, filtros, secciones, tablas y visibilidad legacy por rol. La inspección de la primera muestra detectó cabeceras huérfanas y columnas ausentes en continuaciones: título y columnas de canaletas/activos ahora forman cabeceras repetidas. También se corrigió el fixture multipágina para que sus importes sean coherentes (723 + 100 + 5 = 828). No es un rediseño.

## Verificación propia de root

La compilación y los tests no se equiparan. Los SQL se ejecutaron en clústeres PostgreSQL 17 propios y efímeros, con archivos TEMP exclusivos, identidad de recursos comprobada, sondas SQL exit 0/1 mediante API Docker y comprobación del TRX sin omisiones. Los cuatro runners confirmaron DROP de sus DB y eliminación de sus contenedores; root también comprobó que no quedaran contenedores `steelog-ca-test-*`.

- F6, `scripts/tests/canaletas-integracion.ps1`: **14/14**, exit 0. Log `%TEMP%/steelog-root-f6-final-canaletas-fd6d652c970049f3bf6c204179759c74.log`; TRX `%TEMP%/steelog-canaletas-7c58de7314cd4e83836e09f93e6a7c33/results/canaletas.trx`.
- F5, `scripts/tests/conteos-integracion.ps1`: **15/15**, exit 0. Log `%TEMP%/steelog-root-f6-final-conteos-7666b5a4d6114043a6f2c0a1e1460c41.log`; TRX `%TEMP%/steelog-conteos-96eb7f00ae1e4720bfe35618663ead05/results/conteos.trx`.
- F4, `scripts/tests/inventario-integracion.ps1`: **40/40**, exit 0. Log `%TEMP%/steelog-root-f6-final-inventario-8794fdba0b7c48eca61cb11c84b33f18.log`; TRX `%TEMP%/steelog-inventario-15642678cf8b477585bcbacab5280009/results/inventario.trx`.
- F3, `scripts/tests/ordenes-integracion.ps1`: **6/6**, exit 0. Log `%TEMP%/steelog-root-f6-final-ordenes-32427edabc734af3bd3d1abbb60bf9f4.log`; TRX `%TEMP%/steelog-ordenes-fcc4091b47374f688f66fb063b502924/results/ordenes.trx`.
- PDF aislado de root: **3/3**, exit 0, TRX/log/muestras en `%TEMP%/steelog-root-f6-pdf-8f80f3ed876c48f792e4069bc19a12a4`. Se renderizaron con Poppler y se inspeccionaron las cinco páginas Admin y las tres Galpón: marca, importes, columnas repetidas y activos legibles, sin cabeceras huérfanas ni recortes visibles en las muestras.

La integración SQL F6 cubre CRUD, metadatos, orden y adjuntos concurrentes, restricciones reales, permisos, lectura anterior, bloqueo observado en PostgreSQL, cancelación después del INSERT, fallo después de SQL con rollback, commit incierto antes/después del commit real, incertidumbre irresoluble, limpieza fallida, conciliación durante una operación activa y conservación de una foto referenciada por otra canaleta mediante alias Windows. Las pruebas de disco local incluyen ruta inválida, lease ocupado, directorio destino inválido y foto preexistente sin certificado. No se simularon stock ni operaciones de negocio para declarar los resultados SQL.

Una primera corrida integrada obtuvo 140/140 Debug y 139/140 Release: falló un test nuevo al abrir un Toast real entre hilos STA concurrentes. Se incorporó un punto de inyección interno para capturar sus avisos y verificar estado y advertencia, sin modificar `ToastNotification`, desactivar paralelismo ni omitir la prueba. La corrida aprobada posterior y los controles finales se registran al cierre.

### Cierre integrado

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal`: **exit 0, 140/140 Debug y 140/140 Release**, sin omisiones, más las cuatro familias Configuration, search, Branding y LoginUsability aprobadas. Log `%TEMP%/steelog-root-f6-approved-local-d31080d537214dea886e952f470a2075.log`. Incluye ocho pruebas STA de la vista principal y cuatro casos de lifecycle del informe, además de las tres del renderer y seis de núcleo/archivos.
- Los cuatro SQL anteriores se ejecutaron sobre el backend congelado. La corrección posterior del Toast fue exclusivamente de vista/test; no cambió dominio, casos de uso, SQL ni archivos locales. El PDF aislado propio se ejecutó con el renderer final y sus ocho páginas fueron inspeccionadas por root.
- El test de arquitectura inspecciona también tipos anidados/async: vista/presenter de canaletas sin Datos, Infrastructure, Npgsql, iText ni Supabase; informe sin adaptadores legacy de canaletas ni tokens; renderer sin SQL, persistencia ni sesión global. El allowlist resultante de 163 pares es un subconjunto estricto del anterior, verificado contra el assembly real, no una lista de excepciones ampliada para aprobar.
- Root contrastó contratos, permisos, UoW, rutas, leases, resultado incierto, certificados de propiedad, conservación de referencias y tratamiento visible de limpieza confirmada contra el diff. Se conservaron Designer y recursos; los únicos cambios de Designer llaman a la cancelación al disponer.
- UTF-8 estricto y planes/archivos ajenos sin cambios, comprobados. Staging limitado a los archivos de F6; `git diff --cached --check` y revisión del diff preparados antes del commit. No se hizo push.

Antislop se aplicó durante la implementación como filtro de conservación: misma marca, controles y navegación; sin assets inventados ni rediseño. No se presenta como auditoría visual integral ni como validación manual de todos los controles.

## Subsanaciones de calidad y robustez post-revisión

1. **Gestión de UI y cursor en fallos:** Se desacopló la condición de bloqueo de botones (`bloqueado`) del cursor de espera (`enCurso`), garantizando que ante `_errorFotos = true` el cursor regrese a `Cursors.Default`. Se habilitó la recuperación del formulario mediante `btnLimpiar_Click` cuando no hay operaciones en curso. Ante alta confirmada con fallo incierto posterior en adjuntos, se fuerza el refresco de grilla (`CargarItemsAsync`) para visibilizar el nuevo registro.
2. **Claridad de notificaciones y preview:** Se eliminó la emisión simultánea y contradictoria de Toasts de éxito y advertencia cuando quedan limpiezas pendientes de archivos. En `MostrarPreviewAsync`, la ausencia de bytes en disco se reporta explícitamente limpiando el preview en vez de provocar excepciones de codec GDI engañosas.
3. **Limpieza de código muerto:** Se retiró el método privado sin uso `RefrescarListaFotos` en `Vista_CanaletasPlegados.cs` y los métodos no referenciados `AgregarSeccionMaterialesAcumulados`, `AgregarSeccionRecortes`, `AgregarSeccionEventuales` junto con `fntPie` en `InformeObraPDF.cs`.
4. **Compensación directa en rollback:** En `Canaletas.AdjuntarAsync`, el rollback cierto previo a commit compensa directamente el archivo local sin abrir consultas SQL innecesarias sobre conexiones potencialmente degradadas.
5. **Normalización y leases locales:** `LocalCanaletaStorage` normaliza segmentos recortando puntos y espacios finales en alineación con las consultas SQL, compara identidades de certificados `.owned` de forma normalizada, y elimina en best-effort los archivos `.lock` huérfanos al liberar los leases.
6. **Prevención de PDFs truncos:** `InformeObraPDF.Generar` valida la integridad de `LogoPng` antes de crear el `FileStream`, evitando la generación de archivos `.pdf` de 0 bytes ante logos corruptos o inválidos.

## Límites que no se ocultan

- No se ejecutaron `Program.Main`, updater real, publicación, servicios remotos ni pruebas destructivas sobre instalaciones existentes. No se leyó configuración privada ni la sesión DPAPI real.
- Las pruebas STA/TCS y las muestras PDF no sustituyen Designer interactivo, impresión física, recorrido completo de pantallas ni ensayo offline integral/instalación limpia. Esos pendientes manuales de F0/F9 siguen abiertos.
- La ocultación visual legacy de precios/cantidades no constituye confidencialidad de campos en DTO.
- F7, F8 y F9 no se declaran implementadas por este cierre. No se habilitaron recepción OC→Kardex ni ajustes automáticos de stock por conteo.
- La memoria canónica anterior no reemplaza Git ni esta evidencia. No se considera autoritativa una escritura provisional de subagente sin confirmación de identidad de sesión por el hook del host.
