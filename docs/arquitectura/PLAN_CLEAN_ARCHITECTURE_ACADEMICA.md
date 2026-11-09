# SteeLog: plan de Clean Architecture para Taller de Integración

**Estado:** propuesta de implementación; este documento no implica que la migración esté realizada.
**Fecha:** 2 de noviembre de 2026.
**Objetivo:** transformar la aplicación existente en un sistema local con arquitectura limpia, modularidad verificable y evidencia suficiente para explicar y defender sus decisiones académicas.

## 1. Alcance y criterio de éxito

SteeLog es un proyecto académico, no un producto que se desplegará en producción. La prioridad es la calidad del código, la separación de responsabilidades, la integridad de las operaciones y la posibilidad de demostrar todo eso ante los profesores.

- Se conserva una aplicación de escritorio Windows y un único punto de entrada, `SteeLog.exe`.
- Se mantienen los módulos, las pantallas, la marca, la navegación y las funciones existentes, salvo los cambios explícitos enumerados en este plan.
- PostgreSQL sigue en Docker. WinForms sigue ejecutándose en Windows, no dentro del contenedor Linux.
- Toda la ejecución de la exposición es local: autenticación, fotos, borradores, reportes y demostración de actualizaciones.
- Los datos y las operaciones centrales de inventario son reales y persistentes. No se simulan movimientos, recepciones, saldos ni resultados de pruebas.
- Se admite un catálogo fijo de usuarios demo y una fuente local de actualizaciones. Esos adaptadores se identifican como demostrativos, no como servicios productivos.
- No se promete una nota determinada: se entrega evidencia de diseño y funcionamiento, no solamente carpetas con nombres de patrones.

**Fuera de alcance:** despliegue cloud, Supabase como requisito de ejecución, API web, microservicios, Kubernetes, multitenancy, JWT/refresh tokens, instalador remoto, firma y distribución productiva de actualizaciones, endurecimiento empresarial, cambio de framework y rediseño visual general.

La preparación inicial del equipo puede requerir descargar paquetes y la imagen Docker. La presentación debe funcionar sin Internet, con esos recursos preparados; no se promete un primer restore offline sin caché.

## 2. Base del plan y contraste con el repositorio

Este plan conserva el trabajo de modelado anterior y corrige su alcance. No parte de una plantilla ni propone reescribir el sistema desde cero.

### Antecedentes

- Plan original de agy: `C:/Users/Gime/.gemini/antigravity-cli/brain/5a7af603-bdeb-49fe-b969-a1b86c99b4ca/plan_clean_architecture.md`.
- Diagnóstico previo: `analisis_clean_architecture.md`, en la misma carpeta.
- Recuperación y contraste registrados en Engram, observación **#1517**.
- Aclaración del usuario: Taller de Integración, funcionamiento completamente local y excelencia arquitectónica como prioridad.

### Hechos revisados en el código actual

| Evidencia | Consecuencia para la planificación |
|---|---|
| `SteeLog/SteeLog.sln` contiene el proyecto WinForms .NET Framework 4.8 existente. | Preservar el proyecto de entrada y su diseñador; extraer bibliotecas gradualmente. |
| `Datos/ConexionDB.cs` construye configuración por instancia y entrega conexiones nuevas. | Configuración única y fábrica de conexiones; no introducir una conexión singleton. |
| `Vista_Movimientos.cs:1844-1864` guarda el movimiento y luego mueve insumos por separado. | Materiales, recortes, insumos e historial deben participar de una misma transacción del caso de uso. |
| `AutenticacionLocal.cs` contempla usuarios demo locales y tokens ficticios. | Conservar el adaptador local explícito; no reconstruir autenticación cloud. |
| La clase de storage antigua fue eliminada; `LocalCanaletaStorage` guarda archivos en `%LOCALAPPDATA%/SteeLog/Storage`. | Reutilizar el adaptador local y conservar la compatibilidad de rutas de fotos existentes. |
| `UpdaterService.cs` consulta GitHub y puede reemplazar el ejecutable. | Sustituir el flujo registrado por una demostración local que no reemplace el programa. |
| `CredentialStore.cs` ya cifra las credenciales con DPAPI. | No describir el estado actual como contraseñas guardadas en texto plano. |
| `App.config` contiene runtime y binding redirects; el `.csproj` lo referencia. | Mantenerlo versionado. No desindexarlo como si fuera un secreto. |
| Existen pruebas de integridad, configuración, búsqueda, marca, login y bootstrap. | Migrar sus aserciones útiles; no reemplazarlas por pruebas superficiales nuevas. |
| El esquema versionado revisado declara 69 tablas, incluidas tablas ajenas al galpón. | Delimitar propiedad y dependencias antes de preparar un esquema académico reducido. |
| El modelo C# de líneas de órdenes usa `decimal`, pero las cantidades de la tabla son `integer`. | Hacer explícito el contrato de cantidades, sin truncar datos silenciosamente. |

Estas verificaciones fueron estáticas. La preparación de este plan no ejecutó la aplicación, pruebas de integración ni consultas a bases reales. Los números de línea son referencias de la revisión, no identificadores permanentes.

### Qué se conserva y qué se reemplaza del plan anterior

| Se conserva | Se modifica |
|---|---|
| Domain, Application, Infrastructure, WinForms y Tests. | El funcionamiento local deja de ser un fallback: es el único perfil requerido. |
| MVP, casos de uso explícitos y consultas mediante DTOs. | Las carpetas se organizan también por módulo, no solo por tipo técnico. |
| Repositorios tipados y unidad de trabajo. | Se especifica el alcance de transacción por operación, separado del de una ventana. |
| Reglas de órdenes, stock, conteos, recortes y valorización. | Se contrastan con pruebas de caracterización antes de convertirlas en contratos definitivos. |
| Piloto vertical de órdenes de compra y migración incremental. | No se mezcla la migración con cambios silenciosos en Kardex o permisos. |
| Precauciones con Guna, marca, versión y Costura. | No se exige publicar ni construir un actualizador productivo para cerrar el proyecto. |

