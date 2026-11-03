# Plan Maestro de Correcciones de Calidad y Robustez Arquitectónica (Auditoría Engram #1538)

- **Repositorio:** `A:\SteeLog` (Solución: `A:\SteeLog\SteeLog\SteeLog.sln`)
- **Estado Base del Repositorio:**
  - **Fase 3 (Órdenes de Compra):** Cerrada en commit `8010e36`.
  - **Fase 4 (Inventario y Movimientos):** Cerrada en commit `041602f`.
  - **Fase 5 (Conteos):** En desarrollo activo en el *working tree* bajo `SteeLog.*/Conteos/`.
- **Restricción Crítica de Aislamiento:**
  - **PROHIBIDO** leer para modificar, tocar, formatear o alterar cualquier archivo dentro de `SteeLog.Domain/Conteos/`, `SteeLog.Application/Conteos/`, `SteeLog.Infrastructure/Conteos/`, `SteeLog/Vistas/Conteos/` o cualquier símbolo de Fase 5.

---

## Mapa General de Ejecución por Tareas Atómicas

Las 6 debilidades detectadas en la auditoría técnica (`#1538`) se organizan en **6 Tareas Atómicas independientes y secuenciales**, diseñadas específicamente para ser ejecutadas por subagentes implementadores (`gemini-3.8-flash-high`) sin colisiones de archivos ni regresiones cruzadas:

| Tarea | Hallazgo Auditado | Capa Principal | Riesgo de Colisión | Verificación Primaria |
| :--- | :--- | :--- | :--- | :--- |
| **Tarea 1** | `OrdenesPendientesPDF.cs`: `FileStream` y `Document` sin `using` | Infrastructure (`Pdf`) + Tests | Nulo | `dotnet test --filter "FullyQualifiedName~RemitoPdfTests"` |
| **Tarea 2** | `Recorte.cs`: Invariante dimensional incompleta (`ValidarDimension`) | Domain (`Inventario`) + Tests | Nulo | `dotnet test --filter "FullyQualifiedName~MaterialRecorteTests"` |
| **Tarea 3** | `EstadosOrden.cs`: `Todos` expone `string[]` mutable en lugar de `IReadOnlyList<string>` | Domain (`OrdenesCompra`) + Tests | Nulo | `dotnet test --filter "FullyQualifiedName~OrdenCompraDomainTests"` |
| **Tarea 4** | `NpgsqlMovimientoAuxiliar.cs`: `ActualizarAsync` sin `FOR UPDATE` en línea ni guarda de filas en `UPDATE` | Infrastructure / Application (`Movimientos`) + Tests | Bajo | `scripts/tests/inventario-integracion.ps1` |
| **Tarea 5** | `FiltroMaterial.cs` y `ModalSeleccionProveedores.cs`: Desacoplar `CategoriaDatos` y `ComunDatos` | Application / Infrastructure / Presentation / Host (`Vistas/Gestión`) + `LegacyDependencies.txt` | Medio (actualiza `LegacyDependencies.txt`) | `scripts/verificar.ps1 -LegacyLocal` + `scripts/tests/inventario-integracion.ps1` |
| **Tarea 6** | `ModalRecortes.cs`: Evitar recortes huérfanos con stock `0` al cancelar modal o movimiento | Host (`Vistas/Movimientos`) + Application / Infrastructure (`Movimientos` / `Borradores`) + Tests | Medio (coordina UI carrito, borrador y UoW) | `scripts/verificar.ps1 -LegacyLocal` + `scripts/tests/inventario-integracion.ps1` |

---

## TAREA 1: Gestión Determinista de Recursos en `OrdenesPendientesPDF.cs`

### 1.1. Diagnóstico Exacto
En `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Pdf\OrdenesPendientesPDF.cs` (líneas 49–181), el método `Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items)`:
1. No valida `string.IsNullOrWhiteSpace(rutaDestino)` antes de abrir el archivo.
2. Lanza `new Exception("No hay órdenes pendientes para imprimir.")` genérica en lugar de `InvalidOperationException`.
3. Instancia `new Document(...)` y `new FileStream(rutaDestino, FileMode.Create)` sin bloques `using`. Si `Image.GetInstance(marca.LogoPng)`, el renderizado de tabla o `doc.Open()` lanza una excepción antes de `doc.Close()`, el descriptor de archivo (`FileStream`) queda abierto y bloquea `rutaDestino` en el sistema operativo.
4. En `MovimientoPDF.cs` (líneas 57–74) ya existe el patrón estándar de la solución con validación de `rutaDestino` y doble bloque `using (var stream = new FileStream(...))` + `using (var doc = new Document(...))`.

### 1.2. Archivos Afectados
- **Modificar:**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Pdf\OrdenesPendientesPDF.cs`
  - Relativa: `SteeLog/SteeLog.Infrastructure/Pdf/OrdenesPendientesPDF.cs`
- **Modificar (Pruebas):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\RemitoPdfTests.cs`
  - Relativa: `SteeLog/SteeLog.Tests/RemitoPdfTests.cs`

### 1.3. Impacto y Dependencias Cruzadas (Archivos Vecinos)
- `A:\SteeLog\SteeLog\SteeLog.Application\OrdenesCompra\OrdenesPdf.cs`: Define `IOrdenesPendientesPdf.Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items)`. La firma no cambia.
- `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Pdf\MovimientoPDF.cs`: Vecino de referencia arquitectónica; comparte `IRecursosPdf` y `MarcaPdf`.
- `A:\SteeLog\SteeLog\SteeLog\CompositionRoot\LocalServices.cs` (línea 58): Registra `OrdenesPdf = new OrdenesPendientesPDF(new RecursosPdfLocales());`. No requiere cambios.
- `A:\SteeLog\SteeLog\SteeLog\Presentation\OrdenesPresenter.cs` (líneas 57–63) y `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\Ordenes\Vista_OrdenesListado.cs` (línea 488): Capturan `Exception ex`, por lo que `InvalidOperationException` (subclase de `Exception`) mantiene compatibilidad total con la vista.

### 1.4. Especificación de Código (Antes / Después)

#### En `SteeLog/SteeLog.Infrastructure/Pdf/OrdenesPendientesPDF.cs`:

**ANTES (líneas 49–181):**
```csharp
        public void Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count == 0) throw new Exception("No hay órdenes pendientes para imprimir.");

            MarcaPdf marca = recursos.Capturar();
            Document doc = new Document(PageSize.A4, 40, 40, 40, 60);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(rutaDestino, FileMode.Create));
            writer.PageEvent = new PdfFooter(marca.Pie);

            doc.Open();
            // ... construcción de encabezado, tabla y celdas ...
            doc.Add(table);
            doc.Close();
        }
```

**DESPUÉS:**
```csharp
        public void Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items)
        {
            if (string.IsNullOrWhiteSpace(rutaDestino)) throw new ArgumentException("La ruta de destino es obligatoria.", nameof(rutaDestino));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count == 0) throw new InvalidOperationException("No hay órdenes pendientes para imprimir.");

            MarcaPdf marca = recursos.Capturar();
            using (var stream = new FileStream(rutaDestino, FileMode.Create))
            using (var doc = new Document(PageSize.A4, 40, 40, 40, 60))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, stream);
                writer.PageEvent = new PdfFooter(marca.Pie);

                doc.Open();

                Font fontEmpresa = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(0, 72, 173));
                Font fontSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.GRAY);
                Font fontTituloDoc = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
                Font fontFecha = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DARK_GRAY);

                Font fontTablaHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                Font fontGrupo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(30, 41, 59));
                Font fontCelda = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                Font fontCeldaBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.BLACK);
                Font fontAlerta = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(180, 83, 9));

                PdfPTable headerTable = new PdfPTable(2);
                headerTable.WidthPercentage = 100;
                headerTable.SetWidths(new float[] { 60f, 40f });
                headerTable.SpacingAfter = 20f;

                PdfPCell cellLeft = new PdfPCell();
                cellLeft.Border = Rectangle.NO_BORDER;
                if (marca.LogoPng != null && marca.LogoPng.Length > 0)
                {
                    Image logo = Image.GetInstance(marca.LogoPng);
                    logo.ScaleToFit(120f, 60f);
                    logo.Alignment = Element.ALIGN_LEFT;
                    cellLeft.AddElement(logo);
                }
                else
                {
                    cellLeft.AddElement(new Paragraph("STEELOG", fontEmpresa));
                }
                Paragraph sub = new Paragraph("Sistema de Gestión Logística y Materiales", fontSubtitulo);
                sub.SpacingBefore = 5f;
                cellLeft.AddElement(sub);
                headerTable.AddCell(cellLeft);

                PdfPCell cellRight = new PdfPCell();
                cellRight.Border = Rectangle.NO_BORDER;
                cellRight.HorizontalAlignment = Element.ALIGN_RIGHT;

                Paragraph titulo = new Paragraph("REPORTE DE PENDIENTES", fontTituloDoc);
                titulo.Alignment = Element.ALIGN_RIGHT;
                cellRight.AddElement(titulo);

                Paragraph fecha = new Paragraph($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy HH:mm}", fontFecha);
                fecha.Alignment = Element.ALIGN_RIGHT;
                fecha.SpacingBefore = 5f;
                cellRight.AddElement(fecha);

                Paragraph totalItems = new Paragraph($"Total ítems por recibir: {items.Count}", fontSubtitulo);
                totalItems.Alignment = Element.ALIGN_RIGHT;
                cellRight.AddElement(totalItems);

                headerTable.AddCell(cellRight);
                doc.Add(headerTable);

                LineSeparator line = new LineSeparator(1f, 100f, new BaseColor(200, 200, 200), Element.ALIGN_CENTER, -5);
                doc.Add(new Chunk(line));
                doc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 15f, 41f, 14f, 14f, 16f });
                table.SpacingBefore = 10f;
                table.HeaderRows = 1;

                BaseColor colorPrimario = new BaseColor(0, 72, 173);
                string[] headers = { "N° Orden", "Material Solicitado", "Pedido", "Recibido", "Faltante" };

                foreach (string h in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(h, fontTablaHeader));
                    cell.BackgroundColor = colorPrimario;
                    cell.HorizontalAlignment = h == "Material Solicitado" ? Element.ALIGN_LEFT : Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 8;
                    cell.BorderColor = colorPrimario;
                    table.AddCell(cell);
                }

                var gruposPorProveedor = items.GroupBy(x => new { x.Proveedor, x.EmailProveedor });
                BaseColor colorFondoGrupo = new BaseColor(226, 232, 240);
                BaseColor colorZebra = new BaseColor(248, 250, 252);
                BaseColor colorBordeSuave = new BaseColor(226, 232, 240);

                foreach (var grupo in gruposPorProveedor)
                {
                    string textoGrupo = $"PROVEEDOR: {grupo.Key.Proveedor.ToUpper()}   |   Contacto: {grupo.Key.EmailProveedor}";
                    PdfPCell cellGrupo = new PdfPCell(new Phrase(textoGrupo, fontGrupo));
                    cellGrupo.Colspan = 5;
                    cellGrupo.BackgroundColor = colorFondoGrupo;
                    cellGrupo.PaddingTop = 8;
                    cellGrupo.PaddingBottom = 8;
                    cellGrupo.PaddingLeft = 10;
                    cellGrupo.BorderColor = new BaseColor(203, 213, 225);
                    table.AddCell(cellGrupo);

                    bool filaAlterna = false;
                    foreach (var item in grupo)
                    {
                        BaseColor bgFila = filaAlterna ? colorZebra : BaseColor.WHITE;

                        AgregarCelda(table, item.NumeroOrden, fontCeldaBold, Element.ALIGN_CENTER, bgFila, colorBordeSuave);
                        AgregarCelda(table, item.Material, fontCelda, Element.ALIGN_LEFT, bgFila, colorBordeSuave);
                        AgregarCelda(table, item.CantidadPedida.ToString("0.##"), fontCelda, Element.ALIGN_CENTER, bgFila, colorBordeSuave);
                        AgregarCelda(table, item.CantidadRecibida.ToString("0.##"), fontCelda, Element.ALIGN_CENTER, bgFila, colorBordeSuave);
                        AgregarCelda(table, item.Faltante.ToString("0.##"), fontAlerta, Element.ALIGN_CENTER, bgFila, colorBordeSuave);

                        filaAlterna = !filaAlterna;
                    }
                }

                doc.Add(table);
                doc.Close();
            }
        }
```

