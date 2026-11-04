# Verificación F7: Herramientas y catálogos

**Estado:** F7 completada y aceptada por root en su alcance de implementación y verificación automatizada. Continúan pendientes los ensayos manuales F0/F9 indicados al final.

## Alcance y continuidad

Se continuó desde `cf8e3cb`, en `plan/clean-architecture-academica`, con el árbol limpio. F1–F6 no se reiniciaron. El plan principal permanece intacto. La implementación se delegó a GPT Terra 6.1 High; root revisó el diff, integró y ejecutó las verificaciones propias. No se utilizó agy ni se hizo push.

F7 comprende herramientas, categorías, obras, proveedores, ubicaciones y fletes: dominio, autorización, consultas, escrituras tipadas y presenters. No hay cambios de esquema, auto-DDL ni migraciones F7. No se aplicaron scripts sobre instalaciones existentes.

## Implementación y decisiones

- R: `Herramienta` conserva identidad, nombre, estado, obra y observaciones. Sus métodos controlan edición, identificación y retorno al galpón. Al crear, el prefijo utiliza el ID realmente asignado; si el nombre ya empieza con dígitos se conserva, como antes. La longitud máxima se comprueba también después de prefijar; si falla, se revierte el alta SQL y no queda historial parcial.
- R/C: alta e historial inicial, edición con traslado, retorno y eliminación con historial comparten una sola UoW. Los cambios de herramientas bloquean primero su fila. Dos retornos concurrentes no producen dos movimientos: la segunda ejecución comprueba la pertenencia bajo el mismo bloqueo y rechaza la herramienta que ya está en el galpón.
- R: el historial sigue guardando nombres de origen/destino y mostrando nombre/estado actuales de la herramienta mediante la consulta legacy equivalente. No se presenta como un nuevo sistema de snapshots históricos.
- R/C: el esquema tiene `estado varchar(50)`, sin enumeración cerrada, y existen representaciones históricas distintas de las opciones UI. Las altas aceptan Óptima, Regular y Mal estado. La rehidratación y el retorno conservan el estado histórico; una edición puede conservarlo o elegir una opción canónica, pero no introducir otro valor no canónico. Las observaciones respetan `varchar(500)`.
- R: catálogos simples, con estado y límites reales, sin agregados artificiales ni repositorio universal. Categorías, proveedores y fletes admiten nombres de hasta 50 caracteres; obras y ubicaciones, 100. Los DTOs separan lectura de dominio.
- R: fletes conserva baja lógica, no eliminación física. Alta y desactivación reutilizan `IFletesEscritura` de F4; un alta inicialmente inactiva hace ambas escrituras en una sola transacción. La edición tipada conserva las referencias existentes.
- R: lecturas y operaciones requieren sesión Galpón o Admin en Application, coherente con el acceso legacy. No se agregó silenciosamente una restricción exclusiva de Admin. Las consultas usan conexiones propias y liberadas; los presenters solo conservan el ejecutor, no repositorios/UoW.
- R/C: los helpers transaccionales F7 diferencian fallo previo al commit y excepción durante su confirmación. Esta última informa resultado incierto y exige verificar persistencia antes de reintentar; no afirma que un rollback posterior pruebe ausencia. No hay reenvío automático ni idempotencia prometida. Un fallo secundario de rollback no reemplaza la excepción original. No se modificó la UoW ni los casos de uso de fases cerradas.

## Presentación y retiro legacy

Las cinco vistas de catálogos y herramientas no conocen Datos, Npgsql, Infrastructure ni SQL. Los constructores no consultan ni activan servicios. Las cargas son asíncronas, las respuestas obsoletas se descartan y cerrar/disponer cancela su ciclo de vida. Se bloquea la escritura con el modelo inicial pendiente o fallido y se impide el doble envío. Catálogos deshabilita los campos durante la escritura; herramientas permite una nueva edición local, pero la respuesta anterior no la limpia ni recarga su contexto. Una respuesta de error antigua tampoco muestra un aviso en el contexto nuevo de herramientas.

Las recargas fallidas posteriores a una escritura confirmada no se presentan como rollback de esa escritura. Se conserva marca, navegación, controles Guna, Designer y recursos. En los cinco Designer de catálogos solo se añadió cancelación al disponer. No hubo rediseño ni recursos visuales inventados. Antislop se aplicó como filtro `during` de conservación, no como certificación visual integral.