## 3. Arquitectura objetivo

### 3.1. Cinco proyectos, una aplicación

| Proyecto | Framework propuesto | Responsabilidad | Referencias propias permitidas |
|---|---|---|---|
| `SteeLog.Domain` | `netstandard2.0` | Entidades, invariantes, value objects y cálculos de negocio puros. | Ninguna. |
| `SteeLog.Application` | `netstandard2.0` | Casos de uso, autorización, DTOs y puertos de persistencia/servicios. | Domain. |
| `SteeLog.Infrastructure` | `net48` | PostgreSQL/Npgsql, archivos locales, usuarios demo, PDF y fixtures de actualización. | Application y Domain. |
| `SteeLog` existente | .NET Framework 4.8 | Formularios, presenters, navegación, recursos y composición de dependencias. | Application y Domain; Infrastructure solo desde `CompositionRoot/`. |
| `SteeLog.Tests` | `net48` | Pruebas de dominio, aplicación, integración, arquitectura y humo de UI. | Los proyectos necesarios según la suite. |

Se mantienen las ubicaciones existentes de solución y ejecutable. Las bibliotecas nuevas se agregan como carpetas hermanas de `SteeLog/SteeLog/`; no se mueve todo a `src/` durante esta migración.

```text
SteeLog/
  SteeLog.sln
  SteeLog/                 # WinForms existente
  SteeLog.Domain/
  SteeLog.Application/
  SteeLog.Infrastructure/
  SteeLog.Tests/
docker/
docs/arquitectura/
```

Dependencias de compilación:

```text
WinForms ----------------> Application -------------> Domain
CompositionRoot ---------> Infrastructure ----------> Application / Domain
```

En ejecución, Application invoca interfaces que Infrastructure implementa. Esa inversión de dependencias es el punto central; no basta con mover SQL a otra carpeta.

`Domain` y `Application` no conocen WinForms, Guna, Npgsql, iTextSharp, el contenedor DI, rutas físicas ni Docker. No se introduce EF, MediatR, AutoMapper ni un bus de eventos para justificar la arquitectura.

Los targets se verifican con un prototipo de compilación, diseñador y empaquetado en la fase 1. Mantener `netstandard2.0` en el núcleo sigue el plan anterior y permite verificar su independencia del host Windows; no obliga a migrar la UI.

### 3.2. Organización interna por módulos

```text
SteeLog.Domain/
  Common/                 # Solo primitivas realmente compartidas
  Inventario/             # Material, Recorte, cantidades y ajustes
  Movimientos/            # Movimiento, líneas, impacto de stock, remito
  OrdenesCompra/          # Orden, líneas y transiciones de estado
  Conteos/                # Conteo, detalle, diferencias
  Canaletas/              # Plegado y metadatos de foto
  Activos/                # Herramientas e insumos
  Catalogos/              # Obra, proveedor, categoría, ubicación, flete

SteeLog.Application/
  Common/                 # Transacciones, reloj y errores de aplicación
  Identidad/              # Sesión local, roles y permisos
  Inventario/             # UseCases, Queries, Dtos y Ports
  Movimientos/
  OrdenesCompra/
  Conteos/
  Canaletas/
  Activos/
  Catalogos/
  Reportes/               # Composición de lecturas e interfaces PDF
  Actualizaciones/        # Comparación de versiones y preparación demo

SteeLog.Infrastructure/
  Persistence/            # Configuración de conexiones y UoW
  Inventario/             # Repositorios/queries Npgsql por funcionalidad
  Movimientos/
  OrdenesCompra/
  Conteos/
  Canaletas/
  Activos/
  Catalogos/
  Files/                  # Fotos, borradores y pendientes de limpieza
  Identity/               # DemoAuthService y sesión recordada local
  Pdf/                    # Renderers que reciben DTOs
  DemoUpdates/            # Manifiesto y paquete locales

SteeLog/                  # Proyecto de presentación existente
  CompositionRoot/        # Único lugar que conoce implementaciones
  Presentation/           # Contratos de ejecución/navegación de UI
  Presenters/             # Subcarpetas por módulo
  Vistas/                 # Formularios actuales, migrados gradualmente
```

No se crean carpetas vacías para aparentar avance. Cada una aparece con la primera extracción real de su responsabilidad.

### 3.3. Límites entre módulos

Son módulos de un mismo monolito de escritorio, no microservicios ni dominios completamente autónomos.

- Inventario es dueño de las reglas de saldo de materiales y recortes. Movimientos y Órdenes las utilizan, no las duplican.
- Activos controla el estado de herramientas e insumos; un caso de uso de Movimientos puede coordinar sus traslados.
- Órdenes controla recepción y estado de la orden; Application coordina el efecto en Inventario dentro de la misma transacción.
- Conteos registra una medición y su diferencia. Finalizar un conteo no ajusta stock automáticamente.
- Catálogos concentra referencias compartidas. No depende de Movimientos u Órdenes.
- Reportes compone lecturas; no es propietario de saldos ni modifica entidades.
- Una entidad referencia otra por ID cuando no necesita controlar su ciclo de vida; no se construye un grafo completo de toda la base.
- No se encadenan casos de uso públicos que abran transacciones anidadas. La operación externa coordina repositorios y reglas bajo una sola UoW.

