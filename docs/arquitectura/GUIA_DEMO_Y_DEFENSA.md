# SteeLog: instalación demo y defensa

## Qué se prepara

Monolito modular WinForms net48 en Windows y PostgreSQL 17 local en Docker. No hay API web ni servicios cloud. Los movimientos, saldos, recepciones, conteos y archivos son persistentes. El login de cuentas fijas y la preparación de una actualización local son **demo**: no representan autenticación productiva ni reemplazan el EXE.

F9 está en preparación: esta guía no certifica un recorrido offline ni una validación interactiva del Designer. La evidencia ejecutada se registra en `VERIFICACION_F9.md` del repositorio.

## Preparar el equipo

- Windows con .NET Framework 4.8; para compilar: Visual Studio/MSBuild, targeting pack net48 y SDK dotnet.
- Docker Linux local en `tcp://127.0.0.1:2375`, con la imagen `postgres:17-alpine` ya disponible. Ese endpoint sin TLS es una limitación del laboratorio local, no una recomendación productiva; no exponerlo en otras interfaces.
- Preparar paquetes NuGet y herramientas antes de desconectar Internet. Un primer restore puede necesitar Internet. LazyDocker es opcional para observar; no operar sobre proyectos ajenos.

Desde el repositorio, crear un paquete nuevo (no publicación):

```powershell
$paquete = Join-Path $env:TEMP ('SteeLog-paquete-' + [guid]::NewGuid().ToString('N'))
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/empaquetar-demo.ps1 -Destino $paquete
```

El build usa una copia TEMP de fuentes versionadas, sin leer/copiar `appsettings.json` privado, sesión ni almacenamiento personal. El JSON embebido y externo del paquete apuntan a una conexión deliberadamente sin configurar, no al puerto 5435. `manifest.json` registra commit, árbol modificado y hashes SHA-256; los hashes detectan alteraciones accidentales, **no son una firma de autenticidad**. El ZIP incluye EXE/redirects, actualización demo, SQL y scripts locales. El snapshot saneado se conserva para diagnóstico.

## Crear una instalación separada

Descomprimir el ZIP en una carpeta nueva si se traslada a otro equipo. No apuntar al repositorio ni a una instalación existente:

```powershell
$instalacion = Join-Path $env:TEMP ('SteeLog-exposicion-' + [guid]::NewGuid().ToString('N'))
powershell -NoProfile -ExecutionPolicy Bypass -File "$paquete/tools/preparar-demo.ps1" -Paquete $paquete -Destino $instalacion
```

Se crean contenedor/volumen/DB con GUID y etiqueta propios, con puerto dinámico en loopback, nunca 5435. Se comprueba la identidad y el transporte SQL real. No se reutiliza ningún volumen. Si falla, `instalacion.json` deja los nombres e identidad de recursos **incompletos** conservados para diagnóstico; no se promete rollback conjunto ni se reintenta sobre ese volumen. No hay limpieza general ni `down -v`.

El esquema nuevo deriva de las **23 tablas utilizadas por los adaptadores SQL** y su cierre de FK, secuencias, índices y dos funciones de triggers. `database/tablas.json` conserva procedencia. Incluye los snapshots F4; no requiere aplicar esa migración al usuario. No contiene roles globales ni tablas de administración financiera/asistentes. Los dumps y scripts antiguos no se eliminan. Adoptarlo en una instalación existente requiere backup y copia/migración selectiva explícitos: este instalador no sirve para eso.

## Recorrido pendiente de ejecución autorizada

No ejecutar directamente `app/SteeLog.exe`: usar el lanzador para aislar sesión, fotos, borradores y actualización demo en `datos/`, sin acceder al perfil personal. El marcador `.steelog-demo` evita un fallback accidental. No es una barrera de seguridad contra alguien que altera los archivos.

```powershell
# Solo cuando se autorice ejecutar Main en esta instalación nueva:
powershell -NoProfile -ExecutionPolicy Bypass -File "$instalacion/ejecutar-demo.ps1"
```

El lanzador valida el contenedor y descubre nuevamente su puerto: Docker puede cambiarlo tras un reinicio. Si está detenido, informa el problema sin iniciar recursos ajenos. No cambia variables globales de Windows.

