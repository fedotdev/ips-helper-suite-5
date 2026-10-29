import re, sys
from pathlib import Path
sys.stdout.reconfigure(encoding="utf-8")

SRC = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DATA = SRC / "data"
RES = SRC / "manuals" / "ips-help-chm" / "result"
ROOT = SRC / "manuals" / "ips-help-chm"

files = [f for f in RES.rglob("*.md") if f.name not in ("INDEX.md", "REPORT.md") and not f.name.startswith("VERIFY")]
def fm(f):
    t = f.read_text(encoding="utf-8", errors="replace")
    m = re.match(r"^---\n(.*?)\n---", t, re.S)
    if not m: return {}
    return dict((k.strip(), v.strip()) for k, _, v in (l.partition(":") for l in m.group(1).splitlines()))

n_path = sum(1 for f in files if "path" in fm(f))
n_parent = sum(1 for f in files if "parent_id" in fm(f))
n_ver_ipshelp = sum(1 for f in files if fm(f).get("version") == '"IPSHelp"')
n_ver_unknown = sum(1 for f in files if fm(f).get("version") == '"unknown"')
star = sum(1 for f in files for l in f.read_text(encoding="utf-8", errors="replace").splitlines() if "****" in l)
odd = sum(1 for f in files for l in f.read_text(encoding="utf-8", errors="replace").splitlines() if l.count("**") % 2 == 1)
ocr = sum(f.read_text(encoding="utf-8", errors="replace").count("Изображение выглядит") for f in files)
imgs = sum(len(re.findall(r"!\[[^\]]*\]\([^)]*\)", f.read_text(encoding="utf-8", errors="replace"))) for f in DATA.glob("*.md"))
left = sum(f.read_text(encoding="utf-8", errors="replace").count("![") for f in files)
alts = kept = 0
for f in sorted(DATA.glob("*.md")):
    d = next(iter(sorted(RES.rglob(f.stem + "_*.md"))), None)
    if not d: continue
    dt = d.read_text(encoding="utf-8", errors="replace")
    for m in re.finditer(r"!\[([^\]]*)\]\(([^)]*)\)", f.read_text(encoding="utf-8", errors="replace")):
        a = m.group(1).strip()
        if not a or "Изображение выглядит" in a: continue
        alts += 1
        if a in dt: kept += 1
post_src = sum(1 for f in DATA.glob("*.md") for l in f.read_text(encoding="utf-8", errors="replace").splitlines() if "<post>" in l)
post_dst = sum(1 for f in files for l in f.read_text(encoding="utf-8", errors="replace").splitlines() if "<post>" in l)
sep = re.compile(r"^\s*\|(?:\s*:?-{3,}:?\s*\|)+\s*$")
dup = 0
for f in files:
    l = f.read_text(encoding="utf-8", errors="replace").splitlines()
    dup += sum(1 for i in range(len(l) - 1) if sep.match(l[i]) and sep.match(l[i+1]))
idx_path = RES / "rukovodstvo-polzovatelya" / "INDEX.md"
il = idx_path.read_text(encoding="utf-8", errors="replace").splitlines()
markers = sum(1 for l in il if "только оглавление" in l)
nested = sum(1 for l in il if re.match(r"^\s{2,}- \[", l))

part1 = (RES / "_v4_part1.txt").read_text(encoding="utf-8")
part2 = (RES / "_v4_part2.txt").read_text(encoding="utf-8")

summary = []
def a(s=""):
    summary.append(s)