Un catálogo simple puede tener una entidad sencilla. Los modelos ricos se justifican donde existen reglas, como stock, órdenes y conteos. `Common` no se convierte en un depósito de clases sin dueño.

## 4. Contratos y reglas de implementación

### 4.1. Dominio

Se parte del modelo anterior: `Material`, `Recorte`, `Movimiento`, `OrdenCompra`, `ConteoInventario`, `CanaletaPlegado`, `Herramienta`, `Insumo` y cálculos de valorización. Antes de trasladar cada regla se identifica su implementación actual y su prueba.

- Propiedades de grilla, filas separadoras, filtros y textos formateados pasan a DTOs/ViewModels.
- Las entidades dejan de aceptar estados imposibles a través de setters públicos indiscriminados.
- Las reglas de saldo no negativo, recepción monotónica, estados y ajustes quedan en métodos con nombres de negocio.
- Se usan `decimal` y políticas explícitas de precisión: saldos actuales `numeric(10,2)`, líneas de movimiento `numeric(10,4)` y cantidades enteras de OC. No se aplica un redondeo genérico que cambie todas las operaciones.
- Se caracterizan fracciones y acumulaciones antes de definir las conversiones entre cantidad de línea y saldo; las pruebas deben comparar el resultado del dominio con PostgreSQL.
- Los IDs y textos persistidos de tipos de movimiento se traducen a conceptos tipados en un mapper. No se decide el impacto de stock leyendo el texto de un ComboBox.
- El comportamiento legacy de tipos no canónicos se prueba y documenta antes de reemplazarlo; no se inventa una nueva clasificación durante el traslado.
- No se implementa infraestructura de eventos de dominio si bastan resultados/deltas devueltos por las entidades.

Contratos de negocio a extraer del modelo anterior y comprobar con el baseline:

| Modelo | Contrato verificable |
|---|---|
| `Material` | Entrada/salida no producen saldo negativo; ajuste manual compara el saldo esperado y devuelve un delta firmado para el historial, no ejecuta SQL. |
| `Recorte` | Cantidad no negativa, dimensiones válidas y coincidencia por material/dimensión/estado; una deduplicación no pierde cantidad. |
| `OrdenCompra` | Pendiente pasa a parcial o completa; recepción por línea no retrocede ni supera lo pedido; no se reemplazan líneas ni elimina una orden recibida. Se conservan edición de cabecera y corrección de material en los estados actualmente permitidos. |
| `OrdenCompraLinea` | `faltante = pedida - recibida`; al corregir un material recibido se devuelve un traspaso de saldo que Application aplica de manera atómica. |
| `Movimiento` | Líneas válidas de material, recorte o compra eventual y traslados de insumos coordinados; tipo/impacto explícitos; registro interno no editable desde el historial ordinario. |
| `ConteoInventario` | Solo admite cambios estando abierto; finalizar requiere detalles, fija saldo de comparación y diferencias, y no modifica Material. Autorización de actor en Application. |
| `CanaletaPlegado` | Obra y descripción válidas, precio no negativo y orden de fotos definido; sin dependencias de archivos o servicios externos en la entidad. |
| `Herramienta` / `Insumo` | Traslados y estado coherentes con su historial; conservar identificación de herramienta y restricciones de insumos consumidos. |
| Valorización de obra | Componentes enviados/eventuales/aportes/fletes/canaletas pendientes menos devoluciones, de acuerdo con las fórmulas caracterizadas; cálculo puro, sin acceso a DB. |

Los value objects iniciales se limitan a conceptos con invariantes: cantidad de saldo, cantidad de línea de movimiento, cantidades de orden, importe, número de orden/remito, punto origen/destino y metadatos de foto. Cantidad de saldo y de movimiento no comparten un redondeo implícito. Un delta firmado no se representa con un objeto que prohíba valores negativos.

Para cantidades de OC se distingue pedido positivo de recibido/faltante no negativos. Los límites y representaciones persistidas se validan en el mapper; no se reemplaza cada `int` por un tipo ceremonial sin una regla que explicar.

### 4.2. Application

Casos de uso concretos, con constructor explícito y método `ExecuteAsync`. Cada escritura sigue esta secuencia:

1. Capturar el actor y comprobar el permiso correspondiente.
2. Validar la solicitud y abrir la unidad de trabajo cuando corresponde.
3. Cargar y bloquear los datos necesarios.
4. Invocar reglas de dominio.
5. Persistir todas las modificaciones y el historial relacionado.
6. Confirmar una sola vez y devolver un resultado a la presentación.

Las consultas usan interfaces específicas y DTOs de lectura. No necesitan reconstruir agregados para llenar una grilla. Las restricciones de lectura por rol también se aplican en Application, según la matriz funcional acordada, no únicamente ocultando botones.

Puertos mínimos, creados al necesitarlos: repositorios tipados, queries por módulo, `IUnitOfWork`, `IClock`, `IActorContext`, `IAuthorizationPolicy`, `IAuthService`, `IFileStorage`, `IDraftStore`, `IPdfReportGenerator<T>`, `IAppVersionProvider`, `IBrandingAssetProvider` y los puertos de actualización demo.

No hay repositorio universal con nombres de tabla enviados desde la UI. Tampoco se crea una interfaz por cada clase pura: las abstracciones representan fronteras que realmente deben poder sustituirse.

### 4.3. Transacciones y duración de dependencias

**Una escritura transaccional en PostgreSQL = un scope DI de ejecución = una UoW = una conexión/transacción propia.** Una ventana abierta no mantiene una transacción ni una conexión abierta. Una operación solo de archivos o una consulta de actualización no abre una transacción SQL innecesaria.

