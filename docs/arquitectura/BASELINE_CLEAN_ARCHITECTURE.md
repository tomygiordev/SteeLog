# Baseline funcional y de compatibilidad — Clean Architecture

Fecha: 2 de noviembre de 2026. Referencia: `da3b3cbf8e240e5997508b147127bdbef7bd4b1e`, rama `plan/clean-architecture-academica`. Al comenzar había cambios ajenos en `README.md` y `docker/iniciar.ps1`; no se modificaron. Este documento es una caracterización parcial del código y de pruebas, no una certificación integral ni un cambio de producto.

## Alcance y lectura de la matriz

Las operaciones y permisos se obtuvieron por lectura del código; no se recorrieron manualmente las pantallas ni se verificó cada combinación de rol. «Visible» significa navegación/controles observados, no autorización segura en el backend. Las tablas son las principales, no una lista exhaustiva de joins. El esquema versionado `docker/init/01-schema.sql` no demuestra el estado de una base existente. Las suites y su resultado se detallan más abajo: ninguna prueba listada debe interpretarse como cobertura completa de la fila.

| Pantalla/operación; entrada → resultado esperado por código | Tablas principales / dependencia | Admin / galpón observado | Evidencia y brecha |
|---|---|---|---|
| Login y ventana principal: credenciales → sesión, navegación y cierre | `Servicios/SessionManager`, autenticación y credenciales locales | Perfiles Admin y Galpon; el menú oculta materiales, categorías, proveedores, obras y ubicaciones para no-admin | LoginUsability: foco y asincronía con dobles; no autenticación ni recorrido real. `VistaGeneral.AplicarRol`. |
| Materiales: alta/edición, stock y proveedores → saldo e historial | `materiales`, `materiales_proveedores`, `movimientos`, `movimientomaterial` | Entrada de menú oculta a galpón; no inferir seguridad en datos | `MaterialDatos`; consistency: 18 comprobaciones con zona horaria alineada; falla registrada con zonas distintas. |
| Categorías, proveedores, obras y ubicaciones: CRUD → catálogos | `categorias`, `proveedores`, `obras`, `ubicacion` | Admin visible; botones ocultos a no-admin | Vistas de Gestión y `ComunDatos`; sin prueba funcional completa. |
| Recortes, fletes, canaletas/fotos, herramientas e insumos: gestionar artículos y sus datos/archivos | `recortes`, `canaletas_plegados`, `canaletas_plegados_fotos`, `herramientas`, `herramienta_movimientos`, `insumos`, `insumo_movimientos`; fletes y almacenamiento local | `AplicarRol` no oculta sus entradas; restricciones particulares por operación no auditadas | Vistas de Gestión; sin recorrido funcional ni prueba de fotos. |
| Inventario y conteo: consultar stock, registrar medición y cerrar → diferencias | `materiales`, `recortes`, `conteos_inventario`, `conteos_inventario_detalle` | Inventario oculta precios a no-admin; conteo contiene controles Admin; verificar permisos en casos concretos | `Vista_Inventario`, `Vista_Conteo`, `ConteoInventarioDatos`; sin prueba de cierre/concurrencia aquí. |
| Movimientos e historial: origen/destino y líneas → remito, stock e historial; editar/eliminar | `movimientos`, `movimientomaterial`, materiales, recortes, insumos | Las vistas contienen comprobaciones Admin para acciones y precios; consultar operación concreta antes de migrar | `Vista_Movimientos`, `Vista_HistorialMovimientos`, `MovimientoDatos`; consistency: 18 comprobaciones con zona horaria alineada; no cubre todas las operaciones. |
| Órdenes (módulo, alta, listado, revisión): líneas → crear/editar/eliminar; recibir parcial o completamente | `ordenes_compra`, `ordenes_compra_detalle`, `ordenes_compra_faltantes`, materiales | Alta/editar/eliminar Admin; revisión admite Admin o Galpon; galpón ve abiertas para recepción | Cuatro vistas `Vistas/Movimientos/Ordenes`; sin integración de recepción ejecutada. |
| Kardex, stock, informe de obra y búsqueda: filtros → grilla/PDF/resultados | `movimientos`, `movimientomaterial`, materiales y obras; generadores PDF | Stock y valorización ocultan precios según rol; informe tiene condición específica | Search: seis casos de coordinación/cache; Branding: encabezados PDF demo, no comparación de totales reales. |
| Actualización, borradores y utilidades | updater remoto, archivos locales | No auditados por rol | Sin ejecutar updater ni reemplazo de EXE; smoke de marca desconecta el handler `Shown`. |

