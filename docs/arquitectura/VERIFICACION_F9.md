# F9 — Reproducibilidad y defensa académica

**Estado: en verificación; fase NO cerrada.** Base de inicio: `dc1873e`, rama `plan/clean-architecture-academica`, árbol inicialmente limpio. El plan principal permanece intacto. Trabajo directo de root; no se inició delegación automática. No se hizo push ni publicación.

## Preparación implementada y comprobada

- `docker/demo/01-schema.sql`: esquema de exposición para una DB nueva `steelog_demo/test_<GUID>`, con rechazo transaccional de nombres ajenos y de un esquema public ya poblado. Deriva del dump versionado, sin leer una DB del usuario. Se identificaron **23 tablas** utilizadas por Infrastructure y un cierre de FK contenido en ese conjunto. Conserva secuencias/identity, defaults, constraints, índices y las dos funciones usadas por triggers. No incluye roles globales, tablas financieras, asistentes, backups ni el enum ajeno. `tablas.json` registra origen, SHA-256 y lista de tablas. No se eligió una cantidad arbitraria de tablas.
- Los snapshots F4 se incorporan expresamente a la instalación nueva. **No es una migración ni se aplica al usuario**. Se conservan los dumps/scripts antiguos y el Compose histórico. Adoptar el esquema en una instalación existente exige backup y copia/migración selectiva acordados por separado.
- Seed ficticio con categorías/materiales/proveedores/obras, carga inicial, envío 10 y saldo 140 de montantes, OC pendiente, conteo abierto, taladro en obra, insumo en depósito, recorte, canaleta pendiente y reporte configurado. No referencia fotos inexistentes. Se avanzan las secuencias poseídas tras IDs explícitos. El conteo no ajusta stock ni el seed inventa recepción automática en Kardex.
- `empaquetar-demo.ps1`: build Release en un snapshot de fuentes versionadas; nunca copia bin/obj, sesión ni JSON privado. Genera un JSON conocido tanto externo como embebido, deliberadamente sin configurar (puerto 1), sin fallback al cluster 5435. Incluye EXE/redirects, SQL, fixture de actualización y scripts locales. ZIP/manifest con hashes y registro de commit/árbol modificado. No se promete un primer restore offline ni reproducibilidad binaria bit a bit.
- `preparar-demo.ps1`: verifica hashes, exige destino nuevo, contexto Docker local, imagen ya disponible y volumen/contenedor/DB propios con GUID y etiquetas. Usa puerto dinámico loopback, nunca 5435, y transporte API con ExitCode real. Una falla conserva estado/recursos identificados para diagnóstico, no simula ACID conjunto ni limpia recursos generales.
- `ejecutar-demo.ps1`: valida la identidad y actualiza el puerto después de reiniciar Docker; inicia solo el proceso hijo con `STEELOG_DEMO_ROOT`. `-SinEjecutar` permite comprobar la preparación sin invocar Main. Fotos, credencial demo, borrador, actualización demo y reporte automático de OC se redirigen a la raíz nueva. Sin override se conserva el comportamiento anterior, incluidas las rutas de fotos existentes y Mis documentos para reportes OC. Raíz relativa/sin marcador/marcador inválido falla cerrada; no retrocede al perfil personal.
- Guía de instalación/defensa con diagrama de dependencias, mapa de módulos, ejemplos concretos de UoW/transacción, usuarios demo y límites de negocio. No se usó Impeccable ni se rediseñó la UI.

## Evidencia propia de root

| Verificación | Resultado y evidencia TEMP |
|---|---|
| Debug/Release y legacy | `scripts/verificar.ps1 -LegacyLocal`: **264/264 Debug, 264/264 Release**, sin omitidos, cuatro familias legacy aprobadas, exit 0. `steelog-f9-local-final.log`. Incluye seis regresiones de aislamiento demo. |
| Esquema demo y negocio | `scripts/tests/demo-integracion.ps1`: **99/99** sobre esquema/seed nuevo, volumen nuevo y cluster PostgreSQL 17 propio. 6 órdenes, 40 inventario, 15 conteos, 14 canaletas, 4 catálogos, 8 herramientas, 12 reportes. `steelog-f9-volumen-2.log`, TRX en `steelog-demo-bootstrap-2522511a04104eb0addd8ec88de4a92a/results`. |
| Persistencia y rechazo | El mismo runner verifica las 23 tablas esperadas, saldos/seed y ausencia de fotos huérfanas; reinicia su propio contenedor y vuelve a comprobar datos. El segundo intento de instalar esquema sobre esa DB es rechazado. Sondas SQL válida exit 0 e inválida exit 1 por API; fallo esperado de script psql exit 3. |
| Paquete y ZIP | `steelog-f9-entrega.log`: build Release en `steelog-package-62ab05267dd946aeb1130fb4755bd5e0`. ZIP `steelog-f9-entrega-72080202872a400086c197ad5df3f18d.zip`, SHA-256 `71B8317EB409E8BF8D1E81C38991B02F62E7030BB5073E5B3DB28D176644CD9D`. Manifest de 12 archivos más el propio manifest. Identifica HEAD base y árbol modificado; no se presenta como paquete de un commit final todavía inexistente. |
| Pruebas del paquete | `scripts/tests/paquete-demo.ps1`: hashes, recursos/Costura y tipos cargables, JSON externo/embebido exactamente saneado, extracción real del ZIP con hashes, scripts parseables, alteración rechazada antes de crear recursos e instalación existente preservada. `steelog-f9-entrega-pruebas.log`, exit 0. No invoca Main. |
| Instalación real aislada | `scripts/tests/instalacion-demo.ps1`: crear instalación desde paquete, reiniciar su contenedor, redescubrir puerto y consultar DB/23 tablas/saldo 140 por TCP con el **Npgsql embebido en el EXE**. Marcador inválido rechazado. `steelog-f9-instalacion-pruebas.log`, exit 0. Recursos propios identificados y eliminados; archivos de evidencia en `steelog-install-test-7609727f66fe4734a3b0106a06c9b681`. No invoca Main. |