### 1.5. Pruebas Automatizadas Requeridas
En `A:\SteeLog\SteeLog\SteeLog.Tests\RemitoPdfTests.cs`, agregar dos pruebas unitarias:
1. `OrdenesPendientesPdfValidaRutaItemsVaciosYGeneraArchivo`:
   - Verifica `Assert.Throws<ArgumentException>(() => pdf.Generar(" ", items))`
   - Verifica `Assert.Throws<ArgumentNullException>(() => pdf.Generar(ruta, null))`
   - Verifica `Assert.Throws<InvalidOperationException>(() => pdf.Generar(ruta, new PendienteLectura[0]))`
   - Verifica generación exitosa con `%PDF-` y limpieza del archivo temporal.
2. `OrdenesPendientesPdfLiberaDescriptorSiFallaRenderizadoDeLogo`:
   - Pasa un `IRecursosPdf` cuyo `LogoPng` contiene bytes inválidos (`new byte[] { 1, 2, 3 }`) para forzar un fallo dentro del bloque tras crear el `FileStream`.
   - Verifica que `pdf.Generar(ruta, items)` lanza excepción y que inmediatamente después `File.Delete(ruta)` o `File.Open(ruta, FileMode.Open, FileAccess.ReadWrite, FileShare.None)` **no** lanza `IOException` por archivo bloqueado.

### 1.6. Comando de Verificación
```powershell
dotnet test A:\SteeLog\SteeLog\SteeLog.Tests\SteeLog.Tests.csproj -c Release --filter "FullyQualifiedName~RemitoPdfTests"
```

---

## TAREA 2: Unificación de Invariante Dimensional en `Recorte.cs`

### 2.1. Diagnóstico Exacto
En `A:\SteeLog\SteeLog\SteeLog.Domain\Inventario\Recorte.cs` (líneas 15–28):
- Actualmente se valida `if (medida > 99999999.99m || decimal.Round(medida, 2) != medida) throw new ArgumentOutOfRangeException(nameof(medida));`.
- Cuando el recorte es bidimensional (`ancho > 0 && altura > 0 && medida == 0`), `medida` vale `0`, por lo que `ancho` y `altura` **no** pasan por la validación de escala (máximo 2 decimales, `numeric(10,2)` en PostgreSQL) ni por el límite superior (`99999999.99m`).
- Debe crearse la función estática privada `ValidarDimension(decimal valor, string nombre)` y aplicarse de forma uniforme a `ancho`, `altura` y `medida`, manteniendo intacta la validación previa de combinación de dimensiones positivas (`ArgumentException`) para no alterar el contrato de excepciones existente.

### 2.2. Archivos Afectados
- **Modificar:**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Domain\Inventario\Recorte.cs`
  - Relativa: `SteeLog/SteeLog.Domain/Inventario/Recorte.cs`
- **Modificar (Pruebas):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\MaterialRecorteTests.cs`
  - Relativa: `SteeLog/SteeLog.Tests/MaterialRecorteTests.cs`

### 2.3. Impacto y Dependencias Cruzadas (Archivos Vecinos)
- `A:\SteeLog\SteeLog\SteeLog.Domain\Inventario\Material.cs`: Define `Material.ValidarCantidad(decimal valor, string nombre, bool positivo)`. `ValidarDimension` sigue exactamente la misma convención de firma y excepción (`ArgumentOutOfRangeException(nombre, ...)`).
- `A:\SteeLog\SteeLog\SteeLog.Application\Inventario\Recortes.cs` (`AgregarRecorte.EjecutarAsync`): Instancia `new Recorte(0, materialId, ancho, altura, medida, cantidad, estado)`, beneficiándose automáticamente de la validación dimensional unificada antes de abrir conexión o transacción.
- `A:\SteeLog\SteeLog\SteeLog.Tests\MaterialRecorteTests.cs`: Prueba unitaria existente `RecorteValidaDimensionesCantidadYNormalizaEstado` (líneas 29–43).

### 2.4. Especificación de Código (Antes / Después)

#### En `SteeLog/SteeLog.Domain/Inventario/Recorte.cs`:

**ANTES (líneas 15–41):**
```csharp
        public Recorte(int id, int materialId, decimal ancho, decimal altura, decimal medida, decimal cantidad, string estado)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (materialId <= 0) throw new ArgumentOutOfRangeException(nameof(materialId));
            if (!((ancho > 0 && altura > 0 && medida == 0) || (ancho == 0 && altura == 0 && medida > 0) || (ancho > 0 && altura == 0 && medida == ancho) || (altura > 0 && ancho == 0 && medida == altura)))
                throw new ArgumentException("Ingrese una medida o bien ancho y altura positivos.");
            if (medida > 99999999.99m || decimal.Round(medida, 2) != medida)
                throw new ArgumentOutOfRangeException(nameof(medida));
            Material.ValidarCantidad(cantidad, nameof(cantidad), false);
            var texto = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();
            if (texto != null && texto.Length > 50) throw new ArgumentException("El estado no puede superar 50 caracteres.", nameof(estado));

            Id = id;
            MaterialId = materialId;
            Ancho = ancho;
            Altura = altura;
            Medida = medida > 0 ? medida : 0;
            Cantidad = cantidad;
            Estado = texto;
        }
```

**DESPUÉS:**
```csharp
        public Recorte(int id, int materialId, decimal ancho, decimal altura, decimal medida, decimal cantidad, string estado)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (materialId <= 0) throw new ArgumentOutOfRangeException(nameof(materialId));
            if (!((ancho > 0 && altura > 0 && medida == 0) || (ancho == 0 && altura == 0 && medida > 0) || (ancho > 0 && altura == 0 && medida == ancho) || (altura > 0 && ancho == 0 && medida == altura)))
                throw new ArgumentException("Ingrese una medida o bien ancho y altura positivos.");
            ValidarDimension(ancho, nameof(ancho));
            ValidarDimension(altura, nameof(altura));
            ValidarDimension(medida, nameof(medida));
            Material.ValidarCantidad(cantidad, nameof(cantidad), false);
            var texto = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();
            if (texto != null && texto.Length > 50) throw new ArgumentException("El estado no puede superar 50 caracteres.", nameof(estado));

            Id = id;
            MaterialId = materialId;
            Ancho = ancho;
            Altura = altura;
            Medida = medida > 0 ? medida : 0;
            Cantidad = cantidad;
            Estado = texto;
        }

        private static void ValidarDimension(decimal valor, string nombre)
        {
            if (valor < 0 || valor > 99999999.99m || decimal.Round(valor, 2) != valor)
                throw new ArgumentOutOfRangeException(nombre, "La dimensión debe ser no negativa, no superar 99999999.99 y tener como máximo dos decimales.");
        }
```

### 2.5. Pruebas Automatizadas Requeridas
En `A:\SteeLog\SteeLog\SteeLog.Tests\MaterialRecorteTests.cs`, extender `RecorteValidaDimensionesCantidadYNormalizaEstado` (o agregar un `[Theory]`/`[Fact]` adicional) verificando:
- `Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 1.234m, 2m, 0m, 1m, "Bueno"));` (escala > 2 decimales en `ancho`)
- `Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 1m, 2.345m, 0m, 1m, "Bueno"));` (escala > 2 decimales en `altura`)
- `Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 100000000m, 2m, 0m, 1m, "Bueno"));` (límite superior en `ancho`)
- `Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 1m, 100000000m, 0m, 1m, "Bueno"));` (límite superior en `altura`)
- `Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 0m, 0m, 1.234m, 1m, "Bueno"));` (escala > 2 decimales en `medida`)
- `Assert.Throws<ArgumentOutOfRangeException>(() => new Recorte(0, 1, 0m, 0m, 100000000m, 1m, "Bueno"));` (límite superior en `medida`)

