# Verificación F1 — base estructural

## Alcance

Se agregaron cuatro proyectos hermanos sin trasladar módulos. `SteeLog.Application` conserva el nombre de ensamblado y proyecto del plan, pero sus futuros namespaces empiezan con **`SteeLog.Core.Application`**: `SteeLog.Application` ocultaría a `System.Windows.Forms.Application` en código legacy. Las reglas inspeccionan identidad de ensamblados, no ese namespace. Domain y Application no tienen todavía reglas de negocio; sus marcadores solo permiten verificar el grafo.

`LocalServices` prepara configuración, rutas y fábrica por instancia de composición. Está deliberadamente desconectado de `Program.Main`; hoy el runtime sigue usando `ConexionDB` legacy. F2 deberá crear una sola instancia de composición en el arranque, inyectarla a consumidores migrados y probar su ciclo de vida, override, fallback y ausencia de conexiones retenidas. No se afirma configuración única efectiva para el host actual. La fábrica nueva crea conexiones cerradas independientes; no conserva una conexión.

## Ejecución

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1` restaura, compila y prueba Debug y Release. `-LegacyLocal` agrega Configuration, search, Branding y LoginUsability; no ejecuta SQL. El build de solución no ejecuta pruebas automáticamente. El runner usa `vswhere` o MSBuild del PATH y deja outputs normales por proyecto/configuración, evitando un `OutputPath` global compartido.

Los controles de arquitectura comparan el inventario exacto owner→tipo con fase estimada de retiro: ninguna entrada nueva o eliminada pasa sin actualizarlo; no habilita excepciones por namespace. Se recorre IL, tipos anidados y state machines, constraints genéricos y atributos. Un control negativo verifica acceso async y otro constraints/atributos. El núcleo usa una whitelist de referencias BCL. El smoke extrae los tres ensamblados comprimidos de Costura, los carga y comprueba identidad y tipos, sin ejecutar `Main`; no es publicación ni prueba de aplicación distribuida offline.

## Evidencia y límites

En un export limpio de HEAD con los cambios F1 aplicados, sin appsettings privado, se ejecutó el runner con 12 pruebas por configuración y PASS; el build generó la configuración desde el ejemplo. Luego se añadió la prueba de referencias de proyecto, ejecutada en el workspace con 13 pruebas por configuración. Logs: `%TEMP%/steelog-f1-clean.log` y `%TEMP%/steelog-f1-final2.log`.

En Windows, MSBuild Visual Studio 2022 y .NET SDK 9.0.304: runner Debug y Release, **13 pruebas por configuración**, PASS. La ejecución usa la caché NuGet local o acceso de preparación a NuGet; no requiere DB. Las versiones de xUnit 2.9.3 y runner VS 2.8.2 se fijaron por compatibilidad con .NET Framework 4.8; Mono.Cecil 0.11.5 se usa para inspección sin cargar el host. Referencias oficiales: [xUnit v2](https://xunit.net/docs/getting-started/v2/getting-started), [versiones xUnit](https://xunit.net/releases/), [Mono.Cecil](https://github.com/jbevain/cecil/releases).

No se ejecutaron Designer interactivo, recorridos funcionales, SQL, `Program.Main`, updater ni publicación. La inspección de recursos y build no sustituyen esos controles. Tampoco se hizo migración de autenticación, módulos, PDF o UoW. El archivo de configuración de ejemplo sigue siendo el utilizado para un checkout sin configuración privada; no se copian secretos a la evidencia.

## Corrección del intento 2: suites legacy con outputs aislados

La auditoría detectó `NETSDK1004` al ejecutar Configuration: los argumentos globales de outputs temporales del host llegaban a los nuevos proyectos SDK, que buscaban todos el mismo `project.assets.json` temporal. Cada `ProjectReference` del host elimina esas propiedades globales para que los proyectos SDK usen sus propios directorios. Los cinco launchers que compilan el host (Configuration, search, Branding, LoginUsability y consistency) restauran primero la solución y verifican el código de salida, también al ejecutarse por separado desde un checkout limpio; no cambian aserciones ni SQL. Bootstrap no compila el host.

El comando de build aislado de consistency fue reproducido tras restauración en export limpio y pasó sin ejecutar SQL ni Docker (`%TEMP%/steelog-f1-r2-sql-build.log`). La ejecución completa local de `-LegacyLocal` se registra en `%TEMP%/steelog-f1-attempt2-final.log`; este resultado no cubre bases de datos ni diseñador.

## Contraste del orquestador

El primer intento fue rechazado al reproducir la regresión con `-LegacyLocal` (`%TEMP%/steelog-root-f1-legacy-b43b377a79ef4dd4a64c3d0fc5c3fb19.log`). Tras revisar el diff, el orquestador volvió a ejecutar el mismo comando sobre las fuentes corregidas: **exit 0**, 13 pruebas xUnit en Debug y 13 en Release, más las cuatro suites legacy locales. Evidencia: `%TEMP%/steelog-root-f1-accepted-82008ad9cc034e53ac63d210d4093e94.log`. Se comprobó que las modificaciones a launchers son solo restauración/build, sin cambiar sus aserciones, y que `README.md` y `docker/iniciar.ps1` conservan los hashes de partida. La aceptación es de la base estructural y sus pruebas automatizadas, no de una migración de módulos todavía inexistente.
