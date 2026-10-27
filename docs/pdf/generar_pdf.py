"""Genera docs/informe.pdf desde docs/informe.md, con pandoc y LuaLaTeX.

Uso, desde la raíz del repo:
    python docs/pdf/generar_pdf.py

Requiere pandoc 3 y LuaLaTeX (MiKTeX o TeX Live) con fontspec, titlesec, needspace, caption, setspace,
etoolbox y xcolor. Prefiere Times New Roman y Consolas cuando están instaladas; en Linux usa
Liberation Serif, DejaVu Sans Mono y Latin Modern Math. Los símbolos de la §5 salen de Latin Modern Math
y DejaVu Sans también en Windows: allí hay que tener DejaVu Sans instalada.
Los integrantes de la portada salen del bloque PORTADA de informe.md; el título y las asignaturas están en
portada.tex, y el estilo, en estilo.tex.
"""
import re
import shutil
import subprocess
import sys
import tempfile
import unicodedata
from pathlib import Path

AQUI = Path(__file__).resolve().parent
docs = AQUI.parent
salida = docs / "informe.pdf"
trabajo = Path(tempfile.mkdtemp(prefix="informe_pdf_"))
(trabajo / "img").mkdir()

md = unicodedata.normalize("NFC", (docs / "informe.md").read_text(encoding="utf-8"))

# 1. Los integrantes de la portada salen del bloque PORTADA del markdown.
portada = re.search(r"<!-- PORTADA_INICIO -->(.*?)<!-- PORTADA_FIN -->", md, re.S)
bloque, cuerpo = portada.group(1), md[portada.end():]
integrantes = [(n.strip(), g.strip()) for n, g in re.findall(r"^\|\s*([^|]+?)\s*\|\s*(C\d+)\s*\|\s*$", bloque, re.M)]
filas = " \\\\[6pt]\n".join(f"{n} & {g}" for n, g in integrantes)
plantilla = (AQUI / "portada.tex").read_text(encoding="utf-8")
inicio, fin = plantilla.index(r"\begin{tabular}"), plantilla.index(r"\end{tabular}")
cabecera = plantilla[inicio:plantilla.index("\n", inicio) + 1]
(trabajo / "portada.tex").write_text(plantilla[:inicio] + cabecera + filas + " \\\\\n" + plantilla[fin:], encoding="utf-8")

# 2. Imágenes: se copian a una carpeta sin espacios en el nombre.
def imagen(m):
    ruta = docs / m.group(2).replace("%20", " ")
    destino = trabajo / "img" / ruta.name
    shutil.copy(ruta, destino)
    return f"![{m.group(1)}](img/{ruta.name})"


cuerpo = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)", imagen, cuerpo)


# 3. Tablas: el ancho de cada columna, proporcional a su contenido (si no, pandoc las hace iguales).
def ajustar(tabla):
    filas = [re.split(r"(?<!\\)\|", f.strip().strip("|")) for f in tabla]
    contenido = [f for i, f in enumerate(filas) if i != 1]
    ncol = len(filas[0])
    pesos = []
    for c in range(ncol):
        celdas = [f[c].strip() if c < len(f) else "" for f in contenido]
        largo = max(len(x) for x in celdas)
        # Una palabra (sobre todo en monoespaciado) no se corta: la columna tiene que poder contenerla.
        # El monoespaciado es más ancho que el texto normal, por eso pesa más.
        palabra = max((len(t.strip("`*")) * (2.5 if t.startswith("`") else 1.15)
                       for x in celdas for t in re.findall(r"`[^`]+`|\S+", x)), default=0)
        pesos.append(max(min(max(largo, 4), 70), round(palabra)))
    total = sum(pesos)
    guiones = [max(3, round(p * 100 / total)) for p in pesos]
    tabla[1] = "|" + "|".join("-" * g for g in guiones) + "|"
    return tabla


def partir_codigo(linea):
    """Un fragmento de código largo se separa por sus comas y sus flechas, para que LaTeX pueda cortar la línea.

    Los fragmentos se recorren en pares de comillas, de izquierda a derecha: así nunca se toma por código el
    texto que queda entre el cierre de un fragmento y la apertura del siguiente.
    """
    def partir(m):
        codigo = m.group(1)
        piezas = re.split(r"(, | → | ← )", codigo)
        if len(codigo) < 40 or len(piezas) == 1:
            return m.group(0)
        return "".join(p if i % 2 else f"`{p}`" for i, p in enumerate(piezas))
    return re.sub(r"`([^`\n]+)`", partir, linea)


lineas, salida_lineas, i, en_bloque = cuerpo.split("\n"), [], 0, False
while i < len(lineas):
    if lineas[i].startswith("```"):
        en_bloque = not en_bloque
    elif not en_bloque:
        lineas[i] = partir_codigo(lineas[i])
    # Un título o un rótulo en negrita no queda solo al pie de la página, separado de lo que presenta.
    if re.match(r"^(#{2,4} |\*\*[^*]+\*\*\s*$)", lineas[i]):
        salida_lineas.append("\\needspace{6\\baselineskip}")
        salida_lineas.append("")
    if lineas[i].startswith("|") and i + 1 < len(lineas) and re.match(r"^\|[\s:|-]+\|$", lineas[i + 1]):
        j = i
        while j < len(lineas) and lineas[j].startswith("|"):
            j += 1
        salida_lineas += ajustar(lineas[i:j])
        i = j
    else:
        salida_lineas.append(lineas[i])
        i += 1
(trabajo / "cuerpo.md").write_text("\n".join(salida_lineas), encoding="utf-8")

# 4. pandoc → LuaLaTeX.
cmd = ["pandoc", "cuerpo.md", "-o", str(salida), "--pdf-engine=lualatex",
       "-f", "markdown-implicit_figures+implicit_figures",
       "--shift-heading-level-by=-1", "--toc", "--toc-depth=2",
       "-V", "documentclass=article", "-V", "papersize=a4", "-V", "fontsize=12pt", "-V", "lang=es",
       "-V", "geometry:margin=2.5cm", "-V", "toc-title=Índice", "-V", "colorlinks=true", "-V", "linkcolor=black",
       "-V", "toccolor=black", "-V", "urlcolor=azulclaro",
       "-H", str(AQUI / "estilo.tex"), "-B", "portada.tex"]
r = subprocess.run(cmd, cwd=trabajo, capture_output=True, text=True, encoding="utf-8")
shutil.rmtree(trabajo, ignore_errors=True)
if r.returncode:
    print(r.stdout[-3000:], r.stderr[-3000:])
sys.exit(r.returncode)