Modalidades auxiliares inventariadas: filtro de materiales, selección de proveedores, stock bajo, flete/insumos/recortes de movimiento y configuración de reportes. La matriz no afirma ejecución manual de estos formularios.

## Contratos que deben preservarse o tratarse explícitamente

- `MaterialDatos` usa stock esperado y control de concurrencia para edición manual; ajuste interno diario coordina `pg_advisory_xact_lock(141414, fecha)` según el plan y código. El protocolo de locks completo debe volver a verificarse en integración antes de migrar.
- Un movimiento coordina saldos e historial; actualmente `Vista_Movimientos` guarda movimiento y luego insumos por separado. La atomicidad conjunta es una corrección C, no comportamiento ya demostrado.
- Recepción de orden actualiza stock y estados; evitar doble acreditación; modelos usan `decimal` y el esquema declara cantidades `integer`. No introducir recepción automática como movimiento Kardex sin decisión funcional posterior.
- Conteo registra diferencias y su cierre no ajusta automáticamente stock. El bloqueo y verificación de cabecera para **todos** los escritores de detalles es corrección C pendiente, no garantía de baseline.
- Fotos preexistentes: almacenamiento local bajo `%LOCALAPPDATA%/SteeLog/Storage`; conservar rutas y no borrar datos. El seed versionado declara, entre otros, ubicaciones 1–4, categorías demo, obras, proveedores y materiales; no se afirma que estén presentes en DB existente. `02-seed.sql` evita retroceder la secuencia de ubicaciones.
- Configuración: `ConexionDB` carga recurso embebido y luego `appsettings.json` opcional al lado del ejecutable; pruebas comprueban prioridad, errores sanitizados y liberación de handles. `App.config` está versionado y contiene runtime/binding redirects; no tratarlo como secreto ni eliminarlo por `.gitignore`.

## Clasificación R/C/D para fases posteriores

| Tipo | Alcance previsto | Evidencia aún necesaria |
|---|---|---|
| R | Extraer dominio, SQL, consultas, presenters y PDF manteniendo resultados y UX | Caracterización de saldos, filtros, permisos, reportes y recorridos reales por módulo. |
| C | Transacción conjunta de movimientos e insumos; bloquear cambios de conteos cerrados y permisos previstos; validar cantidades OC y fotos contra esquema | Rollback SQL real, carreras, estados y validaciones, sin cambios SQL improvisados. |
| D | Sustituir auth cloud por catálogo local y updater remoto por fixture local declarada | Ensayo offline completo sin reemplazar EXE ni servicios externos. |
| Fuera de ruta | Registrar recepción OC como movimiento automático Kardex | Decisión funcional separada y tratamiento de historia/doble conteo. |

## Dependencias, recursos y empaquetado

El host es WinForms `.NET Framework 4.8`. `SteeLog/SteeLog/SteeLog.csproj` declara `Guna.UI2` mediante `../packages/Guna.UI2.WinForms.2.0.4.7/lib/net48/Guna.UI2.dll`; archivo presente `SteeLog/packages/Guna.UI2.WinForms.2.0.4.7/lib/net48/Guna.UI2.dll`, identidad `Guna.UI2, Version=2.0.4.7, PublicKeyToken=8b9d14aa5142e261`, SHA-256 `2130C7489F5A5E21812C1EAB37DC4903B901861A2D545AA607555BE269091AFD`. La compilación y smoke de formularios usan esa referencia del proyecto; no se hizo trazado diagnóstico separado de `ResolveAssemblyReference` ni se probó el diseñador interactivo de Visual Studio.

