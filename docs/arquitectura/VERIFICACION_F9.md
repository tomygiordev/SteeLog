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

## Qué falta para cerrar F9 y declarar listo el software

- Completar recorrido de todos los módulos con Admin y Galpón desde la instalación nueva; registrar operaciones reales, errores/rollback, recepción, conteo, foto, PDFs y persistencia al reiniciar la app.
- Renderizar e inspeccionar los PDFs del recorrido real; las pruebas y evidencias PDF F3–F8 no sustituyen esa aceptación de instalación limpia.
- Comprobar Designer de formularios relevantes en un escritorio capturable/interactivo. Cargar solución o construir formularios en STA no equivale a Designer aprobado.
- Recorrido completo con Internet efectivamente desconectado, sin apagar redes del usuario por script ni inferirlo de tener adaptadores locales. No se modificaron firewall ni conectividad del equipo.
- Reconstruir/verificar paquete de entrega cuando se congele el código final y registrar su procedencia final.
- Revisar requisitos F0/F9, diff preparado, UTF-8, cero allowlist y evidencia antes del commit de cierre de fase. No se autoriza autoaplicar F4 ni cambiar bases existentes.

Engram sigue sin identidad de runtime confirmada por el hook: root no realiza escrituras atribuidas ni inventa sesión. Git y esta evidencia preservan el avance. El objetivo «continuar hasta dejar el software listo» continúa activo; esta preparación no redefine el objetivo como un mero build exitoso.