- La configuración, la fábrica de conexiones, el reloj y los proveedores inmutables son singleton. La fábrica crea conexiones; no conserva una conexión singleton.
- UoW, repositorios y casos de uso se resuelven en el scope de ejecución.
- Una sesión interna de Infrastructure comparte la conexión y transacción de esa UoW entre sus repositorios. Sus tipos Npgsql nunca salen a Application.
- Cada query dispone su propia conexión al terminar; no comparte la conexión mutable de otra ejecución.
- Los presenters y formularios tienen duración de ventana y no capturan repositorios/UoW de ese alcance corto.
- Para hacerlo explícito, Presentation define un ejecutor tipado de casos de uso: recibe la clase de caso de uso y una función `ExecuteAsync`, crea un scope, resuelve esa clase, espera su resultado y dispone el scope. Su implementación vive en CompositionRoot.
- El ejecutor no es un bus de mensajes ni decide negocio. No expone `IServiceProvider` al presenter ni al núcleo; no hace commit por su cuenta.
- Las pruebas de Application instancian los casos de uso directamente con dobles, sin DI ni ejecutor.
- Error o cancelación antes de commit implica rollback y liberación. Cancelar después de un commit confirmado no significa que la operación se deshizo.
- Ante pérdida de conexión durante commit se informa resultado incierto y se verifica persistencia antes de sugerir reintentar. No se agregan reintentos automáticos de escrituras sin un contrato de idempotencia.

La navegación utiliza una factoría antes de instanciar formularios, evitando crear y desechar una vista cuando ya está en caché. Al cerrar la ventana se cancelan cargas pendientes, se desuscriben eventos y se evitan actualizaciones sobre controles eliminados. Una escritura ya confirmada no se revierte por cerrar su ventana.

### 4.4. UI y compatibilidad

- Formularios con MVP: muestran información, capturan intención del usuario y delegan al presenter.
- Se conserva el constructor sin parámetros necesario para el diseñador, sin abrir DB ni arrancar servicios desde ese camino. La factoría de ejecución conecta el presenter una sola vez.
- Los eventos de UI pueden ser `async void`; la lógica esperable y testeable del presenter devuelve `Task` y maneja errores explícitamente.
- Estados de carga, vacío y error; bloqueo de doble clic; feedback consistente tras guardar.
- Descomponer `Vista_Movimientos` solo después de extraer su lógica: cabecera, selector de líneas, carrito y coordinador de borrador. No rearmar visualmente el formulario completo de una vez.
- No renombrar/mover DLL Guna ni actualizar paquetes de terceros junto con el refactor. Conservar identidad de assembly, formularios partial, `.resx` y referencias del `.csproj`.
- Mantener `AppVersion`, marca y recursos embebidos en el ejecutable. Los generadores PDF reciben bytes/datos mediante puertos y no buscan recursos en una DLL suponiendo que es el EXE.
- Las rutas físicas se calculan desde el host y se inyectan: no usar `Assembly.GetExecutingAssembly().Location` dentro de una DLL embebida para encontrar el programa.

### 4.5. Errores y configuración

Los errores de negocio, acceso denegado, concurrencia y dependencia local no disponible se distinguen para que el presenter pueda mostrar un mensaje adecuado. Se conserva la causa técnica en el diagnóstico local cuando corresponde, sin mostrar cadenas de conexión o credenciales en pantalla. No se utiliza `catch { }` para transformar un fallo de guardado en éxito.

La configuración de DB se construye una vez y se toma de un archivo externo local o variables del entorno de la presentación; se conserva un ejemplo reproducible y se valida al arrancar sin bloquear innecesariamente la UI. `App.config` mantiene runtime/binding redirects y no se usa como almacén de contraseñas. Al retirar configuración sensible embebida se migra la prueba de fallback a defaults no sensibles; no se deja una aserción engañosa para aparentar compatibilidad.

## 5. Adaptadores exclusivamente locales

### 5.1. Identidad demo, con permisos reales dentro de la aplicación

`DemoAuthService` implementa `IAuthService` y valida un catálogo fijo, explícito y pequeño de cuentas de presentación. Se conservan las cuentas documentadas de administrador y galpón y sus entradas equivalentes existentes que se decida mantener en la matriz de compatibilidad. No se aceptan contraseñas arbitrarias.

El resultado contiene identidad y rol, no JWT. La sesión en memoria comienza sin autenticar y con rol `None`; logout borra todo el estado. Los casos de uso aplican la autorización aunque se invoquen sin pasar por los botones de la UI.

Si se conserva «recordar sesión», se adapta `CredentialStore` con DPAPI y se mantiene esa función aislada en Infrastructure. No se construye un sistema de refresh tokens para usuarios fijos. Se presenta como acceso demo local, no como autenticación empresarial segura.

**Aceptación:** administrador/operador válidos, contraseña incorrecta, usuario inexistente, sesión vacía, logout, acceso denegado desde un caso de uso y login sin conexión a Internet. Ninguna ruta intenta contactar `localhost:54321` ni un servicio remoto.

### 5.2. Fotos y borradores

`LocalFileStorage` reutiliza inicialmente `%LOCALAPPDATA%/SteeLog/Storage` y los `object_path` actuales. El identificador `canaletas-fotos` puede conservarse como categoría de almacenamiento para compatibilidad SQL; ya no implica un bucket remoto.