DLLs locales versionadas: `SteeLog/SteeLog/packages/itextsharp.dll` 5.5.13.3, `EPPlus.dll` 4.5.3.3 y `BouncyCastle.Crypto.dll` 1.8.9.0; también se versiona `dot-net-transitions.../Transitions.dll`. `FodyWeavers.xml` declara Costura; el proyecto incluye recursos de marca e `appsettings.json` embebidos, además de ícono y `App.config`. En el build Release temporal de Branding se obtuvo `SteeLog.exe`, `.config`, `.manifest`, `.application` y carpeta `app.publish`, junto a otros artefactos; el harness copia aparte `itextsharp.dll` para compilar su ejecutable de prueba. Esto es salida de build/smoke, **no** prueba de publicación, instalación, paquete autónomo o ejecución del EXE distribuido. `publicar.ps1` interactúa con GitHub y altera versión: no se ejecutó.

## Ejecución y evidencia

Raíz temporal exclusiva: `C:\Users\Gime\AppData\Local\Temp\steelog-baseline-d4caa2a320d4474eb9e2935a5339c330`. Se establecieron `TEMP` y `TMP` a esa carpeta y se ejecutaron secuencialmente mediante `powershell -NoProfile -ExecutionPolicy Bypass -File SteeLog/SteeLog/Tests/<suite>/run.ps1`; cada salida está en `<suite>.log` en dicha raíz. Los scripts fueron inspeccionados antes de correrlos. No se invocaron `Program.Main`, updater, publicación ni reset de volúmenes.

| Suite | Resultado real y límites |
|---|---|
| Configuration | **PASS**, exit 0. Debug/Release build; override, fallback, JSON malformado/vacío/inválido sanitizado, 500 constructores y 30 aperturas fallidas sin crecimiento de handles. `Configuration.log`. |
| search | **PASS**, exit 0, 6 checks sobre invalidación, error/reintento y carga compartida de caché. `search.log`. |
| Branding | **PASS**, exit 0, Release; recursos, ícono y render smoke de login/principal, encabezados PDF. Ejemplos en `steelog-branding-smoke-a2362c17d53c43f1b7009a80cffeb9c8`: seis encabezados, `Kardex-real.pdf`, capturas PNG. Son datos demo y no cubren totales reales ni Designer. `Branding.log`. |
| LoginUsability | **PASS**, exit 0, Debug y Release, 36 aserciones por ejecución sobre foco, validación, asincronía y doble envío; no login real E2E. `LoginUsability.log`. |
| Bootstrap | **PASS aislado**, exit 0 con transporte API validado (`Bootstrap-api.log`): esquema/seed, primer ID 5, último consumido 13 y siguiente tras reseed 14. El intento anterior `Bootstrap.log` es inválido y no cuenta. |
| consistency | **PASS condicionado**, exit 0, 18 checks con PostgreSQL y Windows en `America/Argentina/Buenos_Aires` (`consistency-api-tz.log`); incluye stock, rollback y carreras de edición/eliminación. La ejecución válida previa con PostgreSQL UTC falló `first daily header shared` (`consistency-api.log`); ver fecha y zona abajo. Los intentos iniciales CLI con 3D000/FK (`consistency.log`, `consistency-ipv4.log`) son inválidos. |

### Aislamiento SQL y anomalía del entorno

Los originales apuntan a `steelog-db` y aplican `01-schema.sql`, que puede crear roles a nivel de clúster. Para evitar modificar el clúster existente, se usaron copias temporales de los scripts con rutas `$PSScriptRoot` resueltas al original, nombre de contenedor y puerto adaptados; la segunda copia de consistency además cambió `Server=localhost` por `Server=127.0.0.1`. No se cambiaron aserciones ni scripts versionados. Se empleó imagen local `postgres:17-alpine`, tmpfs, bind a localhost y nombres/labels únicos; se eliminaron solo los contenedores creados tras verificar nombre y label. Bases con nombres `steelog_test_<GUID>` eran propias del harness. No se modificó intencionalmente `inventario_db` ni se resetearon volúmenes.

