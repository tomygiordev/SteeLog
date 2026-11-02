# Recursos de identidad SteeLog

## Revisión 2 · 2026-11-02

Esta revisión reemplaza los recursos de la primera versión por pedido del usuario: el arte anterior era demasiado básico. Los tres PNG de origen se generaron con la herramienta integrada `image_gen`, sin CLI, API externa ni claves configuradas por el proyecto. Se mantienen los mismos nombres y consumidores para no romper la carga de recursos WinForms, las cabeceras PDF ni el icono del ejecutable.

### Dirección visual y uso

Lectura: identidad industrial de una aplicación de inventario y trazabilidad para construcción, destinada a operadores y a su presentación académica. Energía 2 en la marca, ritmo 1 y movimiento 1 en la interfaz existente; no se rediseñan los formularios.

- **Motivo:** dos perfiles plegados encastrados forman una S con un pasaje central. Representan material organizado y continuidad de movimientos, en lugar del apilado de vigas de la primera versión.
- **Color:** verde esmeralda y retorno verde claro identifican las caras del pliegue; grafito o blanco distinguen los fondos de uso. No se agrega otra paleta a los formularios. Los valores del prompt son una guía, no una garantía de color plano exacto en un raster generado.
- **Tipografía:** el wordmark industrial de terminales biseladas repite el gesto del perfil sin sacrificar la lectura de `SteeLog`. No se cambia la tipografía de controles o reportes.
- **Composición:** marca horizontal con un único símbolo y espacio de separación para la barra lateral, login y cabeceras. No se agregan eslóganes, sellos, métricas, degradados decorativos de interfaz ni animaciones.
- `steelog-logo-dark.png`: wordmark grafito sobre fondo transparente para papel blanco.
- `steelog-logo-light.png`: wordmark claro sobre fondo transparente para login y barra lateral.
- `steelog-symbol.png`: símbolo aislado sobre lienzo cuadrado transparente.
- `steelog-background.png`: reducción técnica a 256 × 256 del símbolo, conservando alfa; panel de inicio existente.
- `steelog.ico`: derivación técnica del mismo símbolo con entradas PNG RGBA de 16, 24, 32, 48, 64, 128 y 256 px.

Solo se normalizaron márgenes de los lockups y se derivaron tamaños/formato: recorte al límite de alfa > 16 más 24 px transparentes por lado, sin redibujar ni recolorear. La derivación del fondo y del ICO usa interpolación bicúbica con alfa de System.Drawing. La imagen no es un SVG ni una identidad vectorial editable.

### Origen de la revisión 2

Los originales quedan en el directorio de generación de Codex `01a19d79-eed5-7ed1-b41b-344046c04b72`. El proyecto consume únicamente los archivos finales presentes en esta carpeta, no depende de ese directorio.

| Paso | Archivo generado |
| --- | --- |
| 1 | `exec-ec451744-ca6f-45c7-8b52-84c692b06111.png` |
| 2, origen dark | `exec-0c4b78d2-ce5e-4df5-9ce8-76013f5af15b.png` |
| 3, origen light | `exec-1518b3cb-2207-4e9b-b4f1-b369182b64e5.png` |
| 4, símbolo final | `exec-c095aacb-9c71-4731-b97b-9b004a7cc6c4.png` |

### Prompts exactos de la revisión 2

Cada llamada solicitó `transparent_background: true`. Las referencias se proporcionaron mediante `referenced_image_paths` al original del paso indicado.

### 1. Generación de la nueva identidad