- Validar tamaño y tipos compatibles con el esquema antes de persistir. El límite actual es 8 MB y los MIME permitidos por la tabla se verifican, no se amplían por aceptar una extensión en la UI.
- Resolver rutas dentro de la raíz configurada y rechazar escapes mediante rutas absolutas o `..`.
- Usar claves únicas, no sobrescribir fotos ajenas ni borrar archivos originales elegidos por el usuario.
- Archivos y PostgreSQL no forman una transacción ACID conjunta. Se utiliza staging, una clave estable por operación y compensación de errores conocidos.
- Un error de commit con resultado incierto exige consultar por esa clave antes de borrar una foto que podría estar referenciada. Si no puede determinarse el resultado, conservarla como pendiente de conciliación.
- Si falla una limpieza, registrar el pendiente localmente para reintento controlado; no ocultar el fallo ni agregar una cola distribuida. La conciliación nunca borra archivos cuya ausencia de referencia no pueda confirmarse.
- Para borrar canaletas: capturar referencias, confirmar eliminación DB y después limpiar archivos. El fallo de limpieza no se informa como rollback de una eliminación ya confirmada.
- Borradores JSON quedan detrás de `IDraftStore`, se escriben con reemplazo seguro y conservan compatibilidad de lectura durante la transición. No se borran borradores existentes al cambiar nombres de clases.

### 5.3. Actualización demostrativa

Se reemplaza la integración GitHub por `LocalDemoUpdateFeed` con un manifiesto y un paquete locales, preparados junto a la aplicación. El manifiesto puede estar fijado para mostrar una versión posterior a `AppVersion.Current`.

Separación prevista:

| Componente | Qué demuestra realmente |
|---|---|
| `ConsultarActualizacionUseCase` + `IUpdateFeed` | Lectura de metadatos y comparación real de versiones. |
| `PrepararActualizacionDemoUseCase` + `IUpdatePackageStager` | Lectura/copia de un archivo local a staging, avance por bytes y verificación de tamaño/hash. |
| Presenter/modal existente adaptado | Notas, progreso, cancelación y errores de la operación. |

El progreso proviene de trabajo real sobre el archivo local, no de un contador que siempre termina en éxito. No se descarga Internet, no se lanza un reemplazador, no se cierra la app y no se modifica el ejecutable en uso.

El cierre dice **«Demostración: paquete local preparado y verificado; instalación real fuera de alcance»**. La versión real del programa no cambia. Se pueden probar nueva versión, versión igual, manifiesto inválido, paquete faltante, hash incorrecto y cancelación.

Esto aprovecha el hardcodeo permitido para demostrar inversión de dependencias: la fuente es fija, pero el caso de uso y el tratamiento de resultados funcionan de verdad. No se representa una instalación inexistente como realizada.

## 6. Matriz de migración y reglas críticas

| Módulo visible | Origen principal | Destino y prueba indispensable |
|---|---|---|
| Materiales e inventario | `MaterialDatos`, `InventarioDatos`, `Vista_Materiales`, `Vista_Inventario` | Material y consultas propias; edición sin pisar stock concurrente; alta usa ID retornado; proveedores e historial atómicos. |
| Movimientos y Kardex | `MovimientoDatos`, `Vista_Movimientos`, historial y PDFs | Casos de registrar/editar/eliminar; stock e insumos atómicos; reversión sin negativos; registro interno protegido. |
| Recortes | `RecorteDatos`, `Vista_Recortes`, modal | Reglas de cantidad/dimensión/estado y deduplicación; salida y reversión concurrentes. |
| Órdenes | `OrdenCompraDatos`, `OrdenCompraEstados`, cuatro vistas | Agregado y cinco operaciones: crear, editar, cambiar material, recibir y eliminar; recepción parcial/completa y sin doble acreditación. |
| Conteos | `ConteoInventarioDatos`, `Vista_Conteo` | Estados, propiedad y diferencias; cierre no altera saldo; todas las mutaciones bloquean/revisan la cabecera antes de escribir detalles. |
| Canaletas y fotos | Datos de canaletas/fotos y vista | Casos de uso y archivos locales; validación, compensación y lectura de fotos anteriores. |
| Herramientas e insumos | `HerramientaDatos`, `InsumoDatos` | Entidades y traslados; conservar historial, prefijo de herramientas y pertenencia a obra/galpón. |
| Catálogos | Datos específicos y `ComunDatos` | CRUD tipado de categorías, obras, proveedores, fletes y ubicaciones; sin nombres de tablas enviados por la vista. |
| Reportes y búsqueda | `InformeObraDatos`, `ReporteStockDatos`, PDFs, `BusquedaGlobal` | DTOs de consultas y renderers sin SQL; mismos totales, secciones, filtros y restricciones de lectura. |
| Login y utilidades | Servicios estáticos, `Program`, `VistaGeneral` | Composición, sesión local, navegación, borradores y actualización demo; sin red externa. |

### Bloqueos que no se pueden perder en el traslado

1. Stock manual: bloqueo de la fila y comparación con el stock esperado por el formulario.
2. Ajuste interno diario: preservar la coordinación de `pg_advisory_xact_lock(141414, fecha)` y sus líneas/cabecera.
3. Movimiento: bloquear los recursos afectados en orden consistente; incluir conjuntos anteriores y nuevos al editar para revertir y aplicar sin carreras.
4. Órdenes: cabecera/detalles y materiales involucrados; recepción y cambio de material no pueden duplicar crédito ni perder saldo.
5. Conteos: no basta con bloquear la cabecera únicamente al cerrar. Alta, edición, eliminación de detalle y cierre deben participar del mismo protocolo y verificar estado dentro de la transacción.

Se documenta un orden de adquisición común entre operaciones, no solo «ordenar los IDs» en un método. Las pruebas concurrentes comprueban el protocolo. No se afirma que usar `FOR UPDATE` por sí solo elimina todos los deadlocks.

## 7. Refactor frente a cambios de comportamiento

Cada tarea futura se clasifica como **R** (reorganización compatible), **C** (corrección funcional identificada) o **D** (demostración deliberada). El tipo y su efecto aparecen en la descripción y las pruebas; no se cambia todo bajo la etiqueta «refactor».

