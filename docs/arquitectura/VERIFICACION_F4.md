# Verificación F4

**F4 aceptada por root en su alcance de implementación y verificación automatizada.** Se preservan como pendientes de F0/F9 el Designer, el recorrido manual completo y la instalación offline de exposición. La migración no se aplicó a la base del usuario; no se afirma que esa instalación existente esté actualizada.

## Historial de intentos previos

Las verificaciones parciales siguientes quedaron superadas por la ejecución final de root registrada al cierre de este documento.

- Root ejecutó `SteeLog/SteeLog/Tests/consistency/run.ps1`, ahora envoltorio de `scripts/tests/inventario-integracion.ps1`: **33/33 aprobadas, 0 omitidas**, exit 0. Migración F4 aplicada dos veces en un PostgreSQL efímero propio, probes de conexión y limpieza verificados; sin contenedores remanentes. Log `TEMP/steelog-root-f4-sql-13618560b0b6472d8fa915e577d331e9.log`, TRX `TEMP/steelog-inventario-c8beb20d076f482c8f29c82d7e6e1809/results/inventario.trx`. Son pruebas contra snapshot anterior a los últimos cambios UI/queries, no aceptación final.
- MSBuild Visual Studio Debug y suite no integración se ejecutaron en snapshot previo: 88/88, tras definir `STEELOG_HOST_ASSEMBLY` y retirar cinco dependencias legacy desaparecidas. Antes, `dotnet test` sin esta variable dio 10 errores de entorno y con ella 1 error de inventario de dependencias; se corrigió retirando únicamente entradas ausentes.
- Root intentó `scripts/verificar.ps1 -LegacyLocal` provisional: **falló build CS0104** por nombres ambiguos Material/Recorte en Vista_Movimientos; se corrigieron imports con alias, aún sin repetir verificación completa. `dotnet test` con build directo había fallado MSB4216 por host x86 de `GenerateResource`; usar MSBuild de Visual Studio.
- PDF remito: pruebas aisladas de autor 3/3; root inspeccionó visualmente la muestra mixta con marca, líneas, flete, observaciones y firmas. No sustituye integración ni smoke de aplicación.

## Mapeo de integridad anterior

Las 18 aserciones legacy se trasladaron al runner F4: aislamiento e identidad DB mediante guardas del runner; ajuste diario concurrente, instantánea obsoleta, metadata y precisión, proveedores inválidos/rollback, ausencia y negativos en material, dos ajustes con una única cabecera e historial coherente; reversión y bloqueo entre dos eliminaciones y entre edición/eliminación de movimiento. La suite amplía con rollback conjunto real, insumos, recortes, OC concurrente y fracciones. El launcher viejo usaba `docker exec` no confiable y podía ejecutar esquema con roles en clúster ajeno; se reemplazó por el runner efímero guardado, sin tocar instalaciones existentes.

## Implementación F4

- Movimiento: casos registrar/editar/eliminar en Application, dominio inmutable y adaptador Npgsql con UoW; recursos de movimiento antiguo/nuevo bloqueados por orden material/recorte/insumo, con verificación de dueño del recorte. Eliminar/editar Registro Interno se rechaza como en persistencia legacy. Kardex mantiene puente temporal que delega al caso nuevo.
- C: migración explícita versionada `docker/migrations/F4_insumo_movimientos_snapshot.sql`, no autoaplicada y sin cambiar dump. Snapshot de estado de insumos y delta de stock real por línea; históricos sin información inequívoca se rechazan en reversión en vez de inferir por nombres o stock posterior. Cambios de ubicación o consumo de insumo generan historial en la misma transacción; eliminar insumo vinculado se rechaza para no borrar la evidencia. No se habilitó OC recibida → Kardex.
- C precisión: líneas `numeric(10,4)` y saldos `numeric(10,2)` no son intercambiables. Registrar diferencia real y revertirla exactamente; para históricos sin snapshot y más de dos decimales se rechaza la reversión ambigua. No redondear cantidades silenciosamente.
- Borrador: DTO/serialización en Application, `IDraftStore` y reemplazo seguro en Infrastructure, ruta real en AppData/Roaming y test con directorios temporales inyectados. Falta recorrido real de UI con archivo histórico.
- Remito: consulta autorizada, DTO y renderer separados, recursos compartidos en Application.Reportes sin dependencia entre módulos; detalle permite remito con solo insumos. Falta prueba completa de navegación e impresión.
- UI: las vistas y modales de materiales, inventario, recortes, insumos, movimientos e historial utilizan consultas tipadas y presenters. La construcción no consulta DB; las cargas son asíncronas y descartan respuestas obsoletas. Las pruebas STA controladas cubren selección, cierre/Dispose y escritura tardía. La recuperación JSON histórica se verifica con un fixture real del contrato anterior; el recorrido interactivo completo de borrador sigue pendiente del ensayo manual. Permanecen puentes de lectura de fases posteriores y la eliminación de Kardex delega al caso nuevo.

