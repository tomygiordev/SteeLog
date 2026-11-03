# Verificación F3 — Órdenes de compra (pendiente auditoría)

## Alcance implementado

- `SteeLog.Domain/OrdenesCompra`: cabecera y líneas, estados, unicidad y cantidades enteras, recepción acumulativa y validación previa a la mutación; `Domain/Inventario/SaldoInventario.cs` calcula el saldo después de un delta sin permitir negativo.
- `SteeLog.Application/OrdenesCompra`, `Inventario` y `Common`: cinco operaciones con autorización antes de comenzar la transacción, DTO/consultas tipadas, reloj, puerto PDF y unidad de trabajo. Se permite rehidratar cabeceras históricas sin proveedor o con flete negativo; crear y editar requieren proveedor y flete no negativo. La fecha elegida por el usuario se conserva.
- `SteeLog.Infrastructure/OrdenesCompra`, `Inventario`, `Persistence` y `Pdf`: SQL parametrizado, UoW scoped con conexión y transacción por operación, bloqueos ordenados y renderer iTextSharp aislado. Las lecturas crean conexiones independientes. La raíz aporta imagen, fecha local y pie; el renderer no conoce el host.
- `SteeLog/Presentation` y `Vistas/Movimientos/Ordenes`: presenter con vista inyectada y ejecutor por scope; vista de alta, listado, revisión y contenedor mantienen controles y navegación. Las lecturas de alta/revisión/preview invalidan solicitudes anteriores o cerradas. `Datos/OrdenCompraDatos.cs`, `Modelos/OrdenCompraEstados.cs` y el renderer host se retiraron; el inventario legacy disminuyó 30 pares.
- La recepción modifica el stock y el historial propio de OC como antes; **no crea un movimiento Kardex automático**. Repetir un total parcial produce delta cero; una orden completa rechaza una nueva confirmación como el código previo.

## Evidencia ejecutada

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verificar.ps1 -LegacyLocal`: exit 0, 64 pruebas en Debug y 64 en Release, más Configuration, search, Branding y LoginUsability. Log `%TEMP%/steelog-f3-verificacion-7ed77b8333cd410984e2ede2888b45dd.log`. Incluye dominio, permisos, UoW con dobles, vista falsa, cancelación, TCS para A→B/nueva/cierre y handlers de vistas sin carga vigente; los dobles no demuestran ACID.
- `SteeLog/SteeLog/Tests/Branding/run.ps1`: exit 0; log `%TEMP%/steelog-f3-branding2-4d7d124becad42b19e9f440381ce7b6e.log`. Verifica cinco encabezados legacy y el PDF real de OC renderizado desde Infrastructure con logo y dos filas (OC-101, 12/5/7; OC-102, 40/10/30); no es apertura manual del visor de PDF.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tests/ordenes-integracion.ps1`: SQL real en PostgreSQL 17 efímero, 6/6 PASS con inventario separado y renderer PDF actual. `%TEMP%/steelog-ordenes-2d2529276129474b937938a8d8ea1aa8/runner.log` y `results/ordenes.trx`; incluye parcial/completa, repetición, concurrencia, transferencia, fallos/cancelación con rollback, restricciones, permisos y scope; también consultas de listado, detalle, pendientes y lookups. API Docker contrastada con salida/exit 0 y error SQL/exit 1. El runner verificó nombre, ID y etiqueta del contenedor propio antes de eliminarlo y usó una base `steelog_test_<GUID>` con guarda; no empleó el clúster de producto.
- Los SHA-256 de `README.md` y `docker/iniciar.ps1` permanecieron en `90E113DAC3B2806F37F0C04963D1FB0A7DFBE84F34C180D4369FE507EE8EB9E1` y `27A92887208DF3A0DA4F986645CB8AEF574593A48606FC22D7EA18D4F665DC0F`, respectivamente.

## Límites y pendientes

No se ejecutaron `Program.Main`, servicios remotos, updater real ni publicación; no se modificaron intencionalmente bases, roles o volúmenes existentes. El smoke STA construye las cuatro vistas y llama APIs sin mostrarlas y los tests del presenter usan una vista falsa: **no comprueban el Designer de Visual Studio ni el recorrido manual completo**. Las pruebas de reloj distinguen fecha de Argentina y UTC alrededor de medianoche; no se cambió la zona del servidor ni se usa el reloj para reemplazar la fecha de orden elegida.

## Auditoría final del orquestador

Se contrastaron fuentes y contratos entre capas, retiro de consumidores legacy, permisos, UoW compartida, tickets de carga y guardas del modelo vigente, renderer y recursos, launchers y pruebas. Las dos entregas intermedias se corrigieron antes del cierre: el PDF dejó de residir en el host y la asincronía dejó de permitir aplicar resultados obsoletos o guardar mientras el modelo seleccionado no estuviera listo.

El orquestador ejecutó las fuentes finales:

- `scripts/verificar.ps1 -LegacyLocal`: **exit 0, 64 pruebas Debug y 64 Release**, más las cuatro suites legacy. Log `%TEMP%/steelog-root-f3-local-9c6d8388d9fc46cf9cc975589ccc98de.log`.
- `scripts/tests/ordenes-integracion.ps1`: **exit 0, 6/6 pruebas reales, cero omitidas**; TRX en `%TEMP%/steelog-ordenes-42a7688845b14e89a8603d13fd0ca1b5/results/ordenes.trx`. Log externo `%TEMP%/steelog-root-f3-sql-d5aade6f2912416e92dea7161bb17303.log`. Se contrastó la limpieza del contenedor y la base propios.
- PDF de muestra renderizado e inspeccionado visualmente: marca, encabezado, tabla y cantidades legibles, sin recortes visibles. Esto no es un recorrido manual del módulo ni prueba del Designer.
- `git diff --cached --check`, UTF-8 y ausencia de mojibake revisados; inventario legacy de **353 a 323 pares**, sin ampliar excepciones. Hashes de cambios previos conservados. Solo se retiraron líneas vacías finales durante el cierre, sin cambio funcional posterior a las pruebas.

**F3 aceptada dentro de su alcance de implementación y verificación automatizada.** Persisten los límites manuales/Designer y del ensayo offline integral ya registrados en F0/F9. El núcleo de stock y las transacciones de movimientos/recortes/insumos restantes corresponden a F4; no se declaran migrados por esta aceptación.
