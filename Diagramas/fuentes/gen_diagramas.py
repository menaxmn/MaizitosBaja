#!/usr/bin/env python3
"""Genera los 5 diagramas ER de Maizitos Baja (Crow's Foot, Classic, Connect to
Columns, UML e IDEF1X) a partir de meta.json, que sale de information_schema
de la base real. Uso: python3 gen_diagramas.py <carpeta_salida>"""
import json, os, re, subprocess, sys

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out'
os.makedirs(OUT, exist_ok=True)
M = json.load(open(os.path.join(os.path.dirname(__file__), 'meta.json')))

# ---------- modelo ----------
pk_of = {t: v.split(',') for t, v in M['pk']}
fk_cols = {(t, c) for t, c, *_ in M['fks']}
nullable = {(r[0], r[1]): r[3] == 'YES' for r in M['cols']}
tables = {}
for r in M['cols']:
    t, c, typ, nul, key, dflt, extra, pos, gen = r
    if t not in M['tables']:
        continue
    typ = re.sub(r"^enum\(.*\)$", 'ENUM', typ)
    typ = typ.replace('tinyint(1)', 'BOOLEAN').upper()
    tables.setdefault(t, []).append(dict(name=c, type=typ, pk=c in pk_of[t], fk=(t, c) in fk_cols))

# (hijo, columna) -> (padre, columna_pk, etiqueta_padre, etiqueta_hijo)
CARD = {
    ('Articulo', 'CategoriaId'): ('1', '0..*'),
    ('Articulo', 'ProveedorId'): ('0..1', '0..*'),
    ('CorteCaja', 'UsuarioId'): ('1', '0..*'),
    ('Venta', 'CorteId'): ('1', '0..*'),
    ('Venta', 'UsuarioId'): ('1', '0..*'),
    ('Pago', 'VentaId'): ('1', '0..3'),
    ('DetalleVenta', 'VentaId'): ('1', '1..*'),
    ('DetalleVenta', 'ProductoId'): ('0..1', '0..*'),
    ('DetalleVenta', 'ArticuloId'): ('0..1', '0..*'),
    ('DetalleVentaArticulo', 'DetalleVentaId'): ('1', '0..*'),
    ('DetalleVentaArticulo', 'ArticuloId'): ('1', '0..*'),
    ('MovimientoInventario', 'ArticuloId'): ('1', '0..*'),
    ('MovimientoInventario', 'UsuarioId'): ('1', '0..*'),
    ('MovimientoInventario', 'ProveedorId'): ('0..1', '0..*'),
    ('MovimientoInventario', 'VentaId'): ('0..1', '0..*'),
}
rels = []
for t, c, name, pt, pc, dr, ur in M['fks']:
    assert (t, c) in CARD, (t, c)
    rels.append(dict(child=t, col=c, parent=pt, pcol=pc, name=name, pl=CARD[(t, c)][0], cl=CARD[(t, c)][1],
                     ident=(c in pk_of[t])))

ARROW = {  # símbolos Crow's Foot en Graphviz
    ('1', 'p'): 'teetee', ('0..1', 'p'): 'teeodot',
    ('0..*', 'c'): 'crowodot', ('1..*', 'c'): 'crowtee', ('0..3', 'c'): 'crowodot',
}

COL_HEAD = '#1B5E20'
FONT = 'Arial'


def esc(s):
    return s.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')


def tabla_html(t, estilo):
    cols = tables[t]
    if estilo == 'uml':
        rows = ''.join(
            f'<TR><TD ALIGN="LEFT" PORT="c_{c["name"]}"><FONT POINT-SIZE="10">{"«PK» " if c["pk"] else ""}{"«FK» " if c["fk"] else ""}'
            f'{"<B>" if c["pk"] else ""}{esc(c["name"])}{"</B>" if c["pk"] else ""} : {esc(c["type"])}</FONT></TD></TR>' for c in cols)
        return (f'<<TABLE BORDER="1" CELLBORDER="0" CELLSPACING="0" CELLPADDING="3" BGCOLOR="white">'
                f'<TR><TD BGCOLOR="#E8F5E9"><B>{t}</B></TD></TR><HR/>{rows}</TABLE>>')
    if estilo == 'idef1x':
        dep = any(r['child'] == t and r['ident'] for r in rels)
        pks = [c for c in cols if c['pk']]
        nopk = [c for c in cols if not c['pk']]

        def fila(c):
            mark = ' (FK)' if c['fk'] else ''
            return (f'<TR><TD ALIGN="LEFT" PORT="c_{c["name"]}"><FONT POINT-SIZE="10">{"<B>" if c["pk"] else ""}'
                    f'{esc(c["name"])}{mark}{"</B>" if c["pk"] else ""}</FONT></TD></TR>')
        rows = ''.join(fila(c) for c in pks) + '<HR/>' + ''.join(fila(c) for c in nopk)
        style = ' STYLE="ROUNDED"' if dep else ''
        return (f'<<TABLE BORDER="2" CELLBORDER="0" CELLSPACING="0" CELLPADDING="3" BGCOLOR="white"{style}>'
                f'<TR><TD><B>{t}</B></TD></TR><HR/>{rows}</TABLE>>')
    # crowsfoot / classic / columns
    rows = ''
    for c in cols:
        tag = 'PK' if c['pk'] else ('FK' if c['fk'] else '')
        if c['pk'] and c['fk']:
            tag = 'PK,FK'
        rows += (f'<TR><TD ALIGN="LEFT" PORT="c_{c["name"]}" WIDTH="26"><FONT POINT-SIZE="9" COLOR="#555555">{tag or '&nbsp;'}</FONT></TD>'
                 f'<TD ALIGN="LEFT"><FONT POINT-SIZE="10">{"<B>" if c["pk"] else ""}{esc(c["name"])}{"</B>" if c["pk"] else ""}</FONT></TD>'
                 f'<TD ALIGN="LEFT"><FONT POINT-SIZE="9" COLOR="#555555">{esc(c["type"])}</FONT></TD></TR>')
    return (f'<<TABLE BORDER="1" CELLBORDER="0" CELLSPACING="0" CELLPADDING="3" BGCOLOR="white">'
            f'<TR><TD COLSPAN="3" BGCOLOR="{COL_HEAD}"><FONT COLOR="white"><B>{t}</B></FONT></TD></TR>{rows}</TABLE>>')