### 2.6. Comando de Verificación
```powershell
dotnet test A:\SteeLog\SteeLog\SteeLog.Tests\SteeLog.Tests.csproj -c Release --filter "FullyQualifiedName~MaterialRecorteTests"
```

---

## TAREA 3: Inmutabilidad de `EstadosOrden.Todos`

### 3.1. Diagnóstico Exacto
En `A:\SteeLog\SteeLog\SteeLog.Domain\OrdenesCompra\EstadosOrden.cs` (línea 8):
- `public static readonly string[] Todos = { Pendiente, RecibidaCompleta, RecibidaParcial };`
- En C#, `readonly` sobre un campo de tipo arreglo (`string[]`) solo impide reasignar la referencia del arreglo, pero permite que cualquier consumidor mute sus elementos en tiempo de ejecución (`EstadosOrden.Todos[0] = "Corrupto"`), rompiendo las invariantes de dominio de `EstadosOrden`.
- Cambiar el tipo expuesto a `IReadOnlyList<string>` inicializado mediante `Array.AsReadOnly(new[] { Pendiente, RecibidaCompleta, RecibidaParcial })`.

### 3.2. Archivos Afectados
- **Modificar:**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Domain\OrdenesCompra\EstadosOrden.cs`
  - Relativa: `SteeLog/SteeLog.Domain/OrdenesCompra/EstadosOrden.cs`
- **Modificar (Pruebas):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\OrdenCompraDomainTests.cs`
  - Relativa: `SteeLog/SteeLog.Tests/OrdenCompraDomainTests.cs`

### 3.3. Impacto y Dependencias Cruzadas (Archivos Vecinos)
Se inspeccionaron todos los usos de `EstadosOrden` en toda la solución:
1. `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\Ordenes\Vista_OrdenesListado.cs` (línea 244):
   - Código existente:
     ```csharp
     var items = new List<string> { "Todos" };
     items.AddRange(EstadosOrden.Todos);
     ```
   - `List<T>.AddRange(IEnumerable<T>)` recibe `IEnumerable<string>`. Como `IReadOnlyList<string>` hereda de `IEnumerable<string>`, **compila y funciona sin requerir ninguna modificación en `Vista_OrdenesListado.cs`**.
2. `A:\SteeLog\SteeLog\SteeLog.Domain\OrdenesCompra\OrdenCompra.cs`, `A:\SteeLog\SteeLog\SteeLog.Infrastructure\OrdenesCompra\NpgsqlOrdenes.cs`, `NpgsqlOrdenesConsulta.cs`, `Vista_OrdenesCompra.cs`, `ModalRecepcionOrden.cs`:
   - Usan las constantes `EstadosOrden.Pendiente`, `EstadosOrden.RecibidaParcial`, `EstadosOrden.RecibidaCompleta` y el método `EstadosOrden.EsValido(string)`. Ninguno indexa ni muta `EstadosOrden.Todos`.

### 3.4. Especificación de Código (Antes / Después)

#### En `SteeLog/SteeLog.Domain/OrdenesCompra/EstadosOrden.cs`:

**ANTES (líneas 1–15):**
```csharp
using System;

namespace SteeLog.Domain.OrdenesCompra
{
    public static class EstadosOrden
    {
        public const string Pendiente = "Pendiente";
        public const string RecibidaCompleta = "Recibida Completa";
        public const string RecibidaParcial = "Recibida Parcial";
        public static readonly string[] Todos = { Pendiente, RecibidaCompleta, RecibidaParcial };

        public static bool EsValido(string estado) =>
            estado == Pendiente || estado == RecibidaCompleta || estado == RecibidaParcial;
    }
}
```

**DESPUÉS:**
```csharp
using System;
using System.Collections.Generic;

namespace SteeLog.Domain.OrdenesCompra
{
    public static class EstadosOrden
    {
        public const string Pendiente = "Pendiente";
        public const string RecibidaCompleta = "Recibida Completa";
        public const string RecibidaParcial = "Recibida Parcial";
        public static readonly IReadOnlyList<string> Todos = Array.AsReadOnly(new[] { Pendiente, RecibidaCompleta, RecibidaParcial });

        public static bool EsValido(string estado) =>
            estado == Pendiente || estado == RecibidaCompleta || estado == RecibidaParcial;
    }
}
```

### 3.5. Pruebas Automatizadas Requeridas
En `A:\SteeLog\SteeLog\SteeLog.Tests\OrdenCompraDomainTests.cs`, agregar un `[Fact]`:
- `EstadosOrdenTodosEsInmutableYContieneLosTresEstadosCanonicos`:
  - Verifica que `EstadosOrden.Todos` contiene exactamente `[Pendiente, RecibidaCompleta, RecibidaParcial]`.
  - Verifica que el intento de mutación mediante cast a `IList<string>` (`((IList<string>)EstadosOrden.Todos)[0] = "Otro"`) lanza `NotSupportedException`.

### 3.6. Comando de Verificación
```powershell
dotnet test A:\SteeLog\SteeLog\SteeLog.Tests\SteeLog.Tests.csproj -c Release --filter "FullyQualifiedName~OrdenCompraDomainTests"
```

---

## TAREA 4: Bloqueo Pesimista y Guarda de Concurrencia en `NpgsqlMovimientoAuxiliar.cs`

### 4.1. Diagnóstico Exacto
En `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Movimientos\NpgsqlMovimientoAuxiliar.cs` (`NpgsqlPrecioEventual.ActualizarAsync`, líneas 23–30):
1. **Lectura sin bloqueo (`FOR UPDATE`):** La consulta inicial `SELECT id_movimiento FROM public.movimientomaterial WHERE id_movimientomaterial=@id AND es_compra_eventual=true` no adquiere `FOR UPDATE` ni valida `x == DBNull.Value`. Si otra transacción concurrente edita o elimina las líneas del movimiento mientras se ejecuta `ActualizarAsync`, la lectura inicial puede observar una fila a punto de borrarse o desincronizada.
   - **Nota sobre orden de bloqueo (Deadlock Prevention):** En `OperacionesMovimiento.cs` (`EditarMovimiento` y `EliminarMovimiento`), la transacción bloquea primero la cabecera `public.movimientos` (`FOR UPDATE OF m`) y luego opera sobre `public.movimientomaterial`. Si `NpgsqlPrecioEventual.ActualizarAsync` bloquease primero `movimientomaterial` y luego `movimientos`, invertiría el orden de bloqueo frente a `EditarMovimiento`/`EliminarMovimiento` cuando ambos compiten por el mismo movimiento.
   - **Solución libre de interbloqueos y con `FOR UPDATE` completo:**
     1. Resolver/bloquear primero la cabecera del movimiento y la línea en el orden canónico (`movimientos` -> `movimientomaterial`), o bien leer `id_movimiento` y bloquear `public.movimientos m LEFT JOIN public.tipo_movimiento t ON t.id_tipomovimiento=m.id_tipomovimiento WHERE m.id_movimiento=@id FOR UPDATE OF m` seguido inmediatamente de `SELECT id_movimiento FROM public.movimientomaterial WHERE id_movimientomaterial=@id AND id_movimiento=@movimiento AND es_compra_eventual=true FOR UPDATE` (o bloquear la línea con `FOR UPDATE` tras bloquear la cabecera `movimientos`).
     2. Espera: ¿qué pasa si la consulta 1 ya incluye `FOR UPDATE` sobre `movimientomaterial`? En `EditarMovimiento` y `EliminarMovimiento`, `movimientomaterial` solo se modifica en `EliminarLineasYTrasladosAsync` **después** de haber adquirido `FOR UPDATE OF m` en `movimientos`. Para garantizar tanto:
        - el requisito explícito de la auditoría (*"lectura de `id_movimiento` con `FOR UPDATE` y `UPDATE` verificando que haya afectado exactamente 1 fila"*), como
        - la ausencia de lecturas sucias y la validación completa de `Movimiento.ExigirEditable(tipoId, nombreTipo)` y del `UPDATE` final de `public.movimientos`:
        ejecutaremos:
        - Paso 1: `SELECT id_movimiento FROM public.movimientomaterial WHERE id_movimientomaterial=@id AND es_compra_eventual=true FOR UPDATE` (verificando `x == null || x == DBNull.Value`).
        - Paso 2: `SELECT m.id_tipomovimiento, t.nombre FROM public.movimientos m LEFT JOIN public.tipo_movimiento t ON t.id_tipomovimiento=m.id_tipomovimiento WHERE m.id_movimiento=@id FOR UPDATE OF m` (pasando tanto `id_tipomovimiento` como `t.nombre` a `Movimiento.ExigirEditable`).
        - Paso 3: `UPDATE public.movimientomaterial SET precio_unitario=@precio WHERE id_movimientomaterial=@id AND id_movimiento=@movimiento AND es_compra_eventual=true` verificando `await c.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1`.
        - Paso 4: En el `UPDATE public.movimientos SET precio_total = (...) WHERE id_movimiento=@id RETURNING precio_total`, verificar explícitamente que el resultado escalar no sea `null` ni `DBNull.Value` (o usar `ExecuteReaderAsync` / validar que afectó 1 fila) antes de `Convert.ToDecimal(...)`, ya que `Convert.ToDecimal(null)` devuelve `0m` silenciosamente si el `UPDATE` de cabecera afectara `0` filas.
2. En `A:\SteeLog\SteeLog\SteeLog.Application\Movimientos\GestionMovimientoAuxiliar.cs` (`ActualizarPrecioEventual.EjecutarAsync`, línea 32):
   - Validar también que `precio` no supere `99999999.99m` y tenga como máximo 2 decimales (`decimal.Round(precio, 2) != precio`), consistente con `numeric(10,2)` y con `LineaMovimiento` del dominio.

### 4.2. Archivos Afectados
- **Modificar:**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Movimientos\NpgsqlMovimientoAuxiliar.cs`
  - Relativa: `SteeLog/SteeLog.Infrastructure/Movimientos/NpgsqlMovimientoAuxiliar.cs`
- **Modificar:**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Application\Movimientos\GestionMovimientoAuxiliar.cs`
  - Relativa: `SteeLog/SteeLog.Application/Movimientos/GestionMovimientoAuxiliar.cs`