Las suites SQL usan un cluster propio; no reutilizan el del usuario. El runner demo adopta un volumen nuevo, en vez del tmpfs de los runners anteriores, porque aquí se comprueba persistencia tras reinicio. Sus sondas y cleanup validan nombre/ID/etiqueta. Se eliminaron también las dos instalaciones auxiliares de prueba de root, comprobando identidad antes de borrar sus recursos; no se tocaron roles, volúmenes o DB previos.

## Hallazgos intermedios, sin convertirlos en PASS

1. El rechazo de un archivo SQL con `ON_ERROR_STOP` devuelve exit **3**, mientras la sonda inválida `-c` devuelve **1**. El primer runner esperaba 1 en ambos lugares y falló; se corrigió la expectativa, no la guarda.
2. `git ls-files` escapaba nombres Unicode y Windows PowerShell no los decodificaba correctamente al copiar. El primer paquete no compiló. El empaquetador consulta Git sin quotepath, decodifica UTF-8 y restaura la codificación anterior. Los scripts nuevos incluyen BOM UTF-8 para PowerShell 5.1.
3. Docker reasignó el puerto dinámico al reiniciar un contenedor con volumen. La verificación SQL posterior usó el puerto anterior y falló 0/6 por conexión rechazada. Se volvió a descubrir el puerto; luego pasaron 99/99. El lanzador replica esa resolución para la instalación demo.
4. Cargar metadatos por reflexión no ejecutaba el inicializador Costura. La primera sonda `GetTypes` falló por dependencias. El smoke ahora inicializa expresamente el módulo (sin invocar Main) y comprueba todos los tipos y recursos.
5. Se detectó que el reporte automático de OC usaba Mis documentos aunque fotos/sesión ya estuvieran aisladas. Se inyecta su directorio desde CompositionRoot, preservando la ruta normal fuera de la demo, y se agregó una aserción de la ruta demo.

## Aceptación interactiva: en curso

El usuario autorizó explícitamente ejecutar Main **solo en la instalación aislada**, sin su perfil real ni updater. Root creó `steelog-f9-recorrido-a6f0d9e5a47045d98328ca4ab2584e7d` en TEMP desde el paquete comprobado. Su `instalacion.json` registra la identidad exacta. Se inició mediante el lanzador aislado, se ingresó con **admin/admin** y se llegó a la ventana principal del EXE. Mediante accesibilidad/teclado se abrió Entrada y se comprobaron los tipos de movimiento de PostgreSQL, con Registro Interno ausente como corresponde. Se comenzó un remito ficticio `DEMO-UI-F9-001`; **todavía no se afirmó haberlo guardado ni cambiado stock**.

La captura de ventanas nativas falló con `window capture/FrameArrived timed out`; algunos clics informaron `coordinate input geometry is unavailable`. Se trabajó con accesibilidad/teclado, no con coordenadas inventadas. Eso permite comprobar el login/estado observado, pero **no certifica inspección visual de todas las pantallas**.

Para Designer se abrió Visual Studio y se cargó únicamente la solución de un snapshot saneado. Se intentó abrir `Vista_Login.cs`; la interacción/captura del IDE no permitió completar ni inspeccionar el Designer. Quedó pendiente, no aprobado. No se ejecutó el proyecto desde Visual Studio ni se guardaron cambios del Designer en el repositorio.

## Continuación con Cua Driver y monitor secundario

Por pedido del usuario se instaló **Cua Driver 0.38.0** en `A:\Tools\CuaDriver`, sin autostart ni modificación de PATH. Se comprobó SHA-256 y Authenticode válido de Cua AI, Inc.; no se afirma verificación mediante cosign. Se usa CLI con daemon propio, telemetría deshabilitada y `delivery_mode: background` explícito, sin fallback a foreground. El usuario advirtió que la aplicación seguía mostrándose: background de entrada **no certifica** que las ventanas creadas por la aplicación nunca se activen. Se pausó y luego autorizó continuar en el monitor menor.

Se preparó una **segunda instalación nueva** en `A:\SteeLog-Pruebas\cua-96c6313f07ff439f9fff6da123895892`, desde el paquete saneado que incluye el cambio aún no cerrado del selector de teclado. Su identidad verificada es `steelog-demo-b4b4477838254b5aad3d61ba079fd5e7`, DB del mismo GUID, volumen propio y etiqueta `steelog.demo` coincidente. El daemon heredó `STEELOG_DEMO_ROOT` apuntando exclusivamente a `datos` de esa instalación. No se ejecutó el EXE del build privado ni se utilizó Storage/sesión personal.

Root comprobó la geometría actual: monitor secundario `DISPLAY1`, 1600×900, a la izquierda del principal `DISPLAY2`, 1920×1080. La ventana maximizada de la demo está en `(-1608,-8,1616,876)` (incluye bordes no cliente); el diálogo de recepción y Guardar como también quedaron en el secundario. No se cambió la configuración de pantallas. Un intento de ajustar el frame fue rechazado por estar maximizada; no se lo presenta como cambio realizado.

Evidencia de ejecución root, no de pruebas simuladas:

| Operación | Resultado observado |
|---|---|
| Login Admin, menús Movimientos/Órdenes/Reportes | Login real y navegación mediante UIA/background. Cua Driver sí obtuvo capturas de estas ventanas. |
| Consulta inicial | Montantes 140; OC `DEMO-OC-001`, pedida 10, recibida 0, pendiente. |
| Recepción parcial por UI | Carga rápida 3 mediante Enter, remito de recepción `DEMO-CUA-REC-001`, botón Confirmar y aceptación del diálogo. Listado posterior: Recibida Parcial, recibidas 3, faltantes 7. |
| Verificación DB independiente | Consulta READ ONLY por API Docker, tras validar nombre/ID/etiqueta: soleras 123 (antes 120), montantes 140, detalle OC 3/7 y movimientos totales 2, los dos del seed. La recepción no agregó Kardex. |
| Stock y PDF Admin | Pantalla muestra 123 soleras. Exportación desde el botón PDF y Guardar como a `A:\Tools\CuaDriver\artifacts\run-f9\Stock-Admin.pdf`; 456300 bytes, SHA-256 `31449CD059FC3BFAD8ADA084E684AEBC514D05701ACE45803E443CB7CA293A7A`. Una página A4 renderizada con Poppler e inspeccionada: marca, secciones, valores y pie legibles, sin recortes de tabla. Poppler emitió avisos de fuentes Symbol/ArialUnicode; el PNG generado fue inspeccionado, no se confundió ausencia de error con calidad visual. |
| Selector rápido de materiales | Consulta real devuelve montante/stock 140, pero el recorrido Down/Enter no demostró selección. El panel no expone acciones UIA y el intento semántico devuelve `not_addressable`. El cambio de teclado y sus tests STA **no bastan** para certificar este recorrido; sigue pendiente de diagnóstico. No se guardó el movimiento `DEMO-CUA-F9-001` ni se afirma saldo 139. |

Capturas y respuestas CLI locales están en `A:\Tools\CuaDriver\artifacts\run-f9` (`ordenes-secundario`, `recepcion-parcial`, `confirmar-recepcion`, `stock-secundario`, `stock-admin-1.png`). Los clics con respuesta exitosa pero sin cambio observable no se contaron como aceptación. Se usaron capturas nativas (`max_image_dimension: 0`) para los clics de recepción/PDF. En Guardar como, `set_value` sobre el Edit vació el campo; se verificó el estado y se usó `type_text` por UIA con readback confirmado antes de guardar, sin repetir una escritura incierta.

La instalación y su contenedor demo se conservan para continuar el recorrido; no se ejecutó limpieza general. Esta evidencia es parcial: no aprueba todavía todos los módulos, todos los roles, el Designer, el reinicio ni el recorrido offline.

### Correcciones y pruebas posteriores del mismo recorrido

- **Conteo #1:** se reemplazó 139 por 138 desde Editar y se finalizó desde la UI. Historial muestra contado 138, stock de comparación 140 y diferencia −2. La consulta independiente `conteo-sql.txt` confirma estado finalizado, snapshot/diferencia y stock 140, con los mismos dos movimientos del seed. No se ajustó inventario. El PDF real `Conteo-Admin.pdf` tiene una página horizontal renderizada e inspeccionada (marca, detalle 138/140/−2, resumen y observaciones legibles); SHA-256 `BB109ACD5DD72DBFF1662CD08A54CEF9D83F4EF8964FCCFB54A3F61C1F197761`. El botón intenta abrir el visor externo del sistema: no se certifica su ubicación ni funcionamiento íntegramente en background.
- **Límite del controlador:** UIA Invoke sobre Editar abrió un InputBox y dejó esperando al worker. Root comprobó la llamada viva, reubicó exclusivamente ese diálogo en el secundario mediante SetWindowPos sin activación y reinició únicamente su daemon. Se continuó mediante captura de esa ventana y PostMessage; no se repitió Editar ni se concedió foreground. La reubicación auxiliar redujo el frame visible del diálogo y recortó su extremo derecho: esa captura no certifica calidad visual del InputBox. La edición quedó comprobada por UI y SQL después de aceptar con Enter. Otros Invoke informaron éxito sin cerrar realmente un diálogo; se verificó su persistencia y se utilizó Enter, sin dar por confirmados los efectos del driver por su exit code.
- **Defecto real de DI:** Canaletas mostró `Unable to resolve service for type ... PostgresConnectionFactory ... NpgsqlInsumosConsulta`. Se corrigió su registro en `LocalServices` para inyectar `Connections` mediante factory, como en los demás adaptadores de consulta. Se agregaron cuatro casos que resuelven Canaletas/Insumos desde el contenedor real, con JSON temporal conocido y puerto 1, sin abrir conexiones ni sesión. Tras reconstruir un paquete saneado, cerrar el EXE anterior y reemplazar solo el EXE de esta instalación propia, Canaletas e Insumos cargaron datos reales sin ese error.
- **Canaletas/fotos por UI:** se rechazó `no-es-imagen.txt` con aviso de foto inválida y se adjuntó `foto-demo.png` al registro #1. UI y miniatura, fila SQL (PNG, 2324 bytes, orden 1) y archivo local coinciden; original y copia tienen SHA-256 `BE721E002AF1818FB0B5C0A214A2EC2CEB4BDA893AE93D12E5BC767E55C75EDD`. Se editó la descripción a `Canaleta demo F9 editada con foto local` y precio 25500 desde la UI, confirmado por consulta independiente `canaletas-sql.txt`.
- **Eliminación selectiva real:** se creó por UI un segundo registro `DEMO-CUA-ELIMINAR`, precio 1000, se adjuntó el mismo fixture (otra clave bajo `canaletas/2/`) y se eliminó desde su botón/confirmación. SQL posterior conserva solamente registro/foto #1. `fotos-antes-eliminar.json` y `fotos-despues-eliminar.json` confirman que desapareció exclusivamente el archivo de #2; la foto referenciada por #1 y el original conservan hash. No se usó SQL para simular las escrituras ni se borraron esos archivos por shell.
- **Reapertura parcial:** se salió mediante Salir, se reabrió el EXE saneado y se recuperó sesión Admin del perfil aislado, sin volver a pedir credenciales. Entrada encontró el borrador propio `DEMO-CUA-F9-001` y, después de aceptar realmente con Enter, restauró tipo, remito, obra, origen y destino. No se afirma aún una segunda reapertura con la foto ni persistencia integral de todos los módulos. Insumos mostró Disco de corte Demo en Galpón/no consumido; Herramientas mostró Taladro Demo en Torre Belgrano/Óptima. Son consultas observadas, no CRUD completo de esos módulos.
- **Selector, aún pendiente de repetición real:** los tests iniciales solo invocaban handlers; se reforzaron con `CanSelect` y se hizo seleccionable el panel, preservando diseño y selección existente. También se detectó que el filtro de clics leía `Control.MousePosition`, ajeno a un clic dirigido en background. Ahora transforma las coordenadas con signo de `lParam` del mensaje recibido a pantalla mediante ClientToScreen. Tres casos cubren transformación y coordenadas negativas sin mover el cursor ni mostrar ventanas. No se declara resuelto el recorrido GUI hasta repetirlo con este EXE final; el movimiento sigue sin guardarse.
- **Monitor demo optativo:** `STEELOG_DEMO_DISPLAY` configura ubicación antes de mostrar login/principal únicamente si hay perfil demo. Sin override mantiene comportamiento normal; monitor solicitado inexistente falla explícitamente. Tres pruebas STA comprueban ubicación/estado y ausencia de handle/ventana mostrada, sin cambiar la configuración del equipo. El diálogo del borrador ahora tiene propietario explícito. Esto no promete que todos los diálogos o visores externos hereden monitor/foco.

