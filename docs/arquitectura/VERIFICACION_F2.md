# Verificación F2 — host local y utilidades demo

## Alcance y decisiones

- **D — Identidad local.** El catálogo conserva los alias demo aceptados por el código anterior y sus contraseñas demo; un usuario desconocido o contraseña incorrecta no intenta HTTP. El núcleo representa identidad y rol sin JWT ni refresh token. La sesión empieza en `None`, centraliza la comprobación de rol y se limpia al cerrar sesión. Los puentes `SupabaseAuthService` y `SessionManager` conservan nombres para los formularios legacy, pero ya no consultan Supabase. Son compatibilidad temporal, no seguridad de producción ni protección contra manipulación local.
- **R — Credenciales y fotos.** El adaptador DPAPI conserva `%LOCALAPPDATA%/SteeLog/session.dat` y su formato. El adaptador de fotos conserva `%LOCALAPPDATA%/SteeLog/Storage` y los object paths existentes; el puente legacy ya no consulta tokens ni realiza red. Los borradores no se migraron ni modificaron.
- **R — Composición.** `LocalServices` se crea una vez por proceso, con configuración y fábrica diferidas y compartidas. `ConexionDB` mantiene su firma pública, obtiene conexión nueva por operación y conserva la comprobación de errores. La construcción de esta composición no abre DB; no se auditó la ausencia de aperturas dentro de todos los formularios legacy. La raíz se dispone en `finally` del entry point. El ejecutor crea y dispone un scope DI por caso de uso; no expone `IServiceProvider` a los formularios ni al núcleo.
- **D — Actualizaciones.** El fixture `demo/actualizaciones` contiene manifiesto y paquete local que no es un EXE. Dos casos de uso consultan versión y preparan una copia mediante el adaptador local. Se comprueban hash y cancelación, y se limpia el archivo parcial. El modal dice DEMO y no instala, reemplaza ni reinicia nada. Ya no se consulta GitHub ni se hace comprobación automática al mostrar la ventana principal.
- **R — Navegación.** Las entradas de `VistaGeneral` solicitan el formulario a través de una factoría después de consultar la caché. Se conservan operaciones de edición y revisión por ID. Se cambió una llamada mecánica en `Vista_OrdenesRevision.cs` para pasar una factoría; no se tocaron recepción, estados ni SQL.

## Configuración y límite de secretos

Se conserva temporalmente la prioridad legacy: recurso embebido y archivo externo opcional como override, cargados una vez por composición. **El recurso embebido aún puede provenir de una configuración privada existente**; no se lo inspeccionó ni copió para esta evidencia. No se afirma que el EXE carezca de secretos. Antes del empaquetado final debe retirarse ese fallback o sustituirse por defaults no sensibles, con migración explícita de los tests de fallback. `App.config` y configuración privada no se modificaron.

## Pruebas

El runner `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal` restaura dependencias de preparación, compila y prueba Debug y Release y luego ejecuta Configuration, search, Branding y LoginUsability. La corrida anterior terminó PASS con 38 casos por configuración y las cuatro suites legacy (`%TEMP%/steelog-f2-final.log`). La verificación de sesión recordada opcional pasó con **40 tests por configuración** y las cuatro suites legacy (exit 0); log `%TEMP%/steelog-f2-attempt2-final.log`. Las pruebas incluyen autenticación válida/inválida y alias, roles y logout, DPAPI en carpeta temporal, lectura de una foto preexistente en copia temporal, navegación con constructor contado, scopes distintos y liberación ante éxito/error/cancelación, y composición/configuración compartida.

Actualización: versión nueva/igual, manifiesto incorrecto o tipos inválidos, ruta inválida, archivo faltante, hash incorrecto, cancelación antes y durante copia y limpieza del parcial. Se verifica también el hash del fixture copiado al output y que los puentes/adaptadores demo no refieran HTTP ni lanzamiento de procesos. Costura: descompresión y carga de los tres assemblies propios y las dos bibliotecas DI, sin `Main`.

No se ejecutaron `Program.Main`, DB real o de prueba, Docker, red de producto, updater anterior, reemplazo de ejecutable ni publicación. Un análisis IL y adaptadores locales reducen el riesgo de red, pero no sustituyen un ensayo manual de toda la aplicación con conectividad deshabilitada. No se comprobaron Designer interactivo ni recorridos manuales de ambos roles. Microsoft.Extensions.DependencyInjection 9.0.12 es compatible con net462/net48: [NuGet oficial](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/9.0.12); los scopes usan el patrón [documentado por Microsoft](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines).

## Pendiente para fases siguientes

Los módulos de negocio, sus consultas y permisos por caso de uso continúan legacy hasta F3 en adelante. Los puentes de nombre Supabase y el contrato público Npgsql de `ConexionDB` se retiran cuando sus consumidores se migren. Las pruebas automáticas no certifican permisos reales sobre la base ni integridad transaccional de esas operaciones.

## Auditoría y regresión del orquestador

El orquestador revisó el diff, las fronteras de composición, el fixture y sus pruebas, y volvió a ejecutar `scripts/verificar.ps1 -LegacyLocal` sobre las fuentes finales: **exit 0, 40 pruebas en Debug y 40 en Release**, más las cuatro suites legacy. Log: `%TEMP%/steelog-root-f2-96b1f1d5bf4e406da9318d173d1248f3.log`. Los archivos nuevos/modificados se comprobaron como UTF-8 sin caracteres de reemplazo; el inventario legacy disminuyó tres pares sin agregar excepciones. Los cambios previos en README y el iniciador Docker conservan sus hashes.

Se repitieron Bootstrap y consistency sobre el código actual en un clúster efímero propio: **Bootstrap PASS y consistency PASS, 18 checks**, con PostgreSQL 17 y zona `America/Argentina/Buenos_Aires`. El transporte API fue contrastado con SQL válido/erróneo y sus códigos 0/1; las aserciones y SQL originales no se alteraron. Logs, script y launchers: `%TEMP%/steelog-f2-sql-3f8133d3b59b44058b0fef089ab3eef7/`. El orquestador contrastó esos archivos; no modificaron bases/volúmenes existentes y se verificó la eliminación del contenedor propio. El resultado no cubre una zona PostgreSQL distinta de la del host.

La política de credenciales recordadas se hizo inyectable y se probó con fallos y DPAPI corrupto en TEMP. El puente ya toleraba esos errores: una sospecha inicial de crash fue descartada al seguir la llamada completa, por lo que no se registra como defecto corregido. La aceptación de F2 no equivale a un ensayo manual integral offline ni al cierre de la migración del negocio.

Se encontró y reprodujo un fallo real de portabilidad del fixture: con `core.autocrlf=true`, Git convertía el paquete de texto a CRLF y su SHA-256 dejaba de coincidir con el manifiesto. Se fijó `eol=lf` únicamente para ese payload mediante `.gitattributes`; el checkout de prueba con autocrlf activado conserva ahora el hash esperado. No se normalizaron las fuentes legacy.

## Intento 2 — sesión recordada opcional

El puente `Servicios.CredentialStore` ya capturaba los errores del adaptador en la primera entrega. Para hacer explícita y comprobable la garantía, `OptionalCredentialStore` envuelve DPAPI: falla de lectura se interpreta como ausencia, y errores al guardar o limpiar no bloquean la sesión en memoria. Las pruebas usan un archivo corrupto en TEMP y un doble que falla en las tres operaciones; no acceden a la sesión real. `Program.Main` no se ejecutó.