Credenciales ficticias: **admin / admin** y **galpon / galpon**. La contraseña PostgreSQL incluida es pública y solo de demostración.

1. Con Admin: inventario inicial, envío `DEMO-ENVIO-001` de 10 montantes y saldo 140; OC pendiente de 10 soleras; conteo abierto con 139 montantes; canaleta pendiente de $25.000; taladro en obra y disco en depósito.
2. Recibir parcialmente la OC y consultar saldo/estado. La recepción **no agrega automáticamente un movimiento Kardex**; explicar ese límite.
3. Registrar y editar un movimiento; comprobar saldo, insumos y remito. Provocar un error válido sin dejar cambios parciales.
4. Cargar/corregir/finalizar conteo: fija comparación, no ajusta stock. Verificar permisos del creador/Admin.
5. Adjuntar una foto de prueba propia a la canaleta y generar informe de obra, stock y Kardex; abrir PDFs y revisar todas las páginas. El seed no referencia fotos inexistentes.
6. Buscar materiales, gestionar herramientas/insumos y catálogos. Reiniciar la aplicación y comprobar datos, sesión demo, borrador y foto.
7. Con Galpón: revisar navegación y restricciones visuales; la ocultación de columnas no se presenta como confidencialidad de DTOs.
8. Consultar/preparar actualización local: mostrar el archivo demo; no ejecutar `publicar.ps1` ni un updater real.
9. Repetir con Internet desconectado **sin deshabilitar redes del equipo del usuario por script**. Registrar método de aislamiento, pantallas, roles y resultados reales. Reabrir formularios en el Designer de Visual Studio; no confundir tests STA con Designer.

## Arquitectura para explicar

```text
Vistas -> Presenters -> Application (puertos, autorización, UoW) -> Domain
                           ^
CompositionRoot -> Infrastructure (Npgsql, archivos, PDF, demo local)
```

Solo CompositionRoot conoce Infrastructure desde el host. `SteeLog.Application` es el proyecto, pero el namespace es `SteeLog.Core.Application`, para no colisionar con WinForms.

| Módulo | Regla y ejemplo verificable |
|---|---|
| Inventario/Movimientos/Activos | `Application/Movimientos/OperacionesMovimiento.cs`: una operación coordina stocks, líneas e insumos en una UoW; `NpgsqlUnitOfWork` aporta una conexión/transacción. Revertir utiliza el impacto persistido, no una aproximación. |
| Órdenes | `Application/OrdenesCompra/OperacionesOrdenes.cs`: recepción monotónica con cantidades enteras y transferencia de material recibida atómica. |
| Conteos | `Application/Conteos/OperacionesConteo.cs`: todos los escritores bloquean primero la misma cabecera y verifican propietario/estado dentro de la transacción. |
| Canaletas | PostgreSQL y archivos no comparten ACID: staging, clave estable, compensación conocida y conciliación ante commit incierto; al eliminar, DB primero y limpieza después. |
| Reportes | DTOs y cálculo puro; renderer sin SQL/sesión. Kardex clasifica impacto, no signo crudo. Mantiene cantidad original y saldo persistido. |
| Catálogos | Casos tipados por catálogo, sin repositorio genérico de tablas suministradas por UI. |

El monolito comparte una DB porque una operación real atraviesa módulos. No se agregaron microservicios, EF, MediatR ni buses para justificar capas. Las reglas de arquitectura inspeccionan IL, incluidos tipos async, y exigen cero excepciones legacy.

## Comandos de evidencia

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tests/demo-integracion.ps1
```

El segundo crea un volumen/clúster propio, restaura el esquema demo, verifica seed/cierre/rechazo de reinstalación y persistencia tras reiniciar; ejecuta las suites SQL separadas y elimina solamente sus recursos identificados. No usa `docker exec` CLI para deducir resultados: el transporte API comprueba frames y ExitCode real. Las pruebas no certifican aceptación manual, impresión ni Designer.

Conservar log/TRX, manifest y hash del ZIP usado en la exposición. Detener y limpiar exclusivamente el contenedor/volumen registrados cuando ya no se necesiten, verificando antes nombre/ID/etiqueta; nunca emplear una limpieza global. La instalación permanente preparada no se borra automáticamente.