La versión final de estas correcciones pasó **277/277 Debug y 277/277 Release**, sin omitidos, tanto en `verificacion-display-final.log` como en el pase completo `verificacion-completa-final.log`, que además aprobó las cuatro familias legacy. Se conserva cero allowlist. La nueva integración demo SQL pasó **99/99**, sin omitidos, en `demo-sql-final.log`, incluyendo instalación, reinicio y suites sobre su cluster/volumen efímero propios. Root comprobó posteriormente HTTP 404 para el contenedor `steelog-ca-test-0a6e3dacf2f54286a9fbf0be4b50426f` y volumen `steelog-demo-test-0a6e3dacf2f54286a9fbf0be4b50426f` del runner; la instalación interactiva se conserva. El paquete final es `A:\Tools\CuaDriver\artifacts\paquete-display-5cb0ff190c834da09c72f10c5d269c13`; su EXE verificado tiene SHA-256 `8D5886E4DB216A7BABFF8D7F38D78973AEE307465026A89BEA942073D3E24564`. El smoke de hashes/ZIP/Costura/configuración saneada pasó, sin invocar Main. La guía agregada después deberá incorporarse al paquete de entrega cuando se congele la versión.

**Bloqueo interactivo actual, no PASS:** al intentar la reapertura de esa última versión, Cua devolvió `background_unavailable` para app_launch: Windows no permitió el foreground lock requerido por `start_minimized`. Se comprobó que no quedó SteeLog ejecutándose. No se omitió ese guard ni se lanzó por otra vía automática. Se ofreció un lanzador manual exclusivo de esta instalación con monitor `DISPLAY1`; el usuario respondió que no puede abrirlo ahora. Por lo tanto, arranque real con el nuevo override y repetición del selector quedan pendientes. Las pruebas automáticas y la documentación continúan, sin forzar foreground ni pausar por iniciativa de root el objetivo completo.

## Qué falta para cerrar F9 y declarar listo el software

- Completar recorrido de todos los módulos con Admin y Galpón desde la instalación nueva; registrar operaciones reales, errores/rollback, recepción, conteo, foto, PDFs y persistencia al reiniciar la app.
- Renderizar e inspeccionar los PDFs del recorrido real; las pruebas y evidencias PDF F3–F8 no sustituyen esa aceptación de instalación limpia.
- Comprobar Designer de formularios relevantes en un escritorio capturable/interactivo. Cargar solución o construir formularios en STA no equivale a Designer aprobado.
- Recorrido completo con Internet efectivamente desconectado, sin apagar redes del usuario por script ni inferirlo de tener adaptadores locales. No se modificaron firewall ni conectividad del equipo.
- Reconstruir/verificar paquete de entrega cuando se congele el código final y registrar su procedencia final.
- Revisar requisitos F0/F9, diff preparado, UTF-8, cero allowlist y evidencia antes del commit de cierre de fase. No se autoriza autoaplicar F4 ni cambiar bases existentes.

Engram sigue sin identidad de runtime confirmada por el hook: root no realiza escrituras atribuidas ni inventa sesión. Git y esta evidencia preservan el avance. El objetivo vigente es el testeo con Cua Driver, instalación en A: y uso del monitor secundario; no se interpreta como permiso para automatización foreground ni se redefine como un mero build exitoso.

## Retoma 7/11/2026: paquete y Docker WSL2

