# SteeLog — Sistema de Gestión y Logística de Inventario

**SteeLog** es una aplicación de escritorio desarrollada en **C# WinForms (.NET Framework 4.8)** con base de datos **PostgreSQL** encapsulada en **Docker**, diseñada para la administración integral de stock, materiales, obras, movimientos de inventario y trazabilidad logística en el sector de la construcción y steel framing.

---

## 🚀 Características Principales

* **Control de Stock y Materiales:** Catálogo completo de materiales, perfiles, placas, fijaciones, etc., con control de stock mínimo y alertas.
* **Trazabilidad de Movimientos:** Registro de entradas, compras, envíos a obra, devoluciones, ventas y transferencias entre depósitos.
* **Gestión de Obras y Proveedores:** Control de asignación de insumos y materiales por obra o cliente.
* **Reportes y Kardex:** Generación automatizada de reportes de existencias, histórico de movimientos y exportación en PDF/Excel.
* **Entorno Aislado con Docker:** Base de datos PostgreSQL contenida en Docker Desktop, permitiendo ejecución 100% local e independiente.

---

## 🛠️ Requisitos Previos

* **Sistema Operativo:** Windows 10 / 11
* **Docker Desktop:** Instalado y en ejecución
* **.NET Framework 4.8 Runtime:** Instalado en el sistema
* **Visual Studio 2022** (opcional, para desarrollo)

---

## 📦 Puesta en Marcha Rápida

### 1. Iniciar la Base de Datos (Docker)
En la raíz del proyecto, ejecuta:
```powershell
docker compose up -d
```
Esto levantará el contenedor `steelog-db` (PostgreSQL 17 en el puerto `5435`) con el esquema de tablas y datos de prueba preconfigurados.

### 2. Ejecutar la Aplicación
Puedes iniciar directamente el ejecutable compilado:
```
WP SYSTEM\PlayerUI\bin\Debug\SteeLog.exe
```
O abrir la solución `WP SYSTEM\WP_Inventario.sln` en Visual Studio y presionar **F5**.

### 3. Inicio de Sesión (Modo Demostración / Tesis)
* **Usuario:** `admin` (o `admin@tesis.local`)
* **Contraseña:** `admin` (o cualquier valor)
* Acceso directo con permisos completos de **Administrador**.

---

## 🛑 Detener Servicios
Para pausar la base de datos:
```powershell
docker compose stop
```