Para retirar `ComunDatos` se migró también el selector de obras del informe a la consulta tipada. La acción existente de retorno de herramientas del informe usa Application, es asíncrona y bloquea doble envío; después de cerrar/cambiar obra no recarga ni avisa sobre el contexto nuevo. El resto de consultas y cálculos legacy del informe sigue perteneciendo a F8. El renderer PDF no cambió.

Se retiraron `ComunDatos`, `CategoriaDatos`, `ObraDatos`, `ProveedorDatos` y `FleteDatos`, ya sin consumidores. De `HerramientaDatos` se retiraron todas las escrituras y el historial sin consumidores; permanece únicamente la lectura que aún usa `BusquedaGlobal`, cuyo traslado corresponde a F8. Los modelos host que todavía participan en UI/búsqueda se conservaron.

El inventario de dependencias temporales disminuye de **163 a 116 pares**, sin incorporaciones. Se obtiene contrastando las referencias reales del assembly con el inventario anterior; las pruebas inspeccionan también tipos anidados y máquinas de estado async. Hay una guarda específica para herramientas y catálogos, para la ausencia de `ComunDatos` y para que el informe no vuelva al escritor legacy de herramientas.

### Ajustes de calidad y robustez

- **UI Herramientas (`Vista_Herramientas.cs`):** Limpieza canónica de `cboEstado` en `LimpiarSeleccion` y `dgvHerramientas_CellClick` para evitar contaminación cruzada de estados históricos entre filas. Deshabilitación de controles y alternancia de pestañas durante lectura o escritura activa (`!leyendo && !ocupada`). Eliminación del doble incremento de revisión en `Load`. Bloqueo de `dgvHerramientas_CellClick` cuando la vista está ocupada. Salvaguarda contra `NullReferenceException` en `btnGuardar_Click`.
- **Application (`GestionarHerramientas.cs`, `Catalogos.cs`, `ConsultarHerramientas.cs`, `HerramientasPresenter.cs`):** Exclusión de `OperationCanceledException` de `ResultadoEscrituraInciertoException` en confirmaciones de commit. Eliminación de llamada redundante a `h.Editar` en `CrearAsync`. Validaciones de nulidad en constructores con `ArgumentNullException`.
- **Infrastructure (`NpgsqlHerramientas.cs`, `NpgsqlHerramientasConsulta.cs`):** Preservación de comportamiento legacy en `RegistrarHistorialAsync` distinguiendo `null` ('Galpón') de obras inexistentes ('Desconocida'). Timeouts explícitos de 15 segundos en comandos SQL y validación de nulidad en constructores.
- **Tests (`CatalogosVistaTests.cs`, `CatalogosResultadoInciertoTests.cs`):** Cobertura agregada para aislamiento de estados históricos en `cboEstado`, bloqueo de grilla ante vista ocupada, descarte de falsos positivos en excepciones canceladas de commit y validación de dependencias nulas en constructores.

## Verificación propia de root

Los runners SQL utilizan clústeres PostgreSQL 17 efímeros propios, tmpfs, puertos loopback dinámicos y DB `steelog_test_<GUID>`. Verifican identidad de contenedor y DB, sondas SQL exit 0/1 por API Docker, copias TEMP sin bin/obj compartidos, hashes de DLL versionadas, TRX sin omitidos y limpieza limitada a sus recursos. El runner F7 no aplica migraciones; las de F4 en sus runners existentes solo se aplican al clúster efímero que crean.

