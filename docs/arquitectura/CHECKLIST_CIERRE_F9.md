# Checklist manual de cierre F9 (demo aislada)

Solo ejecución humana. Datos ficticios. Credenciales demo: **admin/admin** y **galpon/galpon** (ver `GUIA_DEMO_Y_DEFENSA.md`). Usar **Windows PowerShell 5.1** (`powershell.exe`), no pwsh 7: los scripts que cargan el EXE net48 fallan en pwsh 7.

Paquete candidato ya probado (HEAD `6d95eb0`): `A:\SteeLog-Pruebas\paquete-6d95eb0-77e8b3ee3e48423a8cc219ad97f20fd6`, ZIP SHA-256 `80962269407DAE86E75A317FBBA7DFC16016986283A0A937A5825A1111F9E4BA`.

## A. Preparar instalación nueva
- [ ] Docker WSL2 activo en `tcp://127.0.0.1:2375` y `docker image inspect postgres:17-alpine` OK.
- [ ] `$paquete = 'A:\SteeLog-Pruebas\paquete-6d95eb0-77e8b3ee3e48423a8cc219ad97f20fd6'`
- [ ] `$instalacion = 'A:\SteeLog-Pruebas\cierre-f9-' + [guid]::NewGuid().ToString('N')`
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File "$paquete\tools\preparar-demo.ps1" -Paquete $paquete -Destino $instalacion` → "Demo preparada", `instalacion.json` con `estado: preparada`.
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File "$instalacion\ejecutar-demo.ps1" -SinEjecutar` → "Main NO ejecutado".
- [ ] Lanzar sin `-SinEjecutar` (abre SteeLog aislado).

## B. Recorrido GUI con Admin
Estado inicial esperado: montantes 140, OC `DEMO-OC-001` pedida 10/recibida 0, conteo abierto con 139, canaleta pendiente.
- [ ] Login y ventana principal sin errores ni aviso de actualización bloqueante.
- [ ] Movimientos entrada y salida: el saldo cambia exactamente por la cantidad.
- [ ] Movimiento inválido (salida mayor al stock): aviso claro, saldo sin cambios.
- [ ] Selector rápido de materiales (pendiente conocido): Down/Enter selecciona el material. Anotar si no.
- [ ] Guardar un movimiento: el saldo cambia solo tras guardar.
- [ ] Órdenes: alta de OC nueva → aparece Pendiente en el listado.
- [ ] Recepción parcial 3 de 10 en `DEMO-OC-001` → Recibida Parcial, 3/7, soleras +3.
- [ ] Conteos: cargar, corregir, finalizar; historial con contado/comparación/diferencia; stock sin ajuste.
- [ ] Canaletas: adjuntar PNG (miniatura), rechazar `.txt`, editar, eliminar registro de prueba (solo desaparece su foto).
- [ ] Insumos y Herramientas: datos demo visibles; alta/edición de prueba.
- [ ] Catálogos: crear/editar categoría, proveedor u obra; aparece en selectores de Movimientos.
- [ ] Reportes: Stock, Kardex, Informe de obra (ver F).
- [ ] Cerrar y reabrir: datos, saldos, borrador y foto persisten; anotar si recupera sesión.

## C. Recorrido con Galpón
- [ ] Puede registrar movimientos y confirmar recepción de OC abiertas.
- [ ] Puede gestionar Insumos, Herramientas, Canaletas y cargar/corregir conteos; consulta Kardex e Informe de obra.
- [ ] No ve "Nueva orden" ni Editar/Eliminar en Órdenes.
- [ ] No ve cantidades de stock de materiales ni precios en Inventario.
- [ ] No puede crear/editar/eliminar materiales ni usar precio eventual.
- [ ] No puede eliminar conteos, editar ni eliminar movimientos desde Historial.
- [ ] No puede abrir Reporte de stock.
- [ ] Ocultar controles no es confidencialidad: si un dato sigue visible en pantalla o exportación, anotarlo como hallazgo.

## D. Designer (Visual Studio)
- [ ] Abrir sin guardar cambios: Vista_Login, Vista_Movimientos, Vista_HistorialMovimientos, Vista_Conteo, Vista_Inventario, Vista_Kardex, Vista_ReporteStock, Vista_InformeObra, Vista_Insumos, Vista_Herramientas.
- [ ] Aprobado: cada uno se renderiza sin error del diseñador y `git status` sigue limpio.

## E. Offline
- [ ] Antes: Docker local corriendo e imagen disponible.
- [ ] Desconectar Internet a mano (cable/Wi-Fi). Anotar método y hora.
- [ ] Arranque y login Admin/Galpón sin errores de red/actualización.
- [ ] Guardar movimiento, recibir OC, finalizar conteo y generar PDF sin errores ni timeouts.
- [ ] Cerrar y reabrir offline: datos persisten. Restaurar conexión al terminar.

## F. PDFs (inspeccionar todas las páginas)
- [ ] Reporte de stock, Kardex, Informe de obra y Conteo: marca, valores coherentes con pantalla, sin tablas recortadas.
- [ ] Anotar ruta y SHA-256 de cada PDF.

## Evidencia a devolver
- [ ] Salidas y exit codes de A, `instalacion.json`.
- [ ] Tabla por módulo (B y C): OK / falla / no probado, con texto exacto de errores.
- [ ] Saldos antes/después (montantes, soleras).
- [ ] Capturas: selector, recepción parcial, canaleta con foto, menú Galpón, Designer.
- [ ] Rutas/SHA-256 de PDFs y hallazgos visuales.
- [ ] Método y resultado offline; resultado del reinicio.