def grafo(estilo, titulo):
    L = ['digraph ER {',
         f'  graph [rankdir=LR, fontname="{FONT}", fontsize=18, label="{titulo}", labelloc=t, pad=0.3, nodesep=0.45, ranksep=1.3, '
         f'splines={"polyline" if estilo == "columns" else "true"}, dpi=120];',
         f'  node [shape=plain, fontname="{FONT}"];',
         f'  edge [fontname="{FONT}", fontsize=10, color="#333333"];']
    for t in tables:
        L.append(f'  {t} [label={tabla_html(t, estilo)}];')
    for r in rels:
        ch, pa = r['child'], r['parent']
        if estilo == 'crowsfoot':
            L.append(f'  {pa} -> {ch} [dir=both, arrowtail={ARROW[(r["pl"], "p")]}, arrowhead={ARROW[(r["cl"], "c")]}'
                     f', taillabel="{r["pl"]}", headlabel="{r["cl"]}", labeldistance=2.2, labelfontsize=9];')
        elif estilo == 'classic':
            # Clásica: línea con punto lleno en el lado "muchos" y etiquetas 1 / N
            lp = 'N' if r['cl'] != '1..*' else '1..N'
            L.append(f'  {pa} -> {ch} [dir=both, arrowtail=none, arrowhead=dot, taillabel="{"1" if r["pl"] == "1" else "0..1"}", '
                     f'headlabel="{lp}", labeldistance=2, labelfontsize=9];')
        elif estilo == 'columns':
            L.append(f'  {ch}:c_{r["col"]}:w -> {pa}:c_{r["pcol"]}:e [dir=both, arrowtail=dot, arrowhead=none, '
                     f'tooltip="{r["name"]}"];')
        elif estilo == 'uml':
            L.append(f'  {pa} -> {ch} [arrowhead=none, taillabel="{r["pl"]}", headlabel="{r["cl"]}", labeldistance=2.2, '
                     f'labelfontsize=9, label="{r["col"]}", fontsize=8, fontcolor="#555555"];')
        elif estilo == 'idef1x':
            sty = 'solid' if r['ident'] else 'dashed'
            tail = 'none' if r['pl'] == '1' else 'odiamond'
            L.append(f'  {pa} -> {ch} [dir=both, style={sty}, arrowtail={tail}, arrowhead=dot, penwidth={2 if r["ident"] else 1.2}];')
    L.append('}')
    return '\n'.join(L)


NOTACIONES = [
    ('crowsfoot', "01_DER_CrowsFoot", "Maizitos Baja · DER en notación Crow's Foot (pata de gallo)"),
    ('classic', '02_DER_Classic', 'Maizitos Baja · DER en notación Classic'),
    ('columns', '03_DER_ConnectToColumns', 'Maizitos Baja · DER en notación Connect to Columns'),
    ('uml', '04_DER_UML', 'Maizitos Baja · DER en notación UML'),
    ('idef1x', '05_DER_IDEF1X', 'Maizitos Baja · DER en notación IDEF1X'),
]
os.makedirs(os.path.join(OUT, 'fuentes'), exist_ok=True)
for estilo, base, titulo in NOTACIONES:
    dot = grafo(estilo, titulo)
    src = os.path.join(OUT, 'fuentes', base + '.dot')
    open(src, 'w', encoding='utf8').write(dot)
    for fmt in ('png', 'pdf', 'svg'):
        subprocess.run(['dot', f'-T{fmt}', src, '-o', os.path.join(OUT, f'{base}.{fmt}')], check=True)
    print('ok', base)