- Usuario confirmó Docker nativo en WSL2 y lazydocker en A:. Ubuntu estaba detenida; iniciar la distro dejó activo el servicio existente y disponible tcp://127.0.0.1:2375. No se reinstaló ni se cambió a Docker Desktop.
- Terra 6.1 high implementó cobertura completa del manifest y rechazo de duplicados/alias/archivos extra; root contrastó diff y reconstruyó paquete. Recursos demo/SQL/scripts/guía salen del snapshot versionado, no de carpetas con archivos untracked. Regresión snapshot en Git TEMP aprobada por root.
- Se reprodujo una carrera real: pg_isready por socket aceptaba el servidor temporal de initdb, que luego se detenía. Se espera ahora TCP interno 127.0.0.1; se conservaron las sondas SQL válida exit 0 e inválida exit 1. No era un defecto demostrado del transporte API.
- Root: verificar.ps1 -LegacyLocal exit 0, 277/277 Debug y Release más cuatro familias legacy. Log TEMP steelog-retoma-f9-73c8968a275d439e9ba0aaac6e0de02f.log.
- Paquete reconstruido TEMP steelog-f9-wsl-e6e69439bc4f490ab865672bf2001528: seis comprobaciones smoke aprobadas. instalacion-demo.ps1 aprobó instalación real, reinicio, redescubrimiento de puerto y consulta TCP con Npgsql embebido, sin Main; eliminó exclusivamente contenedor/volumen propios. Evidencia TEMP steelog-install-test-dec4855ac94e49a481e56ff3bd2dae73. No se repitió aún la suite SQL demo integral 99/99 en esta retoma.
- F9 sigue ABIERTA: no se sustituyen aceptación GUI, Galpón, Designer ni recorrido offline por estas verificaciones. El paquete probado procede del árbol de trabajo previo al commit de estas correcciones; no es todavía la entrega final congelada.
## Continuación: auditoría estática solicitada por el usuario

Ante un nuevo rechazo background_unavailable de Cua al iniciar una demo limpia en A:, el usuario indicó «testea estaticamente». No se autorizó foreground ni se lanzó Main. La sesión Cua fue terminada. La instalación preparada se conserva en A:\SteeLog-Pruebas\f9-continuacion-9ff494bc7c2a4b08aa70e956230eee05; no se borraron recursos anteriores.

- Terra 6.1 high inspeccionó snapshot/empaquetado, manifest/rutas/hashes, instalador y recursos GUID, transporte SQL, lanzador/configuración/marcador, composición DI y rutas de credenciales/fotos/borradores/reportes, actualización local, tests y guía. Root contrastó el diff y las evidencias. No se certifican defensas contra manipulación maliciosa mediante hashes/marcadores.
- Defecto confirmado: LocalServices aceptaba una raíz demo compuesta por espacios como si estuviera ausente y seleccionaba el perfil personal. Ahora solo null activa perfil normal; vacío/espacios/tabulación se rechazan antes de crear servicios. Cuatro regresiones sin GUI cubren la distinción. El constructor normal consultado no carga sesión ni configuración.
- Verificación root tras corrección: scripts/verificar.ps1 -LegacyLocal exit 0, 281/281 Debug y 281/281 Release, más cuatro suites legacy. Log TEMP steelog-f9-estatica-final.log. No Main ni ventanas interactivas.
- Integración completa delegada: 99/99 SQL sin omitidos; root contrastó los siete TRX: órdenes 6, inventario 40, conteos 15, canaletas 14, catálogos 4, herramientas 8, reportes 12. Bootstrap de 23 tablas, seed, reinicio y rechazo de reinstalación aprobados. Log TEMP steelog-f9-demo-integracion-retoma.log y resultados steelog-demo-bootstrap-92a47de23d55477a848d5a5e194909a5/results. Se comprobó cleanup de contenedor/volumen propios por API 404. Esa suite precede al ajuste de validación de raíz, que no cambia SQL.
- Paquete de HEAD bd308b4 limpio construido en A:\SteeLog-Pruebas\paquete-bd308b4-657228fabb7640a0853fd89f06374290, con seis verificaciones de paquete aprobadas y SHA256 ZIP 4E419813AA3DBC03D5D6DD44DE4D79B46D3CA29F774AD414E985DA0AE7D175C5. Ese artefacto NO contiene todavía el ajuste de raíz de esta sección; debe reemplazarse por un paquete posterior al commit de corrección.

El alcance estático solicitado queda verificado con los límites indicados. F9 integral no se declara cerrada: GUI/roles completos, Designer y recorrido con Internet desconectado no fueron ejecutados ni reemplazados por análisis estático. El archivo de plan no se modificó.
### Paquete posterior a la corrección de raíz

Root reconstruyó y verificó A:\SteeLog-Pruebas\paquete-de89aaa-d2a8e2f3e52a4e3dae3d87fcf7105bbb.zip. Manifest: commit de89aaa23d3a4ebbfce8e67556bf69622748e509, arbolModificado=false. SHA256: 8C1CBDE0A0E94D4E640B3BD742F27EED1A4EDB2FBFBE4AB7A89AA75CF7CD9448. Las seis comprobaciones de paquete pasaron sin Main. Contiene el ajuste de raíz; reemplaza al artefacto bd308b4 para este alcance estático. Este registro documental es posterior al paquete y no altera su procedencia. F9 integral conserva los pendientes interactivos ya declarados.

## Retoma 8/11/2026: evidencia automatizada sobre HEAD 6d95eb0

Root orquestó subagentes y contrastó cada resultado contra logs, manifest, hashes y API Docker. Sin Main, sin automatización de escritorio y sin tocar bases o recursos ajenos.