| Tipo | Cambio | Tratamiento |
|---|---|---|
| R | Extraer SQL, entidades, presenters, queries y generadores PDF. | Mismos resultados y UX, comprobados con caracterización. |
| C | Transacción conjunta de movimientos e insumos. | Corrección explícita en la fase de stock; demostrar rollback y reversión coherente. |
| C | Bloquear modificaciones de conteos cerrados y asegurar permisos previstos. | Pruebas de estado y concurrencia; no inventar roles ni permisos adicionales. |
| C | Coherencia de cantidades de OC y tipos/tamaños de fotos con el esquema. | Rechazo claro antes de guardar; sin truncamiento ni cambios de tipo SQL improvisados. |
| D | Reemplazar auth cloud por catálogo demo y updater remoto por fixture local. | Alcance académico declarado, sin fingir garantías productivas. |
| Pendiente fuera de la ruta principal | Recepción de OC como movimiento automático en Kardex. | Conservar comportamiento de recepción durante la migración. Documentar limitación y no activar el ADR-01 anterior sin una decisión funcional posterior. |

El flujo de Kardex es un punto de decisión de negocio, no un requisito para lograr Clean Architecture. Si se decide ampliarlo después, debe tener una tarea propia, pruebas contra doble contabilización y tratamiento explícito de recepciones históricas.

## 8. Plan de ejecución por fases

Las fases se ejecutan secuencialmente. Cada una deja una aplicación compilable y utilizable. Un módulo no se declara migrado porque exista su carpeta: debe completar dominio, caso de uso, adaptador, presenter y pruebas correspondientes.

No se fija una duración ficticia ni se automatizan agentes. Cada cierre registra archivos, pruebas ejecutadas, resultados reales y pendientes en Engram. Los commits se realizan conforme a la política del repositorio y a la autorización vigente; no se hacen pushes ni publicaciones para verificar una fase.

### Fase 0. Baseline funcional y mapa de compatibilidad

**Trabajo:** registrar commit/estado de partida; inventariar las pantallas y operaciones; capturar ejemplos de PDF, datos semilla y flujos admin/galpón. Ejecutar las seis familias de pruebas existentes en entornos de prueba. Identificar defectos previos sin presentarlos como regresiones nuevas.

Preparar una matriz con operación, entrada, resultado, tablas, permiso y prueba. Registrar DLL Guna resuelta, recursos, configuración y salida del empaquetado. Leer el esquema como fuente de contratos, sin asumir que la DB existente coincide con él.

**Salida:** baseline verificable y lista de diferencias R/C/D. Ningún reset de la base actual ni pérdida de archivos locales. Los fallos de baseline quedan reproducidos y se asignan a la fase pertinente; no se documentan como PASS.

### Fase 1. Esqueleto de solución y reglas de arquitectura

**Trabajo:** agregar los cuatro proyectos nuevos; fijar versiones compatibles de herramientas de pruebas/DI; conservar el `.csproj` WinForms, `App.config`, marca y Guna. Probar restauración y build desde checkout separado. Introducir CompositionRoot, configuración única, rutas y factoría de conexiones.

Crear tests de referencias entre ensamblados y de dependencias de tipos/IL. Registrar una lista temporal, por tipo legacy, con fase de retiro. Esa lista solo puede disminuir. Hacer el primer smoke de diseñador y de carga de ensamblados embebidos antes de extraer módulos grandes.

**Salida:** Debug/Release compilan, tests de arquitectura nuevos ejecutan y hay un camino comprobado para empaquetar sin tocar nube. No se afirma que un `Build` ejecuta xUnit automáticamente.

### Fase 2. Host local, identidad y utilidades demostrativas

**Trabajo:** sustituir auth/sesión por los adaptadores locales; retirar tokens del contrato; conservar «recordar sesión» si pertenece al baseline. Introducir navegación por factoría y ejecutor por operación. Conectar el updater demo y eliminar chequeos GitHub automáticos de arranque.

El servicio de fotos pasa a un adaptador local transitorio compatible para que las vistas todavía no migradas no necesiten tokens. Las fachadas legacy que subsistan son solo puentes temporales y tienen fecha de retiro por fase.

**Salida:** arranque/login/logout/roles y actualización demo funcionan con Internet desconectado. Abrir ventanas no abre transacciones persistentes. No se reemplaza ni relanza el EXE.

### Fase 3. Piloto vertical: órdenes de compra

**Trabajo:** extraer `OrdenCompra`, líneas/estados/value objects necesarios, repositorio, queries, cinco casos de uso y presenters de alta/listado/revisión/módulo. Extraer PDF de pendientes. Incorporar la porción mínima compartida de Inventario necesaria para recepción y cambio de material, con una sola implementación del cálculo de stock.

Mantener cambios de cabecera permitidos por estado y transferencia de stock al corregir material ya recibido. Resolver cantidades enteras antes de persistir. Usar el scope por operación desde el primer piloto; no construirlo después.

**Salida:** mismas cuatro pantallas operativas, cero `new OrdenCompraDatos` desde esas vistas, tests de recepción parcial/completa/concurrente, estado inválido, edición y rollback. Comparar PDF y resultados con baseline. Esta fase valida la estructura completa antes de extenderla al resto.

### Fase 4. Inventario, movimientos, recortes e insumos

**Trabajo:** completar dominio de stock y casos de registrar/editar/eliminar movimientos; migrar materiales, recortes e insumos y sus vistas/modales. Extraer borradores y PDF de remito. Reorganizar componentes de Movimientos después de extraer su comportamiento.

Aplicar explícitamente la corrección C de atomicidad: altas, cambios y eliminación coordinan también el historial/estado de insumos. Preservar ajuste interno diario, guardas de reversión, concurrencia y IDs correctos. No agregar todavía el movimiento automático de recepción OC.