```text
Use case: logo-brand.
Asset type: final production horizontal logo for SteeLog, independent academic desktop inventory and material-traceability software for steel construction.
Primary request: create a distinctive, art-directed second-generation identity, not generic clip art. A compact memorable monogram on the left: two interlocking folded steel-channel silhouettes form an abstract S with a deliberate open passage through the middle, suggesting a material moving through an organized stock system. Show the folds through two clean solid planes, not an I-beam stack, cube, chain link, hexagon, gear or warehouse. The silhouette must be robust at 24 px and feel engineered, precise and individual.
Wordmark: exact text "SteeLog", capital S and L only, to the right. A strong condensed industrial humanist sans, carefully spaced, medium-bold rather than chunky. Subtle chamfered terminals echo the folded profile, but completely readable; no stencil gaps in letters, no slogan.
Color palette: main symbol deep emerald #279A68, a small bright emerald #54C591 return-fold as purposeful directional accent; wordmark solid graphite #203137. Flat solid colors only.
Composition: a single tightly composed horizontal lockup, symbol about the cap height plus 20%, generous but not excessive separation, wordmark dominant width. Wide 3.8:1 occupied-art ratio, modest even transparent outer margin.
Style: premium industrial design studio identity, crisp flat vector-like raster. Genuinely transparent background.
Avoid: perspective, extruded 3D, mockups, texture, grain, gradients, outlines, glow, shadow, decorative grids, arrows, extra text, WP letters, presentation boards, alternatives. This must be a fresh identity, not the common stacked I-beams S logo.
```

### 2. Refinamiento del arte oscuro (referencia: paso 1)

```text
Use case: logo-brand. Refine the attached SteeLog logo into a final flat identity. Keep the exact folded S symbol silhouette, its open diagonal passage, horizontal composition, exact spelling "SteeLog" and chamfered industrial wordmark. Change ONLY the surface finish: make all main symbol planes uniform solid emerald #279A68 and the two return-fold ends uniform solid mint #54C591, with crisp smooth antialiased edges. Wordmark must be uniformly solid graphite #203137. Remove every trace of gradients, illumination, grain, roughness, glow, bevel, shadow or outline. Transparent background including the passage through the S and all counters. The result must look like flat printed two-color identity artwork, not a rendered object. One logo only, no sheet and no extra text.
```

### 3. Variante clara (referencia: paso 2)

```text
Use case: logo-brand. Asset: light-wordmark production variant for a dark desktop sidebar. Use this exact SteeLog logo as the only reference. Preserve the folded emerald S symbol and its mint return-fold ends exactly, preserve all wordmark contours, spelling "SteeLog", spacing, proportions and composition. Change ONLY the graphite wordmark to near-white #F1F5F3. Keep the green symbol unchanged. Genuinely transparent background, no dark rectangle or presentation surface. No glow, shadow, outline or added content. One final horizontal lockup only.
```

### 4. Símbolo aislado (referencia: paso 2)

```text
Use case: logo-brand. Create the isolated app-symbol variant of the reference logo. Extract ONLY the green folded S monogram on the left. Keep its distinctive interlocking channel silhouette, continuous diagonal negative-space passage, emerald main planes and lighter mint end folds. Remove all wordmark letters completely. Place the isolated symbol centered within a square canvas with 10% clear transparent margin on every side. Clean precision-cut smooth edges, no stray marks, no lines, no distressing, no shadows or background. Genuine transparent alpha. This is the final production Windows application symbol and must be readable when reduced to 24 px. Do not invent a different S or a different fold.
```

### Verificación y límites

`Tests/Branding/Run.ps1` compila en un TEMP único por defecto. Permite `-Configuration Debug|Release` y `-OutputDirectory <ruta-propia>` para evidencias aisladas. El harness valida presencia, dimensiones, transparencia, contenido, iconos, seis cabeceras PDF y un Kardex, y captura los formularios offscreen retirando antes el handler Shown que consulta actualización y datos. No ejecuta Program.Main, autenticación, bases reales ni publicación. La generación no es determinista; los prompts documentan procedencia e intención, no reproducción píxel a píxel.

Se preservan los identificadores técnicos `WP_Inventario`, la carpeta/solución histórica, nombres de recursos compatibles, DLL de terceros y PFX. No representan la identidad visible ni requieren que el sistema pertenezca a la empresa de origen. No se certifica anonimización de datos o historial Git, ni validación del Designer de Visual Studio o navegación completa.

## Historial de la revisión 1 (commit 66b0ff1)

Se conserva a continuación la procedencia de la primera entrega, ya sustituida por la revisión 2.