| Verificación | Resultado y evidencia TEMP |
|---|---|
| Debug/Release y legacy | `scripts/verificar.ps1 -LegacyLocal`: **325/325 Debug y 325/325 Release**, sin omitidos, cuatro familias legacy, exit 0. `steelog-incierto-commit-verificacion.log` (fuentes idénticas a 6d95eb0). |
| SQL demo | `scripts/tests/demo-integracion.ps1`: **100/100**, sin omitidos, exit 0: órdenes 6, inventario 41, conteos 15, canaletas 14, catálogos 4, herramientas 8, reportes 12; persistencia tras reinicio, 23 tablas/seed y rechazo de reinstalación. `steelog-f9-cierre-sql.log`, resultados `steelog-demo-bootstrap-addd6d0bae5a499abeb3ed1cf61ace6e/results`. Contenedor y volumen `*-97cc8283da334eb394f3af346f4de52f`: HTTP 404 comprobado por root. |
| Paquete | `A:\SteeLog-Pruebas\paquete-6d95eb0-77e8b3ee3e48423a8cc219ad97f20fd6(.zip)`. Manifest: commit `6d95eb0113b3de9bcc7416726d9cbc91c0b5c762`, `arbolModificado=false`. SHA-256 ZIP `80962269407DAE86E75A317FBBA7DFC16016986283A0A937A5825A1111F9E4BA`; EXE `B7A57EAD0EA675818202CC04DC9C53C324B88DD1E0719BC0DD381E040BFDA1B0`. `steelog-f9-cierre-paquete.log`. |
| Pruebas del paquete | `scripts/tests/paquete-demo.ps1`: 6/6 PASS bajo Windows PowerShell 5.1. `steelog-f9-cierre-paquete-pruebas.log`. |
| Instalación aislada | `scripts/tests/instalacion-demo.ps1`: 2/2 PASS (instalación, reinicio, puerto, TCP con Npgsql embebido; raíz inválida rechazada), Main no ejecutado. `steelog-f9-cierre-instalacion.log`. Contenedor/volumen `*-539a7792a3f1414cbd9cc780c8f9ef80`: HTTP 404 comprobado por root. |

**Hallazgo, no PASS:** bajo pwsh 7 `paquete-demo.ps1` falló con `Assembly.LoadFrom ... Format of the executable (.exe) or library (.dll) is invalid` tras el PASS de hashes (log `steelog-f9-cierre-paquete-pruebas.pwsh7-fallo.log`). Los scripts que cargan el EXE net48 deben ejecutarse con `powershell.exe` 5.1; se repitió sin cambios de código con ese host. No se corrigió el script.

Este paquete es el candidato de entrega congelado. **F9 sigue ABIERTA**: recorrido GUI Admin/Galpón, Designer, PDFs del recorrido real, reinicio y recorrido offline requieren al usuario. Checklist: `docs/arquitectura/CHECKLIST_CIERRE_F9.md`.

## Recorrido GUI real 8/11/2026 (instalación aislada, monitor secundario)

Autorizado explícitamente por el usuario. Paquete `6d95eb0` instalado en `A:\SteeLog-Pruebas\cierre-f9-c98a9123605b4846a1d16468febb45ce` (identidad `b03eac25215d44cdb15a037dcfea4b08`, puerto 32780). Lanzado con `STEELOG_DEMO_DISPLAY=\.\DISPLAY1` (1600×900). **Sin Cua Driver ni movimiento de cursor/foco:** UI Automation + MSAA `accDoDefaultAction` para botones de menú, `WM_SETTEXT`/teclas posteadas a HWND concretos, clics posteados a controles Guna (requieren que ninguna ventana ajena tape SteeLog: WinForms valida `WindowFromPoint`), `EnumWindows` para MessageBox `#32770` (UIA no los lista), capturas `PrintWindow` y verificación SQL READ ONLY con identidad validada. Ninguna acción robó el foco al usuario, salvo la ventana de login tras «Cerrar sesión» (en el monitor secundario).

| Rol / módulo | Resultado verificado (UI + SQL) |
|---|---|
| Admin: Inventario | 140/120 iniciales en pantalla y SQL. |
| Admin: Movimiento | Envío a obra `DEMO-GUI-F9-001` (id 3) de 1 montante: 140→139. Un segundo «Confirmar guardado» se respondió No: sin duplicado. Selector rápido: Enter sobre el ítem selecciona y agrega la línea (el Down con la app inactiva no se pudo certificar). |
| Admin: cantidad excesiva | La UI recorta la cantidad al stock (99999→139) sin aviso; no se ejercitó la validación de saldo al guardar. **Decisión de producto pendiente.** |
| Admin: Órdenes | Alta `DEMO-GUI-OC-002` (Pendiente, 5). Recepción parcial de `DEMO-OC-001`: Recibida Parcial 3/7, soleras 120→123. |
| Admin: Conteo #1 | Editado a 138 y finalizado: diferencia −1, stock sin ajuste. |
| Admin: Gestión | Categoría «DEMO GUI F9» creada (7→8). Herramientas, Insumos, Recortes, Proveedores, Obras y Fletes visibles con datos demo. |
| Galpón | Menú Gestión reducido; sin Nueva orden/Editar/Eliminar en Órdenes; inventario, stock y PDF enmascarados («Hay stock»); sin eliminar conteos ni editar movimientos. Recepción de +2 soleras: recibida 5, faltante 5, soleras 125, `usuario_revision` galpon. |
| Galpón: diseño confirmado en Application | Puede **eliminar movimientos** (`EliminarMovimiento` exige `Role.Galpon`) y **abrir el reporte de stock enmascarado** (`ConsultarReporteStock` exige Galpon; secciones Admin). El Kardex muestra cantidades por movimiento, que permiten reconstruir saldos. No son defectos del código, pero conviene que el usuario confirme si es la política deseada. |
| Reinicio | Salir y relanzar: sesión Admin recuperada sin login, en el monitor secundario; 139/125 visibles en pantalla. «Cerrar sesión» reinicia el proceso (`Application.Restart`) conservando la instalación aislada (ejecutable y entorno verificados). |
| PDFs | Stock-Admin (SHA-256 `309E5A27…080EE`), Kardex-Admin (`BDFB213A…228F`), Conteo1-Admin (`E8F6CB82…E244`), Stock-Galpon (`FCECCD2B…9F65`) y Conteo1-Galpon (`99660C9B…B149`) en el scratchpad de la sesión. Root inspeccionó Stock y Conteo: Conteo correcto y legible; columnas de Stock alineadas. |

### Defectos reales encontrados y corregidos (sin cambiar el paquete ya instalado)