- **Modificar (Pruebas):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`
  - Relativa: `SteeLog/SteeLog.Tests/InventarioIntegrationTests.cs`

### 4.3. Impacto y Dependencias Cruzadas (Archivos Vecinos)
- `A:\SteeLog\SteeLog\SteeLog.Domain\Movimientos\Movimiento.cs`: `Movimiento.ExigirEditable(int tipoId, string nombreTipo)` recibe tanto `tipoId` como `nombreTipo`. Al incluir `LEFT JOIN public.tipo_movimiento t` en `NpgsqlPrecioEventual`, también protege movimientos internos identificados por nombre además de `tipoId == 11`.
- `A:\SteeLog\SteeLog\SteeLog\Presentation\MovimientosPresenter.cs` (líneas 56–57): Llama a `ActualizarPrecioEventual.EjecutarAsync(idDetalle, nuevoPrecio, token)`. La firma pública no cambia.

### 4.4. Especificación de Código (Antes / Después)

#### En `SteeLog/SteeLog.Application/Movimientos/GestionMovimientoAuxiliar.cs`:

**ANTES (líneas 30–36):**
```csharp
        public async Task<decimal> EjecutarAsync(int detalleId, decimal precio, CancellationToken token = default)
        {
            Authorization.Require(session.Current, Role.Admin);
            if (detalleId <= 0) throw new ArgumentOutOfRangeException(nameof(detalleId));
            if (precio < 0) throw new ArgumentOutOfRangeException(nameof(precio));
            await uow.BeginAsync(token).ConfigureAwait(false);
            var total = await repo.ActualizarAsync(detalleId, precio, token).ConfigureAwait(false);
            await uow.CommitAsync(token).ConfigureAwait(false);
            return total;
        }
```

**DESPUÉS:**
```csharp
        public async Task<decimal> EjecutarAsync(int detalleId, decimal precio, CancellationToken token = default)
        {
            Authorization.Require(session.Current, Role.Admin);
            if (detalleId <= 0) throw new ArgumentOutOfRangeException(nameof(detalleId));
            if (precio < 0 || precio > 99999999.99m || decimal.Round(precio, 2) != precio)
                throw new ArgumentOutOfRangeException(nameof(precio), "El precio unitario debe ser no negativo y tener como máximo dos decimales.");
            await uow.BeginAsync(token).ConfigureAwait(false);
            var total = await repo.ActualizarAsync(detalleId, precio, token).ConfigureAwait(false);
            await uow.CommitAsync(token).ConfigureAwait(false);
            return total;
        }
```

#### En `SteeLog/SteeLog.Infrastructure/Movimientos/NpgsqlMovimientoAuxiliar.cs`:

**ANTES (líneas 19–31):**
```csharp
    public sealed class NpgsqlPrecioEventual : IPrecioEventualEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlPrecioEventual(NpgsqlUnitOfWork uow) { this.uow = uow ?? throw new ArgumentNullException(nameof(uow)); }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction) { CommandTimeout = 15 };
        public async Task<decimal> ActualizarAsync(int id,decimal precio,CancellationToken token=default)
        {
            int movimiento;
            using(var c=Cmd("SELECT id_movimiento FROM public.movimientomaterial WHERE id_movimientomaterial=@id AND es_compra_eventual=true")) { c.Parameters.AddWithValue("id",id); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null) throw new InvalidOperationException("Solo se puede modificar el precio de un detalle eventual."); movimiento=Convert.ToInt32(x); }
            using(var c=Cmd("SELECT id_tipomovimiento FROM public.movimientos WHERE id_movimiento=@id FOR UPDATE")) { c.Parameters.AddWithValue("id",movimiento); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null) throw new InvalidOperationException("El movimiento ya no existe."); SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(Convert.ToInt32(x),null); }
            using(var c=Cmd("UPDATE public.movimientomaterial SET precio_unitario=@precio WHERE id_movimientomaterial=@id AND id_movimiento=@movimiento AND es_compra_eventual=true")) { c.Parameters.AddWithValue("id",id); c.Parameters.AddWithValue("movimiento",movimiento); c.Parameters.AddWithValue("precio",precio); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("El detalle ya no existe."); }
            using(var c=Cmd(@"UPDATE public.movimientos SET precio_total=(SELECT COALESCE(SUM(mm.cantidad*CASE WHEN mm.es_compra_eventual THEN COALESCE(mm.precio_unitario,0) ELSE COALESCE(mat.preciounitario,0) END),0) FROM public.movimientomaterial mm LEFT JOIN public.materiales mat ON mat.id=mm.id_material WHERE mm.id_movimiento=@id) WHERE id_movimiento=@id RETURNING precio_total")) { c.Parameters.AddWithValue("id",movimiento); return Convert.ToDecimal(await c.ExecuteScalarAsync(token).ConfigureAwait(false)); }
        }
    }
```

**DESPUÉS:**
```csharp
    public sealed class NpgsqlPrecioEventual : IPrecioEventualEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlPrecioEventual(NpgsqlUnitOfWork uow) { this.uow = uow ?? throw new ArgumentNullException(nameof(uow)); }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction) { CommandTimeout = 15 };
        public async Task<decimal> ActualizarAsync(int id, decimal precio, CancellationToken token = default)
        {
            int movimiento;
            using (var c = Cmd("SELECT id_movimiento FROM public.movimientomaterial WHERE id_movimientomaterial=@id AND es_compra_eventual=true FOR UPDATE"))
            {
                c.Parameters.AddWithValue("id", id);
                var x = await c.ExecuteScalarAsync(token).ConfigureAwait(false);
                if (x == null || x == DBNull.Value) throw new InvalidOperationException("Solo se puede modificar el precio de un detalle eventual.");
                movimiento = Convert.ToInt32(x);
            }
            using (var c = Cmd("SELECT m.id_tipomovimiento, t.nombre FROM public.movimientos m LEFT JOIN public.tipo_movimiento t ON t.id_tipomovimiento=m.id_tipomovimiento WHERE m.id_movimiento=@id FOR UPDATE OF m"))
            {
                c.Parameters.AddWithValue("id", movimiento);
                using (var r = await c.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    if (!await r.ReadAsync(token).ConfigureAwait(false))
                        throw new InvalidOperationException("El movimiento ya no existe.");
                    int tipoId = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                    string nombreTipo = r.IsDBNull(1) ? null : r.GetString(1);
                    SteeLog.Domain.Movimientos.Movimiento.ExigirEditable(tipoId, nombreTipo);
                }
            }
            using (var c = Cmd("UPDATE public.movimientomaterial SET precio_unitario=@precio WHERE id_movimientomaterial=@id AND id_movimiento=@movimiento AND es_compra_eventual=true"))
            {
                c.Parameters.AddWithValue("id", id);
                c.Parameters.AddWithValue("movimiento", movimiento);
                c.Parameters.AddWithValue("precio", precio);
                if (await c.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                    throw new InvalidOperationException("El detalle ya no existe.");
            }
            using (var c = Cmd(@"UPDATE public.movimientos SET precio_total=(SELECT COALESCE(SUM(mm.cantidad*CASE WHEN mm.es_compra_eventual THEN COALESCE(mm.precio_unitario,0) ELSE COALESCE(mat.preciounitario,0) END),0) FROM public.movimientomaterial mm LEFT JOIN public.materiales mat ON mat.id=mm.id_material WHERE mm.id_movimiento=@id) WHERE id_movimiento=@id RETURNING precio_total"))
            {
                c.Parameters.AddWithValue("id", movimiento);
                var total = await c.ExecuteScalarAsync(token).ConfigureAwait(false);
                if (total == null || total == DBNull.Value)
                    throw new InvalidOperationException("El movimiento ya no existe.");
                return Convert.ToDecimal(total);
            }
        }
    }
```

### 4.5. Pruebas Automatizadas Requeridas
En `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`, extender `PrecioEventualActualizaTotalSoloLineaEventualYControlaPermiso` (líneas 581–591):
- Verificar que si otra conexión retiene `SELECT id_movimientomaterial FROM public.movimientomaterial WHERE id_movimientomaterial=@id FOR UPDATE`, la llamada a `ActualizarPrecioEventual.EjecutarAsync` espera el bloqueo en `pg_stat_activity` (`wait_event_type='Lock'` y `query LIKE '%FROM public.movimientomaterial%FOR UPDATE%'`).
- Verificar que un `precio` con más de 2 decimales (`4.567m`) lanza `ArgumentOutOfRangeException` antes de abrir conexión.

### 4.6. Comando de Verificación
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File A:\SteeLog\scripts\tests\inventario-integracion.ps1
```

---

## TAREA 5: Desacoplamiento de `FiltroMaterial.cs` y `ModalSeleccionProveedores.cs`

### 5.1. Diagnóstico Exacto
1. `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\FiltroMaterial.cs`:
   - Importa `using SteeLog.Datos;` e instancia directamente `new CategoriaDatos().GetCategorias()` y `new ComunDatos().ObtenerListaGenerica("proveedores")` en `CargarListas()` llamado desde el constructor `public FiltroMaterial()`.
2. `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\ModalSeleccionProveedores.cs`:
   - Importa `using SteeLog.Datos;` e instancia `new ComunDatos().ObtenerListaGenerica("proveedores")` en `CargarProveedores(List<int> idsSeleccionados)` llamado desde el constructor.