Los tres PNG originales fueron generados con la herramienta integrada `image_gen` para la identidad académica de SteeLog. No se usó el CLI ni una API externa configurada por el proyecto.

## Uso y criterio visual

- `steelog-logo-dark.png`: marca oscura para reportes sobre papel blanco.
- `steelog-logo-light.png`: marca clara para el login y la barra lateral oscura existente.
- `steelog-symbol.png`: símbolo de perfiles de acero apilados, relacionado con el inventario de construcción.
- `steelog.ico`: conversión técnica del símbolo a tamaños 16, 24, 32, 48, 64, 128 y 256 px.
- `steelog-background.png`: reducción técnica del símbolo para el panel de inicio.

Se conserva el verde de la aplicación para no introducir otra paleta. La composición horizontal se adapta a la barra lateral y las cabeceras PDF; el símbolo aislado identifica el ejecutable sin depender de texto pequeño. Se mantiene la estructura y tipografía de los formularios, sin animaciones nuevas: energía 1, ritmo 1, movimiento 1. Los recursos no acreditan que todos los datos o el historial Git estén anonimizados.

## Prompts utilizados

Cada edición usó como referencia la imagen del paso indicado y solicitó fondo transparente (`transparent_background: true`). Los PNG finales conservan su canal alfa; no se redibujaron mediante código.

### 1. Generación inicial

```text
Use case: logo-brand. Asset type: final horizontal logo for SteeLog, an academic Windows inventory and logistics app for steel framing/construction materials. Primary request: create a distinctive clean professional horizontal brand lockup, one compact steel I-beam-inspired S symbol at left and exact wordmark 'SteeLog' at right (capital S and L). Flat, crisp vector-like raster, no mockup. Symbol uses restrained emerald green #27AE60, dark wordmark #17242B on genuinely transparent background for white PDF reports. Robust fairly bold humanist sans-serif typography, no subtitle, no slogan, no additional text. Symbol should communicate stacked steel profiles and inventory organization, with a simple recognizable silhouette that can work as a desktop app icon. Modest clear spacing between symbol and text, horizontally centered lockup, minimal outer margins, all contents fully visible. Avoid WP lettering, gears, globes, sparkles, gradients, shadows, metallic textures, 3D and decorative background. One final logo only, not a sheet of alternatives.
```

### 2. Refinamiento de la marca oscura (referencia: paso 1)

```text
Refine this SteeLog logo into a strictly flat two-color final production logo. Keep the exact word 'SteeLog', the horizontal composition, and the distinctive steel I-beam S-inspired symbol. Remove ALL perspective depth, bevels, grain, outlines, glow, shadows, gradients and edge noise: the symbol should be a single solid emerald green #27AE60 silhouette of the stacked-beam S motif, and the wordmark a single solid deep navy #17242B, crisp smooth clean edges with transparent background. No added text. Compact balanced symbol left, bold readable wordmark right. Produce the clean logo only, no presentation board.
```

### 3. Variante clara (referencia: paso 2)

```text
Make the dark-interface color variant of this exact SteeLog logo. Preserve symbol identity, wordmark letter shapes and exact 'SteeLog' spelling, horizontal layout and proportions. Change only the wordmark color from dark navy to solid near-white #F2F5F3, keeping the symbol emerald green #27AE60. Fully transparent background, no dark background plate, no new text, no gradients, no shadows. Clean final logo for a dark WinForms sidebar.
```

### 4. Símbolo (referencia: paso 2)

```text
Create the desktop app symbol variant of this SteeLog brand. Use ONLY the green stacked steel beam S symbol from the left, faithfully preserving its identity and recognizable silhouette; remove the wordmark entirely. Center the isolated emerald green symbol in a square canvas, with a small safe transparent margin all around. This is a Windows app icon so it must remain legible at small sizes. No text or letters added. Transparent background, clean edges, no glow, shadow, pattern or extra decoration.
```

La generación es no determinista: estos prompts documentan el origen y la intención, no garantizan una reproducción píxel a píxel. Las comprobaciones runtime de recursos, icono y cabeceras están en `Tests/Branding/Run.ps1` (desde la raíz del proyecto WinForms).