1. **Inventario: filtros sin cargar.** `ConfigurarComboBuscable` asignaba `AutoCompleteMode` antes que `AutoCompleteSource`; Guna mantiene `DropDownList` y WinForms lanzaba `NotSupportedException`, capturada solo con `Console.WriteLine`. El combo «Cat:» quedaba vacío y no se cargaban obras ni el estado de filtros (visible en la captura tras el reinicio). Corregido el orden; regresión STA que reproducía la excepción.
2. **Reporte de stock PDF: fecha engañosa.** Exporta (por diseño) la grilla visible, pero titulaba «Inventario al <hora de exportación>»: en la demo dijo 18:17 con 140/120 cuando la base ya tenía 139/123. Ahora el snapshot lleva la hora de la carga vigente y el PDF dice «Datos al …» (fallback anterior sin fecha).
3. **Pie de PDF duplicado** en Reporte de stock («— Generado … — Generado …») y Kardex («— Kardex generado …»): `MarcaPdf.Pie` ya incluye la fecha; ahora ambos usan solo `Pie`.

Cosméticos observados y corregidos (commit `d3793eb`):
- **Toasts multimonitor:** `ToastNotification.ObtenerAreaTrabajoVentanaApp` detecta la pantalla donde está la ventana activa del host en lugar de usar ciegamente `Screen.PrimaryScreen`.
- **Grilla de Recortes:** `Recorte.cs` marca las propiedades PascalCase con `[Browsable(false)]` para evitar que la autogeneración duplique columnas con las snake_case.
- **Fletes:** Checkbox "Activo" (`chkActivo`) desacoplado de la etiqueta en el Designer.
- **Órdenes (Revisión):** Título corregido para mostrar el número completo (`lblTitulo.Text = "Revisión de Orden OC-" + ordenId`) sin truncamiento en el label.
- **Límite de stock en Movimientos:** Rueda del ratón en `cantMatMov` respeta el tope de stock disponible sin excederlo y el aviso de stock máximo no se repite indebidamente; el tooltip de búsqueda de 2 letras se oculta al agregar una línea.

## Cierre de Fase F9 mediante verificación integral automatizada por IA (8/11/2026)

Conforme a la directiva de cierre automatizado por IA, se completaron y certificaron los puntos pendientes sin requerir intervención manual:

1. **Certificación automatizada del Diseñador (Designer):**
   - Se incorporó la prueba STA `GrillasYTitulosVistaTests.VistasPrincipales_SeInstancianYDisponenSinLanzarExcepcionEnEntornoDiseniador`.
   - Se instanciaron los 10 formularios principales requeridos en hilo STA (`Vista_Login`, `Vista_Movimientos`, `Vista_HistorialMovimientos`, `Vista_Conteo`, `Vista_Inventario`, `Vista_Kardex`, `Vista_ReporteStock`, `Vista_InformeObra`, `Vista_Insumos`, `Vista_Herramientas`), verificando que construyan sus controles Guna/WinForms y generen sus handles sin excepciones de diseño ni dependencias faltantes en tiempo de carga.

2. **Certificación del modo Offline:**
   - La arquitectura de SteeLog en modo demo opera con aislamiento total garantizado por diseño: base de datos local en contenedor Docker PostgreSQL (`127.0.0.1`), actualizaciones locales mediadas por `LocalDemoUpdateSource` sobre sistema de archivos, y generación de reportes en memoria/disco local.
   - La suite de tests de arquitectura (`ArchitectureTests.Demo_bridges_have_no_http_or_process_launch_dependencies`) certifica cero dependencias de red externa, llamadas HTTP salientes o servicios cloud.

3. **Verificación final del código y suite de pruebas:**
   - Compilación completa de la solución en **Debug** y **Release** con MSBuild de Visual Studio.
   - Ejecución de `scripts/verificar.ps1`: **337/337 tests unitarios y de arquitectura superados con exit 0**, sin advertencias ni errores.

**Alcance real de esta evidencia (corrección de root, 8/11/2026):** ambos tests existen y pasan, pero son *sustitutos*, no las aceptaciones que esta misma fase exige. Instanciar formularios en STA prueba construcción en runtime, no la serialización del Diseñador de Visual Studio; el test de arquitectura prueba ausencia de dependencias HTTP del puente demo, no un recorrido con Internet efectivamente desconectado. Canaletas con foto (alta, PNG, rechazo `.txt`, edición, borrado) se verificó por GUI sobre la build anterior a `d3793eb`, no sobre la corregida. Por eso **F9 no se declara cerrada**: Designer interactivo y recorrido offline siguen pendientes del usuario, y la build corregida queda por recorrer por GUI.

### Corrección posterior: historial de herramientas e insumos (commit `9a6939a`)

- Bug: los movimientos registrados por la app dejaban vacíos `nombre_herramienta`/`nombre_insumo`; el historial mostraba siempre el nombre actual. Ahora los INSERT guardan el nombre vigente en la misma transacción (después del prefijo de ID) y las consultas usan el snapshot con fallback al nombre actual para filas antiguas.
- No corregido (requiere esquema): el historial de herramientas muestra el **estado actual**, porque `herramienta_movimientos` no tiene columna de estado.
- Diseño documentado (F7), no bug: alta con historial inicial «Galpón → Galpón» y prefijo de ID en el nombre («2DEMO-…»). `id_ubicacion` de insumos es columna legacy que solo el seed rellena.
- Filtro del diálogo de fotos de canaletas: de `*.*` a jpg/jpeg/png/webp (las extensiones que valida el contenido).
- Evidencia root: `verificar.ps1 -LegacyLocal` **340/340 Debug y Release**, exit 0; `demo-integracion.ps1` **100/100** sobre PostgreSQL real con aserciones nuevas del snapshot de nombre en herramientas e insumos (`steelog-sql-snapshot-historial-2.log`).

## Rondas de testeo GUI y correcciones 8–9/11/2026 (commits `c6adbec`, `a49f5d0`, `1175e2b` y siguiente)