## Pendiente antes de cierre

- Integrar y auditar aportes de materiales/recortes e insumos, registrar DI/csproj y actualizar lista legacy solo por pares retirados.
- Repetir Debug, Release, suite local y `-LegacyLocal` sobre snapshot estable; ejecutar integración PG17 efímera final y validar su código de salida.
- Ensayar Designer, flujos reales, PDF, abrir/cerrar/reabrir y recuperar JSON anterior sin tocar borradores del usuario; documentar resultados reales.
- Revisar permisos compatibles, fletes inactivos, bloqueo doble envío, cancelación/latest-load, guards post-commit, stock mínimo tras guardar y carrera de historial contra edición.
- Confirmar protocolo de ajuste diario `pg_advisory_xact_lock(141414, fecha)` y fecha de negocio argentina; no invocar DB/roles/volúmenes existentes.

## Límite de permisos de lectura

Los casos de Application comprueban permisos por operación. La ocultación de cantidades y precios para Galpón mantiene la presentación legacy: no constituye filtrado de campos en DTO ni aislamiento de datos. Una política de lectura más estricta requiere una decisión funcional posterior y no se presenta como garantía de seguridad.

## Checkpoint integrado previo a auditoría root

- MSBuild Visual Studio Debug del host y tests: correcto. Suite `Category!=Integration` con `STEELOG_HOST_ASSEMBLY` configurada: **96/96 aprobadas**, 0 omitidas. Incluye STA con vista real y TaskCompletionSource para guardado tardío, selección A→B y Dispose con consulta pendiente, además de pruebas de materiales/recortes e insumos.
- `tests_f4` ejecutó en clúster efímero propio **36/36** pruebas SQL reales, incluidas consultas historial, edición, catálogos, insumos, precio eventual, flete y rechazo de alta Registro Interno. TRX/log `TEMP/steelog-inventario-be56e6b0c90e4e9b962d2a71b1637849`; DB y contenedor de prueba eliminados. Root debe repetir la corrida final contra snapshot cerrado.
- El inventario arquitectónico solo disminuyó: se retiraron pares ausentes por extracción de vistas, `InventarioDatos` y renderer host de remito; no se agregó ninguna excepción. Kardex conserva temporalmente su lectura legacy y puente de eliminación hacia Application para F8.
- `Vista_Movimientos` conserva Designer y controles; se reorganizaron componentes ya extraídos en partials `Selector`, `Carrito` y `Borrador`. Catálogos, consultas de edición/historial, PDF y operaciones salen por Application/presenters; carga de constructor se difiere y tiene protección de versión.
- Root reforzó la cancelación de `Vista_Insumos` y `ModalInsumos` también al `Dispose` directo; el autor de materiales hizo lo mismo en las cinco vistas/modal propias. Movimientos, historial y modal de flete cancelan al cerrar y al disponer; timers de movimiento se detienen y se desuscribe el filtro.
- `BrandingSmoke` usa ahora el renderer real de remito con DTO e insumo, recursos reales de composición e inspección de logo/texto; se retiró `Reportes/MovimientoPDF.cs` host duplicado. Falta que root ejecute el smoke integral completo y valide evidencia.
- La aplicación requiere aplicar explícitamente la migración F4 en una base respaldada antes de usar los casos nuevos; nunca se aplica automáticamente ni se ha aplicado a la base del usuario.

### Adopción manual en una instalación local

La migración **no se aplicó a la base del usuario**. Antes de usar las operaciones F4 en una instalación existente, identificar explícitamente la base y respaldarla; luego, solo con autorización para esa base, ejecutar el archivo versionado con un cliente PostgreSQL `psql` y detención ante errores, sustituyendo los marcadores por la conexión elegida:

```powershell
psql -X -v ON_ERROR_STOP=1 -h <servidor> -p <puerto> -U <usuario> -d <base-seleccionada> -f docker/migrations/F4_insumo_movimientos_snapshot.sql
```

Antes de aplicar, verificar `SELECT current_database(), current_user, inet_server_addr(), inet_server_port();` contra la base autorizada. Después, comprobar en `information_schema.columns` la presencia de `impacto_stock_real` en `public.movimientomaterial` y las cinco columnas `*_snapshot`/`snapshot_completo` en `public.insumo_movimientos`; contrastar también la constraint de snapshot. El script es idempotente, no rellena históricos ni se ejecuta al iniciar. Se recomienda validar primero sobre una copia restaurada del respaldo. No se usó `docker exec` ni se modificaron bases existentes para esta evidencia.

### Correcciones posteriores al checkpoint

