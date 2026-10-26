# SteeLog — Sistema de Gestión y Logística de Inventario

**SteeLog** es una aplicación de escritorio desarrollada en **C# WinForms (.NET Framework 4.8)** con base de datos **PostgreSQL** encapsulada en **Docker**, diseñada para la administración integral de stock, materiales, obras, movimientos de inventario y trazabilidad logística en el sector de la construcción y steel framing.

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
* **Docker / Docker Engine:** En ejecución (daemon local)
* **LazyDocker:** Instalado en `A:\lazydocker` (accesible ejecutando `lazydocker` en consola)
* **.NET Framework 4.8 Runtime:** Instalado en el sistema
* **Visual Studio 2022** (opcional, para desarrollo)

---

## 📦 Puesta en Marcha Rápida

### 1. Iniciar la Base de Datos (Docker)
En la raíz del proyecto, ejecuta:
```powershell
docker compose up -d
```
Esto levantará el contenedor `steelog-db` (PostgreSQL 17 en el puerto `5435`) con el esquema de tablas, constraints de stock no negativo y datos de prueba preconfigurados.

Para gestionar los contenedores desde la terminal sin abrir Docker Desktop:
```powershell
lazydocker
```

### 2. Configuración de Conexión
El archivo `appsettings.json` contiene la cadena de conexión local:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5435;Database=inventario_db;User Id=postgres;Password=tesispassword123;Pooling=true;"
  }
}
```
*Nota: Si se clona el repositorio desde cero, el script de compilación (MSBuild) genera automáticamente `appsettings.json` a partir de `appsettings.example.json` antes del primer build.*

### 3. Ejecutar la Aplicación
Puedes iniciar directamente el ejecutable compilado:
```
WP SYSTEM\PlayerUI\bin\Release\SteeLog.exe
```
O abrir la solución `WP SYSTEM\WP_Inventario.sln` en Visual Studio y presionar **F5**.

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
Para pausar la base de datos:
```powershell
docker compose stop
```