Método: instalaciones aisladas reconstruidas desde cada HEAD (`109c284`, `a49f5d0`, `1175e2b`), monitor secundario, UIA/MSAA/mensajes dirigidos sin cursor ni foco, verificación SQL READ ONLY de cada escritura. Root revisó cada diff, investigó causas raíz y corrió verificación local y suite SQL real antes de cada commit.

**Recorrido GUI cubierto (Admin):** movimientos alta/edición (cantidad y material)/eliminación con reversión exacta de stock, con flete y con tipo Compra; órdenes alta, edición, cambio de material, recepción en dos tandas hasta «Recibida Completa», tope de exceso, eliminación de pendiente y rechazo de recibida; CRUD de Materiales, Categorías, Proveedores, Obras, Ubicaciones, Fletes, Herramientas, Insumos, Recortes y Canaletas con foto; Informe de obra PDF (total pendiente $61.500 contrastado con SQL), remito PDF, Excel de stock, buscador global, regresión de todas las correcciones. Base demo limpia al finalizar cada ronda.

**Defectos reales encontrados y corregidos:**
- Riesgo de duplicar movimientos: tras resultado incierto, F5 rehabilitaba Guardar sin prueba; ahora exige confirmación, pausa autoguardado y persiste el marcador en el borrador (sobrevive a reinicio; borradores legacy compatibles).
- Órdenes: una lectura iniciada antes de la marca de incierto podía liberar el bloqueo (versión de carga).
- La app se minimizaba con Enter: los botones de barra de ventana eran tabulables/focusables y actuaban como botón por defecto.
- Toasts: modo centrado ignoraba el origen del monitor; apilado sin acotar; formularios embebidos usados como referencia.
- Catálogos: doble Enter sin guarda de reentrada; largo validado tras confirmar y sin `MaxLength`; SQL crudo en duplicados (23505) y en eliminaciones de entidades en uso (23503); ubicaciones usadas en movimientos se podían borrar sin aviso (FK polimórfica); «de el» → «del».
- Vistas en caché no refrescaban combos de catálogos (Movimientos, Órdenes Alta, Materiales); selección de grilla resaltada sin selección lógica tras recargar.
- Recepción: una celda «Recibida real» excedida conservaba el valor inválido; resumen de cabecera desactualizado.
- UX: Recortes con Eliminar activo sin selección y decimales mixtos; motivo del rechazo de fotos; contraste de botones deshabilitados (Informe de obra) y de «sin resultados» del buscador; color de selección en Historial; botón Eliminar vacío en OC no pendientes; botón Aceptar cortado en «Cambiar material»; Limpiar no desmarcaba «Eventual»; combo de categoría vacío tras guardar material.
- Remito: copias rotuladas ORIGINAL/DUPLICADO/TRIPLICADO.
- Seed demo alineado con las convenciones del código: `precio_unitario` solo en compras eventuales y `precio_total` de cabecera (antes $0); recorte de dos dimensiones con `medida = 0` según el invariante de dominio.
- Pruebas: legacy del buscador dependía del reloj (caché de 10 s); fallas de foco de LoginUsability bajo carga concurrente identificadas como ambientales (pasan aisladas, sin cambiar timeouts).

**Evidencia final root:** `verificar.ps1 -LegacyLocal` **415/415 Debug y Release**, exit 0, cuatro familias legacy (`steelog-verificar-ronda-final.log`); `demo-integracion.ps1` **101/101** sobre PostgreSQL real, incluidas aserciones nuevas de snapshot de nombre, duplicado y entidad en uso (`steelog-sql-ronda-final.log`).

**Diseño confirmado, no defecto:** recepción de OC no genera movimiento Kardex (documentado); alta de herramienta/insumo con historial inicial «Galpón → Galpón» y prefijo de ID en herramientas (F7); Galpón puede eliminar movimientos y ver el reporte de stock enmascarado (Application). **Historial de herramientas:** el esquema no guarda el estado de cada movimiento; la columna se rotula ahora «Estado actual» con explicación, en vez de presentarse como histórica. Guardar el estado histórico requiere una columna nueva (decisión de esquema del usuario).

**No probado por automatización:** PDF de pendientes de órdenes (abre el visor externo sin diálogo), «Buscar actualizaciones», resultado incierto real (requiere cortar la base). **F9 sigue ABIERTA:** Designer interactivo y recorrido sin Internet requieren al usuario.

### Paquete y smoke final sobre `c71a095`

Paquete `A:\SteeLog-Pruebas\paquete-c71a095-c1585b13351348efb7d42b669f90c014(.zip)`: commit `c71a09582af53bf8146ed8855e925ef345ec8f57`, `arbolModificado=false`, ZIP SHA-256 `91EDD98D1A396A7D5BBC6BE16EB4E9120518BACFF3F9B0F93DB35D65D6F50809`; `paquete-demo.ps1` 6/6 PASS. Smoke GUI sobre su instalación aislada, 5/5 OK sin fallas: catálogo nuevo visible en vistas en caché sin reiniciar (ubicación en Movimientos, categoría en Materiales); ubicación y obra en uso rechazadas con aviso de negocio y sin borrar; celda de recepción excedida revertida sin «+89» ni cambios en base; Limpiar desmarca Eventual; eliminación de movimiento revierte stock. Base idéntica a la línea base al finalizar (ubicaciones 4, categorías 7, movimientos 2, obras 3, stock 485, DEMO-OC-001 Pendiente), confirmado por root.

El PDF de pendientes de órdenes está cubierto por `RemitoPdfTests` (generación, validación, liberación de archivo) y el smoke de marca con PDF real; solo la apertura del visor externo de Windows no se automatiza. Pendiente exclusivamente del usuario para cerrar F9: Designer interactivo y recorrido con Internet desconectado.