Se reprodujo `docker exec steelog-db echo HELLO` sin salida y `docker exec steelog-db sh -c 'echo test >&2; exit 5'` sin salida y con exit 0, incluso con `-i`/`-t` y redirección a archivo; `docker exec` a contenedor inexistente sí devuelve error. Las primeras ejecuciones por CLI no son evidencia confiable. La consulta previa de roles al clúster existente también queda indeterminada. El endpoint Docker activo es `tcp://127.0.0.1:2375`: el contraste por API directa creó una ejecución con POST `/containers/{nombre}/exec`, arrancó con POST `/exec/{id}/start` (frames con cabecera de 8 bytes y longitud big-endian) y obtuvo estado/exit real con GET `/exec/{id}/json`. En contenedor efímero propio, `SELECT current_database(), version()` devolvió `postgres` y PostgreSQL 17.11 con exit 0; un SQL inválido devolvió error y exit 1. Para evitar stdin del CLI, el shim temporal copió cada entrada SQL íntegra mediante `docker cp` y ejecutó `psql -f` mediante API. Los scripts `docker-exec-api.ps1`, `Bootstrap-api.ps1`, `consistency-api.ps1`, `consistency-api-tz.ps1` y sus logs están en la raíz TEMP. Este shim es evidencia temporal, no runner versionado; sus sustituciones solo adaptan transporte, contenedor, puerto y host IPv4, sin alterar las aserciones ni el contenido SQL. El contenedor de Bootstrap/primer consistency y el del segundo consistency fueron eliminados tras comprobar nombre y label.

**Fecha y zona:** la primera ejecución confiable de consistency falló `first daily header shared` con el host en Argentina el 2/11 a las 23:23 y PostgreSQL por defecto UTC ya el 3/11. El código toma `DateTime.Today` para el ajuste diario y el harness compara `DATE(fecha)=current_date` del servidor: zonas distintas explican el desajuste observado, sin concluir que haya dos cabeceras. Se repitió sin cambiar aserciones en contenedor propio con `TZ=America/Argentina/Buenos_Aires`; se verificó `current_setting('TimeZone')` y `current_date=2026-11-02`, y pasaron los 18 checks. El PASS está condicionado a esa zona alineada. Fase 3 debe definir y probar explícitamente la fecha de negocio y su reloj; no ocultar el caso UTC.

## Pendientes para completar la caracterización

Contraste del orquestador: se revisaron los logs de las seis familias, los launchers SQL temporales y el shim de transporte; además se inspeccionaron visualmente las capturas de login/principal. El orquestador reprodujo de forma independiente el falso exit 0 del CLI y el exit 5 real por API para el mismo comando de prueba. Esto no reemplaza las verificaciones manuales pendientes. Los hashes SHA-256 de los cambios previos se conservaron: `README.md` = `90E113DAC3B2806F37F0C04963D1FB0A7DFBE84F34C180D4369FE507EE8EB9E1`; `docker/iniciar.ps1` = `27A92887208DF3A0DA4F986645CB8AEF574593A48606FC22D7EA18D4F665DC0F`.

1. Incorporar al runner un transporte Docker confiable y probado con salida y exit no cero; caracterizar diferencias de fecha de negocio y repetir integración en zonas distintas.
2. Recorrer manualmente ambos perfiles, permisos por operación, datos y fotos sobre copia segura; capturar ejemplos PDF con datos representativos y comparar cifras.
3. Comprobar Designer de Visual Studio, carga de ensamblados y empaquetado/ejecución local por separado. Los smoke de formularios no sustituyen esas pruebas.
4. Registrar pruebas de órdenes, conteos y concurrencia faltantes antes de afirmar que preservan las invariantes o cobertura completa.
