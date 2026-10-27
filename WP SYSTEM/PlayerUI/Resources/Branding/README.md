# Recursos de identidad SteeLog

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
