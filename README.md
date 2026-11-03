# SteeLog — Sistema de Gestión y Logística de Inventario

**SteeLog** es una aplicación de escritorio desarrollada en **C# WinForms (.NET Framework 4.8)** con base de datos **PostgreSQL** encapsulada en **Docker**, diseñada para la administración integral de stock, materiales, obras, movimientos de inventario y trazabilidad logística en el sector de la construcción y steel framing.

## Identidad académica y compatibilidad

La solución `SteeLog/SteeLog.sln`, el proyecto `SteeLog/SteeLog/SteeLog.csproj`, los namespaces y el ejecutable `SteeLog.exe` usan la identidad SteeLog. Se conservan las dependencias de terceros y el certificado sin alterar sus contenidos.

La procedencia, los prompts y las variantes de la marca están en [Resources/Branding](SteeLog/SteeLog/Resources/Branding/README.md). Esta separación visual no certifica la anonimización de datos de bases existentes ni del historial del repositorio.

---

## 🚀 Características Principales

* **Control de Stock y Materiales:** Catálogo completo de materiales, perfiles, placas, fijaciones, etc., con control de stock mínimo y alertas.
* **Trazabilidad de Movimientos:** Registro de entradas, compras, envíos a obra, devoluciones, ventas y transferencias entre depósitos.
* **Gestión de Obras y Proveedores:** Control de asignación de insumos y materiales por obra o cliente.
* **Reportes y Kardex:** Generación automatizada de reportes de existencias, histórico de movimientos y exportación en PDF/Excel.
* **Entorno Aislado con Docker:** Base de datos PostgreSQL contenida en Docker, permitiendo ejecución 100% local e independiente sin dependencias en la nube.
* **Gestión de Contenedores con LazyDocker:** Monitorización y control de contenedores por terminal sin necesidad de abrir la interfaz pesada de Docker Desktop.

---

## 🛠️ Requisitos Previos

* **Sistema Operativo:** Windows 10 / 11
* **Docker Engine (WSL2) / Docker y Docker Compose:** motor Linux en ejecución (daemon local en WSL2, sin requerir la interfaz de Docker Desktop)
* **LazyDocker:** instalado y disponible en `PATH` (en este equipo: `A:\lazydocker`)
* **.NET Framework 4.8 Runtime:** Instalado en el sistema
* **Visual Studio 2022** (opcional, para desarrollo)

---

## 📦 Puesta en Marcha Rápida

### 1. Iniciar PostgreSQL y abrir LazyDocker

Asegúrate de que el motor Docker en WSL2 esté disponible (el script lo detecta y despierta automáticamente si está en reposo). Desde PowerShell, en la raíz del repositorio:

```powershell
.\docker\iniciar.ps1
```

El script valida Docker y Compose, usa explícitamente el proyecto `steelog` y este archivo Compose, inicia el servicio (límite de 120 segundos), espera hasta 60 segundos a que PostgreSQL acepte conexiones y abre LazyDocker. Si vence el límite de inicio, Docker podría continuar creando recursos: revisa su estado antes de reintentar. También se puede iniciar sin la interfaz con `.\docker\iniciar.ps1 -SinInterfaz`. Para abrir LazyDocker por separado desde la raíz, ejecuta `lazydocker --project steelog --file .\docker-compose.yml`. LazyDocker controla y monitorea contenedores; no sustituye al motor de Docker. Su vista abarca todo el daemon, no solo SteeLog: evita operar sobre contenedores o volúmenes de otros proyectos.

La GUI de SteeLog es WinForms con .NET Framework 4.8: se ejecuta en Windows, no dentro del contenedor Linux. Compose ejecuta únicamente PostgreSQL 17 como `steelog-db`, publicado en `127.0.0.1:5435`, accesible solo desde el host. Si el puerto o el nombre del contenedor ya están ocupados, revisa el conflicto antes de continuar; no se detiene ni borra la instancia ajena.

Los scripts de `docker/init` crean el esquema y datos iniciales **solo al crear un volumen vacío**. No actualizan bases existentes ni las reinicializan al reiniciar el contenedor. El volumen `steelog_postgres_data` conserva los datos: no uses `docker compose down -v` ni limpiezas de volúmenes si quieres conservarlos. Para verificar el estado sin modificar datos:

```powershell
docker compose -p steelog -f .\docker-compose.yml ps
docker compose -p steelog -f .\docker-compose.yml exec -T db pg_isready -U postgres -d inventario_db
docker compose -p steelog -f .\docker-compose.yml logs --tail 50 db
```

### 2. Configuración de Conexión
El archivo privado `SteeLog/SteeLog/appsettings.json` contiene la cadena de conexión. La configuración de ejemplo apunta a PostgreSQL local en el puerto 5435. No compartas ni subas tu archivo privado.

La contraseña incluida en Compose y en el ejemplo es conocida y está destinada solo al desarrollo local; **no uses esta configuración en producción**. Cambiar una variable de Compose no rota automáticamente las credenciales de un volumen PostgreSQL existente. Para un despliegue real se necesitan credenciales propias y un procedimiento de rotación adecuado.

*Nota: Si se clona el repositorio desde cero, el script de compilación (MSBuild) genera `appsettings.json` a partir de `appsettings.example.json` solo si aún no existe.*

### 3. Ejecutar la Aplicación
Puedes iniciar directamente el ejecutable compilado:
```
SteeLog\SteeLog\bin\Release\SteeLog.exe
```
O abrir la solución `SteeLog\SteeLog.sln` en Visual Studio y presionar **F5**.

### 4. Inicio de Sesión (Modo Demostración / Tesis)
La aplicación cuenta con credenciales locales de prueba:
* **Administrador:**
  * **Usuario:** `admin` (o `admin@tesis.local`)
  * **Contraseña:** `admin` (o `123456`)
  * Acceso completo con permisos de edición, valorizaciones, configuración y administración.
* **Operador / Galpón:**
  * **Usuario:** `galpon` (o `galpon@tesis.local`)
  * **Contraseña:** `galpon` (o `123456`)
  * Vista restringida de inventario operativo (cantidades ocultas/restringidas según rol de galpón).

---

## 🛑 Detener Servicios
Para detener la base de datos sin eliminar contenedores ni datos:
```powershell
docker compose -p steelog -f .\docker-compose.yml stop
```

Para volver a iniciarla sin abrir LazyDocker, ejecuta `.\docker\iniciar.ps1 -SinInterfaz`. El comando `stop` indicado conserva el volumen y los datos.