**Salida:** tests de integridad anteriores preservados, rollback conjunto probado, edición/eliminación sin residuos de insumos ni stock negativo. Abrir/cerrar/reabrir y recuperar borrador mantienen la experiencia actual.

### Fase 5. Conteos

**Trabajo:** extraer agregado, queries, operaciones de carga/corrección/finalización/cancelación/eliminación, permisos y PDF. Usar el mismo bloqueo de cabecera desde todos los escritores de detalles.

**Salida:** después de cerrar/cancelar no se aceptan detalles nuevos ni correcciones; una carrera carga/cierre tiene un resultado consistente. Diferencias exactas y stock sin ajuste automático. Vistas y PDF conservados.

### Fase 6. Canaletas y archivos locales

**Trabajo:** migrar agregado, casos de uso, metadatos/fotos, presenter y almacenamiento local definitivo. Incorporar staging/compensación/pendientes y lectura de fotos preexistentes. Quitar completamente dependencias de tokens y servicios Supabase de vistas/reportes de canaletas.

**Salida:** alta, edición, adjunto, lectura, eliminación y reporte funcionan offline. Probar archivo inválido, error de disco, rollback conocido, resultado incierto y limpieza pendiente sin borrar fotos referenciadas.

### Fase 7. Herramientas y catálogos

**Trabajo:** migrar herramientas y completar catálogos tipados, consultas y presenters. Retirar `ComunDatos`, SQL con tablas/columnas recibidas desde UI y los puentes legacy que ya no tengan consumidores.

**Salida:** CRUD y traslados preservados; historial inicial de herramientas correcto; ningún formulario de estos módulos conoce persistencia. No se fabrican agregados complejos para CRUD sin invariantes.

### Fase 8. Reportes, búsqueda y cierre de dependencias

**Trabajo:** completar DTOs y queries de inventario, obra, Kardex, stock configurado y búsqueda. Extraer todo iTextSharp de formularios. Los PDF consumen datos ya obtenidos, no repositorios. Migrar cálculo de valorización a una función/servicio puro con pruebas.

Retirar clases/carpetas legacy solo cuando no queden consumidores; conservar utilidades de presentación como ordenamiento de grillas en su capa correcta. Eliminar referencias directas Npgsql/iTextSharp que ya no correspondan a WinForms y quitar excepciones transitorias de arquitectura.

**Salida:** cero excepciones legacy en los tests; búsqueda no bloquea la UI; reportes mantienen filtros, secciones, cifras, marca y restricciones de rol. Tests UI de foco/login y marca siguen cubiertos.

### Fase 9. Reproducibilidad y defensa académica

**Trabajo:** preparar datos ficticios coherentes y una instalación local reproducible. Delimitar tablas de SteeLog y su cierre de dependencias; preparar un esquema/seed de exposición sin módulos empresariales ajenos cuando ese cierre esté verificado. Probarlo en una DB/volumen nuevos, nunca limpiando los existentes como atajo.

No se impone «exactamente 24 tablas»: el número final se deriva del modelo y las dependencias necesarias. Los scripts y dumps antiguos no se borran sin inspección/decisión. Si el esquema de exposición es nuevo, su adopción requiere backup y migración/copia selectiva explícita, no `down -v`.

Preparar build/paquete local, comandos de verificación, diagrama de dependencias, mapa de módulos, ejemplos de transacción y una guía corta de defensa. Documentar qué es real, qué es demo y qué queda fuera de alcance.

**Salida:** recorrer todos los módulos desde una instalación limpia preparada, con Internet desconectado, sin tocar DB ajenas; ejecutar suites y empaquetado; comprobar Designer y EXE. Registrar la evidencia real, no solo afirmar «funciona offline».

## 9. Estrategia de pruebas y evidencia académica

### Suites previstas

| Suite | Pruebas que la justifican | Dependencias |
|---|---|---|
| Arquitectura | Domain sin dependencias técnicas; Application solo Domain; UI sin SQL/Infrastructure fuera de CompositionRoot; renderers PDF sin persistencia; sin ciclos propios. | Análisis de ensamblados/tipos/IL. |
| Dominio | Stocks, precisión, reversión, estados OC, conteos y valorización. | Ninguna DB, archivo ni ventana. |
| Application | Autorización, una confirmación por operación, rollback, coordinación entre módulos, actualización demo. | Dobles de puertos, reloj y actor. |
| Integración | SQL real, constraints, concurrencia, UoW compartida, bootstrap y archivos. | PostgreSQL Docker de prueba y carpetas temporales. |
| Presenters/UI | Carga, error, doble envío, cierre/cancelación, foco login, cache/navegación y recursos. | Vistas falsas y smoke Windows/STA según prueba. |
| Aceptación | Recorrido por módulos, PDFs, instalación limpia, persistencia al reiniciar y ejecución offline. | Aplicación local preparada; parte manual explícita. |

Un repositorio falso no demuestra rollback SQL. Las pruebas de integración verifican saldos e historial después de provocar un fallo real a mitad de la operación. La prueba del ejecutor DI debe demostrar que dos ejecuciones tienen sesiones distintas y que los repositorios de una misma ejecución comparten transacción.

Los tests de arquitectura inspeccionan también llamadas y tipos anidados/generados por `async`, no únicamente los campos públicos. Deben detectar un acceso indebido introducido deliberadamente en una prueba de control. No se equipara cantidad de interfaces con cumplimiento arquitectónico.

No se promete 100% de cobertura global. Se exige cubrir explícitamente las invariantes y caminos críticos enumerados, con un reporte que identifique lo medido y lo no cubierto. Las pruebas de Designer/visual no se reemplazan por cobertura de líneas.

