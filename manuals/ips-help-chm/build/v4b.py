import re, sys
from pathlib import Path
sys.stdout.reconfigure(encoding="utf-8")

SRC = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DATA = SRC / "data"
RES = SRC / "manuals" / "ips-help-chm" / "result"
ROOT = SRC / "manuals" / "ips-help-chm"
o = []
def p(s=""): o.append(str(s))
def rfile(tid, part=None):
    for f in sorted(RES.rglob(tid + "_*.md")):
        if part and part not in f.name: continue
        return f
files = [f for f in RES.rglob("*.md") if f.name not in ("INDEX.md", "REPORT.md") and not f.name.startswith("VERIFY")]

p("## 5. Подписи картинок")
ph1 = ph2 = 0
for f in files:
    t = f.read_text(encoding="utf-8", errors="replace")
    ph1 += t.count("Изображение выглядит")
    ph2 += t.count("Содержимое, созданное")
imgs = sum(len(re.findall(r"!\[[^\]]*\]\([^)]*\)", f.read_text(encoding="utf-8", errors="replace"))) for f in DATA.glob("*.md"))
left = sum(f.read_text(encoding="utf-8", errors="replace").count("![") for f in files)
p(f"- `Изображение выглядит` в результате: {ph1}")
p(f"- `Содержимое, созданное` в результате: {ph2}")
p(f"- картинок в исходниках: {imgs}; `![` в результате: {left}")
alts, kept, ex = 0, 0, []
for f in sorted(DATA.glob("*.md")):
    d = rfile(f.stem)
    if not d: continue
    dl = d.read_text(encoding="utf-8", errors="replace").splitlines()
    dt = "\n".join(dl)
    for m in re.finditer(r"!\[([^\]]*)\]\(([^)]*)\)", f.read_text(encoding="utf-8", errors="replace")):
        alt = m.group(1).strip()
        if not alt or "Изображение выглядит" in alt: continue
        alts += 1
        if alt in dt:
            kept += 1
            if len(ex) < 10:
                line = next((x for x in dl if alt in x), "")
                ex.append((f.stem, m.group(0), line.strip()))
p(f"- непустых ALT (без OCR): {alts}; ALT-текст присутствует в результате: {kept}")
p("- 10 примеров до/после (текст ALT сохранился, разметка — нет):")
for s, before, after in ex:
    p(f"  - `{s}`: ДО `{before[:110]}` | ПОСЛЕ `{after[:110]}`")
p()

p("## 6. `<post>`")
sh = [(f.stem, i, l.strip()) for f in sorted(DATA.glob("*.md")) for i, l in enumerate(f.read_text(encoding="utf-8", errors="replace").splitlines(), 1) if "<post>" in l]
dh = [(f.name, i, l.strip()) for f in files for i, l in enumerate(f.read_text(encoding="utf-8", errors="replace").splitlines(), 1) if "<post>" in l]
p(f"- в исходниках (буквально `<post>`): {len(sh)}")
p(f"- в результате (буквально `<post>`): {len(dh)}")
for h in dh: p(f"  - `{h[0]}` строка {h[1]}: {h[2][:140]}")
p("- в исходнике 3475.md это ALT картинки `![post](...)`:")
src = (DATA / "3475.md").read_text(encoding="utf-8", errors="replace").splitlines()
for i, l in enumerate(src, 1):
    if "![post]" in l:
        p(f"  ДО 3475.md:{i}: {l.strip()}")
d = rfile("3475"); dl = d.read_text(encoding="utf-8", errors="replace").splitlines()
for i, l in enumerate(dl, 1):
    if "<post>" in l:
        p(f"  ПОСЛЕ {d.name}:{i}: {l.strip()}")
p()

p("## 7. Таблицы целиком")
def tab(path):
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    for i, l in enumerate(lines):
        if l.strip().startswith("|") and i + 1 < len(lines) and re.match(r"^\s*\|[\s:|-]+\|\s*$", lines[i+1]):
            return lines[i:i+5], i + 1
    return [], 0
for tid, part in [("4400", "-p1"), ("3722", None), ("3542", None)]:
    s = DATA / (tid + ".md"); d = rfile(tid, part)
    p(f"### {tid}")
    tb, n = tab(s)
    p(f"ДО `{s.name}` со строки {n}:")
    for x in tb: p("  " + (x if x.strip() else "(пустая строка)"))
    if d:
        tb2, n2 = tab(d)
        p(f"ПОСЛЕ `{d.name}` со строки {n2}:")
        for x in tb2: p("  " + (x if x.strip() else "(пустая строка)"))
    p()

p("## 8. INDEX.md раздела «Руководство пользователя»")
idx = RES / "rukovodstvo-polzovatelya" / "INDEX.md"
il = idx.read_text(encoding="utf-8", errors="replace").splitlines()
p(f"path: `{idx}`; строк: {len(il)}")
p("Первые 40 строк:")
for i, l in enumerate(il[:40], 1): p(f"{i:2}| {l}")
nested = sum(1 for l in il if re.match(r"^\s{2,}- \[", l))
p()
p(f"- вложенных строк (отступ 2+): {nested} -> индекс плоский, не по иерархии")
p(f"- пометок `(только оглавление)`: {sum(1 for l in il if 'только оглавление' in l)} (ожидается 78)")
p(f"- всего записей `- [`: {sum(1 for l in il if l.startswith('- ['))}")
p()

p("## 9. Версия")
hhp = list(Path(r"D:\parsing").rglob("*.hhp"))
hh = [x.name for x in Path(r"D:\parsing").rglob("*.hh*")]
chm = Path(r"D:\parsing\chm-converter\resources\IPSHelp.chm")
raw = chm.open("rb").read(8)
p(f"- файлов .hhp в D:\\parsing: {len(hhp)}")
p(f"- файлы .hhc/.hhk: {hh}")
p(f"- CHM: ITSF={raw[:4]!r}, поле версии формата={int.from_bytes(raw[4:8], 'little')} (это версия формата CHM, не IPS)")
p(f"- в результате version = \"IPSHelp\" (метка из core/index.md, не номер)")
p("- номер версии IPS в данных не найден -> version: unknown, см. VERSION.md")
p()
Path(RES / "_v4_part2.txt").write_text("\n".join(o), encoding="utf-8")
print("part2 ok, lines:", len(o))
