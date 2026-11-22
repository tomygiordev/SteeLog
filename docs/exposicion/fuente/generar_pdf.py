"""Genera SteeLog-Documento-Tecnico.pdf a partir de documento.html.

Uso:  python generar_pdf.py
Requiere Microsoft Edge (impresión a PDF sin interfaz) y los paquetes Pillow y PyMuPDF.
Numera capítulos, figuras y tablas, arma los índices y completa los números de página
en dos pasadas (la primera imprime, la segunda escribe las páginas reales).
"""
import base64, io, os, re, subprocess, sys, tempfile, time
from pathlib import Path

import fitz  # PyMuPDF
from PIL import Image

# --- Datos de portada: completar si se desea que aparezcan -------------------
AUTOR = "Tomás Roldán Giorgi"
INSTITUCION = "UADER · Facultad de Ciencia y Tecnología"
DOCENTE = "Santiago Raul Diaz<br>Marcos Elias Rios Nuñez"   # varios: separar con <br>
FECHA = "Noviembre de 2026"
# -----------------------------------------------------------------------------

AQUI = Path(__file__).resolve().parent
RAIZ = AQUI.parents[2]
SALIDA = AQUI.parent / "SteeLog-Documento-Tecnico.pdf"
SIMBOLO = RAIZ / "SteeLog" / "SteeLog" / "Resources" / "Branding" / "steelog-symbol.png"
EDGE = [r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"]


def simbolo_b64():
    im = Image.open(SIMBOLO).convert("RGBA")
    im.thumbnail((360, 360), Image.LANCZOS)
    b = io.BytesIO()
    im.save(b, "PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(b.getvalue()).decode()


def portada_meta():
    celdas = [("Asignatura", "Taller de Integración")]
    if INSTITUCION: celdas.append(("Institución", INSTITUCION))
    if AUTOR: celdas.append(("Autor", AUTOR))
    if DOCENTE: celdas.append(("Docentes" if "<br>" in DOCENTE else "Docente", DOCENTE))
    celdas += [("Fecha", FECHA)]
    return "".join(f"<div><small>{k}</small>{v}</div>" for k, v in celdas)


def svg_secuencia():
    xs = {"V": 56, "P": 172, "E": 290, "C": 420, "D": 548, "B": 664}
    nombres = {"V": "Vista", "P": "Presentador", "E": "Executor", "C": "Caso de uso", "D": "Dominio", "B": "PostgreSQL"}
    color = {"V": ("#e2eef9", "#6ea6d6"), "P": ("#e2eef9", "#6ea6d6"), "E": ("#ffffff", "#9fb1a7"),
             "C": ("#fbf2d9", "#d3a53c"), "D": ("#d9f2e3", "#27ae60"), "B": ("#fbe9e6", "#dd8c82")}
    msgs = [("V", "P", "confirmar movimiento", 0), ("P", "E", "ExecuteAsync<RegistrarMovimiento>", 0),
            ("E", "C", "nuevo ámbito · EjecutarAsync(solicitud)", 0), ("C", "C", "Require(Galpón)", 0),
            ("C", "D", "validar cabecera y líneas", 0), ("C", "B", "BEGIN", 0),
            ("C", "B", "SELECT … FOR UPDATE (ids ascendentes)", 0), ("C", "D", "clasificar impacto · aplicar saldo", 0),
            ("D", "C", "saldo nuevo · impacto real", 1), ("C", "B", "UPDATE saldos · INSERT líneas", 0),
            ("C", "B", "COMMIT", 0), ("C", "E", "id del movimiento", 1), ("E", "P", "resultado", 1),
            ("P", "V", "confirmación · remito PDF", 1)]
    paso, y0 = 33, 78
    alto = y0 + paso * len(msgs) + 18
    o = [f'<svg viewBox="0 0 720 {alto}" role="img"><defs>'
         '<marker id="sa" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto">'
         '<path d="M1 1L9 5L1 9" fill="none" stroke="#17211c" stroke-width="1.5"/></marker></defs>']
    for k, x in xs.items():
        f, s = color[k]
        o.append(f'<line x1="{x}" y1="40" x2="{x}" y2="{alto-4}" stroke="#b4c1b9" stroke-dasharray="3 3"/>')
        o.append(f'<rect x="{x-52}" y="8" width="104" height="30" rx="6" fill="{f}" stroke="{s}"/>')
        o.append(f'<text x="{x}" y="28" font-size="11.5" font-weight="600" text-anchor="middle" fill="#17211c">{nombres[k]}</text>')
    extra = 0
    halo = 'stroke="#fbfcfb" stroke-width="4" paint-order="stroke" stroke-linejoin="round"'
    for i, (a, b, t, ret) in enumerate(msgs):
        t = t.replace("<", "&lt;").replace(">", "&gt;")
        y = y0 + i * paso + extra
        if a == b: extra = 12
        if a == b:
            x = xs[a]
            o.append(f'<path d="M{x} {y-8}h26v14h-22" fill="none" stroke="#17211c" stroke-width="1.2" marker-end="url(#sa)"/>')
            o.append(f'<text x="{x+32}" y="{y+3}" font-size="10" fill="#17211c" class="mo" {halo}>{t}</text>')
            continue
        x1, x2 = xs[a], xs[b]
        dash = ' stroke-dasharray="5 3"' if ret else ""
        col = "#7b8a81" if ret else "#17211c"
        o.append(f'<line x1="{x1}" y1="{y}" x2="{x2 - (4 if x2 > x1 else -4)}" y2="{y}" stroke="{col}" stroke-width="1.2"{dash} marker-end="url(#sa)"/>')
        o.append(f'<text x="{(x1+x2)/2}" y="{y-5}" font-size="10" text-anchor="middle" fill="{col}" class="mo" {halo}>{t}</text>')
    o.append("</svg>")
    return "".join(o)


def svg_er():
    W, H = 152, 76
    cx = [8, 194, 380, 560]
    ry = [10, 140, 270]
    ent = {  # nombre: (col, fila, [atributos], destacado)
        "categorias": (0, 0, ["id", "nombre"], 0), "proveedores": (1, 0, ["id", "nombre"], 0),
        "tipo_movimiento": (2, 0, ["id", "nombre · impacto"], 0), "obras": (3, 0, ["id", "nombre · activa"], 0),
        "materiales": (0, 1, ["id · nombre", "stockactual · stockminimo"], 1),
        "ordenes_compra": (1, 1, ["id · nro_orden", "estado · fecha"], 0),
        "movimientos": (2, 1, ["id · nroremito · fecha", "origen · destino (tipo, id)"], 1),
        "recortes": (0, 2, ["id · medida · ancho · altura", "cantidad · estado"], 0),
        "ordenes_compra_detalle": (1, 2, ["pedida · recibida", "stock antes / después"], 0),
        "movimientomaterial": (2, 2, ["cantidad · precio_unitario", "stock_actual · impacto real"], 1),
    }
    o = ['<svg viewBox="0 0 720 384" role="img">']
    def linea(d): o.append(f'<path d="{d}" fill="none" stroke="#7b8a81" stroke-width="1.3"/>')
    for c in (0, 1, 2):  # relaciones verticales
        x = cx[c] + W / 2
        linea(f"M{x} {ry[0]+H}V{ry[1]}"); linea(f"M{x} {ry[1]+H}V{ry[2]}")
    linea(f"M{cx[3]+W/2} {ry[0]+H}V{ry[1]+H/2}H{cx[2]+W}")                      # obras -> movimientos
    linea(f"M{cx[1]} {ry[2]+H/2}H{cx[1]-12}V{ry[1]+H/2-10}H{cx[0]+W}")          # detalle -> materiales
    linea(f"M{cx[2]+W/2} {ry[2]+H}V{ry[2]+H+20}H{cx[0]+W+12}V{ry[1]+H/2+12}H{cx[0]+W}")  # linea mov -> materiales
    for n, (c, r, at, hl) in ent.items():
        x, y = cx[c], ry[r]
        f, s = ("#e3f4ea", "#27ae60") if hl else ("#ffffff", "#9fb1a7")
        o.append(f'<rect x="{x}" y="{y}" width="{W}" height="{H}" rx="6" fill="{f}" stroke="{s}"/>')
        o.append(f'<rect x="{x}" y="{y}" width="{W}" height="24" rx="6" fill="{s}"/><rect x="{x}" y="{y+14}" width="{W}" height="10" fill="{s}"/>')
        o.append(f'<text x="{x+9}" y="{y+16.5}" font-size="10.5" font-weight="600" fill="{"#fff" if hl else "#17211c"}" class="mo">{n}</text>')
        for i, a in enumerate(at):
            o.append(f'<text x="{x+9}" y="{y+42+i*16}" font-size="9" fill="#4a5a51">{a}</text>')
    o.append(f'<text x="{cx[3]}" y="{ry[1]+H+40}" font-size="9" fill="#7b8a81">Cada línea une una entidad «uno»</text>')
    o.append(f'<text x="{cx[3]}" y="{ry[1]+H+54}" font-size="9" fill="#7b8a81">(arriba o a la izquierda) con una</text>')
    o.append(f'<text x="{cx[3]}" y="{ry[1]+H+68}" font-size="9" fill="#7b8a81">entidad «muchos».</text>')
    o.append("</svg>")
    return "".join(o)


def preparar(html):
    html = html.replace("%%SYMBOL%%", simbolo_b64()).replace("%%COVERMETA%%", portada_meta())
    html = html.replace("<!--SEQ-->", svg_secuencia()).replace("<!--ER-->", svg_er())
    toc, lof, lot = [], [], []
    cap = [0, 0]

    def titulo(m):
        tag, attrs, cuerpo = m.group(1), m.group(2), m.group(3)
        ident = re.search(r'id="([^"]+)"', attrs).group(1)
        if tag == "h2":
            if "nonum" in attrs:
                if ident.startswith("indice"): return m.group(0)
                am = re.match(r'<span class="n">(.*?)</span>(.*)', cuerpo)
                toc.append(("l1", ident, am.group(1) if am else "", am.group(2) if am else cuerpo))
                return m.group(0)
            cap[0] += 1; cap[1] = 0
            toc.append(("l1", ident, str(cap[0]), cuerpo))
            return f'<h2{attrs}><span class="n">Capítulo {cap[0]}</span>{cuerpo}</h2>'
        cap[1] += 1
        num = f"{cap[0]}.{cap[1]}"
        toc.append(("l2", ident, num, cuerpo))
        return f'<h3{attrs}><span class="n">{num}</span>{cuerpo}</h3>'

    html = re.sub(r"<(h2|h3)([^>]*)>(.*?)</\1>", titulo, html, flags=re.S)

    nf = [0]
    def figura(m):
        nf[0] += 1
        lof.append(("lf", m.group(1), f"Figura {nf[0]}", m.group(3)))
        return f'<figure id="{m.group(1)}">{m.group(2)}<figcaption><b>Figura {nf[0]}.</b> {m.group(3)}</figcaption>'
    html = re.sub(r'<figure id="([^"]+)">(.*?)<figcaption>(.*?)</figcaption>', figura, html, flags=re.S)

    nt = [0]
    def tabla(m):
        nt[0] += 1
        lot.append(("lf", f"tab-{nt[0]}", f"Tabla {nt[0]}", m.group(2)))
        return f'<div class="tbl{m.group(1) or ""}" id="tab-{nt[0]}"><table><caption><b>Tabla {nt[0]}.</b> {m.group(2)}</caption>'
    html = re.sub(r'<div class="tbl( long)?"><table><caption>(.*?)</caption>', tabla, html, flags=re.S)

    def nav(items):
        return "".join(
            f'<a class="{c}" href="#{i}"><span class="t"><span class="k">{k}</span> {t}</span>'
            f'<span class="d"></span><span class="p">@@{i}@@</span></a>' for c, i, k, t in items)
    html = html.replace("<!--TOC-->", nav(toc)).replace("<!--LOF-->", nav(lof)).replace("<!--LOT-->", nav(lot))
    return html, [i for _, i, _, _ in toc + lof + lot]


def imprimir(html, pdf):
    edge = next((e for e in EDGE if os.path.exists(e)), None)
    if not edge: sys.exit("No se encontró Microsoft Edge.")
    with tempfile.TemporaryDirectory() as tmp:
        src = Path(tmp) / "doc.html"
        src.write_text(html, encoding="utf-8")
        if pdf.exists(): pdf.unlink()
        subprocess.run([edge, "--headless=new", "--disable-gpu", "--no-pdf-header-footer",
                        "--generate-pdf-document-outline", f"--user-data-dir={Path(tmp) / 'perfil'}",
                        f"--print-to-pdf={pdf}", src.as_uri()],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=180)
        for _ in range(120):
            if pdf.exists() and pdf.stat().st_size > 0:
                try:
                    fitz.open(pdf).close(); break
                except Exception:
                    pass
            time.sleep(0.5)
        else:
            sys.exit("Edge no generó el PDF.")
        time.sleep(1)


def paginas(pdf, ids):
    doc = fitz.open(pdf)
    nombres = doc.resolve_names()
    res = {i: nombres[i]["page"] + 1 for i in ids if i in nombres and nombres[i].get("page", -1) >= 0}
    total = doc.page_count
    doc.close()
    return res, total


def main():
    plantilla, ids = preparar((AQUI / "documento.html").read_text(encoding="utf-8"))
    def con(pags):
        return re.sub(r"@@([^@]+)@@", lambda m: str(pags.get(m.group(1), "00")), plantilla)
    tmp_pdf = Path(tempfile.gettempdir()) / "steelog-doc-pass.pdf"
    pags = {}
    for pasada in range(1, 5):
        imprimir(con(pags), tmp_pdf)
        nuevas, total = paginas(tmp_pdf, ids)
        faltan = [i for i in ids if i not in nuevas]
        print(f"pasada {pasada}: {total} páginas, {len(nuevas)}/{len(ids)} destinos" + (f", faltan {faltan}" if faltan else ""))
        if nuevas == pags: break
        pags = nuevas
    if SALIDA.exists(): SALIDA.unlink()
    doc = fitz.open(tmp_pdf)
    doc.set_metadata({"title": "SteeLog · Documento técnico de arquitectura y funcionamiento",
                      "author": AUTOR or "SteeLog", "subject": "Taller de Integración",
                      "keywords": "arquitectura limpia, inventario, steel framing, C#, PostgreSQL"})
    doc.save(SALIDA, garbage=3, deflate=True)
    doc.close()
    print("OK ->", SALIDA)


if __name__ == "__main__":
    main()