- Se reanuda automáticamente la carga de edición seleccionada durante un F5 pendiente; prueba STA real con TCS confirma la secuencia catálogo→ID. La consulta anterior sigue protegida contra resultados obsoletos.
- La violación única de `movimientos_nroremito_key` se traduce al mensaje legacy conservando la excepción PostgreSQL interna; otras violaciones no se interceptan. La prueba de rollback SQL la ejecuta el runner efímero.
- Se agregó fixture JSON generado a partir de los DTO exactos del commit `8010e36`, sin utilizar carpetas reales; prueba de lectura de borrador histórico aprobada.
- Build Debug y siete pruebas focalizadas de lifecycle/borradores aprobadas después de estas correcciones. Antes de ellas, root verificó Debug/Release con 96/96 en cada configuración, cuatro familias legacy y regresión F3 6/6; debe repetirse la verificación local final sobre el snapshot actualizado.

## Auditoría final de root

Las entregas se contrastaron con fuentes, diff, contratos y pruebas ejecutadas por root. Se corrigieron durante la revisión el dominio inicialmente vacío, protección del Registro Interno, protocolo material→recorte, composición temporal de borradores, cancelación por Dispose, respuestas tardías, edición seleccionada durante F5 y el mensaje de remito duplicado. Las implementaciones y pruebas delegadas utilizaron Terra 6.1 High; root integró, revisó y decide el cierre.

- `scripts/verificar.ps1 -LegacyLocal`: **exit 0, 98/98 Debug y 98/98 Release**, más Configuration, search, Branding y LoginUsability. Log `%TEMP%/steelog-root-f4-approved-local-ddd12ae5bf414da9b3520eaef29f1611.log`. Incluye arquitectura, dominio, casos, JSON de DTO F3, recursos PDF y STA/TCS reales de carga A→B, escritura tardía, F5→edición y Dispose.
- `scripts/tests/inventario-integracion.ps1`: **exit 0, 39/39 SQL reales, cero omitidas**. Log `%TEMP%/steelog-root-f4-approved-sql-409b53b377e94765826dc66616893190.log`; TRX `%TEMP%/steelog-inventario-547053ebb995443ab4a7c8f901098e89/results/inventario.trx`. PostgreSQL efímero propio, servidor UTC y reloj de negocio argentino; sondas SQL exit 0/1, migración aplicada dos veces, rollback y concurrencia comprobados. La base y el contenedor propios se eliminaron y no quedaron contenedores de prueba.
- Regresión F3 `scripts/tests/ordenes-integracion.ps1`: **exit 0, 6/6 y cero omitidas**. Log `%TEMP%/steelog-root-f4-f3-regresion-84b4e1ec3b7347b8ae3e82f8bc3576fe.log`; TRX `%TEMP%/steelog-ordenes-2ca374b1b6cd4b79bfce4e70baa3e0d3/results/ordenes.trx`. Los cambios posteriores a esa corrida afectaron solo Movimientos y pruebas de borrador/UI, no los contratos ni implementaciones de OC o inventario compartido.
- PDF integrado `Remito-real.pdf` de Branding Release renderizado e inspeccionado: marca, encabezado, insumo, observaciones, firmas y pie legibles, sin recortes visibles. Artefacto `%TEMP%/steelog-branding-smoke-a00fa15fd7524e798148f2acbd664b52/Remito-real.pdf`. También se inspeccionó la muestra mixta de cuatro tipos y flete. Poppler emitió advertencias de fuentes Symbol/ArialUnicode, sin defecto visible en las muestras. Esto no sustituye impresión física ni recorrido manual.
- Inventario legacy: **323→241 pares**, sin agregar excepciones. Se retiraron escritores sin consumidores, `InventarioDatos`, el renderer host duplicado y el harness de integridad reemplazado. Permanecen lecturas necesarias para F5/F8. Se conservaron Designer, recursos y controles; el cambio de namespace de `SortableBindingList` la sitúa en presentación.
- `git diff --cached --check`, UTF-8 y ausencia de mojibake revisados. Durante el cierre solo se retiraron espacios finales y líneas vacías en archivos nuevos, sin cambio funcional posterior a las pruebas. El diff amplio de algunos archivos incluye normalización de terminadores anómalos, no un rediseño.
- Los cambios previos de `README.md` y `docker/iniciar.ps1` quedaron fuera del staging y conservaron sus hashes `90E113DAC3B2806F37F0C04963D1FB0A7DFBE84F34C180D4369FE507EE8EB9E1` y `27A92887208DF3A0DA4F986645CB8AEF574593A48606FC22D7EA18D4F665DC0F`. El plan de entrada no se modificó.

### Límites del cierre

No se ejecutaron `Program.Main`, publicación, actualizador real ni servicios remotos, ni se modificaron bases, roles o volúmenes existentes. No se verificaron manualmente todos los formularios, el Designer o la instalación offline completa; ese ensayo y la configuración embebida no sensible siguen pendientes de F0/F9. La migración sobre la instalación existente requiere la adopción explícita descrita arriba. La ocultación visual por rol no se presenta como confidencialidad de campos. No se activó recepción OC→Kardex automático.

El siguiente tramo del plan es **F5, Conteos**. El estado de implementación de F4 no implica que F5 ni el resto del repositorio ya estén migrados.