- Local: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal`, **exit 0, 180/180 Debug y 180/180 Release**, sin omitidos. Las cuatro familias legacy Configuration, search, Branding y LoginUsability pasaron. Log `%TEMP%/steelog-root-f7-final-local.log`. Incluye 17 casos de catálogos STA, seis de herramientas/presenter, seis de lifecycle del informe, cuatro de dominio de herramientas y diez de Application/catálogos/resultado incierto, además de la suite anterior y las guardas de arquitectura.
- F7 SQL final, sobre el código congelado: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tests/catalogos-integracion.ps1`, **12/12**, exit 0, sin omitidos. Log `%TEMP%/steelog-root-f7-frozen-catalogos.log`; TRX `%TEMP%/steelog-catalogos-784258ec01b345e4a3e3e6bf40368535/results/catalogos.trx`. Incluye edición de observaciones conservando estado histórico. Sondas exit 0/1 y eliminación de DB/contenedor propio confirmadas.
- F6 SQL: **14/14**, exit 0, log `%TEMP%/steelog-root-f7-final-canaletas.log`; TRX `%TEMP%/steelog-canaletas-dde994bae49b4dd0863f37dc3e4a5381/results/canaletas.trx`.
- F5 SQL: **15/15**, exit 0, log `%TEMP%/steelog-root-f7-final-conteos.log`; TRX `%TEMP%/steelog-conteos-a6e83d8a1f654624bd43ef0b0fbca405/results/conteos.trx`.
- F4 SQL: **40/40**, exit 0, log `%TEMP%/steelog-root-f7-final-inventario.log`; TRX `%TEMP%/steelog-inventario-7d47cd6ec50349aab8932db05f5eb8fb/results/inventario.trx`.
- F3 SQL: **6/6**, exit 0, log `%TEMP%/steelog-root-f7-final-ordenes.log`; TRX `%TEMP%/steelog-ordenes-472a6b8481144af9a4041c04a559856e/results/ordenes.trx`.

Las regresiones SQL F3–F6 se ejecutaron antes del último ajuste de clasificación de errores de commit, que solo consumen los dos servicios nuevos F7; no cambiaron sus implementaciones ni su UoW. La corrida local final incluye ese ajuste y los últimos tests de UI. Branding volvió a generar muestras reales de reportes en `%TEMP%/steelog-branding-smoke-19cc51ff9e15442597213ef7d37b7665`; F7 no modificó renderers ni se atribuye una nueva inspección visual de sus páginas.

Root revisó el diff de producción y pruebas, permisos, lifetime de conexiones/UoW, prefijos/estados históricos, coherencia del historial y descarte de respuestas tardías. Se verificaron UTF-8 estricto, `git diff --check`, el subconjunto estricto del allowlist y la ausencia de contenedores `steelog-ca-test-*` al terminar. El staging se limita a F7 y se comprueba también `git diff --cached --check` antes del commit. El plan, los scripts de migración y los recursos originales permanecen intactos.

Las pruebas SQL F7 cubren CRUD de los cinco catálogos, alta inactiva de flete, límites, permisos, FK real, alta de herramienta y prefijo, historial inicial, edición, traslado, eliminación, rollback después de INSERT/historial, UPDATE y DELETE reales, cancelación, bloqueo observado en PostgreSQL, dos retornos concurrentes y conservación de estado histórico. Las pruebas locales añaden dominio, UoW/permisos con dobles, clasificación de commit incierto y lifecycle STA/TCS de vistas/presenters.

Una corrida intermedia propia de F7 obtuvo 10/12 por dos timeouts de autenticación de Npgsql durante la ejecución concurrente de verificaciones; no fue un PASS. Se conservó el log `%TEMP%/steelog-root-f7-approved-catalogos.log` y su TRX en `%TEMP%/steelog-catalogos-db99821f035e450ba34f9f92c740a212/results/catalogos.trx`. La DB y el contenedor de esa corrida fueron eliminados. La causa observada fue el timeout de conexión; la contención de recursos es una hipótesis, no una causa demostrada. La verificación final se realiza sin otros runners concurrentes, sin ampliar timeouts, omitir casos ni incorporar retries.

## Límites y próximos pasos

- No se ejecutaron Main, updater real, publicación ni servicios remotos. No se leyó configuración privada ni sesión DPAPI, ni se modificaron DB, roles o volúmenes del usuario.
- Las pruebas automatizadas no sustituyen Designer interactivo, impresión física, recorrido manual completo ni ensayo de instalación/offline integral. Los pendientes F0/F9 continúan abiertos.
- No se modificaron stock, recepciones OC/Kardex ni cierre de conteos. La migración explícita F4 sigue siendo una decisión de adopción independiente para instalaciones existentes.
- F8 permanece pendiente: consultas/reportes/búsqueda restantes, valorización pura y eliminación del resto del inventario legacy. F9 sigue pendiente.
- El hook no confirmó identidad autoritativa de runtime. Root no escribió memoria ni actualizó la canónica. Un subagente guardó provisionalmente la observación #1547 antes de detener sus escrituras por instrucción de root; no se considera una actualización canónica ni evidencia de cierre. Git y este documento son la continuidad verificable de F7.