### Ejecución prevista, no implementada en esta planificación

Crear un punto de entrada `scripts/verificar.ps1` que restaure/compile y ejecute suites con códigos de salida comprobados. Separar suites rápidas de las que necesitan Docker y de las comprobaciones manuales de UI. Resolver MSBuild con herramientas del entorno, no con una ruta fija a un equipo particular.

Durante la transición se siguen usando los `Tests/*/run.ps1` actuales. Al retirarlos, el nuevo runner debe cubrir sus aserciones o explicar el reemplazo de un contrato obsoleto, por ejemplo token cloud sustituido por sesión demo. No dejar de probar configuración, marca o foco porque cambió la arquitectura.

Toda DB de prueba lleva un nombre validado `steelog_test_<GUID>`, guardas de identidad y limpieza limitada a la base creada por esa ejecución. Nunca apuntar pruebas destructivas a `inventario_db`. Los archivos de prueba se limitan a una carpeta temporal propia.

## 10. Guion técnico de la exposición

1. Iniciar PostgreSQL con Compose y mostrar persistencia/aislamiento mediante LazyDocker; explicar que WinForms corre en Windows.
2. Mostrar el grafo de proyectos y una regla de arquitectura que impide invertir una dependencia.
3. Recorrer una operación real: clic de recepción/movimiento, presenter, caso de uso, regla de dominio, repositorios y commit.
4. Ejecutar una regla de dominio en pruebas sin Docker: demostrar independencia, no solo describirla.
5. Provocar stock insuficiente en integración y mostrar que el remito, los saldos y los insumos quedan consistentes.
6. Mostrar permisos al invocar un caso de uso con el rol incorrecto y luego recorrer las pantallas de ambos perfiles.
7. Adjuntar una foto, generar un PDF y reiniciar la app: datos, fotos y reportes locales, sin Internet.
8. Consultar la actualización demo y preparar su paquete; explicar exactamente qué parte es demostrativa y cuál se ejecutó realmente.
9. Mostrar los resultados de pruebas y una limitación reconocida, como la semántica actual de recepciones OC/Kardex si no se amplió.

No se prepara un fallo artificial para luego ocultarlo ni se promete que el usuario puede manipular el EXE/DB sin saltear permisos. El objetivo es demostrar calidad interna de una aplicación académica local.

## 11. Criterios finales de aceptación

- [ ] Los módulos visibles y la identidad SteeLog se conservan; cualquier diferencia tiene una entrada R/C/D y prueba.
- [ ] Los cinco proyectos respetan el grafo sin excepciones legacy y las pruebas lo detectan.
- [ ] Dominio y casos de uso pueden probarse sin abrir WinForms, PostgreSQL ni archivos.
- [ ] No hay reglas de stock, SQL ni generación iTextSharp dentro de formularios/presenters.
- [ ] Movimientos, insumos, recepciones y ajustes conservan integridad en fallos y concurrencia.
- [ ] Login demo, autorización, fotos, borradores, PDF y actualización demostrativa funcionan sin Internet.
- [ ] No se ejecuta GitHub/Supabase ni reemplazo del EXE en el perfil académico.
- [ ] Los datos de exposición son ficticios y el bootstrap local fue probado sin destruir bases existentes.
- [ ] Debug/Release, suite de pruebas, diseñador y empaquetado tienen resultados registrados; no se infiere uno de otro.
- [ ] Hay un documento/guion breve que permite explicar las decisiones y sus límites con ejemplos del código real.

**Definición de terminado:** organización comprensible, dependencias comprobadas y operaciones demostrablemente correctas. No es «tener muchas capas» ni «haber movido todos los archivos».

## 12. Riesgos y límites de la ejecución futura

| Riesgo | Medida |
|---|---|
| Romper el diseñador o DLL Guna antigua. | Mantener host y bibliotecas; mover lógica primero y verificar Designer/recursos por módulo. |
| Compilar con DLL suelta pero fallar con Costura. | Smoke de ensamblados/versiones/recursos en fase 1 y al cierre, sin publicar. |
| Un presenter conserva una conexión toda la sesión. | Ejecutor por operación, tests de scopes y revisiones de dependencias capturadas. |
| Cambiar un cálculo al crear value objects. | Baseline de fracciones, totales y persistencia antes de reemplazar la lógica. |
| Perder fotos o borradores anteriores. | Compatibilidad de rutas/formatos, backup y prueba sobre copia. |
| Cambiar resultados funcionales bajo un refactor. | Clasificación R/C/D y comparación con baseline; Kardex OC separado. |
| Agregar infraestructura innecesaria por prestigio académico. | Cada puerto/patrón debe resolver una frontera o una regla mostrable; no crear servicios distribuidos. |
| Falta Internet al presentar. | Preparar imágenes/paquetes; ensayo completo offline y fixture local de actualización. |

No se crean propuestas SDD, flujos de agentes ni entregables de Impeccable para ejecutar este plan. El documento es un plan ordinario solicitado por el usuario; no autoriza por sí mismo implementación, migraciones, resets, commits ni publicación.

### Referencias técnicas consultadas

- [Microsoft: .NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard): compatibilidad del núcleo propuesto con .NET Framework y separación del host.
- [Microsoft: pautas de inyección de dependencias](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines): duración de servicios, disposición y scopes explícitos.
- [PostgreSQL 17: bloqueos explícitos](https://www.postgresql.org/docs/17/explicit-locking.html): locks de fila, advisory locks y orden consistente de adquisición.

Las decisiones específicas de SteeLog son propuestas de este documento basadas en el código revisado; las referencias no sustituyen las pruebas del sistema.