3. En `SteeLog.Application/Inventario/MaterialesConsulta.cs`, `IMaterialesConsulta` y `ConsultarMateriales` ya exponen `CategoriasAsync(CancellationToken)` (con `Role.Galpon`), pero `ProveedoresAsync(int materialId, CancellationToken)` devuelve únicamente los `int` de proveedores asociados a un material específico (y exige `Role.Admin`). Falta la consulta de catálogo general de proveedores (`CatalogoProveedoresAsync`) accesible desde `Role.Galpon` (ya que tanto `Vista_Materiales` como `Vista_ReporteStock` permiten filtrar por proveedor a usuarios con rol `Galpon`).
4. **Impacto Crítico en `ArchitectureTests.cs` y `LegacyDependencies.txt`:**
   - El test `ArchitectureTests.Host_legacy_type_dependencies_match_explicit_retirement_inventory` verifica que ninguna dependencia listada en `A:\SteeLog\SteeLog\SteeLog.Tests\LegacyDependencies.txt` haya sido retirada sin actualizar el archivo (`Assert.Empty(retired)`).
   - Al eliminar `using SteeLog.Datos;` de `FiltroMaterial.cs` y `ModalSeleccionProveedores.cs`, **es obligatorio eliminar las 3 líneas correspondientes de `LegacyDependencies.txt`**:
     - `F8 | SteeLog.Vistas.FiltroMaterial -> SteeLog.Datos.CategoriaDatos`
     - `F8 | SteeLog.Vistas.FiltroMaterial -> SteeLog.Datos.ComunDatos`
     - `F8 | SteeLog.Vistas.ModalSeleccionProveedores -> SteeLog.Datos.ComunDatos`

### 5.2. Archivos Afectados
- **Modificar (Application):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Application\Inventario\MaterialesConsulta.cs`
  - Relativa: `SteeLog/SteeLog.Application/Inventario/MaterialesConsulta.cs`
- **Modificar (Infrastructure):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Inventario\NpgsqlMaterialesConsulta.cs`
  - Relativa: `SteeLog/SteeLog.Infrastructure/Inventario/NpgsqlMaterialesConsulta.cs`
- **Modificar (Presentation):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Presentation\MaterialesPresenter.cs`
  - Relativa: `SteeLog/SteeLog/Presentation/MaterialesPresenter.cs`
- **Modificar (Vistas WinForms):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\FiltroMaterial.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Gestión/FiltroMaterial.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\ModalSeleccionProveedores.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Gestión/ModalSeleccionProveedores.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\Vista_Materiales.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Gestión/Vista_Materiales.cs`
- **Modificar (Inventario de Arquitectura y Pruebas):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\LegacyDependencies.txt`
  - Relativa: `SteeLog/SteeLog.Tests/LegacyDependencies.txt`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`
  - Relativa: `SteeLog/SteeLog.Tests/InventarioIntegrationTests.cs`

### 5.3. Impacto y Dependencias Cruzadas (Archivos Vecinos)
- **Consumidores de `FiltroMaterial`:**
  1. `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\Vista_Materiales.cs` (línea 855): `using (var modalFiltro = new FiltroMaterial(_presenter))` (o `new FiltroMaterial()` que resuelve `new MaterialesPresenter(LocalServices.Current.Executor)` por defecto).
  2. `A:\SteeLog\SteeLog\SteeLog\Vistas\Inventario\Vista_ReporteStock.cs` (línea 609): `using (var modalFiltro = new FiltroMaterial())`. Al conservar el constructor sin parámetros `public FiltroMaterial() : this(null)` que usa `LocalServices.Current.Executor` cuando no está en `Designtime`, `Vista_ReporteStock.cs` y el Diseñador de WinForms siguen funcionando sin necesidad de tocar `Vista_ReporteStock.cs`.
- **Consumidores de `ModalSeleccionProveedores`:**
  1. `A:\SteeLog\SteeLog\SteeLog\Vistas\Gestión\Vista_Materiales.cs` (línea 107): `using (var modal = new ModalSeleccionProveedores(_proveedoresSeleccionadosIds, _presenter))` (conservando `public ModalSeleccionProveedores(List<int> idsIniciales) : this(idsIniciales, null)` por compatibilidad total).
- **Diseñador de WinForms (`FiltroMaterial.Designer.cs` y `ModalSeleccionProveedores.Designer.cs`):**
  - No se tocan los archivos `.Designer.cs`. Los controles Guna (`txtStockDesde`, `txtStockHasta`, `btnLimpiar`, `btnAplicar`, `clbCategorias`, `clbProveedores`, `btnConfirmar`, `btnCancelar`) permanecen intactos.
- **Orden determinista de carga y pre-selección:**
  - En `Vista_Materiales.cs` (línea 857) y `Vista_ReporteStock.cs` (línea 611), el código padre llama a `modalFiltro.CargarEstadoPrevio(...)` **inmediatamente después** de `new FiltroMaterial()` y **antes** de `modalFiltro.ShowDialog()`.
  - Si `FiltroMaterial` carga las listas en el evento `Load` de manera asíncrona (`async void FiltroMaterial_Load`), entonces `CargarEstadoPrevio(...)` llamado antes de `ShowDialog()` encontraría `clbCategorias.Items` vacío si intentara marcar los `CheckedListBox` en ese instante.
  - **Solución exacta para compatibilidad 100% con `CargarEstadoPrevio`:**
    En `FiltroMaterial.CargarEstadoPrevio(...)`, guardar una copia de `categoriasprevias` (`_categoriasPrevias`) y `proveedoresPrevios` (`_proveedoresPrevios`) además de asignar los controles de texto/switches; si `clbCategorias.Items.Count > 0` aplicar los checks inmediatamente, y al finalizar `CargarListasAsync()` volver a aplicar `_categoriasPrevias` y `_proveedoresPrevios` sobre `clbCategorias` y `clbProveedores`! Así funciona tanto si los datos se inyectan sincrónicamente como si se cargan en `OnLoad` asíncronamente.

### 5.4. Especificación de Código (Antes / Después)

#### 5.4.1. En `SteeLog/SteeLog.Application/Inventario/MaterialesConsulta.cs`:

Agregar el DTO `ProveedorMaterialLectura` y el método `CatalogoProveedoresAsync` en `IMaterialesConsulta` y `ConsultarMateriales`:
```csharp
    public sealed class ProveedorMaterialLectura
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }

    public interface IMaterialesConsulta
    {
        Task<IReadOnlyList<MaterialLectura>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token = default);
        Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token = default);
        Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default);
        Task<IReadOnlyList<int>> ProveedoresAsync(int materialId, CancellationToken token = default);
    }
```
Y en `ConsultarMateriales`:
```csharp
        public Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default)
        {
            Authorization.Require(session.Current, Role.Galpon);
            return consulta.CatalogoProveedoresAsync(token);
        }
```

#### 5.4.2. En `SteeLog/SteeLog.Infrastructure/Inventario/NpgsqlMaterialesConsulta.cs`:

Agregar la implementación en `NpgsqlMaterialesConsulta`:
```csharp
        public async Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default)
        {
            var lista = new List<ProveedorMaterialLectura>();
            using (var c = factory.Create())
            {
                await c.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id_proveedores, nombre FROM public.proveedores ORDER BY nombre ASC", c) { CommandTimeout = 15 })
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false))
                        lista.Add(new ProveedorMaterialLectura { Id = r.GetInt32(0), Nombre = r.GetString(1) });
            }
            return lista;
        }
```

#### 5.4.3. En `SteeLog/SteeLog/Presentation/MaterialesPresenter.cs`:

Agregar en `MaterialesPresenter`:
```csharp
        public Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token) =>
            executor.ExecuteAsync<ConsultarMateriales, IReadOnlyList<ProveedorMaterialLectura>>(x => x.CatalogoProveedoresAsync(token));
```

#### 5.4.4. En `SteeLog/SteeLog/Vistas/Gestión/FiltroMaterial.cs`:

Reemplazar el uso de `SteeLog.Datos` (`CategoriaDatos` y `ComunDatos`) por `MaterialesPresenter` y los DTOs de `SteeLog.Core.Application.Inventario`:
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Inventario;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class FiltroMaterial : Form
    {
        private readonly MaterialesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private HashSet<int> _categoriasPrevias = new HashSet<int>();
        private HashSet<int> _proveedoresPrevios = new HashSet<int>();

        public decimal? StockDesde { get; private set; }
        public decimal? StockHasta { get; private set; }
        public List<int> CategoriasSeleccionadas { get; private set; } = new List<int>();
        public List<int> ProveedoresSeleccionados { get; private set; } = new List<int>();
        public bool SoloStockBajo { get; private set; }
        public bool SeLimpioFiltro { get; private set; } = false;

        private sealed class ItemLista
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre ?? string.Empty;
        }

        public FiltroMaterial() : this(null)
        {
        }

        internal FiltroMaterial(MaterialesPresenter presenter)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _presenter = presenter ?? new MaterialesPresenter(LocalServices.Current.Executor);
            FormClosed += (_, __) => _cierre.Cancel();
            Disposed += (_, __) => _cierre.Dispose();
            Shown += FiltroMaterial_Shown;
        }

        private async void FiltroMaterial_Shown(object sender, EventArgs e)
        {
            await CargarListasAsync();
        }

        private async Task CargarListasAsync()
        {
            try
            {
                var categorias = await _presenter.CategoriasAsync(_cierre.Token);
                var proveedores = await _presenter.CatalogoProveedoresAsync(_cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;

                clbCategorias.Items.Clear();
                foreach (var cat in categorias)
                {
                    int idx = clbCategorias.Items.Add(new ItemLista { Id = cat.Id, Nombre = cat.Nombre });
                    if (_categoriasPrevias.Contains(cat.Id))
                        clbCategorias.SetItemChecked(idx, true);
                }

                clbProveedores.Items.Clear();
                foreach (var prov in proveedores)
                {
                    int idx = clbProveedores.Items.Add(new ItemLista { Id = prov.Id, Nombre = prov.Nombre });
                    if (_proveedoresPrevios.Contains(prov.Id))
                        clbProveedores.SetItemChecked(idx, true);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Error al cargar listas para filtrar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CargarEstadoPrevio(decimal? desde, decimal? hasta, List<int> categoriasprevias, List<int> proveedoresPrevios, bool stockBajo)
        {
            if (desde.HasValue) txtStockDesde.Text = desde.Value.ToString("0.##");
            if (hasta.HasValue) txtStockHasta.Text = hasta.Value.ToString("0.##");
            tglStockBajo.Checked = stockBajo;

            _categoriasPrevias = categoriasprevias != null ? new HashSet<int>(categoriasprevias) : new HashSet<int>();
            _proveedoresPrevios = proveedoresPrevios != null ? new HashSet<int>(proveedoresPrevios) : new HashSet<int>();

            for (int i = 0; i < clbCategorias.Items.Count; i++)
            {
                var item = (ItemLista)clbCategorias.Items[i];
                clbCategorias.SetItemChecked(i, _categoriasPrevias.Contains(item.Id));
            }

            for (int i = 0; i < clbProveedores.Items.Count; i++)
            {
                var item = (ItemLista)clbProveedores.Items[i];
                clbProveedores.SetItemChecked(i, _proveedoresPrevios.Contains(item.Id));
            }
        }

        private void btnAplicar_Click(object sender, EventArgs e)
        {
            decimal d;
            if (decimal.TryParse(txtStockDesde.Text, out d)) StockDesde = d;
            else StockDesde = null;

            decimal h;
            if (decimal.TryParse(txtStockHasta.Text, out h)) StockHasta = h;
            else StockHasta = null;

            if (StockDesde.HasValue && StockHasta.HasValue && StockDesde > StockHasta)
            {
                MessageBox.Show("El stock 'Desde' no puede ser mayor que el stock 'Hasta'.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SoloStockBajo = tglStockBajo.Checked;

            CategoriasSeleccionadas.Clear();
            foreach (ItemLista item in clbCategorias.CheckedItems)
                CategoriasSeleccionadas.Add(item.Id);

            ProveedoresSeleccionados.Clear();
            foreach (ItemLista item in clbProveedores.CheckedItems)
                ProveedoresSeleccionados.Add(item.Id);

            SeLimpioFiltro = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            StockDesde = null;
            StockHasta = null;
            CategoriasSeleccionadas.Clear();
            ProveedoresSeleccionados.Clear();
            SoloStockBajo = false;

            SeLimpioFiltro = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void txtNumeros_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ',') && (e.KeyChar != '.'))
                e.Handled = true;
        }
    }
}
```

#### 5.4.5. En `SteeLog/SteeLog/Vistas/Gestión/ModalSeleccionProveedores.cs`:

Reemplazar el uso de `SteeLog.Datos` (`ComunDatos`) por `MaterialesPresenter`:
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;

namespace SteeLog.Vistas
{
    public partial class ModalSeleccionProveedores : Form
    {
        private readonly MaterialesPresenter _presenter;
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private readonly HashSet<int> _idsIniciales;

        public List<int> ProveedoresSeleccionadosIds { get; private set; } = new List<int>();

        private sealed class ItemProveedor
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public override string ToString() => Nombre ?? string.Empty;
        }

        public ModalSeleccionProveedores(List<int> idsIniciales) : this(idsIniciales, null)
        {
        }

        internal ModalSeleccionProveedores(List<int> idsIniciales, MaterialesPresenter presenter)
        {
            InitializeComponent();
            _idsIniciales = idsIniciales != null ? new HashSet<int>(idsIniciales) : new HashSet<int>();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _presenter = presenter ?? new MaterialesPresenter(LocalServices.Current.Executor);
            FormClosed += (_, __) => _cierre.Cancel();
            Disposed += (_, __) => _cierre.Dispose();
            Shown += ModalSeleccionProveedores_Shown;
        }

        private async void ModalSeleccionProveedores_Shown(object sender, EventArgs e)
        {
            await CargarProveedoresAsync();
        }

        private async Task CargarProveedoresAsync()
        {
            try
            {
                var listaTodos = await _presenter.CatalogoProveedoresAsync(_cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;

                clbProveedores.Items.Clear();
                foreach (var prov in listaTodos)
                {
                    int index = clbProveedores.Items.Add(new ItemProveedor { Id = prov.Id, Nombre = prov.Nombre });
                    if (_idsIniciales.Contains(prov.Id))
                        clbProveedores.SetItemChecked(index, true);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Error al cargar la lista de proveedores: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            ProveedoresSeleccionadosIds.Clear();
            foreach (ItemProveedor item in clbProveedores.CheckedItems)
                ProveedoresSeleccionadosIds.Add(item.Id);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
```

#### 5.4.6. En `SteeLog/SteeLog/Vistas/Gestión/Vista_Materiales.cs`:
- Línea 107: pasar `_presenter`: `using (var modal = new ModalSeleccionProveedores(_proveedoresSeleccionadosIds, _presenter))`
- Línea 855: pasar `_presenter`: `using (var modalFiltro = new FiltroMaterial(_presenter))`

#### 5.4.7. En `SteeLog/SteeLog.Tests/LegacyDependencies.txt`:
Eliminar exactamente estas 3 líneas (actualmente en las líneas 188, 189 y 192):
```text
F8 | SteeLog.Vistas.FiltroMaterial -> SteeLog.Datos.CategoriaDatos
F8 | SteeLog.Vistas.FiltroMaterial -> SteeLog.Datos.ComunDatos
F8 | SteeLog.Vistas.ModalSeleccionProveedores -> SteeLog.Datos.ComunDatos
```

### 5.5. Pruebas Automatizadas Requeridas
1. En `A:\SteeLog\SteeLog\SteeLog.Tests\ArchitectureTests.cs`:
   - `Host_legacy_type_dependencies_match_explicit_retirement_inventory` validará automáticamente que `FiltroMaterial` y `ModalSeleccionProveedores` ya no dependen de `SteeLog.Datos.*`.
2. En `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`:
   - En `ConsultasMaterialesFiltranYCalculanPrioridadEnConexionIndependiente` (línea 526):
     - Agregar `Assert.Contains(await new ConsultarMateriales(Actor(Role.Galpon), new NpgsqlMaterialesConsulta(Factory())).CatalogoProveedoresAsync(), x => x.Id == 1 && !string.IsNullOrWhiteSpace(x.Nombre));`

### 5.6. Comandos de Verificación
```powershell
dotnet test A:\SteeLog\SteeLog\SteeLog.Tests\SteeLog.Tests.csproj -c Release --filter "FullyQualifiedName~ArchitectureTests"
powershell -NoProfile -ExecutionPolicy Bypass -File A:\SteeLog\scripts\tests\inventario-integracion.ps1
```

---

## TAREA 6: Eliminación de Recortes Huérfanos con Stock 0 en `ModalRecortes.cs` y Movimientos

### 6.1. Diagnóstico Exacto
1. En `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\ModalRecortes.cs` (línea 211):
   - Al pulsar `btnCrearNuevo`, el modal ejecuta `int id = await _presenter.AgregarAsync(_idMaterial, ancho, altura, medida, 0, null, _cierre.Token);`.
   - Esto inserta inmediatamente una fila en `public.recortes` con `cantidad = 0` y `estado = NULL` en una transacción independiente **antes** de que el usuario confirme el movimiento.
   - Si el usuario luego quita el ítem del carrito, limpia el formulario, cierra la ventana, descarta el borrador o si el guardado del remito falla (ej. remito duplicado), el registro con `cantidad = 0` queda huérfano permanentemente en la base de datos.
   - Adicionalmente, en las líneas 213–215 de `ModalRecortes.cs`, tras crear el recorte con `cantidad = 0`, llama a `_presenter.ListarAsync(...)`, cuya consulta SQL (`NpgsqlRecortesConsulta.ListarAsync`) filtra `WHERE r.cantidad > 0`, por lo que `creado` siempre resulta `null`.
2. **Solución Arquitectónica Completa (Transaccional y Sin Huérfanos):**
   - **En `ModalRecortes.cs` (`btnCrearNuevo_Click`):**
     - **No persistir en base de datos** al hacer clic en `btnCrearNuevo`.
     - Validar las dimensiones en memoria mediante el constructor de dominio `var valido = new SteeLog.Domain.Inventario.Recorte(0, _idMaterial, ancho, altura, medida, 0m, null);` (aprovechando la invariante unificada de la Tarea 2).
     - Si en `_listaOriginal` ya existe un recorte cargado con el mismo `(Ancho, Altura, Medida)`, seleccionarlo (`RecorteSeleccionado = existente`).
     - Si no existe aún, instanciar un `SteeLog.Modelos.Recorte` transitorio con `Id = 0`, `IdMaterial = _idMaterial`, `Ancho = valido.Ancho`, `Altura = valido.Altura`, `Medida = valido.Medida`, `Cantidad = 0`, `Estado = null`, asignarlo a `RecorteSeleccionado` y cerrar el modal con `DialogResult.OK`.
   - **En el Carrito y Borrador (`MovimientoMaterial.cs`, `MovimientoDraft.cs`, `MovimientoDraftService.cs`, `Vista_Movimientos.cs`, `Vista_Movimientos.Carrito.cs`, `Vista_Movimientos.Borrador.cs`):**
     - Agregar `AnchoRecorte` (`decimal?`) y `AlturaRecorte` (`decimal?`) a `SteeLog.Modelos.MovimientoMaterial` y `SteeLog.Core.Application.Borradores.MovimientoMaterialDraft` (junto al `MedidaRecorte` ya existente).
     - Cuando el usuario selecciona un recorte nuevo (`Id == 0`) en `ModalRecortes`, conservar en el carrito `EsRecorte = true`, `IdRecorte = null` (o `0`), `AnchoRecorte`, `AlturaRecorte` y `MedidaRecorte`.
     - Permitir que el borrador JSON (`movimiento_draft.json`) serialice y restaure `AnchoRecorte`, `AlturaRecorte` y `MedidaRecorte` sin tocar PostgreSQL.
   - **En la Transacción ACID del Movimiento (`ContratosMovimiento.cs`, `Movimiento.cs`, `OperacionesMovimiento.cs`, `NpgsqlMovimientos.cs`):**
     - Agregar `AnchoRecorte` (`decimal?`) y `AlturaRecorte` (`decimal?`) a `SteeLog.Core.Application.Movimientos.LineaMovimiento`.
     - Agregar `Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken token = default);` en `IMovimientosEscritura` e implementarlo en `NpgsqlMovimientos` (y en el decorador de test `FallarDespuesTraslado` de `InventarioIntegrationTests.cs`).
     - En `MotorMovimiento.EjecutarAsync` (`OperacionesMovimiento.cs`):
       - En la validación previa (`Validar`), si una línea tiene `EsRecorte && (!x.RecorteId.HasValue || x.RecorteId.Value <= 0)`, validar sus dimensiones con `new SteeLog.Domain.Inventario.Recorte(0, x.MaterialId ?? 0, x.AnchoRecorte ?? 0m, x.AlturaRecorte ?? 0m, x.MedidaRecorte ?? 0m, 0m, null)` y validar la estructura de la línea.
       - Dentro de `uow.BeginAsync(token)`, **después** de bloquear los materiales padre (`inventario.BloquearAsync(idsMaterial, token)`) y resolver el factor de stock (`factorNuevo`):
         - Si alguna línea tiene `EsRecorte && (!x.RecorteId.HasValue || x.RecorteId.Value <= 0)`:
           - Verificar que `factorNuevo > 0` (si `factorNuevo <= 0`, lanzar `InvalidOperationException("Stock insuficiente.")` sin insertar nada).
           - Llamar a `x.RecorteId = await repo.ResolverRecorteAsync(x.MaterialId.Value, spec.Ancho, spec.Altura, spec.Medida, token).ConfigureAwait(false);`, que dentro de la misma transacción `NpgsqlUnitOfWork` busca un recorte coincidente (`FOR UPDATE`) o lo inserta con `cantidad = 0`, tras lo cual el flujo normal de `MotorMovimiento` bloquea `idsRecorte`, suma `linea.Cantidad * factorNuevo` (> 0) y ejecuta `repo.GuardarRecorteAsync(par.Key, par.Value, token)`.
         - De este modo, el recorte nuevo se crea y recibe su stock positivo **en la misma transacción ACID del movimiento**. Si el movimiento se cancela o falla por cualquier motivo (ej. remito duplicado), el `Rollback` revierte también la fila en `public.recortes`, garantizando **cero recortes huérfanos con stock 0**.
   - **Compatibilidad con `AgregarRecorte`:**
     - El caso de uso `AgregarRecorte` (`SteeLog.Application/Inventario/Recortes.cs`) mantiene su contrato actual sin cambios, preservando el test existente `AltaRecorteModalConCeroEstadoNuloYGalponEsCompatible`.

### 6.2. Archivos Afectados
- **Modificar (Host UI / Modelos):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\ModalRecortes.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Movimientos/ModalRecortes.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Modelos\MovimientoMaterial.cs`
  - Relativa: `SteeLog/SteeLog/Modelos/MovimientoMaterial.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\MovimientoDraftService.cs`
  - Relativa: `SteeLog/SteeLog/MovimientoDraftService.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\Vista_Movimientos.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Movimientos/Vista_Movimientos.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\Vista_Movimientos.Carrito.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Movimientos/Vista_Movimientos.Carrito.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog\Vistas\Movimientos\Vista_Movimientos.Borrador.cs`
  - Relativa: `SteeLog/SteeLog/Vistas/Movimientos/Vista_Movimientos.Borrador.cs`
- **Modificar (Application / Infrastructure):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Application\Borradores\MovimientoDraft.cs`
  - Relativa: `SteeLog/SteeLog.Application/Borradores/MovimientoDraft.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Application\Movimientos\ContratosMovimiento.cs`
  - Relativa: `SteeLog/SteeLog.Application/Movimientos/ContratosMovimiento.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Application\Movimientos\OperacionesMovimiento.cs`
  - Relativa: `SteeLog/SteeLog.Application/Movimientos/OperacionesMovimiento.cs`
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Infrastructure\Movimientos\NpgsqlMovimientos.cs`
  - Relativa: `SteeLog/SteeLog.Infrastructure/Movimientos/NpgsqlMovimientos.cs`
- **Modificar (Pruebas):**
  - Absoluta: `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`
  - Relativa: `SteeLog/SteeLog.Tests/InventarioIntegrationTests.cs`

### 6.3. Impacto y Dependencias Cruzadas (Archivos Vecinos)
- `A:\SteeLog\SteeLog\SteeLog.Tests\BorradorMovimientoTests.cs`: Serializa y deserializa `MovimientoDraft`. Al añadir miembros `[DataMember]` opcionales (`AnchoRecorte`, `AlturaRecorte`), `DataContractJsonSerializer` mantiene compatibilidad total hacia atrás y hacia adelante.
- `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`:
  - La clase interna `FallarDespuesTraslado : IMovimientosEscritura` (línea 163) implementa `IMovimientosEscritura`, por lo que debe delegar `ResolverRecorteAsync` a `inner`.
- `A:\SteeLog\SteeLog\SteeLog.Domain\Movimientos\Movimiento.cs`: `LineaMovimiento` de dominio sigue exigiendo `recorteId.Value > 0` una vez resuelto el recorte dentro de `MotorMovimiento.EjecutarAsync`.

### 6.4. Especificación de Código (Antes / Después)

#### 6.4.1. En `SteeLog/SteeLog/Vistas/Movimientos/ModalRecortes.cs`:

**ANTES (`btnCrearNuevo_Click`, líneas 185–227):**
```csharp
        private async void btnCrearNuevo_Click(object sender, EventArgs e)
        {
            if ( _esRetiro )
            {
                MessageBox.Show("No se puede crear un recorte nuevo durante un retiro de almacén.", "Acción no permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string strAncho = txtNuevoAncho.Text.Replace(".", ",");
            string strAlto = txtNuevoAlto.Text.Replace(".", ",");
            string strMedida = txtNuevaMedida.Text.Replace(".", ",");

            decimal.TryParse(strAncho, out decimal ancho);
            decimal.TryParse(strAlto, out decimal altura);
            decimal.TryParse(strMedida, out decimal medida);

            if (medida <= 0 && (ancho <= 0 || altura <= 0))
            {
                MessageBox.Show("Por favor, ingrese una medida lineal válida o bien las dimensiones de ancho y alto.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnCrearNuevo.Enabled = false;
                int id = await _presenter.AgregarAsync(_idMaterial, ancho, altura, medida, 0, null, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                var lista = await _presenter.ListarAsync(_idMaterial, ancho, altura, medida, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                var creado = lista.FirstOrDefault(x => x.Id == id);
                RecorteSeleccionado = creado != null ? Mapear(creado) : new Recorte { Id = id, IdMaterial = _idMaterial, Ancho = ancho, Altura = altura, Medida = medida, Cantidad = 0, Estado = null };
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show("Error al crear el recorte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) btnCrearNuevo.Enabled = true;
            }
        }
```

**DESPUÉS (`btnCrearNuevo_Click`):**
```csharp
        private void btnCrearNuevo_Click(object sender, EventArgs e)
        {
            if (_esRetiro)
            {
                MessageBox.Show("No se puede crear un recorte nuevo durante un retiro de almacén.", "Acción no permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string strAncho = txtNuevoAncho.Text.Replace(".", ",");
            string strAlto = txtNuevoAlto.Text.Replace(".", ",");
            string strMedida = txtNuevaMedida.Text.Replace(".", ",");

            decimal.TryParse(strAncho, out decimal ancho);
            decimal.TryParse(strAlto, out decimal altura);
            decimal.TryParse(strMedida, out decimal medida);

            if (medida <= 0 && (ancho <= 0 || altura <= 0))
            {
                MessageBox.Show("Por favor, ingrese una medida lineal válida o bien las dimensiones de ancho y alto.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var validado = new SteeLog.Domain.Inventario.Recorte(0, _idMaterial, ancho, altura, medida, 0m, null);
                var existente = _listaOriginal?.FirstOrDefault(r =>
                    r.Ancho == validado.Ancho &&
                    r.Altura == validado.Altura &&
                    r.Medida == validado.Medida);

                RecorteSeleccionado = existente ?? new Recorte
                {
                    Id = 0,
                    IdMaterial = _idMaterial,
                    Ancho = validado.Ancho,
                    Altura = validado.Altura,
                    Medida = validado.Medida,
                    Cantidad = 0,
                    Estado = null
                };
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show("Error en las dimensiones del recorte: " + ex.Message, "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
```

#### 6.4.2. En `SteeLog/SteeLog.Application/Movimientos/ContratosMovimiento.cs`:
En `LineaMovimiento`:
```csharp
    public sealed class LineaMovimiento
    {
        public int? MaterialId { get; set; }
        public string MaterialEventual { get; set; }
        public decimal Cantidad { get; set; }
        public bool EsRecorte { get; set; }
        public int? RecorteId { get; set; }
        public decimal? AnchoRecorte { get; set; }
        public decimal? AlturaRecorte { get; set; }
        public decimal? MedidaRecorte { get; set; }
        public bool EsInsumo { get; set; }
        public int? InsumoId { get; set; }
        public bool EsCompraEventual { get; set; }
        public bool CompraEventual { get => EsCompraEventual; set => EsCompraEventual = value; }
        public decimal PrecioUnitario { get; set; }
    }
```
Y en `IMovimientosEscritura`:
```csharp
        Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken token = default);
```

#### 6.4.3. En `SteeLog/SteeLog.Infrastructure/Movimientos/NpgsqlMovimientos.cs`:
Agregar en `NpgsqlMovimientos`:
```csharp
        public async Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken token = default)
        {
            using (var c = Cmd("SELECT id FROM public.recortes WHERE id_material=@m AND COALESCE(ancho,0)=@a AND COALESCE(altura,0)=@h AND medida=@med ORDER BY id LIMIT 1 FOR UPDATE"))
            {
                c.Parameters.AddWithValue("m", materialId);
                c.Parameters.AddWithValue("a", ancho);
                c.Parameters.AddWithValue("h", altura);
                c.Parameters.AddWithValue("med", medida);
                var id = await c.ExecuteScalarAsync(token).ConfigureAwait(false);
                if (id != null && id != DBNull.Value) return Convert.ToInt32(id);
            }
            using (var c = Cmd("INSERT INTO public.recortes (id_material, ancho, altura, medida, cantidad, estado) VALUES (@m, @a, @h, @med, 0, NULL) RETURNING id"))
            {
                c.Parameters.AddWithValue("m", materialId);
                c.Parameters.AddWithValue("a", ancho > 0 ? (object)ancho : DBNull.Value);
                c.Parameters.AddWithValue("h", altura > 0 ? (object)altura : DBNull.Value);
                c.Parameters.AddWithValue("med", medida);
                return Convert.ToInt32(await c.ExecuteScalarAsync(token).ConfigureAwait(false));
            }
        }
```

#### 6.4.4. En `SteeLog/SteeLog.Application/Movimientos/OperacionesMovimiento.cs`:
1. En `Dominio(LineaMovimiento x)` / `Validar(MovimientoSolicitud s)`:
   - Si `x.EsRecorte && (!x.RecorteId.HasValue || x.RecorteId.Value <= 0)`:
     - En `Validar`: validar `if (!x.MaterialId.HasValue || x.MaterialId.Value <= 0 || x.EsInsumo || x.EsCompraEventual) throw new ArgumentException("El recorte es inválido.");`, validar dimensiones con `new SteeLog.Domain.Inventario.Recorte(0, x.MaterialId.Value, x.AnchoRecorte ?? 0m, x.AlturaRecorte ?? 0m, x.MedidaRecorte ?? 0m, 0m, null);` y validar cantidad/precio con `new SteeLog.Domain.Movimientos.LineaMovimiento(x.MaterialId, 1, true, false, false, null, x.Cantidad, x.PrecioUnitario);`.
2. En `MotorMovimiento.EjecutarAsync`:
   - Dentro de `uow.BeginAsync(token)`, una vez bloqueados `saldosMaterial = await inventario.BloquearAsync(idsMaterial, token)` y obtenido `factorNuevo`:
     ```csharp
     foreach (var linea in solicitud.Lineas.Where(x => x.EsRecorte && (!x.RecorteId.HasValue || x.RecorteId.Value <= 0)))
     {
         if (!saldosMaterial.ContainsKey(linea.MaterialId.Value))
             throw new InvalidOperationException("El material indicado no existe.");
         if (factorNuevo <= 0)
             throw new InvalidOperationException("Stock insuficiente.");
         var spec = new SteeLog.Domain.Inventario.Recorte(0, linea.MaterialId.Value, linea.AnchoRecorte ?? 0m, linea.AlturaRecorte ?? 0m, linea.MedidaRecorte ?? 0m, 0m, null);
         linea.RecorteId = await repo.ResolverRecorteAsync(spec.MaterialId, spec.Ancho, spec.Altura, spec.Medida, token).ConfigureAwait(false);
     }
     ```
   - A continuación se calcula `idsRecorte` (que ya incluye los `RecorteId` recién resueltos), se bloquean en orden con `repo.ObtenerMaterialesRecortesAsync` y `repo.BloquearRecortesAsync`, se les aplica el delta positivo (`linea.Cantidad * factorNuevo`) y se persisten con `repo.GuardarRecorteAsync`.

#### 6.4.5. En `SteeLog/SteeLog/Modelos/MovimientoMaterial.cs`, `MovimientoDraft.cs`, `MovimientoDraftService.cs`, `Vista_Movimientos.cs`, `Vista_Movimientos.Carrito.cs` y `Vista_Movimientos.Borrador.cs`:
- En `MovimientoMaterial.cs`: agregar `public decimal? AnchoRecorte { get; set; }` y `public decimal? AlturaRecorte { get; set; }`.
- En `MovimientoMaterialDraft` (`MovimientoDraft.cs`): agregar `[DataMember] public decimal? AnchoRecorte { get; set; }` y `[DataMember] public decimal? AlturaRecorte { get; set; }`.
- En `MovimientoDraftService.cs` y `Vista_Movimientos.Borrador.cs`: mapear `AnchoRecorte` y `AlturaRecorte` en ambos sentidos.
- En `Vista_Movimientos.cs`: agregar `private decimal _anchoRecorteSeleccionado = 0;` y `private decimal _alturaRecorteSeleccionado = 0;` y reiniciarlos a `0` junto con `_idRecorteSeleccionado` y `_medidaRecorteSeleccionado`. En `GuardarMovimiento_Click` (línea 861), incluir `AnchoRecorte = x.AnchoRecorte`, `AlturaRecorte = x.AlturaRecorte`, `MedidaRecorte = x.MedidaRecorte` al construir cada `SteeLog.Core.Application.Movimientos.LineaMovimiento`.
- En `Vista_Movimientos.Carrito.cs`:
  - Reconocer que hay recorte seleccionado cuando `_idRecorteSeleccionado > 0 || _medidaRecorteSeleccionado > 0 || (_anchoRecorteSeleccionado > 0 && _alturaRecorteSeleccionado > 0)`.
  - Al volver de `ModalRecortes` con `DialogResult.OK`: asignar `_idRecorteSeleccionado = modal.RecorteSeleccionado.Id`, `_anchoRecorteSeleccionado = modal.RecorteSeleccionado.Ancho`, `_alturaRecorteSeleccionado = modal.RecorteSeleccionado.Altura`, `_medidaRecorteSeleccionado = modal.RecorteSeleccionado.Medida`.
  - Si `_idRecorteSeleccionado == 0` (recorte nuevo pendiente de alta en la transacción del movimiento):
    - Detectar duplicados en el carrito comparando `m.EsRecorte && (!m.IdRecorte.HasValue || m.IdRecorte == 0) && m.id_material == idMaterial && m.AnchoRecorte == _anchoRecorteSeleccionado && m.AlturaRecorte == _alturaRecorteSeleccionado && m.MedidaRecorte == _medidaRecorteSeleccionado`.
    - Asignar `IdRecorte = _idRecorteSeleccionado > 0 ? (int?)_idRecorteSeleccionado : null`, `AnchoRecorte = _anchoRecorteSeleccionado`, `AlturaRecorte = _alturaRecorteSeleccionado`, `MedidaRecorte = _medidaRecorteSeleccionado`.

### 6.5. Pruebas Automatizadas Requeridas
En `A:\SteeLog\SteeLog\SteeLog.Tests\InventarioIntegrationTests.cs`:
1. Actualizar `FallarDespuesTraslado` (línea 163) agregando:
   `public Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken t = default) => inner.ResolverRecorteAsync(materialId, ancho, altura, medida, t);`
2. Agregar un nuevo test de integración `[Fact]`:
   - `MovimientoIngresoConRecorteNuevoLoCreaEnTransaccionYRollbackNoDejaHuerfanoStockCero`:
     - Caso 1 (Éxito en ingreso/devolución a almacén, `tipo = 2` o `Origen = OBRA, Destino = UBICACION`): envía una `LineaMovimiento` con `MaterialId = material, EsRecorte = true, RecorteId = null, AnchoRecorte = 1.5m, AlturaRecorte = 2.5m, MedidaRecorte = 0m, Cantidad = 3m`. Verifica que el recorte se crea en `public.recortes` directamente con `cantidad = 3m` y que existen `0` filas con `cantidad = 0` para ese material.
     - Caso 2 (Reutilización de recorte coincidente): un segundo ingreso con las mismas dimensiones (`1.5m x 2.5m`, `Cantidad = 2m`) reutiliza la misma fila en `public.recortes` y deja `cantidad = 5m`.
     - Caso 3 (Rollback por fallo / remito duplicado): intenta registrar un movimiento con un recorte nuevo (`MedidaRecorte = 4.25m`) pero con `nroremito` duplicado. Verifica que lanza `InvalidOperationException` y que `SELECT count(*) FROM public.recortes WHERE id_material=@id AND medida=4.25` es **`0`** (ningún recorte huérfano con stock `0`).
     - Caso 4 (Rechazo en salida): intenta registrar una salida (`tipo = 7`) con `EsRecorte = true, RecorteId = null, MedidaRecorte = 1.75m`. Verifica que lanza `InvalidOperationException("Stock insuficiente.")` y no deja ningún recorte en `public.recortes`.

### 6.6. Comandos de Verificación
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File A:\SteeLog\scripts\verificar.ps1 -LegacyLocal
powershell -NoProfile -ExecutionPolicy Bypass -File A:\SteeLog\scripts\tests\inventario-integracion.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File A:\SteeLog\scripts\tests\ordenes-integracion.ps1
```

---

## Protocolo de Ejecución para Subagentes (`gemini-3.8-flash-high`)

1. **Aislamiento de Fase 5 (`Conteos`):** Ningún subagente debe tocar archivos dentro de `SteeLog.*/Conteos/`.
2. **Orden Recomendado de Subagentes:**
   - **Lote A (Paralelizable — Dominio y PDF aislado):** Tareas 1, 2 y 3 (archivos disjuntos: `OrdenesPendientesPDF.cs`, `Recorte.cs`, `EstadosOrden.cs` y sus respectivos archivos de test unitario `RemitoPdfTests.cs`, `MaterialRecorteTests.cs`, `OrdenCompraDomainTests.cs`).
   - **Lote B (Secuencial — Persistencia, UI y Pruebas de Integración):**
     1. Tarea 4 (`NpgsqlMovimientoAuxiliar.cs` + `GestionMovimientoAuxiliar.cs`).
     2. Tarea 5 (`MaterialesConsulta.cs` + `NpgsqlMaterialesConsulta.cs` + `MaterialesPresenter.cs` + `FiltroMaterial.cs` + `ModalSeleccionProveedores.cs` + `Vista_Materiales.cs` + `LegacyDependencies.txt`).
     3. Tarea 6 (`ModalRecortes.cs` + `Vista_Movimientos*.cs` + `MovimientoDraft*.cs` + `ContratosMovimiento.cs` + `OperacionesMovimiento.cs` + `NpgsqlMovimientos.cs` + `InventarioIntegrationTests.cs`).
3. **Criterio de Cierre Global:**
   - Compilación limpia (`0 Errores`) en `scripts/verificar.ps1 -LegacyLocal`.
   - `100%` de tests unitarios y de arquitectura pasando (`SteeLog.Tests`).
   - `100%` de tests de integración contra PostgreSQL efímero pasando (`inventario-integracion.ps1` y `ordenes-integracion.ps1`).