a("## 10. Сводка (все значения посчитаны скриптом)")
a()
a("| Проверка | Значение |")
a("| --- | --- |")
a(f"| файлов в результате | {len(files)} |")
a(f"| файлов с полем path | {n_path} |")
a(f"| файлов с полем parent_id | {n_parent} |")
a(f"| строк с `****` | {star} |")
a(f"| строк с нечётным числом `**` | {odd} |")
a(f"| вхождений `Изображение выглядит` | {ocr} |")
a(f"| картинок в исходниках -> `![` в результате | {imgs} -> {left} |")
a(f"| ALT-текст сохранён в результате | {kept} из {alts} непустых ALT |")
a(f"| `<post>` в исходниках -> в результате | {post_src} -> {post_dst} |")
a(f"| подряд идущих строк-разделителей таблиц в результате | {dup} |")
a(f"| записей `(только оглавление)` в INDEX раздела | {markers} (ожидалось 78) |")
a(f"| вложенных строк в INDEX раздела | {nested} |")
a(f"| version: \"IPSHelp\" / \"unknown\" | {n_ver_ipshelp} / {n_ver_unknown} |")
a()
a("### Что проверить не удалось")
a()
a("- п.2: `parent_id` в файлах нет -> «null только у цепочки длиной 2» и «parent_id существует как topic_id» проверить нельзя;")
a("  число нарушений не считается. Ожидаемое распределение по исходникам: 95 / 346 / 974 / 743 / 129.")
a("- п.3: поля `path` нет -> сравнение последнего сегмента с `title` невозможно.")
a("- п.9: в файлах сейчас `version: \"IPSHelp\"`, а не `unknown`; файлы не менялись по указанию «ничего не меняй».")
a()
a("### Чем получено")
a()
a("- `v4a.py` -> `result/_v4_part1.txt` (пп. 1-4)")
a("- `v4b.py` -> `result/_v4_part2.txt` (пп. 5-9)")
a("- `v4c.py` -> этот файл и `VERSION.md`")

(RES / "VERIFY-4.md").write_text(part1 + "\n" + part2 + "\n" + "\n".join(summary) + "\n", encoding="utf-8")

# VERSION.md
v = []
def b(s=""):
    v.append(s)
b("# Версия справки IPS")
b()
b("**version: unknown** — номер версии IPS в метаданных не найден.")
b()
b("## Что проверено (вывод команд)")
b()
b(f"- файлов `.hhp` в `D:\\parsing`: {len(list(Path(r'D:\parsing').rglob('*.hhp')))}")
b(f"- файлы `.hhc`/`.hhk`: {[x.name for x in Path(r'D:\parsing').rglob('*.hh*')]}")
raw = (Path(r"D:\parsing\chm-converter\resources\IPSHelp.chm")).open("rb").read(8)
b(f"- CHM `resources/IPSHelp.chm`: сигнатура {raw[:4]!r}, поле версии формата = {int.from_bytes(raw[4:8], 'little')} (версия формата CHM, не версия IPS)")
b("- `.hhc`/`.hhk` (cp1251) содержат только `1.0` и `5.0` — это версии формата sitemap/HTML, не IPS")
b("- `properties.xml`: только `1.0` и `8.25` (размер шрифта)")
b("- `core/index.md`: `# Documentation Index - Version IPSHelp` — метка `IPSHelp`, не номер")
b()
ips_hits = 0
examples = []
for p in sorted(DATA.glob("*.md")):
    t = p.read_text(encoding="utf-8", errors="replace")
    for m in re.finditer(r"IPS\s*9[.,]\d+", t):
        ips_hits += 1
        if len(examples) < 3: examples.append((p.stem, m.group(0)))
b(f"- поиск `IPS 9.x` по {len(list(DATA.glob('*.md')))} исходникам: {ips_hits} вхождений")
for s, g in examples:
    b(f"  - {s}: `{g}`")
b()
b("## Состояние файлов")
b()
b(f"- `version: \"IPSHelp\"` — {n_ver_ipshelp} файлов")
b(f"- `version: \"unknown\"` — {n_ver_unknown} файлов")
b()
b("Чтобы привести к `version: unknown`, нужен прогон build-скрипта; файлы не изменялись.")
(ROOT / "VERSION.md").write_text("\n".join(v) + "\n", encoding="utf-8")
print("VERIFY-4.md and VERSION.md written")
print("files:", len(files), "path:", n_path, "parent:", n_parent, "star:", star, "odd:", odd,
      "ocr:", ocr, "imgs:", imgs, "left:", left, "alts:", kept, "/", alts,
      "post:", post_src, "->", post_dst, "dup:", dup, "markers:", markers, "nested:", nested,
      "ver:", n_ver_ipshelp, n_ver_unknown)
